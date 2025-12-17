using System.Net.Http.Json;
using EnvironmentBuilder.Core.Models;

namespace EnvironmentBuilder.Web.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<List<PresetInfo>> GetPresetsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<PresetInfo>>("api/environment/presets");
            return response ?? new List<PresetInfo>();
        }
        catch
        {
            return new List<PresetInfo>();
        }
    }

    public async Task<BuildResult?> StartBuildAsync(BuildRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/environment/build", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BuildResult>();
        }
        return null;
    }

    public async Task<BuildResult?> StartCleanupAsync(CleanupRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/environment/cleanup", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BuildResult>();
        }
        return null;
    }

    public async Task<OperationStatus?> GetOperationStatusAsync(string operationId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<OperationStatus>($"api/environment/operations/{operationId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<OperationStatus>> GetOperationsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<OperationStatus>>("api/environment/operations");
            return response ?? new List<OperationStatus>();
        }
        catch
        {
            return new List<OperationStatus>();
        }
    }

    public async Task<HealthCheckResult?> HealthCheckAsync(ConnectionInfo connection)
    {
        var response = await _httpClient.PostAsJsonAsync("api/environment/health", connection);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<HealthCheckResult>();
        }
        return null;
    }

    public async Task<bool> CancelOperationAsync(string operationId)
    {
        var response = await _httpClient.PostAsync($"api/environment/operations/{operationId}/cancel", null);
        return response.IsSuccessStatusCode;
    }
}

public class PresetInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Users { get; set; }
}

public class BuildRequest
{
    public string? Preset { get; set; }
    public string? Server { get; set; }
    public int? Port { get; set; }
    public string? BindDn { get; set; }
    public string? Password { get; set; }
    public string? BaseDn { get; set; }
    public bool? UseSsl { get; set; }
    public int UserCount { get; set; }
    public string? UserPrefix { get; set; }
    public bool? DryRun { get; set; }
}

public class CleanupRequest
{
    public string? Server { get; set; }
    public int? Port { get; set; }
    public string? BindDn { get; set; }
    public string? Password { get; set; }
    public string? BaseDn { get; set; }
    public string? Prefix { get; set; }
    public bool? DryRun { get; set; }
}

public class BuildResult
{
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class OperationStatus
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ProgressInfo? CurrentProgress { get; set; }
    public List<string> Logs { get; set; } = new();
}

public class ProgressInfo
{
    public int CurrentItem { get; set; }
    public int TotalItems { get; set; }
    public double PercentComplete { get; set; }
    public string CurrentItemName { get; set; } = string.Empty;
    public double ItemsPerSecond { get; set; }
}

public class ConnectionInfo
{
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 389;
    public string BindDn { get; set; } = "cn=admin,o=org";
    public string Password { get; set; } = string.Empty;
    public string BaseDn { get; set; } = "o=org";
}

