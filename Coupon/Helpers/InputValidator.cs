using System;
using System.Text.RegularExpressions;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// Input validation and sanitization helper to prevent injection attacks
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Validate coupon ID format (alphanumeric only)
        /// SECURITY: Prevents SQL injection and path traversal attacks
        /// </summary>
        public static bool IsValidCouponId(string id, int expectedLength = 4)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            // Allow only alphanumeric characters
            if (!Regex.IsMatch(id, @"^[a-zA-Z0-9]+$"))
            {
                return false;
            }

            // Check length if specified
            if (expectedLength > 0 && id.Length != expectedLength)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sanitize input by removing potentially dangerous characters
        /// </summary>
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // HTML encode to prevent XSS
            string sanitized = HttpUtility.HtmlEncode(input);

            // Remove SQL injection characters
            sanitized = sanitized.Replace("'", "");
            sanitized = sanitized.Replace("--", "");
            sanitized = sanitized.Replace(";", "");
            sanitized = sanitized.Replace("/*", "");
            sanitized = sanitized.Replace("*/", "");
            sanitized = sanitized.Replace("xp_", "");
            sanitized = sanitized.Replace("sp_", "");

            return sanitized;
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                // Simple email validation pattern
                var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate date format
        /// </summary>
        public static bool IsValidDate(string dateString, out DateTime date)
        {
            return DateTime.TryParse(dateString, out date);
        }

        /// <summary>
        /// Validate IP address format
        /// </summary>
        public static bool IsValidIPAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            // IPv4 pattern
            var ipPattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            return Regex.IsMatch(ip, ipPattern);
        }

        /// <summary>
        /// Get safe query string parameter
        /// SECURITY: Returns null if validation fails
        /// </summary>
        public static string GetSafeQueryStringParameter(HttpRequest request, string parameterName, int maxLength = 50)
        {
            try
            {
                string value = request.QueryString[parameterName];

                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                // FEATURE: Strip leading slashes from parameter values
                // Fix for /Central.aspx?id=/ABC123 -> /Central.aspx?id=ABC123
                value = value.TrimStart('/');

                // Limit length to prevent buffer overflow
                if (value.Length > maxLength)
                {
                    ErrorLogger.LogWarning($"Query parameter '{parameterName}' exceeds max length", $"Value length: {value.Length}");
                    return null;
                }

                return value;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error getting query parameter '{parameterName}'", ex);
                return null;
            }
        }

        /// <summary>
        /// Prevent path traversal attacks in file paths
        /// </summary>
        public static bool IsSafeFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            // Check for path traversal patterns
            if (filePath.Contains("..") ||
                filePath.Contains("~") ||
                filePath.Contains("//") ||
                filePath.Contains("\\\\"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate numeric input
        /// </summary>
        public static bool IsValidNumeric(string input, out int number)
        {
            return int.TryParse(input, out number);
        }

        /// <summary>
        /// Validate boolean input
        /// </summary>
        public static bool IsValidBoolean(string input, out bool value)
        {
            value = false;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string normalized = input.ToLower().Trim();

            if (normalized == "true" || normalized == "1" || normalized == "yes")
            {
                value = true;
                return true;
            }

            if (normalized == "false" || normalized == "0" || normalized == "no")
            {
                value = false;
                return true;
            }

            return false;
        }
    }
}
