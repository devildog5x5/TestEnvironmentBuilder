// ============================================================================
// ConnectionSettings.cs - LDAP/Directory Connection Configuration
// Environment Builder - Modern test environment creation tool
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

namespace EnvironmentBuilderApp.Models;

/// <summary>
/// Represents the connection settings for LDAP/Directory operations.
/// Stores server information, credentials, and SSL configuration.
/// </summary>
public class ConnectionSettings
{
    // ----------------------------------------------------------------------------
    // Server Connection Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// The IP address or hostname of the LDAP server
    /// </summary>
    public string ServerAddress { get; set; } = "localhost";
    
    /// <summary>
    /// The port number for LDAP connection (default: 389, SSL: 636)
    /// </summary>
    public int Port { get; set; } = 389;
    
    /// <summary>
    /// Whether to use SSL/TLS for the connection
    /// </summary>
    public bool UseSSL { get; set; } = false;
    
    // ----------------------------------------------------------------------------
    // Authentication Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// The distinguished name (DN) of the user for authentication
    /// Example: cn=admin,ou=Users,o=Organization
    /// </summary>
    public string BindDN { get; set; } = string.Empty;
    
    /// <summary>
    /// The password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to use anonymous binding (no authentication)
    /// </summary>
    public bool UseAnonymousBind { get; set; } = false;
    
    // ----------------------------------------------------------------------------
    // SSL/Certificate Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Path to the root certificate file (.der or .pem)
    /// Used for SSL certificate validation
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to skip SSL certificate validation (use with caution)
    /// </summary>
    public bool SkipCertificateValidation { get; set; } = false;
    
    // ----------------------------------------------------------------------------
    // Connection Options
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;
    
    /// <summary>
    /// Whether to stop processing on first error
    /// </summary>
    public bool StopOnError { get; set; } = true;
    
    /// <summary>
    /// The base DN for directory operations
    /// Example: o=Organization
    /// </summary>
    public string BaseDN { get; set; } = string.Empty;
    
    // ----------------------------------------------------------------------------
    // Helper Methods
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Gets the full LDAP URI for connection
    /// </summary>
    public string GetLdapUri()
    {
        string protocol = UseSSL ? "ldaps" : "ldap";
        return $"{protocol}://{ServerAddress}:{Port}";
    }
    
    /// <summary>
    /// Creates a copy of this settings object
    /// </summary>
    public ConnectionSettings Clone()
    {
        return new ConnectionSettings
        {
            ServerAddress = this.ServerAddress,
            Port = this.Port,
            UseSSL = this.UseSSL,
            BindDN = this.BindDN,
            Password = this.Password,
            UseAnonymousBind = this.UseAnonymousBind,
            CertificatePath = this.CertificatePath,
            SkipCertificateValidation = this.SkipCertificateValidation,
            ConnectionTimeout = this.ConnectionTimeout,
            StopOnError = this.StopOnError,
            BaseDN = this.BaseDN
        };
    }
}

