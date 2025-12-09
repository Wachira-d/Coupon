using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.DirectoryServices.AccountManagement;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text.RegularExpressions;
using Coupon.Helpers;

namespace Coupon.Admin
{
    /// <summary>
    /// Admin reporting page with secure authentication and database access
    /// SECURITY: Uses parameterized queries, improved authentication, and input validation
    /// </summary>
    public partial class Report : Page
    {
        private DatabaseHelper _dbHelper = new DatabaseHelper();

        protected void Page_Load(object sender, EventArgs e)
        {
            // SECURITY: Check authentication before allowing access
            if (!AuthenticationHelper.ValidateSession(Session))
            {
                // Redirect to login page with return URL
                string returnUrl = Request.Url.PathAndQuery;
                Response.Redirect($"~/Admin/Login.aspx?ReturnUrl={Server.UrlEncode(returnUrl)}", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                // Show user info
                UserInfo currentUser = AuthenticationHelper.GetCurrentUser(Session);
                if (currentUser != null)
                {
                    // You can display welcome message or user info here if needed
                    // e.g., lblWelcome.Text = $"Welcome, {currentUser.DisplayName}";
                }

                LoadDashboardStats();

                // Hide old login controls
                Panel1.Visible = true;
                TextBox1.Visible = false;
                TextBox13.Visible = false;
                Button1.Visible = false;
            }
        }

        /// <summary>
        /// Load dashboard statistics
        /// </summary>
        private void LoadDashboardStats()
        {
            try
            {
                // SECURITY: Safe queries without user input
                string queryAll = $"SELECT * FROM {_dbHelper.GetTableName("Coupon")}";
                string querySend = $"SELECT * FROM {_dbHelper.GetTableName("Coupon")} WHERE {_dbHelper.GetColumnName("Send_Status")} = @status";
                string queryUsed = $"SELECT * FROM {_dbHelper.GetTableName("Coupon")} WHERE {_dbHelper.GetColumnName("Used_Status")} = @status";

                DataTable dtCodeAll = _dbHelper.ExecuteQuery(queryAll);
                DataTable dtCodeSend = _dbHelper.ExecuteQuery(querySend, _dbHelper.CreateParameter("@status", _dbHelper.GetBooleanValue(true)));
                DataTable dtCodeUsed = _dbHelper.ExecuteQuery(queryUsed, _dbHelper.CreateParameter("@status", _dbHelper.GetBooleanValue(true)));

                Label1.Text = dtCodeUsed.Rows.Count + "\\" + dtCodeAll.Rows.Count;
                Label3.Text = dtCodeSend.Rows.Count + "\\" + dtCodeAll.Rows.Count;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error loading dashboard stats", ex);
            }
        }

        /// <summary>
        /// Admin login button click handler
        /// SECURITY: Improved authentication with better validation
        /// </summary>
        protected void Button1_Click(object sender, EventArgs e)
        {
            string email = TextBox1.Text?.Trim();
            string password = TextBox13.Text;
            bool isValid = false;

            // SECURITY: Validate input
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ErrorLogger.LogWarning("Admin login attempt with empty credentials", $"IP: {Request.UserHostAddress}");
                TextBox13.Text = "Email and password required";
                LogAdminAccess(email, false);
                return;
            }

            // SECURITY: Validate email format
            if (!InputValidator.IsValidEmail(email))
            {
                ErrorLogger.LogWarning("Admin login attempt with invalid email format", $"Email: {email}, IP: {Request.UserHostAddress}");
                TextBox13.Text = "Invalid email format";
                LogAdminAccess(email, false);
                return;
            }

            // Check if email is in authorized list
            string[] authorizedEmails = ConfigurationManager.AppSettings["ReportPassword"]?.Split(';') ?? new string[0];
            bool isAuthorized = false;

            foreach (string authorizedEmail in authorizedEmails)
            {
                if (email.Equals(authorizedEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    isAuthorized = true;
                    break;
                }
            }

            if (!isAuthorized)
            {
                ErrorLogger.LogWarning("Admin login attempt with unauthorized email", $"Email: {email}, IP: {Request.UserHostAddress}");
                AuditLogger.LogSecurityEvent(
                    "Unauthorized Admin Access Attempt",
                    Request.UserHostAddress,
                    $"Email: {email} attempted to access admin panel",
                    SecuritySeverity.High
                );
                TextBox13.Text = "Unauthorized email";
                LogAdminAccess(email, false);
                return;
            }

            // Validate credentials against LDAP
            try
            {
                string ldapServer = ConfigurationManager.AppSettings["LDAP"];
                if (!string.IsNullOrEmpty(ldapServer))
                {
                    using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, ldapServer))
                    {
                        isValid = pc.ValidateCredentials(email, password);
                    }
                }
                else
                {
                    ErrorLogger.LogError("LDAP server not configured", null);
                    TextBox13.Text = "Authentication system error";
                    return;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("LDAP authentication error", ex, $"Email: {email}");
                TextBox13.Text = "Authentication system error";
                LogAdminAccess(email, false);
                return;
            }

            if (isValid)
            {
                // Successful login
                Panel1.Visible = true;
                TextBox1.Visible = false;
                TextBox13.Visible = false;
                Button1.Visible = false;

                LogAdminAccess(email, true);
                ErrorLogger.LogInfo("Admin login successful", $"Email: {email}, IP: {Request.UserHostAddress}");

                // SECURITY: Log successful authentication to audit log
                AuditLogger.LogAuthentication(email, true, Request.UserHostAddress, "Admin panel access");

                // SECURITY: Regenerate session ID to prevent session fixation
                SessionManager.RegenerateSessionId(Context);
            }
            else
            {
                TextBox13.Text = "Password incorrect";
                LogAdminAccess(email, false);
                ErrorLogger.LogWarning("Admin login failed - invalid password", $"Email: {email}, IP: {Request.UserHostAddress}");

                // SECURITY: Log failed authentication to audit log
                AuditLogger.LogAuthentication(email, false, Request.UserHostAddress, "Invalid password");
            }
        }

        /// <summary>
        /// Log admin access attempt
        /// SECURITY: Uses parameterized query
        /// </summary>
        private void LogAdminAccess(string email, bool canAccess)
        {
            try
            {
                string machineName = Request.UserHostAddress;
                try
                {
                    machineName = System.Net.Dns.GetHostEntry(Request.UserHostAddress).HostName;
                }
                catch { }

                string logQuery = $"INSERT INTO {_dbHelper.GetTableName("Admin_Logs")} " +
                                $"({_dbHelper.GetColumnName("Created_DateTime")}, {_dbHelper.GetColumnName("Machine_Name")}, " +
                                $"{_dbHelper.GetColumnName("IP_Address")}, {_dbHelper.GetColumnName("Browser")}, " +
                                $"{_dbHelper.GetColumnName("Access_URL")}, {_dbHelper.GetColumnName("Password")}, " +
                                $"{_dbHelper.GetColumnName("CanAccess")}) " +
                                $"VALUES(@createdDateTime, @machineName, @ipAddress, @browser, @accessUrl, @password, @canAccess)";

                var logParams = new[]
                {
                    _dbHelper.CreateParameter("@createdDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    _dbHelper.CreateParameter("@machineName", machineName),
                    _dbHelper.CreateParameter("@ipAddress", Request.UserHostAddress),
                    _dbHelper.CreateParameter("@browser", Request.Browser.Browser),
                    _dbHelper.CreateParameter("@accessUrl", Request.Url.AbsoluteUri),
                    _dbHelper.CreateParameter("@password", email), // Log email, not password
                    _dbHelper.CreateParameter("@canAccess", _dbHelper.GetBooleanValue(canAccess))
                };

                _dbHelper.ExecuteNonQuery(logQuery, logParams);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error logging admin access", ex);
            }
        }

        /// <summary>
        /// Search coupon button click handler
        /// SECURITY: Uses parameterized queries for all user inputs
        /// </summary>
        protected void Button2_Click(object sender, EventArgs e)
        {
            try
            {
                string query = $"SELECT * FROM {_dbHelper.GetTableName("Coupon")} WHERE 1=1";
                var parameters = new System.Collections.Generic.List<DbParameter>();

                // SECURITY: Validate and add parameters for each search field
                if (!string.IsNullOrWhiteSpace(TextBox2.Text))
                {
                    string id = InputValidator.SanitizeInput(TextBox2.Text);
                    query += $" AND {_dbHelper.GetColumnName("ID")} LIKE @id";
                    parameters.Add(_dbHelper.CreateParameter("@id", "%" + id + "%"));
                }

                if (!string.IsNullOrWhiteSpace(TextBox3.Text))
                {
                    string code = InputValidator.SanitizeInput(TextBox3.Text);
                    query += $" AND {_dbHelper.GetColumnName("Code")} LIKE @code";
                    parameters.Add(_dbHelper.CreateParameter("@code", "%" + code + "%"));
                }

                if (!string.IsNullOrWhiteSpace(TextBox4.Text))
                {
                    if (DateTime.TryParse(TextBox4.Text, out DateTime dateFrom))
                    {
                        query += $" AND {_dbHelper.GetColumnName("Date_DataIn")} >= @dateFrom";
                        parameters.Add(_dbHelper.CreateParameter("@dateFrom", dateFrom.ToString("yyyy-MM-dd")));
                    }
                }

                if (!string.IsNullOrWhiteSpace(TextBox5.Text))
                {
                    if (DateTime.TryParse(TextBox5.Text, out DateTime dateTo))
                    {
                        query += $" AND {_dbHelper.GetColumnName("Date_DataIn")} <= @dateTo";
                        parameters.Add(_dbHelper.CreateParameter("@dateTo", dateTo.ToString("yyyy-MM-dd")));
                    }
                }

                if (!string.IsNullOrWhiteSpace(TextBox6.Text))
                {
                    if (DateTime.TryParse(TextBox6.Text, out DateTime sendFrom))
                    {
                        query += $" AND {_dbHelper.GetColumnName("Date_Send")} >= @sendFrom";
                        parameters.Add(_dbHelper.CreateParameter("@sendFrom", sendFrom.ToString("yyyy-MM-dd")));
                    }
                }

                if (!string.IsNullOrWhiteSpace(TextBox7.Text))
                {
                    if (DateTime.TryParse(TextBox7.Text, out DateTime sendTo))
                    {
                        query += $" AND {_dbHelper.GetColumnName("Date_Send")} <= @sendTo";
                        parameters.Add(_dbHelper.CreateParameter("@sendTo", sendTo.ToString("yyyy-MM-dd")));
                    }
                }

                if (!string.IsNullOrWhiteSpace(TextBox8.Text))
                {
                    if (DateTime.TryParse(TextBox8.Text, out DateTime useFrom))
                    {
                        query += $" AND {_dbHelper.GetColumnName("Date_Use")} >= @useFrom";
                        parameters.Add(_dbHelper.CreateParameter("@useFrom", useFrom.ToString("yyyy-MM-dd")));
                    }
                }

                if (!string.IsNullOrWhiteSpace(TextBox9.Text))
                {
                    if (DateTime.TryParse(TextBox9.Text, out DateTime useTo))
                    {
                        query += $" AND {_dbHelper.GetColumnName("Date_Use")} <= @useTo";
                        parameters.Add(_dbHelper.CreateParameter("@useTo", useTo.ToString("yyyy-MM-dd")));
                    }
                }

                if (DropDownList1.SelectedIndex > 0)
                {
                    query += $" AND {_dbHelper.GetColumnName("Send_Status")} = @sendStatus";
                    parameters.Add(_dbHelper.CreateParameter("@sendStatus", DropDownList1.SelectedValue));
                }

                if (DropDownList2.SelectedIndex > 0)
                {
                    query += $" AND {_dbHelper.GetColumnName("Used_Status")} = @usedStatus";
                    parameters.Add(_dbHelper.CreateParameter("@usedStatus", DropDownList2.SelectedValue));
                }

                DataTable dtResult = _dbHelper.ExecuteQuery(query, parameters.ToArray());
                GridView1.DataSource = dtResult;
                GridView1.DataBind();
                Label2.Text = dtResult.Rows.Count + " rows";

                ErrorLogger.LogInfo("Coupon search executed", $"Results: {dtResult.Rows.Count}, IP: {Request.UserHostAddress}");

                // SECURITY: Log admin search activity
                AuditLogger.LogAdminAction(
                    "admin",
                    "Coupon Search",
                    Request.UserHostAddress,
                    $"Results: {dtResult.Rows.Count} records"
                );
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error during coupon search", ex);
                Label2.Text = "Search error";
            }
        }

        /// <summary>
        /// Clear search results button
        /// </summary>
        protected void Button3_Click(object sender, EventArgs e)
        {
            GridView1.DataSource = null;
            GridView1.DataBind();
            Label2.Text = "";
        }

        /// <summary>
        /// Search logs button click handler
        /// SECURITY: Uses parameterized queries for all user inputs
        /// </summary>
        protected void Button4_Click(object sender, EventArgs e)
        {
            try
            {
                string query = $"SELECT * FROM {_dbHelper.GetTableName("Logs")} WHERE 1=1";
                var parameters = new System.Collections.Generic.List<DbParameter>();

                // SECURITY: Validate and add parameters for each search field
                if (!string.IsNullOrWhiteSpace(TextBox12.Text))
                {
                    string url = InputValidator.SanitizeInput(TextBox12.Text);
                    query += $" AND {_dbHelper.GetColumnName("Access_URL")} LIKE @url";
                    parameters.Add(_dbHelper.CreateParameter("@url", "%" + url + "%"));
                }

                if (CheckBox4.Checked)
                {
                    query += $" AND {_dbHelper.GetColumnName("Access_URL")} LIKE @redeem";
                    parameters.Add(_dbHelper.CreateParameter("@redeem", "%Redeem%"));
                }

                if (!string.IsNullOrWhiteSpace(TextBox10.Text))
                {
                    if (DateTime.TryParse(TextBox10.Text, out DateTime dateFrom))
                    {
                        query += $" AND {_dbHelper.GetColumnName("Created_DateTime")} >= @dateFrom";
                        parameters.Add(_dbHelper.CreateParameter("@dateFrom", dateFrom.ToString("yyyy-MM-dd")));
                    }
                }

                if (!string.IsNullOrWhiteSpace(TextBox11.Text))
                {
                    if (DateTime.TryParse(TextBox11.Text, out DateTime dateTo))
                    {
                        query += $" AND {_dbHelper.GetColumnName("Created_DateTime")} <= @dateTo";
                        parameters.Add(_dbHelper.CreateParameter("@dateTo", dateTo.ToString("yyyy-MM-dd")));
                    }
                }

                query += $" AND {_dbHelper.GetColumnName("Found_Coupon")} = @foundCoupon";
                parameters.Add(_dbHelper.CreateParameter("@foundCoupon", _dbHelper.GetBooleanValue(CheckBox3.Checked)));

                DataTable dtResult = _dbHelper.ExecuteQuery(query, parameters.ToArray());
                GridView1.DataSource = dtResult;
                GridView1.DataBind();
                Label2.Text = dtResult.Rows.Count + " rows";

                ErrorLogger.LogInfo("Logs search executed", $"Results: {dtResult.Rows.Count}, IP: {Request.UserHostAddress}");

                // SECURITY: Log admin search activity
                AuditLogger.LogAdminAction(
                    "admin",
                    "Logs Search",
                    Request.UserHostAddress,
                    $"Results: {dtResult.Rows.Count} records"
                );
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error during logs search", ex);
                Label2.Text = "Search error";
            }
        }
    }
}
