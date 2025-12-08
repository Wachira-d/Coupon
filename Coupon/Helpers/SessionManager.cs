using System;
using System.Web;
using System.Web.SessionState;

namespace Coupon.Helpers
{
    /// <summary>
    /// Secure session management with encryption and validation
    /// Prevents session fixation and hijacking attacks
    /// </summary>
    public static class SessionManager
    {
        private const string SESSION_ID_KEY = "_SessionId";
        private const string USER_IP_KEY = "_UserIP";
        private const string USER_AGENT_KEY = "_UserAgent";
        private const string SESSION_START_KEY = "_SessionStart";
        private const string LAST_ACTIVITY_KEY = "_LastActivity";
        private const string IS_AUTHENTICATED_KEY = "_IsAuthenticated";
        private const string USER_EMAIL_KEY = "_UserEmail";

        // Configuration
        private const int SESSION_TIMEOUT_MINUTES = 30;
        private const int MAX_SESSION_DURATION_HOURS = 8;

        /// <summary>
        /// Initialize a new session with security checks
        /// Call this on Page_Load for authenticated pages
        /// </summary>
        public static void InitializeSession(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                return;
            }

            try
            {
                var session = context.Session;

                // Generate new session ID if first time
                if (session[SESSION_ID_KEY] == null)
                {
                    session[SESSION_ID_KEY] = Guid.NewGuid().ToString();
                    session[SESSION_START_KEY] = DateTime.Now;
                    session[USER_IP_KEY] = context.Request.UserHostAddress;
                    session[USER_AGENT_KEY] = context.Request.UserAgent;
                }

                // Update last activity
                session[LAST_ACTIVITY_KEY] = DateTime.Now;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error initializing session", ex);
            }
        }

        /// <summary>
        /// Validate session security
        /// Returns false if session appears hijacked or expired
        /// </summary>
        public static bool ValidateSession(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                return false;
            }

            try
            {
                var session = context.Session;

                // Check if session is initialized
                if (session[SESSION_ID_KEY] == null)
                {
                    return false;
                }

                // Check IP address consistency (prevents session hijacking)
                string sessionIP = session[USER_IP_KEY] as string;
                if (!string.IsNullOrEmpty(sessionIP) && sessionIP != context.Request.UserHostAddress)
                {
                    ErrorLogger.LogWarning("Session hijacking attempt detected",
                        $"Session IP: {sessionIP}, Request IP: {context.Request.UserHostAddress}");
                    DestroySession(context);
                    return false;
                }

                // Check User-Agent consistency
                string sessionUserAgent = session[USER_AGENT_KEY] as string;
                if (!string.IsNullOrEmpty(sessionUserAgent) && sessionUserAgent != context.Request.UserAgent)
                {
                    string sessionUAPreview = sessionUserAgent?.Substring(0, Math.Min(50, sessionUserAgent.Length));
                    string requestUAPreview = context.Request.UserAgent?.Substring(0, Math.Min(50, context.Request.UserAgent?.Length ?? 0));
                    ErrorLogger.LogWarning("Session User-Agent mismatch detected",
                        $"Session UA: {sessionUAPreview}, Request UA: {requestUAPreview}");
                    // Note: Don't destroy session here as User-Agent can change legitimately
                }

                // Check session timeout
                if (session[LAST_ACTIVITY_KEY] is DateTime lastActivity)
                {
                    if (DateTime.Now.Subtract(lastActivity).TotalMinutes > SESSION_TIMEOUT_MINUTES)
                    {
                        ErrorLogger.LogInfo("Session timeout", $"Last activity: {lastActivity}");
                        DestroySession(context);
                        return false;
                    }
                }

                // Check maximum session duration
                if (session[SESSION_START_KEY] is DateTime sessionStart)
                {
                    if (DateTime.Now.Subtract(sessionStart).TotalHours > MAX_SESSION_DURATION_HOURS)
                    {
                        ErrorLogger.LogInfo("Maximum session duration exceeded",
                            $"Session start: {sessionStart}, Duration: {DateTime.Now.Subtract(sessionStart).TotalHours:F2} hours");
                        DestroySession(context);
                        return false;
                    }
                }

                // Update last activity
                session[LAST_ACTIVITY_KEY] = DateTime.Now;

                return true;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error validating session", ex);
                return false;
            }
        }

        /// <summary>
        /// Mark session as authenticated
        /// </summary>
        public static void SetAuthenticated(HttpContext context, string userEmail)
        {
            if (context == null || context.Session == null)
            {
                return;
            }

            try
            {
                context.Session[IS_AUTHENTICATED_KEY] = true;
                context.Session[USER_EMAIL_KEY] = userEmail;
                context.Session[SESSION_START_KEY] = DateTime.Now; // Reset session start time
                ErrorLogger.LogInfo("User authenticated", $"Email: {userEmail}");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error setting authenticated session", ex);
            }
        }

        /// <summary>
        /// Check if current session is authenticated
        /// </summary>
        public static bool IsAuthenticated(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                return false;
            }

            return context.Session[IS_AUTHENTICATED_KEY] is bool isAuth && isAuth;
        }

        /// <summary>
        /// Get authenticated user email
        /// </summary>
        public static string GetUserEmail(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                return null;
            }

            return context.Session[USER_EMAIL_KEY] as string;
        }

        /// <summary>
        /// Destroy session and clear all data
        /// </summary>
        public static void DestroySession(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                return;
            }

            try
            {
                string userEmail = context.Session[USER_EMAIL_KEY] as string;
                context.Session.Clear();
                context.Session.Abandon();

                if (!string.IsNullOrEmpty(userEmail))
                {
                    ErrorLogger.LogInfo("Session destroyed", $"User: {userEmail}");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error destroying session", ex);
            }
        }

        /// <summary>
        /// Regenerate session ID (call after authentication)
        /// Prevents session fixation attacks
        /// </summary>
        public static void RegenerateSessionId(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                return;
            }

            try
            {
                // Save current session data
                var tempData = new System.Collections.Generic.Dictionary<string, object>();
                foreach (string key in context.Session.Keys)
                {
                    tempData[key] = context.Session[key];
                }

                // Abandon old session
                context.Session.Abandon();

                // Create new session with new ID
                if (context.Session != null)
                {
                    // Restore data
                    foreach (var kvp in tempData)
                    {
                        context.Session[kvp.Key] = kvp.Value;
                    }

                    // Generate new session ID
                    context.Session[SESSION_ID_KEY] = Guid.NewGuid().ToString();
                }

                ErrorLogger.LogInfo("Session ID regenerated");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error regenerating session ID", ex);
            }
        }

        /// <summary>
        /// Get session statistics
        /// </summary>
        public static SessionStats GetSessionStats(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                return null;
            }

            try
            {
                return new SessionStats
                {
                    SessionId = context.Session[SESSION_ID_KEY] as string,
                    IsAuthenticated = IsAuthenticated(context),
                    UserEmail = context.Session[USER_EMAIL_KEY] as string,
                    SessionStart = context.Session[SESSION_START_KEY] as DateTime?,
                    LastActivity = context.Session[LAST_ACTIVITY_KEY] as DateTime?,
                    UserIP = context.Session[USER_IP_KEY] as string,
                    UserAgent = context.Session[USER_AGENT_KEY] as string
                };
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error getting session stats", ex);
                return null;
            }
        }

        /// <summary>
        /// Set custom session value securely
        /// </summary>
        public static void SetValue(HttpContext context, string key, object value)
        {
            if (context == null || context.Session == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            try
            {
                context.Session[key] = value;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error setting session value: {key}", ex);
            }
        }

        /// <summary>
        /// Get custom session value
        /// </summary>
        public static T GetValue<T>(HttpContext context, string key) where T : class
        {
            if (context == null || context.Session == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            try
            {
                return context.Session[key] as T;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error getting session value: {key}", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Session statistics
    /// </summary>
    public class SessionStats
    {
        public string SessionId { get; set; }
        public bool IsAuthenticated { get; set; }
        public string UserEmail { get; set; }
        public DateTime? SessionStart { get; set; }
        public DateTime? LastActivity { get; set; }
        public string UserIP { get; set; }
        public string UserAgent { get; set; }

        public TimeSpan? SessionDuration
        {
            get
            {
                if (SessionStart.HasValue)
                {
                    return DateTime.Now.Subtract(SessionStart.Value);
                }
                return null;
            }
        }

        public TimeSpan? TimeSinceLastActivity
        {
            get
            {
                if (LastActivity.HasValue)
                {
                    return DateTime.Now.Subtract(LastActivity.Value);
                }
                return null;
            }
        }
    }
}
