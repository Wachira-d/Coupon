using System;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// Security headers helper to protect against common web vulnerabilities
    /// Add this to Global.asax Application_BeginRequest or use HTTP Module
    /// </summary>
    public static class SecurityHeaders
    {
        /// <summary>
        /// Apply all security headers to current response
        /// Call this from Global.asax Application_BeginRequest
        /// </summary>
        public static void ApplySecurityHeaders(HttpContext context)
        {
            if (context == null || context.Response == null)
            {
                return;
            }

            try
            {
                var response = context.Response;

                // X-Frame-Options: Prevents clickjacking attacks
                // DENY = Page cannot be displayed in a frame
                // SAMEORIGIN = Page can only be displayed in a frame on the same origin
                if (!response.Headers.AllKeys.Contains("X-Frame-Options"))
                {
                    response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                }

                // X-Content-Type-Options: Prevents MIME type sniffing
                // nosniff = Browser should not try to MIME-sniff the content type
                if (!response.Headers.AllKeys.Contains("X-Content-Type-Options"))
                {
                    response.Headers.Add("X-Content-Type-Options", "nosniff");
                }

                // X-XSS-Protection: Legacy XSS protection (mostly replaced by CSP)
                // 1; mode=block = Enable XSS filter and block page if attack detected
                if (!response.Headers.AllKeys.Contains("X-XSS-Protection"))
                {
                    response.Headers.Add("X-XSS-Protection", "1; mode=block");
                }

                // Referrer-Policy: Controls how much referrer information is sent
                // strict-origin-when-cross-origin = Send full URL for same-origin, only origin for cross-origin
                if (!response.Headers.AllKeys.Contains("Referrer-Policy"))
                {
                    response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                }

                // Content-Security-Policy: Powerful protection against XSS and injection attacks
                // NOTE: This is a basic policy - customize based on your needs
                // default-src 'self' = Only allow resources from same origin
                // script-src 'self' 'unsafe-inline' = Allow scripts from same origin and inline scripts (needed for ASP.NET WebForms)
                // style-src 'self' 'unsafe-inline' = Allow styles from same origin and inline styles
                // img-src 'self' data: = Allow images from same origin and data URLs
                if (!response.Headers.AllKeys.Contains("Content-Security-Policy"))
                {
                    response.Headers.Add("Content-Security-Policy",
                        "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // unsafe-eval needed for some JS frameworks
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data:; " +
                        "font-src 'self'; " +
                        "connect-src 'self'; " +
                        "frame-ancestors 'self'");
                }

                // Permissions-Policy: Controls which browser features can be used
                // Replaces Feature-Policy header
                if (!response.Headers.AllKeys.Contains("Permissions-Policy"))
                {
                    response.Headers.Add("Permissions-Policy",
                        "geolocation=(), " +
                        "microphone=(), " +
                        "camera=(), " +
                        "payment=(), " +
                        "usb=(), " +
                        "magnetometer=(), " +
                        "gyroscope=(), " +
                        "accelerometer=()");
                }

                // Strict-Transport-Security (HSTS): Force HTTPS connections
                // max-age=31536000 = 1 year
                // includeSubDomains = Apply to all subdomains
                // preload = Allow inclusion in browser HSTS preload list
                // NOTE: Only add this if you're using HTTPS!
                if (context.Request.IsSecureConnection)
                {
                    if (!response.Headers.AllKeys.Contains("Strict-Transport-Security"))
                    {
                        response.Headers.Add("Strict-Transport-Security",
                            "max-age=31536000; includeSubDomains; preload");
                    }
                }

                // Remove server information headers (reduce information disclosure)
                RemoveServerHeaders(response);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error applying security headers", ex);
            }
        }

        /// <summary>
        /// Remove server information headers to reduce information disclosure
        /// </summary>
        private static void RemoveServerHeaders(HttpResponse response)
        {
            try
            {
                // Remove X-Powered-By header (reveals ASP.NET)
                if (response.Headers.AllKeys.Contains("X-Powered-By"))
                {
                    response.Headers.Remove("X-Powered-By");
                }

                // Remove X-AspNet-Version header (reveals ASP.NET version)
                if (response.Headers.AllKeys.Contains("X-AspNet-Version"))
                {
                    response.Headers.Remove("X-AspNet-Version");
                }

                // Remove X-AspNetMvc-Version header (reveals MVC version)
                if (response.Headers.AllKeys.Contains("X-AspNetMvc-Version"))
                {
                    response.Headers.Remove("X-AspNetMvc-Version");
                }

                // Note: Server header can only be removed via web.config or IIS configuration
                // Add this to web.config under <system.webServer>:
                // <httpProtocol>
                //   <customHeaders>
                //     <remove name="X-Powered-By" />
                //   </customHeaders>
                // </httpProtocol>
                // <security>
                //   <requestFiltering removeServerHeader="true" />
                // </security>
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error removing server headers", ex);
            }
        }

        /// <summary>
        /// Apply HTTPS-only policy
        /// Redirect HTTP requests to HTTPS
        /// </summary>
        public static void EnforceHttps(HttpContext context)
        {
            if (context == null || context.Request == null)
            {
                return;
            }

            try
            {
                // Check if request is not secure and not from localhost
                if (!context.Request.IsSecureConnection && !context.Request.IsLocal)
                {
                    string secureUrl = context.Request.Url.ToString().Replace("http://", "https://");
                    context.Response.Redirect(secureUrl, true);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error enforcing HTTPS", ex);
            }
        }
    }
}
