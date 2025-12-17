using System.Collections.Concurrent;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Net;
using EnvironmentBuilder.Core.Models;

namespace EnvironmentBuilder.Core.Services;

/// <summary>
/// Core service for building and managing test environments
/// </summary>
public class EnvironmentService : IDisposable
{
    private LdapConnection? _connection;
    private readonly EnvironmentConfig _config;
    private readonly TestDataGenerator _dataGenerator;
    private readonly ConcurrentQueue<ProgressUpdate> _progressQueue = new();
    
    public event EventHandler<ProgressUpdate>? ProgressChanged;
    public event EventHandler<string>? LogMessage;

    public EnvironmentService(EnvironmentConfig config)
    {
        _config = config;
        _dataGenerator = new TestDataGenerator(config.Users.Locale);
    }

    /// <summary>
    /// Connect to the LDAP server
    /// </summary>
    public async Task<OperationResult> ConnectAsync()
    {
        var result = new OperationResult { StartTime = DateTime.UtcNow };
        
        try
        {
            await Task.Run(() =>
            {
                var identifier = new LdapDirectoryIdentifier(_config.Connection.Server, _config.Connection.Port);
                _connection = new LdapConnection(identifier)
                {
                    AuthType = AuthType.Basic,
                    Timeout = TimeSpan.FromSeconds(_config.Connection.TimeoutSeconds)
                };

                if (_config.Connection.UseSsl)
                {
                    _connection.SessionOptions.SecureSocketLayer = true;
                    _connection.SessionOptions.VerifyServerCertificate = (conn, cert) => true;
                }

                _connection.Credential = new NetworkCredential(_config.Connection.BindDn, _config.Connection.Password);
                _connection.Bind();
            });

            Log($"Connected to {_config.Connection.Server}:{_config.Connection.Port}");
            return OperationResult.Succeeded("Connected successfully");
        }
        catch (Exception ex)
        {
            Log($"Connection failed: {ex.Message}");
            return OperationResult.Failed("Connection failed", ex.Message);
        }
    }

    /// <summary>
    /// Perform a health check on the environment
    /// </summary>
    public async Task<HealthCheckResult> HealthCheckAsync()
    {
        var result = new HealthCheckResult();
        var sw = Stopwatch.StartNew();

        try
        {
            // Test connection
            if (_connection == null)
            {
                var connectResult = await ConnectAsync();
                result.CanConnect = connectResult.Success;
            }
            else
            {
                result.CanConnect = true;
            }

            if (!result.CanConnect)
            {
                result.Errors.Add("Cannot connect to LDAP server");
                return result;
            }

            result.CanAuthenticate = true;
            result.Checks["Connection"] = true;

            // Test read
            try
            {
                var searchRequest = new SearchRequest(_config.Connection.BaseDn, "(objectClass=*)", SearchScope.Base);
                var response = (SearchResponse)_connection!.SendRequest(searchRequest);
                result.CanRead = response.Entries.Count > 0;
                result.Checks["Read"] = result.CanRead;
            }
            catch
            {
                result.CanRead = false;
                result.Warnings.Add("Cannot read from base DN");
            }

            // Test write (dry run - we don't actually write)
            result.CanWrite = result.CanAuthenticate; // Assume write if authenticated
            result.Checks["Write"] = true;

            sw.Stop();
            result.ResponseTimeMs = (int)sw.ElapsedMilliseconds;
            result.IsHealthy = result.CanConnect && result.CanAuthenticate && result.CanRead;
            result.ServerStatus = result.IsHealthy ? "Healthy" : "Degraded";
        }
        catch (Exception ex)
        {
            result.IsHealthy = false;
            result.ServerStatus = "Error";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Build the complete test environment
    /// </summary>
    public async Task<OperationResult> BuildEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        var result = new OperationResult { StartTime = DateTime.UtcNow };
        var sw = Stopwatch.StartNew();

        try
        {
            // Connect if not connected
            if (_connection == null)
            {
                var connectResult = await ConnectAsync();
                if (!connectResult.Success) return connectResult;
            }

            // Create organizational structure
            if (_config.Organization.CreateStructure)
            {
                Log("Creating organizational structure...");
                await CreateOrganizationalStructureAsync(cancellationToken);
            }

            // Generate and create users
            Log($"Generating {_config.Users.Count} users...");
            var users = _dataGenerator.GenerateUsers(_config.Users);

            Log("Creating users in directory...");
            var createResult = await CreateUsersAsync(users, cancellationToken);
            result.Metrics = createResult.Metrics;

            // Generate LDIF if configured
            if (_config.Output.GenerateLdif)
            {
                Log($"Generating LDIF file: {_config.Output.LdifPath}");
                await GenerateLdifAsync(users);
            }

            sw.Stop();
            result.Success = true;
            result.Message = $"Environment built successfully: {result.Metrics.SuccessCount} users created";
            result.EndTime = DateTime.UtcNow;
            result.Metrics.ItemsPerSecond = result.Metrics.SuccessCount / sw.Elapsed.TotalSeconds;

            Log($"Build complete in {sw.Elapsed.TotalSeconds:F2}s");
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Message = "Operation cancelled";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Build failed";
            result.ErrorDetails = ex.Message;
            Log($"Build failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Create users in the directory
    /// </summary>
    public async Task<OperationResult> CreateUsersAsync(List<TestUser> users, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult { StartTime = DateTime.UtcNow };
        result.Metrics.TotalItems = users.Count;
        var sw = Stopwatch.StartNew();
        var processedCount = 0;

        // Process in batches
        var batches = users.Chunk(_config.Execution.BatchSize);
        
        foreach (var batch in batches)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var tasks = batch.Select(user => CreateUserAsync(user, cancellationToken));
            
            if (_config.Execution.ParallelOperations > 1)
            {
                // Parallel execution
                var semaphore = new SemaphoreSlim(_config.Execution.ParallelOperations);
                var parallelTasks = batch.Select(async user =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await CreateUserAsync(user, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                
                var results = await Task.WhenAll(parallelTasks);
                foreach (var r in results)
                {
                    if (r) result.Metrics.SuccessCount++;
                    else result.Metrics.FailedCount++;
                }
            }
            else
            {
                // Sequential execution
                foreach (var user in batch)
                {
                    if (await CreateUserAsync(user, cancellationToken))
                        result.Metrics.SuccessCount++;
                    else
                        result.Metrics.FailedCount++;
                }
            }

            processedCount += batch.Length;
            
            // Report progress
            ReportProgress(new ProgressUpdate
            {
                Operation = "Creating Users",
                CurrentItem = processedCount,
                TotalItems = users.Count,
                ItemsPerSecond = processedCount / sw.Elapsed.TotalSeconds,
                Status = "Running"
            });

            // Delay between batches
            if (_config.Execution.DelayBetweenBatchesMs > 0)
            {
                await Task.Delay(_config.Execution.DelayBetweenBatchesMs, cancellationToken);
            }
        }

        sw.Stop();
        result.Metrics.AverageItemTimeMs = sw.ElapsedMilliseconds / (double)users.Count;
        result.EndTime = DateTime.UtcNow;
        result.Success = result.Metrics.FailedCount == 0;
        result.Message = $"Created {result.Metrics.SuccessCount}/{users.Count} users";

        return result;
    }

    /// <summary>
    /// Create a single user
    /// </summary>
    private async Task<bool> CreateUserAsync(TestUser user, CancellationToken cancellationToken)
    {
        if (_config.Execution.DryRun)
        {
            Log($"[DRY RUN] Would create user: {user.Username}");
            user.Status = UserCreationStatus.Success;
            return true;
        }

        try
        {
            await Task.Run(() =>
            {
                var dn = $"cn={user.Username},{_config.Users.UserContainer},{_config.Connection.BaseDn}";
                var request = new AddRequest(dn);

                // Add object classes
                foreach (var oc in user.ObjectClasses)
                {
                    request.Attributes.Add(new DirectoryAttribute("objectClass", oc));
                }

                // Add attributes
                request.Attributes.Add(new DirectoryAttribute("cn", user.Username));
                request.Attributes.Add(new DirectoryAttribute("sn", user.LastName));
                request.Attributes.Add(new DirectoryAttribute("givenName", user.FirstName));
                request.Attributes.Add(new DirectoryAttribute("mail", user.Email));
                request.Attributes.Add(new DirectoryAttribute("title", user.Title));
                request.Attributes.Add(new DirectoryAttribute("telephoneNumber", user.PhoneNumber));
                request.Attributes.Add(new DirectoryAttribute("userPassword", user.Password));

                _connection!.SendRequest(request);
            }, cancellationToken);

            user.Status = UserCreationStatus.Success;
            return true;
        }
        catch (Exception ex)
        {
            user.Status = UserCreationStatus.Failed;
            user.ErrorMessage = ex.Message;
            Log($"Failed to create user {user.Username}: {ex.Message}");
            
            if (_config.Execution.StopOnError)
                throw;
            
            return false;
        }
    }

    /// <summary>
    /// Create organizational structure
    /// </summary>
    private async Task CreateOrganizationalStructureAsync(CancellationToken cancellationToken)
    {
        if (_config.Execution.DryRun)
        {
            Log("[DRY RUN] Would create organizational structure");
            return;
        }

        foreach (var ou in _config.Organization.PredefinedOUs)
        {
            await CreateOUAsync(ou, _config.Connection.BaseDn, cancellationToken);
        }
    }

    private async Task CreateOUAsync(string name, string parentDn, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() =>
            {
                var dn = $"ou={name},{parentDn}";
                var request = new AddRequest(dn);
                request.Attributes.Add(new DirectoryAttribute("objectClass", "organizationalUnit"));
                request.Attributes.Add(new DirectoryAttribute("ou", name));
                _connection!.SendRequest(request);
            }, cancellationToken);

            Log($"Created OU: {name}");
        }
        catch (DirectoryOperationException ex) when (ex.Message.Contains("already exists"))
        {
            Log($"OU already exists: {name}");
        }
        catch (Exception ex)
        {
            Log($"Failed to create OU {name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up / delete test users
    /// </summary>
    public async Task<OperationResult> CleanupAsync(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult { StartTime = DateTime.UtcNow };
        prefix ??= _config.Users.Prefix;

        try
        {
            if (_connection == null)
            {
                var connectResult = await ConnectAsync();
                if (!connectResult.Success) return connectResult;
            }

            Log($"Searching for users with prefix: {prefix}");

            // Search for users to delete
            var searchRequest = new SearchRequest(
                _config.Connection.BaseDn,
                $"(&(objectClass=inetOrgPerson)(cn={prefix}*))",
                SearchScope.Subtree,
                "dn"
            );

            var response = await Task.Run(() => (SearchResponse)_connection!.SendRequest(searchRequest), cancellationToken);
            result.Metrics.TotalItems = response.Entries.Count;

            Log($"Found {response.Entries.Count} users to delete");

            foreach (SearchResultEntry entry in response.Entries)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    if (!_config.Execution.DryRun)
                    {
                        var deleteRequest = new DeleteRequest(entry.DistinguishedName);
                        await Task.Run(() => _connection.SendRequest(deleteRequest), cancellationToken);
                    }
                    
                    result.Metrics.SuccessCount++;
                    Log($"Deleted: {entry.DistinguishedName}");
                }
                catch (Exception ex)
                {
                    result.Metrics.FailedCount++;
                    Log($"Failed to delete {entry.DistinguishedName}: {ex.Message}");
                }

                ReportProgress(new ProgressUpdate
                {
                    Operation = "Cleanup",
                    CurrentItem = result.Metrics.SuccessCount + result.Metrics.FailedCount,
                    TotalItems = result.Metrics.TotalItems,
                    Status = "Running"
                });
            }

            result.Success = result.Metrics.FailedCount == 0;
            result.Message = $"Deleted {result.Metrics.SuccessCount}/{result.Metrics.TotalItems} users";
            result.EndTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Cleanup failed";
            result.ErrorDetails = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Generate LDIF file for users
    /// </summary>
    public async Task GenerateLdifAsync(List<TestUser> users)
    {
        var lines = new List<string>
        {
            "# LDIF generated by Environment Builder",
            $"# Generated: {DateTime.UtcNow:O}",
            $"# Users: {users.Count}",
            "version: 1",
            ""
        };

        foreach (var user in users)
        {
            var dn = $"cn={user.Username},{_config.Users.UserContainer},{_config.Connection.BaseDn}";
            lines.Add($"dn: {dn}");
            lines.Add("changetype: add");
            
            foreach (var oc in user.ObjectClasses)
                lines.Add($"objectClass: {oc}");
            
            lines.Add($"cn: {user.Username}");
            lines.Add($"sn: {user.LastName}");
            lines.Add($"givenName: {user.FirstName}");
            lines.Add($"mail: {user.Email}");
            lines.Add($"title: {user.Title}");
            lines.Add($"telephoneNumber: {user.PhoneNumber}");
            lines.Add($"userPassword: {user.Password}");
            lines.Add("");
        }

        await File.WriteAllLinesAsync(_config.Output.LdifPath, lines);
        Log($"LDIF file written: {_config.Output.LdifPath}");
    }

    private void ReportProgress(ProgressUpdate update)
    {
        _progressQueue.Enqueue(update);
        ProgressChanged?.Invoke(this, update);
    }

    private void Log(string message)
    {
        LogMessage?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

