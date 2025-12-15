// ============================================================================
// TreeConfiguration.cs - Directory Tree/Container Configuration
// Environment Builder - Modern test environment creation tool
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

namespace EnvironmentBuilderApp.Models;

/// <summary>
/// Represents a node in the directory tree structure.
/// Can be an Organization, Organizational Unit, or Container.
/// </summary>
public class TreeNode
{
    /// <summary>
    /// The name of this node (e.g., "Users", "Groups", "Provo")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The type of directory object
    /// </summary>
    public TreeNodeType NodeType { get; set; } = TreeNodeType.OrganizationalUnit;
    
    /// <summary>
    /// Child nodes under this node
    /// </summary>
    public List<TreeNode> Children { get; set; } = new();
    
    /// <summary>
    /// Description for this container
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the LDAP attribute name for this node type
    /// </summary>
    public string GetAttributeName()
    {
        return NodeType switch
        {
            TreeNodeType.Organization => "o",
            TreeNodeType.OrganizationalUnit => "ou",
            TreeNodeType.Container => "cn",
            TreeNodeType.Country => "c",
            TreeNodeType.Domain => "dc",
            _ => "ou"
        };
    }
    
    /// <summary>
    /// Gets the RDN (Relative Distinguished Name) for this node
    /// </summary>
    public string GetRDN()
    {
        return $"{GetAttributeName()}={Name}";
    }
}

/// <summary>
/// Types of directory tree nodes
/// </summary>
public enum TreeNodeType
{
    Organization,       // o=
    OrganizationalUnit, // ou=
    Container,          // cn=
    Country,            // c=
    Domain              // dc=
}

/// <summary>
/// Configuration for the entire directory tree structure.
/// Defines the organizational hierarchy to be created.
/// </summary>
public class TreeConfiguration
{
    // ----------------------------------------------------------------------------
    // Tree Structure Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Name for this tree configuration
    /// </summary>
    public string ConfigurationName { get; set; } = "Default Environment";
    
    /// <summary>
    /// Description of this environment
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Root nodes of the directory tree
    /// </summary>
    public List<TreeNode> RootNodes { get; set; } = new();
    
    /// <summary>
    /// Whether to create tree structure during import
    /// </summary>
    public bool CreateTreeStructure { get; set; } = true;
    
    // ----------------------------------------------------------------------------
    // Environment Type
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// The type of environment (for preset configurations)
    /// </summary>
    public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.Development;
    
    // ----------------------------------------------------------------------------
    // Preset Templates
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Creates a default tree structure for a simple test environment
    /// </summary>
    public static TreeConfiguration CreateDefaultTemplate(string organizationName)
    {
        return new TreeConfiguration
        {
            ConfigurationName = $"{organizationName} Environment",
            Description = "Default test environment structure",
            RootNodes = new List<TreeNode>
            {
                new TreeNode
                {
                    Name = organizationName,
                    NodeType = TreeNodeType.Organization,
                    Description = "Root organization",
                    Children = new List<TreeNode>
                    {
                        new TreeNode
                        {
                            Name = "Users",
                            NodeType = TreeNodeType.OrganizationalUnit,
                            Description = "User accounts container"
                        },
                        new TreeNode
                        {
                            Name = "Groups",
                            NodeType = TreeNodeType.OrganizationalUnit,
                            Description = "Security and distribution groups"
                        },
                        new TreeNode
                        {
                            Name = "Services",
                            NodeType = TreeNodeType.OrganizationalUnit,
                            Description = "Service accounts"
                        },
                        new TreeNode
                        {
                            Name = "Computers",
                            NodeType = TreeNodeType.OrganizationalUnit,
                            Description = "Computer accounts"
                        }
                    }
                }
            }
        };
    }
    
    /// <summary>
    /// Creates a geographic-based tree structure
    /// </summary>
    public static TreeConfiguration CreateGeographicTemplate(string organizationName, List<string> locations)
    {
        var config = new TreeConfiguration
        {
            ConfigurationName = $"{organizationName} Geographic Environment",
            Description = "Geographic-based organizational structure"
        };
        
        var orgNode = new TreeNode
        {
            Name = organizationName,
            NodeType = TreeNodeType.Organization,
            Description = "Root organization"
        };
        
        foreach (var location in locations)
        {
            orgNode.Children.Add(new TreeNode
            {
                Name = location,
                NodeType = TreeNodeType.OrganizationalUnit,
                Description = $"{location} office",
                Children = new List<TreeNode>
                {
                    new TreeNode { Name = "Users", NodeType = TreeNodeType.OrganizationalUnit },
                    new TreeNode { Name = "Groups", NodeType = TreeNodeType.OrganizationalUnit },
                    new TreeNode { Name = "Resources", NodeType = TreeNodeType.OrganizationalUnit }
                }
            });
        }
        
        config.RootNodes.Add(orgNode);
        return config;
    }
}

/// <summary>
/// Types of environments for preset configurations
/// </summary>
public enum EnvironmentType
{
    Development,
    Testing,
    Staging,
    Production,
    Custom
}

