using Microsoft.AspNetCore.Mvc;
using EnvironmentBuilder.API.Services;
using EnvironmentBuilder.Core.Models;

namespace EnvironmentBuilder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnvironmentController : ControllerBase
{
    private readonly EnvironmentManager _manager;

    public EnvironmentController(EnvironmentManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Start a new environment build operation
    /// </summary>
    [HttpPost("build")]
    public async Task<ActionResult<BuildResponse>> StartBuild([FromBody] BuildRequest request)
    {
        var config = new EnvironmentConfig();
        
        // Apply preset if specified
        if (!string.IsNullOrEmpty(request.Preset))
        {
            var level = request.Preset.ToLower() switch
            {
                "simple" => ComplexityLevel.Simple,
                "medium" => ComplexityLevel.Medium,
                "complex" => ComplexityLevel.Complex,
                "brutal" => ComplexityLevel.Brutal,
                _ => ComplexityLevel.Simple
            };
            config.ApplyPreset(ComplexityPreset.FromLevel(level));
        }

        // Apply connection settings
        config.Connection.Server = request.Server ?? "localhost";
        config.Connection.Port = request.Port ?? 389;
        config.Connection.BindDn = request.BindDn ?? "cn=admin,o=org";
        config.Connection.Password = request.Password ?? "";
        config.Connection.BaseDn = request.BaseDn ?? "o=org";
        config.Connection.UseSsl = request.UseSsl ?? false;

        // Apply user settings
        if (request.UserCount > 0) config.Users.Count = request.UserCount;
        if (!string.IsNullOrEmpty(request.UserPrefix)) config.Users.Prefix = request.UserPrefix;
        config.Execution.DryRun = request.DryRun ?? false;

        var operationId = await _manager.StartBuildAsync(config);

        return Ok(new BuildResponse
        {
            OperationId = operationId,
            Status = "Started",
            Message = $"Build operation started with {config.Users.Count} users"
        });
    }

    /// <summary>
    /// Start a cleanup operation
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<BuildResponse>> StartCleanup([FromBody] CleanupRequest request)
    {
        var config = new EnvironmentConfig
        {
            Connection = new ConnectionConfig
            {
                Server = request.Server ?? "localhost",
                Port = request.Port ?? 389,
                BindDn = request.BindDn ?? "cn=admin,o=org",
                Password = request.Password ?? "",
                BaseDn = request.BaseDn ?? "o=org"
            },
            Execution = new ExecutionConfig { DryRun = request.DryRun ?? false }
        };

        var operationId = await _manager.StartCleanupAsync(config, request.Prefix ?? "testuser");

        return Ok(new BuildResponse
        {
            OperationId = operationId,
            Status = "Started",
            Message = $"Cleanup operation started for prefix: {request.Prefix}"
        });
    }

    /// <summary>
    /// Get operation status
    /// </summary>
    [HttpGet("operations/{operationId}")]
    public ActionResult<OperationState> GetOperation(string operationId)
    {
        var operation = _manager.GetOperation(operationId);
        if (operation == null)
            return NotFound(new { Message = "Operation not found" });

        return Ok(operation);
    }

    /// <summary>
    /// Get all operations
    /// </summary>
    [HttpGet("operations")]
    public ActionResult<IEnumerable<OperationState>> GetOperations()
    {
        return Ok(_manager.GetAllOperations());
    }

    /// <summary>
    /// Cancel an operation
    /// </summary>
    [HttpPost("operations/{operationId}/cancel")]
    public ActionResult CancelOperation(string operationId)
    {
        var success = _manager.CancelOperation(operationId);
        if (!success)
            return NotFound(new { Message = "Operation not found" });

        return Ok(new { Message = "Cancellation requested" });
    }

    /// <summary>
    /// Perform health check
    /// </summary>
    [HttpPost("health")]
    public async Task<ActionResult<HealthCheckResult>> HealthCheck([FromBody] ConnectionRequest request)
    {
        var config = new EnvironmentConfig
        {
            Connection = new ConnectionConfig
            {
                Server = request.Server ?? "localhost",
                Port = request.Port ?? 389,
                BindDn = request.BindDn ?? "cn=admin,o=org",
                Password = request.Password ?? "",
                BaseDn = request.BaseDn ?? "o=org"
            }
        };

        var result = await _manager.HealthCheckAsync(config);
        return Ok(result);
    }

    /// <summary>
    /// Get available presets
    /// </summary>
    [HttpGet("presets")]
    public ActionResult GetPresets()
    {
        return Ok(new[]
        {
            new { Name = "Simple", Description = ComplexityPreset.Simple.Description, Users = ComplexityPreset.Simple.UserCount },
            new { Name = "Medium", Description = ComplexityPreset.Medium.Description, Users = ComplexityPreset.Medium.UserCount },
            new { Name = "Complex", Description = ComplexityPreset.Complex.Description, Users = ComplexityPreset.Complex.UserCount },
            new { Name = "Brutal", Description = ComplexityPreset.Brutal.Description, Users = ComplexityPreset.Brutal.UserCount }
        });
    }
}

// Request/Response models
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

public class ConnectionRequest
{
    public string? Server { get; set; }
    public int? Port { get; set; }
    public string? BindDn { get; set; }
    public string? Password { get; set; }
    public string? BaseDn { get; set; }
}

public class BuildResponse
{
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

