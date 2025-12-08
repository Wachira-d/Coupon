using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// Error logging utility to replace empty catch blocks
    /// </summary>
    public static class ErrorLogger
    {
        private static readonly string LogPath = GetLogPath();
        private static readonly bool UseFixedLogFile = IsFixedLogFile();

        private static string GetLogPath()
        {
            try
            {
                string path = ConfigurationManager.AppSettings["LogPath"];
                if (string.IsNullOrEmpty(path))
                {
                    // Default to App_Data folder
                    path = HttpContext.Current?.Server.MapPath("~/App_Data/Logs") ??
                           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                }

                // Check if LogPath is a file path (has extension like .txt, .log)
                string extension = Path.GetExtension(path);
                if (!string.IsNullOrEmpty(extension))
                {
                    // It's a file path - ensure directory exists
                    string directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    return path;
                }
                else
                {
                    // It's a folder path - ensure folder exists
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                }
            }
            catch
            {
                // Fallback to temp directory if all else fails
                return Path.GetTempPath();
            }
        }

        private static bool IsFixedLogFile()
        {
            try
            {
                string path = ConfigurationManager.AppSettings["LogPath"];
                if (!string.IsNullOrEmpty(path))
                {
                    string extension = Path.GetExtension(path);
                    return !string.IsNullOrEmpty(extension);
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Log an error with context information
        /// </summary>
        public static void LogError(string message, Exception ex = null, string additionalInfo = null)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logFilePath;

                if (UseFixedLogFile)
                {
                    // Use the fixed file path from config
                    logFilePath = LogPath;
                }
                else
                {
                    // Use daily rotating log files
                    string logFileName = $"Error_{DateTime.Now:yyyyMMdd}.log";
                    logFilePath = Path.Combine(LogPath, logFileName);
                }

                string logMessage = $"[{timestamp}] ERROR: {message}\n";

                if (ex != null)
                {
                    logMessage += $"Exception Type: {ex.GetType().Name}\n";
                    logMessage += $"Message: {ex.Message}\n";
                    logMessage += $"Stack Trace: {ex.StackTrace}\n";

                    if (ex.InnerException != null)
                    {
                        logMessage += $"Inner Exception: {ex.InnerException.Message}\n";
                    }
                }

                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    logMessage += $"Additional Info: {additionalInfo}\n";
                }

                // Add request context if available
                if (HttpContext.Current != null)
                {
                    try
                    {
                        logMessage += $"URL: {HttpContext.Current.Request.Url}\n";
                        logMessage += $"User IP: {HttpContext.Current.Request.UserHostAddress}\n";
                        logMessage += $"User Agent: {HttpContext.Current.Request.UserAgent}\n";
                    }
                    catch
                    {
                        // Ignore if request context is not available
                    }
                }

                logMessage += new string('-', 80) + "\n\n";

                // Write to file
                File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {
                // Last resort: cannot even log the error
                // In production, you might want to log to Windows Event Log or other persistent storage
            }
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string message, string additionalInfo = null)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logFilePath;

                if (UseFixedLogFile)
                {
                    // Use the fixed file path from config
                    logFilePath = LogPath;
                }
                else
                {
                    // Use daily rotating log files
                    string logFileName = $"Warning_{DateTime.Now:yyyyMMdd}.log";
                    logFilePath = Path.Combine(LogPath, logFileName);
                }

                string logMessage = $"[{timestamp}] WARNING: {message}\n";

                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    logMessage += $"Details: {additionalInfo}\n";
                }

                logMessage += new string('-', 80) + "\n\n";

                File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        /// <summary>
        /// Log informational message
        /// </summary>
        public static void LogInfo(string message, string additionalInfo = null)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logFilePath;

                if (UseFixedLogFile)
                {
                    // Use the fixed file path from config
                    logFilePath = LogPath;
                }
                else
                {
                    // Use daily rotating log files
                    string logFileName = $"Info_{DateTime.Now:yyyyMMdd}.log";
                    logFilePath = Path.Combine(LogPath, logFileName);
                }

                string logMessage = $"[{timestamp}] INFO: {message}\n";

                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    logMessage += $"Details: {additionalInfo}\n";
                }

                logMessage += new string('-', 80) + "\n\n";

                File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
