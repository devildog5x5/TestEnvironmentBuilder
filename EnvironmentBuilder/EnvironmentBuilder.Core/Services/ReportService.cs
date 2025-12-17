using System.Text;
using EnvironmentBuilder.Core.Models;

namespace EnvironmentBuilder.Core.Services;

/// <summary>
/// Generates reports in various formats
/// </summary>
public class ReportService
{
    /// <summary>
    /// Generate an HTML report for an operation
    /// </summary>
    public async Task<string> GenerateHtmlReportAsync(OperationResult result, List<TestUser> users, EnvironmentConfig config)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang='en'>");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset='UTF-8'>");
        html.AppendLine("  <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        html.AppendLine("  <title>Environment Builder Report</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: 'Segoe UI', sans-serif; background: #0d1117; color: #c9d1d9; margin: 0; padding: 20px; }");
        html.AppendLine("    .container { max-width: 1200px; margin: 0 auto; }");
        html.AppendLine("    .header { text-align: center; padding: 40px 0; border-bottom: 1px solid #30363d; }");
        html.AppendLine("    .header h1 { color: #FFD700; margin: 0; }");
        html.AppendLine("    .header .tagline { color: #8b949e; font-style: italic; }");
        html.AppendLine("    .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin: 30px 0; }");
        html.AppendLine("    .stat-card { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 20px; text-align: center; }");
        html.AppendLine("    .stat-value { font-size: 2rem; font-weight: bold; color: #FFD700; }");
        html.AppendLine("    .stat-label { color: #8b949e; }");
        html.AppendLine("    .success { color: #238636; }");
        html.AppendLine("    .error { color: #da3633; }");
        html.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        html.AppendLine("    th, td { padding: 12px; text-align: left; border-bottom: 1px solid #30363d; }");
        html.AppendLine("    th { background: #161b22; color: #FFD700; }");
        html.AppendLine("    tr:hover { background: #161b22; }");
        html.AppendLine("    .status-success { color: #238636; }");
        html.AppendLine("    .status-failed { color: #da3633; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <div class='container'>");
        
        // Header
        html.AppendLine("    <div class='header'>");
        html.AppendLine("      <h1>🛡️ Environment Builder Report</h1>");
        html.AppendLine("      <p class='tagline'>Test Brutally - Build Your Level of Complexity</p>");
        html.AppendLine($"     <p>Generated: {DateTime.Now:F}</p>");
        html.AppendLine("    </div>");

        // Summary stats
        html.AppendLine("    <div class='summary'>");
        html.AppendLine($"     <div class='stat-card'><div class='stat-value'>{result.Metrics.TotalItems}</div><div class='stat-label'>Total Users</div></div>");
        html.AppendLine($"     <div class='stat-card'><div class='stat-value success'>{result.Metrics.SuccessCount}</div><div class='stat-label'>Successful</div></div>");
        html.AppendLine($"     <div class='stat-card'><div class='stat-value error'>{result.Metrics.FailedCount}</div><div class='stat-label'>Failed</div></div>");
        html.AppendLine($"     <div class='stat-card'><div class='stat-value'>{result.Duration.TotalSeconds:F2}s</div><div class='stat-label'>Duration</div></div>");
        html.AppendLine($"     <div class='stat-card'><div class='stat-value'>{result.Metrics.ItemsPerSecond:F1}</div><div class='stat-label'>Users/sec</div></div>");
        html.AppendLine("    </div>");

        // Configuration
        html.AppendLine("    <h2>Configuration</h2>");
        html.AppendLine("    <table>");
        html.AppendLine($"     <tr><td>Server</td><td>{config.Connection.Server}:{config.Connection.Port}</td></tr>");
        html.AppendLine($"     <tr><td>Base DN</td><td>{config.Connection.BaseDn}</td></tr>");
        html.AppendLine($"     <tr><td>Complexity</td><td>{config.ComplexityLevel}</td></tr>");
        html.AppendLine($"     <tr><td>User Prefix</td><td>{config.Users.Prefix}</td></tr>");
        html.AppendLine($"     <tr><td>Batch Size</td><td>{config.Execution.BatchSize}</td></tr>");
        html.AppendLine($"     <tr><td>Parallel Ops</td><td>{config.Execution.ParallelOperations}</td></tr>");
        html.AppendLine("    </table>");

        // User list
        html.AppendLine("    <h2>Users Created</h2>");
        html.AppendLine("    <table>");
        html.AppendLine("      <tr><th>Username</th><th>Name</th><th>Email</th><th>Department</th><th>Status</th></tr>");
        
        foreach (var user in users.Take(100)) // Limit to first 100 for performance
        {
            var statusClass = user.Status == UserCreationStatus.Success ? "status-success" : "status-failed";
            html.AppendLine($"     <tr>");
            html.AppendLine($"       <td>{user.Username}</td>");
            html.AppendLine($"       <td>{user.FirstName} {user.LastName}</td>");
            html.AppendLine($"       <td>{user.Email}</td>");
            html.AppendLine($"       <td>{user.Department}</td>");
            html.AppendLine($"       <td class='{statusClass}'>{user.Status}</td>");
            html.AppendLine($"     </tr>");
        }

        if (users.Count > 100)
        {
            html.AppendLine($"     <tr><td colspan='5'>... and {users.Count - 100} more users</td></tr>");
        }

        html.AppendLine("    </table>");
        html.AppendLine("  </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    /// <summary>
    /// Generate a CSV export of users
    /// </summary>
    public async Task<string> GenerateCsvAsync(List<TestUser> users)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Username,FirstName,LastName,Email,Title,Department,Phone,Location,Status");
        
        foreach (var user in users)
        {
            csv.AppendLine($"\"{user.Username}\",\"{user.FirstName}\",\"{user.LastName}\",\"{user.Email}\",\"{user.Title}\",\"{user.Department}\",\"{user.PhoneNumber}\",\"{user.Location}\",\"{user.Status}\"");
        }

        return csv.ToString();
    }

    /// <summary>
    /// Generate a JSON export of the configuration and results
    /// </summary>
    public string GenerateJson(OperationResult result, List<TestUser> users, EnvironmentConfig config)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            Config = config,
            Result = result,
            Users = users
        };

        return Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
    }

    /// <summary>
    /// Save report to file
    /// </summary>
    public async Task SaveReportAsync(string content, string path)
    {
        await File.WriteAllTextAsync(path, content);
    }
}

