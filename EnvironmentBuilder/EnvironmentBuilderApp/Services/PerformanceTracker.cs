// ============================================================================
// PerformanceTracker.cs - Performance Metrics Tracking Service
// Tracks LDAP response times, throughput, and identifies bottlenecks
// Environment Builder - Test Brutally
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EnvironmentBuilderApp.Services
{
    /// <summary>
    /// Tracks and analyzes performance metrics during operations
    /// </summary>
    public class PerformanceTracker
    {
        private readonly ConcurrentQueue<OperationMetric> _metrics = new();
        private readonly Stopwatch _sessionTimer = new();
        private readonly object _lock = new();
        
        private int _totalOperations;
        private int _successfulOperations;
        private int _failedOperations;
        private DateTime _sessionStart;

        #region Properties

        public bool IsTracking { get; private set; }
        public TimeSpan SessionDuration => _sessionTimer.Elapsed;
        public int TotalOperations => _totalOperations;
        public int SuccessfulOperations => _successfulOperations;
        public int FailedOperations => _failedOperations;
        public double SuccessRate => _totalOperations > 0 ? (double)_successfulOperations / _totalOperations * 100 : 0;

        #endregion

        #region Session Control

        public void StartSession()
        {
            _metrics.Clear();
            _totalOperations = 0;
            _successfulOperations = 0;
            _failedOperations = 0;
            _sessionStart = DateTime.Now;
            _sessionTimer.Restart();
            IsTracking = true;
        }

        public void StopSession()
        {
            _sessionTimer.Stop();
            IsTracking = false;
        }

        public void ResetSession()
        {
            StopSession();
            _metrics.Clear();
            _totalOperations = 0;
            _successfulOperations = 0;
            _failedOperations = 0;
        }

        #endregion

        #region Recording Methods

        /// <summary>
        /// Records a single operation metric
        /// </summary>
        public void RecordOperation(string operationType, TimeSpan duration, bool success, string? details = null)
        {
            var metric = new OperationMetric
            {
                Timestamp = DateTime.Now,
                OperationType = operationType,
                Duration = duration,
                Success = success,
                Details = details ?? ""
            };

            _metrics.Enqueue(metric);
            
            lock (_lock)
            {
                _totalOperations++;
                if (success)
                    _successfulOperations++;
                else
                    _failedOperations++;
            }

            // Keep only last 10000 metrics to prevent memory issues
            while (_metrics.Count > 10000)
            {
                _metrics.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Creates a scoped timer for automatic recording
        /// </summary>
        public OperationTimer StartOperation(string operationType)
        {
            return new OperationTimer(this, operationType);
        }

        #endregion

        #region Analysis Methods

        /// <summary>
        /// Gets current throughput (operations per second)
        /// </summary>
        public double GetThroughput()
        {
            if (_sessionTimer.Elapsed.TotalSeconds < 1)
                return 0;
            return _totalOperations / _sessionTimer.Elapsed.TotalSeconds;
        }

        /// <summary>
        /// Gets throughput for a specific operation type
        /// </summary>
        public double GetThroughput(string operationType)
        {
            var ops = _metrics.Where(m => m.OperationType == operationType).ToList();
            if (!ops.Any() || _sessionTimer.Elapsed.TotalSeconds < 1)
                return 0;
            return ops.Count / _sessionTimer.Elapsed.TotalSeconds;
        }

        /// <summary>
        /// Gets average response time in milliseconds
        /// </summary>
        public double GetAverageResponseTime()
        {
            var metrics = _metrics.ToList();
            return metrics.Any() ? metrics.Average(m => m.Duration.TotalMilliseconds) : 0;
        }

        /// <summary>
        /// Gets average response time for a specific operation type
        /// </summary>
        public double GetAverageResponseTime(string operationType)
        {
            var ops = _metrics.Where(m => m.OperationType == operationType).ToList();
            return ops.Any() ? ops.Average(m => m.Duration.TotalMilliseconds) : 0;
        }

        /// <summary>
        /// Gets percentile response time (e.g., P95, P99)
        /// </summary>
        public double GetPercentileResponseTime(int percentile)
        {
            var sorted = _metrics.OrderBy(m => m.Duration).ToList();
            if (!sorted.Any()) return 0;

            int index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
            index = Math.Max(0, Math.Min(index, sorted.Count - 1));
            return sorted[index].Duration.TotalMilliseconds;
        }

        /// <summary>
        /// Gets minimum response time
        /// </summary>
        public double GetMinResponseTime()
        {
            var metrics = _metrics.ToList();
            return metrics.Any() ? metrics.Min(m => m.Duration.TotalMilliseconds) : 0;
        }

        /// <summary>
        /// Gets maximum response time
        /// </summary>
        public double GetMaxResponseTime()
        {
            var metrics = _metrics.ToList();
            return metrics.Any() ? metrics.Max(m => m.Duration.TotalMilliseconds) : 0;
        }

        /// <summary>
        /// Gets response time distribution by buckets
        /// </summary>
        public Dictionary<string, int> GetResponseTimeDistribution()
        {
            var buckets = new Dictionary<string, int>
            {
                { "< 10ms", 0 },
                { "10-50ms", 0 },
                { "50-100ms", 0 },
                { "100-500ms", 0 },
                { "500ms-1s", 0 },
                { "> 1s", 0 }
            };

            foreach (var metric in _metrics)
            {
                var ms = metric.Duration.TotalMilliseconds;
                if (ms < 10) buckets["< 10ms"]++;
                else if (ms < 50) buckets["10-50ms"]++;
                else if (ms < 100) buckets["50-100ms"]++;
                else if (ms < 500) buckets["100-500ms"]++;
                else if (ms < 1000) buckets["500ms-1s"]++;
                else buckets["> 1s"]++;
            }

            return buckets;
        }

        /// <summary>
        /// Gets operations per minute over time (for graphing)
        /// </summary>
        public List<(DateTime Time, double OpsPerMinute)> GetThroughputOverTime()
        {
            var result = new List<(DateTime, double)>();
            var metrics = _metrics.ToList();
            
            if (!metrics.Any()) return result;

            var startTime = metrics.Min(m => m.Timestamp);
            var endTime = metrics.Max(m => m.Timestamp);
            var currentMinute = new DateTime(startTime.Year, startTime.Month, startTime.Day, 
                                              startTime.Hour, startTime.Minute, 0);

            while (currentMinute <= endTime)
            {
                var nextMinute = currentMinute.AddMinutes(1);
                var count = metrics.Count(m => m.Timestamp >= currentMinute && m.Timestamp < nextMinute);
                result.Add((currentMinute, count));
                currentMinute = nextMinute;
            }

            return result;
        }

        /// <summary>
        /// Identifies potential bottlenecks
        /// </summary>
        public List<BottleneckInfo> IdentifyBottlenecks()
        {
            var bottlenecks = new List<BottleneckInfo>();
            var metrics = _metrics.ToList();

            if (!metrics.Any()) return bottlenecks;

            // Check for slow operations
            var avgTime = GetAverageResponseTime();
            var slowOps = metrics.Where(m => m.Duration.TotalMilliseconds > avgTime * 3).ToList();
            if (slowOps.Any())
            {
                bottlenecks.Add(new BottleneckInfo
                {
                    Type = BottleneckType.SlowOperations,
                    Severity = slowOps.Count > 10 ? Severity.High : Severity.Medium,
                    Description = $"{slowOps.Count} operations took 3x longer than average",
                    AffectedOperations = slowOps.Take(5).Select(o => o.OperationType).ToList()
                });
            }

            // Check for high failure rate
            if (SuccessRate < 95 && _totalOperations > 10)
            {
                bottlenecks.Add(new BottleneckInfo
                {
                    Type = BottleneckType.HighFailureRate,
                    Severity = SuccessRate < 80 ? Severity.Critical : Severity.High,
                    Description = $"Success rate is only {SuccessRate:F1}%",
                    AffectedOperations = metrics.Where(m => !m.Success)
                                                .Select(m => m.OperationType)
                                                .Distinct().ToList()
                });
            }

            // Check for degrading performance over time
            var firstHalf = metrics.Take(metrics.Count / 2).ToList();
            var secondHalf = metrics.Skip(metrics.Count / 2).ToList();
            if (firstHalf.Any() && secondHalf.Any())
            {
                var firstHalfAvg = firstHalf.Average(m => m.Duration.TotalMilliseconds);
                var secondHalfAvg = secondHalf.Average(m => m.Duration.TotalMilliseconds);
                if (secondHalfAvg > firstHalfAvg * 1.5)
                {
                    bottlenecks.Add(new BottleneckInfo
                    {
                        Type = BottleneckType.PerformanceDegradation,
                        Severity = Severity.Medium,
                        Description = $"Performance degraded by {((secondHalfAvg / firstHalfAvg) - 1) * 100:F0}% over time"
                    });
                }
            }

            return bottlenecks;
        }

        /// <summary>
        /// Gets a summary of all metrics
        /// </summary>
        public PerformanceSummary GetSummary()
        {
            return new PerformanceSummary
            {
                SessionStart = _sessionStart,
                SessionDuration = SessionDuration,
                TotalOperations = TotalOperations,
                SuccessfulOperations = SuccessfulOperations,
                FailedOperations = FailedOperations,
                SuccessRate = SuccessRate,
                Throughput = GetThroughput(),
                AverageResponseTime = GetAverageResponseTime(),
                MinResponseTime = GetMinResponseTime(),
                MaxResponseTime = GetMaxResponseTime(),
                P50ResponseTime = GetPercentileResponseTime(50),
                P95ResponseTime = GetPercentileResponseTime(95),
                P99ResponseTime = GetPercentileResponseTime(99),
                ResponseTimeDistribution = GetResponseTimeDistribution(),
                Bottlenecks = IdentifyBottlenecks()
            };
        }

        #endregion
    }

    #region Supporting Classes

    public class OperationMetric
    {
        public DateTime Timestamp { get; set; }
        public string OperationType { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string Details { get; set; } = "";
    }

    public class OperationTimer : IDisposable
    {
        private readonly PerformanceTracker _tracker;
        private readonly string _operationType;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;
        private bool _success = true;
        private string? _details;

        public OperationTimer(PerformanceTracker tracker, string operationType)
        {
            _tracker = tracker;
            _operationType = operationType;
            _stopwatch = Stopwatch.StartNew();
        }

        public void MarkFailed(string? details = null)
        {
            _success = false;
            _details = details;
        }

        public void SetDetails(string details)
        {
            _details = details;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stopwatch.Stop();
            _tracker.RecordOperation(_operationType, _stopwatch.Elapsed, _success, _details);
        }
    }

    public class PerformanceSummary
    {
        public DateTime SessionStart { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double SuccessRate { get; set; }
        public double Throughput { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double P50ResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public Dictionary<string, int> ResponseTimeDistribution { get; set; } = new();
        public List<BottleneckInfo> Bottlenecks { get; set; } = new();
    }

    public class BottleneckInfo
    {
        public BottleneckType Type { get; set; }
        public Severity Severity { get; set; }
        public string Description { get; set; } = "";
        public List<string> AffectedOperations { get; set; } = new();
    }

    public enum BottleneckType
    {
        SlowOperations,
        HighFailureRate,
        PerformanceDegradation,
        ConnectionPoolExhaustion,
        MemoryPressure
    }

    public enum Severity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}

