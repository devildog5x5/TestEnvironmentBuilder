namespace EnvironmentBuilder.Core.Models;

/// <summary>
/// Result of an environment operation
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public OperationMetrics Metrics { get; set; } = new();

    public static OperationResult Succeeded(string message = "Operation completed successfully") => new()
    {
        Success = true,
        Message = message,
        EndTime = DateTime.UtcNow
    };

    public static OperationResult Failed(string message, string? details = null) => new()
    {
        Success = false,
        Message = message,
        ErrorDetails = details,
        EndTime = DateTime.UtcNow
    };
}

/// <summary>
/// Performance metrics for an operation
/// </summary>
public class OperationMetrics
{
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public double ItemsPerSecond { get; set; }
    public double AverageItemTimeMs { get; set; }
    public long PeakMemoryBytes { get; set; }
    public Dictionary<string, int> ErrorBreakdown { get; set; } = new();
}

/// <summary>
/// Progress update during long-running operations
/// </summary>
public class ProgressUpdate
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString();
    public string Operation { get; set; } = string.Empty;
    public int CurrentItem { get; set; }
    public int TotalItems { get; set; }
    public double PercentComplete => TotalItems > 0 ? (CurrentItem * 100.0 / TotalItems) : 0;
    public string CurrentItemName { get; set; } = string.Empty;
    public string Status { get; set; } = "Running";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double ItemsPerSecond { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public List<string> RecentErrors { get; set; } = new();
}

/// <summary>
/// Environment health check result
/// </summary>
public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string ServerStatus { get; set; } = "Unknown";
    public bool CanConnect { get; set; }
    public bool CanAuthenticate { get; set; }
    public bool CanWrite { get; set; }
    public bool CanRead { get; set; }
    public int ResponseTimeMs { get; set; }
    public string ServerVersion { get; set; } = string.Empty;
    public Dictionary<string, bool> Checks { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

