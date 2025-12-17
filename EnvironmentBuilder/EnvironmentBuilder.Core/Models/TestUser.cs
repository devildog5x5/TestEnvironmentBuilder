using System;

namespace EnvironmentBuilder.Core.Models;

/// <summary>
/// Represents a test user to be created in the directory
/// </summary>
public class TestUser
{
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string[] ObjectClasses { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> CustomAttributes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public UserCreationStatus Status { get; set; } = UserCreationStatus.Pending;
    public string? ErrorMessage { get; set; }
}

public enum UserCreationStatus
{
    Pending,
    InProgress,
    Success,
    Failed,
    Skipped
}

