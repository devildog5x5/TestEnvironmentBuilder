// ============================================================================
// EnvironmentConfiguration.cs - Complete Environment Configuration
// Environment Builder - Modern test environment creation tool
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

using System.IO;
using Newtonsoft.Json;

namespace EnvironmentBuilderApp.Models;

/// <summary>
/// Master configuration that combines all settings for an environment.
/// This is the root object that gets saved/loaded from configuration files.
/// </summary>
public class EnvironmentConfiguration
{
    // ----------------------------------------------------------------------------
    // Configuration Metadata
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Unique identifier for this configuration
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Name of this environment configuration
    /// </summary>
    public string Name { get; set; } = "New Environment";
    
    /// <summary>
    /// Description of this environment
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Date this configuration was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Date this configuration was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Version of the configuration schema
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0";
    
    // ----------------------------------------------------------------------------
    // Component Configurations
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// LDAP connection settings
    /// </summary>
    public ConnectionSettings Connection { get; set; } = new();
    
    /// <summary>
    /// Directory tree structure configuration
    /// </summary>
    public TreeConfiguration TreeConfig { get; set; } = new();
    
    /// <summary>
    /// User creation configurations (supports multiple user sets)
    /// </summary>
    public List<UserConfiguration> UserConfigs { get; set; } = new();
    
    /// <summary>
    /// Home directory configuration
    /// </summary>
    public HomeDirectoryConfiguration HomeDirectoryConfig { get; set; } = new();
    
    // ----------------------------------------------------------------------------
    // Output Settings
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Path for LDIF export file
    /// </summary>
    public string LdifExportPath { get; set; } = @"C:\Temp\environment_export.ldif";
    
    /// <summary>
    /// Path for log file
    /// </summary>
    public string LogFilePath { get; set; } = @"C:\Temp\environment_builder.log";
    
    // ----------------------------------------------------------------------------
    // Execution Options
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Whether to only generate LDIF without importing
    /// </summary>
    public bool WriteOnlyMode { get; set; } = false;
    
    /// <summary>
    /// Whether to delete objects instead of creating (cleanup mode)
    /// </summary>
    public bool DeleteMode { get; set; } = false;
    
    /// <summary>
    /// Whether to modify existing objects
    /// </summary>
    public bool ModifyMode { get; set; } = false;
    
    // ----------------------------------------------------------------------------
    // Serialization Methods
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Saves this configuration to a JSON file
    /// </summary>
    public void SaveToFile(string filePath)
    {
        ModifiedDate = DateTime.Now;
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }
    
    /// <summary>
    /// Loads a configuration from a JSON file
    /// </summary>
    public static EnvironmentConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
            
        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<EnvironmentConfiguration>(json) 
            ?? new EnvironmentConfiguration();
    }
    
    /// <summary>
    /// Creates a default test environment configuration
    /// </summary>
    public static EnvironmentConfiguration CreateDefault()
    {
        var config = new EnvironmentConfiguration
        {
            Name = "Test Environment",
            Description = "Default test environment configuration"
        };
        
        // Add default user set
        config.UserConfigs.Add(new UserConfiguration
        {
            SetName = "Test Users",
            UserNamePrefix = "TestUser",
            StartNumber = 1,
            EndNumber = 10,
            Password = "Password123!"
        });
        
        // Set default tree structure
        config.TreeConfig = TreeConfiguration.CreateDefaultTemplate("TestOrg");
        
        return config;
    }
}

/// <summary>
/// Configuration for home directory creation
/// </summary>
public class HomeDirectoryConfiguration
{
    /// <summary>
    /// Whether home directory creation is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;
    
    /// <summary>
    /// Base network path for home directories
    /// Example: \\server\home\
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Drive letter to map for home directory
    /// </summary>
    public string DriveLetter { get; set; } = "H:";
    
    /// <summary>
    /// Template for home directory naming
    /// Use {username} as placeholder
    /// </summary>
    public string DirectoryTemplate { get; set; } = "{username}";
    
    /// <summary>
    /// Whether to set permissions on created directories
    /// </summary>
    public bool SetPermissions { get; set; } = true;
    
    /// <summary>
    /// Creates the full home directory path for a user
    /// </summary>
    public string GetHomeDirectoryPath(string username)
    {
        var dirName = DirectoryTemplate.Replace("{username}", username);
        return Path.Combine(BasePath, dirName);
    }
}

