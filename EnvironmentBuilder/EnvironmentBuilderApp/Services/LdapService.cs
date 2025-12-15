// ============================================================================
// LdapService.cs - LDAP Directory Operations Service
// Environment Builder - Modern test environment creation tool
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

using System.DirectoryServices.Protocols;
using System.Net;
using EnvironmentBuilderApp.Models;
using Serilog;

namespace EnvironmentBuilderApp.Services;

/// <summary>
/// Service for performing LDAP directory operations.
/// Handles connections, authentication, and CRUD operations on directory objects.
/// </summary>
public class LdapService : IDisposable
{
    // ----------------------------------------------------------------------------
    // Private Fields
    // ----------------------------------------------------------------------------
    
    private LdapConnection? _connection;
    private readonly ConnectionSettings _settings;
    private readonly ILogger _logger;
    private bool _isConnected;
    
    // ----------------------------------------------------------------------------
    // Events
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Raised when an operation progresses
    /// </summary>
    public event EventHandler<ProgressEventArgs>? ProgressChanged;
    
    /// <summary>
    /// Raised when an operation encounters an error
    /// </summary>
    public event EventHandler<ErrorEventArgs>? ErrorOccurred;
    
    // ----------------------------------------------------------------------------
    // Constructor
    // ----------------------------------------------------------------------------
    
    public LdapService(ConnectionSettings settings, ILogger logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // ----------------------------------------------------------------------------
    // Connection Methods
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Establishes a connection to the LDAP server
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            _logger.Information("Connecting to LDAP server {Server}:{Port}", 
                _settings.ServerAddress, _settings.Port);
            
            // Create LDAP directory identifier
            var identifier = new LdapDirectoryIdentifier(
                _settings.ServerAddress, 
                _settings.Port);
            
            // Create connection with appropriate credentials
            if (_settings.UseAnonymousBind)
            {
                _connection = new LdapConnection(identifier);
            }
            else
            {
                var credentials = new NetworkCredential(
                    _settings.BindDN, 
                    _settings.Password);
                _connection = new LdapConnection(identifier, credentials);
            }
            
            // Configure connection options
            _connection.SessionOptions.ProtocolVersion = 3;
            _connection.SessionOptions.SecureSocketLayer = _settings.UseSSL;
            _connection.Timeout = TimeSpan.FromSeconds(_settings.ConnectionTimeout);
            
            // Handle certificate validation if needed
            if (_settings.SkipCertificateValidation)
            {
                _connection.SessionOptions.VerifyServerCertificate = 
                    (conn, cert) => true;
            }
            
            // Perform bind operation
            await Task.Run(() => _connection.Bind());
            
            _isConnected = true;
            _logger.Information("Successfully connected to LDAP server");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to connect to LDAP server");
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
            return false;
        }
    }
    
    /// <summary>
    /// Tests the connection without maintaining it
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            var result = await ConnectAsync();
            if (result)
            {
                Disconnect();
                return (true, "Connection successful!");
            }
            return (false, "Connection failed.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Disconnects from the LDAP server
    /// </summary>
    public void Disconnect()
    {
        if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
            _isConnected = false;
            _logger.Information("Disconnected from LDAP server");
        }
    }
    
    // ----------------------------------------------------------------------------
    // Object Creation Methods
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Creates a container object (Organization, OU, etc.)
    /// </summary>
    public async Task<bool> CreateContainerAsync(string dn, TreeNodeType nodeType, string description = "")
    {
        if (!_isConnected || _connection == null)
        {
            _logger.Warning("Cannot create container - not connected");
            return false;
        }
        
        try
        {
            var request = new AddRequest(dn);
            
            // Add object classes
            foreach (var objectClass in GetObjectClassesForNodeType(nodeType))
            {
                request.Attributes.Add(new DirectoryAttribute("objectClass", objectClass));
            }
            
            // Add description if provided
            if (!string.IsNullOrEmpty(description))
            {
                request.Attributes.Add(new DirectoryAttribute("description", description));
            }
            
            await Task.Run(() => _connection.SendRequest(request));
            _logger.Information("Created container: {DN}", dn);
            
            return true;
        }
        catch (DirectoryOperationException ex) when (ex.Response?.ResultCode == ResultCode.EntryAlreadyExists)
        {
            _logger.Warning("Container already exists: {DN}", dn);
            return true; // Not a failure - object exists
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create container: {DN}", dn);
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
            return false;
        }
    }
    
    /// <summary>
    /// Creates a user object
    /// </summary>
    public async Task<bool> CreateUserAsync(string dn, UserConfiguration config, int sequenceNumber)
    {
        if (!_isConnected || _connection == null)
        {
            _logger.Warning("Cannot create user - not connected");
            return false;
        }
        
        try
        {
            var username = config.GenerateUsername(sequenceNumber);
            var email = config.GenerateEmail(username);
            var password = config.GetPassword(username);
            
            var request = new AddRequest(dn);
            
            // Add object classes
            foreach (var objectClass in config.ObjectClasses)
            {
                request.Attributes.Add(new DirectoryAttribute("objectClass", objectClass));
            }
            
            // Add required attributes
            request.Attributes.Add(new DirectoryAttribute("cn", username));
            request.Attributes.Add(new DirectoryAttribute("uid", username));
            request.Attributes.Add(new DirectoryAttribute("sn", config.Surname));
            request.Attributes.Add(new DirectoryAttribute("givenName", config.GivenName));
            request.Attributes.Add(new DirectoryAttribute("mail", email));
            request.Attributes.Add(new DirectoryAttribute("userPassword", password));
            
            // Add optional attributes
            if (!string.IsNullOrEmpty(config.Title))
                request.Attributes.Add(new DirectoryAttribute("title", config.Title));
            if (!string.IsNullOrEmpty(config.TelephoneNumber))
                request.Attributes.Add(new DirectoryAttribute("telephoneNumber", config.TelephoneNumber));
            if (!string.IsNullOrEmpty(config.Location))
                request.Attributes.Add(new DirectoryAttribute("l", config.Location));
            if (!string.IsNullOrEmpty(config.Department))
                request.Attributes.Add(new DirectoryAttribute("departmentNumber", config.Department));
            
            await Task.Run(() => _connection.SendRequest(request));
            _logger.Information("Created user: {Username}", username);
            
            return true;
        }
        catch (DirectoryOperationException ex) when (ex.Response?.ResultCode == ResultCode.EntryAlreadyExists)
        {
            _logger.Warning("User already exists: {DN}", dn);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create user: {DN}", dn);
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
            return false;
        }
    }
    
    /// <summary>
    /// Deletes a directory object
    /// </summary>
    public async Task<bool> DeleteObjectAsync(string dn)
    {
        if (!_isConnected || _connection == null)
        {
            _logger.Warning("Cannot delete object - not connected");
            return false;
        }
        
        try
        {
            var request = new DeleteRequest(dn);
            await Task.Run(() => _connection.SendRequest(request));
            _logger.Information("Deleted object: {DN}", dn);
            return true;
        }
        catch (DirectoryOperationException ex) when (ex.Response?.ResultCode == ResultCode.NoSuchObject)
        {
            _logger.Warning("Object does not exist: {DN}", dn);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete object: {DN}", dn);
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
            return false;
        }
    }
    
    // ----------------------------------------------------------------------------
    // Helper Methods
    // ----------------------------------------------------------------------------
    
    private string[] GetObjectClassesForNodeType(TreeNodeType nodeType)
    {
        return nodeType switch
        {
            TreeNodeType.Organization => new[] { "organization", "top" },
            TreeNodeType.OrganizationalUnit => new[] { "organizationalUnit", "top" },
            TreeNodeType.Container => new[] { "container", "top" },
            TreeNodeType.Country => new[] { "country", "top" },
            TreeNodeType.Domain => new[] { "domain", "top" },
            _ => new[] { "organizationalUnit", "top" }
        };
    }
    
    // ----------------------------------------------------------------------------
    // IDisposable Implementation
    // ----------------------------------------------------------------------------
    
    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }
}

// ----------------------------------------------------------------------------
// Event Argument Classes
// ----------------------------------------------------------------------------

/// <summary>
/// Event arguments for progress updates
/// </summary>
public class ProgressEventArgs : EventArgs
{
    public int Current { get; }
    public int Total { get; }
    public string Message { get; }
    public int PercentComplete => Total > 0 ? (Current * 100) / Total : 0;
    
    public ProgressEventArgs(int current, int total, string message)
    {
        Current = current;
        Total = total;
        Message = message;
    }
}

/// <summary>
/// Event arguments for errors
/// </summary>
public class ErrorEventArgs : EventArgs
{
    public Exception Exception { get; }
    public string Message => Exception.Message;
    
    public ErrorEventArgs(Exception exception)
    {
        Exception = exception;
    }
}

