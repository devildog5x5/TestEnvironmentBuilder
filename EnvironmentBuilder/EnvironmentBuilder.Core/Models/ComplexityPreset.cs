namespace EnvironmentBuilder.Core.Models;

/// <summary>
/// Predefined complexity levels for test environments
/// Test Brutally - Build Your Level of Complexity
/// </summary>
public enum ComplexityLevel
{
    Simple,
    Medium,
    Complex,
    Brutal
}

/// <summary>
/// Configuration preset for different complexity levels
/// </summary>
public class ComplexityPreset
{
    public ComplexityLevel Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // User settings
    public int UserCount { get; set; }
    public bool RandomizeUserData { get; set; }
    public bool IncludePhotos { get; set; }
    
    // Organizational structure
    public int OrganizationalUnitDepth { get; set; }
    public int OrganizationalUnitsPerLevel { get; set; }
    public int GroupCount { get; set; }
    public bool NestedGroups { get; set; }
    
    // Advanced settings
    public bool CreateHomeDirectories { get; set; }
    public bool AssignRandomPermissions { get; set; }
    public int BatchSize { get; set; }
    public int ParallelOperations { get; set; }

    public static ComplexityPreset Simple => new()
    {
        Level = ComplexityLevel.Simple,
        Name = "Simple",
        Description = "Quick setup for basic testing - 10 users, flat structure",
        UserCount = 10,
        RandomizeUserData = false,
        IncludePhotos = false,
        OrganizationalUnitDepth = 1,
        OrganizationalUnitsPerLevel = 3,
        GroupCount = 2,
        NestedGroups = false,
        CreateHomeDirectories = false,
        AssignRandomPermissions = false,
        BatchSize = 10,
        ParallelOperations = 1
    };

    public static ComplexityPreset Medium => new()
    {
        Level = ComplexityLevel.Medium,
        Name = "Medium",
        Description = "Standard test environment - 100 users, nested OUs, groups",
        UserCount = 100,
        RandomizeUserData = true,
        IncludePhotos = false,
        OrganizationalUnitDepth = 2,
        OrganizationalUnitsPerLevel = 5,
        GroupCount = 10,
        NestedGroups = true,
        CreateHomeDirectories = false,
        AssignRandomPermissions = false,
        BatchSize = 25,
        ParallelOperations = 4
    };

    public static ComplexityPreset Complex => new()
    {
        Level = ComplexityLevel.Complex,
        Name = "Complex",
        Description = "Enterprise-scale testing - 1,000 users, deep hierarchy",
        UserCount = 1000,
        RandomizeUserData = true,
        IncludePhotos = true,
        OrganizationalUnitDepth = 4,
        OrganizationalUnitsPerLevel = 8,
        GroupCount = 50,
        NestedGroups = true,
        CreateHomeDirectories = true,
        AssignRandomPermissions = true,
        BatchSize = 50,
        ParallelOperations = 8
    };

    public static ComplexityPreset Brutal => new()
    {
        Level = ComplexityLevel.Brutal,
        Name = "Brutal",
        Description = "Stress test - 10,000+ users, maximum complexity, edge cases",
        UserCount = 10000,
        RandomizeUserData = true,
        IncludePhotos = true,
        OrganizationalUnitDepth = 6,
        OrganizationalUnitsPerLevel = 10,
        GroupCount = 200,
        NestedGroups = true,
        CreateHomeDirectories = true,
        AssignRandomPermissions = true,
        BatchSize = 100,
        ParallelOperations = 16
    };

    public static ComplexityPreset FromLevel(ComplexityLevel level) => level switch
    {
        ComplexityLevel.Simple => Simple,
        ComplexityLevel.Medium => Medium,
        ComplexityLevel.Complex => Complex,
        ComplexityLevel.Brutal => Brutal,
        _ => Simple
    };
}

