using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// CSRF (Cross-Site Request Forgery) protection helper
    /// Implements token-based CSRF protection for ASP.NET Web Forms
    /// </summary>
    public static class CsrfProtection
    {
        private const string CSRF_TOKEN_KEY = "__RequestVerificationToken";
        private const string CSRF_SESSION_KEY = "_CsrfToken";

        /// <summary>
        /// Generate CSRF token and store in session
        /// Call this in Page_Load when !IsPostBack
        /// </summary>
        public static string GenerateToken(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                ErrorLogger.LogWarning("Cannot generate CSRF token - context or session is null");
                return string.Empty;
            }

            try
            {
                // Generate a random token
                string token = GenerateRandomToken();

                // Store in session
                context.Session[CSRF_SESSION_KEY] = token;

                return token;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error generating CSRF token", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Get the current CSRF token from session
        /// Returns null if no token exists
        /// </summary>
        public static string GetToken(HttpContext context)
        {
            if (context == null || context.Session == null)
            {
                return null;
            }

            return context.Session[CSRF_SESSION_KEY] as string;
        }

        /// <summary>
        /// Validate CSRF token from request against session token
        /// Call this in Page_Load when IsPostBack or before processing form data
        /// </summary>
        public static bool ValidateToken(HttpContext context)
        {
            if (context == null || context.Request == null || context.Session == null)
            {
                ErrorLogger.LogWarning("Cannot validate CSRF token - context is null");
                return false;
            }

            try
            {
                // Get token from request (form or header)
                string requestToken = context.Request.Form[CSRF_TOKEN_KEY] ??
                                    context.Request.Headers[CSRF_TOKEN_KEY];

                // Get token from session
                string sessionToken = context.Session[CSRF_SESSION_KEY] as string;

                // Both must exist
                if (string.IsNullOrEmpty(requestToken) || string.IsNullOrEmpty(sessionToken))
                {
                    ErrorLogger.LogWarning("CSRF token validation failed - token missing",
                        $"IP: {context.Request.UserHostAddress}, URL: {context.Request.Url}");
                    return false;
                }

                // Compare tokens (constant-time comparison to prevent timing attacks)
                bool isValid = ConstantTimeEquals(requestToken, sessionToken);

                if (!isValid)
                {
                    ErrorLogger.LogWarning("CSRF token validation failed - token mismatch",
                        $"IP: {context.Request.UserHostAddress}, URL: {context.Request.Url}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error validating CSRF token", ex);
                return false;
            }
        }

        /// <summary>
        /// Generate HTML hidden field with CSRF token
        /// Add this to your forms: <%= CsrfProtection.GetTokenField(Context) %>
        /// </summary>
        public static string GetTokenField(HttpContext context)
        {
            string token = GetToken(context);
            if (string.IsNullOrEmpty(token))
            {
                token = GenerateToken(context);
            }

            return $"<input type=\"hidden\" name=\"{CSRF_TOKEN_KEY}\" value=\"{HttpUtility.HtmlEncode(token)}\" />";
        }

        /// <summary>
        /// Get CSRF token as meta tag for JavaScript access
        /// Add to page head: <%= CsrfProtection.GetTokenMeta(Context) %>
        /// </summary>
        public static string GetTokenMeta(HttpContext context)
        {
            string token = GetToken(context);
            if (string.IsNullOrEmpty(token))
            {
                token = GenerateToken(context);
            }

            return $"<meta name=\"{CSRF_TOKEN_KEY}\" content=\"{HttpUtility.HtmlEncode(token)}\" />";
        }

        /// <summary>
        /// Generate a cryptographically secure random token
        /// </summary>
        private static string GenerateRandomToken()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenBytes = new byte[32]; // 256 bits
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        /// <summary>
        /// Constant-time string comparison to prevent timing attacks
        /// </summary>
        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            uint diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }

            return diff == 0;
        }

        /// <summary>
        /// Invalidate current CSRF token (e.g., after form submission)
        /// </summary>
        public static void InvalidateToken(HttpContext context)
        {
            if (context != null && context.Session != null)
            {
                context.Session.Remove(CSRF_SESSION_KEY);
            }
        }

        /// <summary>
        /// Regenerate CSRF token (e.g., after authentication)
        /// </summary>
        public static string RegenerateToken(HttpContext context)
        {
            InvalidateToken(context);
            return GenerateToken(context);
        }
    }

    /// <summary>
    /// Usage example in Page_Load:
    ///
    /// protected void Page_Load(object sender, EventArgs e)
    /// {
    ///     if (!IsPostBack)
    ///     {
    ///         // Generate token for new page load
    ///         CsrfProtection.GenerateToken(Context);
    ///     }
    ///     else
    ///     {
    ///         // Validate token on postback
    ///         if (!CsrfProtection.ValidateToken(Context))
    ///         {
    ///             ErrorLogger.LogWarning("CSRF attack detected");
    ///             Response.StatusCode = 403;
    ///             Response.End();
    ///             return;
    ///         }
    ///     }
    /// }
    ///
    /// In your .aspx form:
    /// <form runat="server">
    ///     <%= CsrfProtection.GetTokenField(Context) %>
    ///     <!-- your form fields -->
    /// </form>
    ///
    /// For AJAX requests, add token to headers:
    /// <script>
    /// var token = document.querySelector('meta[name="__RequestVerificationToken"]').content;
    /// fetch('/api/endpoint', {
    ///     method: 'POST',
    ///     headers: {
    ///         '__RequestVerificationToken': token
    ///     }
    /// });
    /// </script>
    /// </summary>
}
