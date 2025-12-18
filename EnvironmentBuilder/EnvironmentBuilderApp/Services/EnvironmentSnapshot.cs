// ============================================================================
// EnvironmentSnapshot.cs - Environment State Capture and Comparison
// Captures snapshots for before/after comparison
// Environment Builder - Test Brutally
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace EnvironmentBuilderApp.Services
{
    /// <summary>
    /// Captures and compares environment snapshots
    /// </summary>
    public class EnvironmentSnapshotService
    {
        private readonly string _snapshotsPath;

        public EnvironmentSnapshotService(string snapshotsPath = "")
        {
            _snapshotsPath = string.IsNullOrEmpty(snapshotsPath) 
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                               "EnvironmentBuilder", "Snapshots")
                : snapshotsPath;
            Directory.CreateDirectory(_snapshotsPath);
        }

        /// <summary>
        /// Creates a snapshot of the current environment state
        /// </summary>
        public EnvironmentSnapshot CreateSnapshot(string name, List<UserSnapshot> users, 
                                                   List<ContainerSnapshot> containers)
        {
            var snapshot = new EnvironmentSnapshot
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                CreatedAt = DateTime.Now,
                Users = users,
                Containers = containers,
                TotalUsers = users.Count,
                TotalContainers = containers.Count
            };

            // Calculate hash for comparison
            snapshot.Hash = CalculateHash(snapshot);
            
            return snapshot;
        }

        /// <summary>
        /// Saves a snapshot to disk
        /// </summary>
        public void SaveSnapshot(EnvironmentSnapshot snapshot)
        {
            var filePath = Path.Combine(_snapshotsPath, $"{snapshot.Id}.json");
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads a snapshot from disk
        /// </summary>
        public EnvironmentSnapshot? LoadSnapshot(string id)
        {
            var filePath = Path.Combine(_snapshotsPath, $"{id}.json");
            if (!File.Exists(filePath)) return null;
            
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<EnvironmentSnapshot>(json);
        }

        /// <summary>
        /// Gets all saved snapshots
        /// </summary>
        public List<SnapshotInfo> GetAllSnapshots()
        {
            var snapshots = new List<SnapshotInfo>();
            
            foreach (var file in Directory.GetFiles(_snapshotsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var snapshot = JsonSerializer.Deserialize<EnvironmentSnapshot>(json);
                    if (snapshot != null)
                    {
                        snapshots.Add(new SnapshotInfo
                        {
                            Id = snapshot.Id,
                            Name = snapshot.Name,
                            CreatedAt = snapshot.CreatedAt,
                            TotalUsers = snapshot.TotalUsers,
                            TotalContainers = snapshot.TotalContainers
                        });
                    }
                }
                catch { /* Skip invalid files */ }
            }

            return snapshots.OrderByDescending(s => s.CreatedAt).ToList();
        }

        /// <summary>
        /// Compares two snapshots and returns differences
        /// </summary>
        public SnapshotComparison CompareSnapshots(EnvironmentSnapshot before, EnvironmentSnapshot after)
        {
            var comparison = new SnapshotComparison
            {
                BeforeSnapshot = before.Name,
                AfterSnapshot = after.Name,
                BeforeTime = before.CreatedAt,
                AfterTime = after.CreatedAt,
                TimeDifference = after.CreatedAt - before.CreatedAt
            };

            // Compare users
            var beforeUsernames = before.Users.Select(u => u.Username).ToHashSet();
            var afterUsernames = after.Users.Select(u => u.Username).ToHashSet();

            comparison.AddedUsers = after.Users
                .Where(u => !beforeUsernames.Contains(u.Username))
                .ToList();

            comparison.RemovedUsers = before.Users
                .Where(u => !afterUsernames.Contains(u.Username))
                .ToList();

            // Find modified users
            foreach (var afterUser in after.Users)
            {
                var beforeUser = before.Users.FirstOrDefault(u => u.Username == afterUser.Username);
                if (beforeUser != null && beforeUser.Hash != afterUser.Hash)
                {
                    comparison.ModifiedUsers.Add(new UserModification
                    {
                        Username = afterUser.Username,
                        Changes = GetUserChanges(beforeUser, afterUser)
                    });
                }
            }

            // Compare containers
            var beforeContainers = before.Containers.Select(c => c.DN).ToHashSet();
            var afterContainers = after.Containers.Select(c => c.DN).ToHashSet();

            comparison.AddedContainers = after.Containers
                .Where(c => !beforeContainers.Contains(c.DN))
                .ToList();

            comparison.RemovedContainers = before.Containers
                .Where(c => !afterContainers.Contains(c.DN))
                .ToList();

            // Summary
            comparison.HasChanges = comparison.AddedUsers.Any() || 
                                   comparison.RemovedUsers.Any() || 
                                   comparison.ModifiedUsers.Any() ||
                                   comparison.AddedContainers.Any() || 
                                   comparison.RemovedContainers.Any();

            return comparison;
        }

        /// <summary>
        /// Generates a diff report
        /// </summary>
        public string GenerateDiffReport(SnapshotComparison comparison)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║              ENVIRONMENT COMPARISON REPORT                   ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Before: {comparison.BeforeSnapshot} ({comparison.BeforeTime:yyyy-MM-dd HH:mm:ss})");
            sb.AppendLine($"After:  {comparison.AfterSnapshot} ({comparison.AfterTime:yyyy-MM-dd HH:mm:ss})");
            sb.AppendLine($"Time Difference: {comparison.TimeDifference}");
            sb.AppendLine();

            if (!comparison.HasChanges)
            {
                sb.AppendLine("✅ NO CHANGES DETECTED - Environments are identical");
                return sb.ToString();
            }

            // Added Users
            if (comparison.AddedUsers.Any())
            {
                sb.AppendLine($"➕ ADDED USERS ({comparison.AddedUsers.Count}):");
                foreach (var user in comparison.AddedUsers.Take(20))
                {
                    sb.AppendLine($"   + {user.Username} ({user.Email})");
                }
                if (comparison.AddedUsers.Count > 20)
                    sb.AppendLine($"   ... and {comparison.AddedUsers.Count - 20} more");
                sb.AppendLine();
            }

            // Removed Users
            if (comparison.RemovedUsers.Any())
            {
                sb.AppendLine($"➖ REMOVED USERS ({comparison.RemovedUsers.Count}):");
                foreach (var user in comparison.RemovedUsers.Take(20))
                {
                    sb.AppendLine($"   - {user.Username} ({user.Email})");
                }
                if (comparison.RemovedUsers.Count > 20)
                    sb.AppendLine($"   ... and {comparison.RemovedUsers.Count - 20} more");
                sb.AppendLine();
            }

            // Modified Users
            if (comparison.ModifiedUsers.Any())
            {
                sb.AppendLine($"✏️ MODIFIED USERS ({comparison.ModifiedUsers.Count}):");
                foreach (var mod in comparison.ModifiedUsers.Take(10))
                {
                    sb.AppendLine($"   ~ {mod.Username}:");
                    foreach (var change in mod.Changes)
                    {
                        sb.AppendLine($"      {change.Attribute}: \"{change.OldValue}\" → \"{change.NewValue}\"");
                    }
                }
                if (comparison.ModifiedUsers.Count > 10)
                    sb.AppendLine($"   ... and {comparison.ModifiedUsers.Count - 10} more");
                sb.AppendLine();
            }

            // Container changes
            if (comparison.AddedContainers.Any())
            {
                sb.AppendLine($"📁 ADDED CONTAINERS ({comparison.AddedContainers.Count}):");
                foreach (var container in comparison.AddedContainers)
                {
                    sb.AppendLine($"   + {container.DN}");
                }
                sb.AppendLine();
            }

            if (comparison.RemovedContainers.Any())
            {
                sb.AppendLine($"📁 REMOVED CONTAINERS ({comparison.RemovedContainers.Count}):");
                foreach (var container in comparison.RemovedContainers)
                {
                    sb.AppendLine($"   - {container.DN}");
                }
                sb.AppendLine();
            }

            // Summary
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("SUMMARY:");
            sb.AppendLine($"  Users Added:      {comparison.AddedUsers.Count}");
            sb.AppendLine($"  Users Removed:    {comparison.RemovedUsers.Count}");
            sb.AppendLine($"  Users Modified:   {comparison.ModifiedUsers.Count}");
            sb.AppendLine($"  Containers Added: {comparison.AddedContainers.Count}");
            sb.AppendLine($"  Containers Removed: {comparison.RemovedContainers.Count}");

            return sb.ToString();
        }

        private string CalculateHash(EnvironmentSnapshot snapshot)
        {
            var data = string.Join("|", snapshot.Users.Select(u => u.Hash))
                     + string.Join("|", snapshot.Containers.Select(c => c.DN));
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    Encoding.UTF8.GetBytes(data)));
        }

        private List<AttributeChange> GetUserChanges(UserSnapshot before, UserSnapshot after)
        {
            var changes = new List<AttributeChange>();

            if (before.Email != after.Email)
                changes.Add(new AttributeChange { Attribute = "Email", OldValue = before.Email, NewValue = after.Email });
            if (before.FirstName != after.FirstName)
                changes.Add(new AttributeChange { Attribute = "FirstName", OldValue = before.FirstName, NewValue = after.FirstName });
            if (before.LastName != after.LastName)
                changes.Add(new AttributeChange { Attribute = "LastName", OldValue = before.LastName, NewValue = after.LastName });
            if (before.Department != after.Department)
                changes.Add(new AttributeChange { Attribute = "Department", OldValue = before.Department, NewValue = after.Department });
            if (before.Enabled != after.Enabled)
                changes.Add(new AttributeChange { Attribute = "Enabled", OldValue = before.Enabled.ToString(), NewValue = after.Enabled.ToString() });

            return changes;
        }
    }

    #region Models

    public class EnvironmentSnapshot
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Hash { get; set; } = "";
        public int TotalUsers { get; set; }
        public int TotalContainers { get; set; }
        public List<UserSnapshot> Users { get; set; } = new();
        public List<ContainerSnapshot> Containers { get; set; } = new();
    }

    public class SnapshotInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int TotalUsers { get; set; }
        public int TotalContainers { get; set; }
    }

    public class UserSnapshot
    {
        public string Username { get; set; } = "";
        public string DN { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public bool Enabled { get; set; }
        public string Hash { get; set; } = "";
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    public class ContainerSnapshot
    {
        public string DN { get; set; } = "";
        public string Name { get; set; } = "";
        public int ChildCount { get; set; }
    }

    public class SnapshotComparison
    {
        public string BeforeSnapshot { get; set; } = "";
        public string AfterSnapshot { get; set; } = "";
        public DateTime BeforeTime { get; set; }
        public DateTime AfterTime { get; set; }
        public TimeSpan TimeDifference { get; set; }
        public bool HasChanges { get; set; }
        public List<UserSnapshot> AddedUsers { get; set; } = new();
        public List<UserSnapshot> RemovedUsers { get; set; } = new();
        public List<UserModification> ModifiedUsers { get; set; } = new();
        public List<ContainerSnapshot> AddedContainers { get; set; } = new();
        public List<ContainerSnapshot> RemovedContainers { get; set; } = new();
    }

    public class UserModification
    {
        public string Username { get; set; } = "";
        public List<AttributeChange> Changes { get; set; } = new();
    }

    public class AttributeChange
    {
        public string Attribute { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }

    #endregion
}

