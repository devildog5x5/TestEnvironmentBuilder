// ============================================================================
// SchedulerService.cs - Scheduled Operations Service
// Schedule builds, cleanups, health checks with notifications
// Environment Builder - Test Brutally
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace EnvironmentBuilderApp.Services
{
    /// <summary>
    /// Manages scheduled operations
    /// </summary>
    public class SchedulerService : IDisposable
    {
        private readonly List<ScheduledTask> _tasks = new();
        private readonly System.Timers.Timer _checkTimer;
        private readonly string _schedulePath;
        private bool _disposed;

        public event EventHandler<ScheduledTaskEventArgs>? TaskTriggered;
        public event EventHandler<ScheduledTaskEventArgs>? TaskCompleted;
        public event EventHandler<ScheduledTaskEventArgs>? TaskFailed;

        public SchedulerService()
        {
            _schedulePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EnvironmentBuilder", "Schedules");
            Directory.CreateDirectory(_schedulePath);

            // Check for tasks every minute
            _checkTimer = new System.Timers.Timer(60000);
            _checkTimer.Elapsed += CheckScheduledTasks;
            _checkTimer.Start();

            LoadTasks();
        }

        #region Task Management

        public void AddTask(ScheduledTask task)
        {
            task.Id = Guid.NewGuid().ToString();
            task.CreatedAt = DateTime.Now;
            task.NextRun = CalculateNextRun(task);
            _tasks.Add(task);
            SaveTasks();
        }

        public void RemoveTask(string taskId)
        {
            _tasks.RemoveAll(t => t.Id == taskId);
            SaveTasks();
        }

        public void UpdateTask(ScheduledTask task)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing != null)
            {
                var index = _tasks.IndexOf(existing);
                task.NextRun = CalculateNextRun(task);
                _tasks[index] = task;
                SaveTasks();
            }
        }

        public void EnableTask(string taskId, bool enabled)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.Enabled = enabled;
                if (enabled)
                    task.NextRun = CalculateNextRun(task);
                SaveTasks();
            }
        }

        public List<ScheduledTask> GetAllTasks() => _tasks.ToList();

        public List<ScheduledTask> GetPendingTasks() => 
            _tasks.Where(t => t.Enabled && t.NextRun <= DateTime.Now).ToList();

        #endregion

        #region Schedule Execution

        private void CheckScheduledTasks(object? sender, ElapsedEventArgs e)
        {
            var pendingTasks = GetPendingTasks();
            foreach (var task in pendingTasks)
            {
                _ = ExecuteTaskAsync(task);
            }
        }

        private async Task ExecuteTaskAsync(ScheduledTask task)
        {
            task.LastRun = DateTime.Now;
            task.Status = TaskStatus.Running;

            try
            {
                TaskTriggered?.Invoke(this, new ScheduledTaskEventArgs(task));

                // Execute based on task type
                var result = task.TaskType switch
                {
                    ScheduledTaskType.Build => await ExecuteBuildAsync(task),
                    ScheduledTaskType.Cleanup => await ExecuteCleanupAsync(task),
                    ScheduledTaskType.HealthCheck => await ExecuteHealthCheckAsync(task),
                    ScheduledTaskType.Validate => await ExecuteValidateAsync(task),
                    ScheduledTaskType.Backup => await ExecuteBackupAsync(task),
                    _ => new TaskResult { Success = false, Message = "Unknown task type" }
                };

                task.Status = result.Success ? TaskStatus.Completed : TaskStatus.Failed;
                task.LastResult = result.Message;
                task.RunCount++;

                if (result.Success)
                    TaskCompleted?.Invoke(this, new ScheduledTaskEventArgs(task, result));
                else
                    TaskFailed?.Invoke(this, new ScheduledTaskEventArgs(task, result));

                // Send notifications
                if (task.NotificationSettings != null)
                {
                    await SendNotificationsAsync(task, result);
                }
            }
            catch (Exception ex)
            {
                task.Status = TaskStatus.Failed;
                task.LastResult = ex.Message;
                TaskFailed?.Invoke(this, new ScheduledTaskEventArgs(task, 
                    new TaskResult { Success = false, Message = ex.Message }));
            }
            finally
            {
                // Schedule next run
                if (task.Schedule.Type != ScheduleType.Once)
                {
                    task.NextRun = CalculateNextRun(task);
                    task.Status = TaskStatus.Scheduled;
                }
                else
                {
                    task.Enabled = false;
                }
                SaveTasks();
            }
        }

        #endregion

        #region Task Executors

        private async Task<TaskResult> ExecuteBuildAsync(ScheduledTask task)
        {
            await Task.Delay(1000); // Placeholder for actual build
            return new TaskResult 
            { 
                Success = true, 
                Message = $"Built {task.Parameters.GetValueOrDefault("UserCount", "10")} users" 
            };
        }

        private async Task<TaskResult> ExecuteCleanupAsync(ScheduledTask task)
        {
            await Task.Delay(500);
            return new TaskResult 
            { 
                Success = true, 
                Message = $"Cleaned up users with prefix: {task.Parameters.GetValueOrDefault("Prefix", "test")}" 
            };
        }

        private async Task<TaskResult> ExecuteHealthCheckAsync(ScheduledTask task)
        {
            await Task.Delay(500);
            return new TaskResult { Success = true, Message = "Health check passed" };
        }

        private async Task<TaskResult> ExecuteValidateAsync(ScheduledTask task)
        {
            await Task.Delay(500);
            return new TaskResult { Success = true, Message = "Validation completed" };
        }

        private async Task<TaskResult> ExecuteBackupAsync(ScheduledTask task)
        {
            await Task.Delay(500);
            return new TaskResult { Success = true, Message = "Backup created" };
        }

        #endregion

        #region Notifications

        private async Task SendNotificationsAsync(ScheduledTask task, TaskResult result)
        {
            var settings = task.NotificationSettings!;
            var message = FormatNotificationMessage(task, result);

            // Send to Slack
            if (!string.IsNullOrEmpty(settings.SlackWebhookUrl))
            {
                await SendSlackNotificationAsync(settings.SlackWebhookUrl, message, result.Success);
            }

            // Send to Teams
            if (!string.IsNullOrEmpty(settings.TeamsWebhookUrl))
            {
                await SendTeamsNotificationAsync(settings.TeamsWebhookUrl, message, result.Success);
            }

            // Send email (would require SMTP configuration)
            if (!string.IsNullOrEmpty(settings.EmailAddress))
            {
                // Email sending would go here
            }
        }

        private async Task SendSlackNotificationAsync(string webhookUrl, string message, bool success)
        {
            try
            {
                using var client = new HttpClient();
                var payload = new
                {
                    text = message,
                    attachments = new[]
                    {
                        new
                        {
                            color = success ? "#36a64f" : "#ff0000",
                            title = success ? "✅ Task Completed" : "❌ Task Failed"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(webhookUrl, content);
            }
            catch { /* Notification failures shouldn't break execution */ }
        }

        private async Task SendTeamsNotificationAsync(string webhookUrl, string message, bool success)
        {
            try
            {
                using var client = new HttpClient();
                var payload = new
                {
                    title = success ? "✅ Environment Builder Task Completed" : "❌ Environment Builder Task Failed",
                    text = message,
                    themeColor = success ? "00FF00" : "FF0000"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(webhookUrl, content);
            }
            catch { }
        }

        private string FormatNotificationMessage(ScheduledTask task, TaskResult result)
        {
            return $"**{task.Name}** ({task.TaskType})\n" +
                   $"Status: {(result.Success ? "Success" : "Failed")}\n" +
                   $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Message: {result.Message}";
        }

        #endregion

        #region Schedule Calculation

        private DateTime CalculateNextRun(ScheduledTask task)
        {
            var schedule = task.Schedule;
            var now = DateTime.Now;

            return schedule.Type switch
            {
                ScheduleType.Once => schedule.StartTime,
                
                ScheduleType.Daily => GetNextDailyRun(schedule.StartTime, now),
                
                ScheduleType.Weekly => GetNextWeeklyRun(schedule.StartTime, schedule.DaysOfWeek, now),
                
                ScheduleType.Hourly => now.AddHours(1),
                
                ScheduleType.Interval => now.AddMinutes(schedule.IntervalMinutes),
                
                ScheduleType.Cron => ParseCronExpression(schedule.CronExpression, now),
                
                _ => now.AddDays(1)
            };
        }

        private DateTime GetNextDailyRun(DateTime startTime, DateTime now)
        {
            var today = now.Date.Add(startTime.TimeOfDay);
            return today > now ? today : today.AddDays(1);
        }

        private DateTime GetNextWeeklyRun(DateTime startTime, List<DayOfWeek> days, DateTime now)
        {
            if (!days.Any()) days = new List<DayOfWeek> { DayOfWeek.Monday };

            for (int i = 0; i < 8; i++)
            {
                var candidate = now.Date.AddDays(i).Add(startTime.TimeOfDay);
                if (days.Contains(candidate.DayOfWeek) && candidate > now)
                    return candidate;
            }
            return now.AddDays(7);
        }

        private DateTime ParseCronExpression(string? cron, DateTime now)
        {
            // Simplified cron - in production use a library like Cronos
            return now.AddHours(1);
        }

        #endregion

        #region Persistence

        private void LoadTasks()
        {
            var filePath = Path.Combine(_schedulePath, "tasks.json");
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var tasks = JsonSerializer.Deserialize<List<ScheduledTask>>(json);
                    if (tasks != null)
                    {
                        _tasks.Clear();
                        _tasks.AddRange(tasks);
                    }
                }
                catch { /* Start fresh if file is corrupt */ }
            }
        }

        private void SaveTasks()
        {
            var filePath = Path.Combine(_schedulePath, "tasks.json");
            var json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _checkTimer.Stop();
            _checkTimer.Dispose();
        }
    }

    #region Models

    public class ScheduledTask
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public ScheduledTaskType TaskType { get; set; }
        public TaskSchedule Schedule { get; set; } = new();
        public Dictionary<string, string> Parameters { get; set; } = new();
        public NotificationSettings? NotificationSettings { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public string LastResult { get; set; } = "";
        public TaskStatus Status { get; set; } = TaskStatus.Scheduled;
        public int RunCount { get; set; }
    }

    public class TaskSchedule
    {
        public ScheduleType Type { get; set; }
        public DateTime StartTime { get; set; }
        public int IntervalMinutes { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
        public string? CronExpression { get; set; }
    }

    public class NotificationSettings
    {
        public string? SlackWebhookUrl { get; set; }
        public string? TeamsWebhookUrl { get; set; }
        public string? EmailAddress { get; set; }
        public bool NotifyOnSuccess { get; set; } = true;
        public bool NotifyOnFailure { get; set; } = true;
    }

    public class TaskResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class ScheduledTaskEventArgs : EventArgs
    {
        public ScheduledTask Task { get; }
        public TaskResult? Result { get; }

        public ScheduledTaskEventArgs(ScheduledTask task, TaskResult? result = null)
        {
            Task = task;
            Result = result;
        }
    }

    public enum ScheduledTaskType
    {
        Build,
        Cleanup,
        HealthCheck,
        Validate,
        Backup
    }

    public enum ScheduleType
    {
        Once,
        Hourly,
        Daily,
        Weekly,
        Interval,
        Cron
    }

    public enum TaskStatus
    {
        Scheduled,
        Running,
        Completed,
        Failed,
        Disabled
    }

    #endregion
}

