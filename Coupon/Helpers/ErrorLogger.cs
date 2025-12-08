using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// Error logging utility to replace empty catch blocks
    /// Logs to both file and database for redundancy
    /// </summary>
    public static class ErrorLogger
    {
        private static readonly string LogPath = GetLogPath();
        private static readonly bool UseFixedLogFile = IsFixedLogFile();
        private static readonly DatabaseHelper _dbHelper = new DatabaseHelper();

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
        /// Write log to database
        /// IMPORTANT: This method should never throw exceptions to prevent logging failures from breaking the application
        /// </summary>
        private static void LogToDatabase(string logLevel, string message, Exception ex = null, string additionalInfo = null)
        {
            try
            {
                string requestUrl = null;
                string userIP = null;
                string userAgent = null;

                // Collect request context if available
                if (HttpContext.Current != null)
                {
                    try
                    {
                        requestUrl = HttpContext.Current.Request.Url?.ToString();
                        userIP = HttpContext.Current.Request.UserHostAddress;
                        userAgent = HttpContext.Current.Request.UserAgent;
                    }
                    catch
                    {
                        // Ignore if request context is not available
                    }
                }

                // Prepare insert query
                string query = $@"INSERT INTO {_dbHelper.GetTableName("ErrorLogs")}
                    ({_dbHelper.GetColumnName("LogLevel")},
                     {_dbHelper.GetColumnName("Message")},
                     {_dbHelper.GetColumnName("ExceptionType")},
                     {_dbHelper.GetColumnName("ExceptionMessage")},
                     {_dbHelper.GetColumnName("StackTrace")},
                     {_dbHelper.GetColumnName("InnerException")},
                     {_dbHelper.GetColumnName("AdditionalInfo")},
                     {_dbHelper.GetColumnName("RequestUrl")},
                     {_dbHelper.GetColumnName("UserIP")},
                     {_dbHelper.GetColumnName("UserAgent")},
                     {_dbHelper.GetColumnName("CreatedDateTime")},
                     {_dbHelper.GetColumnName("ServerName")},
                     {_dbHelper.GetColumnName("ApplicationName")})
                    VALUES
                    (@logLevel, @message, @exceptionType, @exceptionMessage, @stackTrace,
                     @innerException, @additionalInfo, @requestUrl, @userIP, @userAgent,
                     @createdDateTime, @serverName, @applicationName)";

                var parameters = new[]
                {
                    _dbHelper.CreateParameter("@logLevel", logLevel ?? ""),
                    _dbHelper.CreateParameter("@message", TruncateString(message, 500)),
                    _dbHelper.CreateParameter("@exceptionType", ex?.GetType().Name),
                    _dbHelper.CreateParameter("@exceptionMessage", ex?.Message),
                    _dbHelper.CreateParameter("@stackTrace", ex?.StackTrace),
                    _dbHelper.CreateParameter("@innerException", ex?.InnerException?.ToString()),
                    _dbHelper.CreateParameter("@additionalInfo", additionalInfo),
                    _dbHelper.CreateParameter("@requestUrl", TruncateString(requestUrl, 500)),
                    _dbHelper.CreateParameter("@userIP", TruncateString(userIP, 50)),
                    _dbHelper.CreateParameter("@userAgent", TruncateString(userAgent, 500)),
                    _dbHelper.CreateParameter("@createdDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    _dbHelper.CreateParameter("@serverName", Environment.MachineName),
                    _dbHelper.CreateParameter("@applicationName", "Coupon System")
                };

                _dbHelper.ExecuteNonQuery(query, parameters);
            }
            catch
            {
                // CRITICAL: Never throw exception from logging method
                // If database logging fails, at least file logging should work
                // Do nothing - fail silently to prevent infinite loops
            }
        }

        /// <summary>
        /// Truncate string to specified length to prevent database errors
        /// </summary>
        private static string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength - 3) + "...";
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

                // ALSO write to database
                LogToDatabase("ERROR", message, ex, additionalInfo);
            }
            catch
            {
                // Last resort: cannot even log the error
                // Try database logging as fallback
                try
                {
                    LogToDatabase("ERROR", message, ex, additionalInfo);
                }
                catch
                {
                    // Complete failure - give up silently to prevent infinite loops
                }
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

                // ALSO write to database
                LogToDatabase("WARNING", message, null, additionalInfo);
            }
            catch
            {
                // Try database logging as fallback
                try
                {
                    LogToDatabase("WARNING", message, null, additionalInfo);
                }
                catch
                {
                    // Complete failure - give up silently
                }
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

                // ALSO write to database
                LogToDatabase("INFO", message, null, additionalInfo);
            }
            catch
            {
                // Try database logging as fallback
                try
                {
                    LogToDatabase("INFO", message, null, additionalInfo);
                }
                catch
                {
                    // Complete failure - give up silently
                }
            }
        }
    }
}
