namespace EnvironmentBuilder.Core.Models;

/// <summary>
/// Complete configuration for a test environment
/// </summary>
public class EnvironmentConfig
{
    public string Name { get; set; } = "Test Environment";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Connection settings
    public ConnectionConfig Connection { get; set; } = new();
    
    // Complexity preset (can be overridden)
    public ComplexityLevel ComplexityLevel { get; set; } = ComplexityLevel.Simple;
    
    // User configuration
    public UserGenerationConfig Users { get; set; } = new();
    
    // Organizational structure
    public OrganizationConfig Organization { get; set; } = new();
    
    // Output settings
    public OutputConfig Output { get; set; } = new();
    
    // Execution settings
    public ExecutionConfig Execution { get; set; } = new();

    /// <summary>
    /// Apply a complexity preset to this configuration
    /// </summary>
    public void ApplyPreset(ComplexityPreset preset)
    {
        ComplexityLevel = preset.Level;
        Users.Count = preset.UserCount;
        Users.RandomizeData = preset.RandomizeUserData;
        Organization.MaxDepth = preset.OrganizationalUnitDepth;
        Organization.UnitsPerLevel = preset.OrganizationalUnitsPerLevel;
        Organization.GroupCount = preset.GroupCount;
        Organization.NestedGroups = preset.NestedGroups;
        Execution.BatchSize = preset.BatchSize;
        Execution.ParallelOperations = preset.ParallelOperations;
    }
}

public class ConnectionConfig
{
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 389;
    public bool UseSsl { get; set; } = false;
    public string BindDn { get; set; } = "cn=admin,o=org";
    public string Password { get; set; } = string.Empty;
    public string BaseDn { get; set; } = "o=org";
    public int TimeoutSeconds { get; set; } = 30;
}

public class UserGenerationConfig
{
    public int Count { get; set; } = 10;
    public string Prefix { get; set; } = "testuser";
    public int StartNumber { get; set; } = 1;
    public bool RandomizeData { get; set; } = true;
    public string DefaultPassword { get; set; } = "Test123!";
    public bool PasswordMatchesUsername { get; set; } = false;
    public string UserContainer { get; set; } = "ou=users";
    public string[] ObjectClasses { get; set; } = { "inetOrgPerson", "organizationalPerson", "person", "top" };
    public string Locale { get; set; } = "en_US"; // For Bogus data generation
}

public class OrganizationConfig
{
    public bool CreateStructure { get; set; } = true;
    public int MaxDepth { get; set; } = 2;
    public int UnitsPerLevel { get; set; } = 3;
    public int GroupCount { get; set; } = 5;
    public bool NestedGroups { get; set; } = false;
    public List<string> PredefinedOUs { get; set; } = new() { "users", "groups", "services" };
}

public class OutputConfig
{
    public bool GenerateLdif { get; set; } = true;
    public string LdifPath { get; set; } = "output.ldif";
    public bool GenerateReport { get; set; } = true;
    public string ReportPath { get; set; } = "report.html";
    public bool ExportCsv { get; set; } = false;
    public string CsvPath { get; set; } = "users.csv";
}

public class ExecutionConfig
{
    public bool DryRun { get; set; } = false;
    public bool StopOnError { get; set; } = false;
    public int BatchSize { get; set; } = 25;
    public int ParallelOperations { get; set; } = 4;
    public int DelayBetweenBatchesMs { get; set; } = 100;
    public bool ValidateAfterCreation { get; set; } = true;
}

