// ============================================================================
// UserConfiguration.cs - User Creation Configuration
// Environment Builder - Modern test environment creation tool
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

namespace EnvironmentBuilderApp.Models;

/// <summary>
/// Represents the configuration for creating directory users.
/// Supports bulk user creation with templates and customizable attributes.
/// </summary>
public class UserConfiguration
{
    // ----------------------------------------------------------------------------
    // User Set Identification
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Unique identifier for this user set
    /// </summary>
    public string SetId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Display name for this user set (e.g., "Test Users Set 1")
    /// </summary>
    public string SetName { get; set; } = "User Set";
    
    /// <summary>
    /// Whether this user set is enabled for creation
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    // ----------------------------------------------------------------------------
    // User Naming Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Prefix for user names (e.g., "TestUser" creates TestUser001, TestUser002...)
    /// </summary>
    public string UserNamePrefix { get; set; } = "TestUser";
    
    /// <summary>
    /// Starting number for user sequence
    /// </summary>
    public int StartNumber { get; set; } = 1;
    
    /// <summary>
    /// Ending number for user sequence
    /// </summary>
    public int EndNumber { get; set; } = 10;
    
    /// <summary>
    /// Number of digits for zero-padding (e.g., 3 = 001, 002...)
    /// </summary>
    public int NumberPadding { get; set; } = 3;
    
    // ----------------------------------------------------------------------------
    // User Context/Location Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// The container/OU where users will be created
    /// Example: ou=Users,o=Organization
    /// </summary>
    public string UserContext { get; set; } = string.Empty;
    
    /// <summary>
    /// Domain name for email addresses
    /// </summary>
    public string DomainName { get; set; } = "example.com";
    
    // ----------------------------------------------------------------------------
    // User Attribute Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Default given name (first name) for users
    /// </summary>
    public string GivenName { get; set; } = "Test";
    
    /// <summary>
    /// Default surname (last name) for users
    /// </summary>
    public string Surname { get; set; } = "User";
    
    /// <summary>
    /// Default title for users
    /// </summary>
    public string Title { get; set; } = "Test Account";
    
    /// <summary>
    /// Default telephone number for users
    /// </summary>
    public string TelephoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Default location/city for users
    /// </summary>
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// Default department for users
    /// </summary>
    public string Department { get; set; } = string.Empty;
    
    // ----------------------------------------------------------------------------
    // Password Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Default password for all users in this set
    /// </summary>
    public string Password { get; set; } = "Password123!";
    
    /// <summary>
    /// Whether to use simple passwords (username = password)
    /// </summary>
    public bool UseSimplePasswords { get; set; } = false;
    
    /// <summary>
    /// Whether password must be changed on first login
    /// </summary>
    public bool RequirePasswordChange { get; set; } = false;
    
    // ----------------------------------------------------------------------------
    // Object Class Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// List of object classes for the user
    /// </summary>
    public List<string> ObjectClasses { get; set; } = new()
    {
        "inetOrgPerson",
        "organizationalPerson", 
        "person",
        "top"
    };
    
    // ----------------------------------------------------------------------------
    // Home Directory Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Whether to create home directories for users
    /// </summary>
    public bool CreateHomeDirectory { get; set; } = false;
    
    /// <summary>
    /// Base path for home directories
    /// </summary>
    public string HomeDirectoryBasePath { get; set; } = string.Empty;
    
    // ----------------------------------------------------------------------------
    // Helper Methods
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Gets the total number of users to be created
    /// </summary>
    public int GetUserCount()
    {
        return Math.Max(0, EndNumber - StartNumber + 1);
    }
    
    /// <summary>
    /// Generates a username for a given sequence number
    /// </summary>
    public string GenerateUsername(int sequenceNumber)
    {
        return $"{UserNamePrefix}{sequenceNumber.ToString().PadLeft(NumberPadding, '0')}";
    }
    
    /// <summary>
    /// Generates an email address for a given username
    /// </summary>
    public string GenerateEmail(string username)
    {
        return $"{username}@{DomainName}";
    }
    
    /// <summary>
    /// Gets the password for a specific user
    /// </summary>
    public string GetPassword(string username)
    {
        return UseSimplePasswords ? username : Password;
    }
}

