using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Coupon.Helpers
{
    /// <summary>
    /// Secure configuration management
    /// Provides encrypted configuration values and environment-based config
    /// </summary>
    public static class SecureConfig
    {
        // Encryption key - CHANGE THIS IN PRODUCTION!
        // Better: Store in Azure Key Vault or AWS Secrets Manager
        private static readonly byte[] ENCRYPTION_KEY = Encoding.UTF8.GetBytes("YourSecureKey32BytesLongString!");
        private static readonly byte[] ENCRYPTION_IV = Encoding.UTF8.GetBytes("YourSecureIV16By");

        /// <summary>
        /// Get configuration value with fallback to environment variable
        /// Priority: Environment Variable > Web.config
        /// </summary>
        public static string GetConfigValue(string key, string defaultValue = null)
        {
            try
            {
                // First, try environment variable
                string envValue = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(envValue))
                {
                    return envValue;
                }

                // Then, try app settings
                string appValue = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrEmpty(appValue))
                {
                    return appValue;
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error getting config value: {key}", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// Get database connection string with environment variable support
        /// </summary>
        public static string GetConnectionString(string databaseType = null)
        {
            try
            {
                // Determine database type
                if (string.IsNullOrEmpty(databaseType))
                {
                    databaseType = GetConfigValue("DatabaseType", "PostgreSQL");
                }

                string key = databaseType.ToLower() == "postgresql"
                    ? "PostgreSQLConnectionString"
                    : "MSSQLConnectionString";

                // Check environment variable first
                string envKey = $"DB_CONNECTION_{databaseType.ToUpper()}";
                string connectionString = Environment.GetEnvironmentVariable(envKey);

                if (string.IsNullOrEmpty(connectionString))
                {
                    // Fallback to app settings
                    connectionString = ConfigurationManager.AppSettings[key];
                }

                // Log warning if using hardcoded connection string
                if (Environment.GetEnvironmentVariable(envKey) == null)
                {
                    ErrorLogger.LogWarning("Using hardcoded connection string",
                        $"Consider moving {key} to environment variable: {envKey}");
                }

                return connectionString;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error getting connection string", ex);
                return null;
            }
        }

        /// <summary>
        /// Get LDAP server address
        /// </summary>
        public static string GetLdapServer()
        {
            return GetConfigValue("LDAP") ?? GetConfigValue("LDAP_SERVER");
        }

        /// <summary>
        /// Get authorized admin emails
        /// </summary>
        public static string[] GetAuthorizedEmails()
        {
            try
            {
                string emailList = GetConfigValue("ReportPassword") ?? GetConfigValue("AUTHORIZED_EMAILS");
                if (string.IsNullOrEmpty(emailList))
                {
                    return new string[0];
                }

                return emailList.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error getting authorized emails", ex);
                return new string[0];
            }
        }

        /// <summary>
        /// Encrypt sensitive configuration value
        /// </summary>
        public static string EncryptValue(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = ENCRYPTION_KEY;
                    aes.IV = ENCRYPTION_IV;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error encrypting value", ex);
                return null;
            }
        }

        /// <summary>
        /// Decrypt encrypted configuration value
        /// </summary>
        public static string DecryptValue(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }

            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = ENCRYPTION_KEY;
                    aes.IV = ENCRYPTION_IV;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(buffer))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error decrypting value", ex);
                return null;
            }
        }

        /// <summary>
        /// Get barcode storage path
        /// </summary>
        public static string GetBarcodePath()
        {
            return GetConfigValue("Path") ?? GetConfigValue("BARCODE_PATH") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Barcode");
        }

        /// <summary>
        /// Get log path
        /// </summary>
        public static string GetLogPath()
        {
            return GetConfigValue("LogPath") ?? GetConfigValue("LOG_PATH") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        }

        /// <summary>
        /// Get audit log path
        /// </summary>
        public static string GetAuditLogPath()
        {
            return GetConfigValue("AuditLogPath") ?? GetConfigValue("AUDIT_LOG_PATH") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AuditLogs");
        }

        /// <summary>
        /// Get coupon ID length
        /// </summary>
        public static int GetCouponIdLength()
        {
            string value = GetConfigValue("NumberofCode", "4");
            return int.TryParse(value, out int length) ? length : 4;
        }

        /// <summary>
        /// Get rate limit configuration
        /// </summary>
        public static RateLimitConfig GetRateLimitConfig()
        {
            return new RateLimitConfig
            {
                MaxAttempts = int.TryParse(GetConfigValue("RateLimit_MaxAttempts", "10"), out int max) ? max : 10,
                WindowMinutes = int.TryParse(GetConfigValue("RateLimit_WindowMinutes", "60"), out int window) ? window : 60
            };
        }

        /// <summary>
        /// Get session timeout in minutes
        /// </summary>
        public static int GetSessionTimeoutMinutes()
        {
            string value = GetConfigValue("SessionTimeoutMinutes", "30");
            return int.TryParse(value, out int timeout) ? timeout : 30;
        }

        /// <summary>
        /// Check if running in production environment
        /// </summary>
        public static bool IsProduction()
        {
            string env = GetConfigValue("ASPNETCORE_ENVIRONMENT") ?? GetConfigValue("Environment");
            return string.IsNullOrEmpty(env) || env.ToLower() == "production";
        }

        /// <summary>
        /// Check if debug mode is enabled
        /// </summary>
        public static bool IsDebugMode()
        {
            return !IsProduction();
        }

        /// <summary>
        /// Validate configuration on application start
        /// </summary>
        public static ConfigValidationResult ValidateConfiguration()
        {
            var result = new ConfigValidationResult();

            try
            {
                // Check database connection string
                string connString = GetConnectionString();
                if (string.IsNullOrEmpty(connString))
                {
                    result.Errors.Add("Database connection string is missing");
                }
                else if (connString.Contains("Password=") && Environment.GetEnvironmentVariable("DB_CONNECTION_POSTGRESQL") == null)
                {
                    result.Warnings.Add("Database credentials are hardcoded in Web.config. Consider using environment variables.");
                }

                // Check LDAP server
                string ldap = GetLdapServer();
                if (string.IsNullOrEmpty(ldap))
                {
                    result.Warnings.Add("LDAP server is not configured");
                }

                // Check authorized emails
                var emails = GetAuthorizedEmails();
                if (emails.Length == 0)
                {
                    result.Warnings.Add("No authorized admin emails configured");
                }

                // Check paths exist
                string barcodePath = GetBarcodePath();
                if (!Directory.Exists(barcodePath))
                {
                    result.Warnings.Add($"Barcode path does not exist: {barcodePath}");
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Configuration validation error: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Rate limit configuration
    /// </summary>
    public class RateLimitConfig
    {
        public int MaxAttempts { get; set; }
        public int WindowMinutes { get; set; }
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigValidationResult
    {
        public bool IsValid { get; set; }
        public System.Collections.Generic.List<string> Errors { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> Warnings { get; set; } = new System.Collections.Generic.List<string>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Configuration Valid: {IsValid}");

            if (Errors.Count > 0)
            {
                sb.AppendLine("Errors:");
                foreach (var error in Errors)
                {
                    sb.AppendLine($"  - {error}");
                }
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine("Warnings:");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  - {warning}");
                }
            }

            return sb.ToString();
        }
    }
}
