using System;
using System.Web;
using Coupon.Helpers;

namespace Coupon
{
    public partial class Error : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                // Get error information
                Exception lastError = Server.GetLastError();

                // Check for HTTP error code from query string
                string errorCode = Request.QueryString["code"];
                string errorMessage = Request.QueryString["msg"];

                // Handle specific HTTP error codes
                if (!string.IsNullOrEmpty(errorCode))
                {
                    HandleHttpError(errorCode, errorMessage);
                }
                else if (lastError != null)
                {
                    HandleException(lastError);
                }
                else
                {
                    // Generic error
                    litErrorTitle.Text = "Page Not Found";
                    litErrorMessage.Text = "The page you are looking for might have been removed, had its name changed, or is temporarily unavailable.";
                }

                // Clear the error
                Server.ClearError();
            }
            catch (Exception ex)
            {
                // Last resort - don't throw from error page
                try
                {
                    ErrorLogger.LogError("Error in Error.aspx", ex);
                }
                catch
                {
                    // Silently fail
                }

                litErrorTitle.Text = "Error";
                litErrorMessage.Text = "An unexpected error occurred. Please try again later.";
            }
        }

        private void HandleHttpError(string errorCode, string errorMessage)
        {
            divErrorCode.Visible = true;
            litErrorCode.Text = errorCode;

            switch (errorCode)
            {
                case "400":
                    litErrorTitle.Text = "Bad Request";
                    litErrorMessage.Text = "The request could not be understood by the server. Please check your input and try again.";
                    Response.StatusCode = 400;
                    break;

                case "401":
                    litErrorTitle.Text = "Unauthorized";
                    litErrorMessage.Text = "You are not authorized to access this resource. Please log in and try again.";
                    Response.StatusCode = 401;
                    lnkHome.NavigateUrl = "~/Admin/Login.aspx";
                    lnkHome.Text = "Login";
                    break;

                case "403":
                    litErrorTitle.Text = "Access Denied";
                    litErrorMessage.Text = "You don't have permission to access this resource. Contact your administrator if you believe this is an error.";
                    Response.StatusCode = 403;
                    break;

                case "404":
                    litErrorTitle.Text = "Page Not Found";
                    litErrorMessage.Text = "The page you are looking for might have been removed, had its name changed, or is temporarily unavailable.";
                    Response.StatusCode = 404;
                    break;

                case "500":
                    litErrorTitle.Text = "Internal Server Error";
                    litErrorMessage.Text = "The server encountered an internal error and was unable to complete your request. Our team has been notified.";
                    Response.StatusCode = 500;
                    break;

                case "503":
                    litErrorTitle.Text = "Service Unavailable";
                    litErrorMessage.Text = "The service is temporarily unavailable due to maintenance or high traffic. Please try again later.";
                    Response.StatusCode = 503;
                    break;

                default:
                    litErrorTitle.Text = "Error";
                    litErrorMessage.Text = !string.IsNullOrEmpty(errorMessage) ? errorMessage : "An unexpected error occurred.";
                    break;
            }
        }

        private void HandleException(Exception exception)
        {
            // Log the error
            try
            {
                string url = Request.Url?.ToString() ?? "Unknown";
                string ip = Request.UserHostAddress ?? "Unknown";
                ErrorLogger.LogError("Unhandled exception", exception, $"URL: {url}, IP: {ip}");
            }
            catch
            {
                // Silently fail
            }

            // Show user-friendly message
            if (exception is HttpException httpEx)
            {
                HandleHttpError(httpEx.GetHttpCode().ToString(), null);
            }
            else
            {
                litErrorTitle.Text = "Something Went Wrong";
                litErrorMessage.Text = "We apologize for the inconvenience. An unexpected error occurred. Please try again later.";

                // Show debug info in development mode
                if (!SecureConfig.IsProduction())
                {
                    divDebugInfo.Visible = true;
                    litDebugInfo.Text = $"Exception Type: {exception.GetType().Name}\n" +
                                       $"Message: {exception.Message}\n" +
                                       $"Stack Trace:\n{exception.StackTrace}";

                    if (exception.InnerException != null)
                    {
                        litDebugInfo.Text += $"\n\nInner Exception:\n{exception.InnerException.Message}";
                    }
                }
            }
        }
    }
}
