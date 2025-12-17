using System.Collections.Concurrent;
using EnvironmentBuilder.Core.Models;
using EnvironmentBuilder.Core.Services;

namespace EnvironmentBuilder.API.Services;

/// <summary>
/// Manages multiple environment operations and their state
/// </summary>
public class EnvironmentManager
{
    private readonly ConcurrentDictionary<string, OperationState> _operations = new();
    private readonly ConcurrentDictionary<string, EnvironmentService> _services = new();

    public event EventHandler<ProgressUpdate>? ProgressChanged;
    public event EventHandler<(string OperationId, string Message)>? LogMessage;

    /// <summary>
    /// Start a new build operation
    /// </summary>
    public async Task<string> StartBuildAsync(EnvironmentConfig config, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString();
        var state = new OperationState
        {
            Id = operationId,
            Type = OperationType.Build,
            Status = "Starting",
            StartTime = DateTime.UtcNow,
            Config = config
        };

        _operations[operationId] = state;

        var service = new EnvironmentService(config);
        _services[operationId] = service;

        service.ProgressChanged += (s, update) =>
        {
            update.OperationId = operationId;
            state.CurrentProgress = update;
            ProgressChanged?.Invoke(this, update);
        };

        service.LogMessage += (s, msg) =>
        {
            state.Logs.Add(msg);
            LogMessage?.Invoke(this, (operationId, msg));
        };

        // Run in background
        _ = Task.Run(async () =>
        {
            try
            {
                state.Status = "Running";
                var result = await service.BuildEnvironmentAsync(cancellationToken);
                state.Result = result;
                state.Status = result.Success ? "Completed" : "Failed";
                state.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                state.Status = "Error";
                state.Result = OperationResult.Failed("Operation failed", ex.Message);
                state.EndTime = DateTime.UtcNow;
            }
        }, cancellationToken);

        return operationId;
    }

    /// <summary>
    /// Start a cleanup operation
    /// </summary>
    public async Task<string> StartCleanupAsync(EnvironmentConfig config, string prefix, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString();
        var state = new OperationState
        {
            Id = operationId,
            Type = OperationType.Cleanup,
            Status = "Starting",
            StartTime = DateTime.UtcNow,
            Config = config
        };

        _operations[operationId] = state;

        var service = new EnvironmentService(config);
        _services[operationId] = service;

        service.ProgressChanged += (s, update) =>
        {
            update.OperationId = operationId;
            state.CurrentProgress = update;
            ProgressChanged?.Invoke(this, update);
        };

        service.LogMessage += (s, msg) =>
        {
            state.Logs.Add(msg);
            LogMessage?.Invoke(this, (operationId, msg));
        };

        _ = Task.Run(async () =>
        {
            try
            {
                state.Status = "Running";
                var result = await service.CleanupAsync(prefix, cancellationToken);
                state.Result = result;
                state.Status = result.Success ? "Completed" : "Failed";
                state.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                state.Status = "Error";
                state.Result = OperationResult.Failed("Cleanup failed", ex.Message);
                state.EndTime = DateTime.UtcNow;
            }
        }, cancellationToken);

        return operationId;
    }

    /// <summary>
    /// Get operation status
    /// </summary>
    public OperationState? GetOperation(string operationId)
    {
        return _operations.TryGetValue(operationId, out var state) ? state : null;
    }

    /// <summary>
    /// Get all operations
    /// </summary>
    public IEnumerable<OperationState> GetAllOperations()
    {
        return _operations.Values.OrderByDescending(o => o.StartTime);
    }

    /// <summary>
    /// Cancel an operation
    /// </summary>
    public bool CancelOperation(string operationId)
    {
        if (_operations.TryGetValue(operationId, out var state))
        {
            state.CancellationSource?.Cancel();
            state.Status = "Cancelling";
            return true;
        }
        return false;
    }

    /// <summary>
    /// Perform health check
    /// </summary>
    public async Task<HealthCheckResult> HealthCheckAsync(EnvironmentConfig config)
    {
        using var service = new EnvironmentService(config);
        return await service.HealthCheckAsync();
    }
}

public class OperationState
{
    public string Id { get; set; } = string.Empty;
    public OperationType Type { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public EnvironmentConfig? Config { get; set; }
    public ProgressUpdate? CurrentProgress { get; set; }
    public OperationResult? Result { get; set; }
    public List<string> Logs { get; set; } = new();
    public CancellationTokenSource? CancellationSource { get; set; }
}

public enum OperationType
{
    Build,
    Cleanup,
    Validate,
    HealthCheck
}

