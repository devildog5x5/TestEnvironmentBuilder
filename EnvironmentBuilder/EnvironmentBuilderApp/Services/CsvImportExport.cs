// ============================================================================
// CsvImportExport.cs - CSV Import/Export Service
// Import users from CSV, export for other test tools
// Environment Builder - Test Brutally
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace EnvironmentBuilderApp.Services
{
    /// <summary>
    /// Handles CSV import and export operations
    /// </summary>
    public class CsvImportExportService
    {
        #region Export Methods

        /// <summary>
        /// Exports users to CSV file
        /// </summary>
        public void ExportUsers(List<TestUser> users, string filePath, CsvExportFormat format = CsvExportFormat.Standard)
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            switch (format)
            {
                case CsvExportFormat.Standard:
                    csv.WriteRecords(users.Select(u => new StandardCsvUser
                    {
                        Username = u.Username,
                        Password = u.Password,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        Department = u.Department,
                        Title = u.Title,
                        Phone = u.Phone,
                        EmployeeId = u.EmployeeId,
                        Groups = string.Join(";", u.Groups)
                    }));
                    break;

                case CsvExportFormat.Selenium:
                    csv.WriteRecords(users.Select(u => new SeleniumCsvUser
                    {
                        username = u.Username,
                        password = u.Password,
                        email = u.Email,
                        display_name = u.FullName
                    }));
                    break;

                case CsvExportFormat.JMeter:
                    csv.WriteRecords(users.Select(u => new JMeterCsvUser
                    {
                        USER = u.Username,
                        PASS = u.Password,
                        EMAIL = u.Email
                    }));
                    break;

                case CsvExportFormat.Postman:
                    csv.WriteRecords(users.Select(u => new PostmanCsvUser
                    {
                        key = u.Username,
                        value = u.Password,
                        email = u.Email,
                        name = u.FullName
                    }));
                    break;

                case CsvExportFormat.LoadRunner:
                    csv.WriteRecords(users.Select(u => new LoadRunnerCsvUser
                    {
                        login_name = u.Username,
                        login_password = u.Password,
                        user_email = u.Email,
                        user_fullname = u.FullName
                    }));
                    break;

                case CsvExportFormat.Credentials:
                    // Simple username:password format
                    foreach (var user in users)
                    {
                        writer.WriteLine($"{user.Username},{user.Password}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Exports to multiple formats at once
        /// </summary>
        public void ExportAllFormats(List<TestUser> users, string directory)
        {
            Directory.CreateDirectory(directory);
            
            ExportUsers(users, Path.Combine(directory, "users_standard.csv"), CsvExportFormat.Standard);
            ExportUsers(users, Path.Combine(directory, "users_selenium.csv"), CsvExportFormat.Selenium);
            ExportUsers(users, Path.Combine(directory, "users_jmeter.csv"), CsvExportFormat.JMeter);
            ExportUsers(users, Path.Combine(directory, "users_postman.csv"), CsvExportFormat.Postman);
            ExportUsers(users, Path.Combine(directory, "users_credentials.csv"), CsvExportFormat.Credentials);
        }

        /// <summary>
        /// Exports environment configuration for automation
        /// </summary>
        public void ExportEnvironmentConfig(EnvironmentExportConfig config, string filePath)
        {
            var lines = new List<string>
            {
                "# Environment Builder Configuration Export",
                $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "[Server]",
                $"Address={config.ServerAddress}",
                $"Port={config.Port}",
                $"BaseDN={config.BaseDN}",
                $"UseSSL={config.UseSSL}",
                "",
                "[Users]",
                $"Prefix={config.UserPrefix}",
                $"StartNumber={config.StartNumber}",
                $"EndNumber={config.EndNumber}",
                $"ContainerDN={config.UserContainerDN}",
                $"DefaultPassword={config.DefaultPassword}",
                "",
                "[Settings]",
                $"TotalUsers={config.EndNumber - config.StartNumber + 1}",
                $"Preset={config.Preset}"
            };

            File.WriteAllLines(filePath, lines);
        }

        #endregion

        #region Import Methods

        /// <summary>
        /// Imports users from CSV file
        /// </summary>
        public List<TestUser> ImportUsers(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            });

            var users = new List<TestUser>();
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                var dict = (IDictionary<string, object>)record;
                var user = new TestUser
                {
                    Username = GetValue(dict, "Username", "username", "user", "login"),
                    Password = GetValue(dict, "Password", "password", "pass", "pwd"),
                    FirstName = GetValue(dict, "FirstName", "firstname", "first_name", "givenName"),
                    LastName = GetValue(dict, "LastName", "lastname", "last_name", "sn", "surname"),
                    Email = GetValue(dict, "Email", "email", "mail", "emailAddress"),
                    Department = GetValue(dict, "Department", "department", "dept"),
                    Title = GetValue(dict, "Title", "title", "jobTitle"),
                    Phone = GetValue(dict, "Phone", "phone", "telephone", "mobile"),
                    EmployeeId = GetValue(dict, "EmployeeId", "employeeid", "employee_id", "empId"),
                    Description = GetValue(dict, "Description", "description", "desc")
                };

                // Handle groups (semicolon or comma separated)
                var groups = GetValue(dict, "Groups", "groups", "memberOf");
                if (!string.IsNullOrEmpty(groups))
                {
                    user.Groups = groups.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(g => g.Trim()).ToList();
                }

                user.FullName = $"{user.FirstName} {user.LastName}".Trim();
                users.Add(user);
            }

            return users;
        }

        /// <summary>
        /// Validates CSV file format
        /// </summary>
        public CsvValidationResult ValidateCsvFile(string filePath)
        {
            var result = new CsvValidationResult { FilePath = filePath };

            if (!File.Exists(filePath))
            {
                result.IsValid = false;
                result.Errors.Add("File does not exist");
                return result;
            }

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                csv.Read();
                csv.ReadHeader();
                
                var headers = csv.HeaderRecord;
                result.Headers = headers?.ToList() ?? new List<string>();
                result.HasUsernameColumn = headers?.Any(h => 
                    h.Equals("Username", StringComparison.OrdinalIgnoreCase) ||
                    h.Equals("user", StringComparison.OrdinalIgnoreCase) ||
                    h.Equals("login", StringComparison.OrdinalIgnoreCase)) ?? false;

                if (!result.HasUsernameColumn)
                {
                    result.Warnings.Add("No obvious username column found. Will try to auto-detect.");
                }

                // Count records
                int count = 0;
                while (csv.Read()) count++;
                result.RecordCount = count;
                
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Parse error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Generates a sample CSV template
        /// </summary>
        public void GenerateSampleTemplate(string filePath)
        {
            var template = @"Username,Password,FirstName,LastName,Email,Department,Title,Phone,EmployeeId,Groups
testuser1,P@ssw0rd123,John,Smith,john.smith@company.com,Engineering,Developer,555-0101,EMP001,""Users;Developers""
testuser2,P@ssw0rd123,Jane,Doe,jane.doe@company.com,Marketing,Manager,555-0102,EMP002,""Users;Marketing""
testuser3,P@ssw0rd123,Bob,Johnson,bob.johnson@company.com,Sales,Analyst,555-0103,EMP003,""Users;Sales""";

            File.WriteAllText(filePath, template);
        }

        #endregion

        #region Helper Methods

        private string GetValue(IDictionary<string, object> dict, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                var match = dict.Keys.FirstOrDefault(k => 
                    k.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (match != null && dict[match] != null)
                    return dict[match].ToString() ?? "";
            }
            return "";
        }

        #endregion
    }

    #region Enums and Models

    public enum CsvExportFormat
    {
        Standard,       // Full user details
        Selenium,       // Selenium-friendly format
        JMeter,         // JMeter CSV Data Set Config format
        Postman,        // Postman collection format
        LoadRunner,     // LoadRunner format
        Credentials     // Simple username,password
    }

    public class StandardCsvUser
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string Title { get; set; } = "";
        public string Phone { get; set; } = "";
        public string EmployeeId { get; set; } = "";
        public string Groups { get; set; } = "";
    }

    public class SeleniumCsvUser
    {
        public string username { get; set; } = "";
        public string password { get; set; } = "";
        public string email { get; set; } = "";
        public string display_name { get; set; } = "";
    }

    public class JMeterCsvUser
    {
        public string USER { get; set; } = "";
        public string PASS { get; set; } = "";
        public string EMAIL { get; set; } = "";
    }

    public class PostmanCsvUser
    {
        public string key { get; set; } = "";
        public string value { get; set; } = "";
        public string email { get; set; } = "";
        public string name { get; set; } = "";
    }

    public class LoadRunnerCsvUser
    {
        public string login_name { get; set; } = "";
        public string login_password { get; set; } = "";
        public string user_email { get; set; } = "";
        public string user_fullname { get; set; } = "";
    }

    public class EnvironmentExportConfig
    {
        public string ServerAddress { get; set; } = "";
        public string Port { get; set; } = "";
        public string BaseDN { get; set; } = "";
        public bool UseSSL { get; set; }
        public string UserPrefix { get; set; } = "";
        public int StartNumber { get; set; }
        public int EndNumber { get; set; }
        public string UserContainerDN { get; set; } = "";
        public string DefaultPassword { get; set; } = "";
        public string Preset { get; set; } = "";
    }

    public class CsvValidationResult
    {
        public string FilePath { get; set; } = "";
        public bool IsValid { get; set; }
        public int RecordCount { get; set; }
        public List<string> Headers { get; set; } = new();
        public bool HasUsernameColumn { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    #endregion
}

