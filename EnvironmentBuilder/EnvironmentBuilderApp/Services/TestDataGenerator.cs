// ============================================================================
// TestDataGenerator.cs - Comprehensive Test Data Generation Service
// Generates realistic, edge case, and negative test data
// Environment Builder - Test Brutally
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnvironmentBuilderApp.Services
{
    /// <summary>
    /// Generates various types of test data for QA/Testing scenarios
    /// </summary>
    public class TestDataGenerator
    {
        private readonly Random _random = new();

        #region Name Data
        
        private static readonly string[] FirstNames = {
            // Common English
            "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda",
            "William", "Elizabeth", "David", "Barbara", "Richard", "Susan", "Joseph", "Jessica",
            "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Lisa", "Daniel", "Nancy",
            // International
            "José", "María", "Wei", "Yuki", "Mohammed", "Fatima", "Aleksandr", "Olga",
            "Hans", "Ingrid", "Pierre", "Marie", "Giovanni", "Francesca", "Raj", "Priya",
            "Björn", "Astrid", "Dmitri", "Natasha", "Chen", "Mei", "Takeshi", "Sakura",
            // Edge cases
            "O'Brien", "McDonald", "Van Der Berg", "De La Cruz", "St. John", "Mary-Jane"
        };

        private static readonly string[] LastNames = {
            // Common English
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
            "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
            "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
            // International
            "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Dubois", "Lefebvre",
            "Rossi", "Russo", "Ferrari", "Kowalski", "Nowak", "Ivanov", "Petrov", "Smirnov",
            "Tanaka", "Yamamoto", "Watanabe", "Wang", "Li", "Zhang", "Chen", "Patel", "Singh",
            // Edge cases
            "O'Connor", "MacDonald", "Van Dyke", "De Souza", "St. Claire", "Smith-Jones"
        };

        #endregion

        #region Domain Data

        private static readonly string[] EmailDomains = {
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "company.com",
            "test.org", "example.net", "mail.co.uk", "proton.me", "icloud.com"
        };

        private static readonly string[] Departments = {
            "Engineering", "Sales", "Marketing", "HR", "Finance", "IT", "Support",
            "Operations", "Legal", "Research", "Development", "QA", "Security"
        };

        private static readonly string[] Titles = {
            "Engineer", "Manager", "Analyst", "Developer", "Administrator", "Specialist",
            "Director", "Coordinator", "Lead", "Architect", "Consultant", "Associate"
        };

        #endregion

        #region Edge Case Data

        private static readonly string[] UnicodeStrings = {
            "Ñoño", "Müller", "日本語", "العربية", "עברית", "Ελληνικά",
            "中文字符", "한국어", "Ω≈ç√∫", "émoji👨‍💻", "Ⓤⓝⓘⓒⓞⓓⓔ"
        };

        private static readonly string[] SqlInjectionStrings = {
            "'; DROP TABLE users; --",
            "1' OR '1'='1",
            "admin'--",
            "1; SELECT * FROM users",
            "' UNION SELECT * FROM passwords --",
            "1' AND 1=1 --",
            "'; EXEC xp_cmdshell('dir'); --",
            "' OR 1=1 #",
            "admin\" OR \"1\"=\"1",
            "1' WAITFOR DELAY '0:0:10' --"
        };

        private static readonly string[] XssPayloads = {
            "<script>alert('XSS')</script>",
            "<img src=x onerror=alert('XSS')>",
            "javascript:alert('XSS')",
            "<svg onload=alert('XSS')>",
            "'\"><script>alert('XSS')</script>",
            "<body onload=alert('XSS')>",
            "<iframe src=\"javascript:alert('XSS')\">",
            "<input onfocus=alert('XSS') autofocus>",
            "{{constructor.constructor('alert(1)')()}}",
            "${alert('XSS')}"
        };

        private static readonly string[] PathTraversalStrings = {
            "../../../etc/passwd",
            "..\\..\\..\\windows\\system32\\config\\sam",
            "....//....//....//etc/passwd",
            "%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd",
            "..%252f..%252f..%252fetc/passwd"
        };

        private static readonly string[] SpecialCharStrings = {
            "Test\0User",           // Null byte
            "Test\rUser",           // Carriage return
            "Test\nUser",           // Newline
            "Test\tUser",           // Tab
            "Test\\User",           // Backslash
            "Test\"User",           // Quote
            "Test'User",            // Single quote
            "Test&User",            // Ampersand
            "Test<User>",           // Angle brackets
            "Test|User",            // Pipe
            "Test;User",            // Semicolon
            "Test`User",            // Backtick
            "Test$User",            // Dollar sign
            "Test%User",            // Percent
            "Test User",           // Space only (trimming test)
            "   TestUser   "        // Leading/trailing spaces
        };

        #endregion

        #region Password Data

        private static readonly string[] WeakPasswords = {
            "password", "123456", "password123", "admin", "letmein",
            "welcome", "monkey", "dragon", "master", "qwerty",
            "abc123", "111111", "iloveyou", "trustno1", "sunshine"
        };

        private static readonly string[] StrongPasswords = {
            "Tr0ub4dor&3", "P@$$w0rd!2024", "Secur3#Pass!",
            "C0mpl3x!Pwd#", "H@rdT0Gu3ss!", "Str0ng&S@fe1"
        };

        #endregion

        #region Generation Methods

        /// <summary>
        /// Generates a realistic test user
        /// </summary>
        public TestUser GenerateRealisticUser(string prefix, int number)
        {
            var firstName = FirstNames[_random.Next(FirstNames.Length)];
            var lastName = LastNames[_random.Next(LastNames.Length)];
            var department = Departments[_random.Next(Departments.Length)];
            var title = Titles[_random.Next(Titles.Length)];
            var domain = EmailDomains[_random.Next(EmailDomains.Length)];

            return new TestUser
            {
                Username = $"{prefix}{number}",
                FirstName = firstName,
                LastName = lastName,
                FullName = $"{firstName} {lastName}",
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@{domain}",
                Department = department,
                Title = $"{department} {title}",
                Phone = GeneratePhoneNumber(),
                EmployeeId = $"EMP{number:D6}",
                Password = GenerateStrongPassword(),
                Description = $"Test user {number} - {department}",
                Created = DateTime.Now
            };
        }

        /// <summary>
        /// Generates an edge case test user with unicode/special characters
        /// </summary>
        public TestUser GenerateEdgeCaseUser(string prefix, int number, EdgeCaseType type)
        {
            var user = GenerateRealisticUser(prefix, number);

            switch (type)
            {
                case EdgeCaseType.Unicode:
                    user.FirstName = UnicodeStrings[_random.Next(UnicodeStrings.Length)];
                    user.LastName = UnicodeStrings[_random.Next(UnicodeStrings.Length)];
                    break;

                case EdgeCaseType.MaxLength:
                    user.FirstName = new string('A', 255);
                    user.LastName = new string('B', 255);
                    user.Description = new string('X', 4000);
                    break;

                case EdgeCaseType.MinLength:
                    user.FirstName = "A";
                    user.LastName = "B";
                    user.Description = "";
                    break;

                case EdgeCaseType.SpecialCharacters:
                    user.FirstName = SpecialCharStrings[_random.Next(SpecialCharStrings.Length)];
                    break;

                case EdgeCaseType.Whitespace:
                    user.FirstName = "   John   ";
                    user.LastName = "\tSmith\n";
                    break;

                case EdgeCaseType.CaseSensitivity:
                    user.Username = _random.Next(2) == 0 ? user.Username.ToUpper() : user.Username.ToLower();
                    break;
            }

            user.FullName = $"{user.FirstName} {user.LastName}";
            return user;
        }

        /// <summary>
        /// Generates a negative/malicious test user for security testing
        /// </summary>
        public TestUser GenerateNegativeTestUser(string prefix, int number, NegativeTestType type)
        {
            var user = GenerateRealisticUser(prefix, number);

            switch (type)
            {
                case NegativeTestType.SqlInjection:
                    user.FirstName = SqlInjectionStrings[_random.Next(SqlInjectionStrings.Length)];
                    user.Description = SqlInjectionStrings[_random.Next(SqlInjectionStrings.Length)];
                    break;

                case NegativeTestType.XssCrossSiteScripting:
                    user.FirstName = XssPayloads[_random.Next(XssPayloads.Length)];
                    user.Description = XssPayloads[_random.Next(XssPayloads.Length)];
                    break;

                case NegativeTestType.PathTraversal:
                    user.Description = PathTraversalStrings[_random.Next(PathTraversalStrings.Length)];
                    break;

                case NegativeTestType.NullBytes:
                    user.FirstName = $"Test\0{number}";
                    user.LastName = "User\0Name";
                    break;

                case NegativeTestType.BufferOverflow:
                    user.FirstName = new string('A', 10000);
                    user.Description = new string('B', 100000);
                    break;

                case NegativeTestType.FormatString:
                    user.FirstName = "%s%s%s%s%s%s%s%s%s%s";
                    user.Description = "%n%n%n%n%n%n%n%n%n%n";
                    break;

                case NegativeTestType.CommandInjection:
                    user.Description = "; cat /etc/passwd";
                    user.FirstName = "| dir c:\\";
                    break;

                case NegativeTestType.LdapInjection:
                    user.FirstName = "*)(&(objectClass=*)";
                    user.Description = "*)(uid=*))(|(uid=*";
                    break;
            }

            user.FullName = $"{user.FirstName} {user.LastName}";
            return user;
        }

        /// <summary>
        /// Generates a user for password policy testing
        /// </summary>
        public TestUser GeneratePasswordTestUser(string prefix, int number, PasswordTestType type)
        {
            var user = GenerateRealisticUser(prefix, number);

            switch (type)
            {
                case PasswordTestType.Weak:
                    user.Password = WeakPasswords[_random.Next(WeakPasswords.Length)];
                    break;

                case PasswordTestType.Strong:
                    user.Password = StrongPasswords[_random.Next(StrongPasswords.Length)];
                    break;

                case PasswordTestType.Empty:
                    user.Password = "";
                    break;

                case PasswordTestType.MaxLength:
                    user.Password = GenerateRandomString(128, true, true, true);
                    break;

                case PasswordTestType.UnicodePassword:
                    user.Password = "Pässwörd日本語!";
                    break;

                case PasswordTestType.SpacesOnly:
                    user.Password = "     ";
                    break;

                case PasswordTestType.SameAsUsername:
                    user.Password = user.Username;
                    break;

                case PasswordTestType.CommonPatterns:
                    user.Password = "Password1!"; // Meets complexity but is predictable
                    break;

                case PasswordTestType.Expired:
                    user.PasswordExpired = true;
                    user.PasswordLastSet = DateTime.Now.AddDays(-91);
                    break;

                case PasswordTestType.NeverExpires:
                    user.PasswordNeverExpires = true;
                    break;

                case PasswordTestType.MustChange:
                    user.MustChangePassword = true;
                    break;

                case PasswordTestType.Locked:
                    user.AccountLocked = true;
                    user.LockoutTime = DateTime.Now.AddMinutes(-5);
                    user.FailedLoginAttempts = 5;
                    break;
            }

            return user;
        }

        /// <summary>
        /// Generates a user with specific role/permission level
        /// </summary>
        public TestUser GenerateRoleBasedUser(string prefix, int number, UserRole role)
        {
            var user = GenerateRealisticUser(prefix, number);
            user.Role = role;

            switch (role)
            {
                case UserRole.Admin:
                    user.Groups = new List<string> { "Administrators", "Domain Admins", "Schema Admins" };
                    user.IsAdmin = true;
                    break;

                case UserRole.PowerUser:
                    user.Groups = new List<string> { "Power Users", "Remote Desktop Users" };
                    break;

                case UserRole.StandardUser:
                    user.Groups = new List<string> { "Domain Users" };
                    break;

                case UserRole.ReadOnly:
                    user.Groups = new List<string> { "Read Only Users", "Viewers" };
                    break;

                case UserRole.ServiceAccount:
                    user.Username = $"svc_{prefix}{number}";
                    user.Groups = new List<string> { "Service Accounts" };
                    user.IsServiceAccount = true;
                    user.PasswordNeverExpires = true;
                    break;

                case UserRole.Guest:
                    user.Groups = new List<string> { "Guests" };
                    user.AccountDisabled = true;
                    break;

                case UserRole.Auditor:
                    user.Groups = new List<string> { "Auditors", "Log Viewers", "Read Only Users" };
                    break;
            }

            return user;
        }

        /// <summary>
        /// Generates batch of users for load testing
        /// </summary>
        public List<TestUser> GenerateLoadTestUsers(string prefix, int count, bool realistic = true)
        {
            var users = new List<TestUser>();
            for (int i = 1; i <= count; i++)
            {
                users.Add(realistic 
                    ? GenerateRealisticUser(prefix, i) 
                    : new TestUser { Username = $"{prefix}{i}", Password = "LoadTest123!" });
            }
            return users;
        }

        #endregion

        #region Helper Methods

        private string GeneratePhoneNumber()
        {
            return $"+1-{_random.Next(200, 999)}-{_random.Next(100, 999)}-{_random.Next(1000, 9999)}";
        }

        private string GenerateStrongPassword()
        {
            return GenerateRandomString(16, true, true, true);
        }

        private string GenerateRandomString(int length, bool includeUpper, bool includeDigits, bool includeSpecial)
        {
            var chars = "abcdefghijklmnopqrstuvwxyz";
            if (includeUpper) chars += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (includeDigits) chars += "0123456789";
            if (includeSpecial) chars += "!@#$%^&*";

            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[_random.Next(chars.Length)]).ToArray());
        }

        public string GenerateRandomEmail()
        {
            var name = FirstNames[_random.Next(FirstNames.Length)].ToLower();
            var domain = EmailDomains[_random.Next(EmailDomains.Length)];
            return $"{name}{_random.Next(1000)}@{domain}";
        }

        #endregion
    }

    #region Enums

    public enum EdgeCaseType
    {
        Unicode,
        MaxLength,
        MinLength,
        SpecialCharacters,
        Whitespace,
        CaseSensitivity
    }

    public enum NegativeTestType
    {
        SqlInjection,
        XssCrossSiteScripting,
        PathTraversal,
        NullBytes,
        BufferOverflow,
        FormatString,
        CommandInjection,
        LdapInjection
    }

    public enum PasswordTestType
    {
        Weak,
        Strong,
        Empty,
        MaxLength,
        UnicodePassword,
        SpacesOnly,
        SameAsUsername,
        CommonPatterns,
        Expired,
        NeverExpires,
        MustChange,
        Locked
    }

    public enum UserRole
    {
        Admin,
        PowerUser,
        StandardUser,
        ReadOnly,
        ServiceAccount,
        Guest,
        Auditor
    }

    #endregion

    #region Models

    /// <summary>
    /// Comprehensive test user model
    /// </summary>
    public class TestUser
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Department { get; set; } = "";
        public string Title { get; set; } = "";
        public string EmployeeId { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Created { get; set; }
        public DateTime? PasswordLastSet { get; set; }
        public DateTime? LockoutTime { get; set; }
        public bool PasswordExpired { get; set; }
        public bool PasswordNeverExpires { get; set; }
        public bool MustChangePassword { get; set; }
        public bool AccountLocked { get; set; }
        public bool AccountDisabled { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsServiceAccount { get; set; }
        public int FailedLoginAttempts { get; set; }
        public UserRole Role { get; set; } = UserRole.StandardUser;
        public List<string> Groups { get; set; } = new();
        public Dictionary<string, string> CustomAttributes { get; set; } = new();
    }

    #endregion
}

