using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using Coupon.Helpers;

namespace Coupon
{
    /// <summary>
    /// Global application class with comprehensive security middleware
    /// Implements security headers, rate limiting, session management, and audit logging
    /// </summary>
    public class Global : HttpApplication
    {
        // Application health status
        private static bool _applicationHealthy = false;
        private static string _healthCheckError = null;
        private static readonly object _healthLock = new object();

        /// <summary>
        /// Check if application is healthy and ready to serve requests
        /// </summary>
        public static bool IsApplicationHealthy => _applicationHealthy;
        public static string HealthCheckError => _healthCheckError;

        void Application_Start(object sender, EventArgs e)
        {
            try
            {
                // Code that runs on application startup
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);

                // SECURITY: Validate configuration on startup
                PerformStartupHealthCheck();
            }
            catch (Exception ex)
            {
                // Critical startup error - log to file only (database may not be ready)
                try
                {
                    ErrorLogger.LogToFileOnly("CRITICAL: Application startup failed", ex);
                }
                catch
                {
                    // Last resort - cannot even log
                }

                // Mark application as unhealthy
                lock (_healthLock)
                {
                    _applicationHealthy = false;
                    _healthCheckError = ex.Message;
                }
            }
        }

        /// <summary>
        /// Perform comprehensive startup health check
        /// </summary>
        private void PerformStartupHealthCheck()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Step 1: Validate configuration (file-based logging only)
                var configResult = SecureConfig.ValidateConfiguration();
                if (!configResult.IsValid)
                {
                    foreach (var error in configResult.Errors)
                    {
                        errors.Add($"Config: {error}");
                    }
                }
                warnings.AddRange(configResult.Warnings);

                // Step 2: Check critical paths
                string barcodePath = SecureConfig.GetBarcodePath();
                if (!System.IO.Directory.Exists(barcodePath))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(barcodePath);
                        warnings.Add($"Created missing barcode directory: {barcodePath}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Cannot create barcode directory: {ex.Message}");
                    }
                }

                // Step 3: Check log path
                string logPath = SecureConfig.GetLogPath();
                if (!System.IO.Directory.Exists(logPath))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(logPath);
                    }
                    catch
                    {
                        warnings.Add($"Cannot create log directory: {logPath}");
                    }
                }

                // Step 4: Test database connection (with timeout)
                try
                {
                    var dbHealthy = SecureConfig.TestDatabaseConnection(timeoutSeconds: 5);
                    if (!dbHealthy)
                    {
                        warnings.Add("Database connection test failed - some features may not work");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Database connection test error: {ex.Message}");
                }

                // Set health status
                lock (_healthLock)
                {
                    _applicationHealthy = errors.Count == 0;
                    _healthCheckError = errors.Count > 0 ? string.Join("; ", errors) : null;
                }

                // Log startup result (file only first, then database)
                string startupInfo = $"Environment: {(SecureConfig.IsProduction() ? "Production" : "Development")}\n" +
                    $"Session Timeout: {SecureConfig.GetSessionTimeoutMinutes()} minutes\n" +
                    $"Rate Limit: {SecureConfig.GetRateLimitConfig().MaxAttempts} attempts per {SecureConfig.GetRateLimitConfig().WindowMinutes} minutes\n" +
                    $"Health Status: {(_applicationHealthy ? "Healthy" : "Degraded")}";

                if (errors.Count > 0)
                {
                    startupInfo += $"\nErrors: {string.Join(", ", errors)}";
                }
                if (warnings.Count > 0)
                {
                    startupInfo += $"\nWarnings: {string.Join(", ", warnings)}";
                }

                ErrorLogger.LogToFileOnly("Application started", null, startupInfo);

                // Now try database logging (async, non-blocking)
                try
                {
                    ErrorLogger.LogInfo("Application started - Configuration validation", configResult.ToString());
                }
                catch
                {
                    // Database logging failed - continue anyway
                }

                if (!_applicationHealthy)
                {
                    ErrorLogger.LogToFileOnly("CRITICAL: Application started in DEGRADED mode", null, string.Join("; ", errors));
                }
            }
            catch (Exception ex)
            {
                lock (_healthLock)
                {
                    _applicationHealthy = false;
                    _healthCheckError = ex.Message;
                }
                ErrorLogger.LogToFileOnly("CRITICAL: Health check failed", ex);
            }
        }

        void Application_BeginRequest(object sender, EventArgs e)
        {
            try
            {
                HttpContext context = HttpContext.Current;
                if (context == null) return;

                // SECURITY: Apply security headers to all responses
                SecurityHeaders.ApplySecurityHeaders(context);

                // SECURITY: Check if IP is blocked (rate limiting / blacklist)
                string ipAddress = GetClientIpAddress(context);
                if (RateLimiter.IsBlocked(ipAddress))
                {
                    try
                    {
                        ErrorLogger.LogWarning("Blocked IP attempted access",
                            $"IP: {ipAddress}, URL: {context.Request.Url}");

                        AuditLogger.LogSecurityEvent(
                            "Blocked IP Access Attempt",
                            ipAddress,
                            $"URL: {context.Request.Url}",
                            SecuritySeverity.High
                        );
                    }
                    catch
                    {
                        // Logging failed - continue with block
                    }

                    context.Response.StatusCode = 403;
                    context.Response.StatusDescription = "Forbidden - Too Many Requests";
                    context.Response.Write("Access denied. Your IP has been temporarily blocked due to suspicious activity.");
                    context.Response.End();
                    return;
                }

                // SECURITY: Enforce HTTPS in production
                if (SecureConfig.IsProduction())
                {
                    SecurityHeaders.EnforceHttps(context);
                }

                // SECURITY: Validate session security (prevent hijacking)
                if (context.Session != null)
                {
                    try
                    {
                        if (!SessionManager.ValidateSession(context))
                        {
                            ErrorLogger.LogWarning("Potential session hijacking detected",
                                $"IP: {ipAddress}, URL: {context.Request.Url}");

                            AuditLogger.LogSecurityEvent(
                                "Session Hijacking Detected",
                                ipAddress,
                                $"URL: {context.Request.Url}",
                                SecuritySeverity.Critical
                            );

                            // Destroy session and redirect to home
                            context.Session.Clear();
                            context.Session.Abandon();
                            context.Response.Redirect("~/Central.aspx", false);
                            context.ApplicationInstance.CompleteRequest();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Session validation error - log and continue
                        ErrorLogger.LogToFileOnly("Session validation error", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ErrorLogger.LogError("Error in Application_BeginRequest", ex);
                }
                catch
                {
                    // Logging failed - continue
                }
            }
        }

        void Session_Start(object sender, EventArgs e)
        {
            try
            {
                HttpContext context = HttpContext.Current;
                if (context == null) return;

                // Check if session is available
                if (context.Session == null)
                {
                    ErrorLogger.LogToFileOnly("Session_Start: Session is null");
                    return;
                }

                // SECURITY: Initialize session with security metadata
                try
                {
                    SessionManager.InitializeSession(context);
                }
                catch (Exception sessionEx)
                {
                    ErrorLogger.LogToFileOnly("Failed to initialize session", sessionEx);
                }

                string ipAddress = GetClientIpAddress(context);

                try
                {
                    ErrorLogger.LogInfo("New session started",
                        $"IP: {ipAddress}, SessionID: {Session.SessionID}");
                }
                catch
                {
                    // Database logging failed - already logged to file
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ErrorLogger.LogToFileOnly("Error in Session_Start", ex);
                }
                catch
                {
                    // Complete failure
                }
            }
        }

        void Session_End(object sender, EventArgs e)
        {
            try
            {
                // Session ended (timeout or explicit)
                string sessionId = Session?.SessionID ?? "Unknown";
                ErrorLogger.LogToFileOnly($"Session ended: {sessionId}");
            }
            catch
            {
                // Silently fail - Session_End runs outside HTTP context
            }
        }

        void Application_Error(object sender, EventArgs e)
        {
            Exception exception = null;
            HttpContext context = null;

            try
            {
                exception = Server.GetLastError();
                context = HttpContext.Current;

                if (exception != null)
                {
                    string url = "Unknown";
                    string ipAddress = "Unknown";

                    try
                    {
                        url = context?.Request?.Url?.ToString() ?? "Unknown";
                        ipAddress = context != null ? GetClientIpAddress(context) : "Unknown";
                    }
                    catch
                    {
                        // Cannot get request info
                    }

                    // Log error
                    try
                    {
                        ErrorLogger.LogError("Unhandled application error", exception,
                            $"URL: {url}, IP: {ipAddress}");
                    }
                    catch
                    {
                        // Try file-only logging
                        try
                        {
                            ErrorLogger.LogToFileOnly("Unhandled application error", exception,
                                $"URL: {url}, IP: {ipAddress}");
                        }
                        catch
                        {
                            // Complete logging failure
                        }
                    }

                    // Log security events for specific error types
                    if (exception is HttpException httpEx)
                    {
                        int httpCode = httpEx.GetHttpCode();
                        if (httpCode == 403 || httpCode == 401)
                        {
                            try
                            {
                                AuditLogger.LogSecurityEvent(
                                    "Unauthorized Access Attempt",
                                    ipAddress,
                                    $"URL: {url}, Status: {httpCode}",
                                    SecuritySeverity.Medium
                                );
                            }
                            catch
                            {
                                // Audit logging failed
                            }
                        }
                    }

                    // Clear error to prevent default error page
                    Server.ClearError();

                    // Redirect to friendly error page
                    if (context != null)
                    {
                        try
                        {
                            string errorCode = "500";
                            if (exception is HttpException httpException)
                            {
                                errorCode = httpException.GetHttpCode().ToString();
                            }

                            // Use safe redirect
                            context.Response.Clear();
                            context.Response.StatusCode = int.Parse(errorCode);
                            context.Response.Redirect($"~/Error.aspx?code={errorCode}", false);
                            context.ApplicationInstance.CompleteRequest();
                        }
                        catch
                        {
                            // Cannot redirect - return minimal error
                            try
                            {
                                context.Response.Clear();
                                context.Response.StatusCode = 500;
                                context.Response.Write("<html><body><h1>Error</h1><p>An error occurred. Please try again later.</p></body></html>");
                                context.Response.End();
                            }
                            catch
                            {
                                // Complete failure
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Last resort logging
                try
                {
                    ErrorLogger.LogToFileOnly("Error in Application_Error handler", ex,
                        $"Original exception: {exception?.Message}");
                }
                catch
                {
                    // If even error logging fails, nothing we can do
                }
            }
        }

        void Application_End(object sender, EventArgs e)
        {
            try
            {
                // Get shutdown reason if available
                string shutdownReason = "Unknown";
                try
                {
                    var runtime = typeof(HttpRuntime).InvokeMember("_theRuntime",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetField,
                        null, null, null) as HttpRuntime;

                    if (runtime != null)
                    {
                        shutdownReason = typeof(HttpRuntime).InvokeMember("_shutDownMessage",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetField,
                            null, runtime, null)?.ToString() ?? "Unknown";
                    }
                }
                catch
                {
                    // Cannot get shutdown reason
                }

                ErrorLogger.LogToFileOnly("Application shutting down",
                    null,
                    $"Time: {DateTime.Now}, Reason: {shutdownReason}");
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Get client IP address, handling proxies
        /// </summary>
        private string GetClientIpAddress(HttpContext context)
        {
            try
            {
                // Check for forwarded IP (proxy/load balancer)
                string forwardedFor = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // Take first IP in the list
                    string[] ips = forwardedFor.Split(',');
                    if (ips.Length > 0)
                    {
                        return ips[0].Trim();
                    }
                }

                // Fallback to direct IP
                return context.Request.UserHostAddress ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
