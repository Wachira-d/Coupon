using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Coupon.Helpers;

namespace Coupon.Admin
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Check if already logged in
                if (AuthenticationHelper.ValidateSession(Session))
                {
                    RedirectToAdminPage();
                    return;
                }

                // Check for remember me cookie
                HttpCookie rememberCookie = Request.Cookies["RememberMe"];
                if (rememberCookie != null && !string.IsNullOrEmpty(rememberCookie["Username"]))
                {
                    txtUsername.Text = rememberCookie["Username"];
                    chkRememberMe.Checked = true;
                }

                // Check for return URL
                if (!string.IsNullOrEmpty(Request.QueryString["ReturnUrl"]))
                {
                    ViewState["ReturnUrl"] = Request.QueryString["ReturnUrl"];
                }
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("Please enter both username and password");
                    return;
                }

                // Get client info for logging
                string ipAddress = GetClientIpAddress();
                string userAgent = Request.UserAgent;

                // Authenticate
                AuthResult result = AuthenticationHelper.Authenticate(username, password, ipAddress, userAgent);

                if (result.Success)
                {
                    // Create session
                    Session["Username"] = result.User.Username;
                    Session["DisplayName"] = result.User.DisplayName;
                    Session["Email"] = result.User.Email;
                    Session["Department"] = result.User.Department;
                    Session["Title"] = result.User.Title;
                    Session["Groups"] = result.User.Groups;
                    Session["LoginTime"] = DateTime.Now;
                    Session["AuthSource"] = result.AuthSource;

                    // Handle remember me
                    if (chkRememberMe.Checked)
                    {
                        HttpCookie rememberCookie = new HttpCookie("RememberMe");
                        rememberCookie["Username"] = result.User.Username;
                        rememberCookie.Expires = DateTime.Now.AddDays(30);
                        Response.Cookies.Add(rememberCookie);
                    }
                    else
                    {
                        // Clear remember me cookie
                        if (Request.Cookies["RememberMe"] != null)
                        {
                            HttpCookie rememberCookie = new HttpCookie("RememberMe");
                            rememberCookie.Expires = DateTime.Now.AddDays(-1);
                            Response.Cookies.Add(rememberCookie);
                        }
                    }

                    // Log successful login
                    AuditLogger.LogAuthentication(
                        result.User.Username,
                        true,
                        ipAddress,
                        $"Auth Source: {result.AuthSource}"
                    );

                    // Show success message
                    ShowSuccess($"Welcome, {result.User.DisplayName}! Redirecting...");

                    // Redirect after short delay (using client-side redirect)
                    string returnUrl = ViewState["ReturnUrl"]?.ToString();
                    if (!string.IsNullOrEmpty(returnUrl) && IsLocalUrl(returnUrl))
                    {
                        ClientScript.RegisterStartupScript(this.GetType(), "redirect",
                            $"setTimeout(function(){{ window.location = '{returnUrl}'; }}, 1000);", true);
                    }
                    else
                    {
                        RedirectToAdminPage();
                    }
                }
                else
                {
                    // Show error message
                    ShowError(result.Message);

                    // Log failed login
                    AuditLogger.LogSecurityEvent(
                        "Failed Login Attempt",
                        ipAddress,
                        $"Username: {username}, Reason: {result.Message}",
                        SecuritySeverity.Medium
                    );

                    // Clear password field
                    txtPassword.Text = "";
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Login error", ex);
                ShowError("An error occurred during login. Please try again.");
            }
        }

        private void ShowError(string message)
        {
            panelAlert.CssClass = "alert alert-error show";
            litMessage.Text = $"<strong>❌ Error:</strong> {Server.HtmlEncode(message)}";
            panelAlert.Visible = true;
        }

        private void ShowSuccess(string message)
        {
            panelAlert.CssClass = "alert alert-success show";
            litMessage.Text = $"<strong>✅ Success:</strong> {Server.HtmlEncode(message)}";
            panelAlert.Visible = true;
        }

        private void ShowInfo(string message)
        {
            panelAlert.CssClass = "alert alert-info show";
            litMessage.Text = $"<strong>ℹ️ Info:</strong> {Server.HtmlEncode(message)}";
            panelAlert.Visible = true;
        }

        private void RedirectToAdminPage()
        {
            // Default admin page after login
            Response.Redirect("~/Admin/Report.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private string GetClientIpAddress()
        {
            string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            }
            else
            {
                // Take the first IP if there are multiple (proxy chain)
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length > 0)
                {
                    ipAddress = addresses[0].Trim();
                }
            }

            return ipAddress ?? "Unknown";
        }

        private bool IsLocalUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // Prevent open redirect vulnerability
            return url.StartsWith("/") && !url.StartsWith("//") && !url.StartsWith("/\\");
        }
    }
}
