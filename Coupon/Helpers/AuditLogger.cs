using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// Audit logging for security-critical events and admin actions
    /// Provides tamper-evident logging with detailed context
    /// </summary>
    public static class AuditLogger
    {
        private static readonly string AuditLogPath = GetAuditLogPath();
        private static readonly object _lock = new object();

        private static string GetAuditLogPath()
        {
            try
            {
                string path = ConfigurationManager.AppSettings["AuditLogPath"];
                if (string.IsNullOrEmpty(path))
                {
                    path = HttpContext.Current?.Server.MapPath("~/App_Data/AuditLogs") ??
                           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AuditLogs");
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
            catch
            {
                return Path.GetTempPath();
            }
        }

        /// <summary>
        /// Log authentication event
        /// </summary>
        public static void LogAuthentication(string userEmail, bool success, string ipAddress, string details = null)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.Authentication,
                Action = success ? "Login Success" : "Login Failed",
                UserEmail = userEmail,
                IPAddress = ipAddress,
                Success = success,
                Details = details
            });
        }

        /// <summary>
        /// Log authorization event (access attempt)
        /// </summary>
        public static void LogAuthorization(string userEmail, string resource, bool granted, string ipAddress, string details = null)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.Authorization,
                Action = granted ? "Access Granted" : "Access Denied",
                UserEmail = userEmail,
                IPAddress = ipAddress,
                Resource = resource,
                Success = granted,
                Details = details
            });
        }

        /// <summary>
        /// Log data access event
        /// </summary>
        public static void LogDataAccess(string userEmail, string resource, string action, string ipAddress, string details = null)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.DataAccess,
                Action = action,
                UserEmail = userEmail,
                IPAddress = ipAddress,
                Resource = resource,
                Success = true,
                Details = details
            });
        }

        /// <summary>
        /// Log data modification event
        /// </summary>
        public static void LogDataModification(string userEmail, string resource, string action, string oldValue, string newValue, string ipAddress)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.DataModification,
                Action = action,
                UserEmail = userEmail,
                IPAddress = ipAddress,
                Resource = resource,
                Success = true,
                Details = $"Changed from: {oldValue} to: {newValue}"
            });
        }

        /// <summary>
        /// Log configuration change
        /// </summary>
        public static void LogConfigurationChange(string userEmail, string setting, string oldValue, string newValue, string ipAddress)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.ConfigurationChange,
                Action = $"Configuration Change: {setting}",
                UserEmail = userEmail,
                IPAddress = ipAddress,
                Resource = setting,
                Success = true,
                Details = $"Old: {oldValue}, New: {newValue}"
            });
        }

        /// <summary>
        /// Log security event (e.g., rate limit exceeded, suspicious activity)
        /// </summary>
        public static void LogSecurityEvent(string eventName, string ipAddress, string details, SecuritySeverity severity = SecuritySeverity.Medium)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.SecurityEvent,
                Action = eventName,
                IPAddress = ipAddress,
                Success = false,
                Details = $"[{severity}] {details}",
                Severity = severity
            });
        }

        /// <summary>
        /// Log coupon redemption
        /// </summary>
        public static void LogCouponRedemption(string couponId, string brand, int value, string ipAddress)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.CouponRedemption,
                Action = "Coupon Redeemed",
                IPAddress = ipAddress,
                Resource = couponId,
                Success = true,
                Details = $"Brand: {brand}, Value: {value} baht"
            });
        }

        /// <summary>
        /// Log coupon access attempt
        /// </summary>
        public static void LogCouponAccess(string couponId, bool found, string ipAddress, string reason = null)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.CouponAccess,
                Action = found ? "Coupon Found" : "Coupon Not Found",
                IPAddress = ipAddress,
                Resource = couponId,
                Success = found,
                Details = reason
            });
        }

        /// <summary>
        /// Log admin action
        /// </summary>
        public static void LogAdminAction(string userEmail, string action, string resource, string ipAddress, string details = null)
        {
            LogAuditEvent(new AuditEvent
            {
                EventType = AuditEventType.AdminAction,
                Action = action,
                UserEmail = userEmail,
                IPAddress = ipAddress,
                Resource = resource,
                Success = true,
                Details = details
            });
        }

        /// <summary>
        /// Core audit event logging
        /// </summary>
        private static void LogAuditEvent(AuditEvent auditEvent)
        {
            lock (_lock)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logFileName = $"Audit_{DateTime.Now:yyyyMMdd}.log";
                    string logFilePath = Path.Combine(AuditLogPath, logFileName);

                    // Build log entry
                    var logEntry = new System.Text.StringBuilder();
                    logEntry.AppendLine($"[{timestamp}] [{auditEvent.EventType}] [{auditEvent.Action}]");

                    if (!string.IsNullOrEmpty(auditEvent.UserEmail))
                    {
                        logEntry.AppendLine($"  User: {auditEvent.UserEmail}");
                    }

                    if (!string.IsNullOrEmpty(auditEvent.IPAddress))
                    {
                        logEntry.AppendLine($"  IP Address: {auditEvent.IPAddress}");
                    }

                    if (!string.IsNullOrEmpty(auditEvent.Resource))
                    {
                        logEntry.AppendLine($"  Resource: {auditEvent.Resource}");
                    }

                    logEntry.AppendLine($"  Success: {auditEvent.Success}");

                    if (auditEvent.Severity.HasValue)
                    {
                        logEntry.AppendLine($"  Severity: {auditEvent.Severity}");
                    }

                    if (!string.IsNullOrEmpty(auditEvent.Details))
                    {
                        logEntry.AppendLine($"  Details: {auditEvent.Details}");
                    }

                    // Add request context if available
                    if (HttpContext.Current != null)
                    {
                        try
                        {
                            logEntry.AppendLine($"  URL: {HttpContext.Current.Request.Url}");
                            logEntry.AppendLine($"  User Agent: {HttpContext.Current.Request.UserAgent}");
                        }
                        catch
                        {
                            // Ignore if request context not available
                        }
                    }

                    logEntry.AppendLine(new string('-', 80));
                    logEntry.AppendLine();

                    // Write to file
                    File.AppendAllText(logFilePath, logEntry.ToString());

                    // Also log high-severity security events to error log
                    if (auditEvent.EventType == AuditEventType.SecurityEvent &&
                        (auditEvent.Severity == SecuritySeverity.High || auditEvent.Severity == SecuritySeverity.Critical))
                    {
                        ErrorLogger.LogWarning($"[AUDIT] {auditEvent.Action}", auditEvent.Details);
                    }
                }
                catch (Exception ex)
                {
                    // Fallback to error logger
                    ErrorLogger.LogError("Failed to write audit log", ex, $"Event: {auditEvent.EventType} - {auditEvent.Action}");
                }
            }
        }

        /// <summary>
        /// Get audit log statistics
        /// </summary>
        public static AuditStats GetStats(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var stats = new AuditStats
                {
                    FromDate = fromDate ?? DateTime.Now.AddDays(-30),
                    ToDate = toDate ?? DateTime.Now
                };

                // Count log files in date range
                var files = Directory.GetFiles(AuditLogPath, "Audit_*.log");
                stats.TotalLogFiles = files.Length;

                // Calculate total size
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    stats.TotalSizeBytes += fileInfo.Length;
                }

                return stats;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error getting audit stats", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Audit event types
    /// </summary>
    public enum AuditEventType
    {
        Authentication,
        Authorization,
        DataAccess,
        DataModification,
        ConfigurationChange,
        SecurityEvent,
        CouponRedemption,
        CouponAccess,
        AdminAction
    }

    /// <summary>
    /// Security event severity levels
    /// </summary>
    public enum SecuritySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Audit event data
    /// </summary>
    internal class AuditEvent
    {
        public AuditEventType EventType { get; set; }
        public string Action { get; set; }
        public string UserEmail { get; set; }
        public string IPAddress { get; set; }
        public string Resource { get; set; }
        public bool Success { get; set; }
        public string Details { get; set; }
        public SecuritySeverity? Severity { get; set; }
    }

    /// <summary>
    /// Audit statistics
    /// </summary>
    public class AuditStats
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalLogFiles { get; set; }
        public long TotalSizeBytes { get; set; }

        public double TotalSizeMB => TotalSizeBytes / (1024.0 * 1024.0);
    }
}
