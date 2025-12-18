// ============================================================================
// AuditLogService.cs - Audit Trail and Operation History
// Complete history of all operations for compliance and debugging
// Environment Builder - Test Brutally
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace EnvironmentBuilderApp.Services
{
    /// <summary>
    /// Maintains complete audit trail of all operations
    /// </summary>
    public class AuditLogService
    {
        private readonly string _logPath;
        private readonly List<AuditEntry> _currentSessionLog = new();
        private readonly object _lock = new();
        private string _currentLogFile;

        public AuditLogService()
        {
            _logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EnvironmentBuilder", "AuditLogs");
            Directory.CreateDirectory(_logPath);
            _currentLogFile = Path.Combine(_logPath, $"audit_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        }

        #region Logging Methods

        /// <summary>
        /// Logs an operation
        /// </summary>
        public void Log(AuditAction action, string description, 
                       string? targetObject = null, 
                       Dictionary<string, string>? details = null,
                       bool success = true,
                       string? errorMessage = null)
        {
            var entry = new AuditEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Action = action,
                Description = description,
                TargetObject = targetObject ?? "",
                Details = details ?? new Dictionary<string, string>(),
                Success = success,
                ErrorMessage = errorMessage ?? "",
                MachineName = Environment.MachineName,
                UserName = Environment.UserName
            };

            lock (_lock)
            {
                _currentSessionLog.Add(entry);
                AppendToFile(entry);
            }
        }

        /// <summary>
        /// Logs start of an operation
        /// </summary>
        public AuditEntry LogOperationStart(AuditAction action, string description)
        {
            var entry = new AuditEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Action = action,
                Description = description,
                MachineName = Environment.MachineName,
                UserName = Environment.UserName
            };

            lock (_lock)
            {
                _currentSessionLog.Add(entry);
            }

            return entry;
        }

        /// <summary>
        /// Logs completion of an operation
        /// </summary>
        public void LogOperationComplete(AuditEntry entry, bool success, string? message = null)
        {
            entry.EndTime = DateTime.Now;
            entry.Duration = entry.EndTime - entry.Timestamp;
            entry.Success = success;
            if (!string.IsNullOrEmpty(message))
            {
                if (success)
                    entry.Description += $" - {message}";
                else
                    entry.ErrorMessage = message;
            }

            lock (_lock)
            {
                AppendToFile(entry);
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets current session log
        /// </summary>
        public List<AuditEntry> GetCurrentSessionLog() => _currentSessionLog.ToList();

        /// <summary>
        /// Gets log entries by date range
        /// </summary>
        public List<AuditEntry> GetLogsByDateRange(DateTime startDate, DateTime endDate)
        {
            var entries = new List<AuditEntry>();

            foreach (var file in Directory.GetFiles(_logPath, "audit_*.json"))
            {
                try
                {
                    var lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var entry = JsonSerializer.Deserialize<AuditEntry>(line);
                        if (entry != null && entry.Timestamp >= startDate && entry.Timestamp <= endDate)
                        {
                            entries.Add(entry);
                        }
                    }
                }
                catch { /* Skip corrupt files */ }
            }

            return entries.OrderByDescending(e => e.Timestamp).ToList();
        }

        /// <summary>
        /// Gets log entries by action type
        /// </summary>
        public List<AuditEntry> GetLogsByAction(AuditAction action)
        {
            return GetLogsByDateRange(DateTime.MinValue, DateTime.MaxValue)
                   .Where(e => e.Action == action).ToList();
        }

        /// <summary>
        /// Gets failed operations
        /// </summary>
        public List<AuditEntry> GetFailedOperations()
        {
            return GetLogsByDateRange(DateTime.MinValue, DateTime.MaxValue)
                   .Where(e => !e.Success).ToList();
        }

        /// <summary>
        /// Gets log summary
        /// </summary>
        public AuditSummary GetSummary(DateTime? since = null)
        {
            var startDate = since ?? DateTime.Today.AddDays(-30);
            var entries = GetLogsByDateRange(startDate, DateTime.Now);

            return new AuditSummary
            {
                TotalOperations = entries.Count,
                SuccessfulOperations = entries.Count(e => e.Success),
                FailedOperations = entries.Count(e => !e.Success),
                OperationsByType = entries.GroupBy(e => e.Action)
                                          .ToDictionary(g => g.Key, g => g.Count()),
                FirstOperation = entries.MinBy(e => e.Timestamp)?.Timestamp,
                LastOperation = entries.MaxBy(e => e.Timestamp)?.Timestamp,
                TotalUsersCreated = entries.Where(e => e.Action == AuditAction.CreateUser && e.Success).Count(),
                TotalUsersDeleted = entries.Where(e => e.Action == AuditAction.DeleteUser && e.Success).Count()
            };
        }

        #endregion

        #region Export Methods

        /// <summary>
        /// Exports audit log to CSV
        /// </summary>
        public void ExportToCsv(string filePath, DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = GetLogsByDateRange(
                startDate ?? DateTime.MinValue, 
                endDate ?? DateTime.MaxValue);

            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Action,Description,Target,Success,Duration,User,Machine,Error");

            foreach (var entry in entries)
            {
                sb.AppendLine($"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                             $"\"{entry.Action}\"," +
                             $"\"{entry.Description.Replace("\"", "\"\"")}\"," +
                             $"\"{entry.TargetObject}\"," +
                             $"{entry.Success}," +
                             $"\"{entry.Duration}\"," +
                             $"\"{entry.UserName}\"," +
                             $"\"{entry.MachineName}\"," +
                             $"\"{entry.ErrorMessage.Replace("\"", "\"\"")}\"");
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        /// <summary>
        /// Exports audit log to HTML report
        /// </summary>
        public void ExportToHtml(string filePath, DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = GetLogsByDateRange(
                startDate ?? DateTime.MinValue, 
                endDate ?? DateTime.MaxValue);

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><title>Audit Log Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', sans-serif; background: #0d1117; color: #c9d1d9; padding: 20px; }");
            sb.AppendLine("h1 { color: #f0a500; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            sb.AppendLine("th, td { padding: 10px; text-align: left; border-bottom: 1px solid #30363d; }");
            sb.AppendLine("th { background: #161b22; color: #f0a500; }");
            sb.AppendLine(".success { color: #3fb950; }");
            sb.AppendLine(".failed { color: #f85149; }");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<h1>🛡️ Environment Builder - Audit Log Report</h1>");
            sb.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine($"<p>Total Entries: {entries.Count}</p>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Time</th><th>Action</th><th>Description</th><th>Status</th><th>Duration</th></tr>");

            foreach (var entry in entries)
            {
                var statusClass = entry.Success ? "success" : "failed";
                var statusText = entry.Success ? "✅ Success" : "❌ Failed";
                sb.AppendLine($"<tr>" +
                             $"<td>{entry.Timestamp:HH:mm:ss}</td>" +
                             $"<td>{entry.Action}</td>" +
                             $"<td>{System.Web.HttpUtility.HtmlEncode(entry.Description)}</td>" +
                             $"<td class='{statusClass}'>{statusText}</td>" +
                             $"<td>{entry.Duration}</td></tr>");
            }

            sb.AppendLine("</table></body></html>");
            File.WriteAllText(filePath, sb.ToString());
        }

        #endregion

        #region Cleanup Methods

        /// <summary>
        /// Clears current session log
        /// </summary>
        public void ClearCurrentSession()
        {
            lock (_lock)
            {
                _currentSessionLog.Clear();
            }
        }

        /// <summary>
        /// Archives old log files
        /// </summary>
        public void ArchiveOldLogs(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var archivePath = Path.Combine(_logPath, "Archive");
            Directory.CreateDirectory(archivePath);

            foreach (var file in Directory.GetFiles(_logPath, "audit_*.json"))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Move(file, Path.Combine(archivePath, fileInfo.Name), true);
                }
            }
        }

        #endregion

        #region Private Methods

        private void AppendToFile(AuditEntry entry)
        {
            try
            {
                var json = JsonSerializer.Serialize(entry);
                File.AppendAllText(_currentLogFile, json + Environment.NewLine);
            }
            catch { /* Logging failures shouldn't break the app */ }
        }

        #endregion
    }

    #region Models

    public class AuditEntry
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public AuditAction Action { get; set; }
        public string Description { get; set; } = "";
        public string TargetObject { get; set; } = "";
        public Dictionary<string, string> Details { get; set; } = new();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string MachineName { get; set; } = "";
        public string UserName { get; set; } = "";
    }

    public class AuditSummary
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public Dictionary<AuditAction, int> OperationsByType { get; set; } = new();
        public DateTime? FirstOperation { get; set; }
        public DateTime? LastOperation { get; set; }
        public int TotalUsersCreated { get; set; }
        public int TotalUsersDeleted { get; set; }
    }

    public enum AuditAction
    {
        // Connection
        Connect,
        Disconnect,
        TestConnection,

        // Build operations
        BuildStart,
        BuildComplete,
        CreateContainer,
        CreateUser,
        CreateHomeDirectory,

        // Cleanup operations
        CleanupStart,
        CleanupComplete,
        DeleteContainer,
        DeleteUser,
        FullReset,

        // Validation
        ValidateStart,
        ValidateComplete,
        HealthCheck,
        AuthenticationTest,

        // Configuration
        LoadConfig,
        SaveConfig,
        ImportCsv,
        ExportCsv,

        // Snapshots
        CreateSnapshot,
        CompareSnapshots,

        // Scheduled tasks
        ScheduledTaskRun,
        ScheduledTaskComplete,

        // Reports
        GenerateReport,
        ExportReport,

        // Errors
        Error,
        Warning
    }

    #endregion
}

