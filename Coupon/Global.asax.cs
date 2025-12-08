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
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // SECURITY: Validate configuration on startup
            try
            {
                var configResult = SecureConfig.ValidateConfiguration();
                ErrorLogger.LogInfo("Application started - Configuration validation", configResult.ToString());

                if (!configResult.IsValid)
                {
                    ErrorLogger.LogError("CRITICAL: Configuration validation failed", null, configResult.ToString());
                }

                // Log security settings
                ErrorLogger.LogInfo("Security Configuration",
                    $"Environment: {(SecureConfig.IsProduction() ? "Production" : "Development")}\n" +
                    $"Session Timeout: {SecureConfig.GetSessionTimeoutMinutes()} minutes\n" +
                    $"Rate Limit: {SecureConfig.GetRateLimitConfig().MaxAttempts} attempts per {SecureConfig.GetRateLimitConfig().WindowMinutes} minutes");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error during application startup validation", ex);
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
                string ipAddress = context.Request.UserHostAddress;
                if (RateLimiter.IsBlocked(ipAddress))
                {
                    ErrorLogger.LogWarning("Blocked IP attempted access",
                        $"IP: {ipAddress}, URL: {context.Request.Url}");

                    AuditLogger.LogSecurityEvent(
                        "Blocked IP Access Attempt",
                        ipAddress,
                        $"URL: {context.Request.Url}",
                        SecuritySeverity.High
                    );

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
                        context.Response.Redirect("~/Default.aspx", true);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in Application_BeginRequest", ex);
            }
        }

        void Session_Start(object sender, EventArgs e)
        {
            try
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    // SECURITY: Initialize session with security metadata
                    SessionManager.InitializeSession(context);

                    string ipAddress = context.Request.UserHostAddress;
                    ErrorLogger.LogInfo("New session started",
                        $"IP: {ipAddress}, SessionID: {Session.SessionID}");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in Session_Start", ex);
            }
        }

        void Session_End(object sender, EventArgs e)
        {
            try
            {
                // Session ended (timeout or explicit)
                ErrorLogger.LogInfo("Session ended", $"SessionID: {Session.SessionID}");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in Session_End", ex);
            }
        }

        void Application_Error(object sender, EventArgs e)
        {
            try
            {
                Exception exception = Server.GetLastError();
                HttpContext context = HttpContext.Current;

                if (exception != null && context != null)
                {
                    string url = context.Request?.Url?.ToString() ?? "Unknown";
                    string ipAddress = context.Request?.UserHostAddress ?? "Unknown";

                    ErrorLogger.LogError("Unhandled application error", exception,
                        $"URL: {url}, IP: {ipAddress}");

                    // Log security events for specific error types
                    if (exception is HttpException httpEx)
                    {
                        if (httpEx.GetHttpCode() == 403 || httpEx.GetHttpCode() == 401)
                        {
                            AuditLogger.LogSecurityEvent(
                                "Unauthorized Access Attempt",
                                ipAddress,
                                $"URL: {url}, Status: {httpEx.GetHttpCode()}",
                                SecuritySeverity.Medium
                            );
                        }
                    }

                    // Clear error to prevent default error page
                    Server.ClearError();

                    // Redirect to friendly error page in production
                    if (SecureConfig.IsProduction())
                    {
                        context.Response.Redirect("~/Error.aspx", false);
                    }
                }
            }
            catch (Exception ex)
            {
                // Last resort logging
                try
                {
                    ErrorLogger.LogError("Error in Application_Error handler", ex);
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
                ErrorLogger.LogInfo("Application shutting down", $"Time: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in Application_End", ex);
            }
        }
    }
}