using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// Error logging utility with failsafe mechanisms
    /// Logs to both file and database for redundancy
    /// File logging is always attempted first to prevent circular dependencies
    /// </summary>
    public static class ErrorLogger
    {
        private static readonly string LogPath = GetLogPath();
        private static readonly bool UseFixedLogFile = IsFixedLogFile();
        private static readonly object _fileLock = new object();
        private static DatabaseHelper _dbHelper = null;
        private static bool _dbInitialized = false;
        private static bool _dbAvailable = true;
        private static readonly object _dbLock = new object();

        /// <summary>
        /// Lazy initialization of database helper to prevent startup issues
        /// </summary>
        private static DatabaseHelper GetDatabaseHelper()
        {
            if (!_dbAvailable) return null;

            lock (_dbLock)
            {
                if (!_dbInitialized)
                {
                    try
                    {
                        _dbHelper = new DatabaseHelper();
                        _dbInitialized = true;
                    }
                    catch
                    {
                        _dbAvailable = false;
                        return null;
                    }
                }
                return _dbHelper;
            }
        }

        private static string GetLogPath()
        {
            try
            {
                string path = ConfigurationManager.AppSettings["LogPath"];
                if (string.IsNullOrEmpty(path))
                {
                    // Default to App_Data folder
                    path = HttpContext.Current?.Server.MapPath("~/App_Data/Logs") ??
                           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs");
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
                string tempPath = Path.Combine(Path.GetTempPath(), "CouponLogs");
                try
                {
                    if (!Directory.Exists(tempPath))
                    {
                        Directory.CreateDirectory(tempPath);
                    }
                }
                catch { }
                return tempPath;
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
        /// Log to file only - use this during startup or when database may not be available
        /// This method NEVER throws exceptions
        /// </summary>
        public static void LogToFileOnly(string message, Exception ex = null, string additionalInfo = null)
        {
            try
            {
                WriteToFile("INFO", message, ex, additionalInfo);
            }
            catch
            {
                // Complete failure - silently fail
            }
        }

        /// <summary>
        /// Write log message to file (thread-safe)
        /// </summary>
        private static void WriteToFile(string level, string message, Exception ex = null, string additionalInfo = null)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logFilePath;

                if (UseFixedLogFile)
                {
                    logFilePath = LogPath;
                }
                else
                {
                    string logFileName = $"App_{DateTime.Now:yyyyMMdd}.log";
                    logFilePath = Path.Combine(LogPath, logFileName);
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[{timestamp}] [{level}] {message}");

                if (ex != null)
                {
                    sb.AppendLine($"  Exception Type: {ex.GetType().Name}");
                    sb.AppendLine($"  Message: {ex.Message}");
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        sb.AppendLine($"  Stack Trace: {ex.StackTrace}");
                    }
                    if (ex.InnerException != null)
                    {
                        sb.AppendLine($"  Inner Exception: {ex.InnerException.Message}");
                    }
                }

                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    sb.AppendLine($"  Additional Info: {additionalInfo}");
                }

                // Add request context if available
                try
                {
                    if (HttpContext.Current != null)
                    {
                        sb.AppendLine($"  URL: {HttpContext.Current.Request.Url}");
                        sb.AppendLine($"  User IP: {HttpContext.Current.Request.UserHostAddress}");
                    }
                }
                catch { }

                sb.AppendLine(new string('-', 80));

                // Thread-safe file write with retry
                lock (_fileLock)
                {
                    int retries = 3;
                    while (retries > 0)
                    {
                        try
                        {
                            File.AppendAllText(logFilePath, sb.ToString());
                            break;
                        }
                        catch (IOException)
                        {
                            retries--;
                            if (retries > 0)
                            {
                                Thread.Sleep(50);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Complete failure - silently fail
            }
        }

        /// <summary>
        /// Write log to database (non-blocking)
        /// </summary>
        private static void LogToDatabase(string logLevel, string message, Exception ex = null, string additionalInfo = null)
        {
            if (!_dbAvailable) return;

            try
            {
                var dbHelper = GetDatabaseHelper();
                if (dbHelper == null) return;

                string requestUrl = null;
                string userIP = null;
                string userAgent = null;

                // Collect request context if available
                try
                {
                    if (HttpContext.Current != null)
                    {
                        requestUrl = HttpContext.Current.Request.Url?.ToString();
                        userIP = HttpContext.Current.Request.UserHostAddress;
                        userAgent = HttpContext.Current.Request.UserAgent;
                    }
                }
                catch { }

                // Prepare insert query
                string query = $@"INSERT INTO {dbHelper.GetTableName("ErrorLogs")}
                    ({dbHelper.GetColumnName("LogLevel")},
                     {dbHelper.GetColumnName("Message")},
                     {dbHelper.GetColumnName("ExceptionType")},
                     {dbHelper.GetColumnName("ExceptionMessage")},
                     {dbHelper.GetColumnName("StackTrace")},
                     {dbHelper.GetColumnName("InnerException")},
                     {dbHelper.GetColumnName("AdditionalInfo")},
                     {dbHelper.GetColumnName("RequestUrl")},
                     {dbHelper.GetColumnName("UserIP")},
                     {dbHelper.GetColumnName("UserAgent")},
                     {dbHelper.GetColumnName("CreatedDateTime")},
                     {dbHelper.GetColumnName("ServerName")},
                     {dbHelper.GetColumnName("ApplicationName")})
                    VALUES
                    (@logLevel, @message, @exceptionType, @exceptionMessage, @stackTrace,
                     @innerException, @additionalInfo, @requestUrl, @userIP, @userAgent,
                     @createdDateTime, @serverName, @applicationName)";

                var parameters = new[]
                {
                    dbHelper.CreateParameter("@logLevel", logLevel ?? ""),
                    dbHelper.CreateParameter("@message", TruncateString(message, 500)),
                    dbHelper.CreateParameter("@exceptionType", ex?.GetType().Name),
                    dbHelper.CreateParameter("@exceptionMessage", ex?.Message),
                    dbHelper.CreateParameter("@stackTrace", ex?.StackTrace),
                    dbHelper.CreateParameter("@innerException", ex?.InnerException?.ToString()),
                    dbHelper.CreateParameter("@additionalInfo", additionalInfo),
                    dbHelper.CreateParameter("@requestUrl", TruncateString(requestUrl, 500)),
                    dbHelper.CreateParameter("@userIP", TruncateString(userIP, 50)),
                    dbHelper.CreateParameter("@userAgent", TruncateString(userAgent, 500)),
                    dbHelper.CreateParameter("@createdDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    dbHelper.CreateParameter("@serverName", Environment.MachineName),
                    dbHelper.CreateParameter("@applicationName", "Coupon System")
                };

                dbHelper.ExecuteNonQuery(query, parameters);
            }
            catch
            {
                // Database logging failed - silently fail
                // File logging should already have the entry
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
        /// Always logs to file first, then attempts database logging
        /// </summary>
        public static void LogError(string message, Exception ex = null, string additionalInfo = null)
        {
            // Always log to file first
            WriteToFile("ERROR", message, ex, additionalInfo);

            // Then try database logging
            try
            {
                LogToDatabase("ERROR", message, ex, additionalInfo);
            }
            catch
            {
                // Database logging failed - file log is sufficient
            }
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string message, string additionalInfo = null)
        {
            // Always log to file first
            WriteToFile("WARNING", message, null, additionalInfo);

            // Then try database logging
            try
            {
                LogToDatabase("WARNING", message, null, additionalInfo);
            }
            catch
            {
                // Database logging failed - file log is sufficient
            }
        }

        /// <summary>
        /// Log informational message
        /// </summary>
        public static void LogInfo(string message, string additionalInfo = null)
        {
            // Always log to file first
            WriteToFile("INFO", message, null, additionalInfo);

            // Then try database logging
            try
            {
                LogToDatabase("INFO", message, null, additionalInfo);
            }
            catch
            {
                // Database logging failed - file log is sufficient
            }
        }

        /// <summary>
        /// Log debug message (only in non-production)
        /// </summary>
        public static void LogDebug(string message, string additionalInfo = null)
        {
            try
            {
                if (!SecureConfig.IsProduction())
                {
                    WriteToFile("DEBUG", message, null, additionalInfo);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Reset database connection state (useful after reconnection)
        /// </summary>
        public static void ResetDatabaseState()
        {
            lock (_dbLock)
            {
                _dbHelper = null;
                _dbInitialized = false;
                _dbAvailable = true;
            }
        }

        /// <summary>
        /// Check if database logging is available
        /// </summary>
        public static bool IsDatabaseLoggingAvailable()
        {
            return _dbAvailable && _dbInitialized;
        }
    }
}
