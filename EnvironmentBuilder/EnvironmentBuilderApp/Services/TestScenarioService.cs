// ============================================================================
// TestScenarioService.cs - Predefined Test Scenarios
// Load testing, security audits, password policy, edge cases, chaos testing
// Environment Builder - Test Brutally
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentBuilderApp.Services
{
    /// <summary>
    /// Provides predefined test scenarios for various QA purposes
    /// </summary>
    public class TestScenarioService
    {
        private readonly TestDataGenerator _dataGenerator;
        private readonly PerformanceTracker _perfTracker;

        public TestScenarioService(TestDataGenerator? dataGenerator = null, PerformanceTracker? perfTracker = null)
        {
            _dataGenerator = dataGenerator ?? new TestDataGenerator();
            _perfTracker = perfTracker ?? new PerformanceTracker();
        }

        #region Predefined Scenarios

        /// <summary>
        /// Gets all available test scenarios
        /// </summary>
        public List<TestScenario> GetAllScenarios()
        {
            return new List<TestScenario>
            {
                GetLoadTestScenario(),
                GetStressTestScenario(),
                GetSecurityAuditScenario(),
                GetPasswordPolicyScenario(),
                GetEdgeCaseScenario(),
                GetChaosTestScenario(),
                GetRegressionScenario(),
                GetPermissionTestScenario(),
                GetUnicodeTestScenario(),
                GetNegativeTestScenario()
            };
        }

        public TestScenario GetLoadTestScenario()
        {
            return new TestScenario
            {
                Id = "load-test",
                Name = "Load Test",
                Description = "Creates 10,000 users to measure LDAP performance and throughput",
                Category = ScenarioCategory.Performance,
                Icon = "🏋️",
                EstimatedDuration = TimeSpan.FromMinutes(30),
                UserCount = 10000,
                ContainerCount = 10,
                DataProfile = DataProfile.Minimal,
                Tags = new[] { "performance", "load", "throughput" },
                Parameters = new Dictionary<string, object>
                {
                    { "BatchSize", 100 },
                    { "MeasureResponseTimes", true },
                    { "ConcurrentConnections", 5 }
                }
            };
        }

        public TestScenario GetStressTestScenario()
        {
            return new TestScenario
            {
                Id = "stress-test",
                Name = "Stress Test",
                Description = "Creates maximum load to find system breaking points",
                Category = ScenarioCategory.Performance,
                Icon = "💥",
                EstimatedDuration = TimeSpan.FromHours(1),
                UserCount = 50000,
                ContainerCount = 50,
                DataProfile = DataProfile.Minimal,
                Tags = new[] { "stress", "breaking-point", "limits" },
                Parameters = new Dictionary<string, object>
                {
                    { "MaxConcurrency", 20 },
                    { "RampUpPeriod", 300 },
                    { "StopOnError", false }
                }
            };
        }

        public TestScenario GetSecurityAuditScenario()
        {
            return new TestScenario
            {
                Id = "security-audit",
                Name = "Security Audit",
                Description = "Creates users with various permission levels to verify ACLs",
                Category = ScenarioCategory.Security,
                Icon = "🔐",
                EstimatedDuration = TimeSpan.FromMinutes(15),
                UserCount = 100,
                ContainerCount = 5,
                DataProfile = DataProfile.RoleBased,
                Tags = new[] { "security", "permissions", "acl", "audit" },
                Parameters = new Dictionary<string, object>
                {
                    { "IncludeAdminUsers", true },
                    { "IncludeServiceAccounts", true },
                    { "IncludeGuestUsers", true },
                    { "VerifyPermissions", true }
                },
                UserRoleDistribution = new Dictionary<UserRole, int>
                {
                    { UserRole.Admin, 5 },
                    { UserRole.PowerUser, 10 },
                    { UserRole.StandardUser, 50 },
                    { UserRole.ReadOnly, 20 },
                    { UserRole.ServiceAccount, 10 },
                    { UserRole.Guest, 3 },
                    { UserRole.Auditor, 2 }
                }
            };
        }

        public TestScenario GetPasswordPolicyScenario()
        {
            return new TestScenario
            {
                Id = "password-policy",
                Name = "Password Policy Test",
                Description = "Creates users with various password states to test policy enforcement",
                Category = ScenarioCategory.Security,
                Icon = "🔑",
                EstimatedDuration = TimeSpan.FromMinutes(10),
                UserCount = 50,
                ContainerCount = 1,
                DataProfile = DataProfile.PasswordTest,
                Tags = new[] { "password", "policy", "expiration", "lockout" },
                Parameters = new Dictionary<string, object>
                {
                    { "IncludeWeakPasswords", true },
                    { "IncludeExpiredPasswords", true },
                    { "IncludeLockedAccounts", true },
                    { "IncludeMustChangePassword", true }
                },
                PasswordTestDistribution = new Dictionary<PasswordTestType, int>
                {
                    { PasswordTestType.Weak, 5 },
                    { PasswordTestType.Strong, 10 },
                    { PasswordTestType.Empty, 2 },
                    { PasswordTestType.MaxLength, 3 },
                    { PasswordTestType.Expired, 5 },
                    { PasswordTestType.Locked, 5 },
                    { PasswordTestType.MustChange, 5 },
                    { PasswordTestType.SameAsUsername, 3 },
                    { PasswordTestType.CommonPatterns, 5 },
                    { PasswordTestType.NeverExpires, 5 },
                    { PasswordTestType.UnicodePassword, 2 }
                }
            };
        }

        public TestScenario GetEdgeCaseScenario()
        {
            return new TestScenario
            {
                Id = "edge-cases",
                Name = "Edge Case Test",
                Description = "Creates users with boundary conditions and special characters",
                Category = ScenarioCategory.Functional,
                Icon = "🔬",
                EstimatedDuration = TimeSpan.FromMinutes(5),
                UserCount = 30,
                ContainerCount = 1,
                DataProfile = DataProfile.EdgeCase,
                Tags = new[] { "edge-case", "boundary", "special-chars", "unicode" },
                EdgeCaseDistribution = new Dictionary<EdgeCaseType, int>
                {
                    { EdgeCaseType.Unicode, 5 },
                    { EdgeCaseType.MaxLength, 5 },
                    { EdgeCaseType.MinLength, 5 },
                    { EdgeCaseType.SpecialCharacters, 5 },
                    { EdgeCaseType.Whitespace, 5 },
                    { EdgeCaseType.CaseSensitivity, 5 }
                }
            };
        }

        public TestScenario GetChaosTestScenario()
        {
            return new TestScenario
            {
                Id = "chaos-test",
                Name = "Chaos / Negative Test",
                Description = "Creates malicious data to test input validation and security",
                Category = ScenarioCategory.Security,
                Icon = "😈",
                EstimatedDuration = TimeSpan.FromMinutes(10),
                UserCount = 40,
                ContainerCount = 1,
                DataProfile = DataProfile.NegativeTest,
                Tags = new[] { "chaos", "negative", "injection", "security" },
                IsDangerous = true,
                WarningMessage = "This scenario creates potentially malicious data (SQL injection, XSS, etc.)",
                NegativeTestDistribution = new Dictionary<NegativeTestType, int>
                {
                    { NegativeTestType.SqlInjection, 5 },
                    { NegativeTestType.XssCrossSiteScripting, 5 },
                    { NegativeTestType.PathTraversal, 5 },
                    { NegativeTestType.NullBytes, 5 },
                    { NegativeTestType.BufferOverflow, 5 },
                    { NegativeTestType.CommandInjection, 5 },
                    { NegativeTestType.LdapInjection, 5 },
                    { NegativeTestType.FormatString, 5 }
                }
            };
        }

        public TestScenario GetRegressionScenario()
        {
            return new TestScenario
            {
                Id = "regression",
                Name = "Regression Test Environment",
                Description = "Creates a standard test environment for regression testing",
                Category = ScenarioCategory.Functional,
                Icon = "🔄",
                EstimatedDuration = TimeSpan.FromMinutes(20),
                UserCount = 500,
                ContainerCount = 10,
                DataProfile = DataProfile.Realistic,
                Tags = new[] { "regression", "standard", "baseline" },
                Parameters = new Dictionary<string, object>
                {
                    { "CreateSnapshot", true },
                    { "SnapshotName", $"Regression_{DateTime.Now:yyyyMMdd}" }
                }
            };
        }

        public TestScenario GetPermissionTestScenario()
        {
            return new TestScenario
            {
                Id = "permission-test",
                Name = "Permission Matrix Test",
                Description = "Creates a complete permission matrix for authorization testing",
                Category = ScenarioCategory.Security,
                Icon = "🛡️",
                EstimatedDuration = TimeSpan.FromMinutes(15),
                UserCount = 70,
                ContainerCount = 7,
                DataProfile = DataProfile.RoleBased,
                Tags = new[] { "permissions", "authorization", "rbac", "matrix" }
            };
        }

        public TestScenario GetUnicodeTestScenario()
        {
            return new TestScenario
            {
                Id = "unicode-test",
                Name = "Internationalization Test",
                Description = "Creates users with international characters and names",
                Category = ScenarioCategory.Functional,
                Icon = "🌍",
                EstimatedDuration = TimeSpan.FromMinutes(5),
                UserCount = 50,
                ContainerCount = 1,
                DataProfile = DataProfile.International,
                Tags = new[] { "unicode", "i18n", "international", "encoding" }
            };
        }

        public TestScenario GetNegativeTestScenario()
        {
            return new TestScenario
            {
                Id = "negative-test",
                Name = "Negative / Error Path Test",
                Description = "Creates invalid configurations to test error handling",
                Category = ScenarioCategory.Functional,
                Icon = "❌",
                EstimatedDuration = TimeSpan.FromMinutes(10),
                UserCount = 30,
                ContainerCount = 1,
                DataProfile = DataProfile.NegativeTest,
                Tags = new[] { "negative", "error", "validation", "handling" }
            };
        }

        #endregion

        #region Scenario Execution

        /// <summary>
        /// Generates users based on scenario configuration
        /// </summary>
        public List<TestUser> GenerateScenarioUsers(TestScenario scenario, string prefix)
        {
            var users = new List<TestUser>();
            int counter = 1;

            switch (scenario.DataProfile)
            {
                case DataProfile.Realistic:
                case DataProfile.Minimal:
                    for (int i = 0; i < scenario.UserCount; i++)
                    {
                        users.Add(_dataGenerator.GenerateRealisticUser(prefix, counter++));
                    }
                    break;

                case DataProfile.RoleBased:
                    if (scenario.UserRoleDistribution != null)
                    {
                        foreach (var (role, count) in scenario.UserRoleDistribution)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                users.Add(_dataGenerator.GenerateRoleBasedUser(prefix, counter++, role));
                            }
                        }
                    }
                    break;

                case DataProfile.PasswordTest:
                    if (scenario.PasswordTestDistribution != null)
                    {
                        foreach (var (pwdType, count) in scenario.PasswordTestDistribution)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                users.Add(_dataGenerator.GeneratePasswordTestUser(prefix, counter++, pwdType));
                            }
                        }
                    }
                    break;

                case DataProfile.EdgeCase:
                    if (scenario.EdgeCaseDistribution != null)
                    {
                        foreach (var (edgeType, count) in scenario.EdgeCaseDistribution)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                users.Add(_dataGenerator.GenerateEdgeCaseUser(prefix, counter++, edgeType));
                            }
                        }
                    }
                    break;

                case DataProfile.NegativeTest:
                    if (scenario.NegativeTestDistribution != null)
                    {
                        foreach (var (negType, count) in scenario.NegativeTestDistribution)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                users.Add(_dataGenerator.GenerateNegativeTestUser(prefix, counter++, negType));
                            }
                        }
                    }
                    break;

                case DataProfile.International:
                    for (int i = 0; i < scenario.UserCount; i++)
                    {
                        users.Add(_dataGenerator.GenerateEdgeCaseUser(prefix, counter++, EdgeCaseType.Unicode));
                    }
                    break;
            }

            return users;
        }

        /// <summary>
        /// Gets scenarios by category
        /// </summary>
        public List<TestScenario> GetScenariosByCategory(ScenarioCategory category)
        {
            return GetAllScenarios().Where(s => s.Category == category).ToList();
        }

        /// <summary>
        /// Gets a scenario by ID
        /// </summary>
        public TestScenario? GetScenarioById(string id)
        {
            return GetAllScenarios().FirstOrDefault(s => s.Id == id);
        }

        #endregion
    }

    #region Models

    public class TestScenario
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public ScenarioCategory Category { get; set; }
        public string Icon { get; set; } = "";
        public TimeSpan EstimatedDuration { get; set; }
        public int UserCount { get; set; }
        public int ContainerCount { get; set; }
        public DataProfile DataProfile { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public bool IsDangerous { get; set; }
        public string? WarningMessage { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<UserRole, int>? UserRoleDistribution { get; set; }
        public Dictionary<PasswordTestType, int>? PasswordTestDistribution { get; set; }
        public Dictionary<EdgeCaseType, int>? EdgeCaseDistribution { get; set; }
        public Dictionary<NegativeTestType, int>? NegativeTestDistribution { get; set; }
    }

    public enum ScenarioCategory
    {
        Performance,
        Security,
        Functional,
        Regression
    }

    public enum DataProfile
    {
        Minimal,
        Realistic,
        RoleBased,
        PasswordTest,
        EdgeCase,
        NegativeTest,
        International
    }

    #endregion
}

