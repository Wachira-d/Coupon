using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Coupon.Helpers;

namespace Coupon.Admin
{
    /// <summary>
    /// Enhanced Admin reporting dashboard with modern UI and advanced features
    /// SECURITY: Uses parameterized queries, session authentication, and comprehensive auditing
    /// </summary>
    public partial class Report : Page
    {
        private DatabaseHelper _dbHelper = new DatabaseHelper();
        private DataTable _currentSearchResults; // Store for export

        protected void Page_Load(object sender, EventArgs e)
        {
            // Store search results in ViewState for paging/sorting/export
            if (ViewState["SearchResults"] != null)
            {
                _currentSearchResults = (DataTable)ViewState["SearchResults"];
            }

            // SECURITY: Check authentication - redirect to Login page if not authenticated
            if (!AuthenticationHelper.ValidateSession(Session))
            {
                Response.Redirect("~/Admin/Login.aspx?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                // Show user info
                UserInfo currentUser = AuthenticationHelper.GetCurrentUser(Session);
                if (currentUser != null)
                {
                    lblCurrentUser.Text = currentUser.DisplayName ?? currentUser.Username;
                    lblCurrentEmail.Text = currentUser.Email ?? "";
                }

                LoadDashboardStatistics();
            }
        }

        #region Dashboard Statistics

        /// <summary>
        /// Load comprehensive dashboard statistics
        /// </summary>
        private void LoadDashboardStatistics()
        {
            try
            {
                // Get all coupons
                string queryAll = $"SELECT * FROM {_dbHelper.GetTableName("Coupon")}";
                DataTable dtAll = _dbHelper.ExecuteQuery(queryAll);
                int totalCoupons = dtAll.Rows.Count;

                // Get sent coupons
                string querySent = $"SELECT * FROM {_dbHelper.GetTableName("Coupon")} WHERE {_dbHelper.GetColumnName("Send_Status")} = @status";
                DataTable dtSent = _dbHelper.ExecuteQuery(querySent, _dbHelper.CreateParameter("@status", _dbHelper.GetBooleanValue(true)));
                int sentCoupons = dtSent.Rows.Count;

                // Get used coupons
                string queryUsed = $"SELECT * FROM {_dbHelper.GetTableName("Coupon")} WHERE {_dbHelper.GetColumnName("Used_Status")} = @status";
                DataTable dtUsed = _dbHelper.ExecuteQuery(queryUsed, _dbHelper.CreateParameter("@status", _dbHelper.GetBooleanValue(true)));
                int usedCoupons = dtUsed.Rows.Count;

                // Calculate expired coupons
                int expiredCoupons = 0;
                foreach (DataRow row in dtAll.Rows)
                {
                    try
                    {
                        string expireDateStr = row[_dbHelper.GetColumnName("Expire_DateTime")]?.ToString();
                        if (!string.IsNullOrEmpty(expireDateStr) && DateTime.TryParse(expireDateStr, out DateTime expireDate))
                        {
                            if (expireDate < DateTime.Now)
                            {
                                expiredCoupons++;
                            }
                        }
                    }
                    catch { }
                }

                // Update labels
                lblTotalCoupons.Text = totalCoupons.ToString("N0");
                lblCouponsSent.Text = sentCoupons.ToString("N0");
                lblCouponsUsed.Text = usedCoupons.ToString("N0");
                lblExpiredCoupons.Text = expiredCoupons.ToString("N0");

                // Calculate percentages
                if (totalCoupons > 0)
                {
                    lblSentPercentage.Text = $"{(sentCoupons * 100.0 / totalCoupons):F1}% of total";
                    lblUsedPercentage.Text = $"{(usedCoupons * 100.0 / totalCoupons):F1}% of total";
                    lblExpiredPercentage.Text = $"{(expiredCoupons * 100.0 / totalCoupons):F1}% of total";
                }
                else
                {
                    lblSentPercentage.Text = "0% of total";
                    lblUsedPercentage.Text = "0% of total";
                    lblExpiredPercentage.Text = "0% of total";
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error loading dashboard statistics", ex);
                lblTotalCoupons.Text = "Error";
                lblCouponsSent.Text = "Error";
                lblCouponsUsed.Text = "Error";
                lblExpiredCoupons.Text = "Error";
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Log admin access attempts
        /// </summary>
        private void LogAdminAccess(string email, bool canAccess)
        {
            try
            {
                string machineName = GetClientIpAddress();
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
                                $"VALUES(@createdDateTime, @machineName, @ipAddress, @browser, @accessUrl, @email, @canAccess)";

                var logParams = new[]
                {
                    _dbHelper.CreateParameter("@createdDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    _dbHelper.CreateParameter("@machineName", machineName),
                    _dbHelper.CreateParameter("@ipAddress", Request.UserHostAddress),
                    _dbHelper.CreateParameter("@browser", Request.Browser.Browser),
                    _dbHelper.CreateParameter("@accessUrl", Request.Url.AbsoluteUri),
                    _dbHelper.CreateParameter("@email", email),
                    _dbHelper.CreateParameter("@canAccess", _dbHelper.GetBooleanValue(canAccess))
                };

                _dbHelper.ExecuteNonQuery(logQuery, logParams);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error logging admin access", ex);
            }
        }

        private string GetClientIpAddress()
        {
            string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length > 0)
                {
                    ipAddress = addresses[0].Trim();
                }
            }
            else
            {
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            }

            return ipAddress ?? "Unknown";
        }

        #endregion

        #region Coupon Search

        /// <summary>
        /// Search coupons with advanced filtering
        /// </summary>
        protected void btnSearchCoupons_Click(object sender, EventArgs e)
        {
            try
            {
                string query = $"SELECT * FROM {_dbHelper.GetTableName("Coupon")} WHERE 1=1";
                var parameters = new System.Collections.Generic.List<DbParameter>();

                // Add search filters
                if (!string.IsNullOrWhiteSpace(txtCouponId.Text))
                {
                    query += $" AND {_dbHelper.GetColumnName("ID")} LIKE @id";
                    parameters.Add(_dbHelper.CreateParameter("@id", "%" + InputValidator.SanitizeInput(txtCouponId.Text) + "%"));
                }

                if (!string.IsNullOrWhiteSpace(txtCouponCode.Text))
                {
                    query += $" AND {_dbHelper.GetColumnName("Code")} LIKE @code";
                    parameters.Add(_dbHelper.CreateParameter("@code", "%" + InputValidator.SanitizeInput(txtCouponCode.Text) + "%"));
                }

                if (!string.IsNullOrWhiteSpace(txtRegisterFrom.Text) && DateTime.TryParse(txtRegisterFrom.Text, out DateTime regFrom))
                {
                    query += $" AND {_dbHelper.GetColumnName("Date_DataIn")} >= @regFrom";
                    parameters.Add(_dbHelper.CreateParameter("@regFrom", regFrom.ToString("yyyy-MM-dd")));
                }

                if (!string.IsNullOrWhiteSpace(txtRegisterTo.Text) && DateTime.TryParse(txtRegisterTo.Text, out DateTime regTo))
                {
                    query += $" AND {_dbHelper.GetColumnName("Date_DataIn")} <= @regTo";
                    parameters.Add(_dbHelper.CreateParameter("@regTo", regTo.ToString("yyyy-MM-dd")));
                }

                if (!string.IsNullOrWhiteSpace(txtSendFrom.Text) && DateTime.TryParse(txtSendFrom.Text, out DateTime sendFrom))
                {
                    query += $" AND {_dbHelper.GetColumnName("Date_Send")} >= @sendFrom";
                    parameters.Add(_dbHelper.CreateParameter("@sendFrom", sendFrom.ToString("yyyy-MM-dd")));
                }

                if (!string.IsNullOrWhiteSpace(txtSendTo.Text) && DateTime.TryParse(txtSendTo.Text, out DateTime sendTo))
                {
                    query += $" AND {_dbHelper.GetColumnName("Date_Send")} <= @sendTo";
                    parameters.Add(_dbHelper.CreateParameter("@sendTo", sendTo.ToString("yyyy-MM-dd")));
                }

                if (!string.IsNullOrWhiteSpace(txtUsedFrom.Text) && DateTime.TryParse(txtUsedFrom.Text, out DateTime useFrom))
                {
                    query += $" AND {_dbHelper.GetColumnName("Date_Use")} >= @useFrom";
                    parameters.Add(_dbHelper.CreateParameter("@useFrom", useFrom.ToString("yyyy-MM-dd")));
                }

                if (!string.IsNullOrWhiteSpace(txtUsedTo.Text) && DateTime.TryParse(txtUsedTo.Text, out DateTime useTo))
                {
                    query += $" AND {_dbHelper.GetColumnName("Date_Use")} <= @useTo";
                    parameters.Add(_dbHelper.CreateParameter("@useTo", useTo.ToString("yyyy-MM-dd")));
                }

                if (!string.IsNullOrEmpty(ddlSendStatus.SelectedValue))
                {
                    query += $" AND {_dbHelper.GetColumnName("Send_Status")} = @sendStatus";
                    parameters.Add(_dbHelper.CreateParameter("@sendStatus", _dbHelper.GetBooleanValue(ddlSendStatus.SelectedValue == "True")));
                }

                if (!string.IsNullOrEmpty(ddlUsedStatus.SelectedValue))
                {
                    query += $" AND {_dbHelper.GetColumnName("Used_Status")} = @usedStatus";
                    parameters.Add(_dbHelper.CreateParameter("@usedStatus", _dbHelper.GetBooleanValue(ddlUsedStatus.SelectedValue == "True")));
                }

                // Execute query
                DataTable dtResults = _dbHelper.ExecuteQuery(query, parameters.ToArray());

                // Store in ViewState for export
                ViewState["SearchResults"] = dtResults;
                ViewState["SearchType"] = "Coupons";

                // Bind to grid
                gvResults.DataSource = dtResults;
                gvResults.DataBind();

                // Show results
                pnlResults.Visible = true;
                lblResultCount.Text = $"Found {dtResults.Rows.Count:N0} coupon(s)";

                // Log search
                UserInfo user = AuthenticationHelper.GetCurrentUser(Session);
                AuditLogger.LogAdminAction(
                    user?.Username ?? "admin",
                    "Coupon Search",
                    "Report",
                    GetClientIpAddress(),
                    $"Found {dtResults.Rows.Count} records"
                );
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error searching coupons", ex);
                lblResultCount.Text = "Search error occurred";
            }
        }

        /// <summary>
        /// Clear coupon search form
        /// </summary>
        protected void btnClearCouponSearch_Click(object sender, EventArgs e)
        {
            txtCouponId.Text = "";
            txtCouponCode.Text = "";
            txtRegisterFrom.Text = "";
            txtRegisterTo.Text = "";
            txtSendFrom.Text = "";
            txtSendTo.Text = "";
            txtUsedFrom.Text = "";
            txtUsedTo.Text = "";
            ddlSendStatus.SelectedIndex = 0;
            ddlUsedStatus.SelectedIndex = 0;
            pnlResults.Visible = false;
            ViewState["SearchResults"] = null;
        }

        #endregion

        #region Logs Search

        /// <summary>
        /// Search access logs
        /// </summary>
        protected void btnSearchLogs_Click(object sender, EventArgs e)
        {
            try
            {
                string query = $"SELECT * FROM {_dbHelper.GetTableName("Logs")} WHERE 1=1";
                var parameters = new System.Collections.Generic.List<DbParameter>();

                if (!string.IsNullOrWhiteSpace(txtLogCouponId.Text))
                {
                    query += $" AND {_dbHelper.GetColumnName("Access_URL")} LIKE @url";
                    parameters.Add(_dbHelper.CreateParameter("@url", "%" + InputValidator.SanitizeInput(txtLogCouponId.Text) + "%"));
                }

                if (chkRedeemAction.Checked)
                {
                    query += $" AND {_dbHelper.GetColumnName("Access_URL")} LIKE @redeem";
                    parameters.Add(_dbHelper.CreateParameter("@redeem", "%Redeem%"));
                }

                if (!string.IsNullOrWhiteSpace(txtLogFrom.Text) && DateTime.TryParse(txtLogFrom.Text, out DateTime logFrom))
                {
                    query += $" AND {_dbHelper.GetColumnName("Created_DateTime")} >= @logFrom";
                    parameters.Add(_dbHelper.CreateParameter("@logFrom", logFrom.ToString("yyyy-MM-dd")));
                }

                if (!string.IsNullOrWhiteSpace(txtLogTo.Text) && DateTime.TryParse(txtLogTo.Text, out DateTime logTo))
                {
                    query += $" AND {_dbHelper.GetColumnName("Created_DateTime")} <= @logTo";
                    parameters.Add(_dbHelper.CreateParameter("@logTo", logTo.ToString("yyyy-MM-dd")));
                }

                query += $" AND {_dbHelper.GetColumnName("Found_Coupon")} = @foundCoupon";
                parameters.Add(_dbHelper.CreateParameter("@foundCoupon", _dbHelper.GetBooleanValue(chkFoundCoupon.Checked)));

                // Execute query
                DataTable dtResults = _dbHelper.ExecuteQuery(query, parameters.ToArray());

                // Store in ViewState
                ViewState["SearchResults"] = dtResults;
                ViewState["SearchType"] = "Logs";

                // Bind to grid
                gvResults.DataSource = dtResults;
                gvResults.DataBind();

                // Show results
                pnlResults.Visible = true;
                lblResultCount.Text = $"Found {dtResults.Rows.Count:N0} log(s)";

                // Log search
                UserInfo user = AuthenticationHelper.GetCurrentUser(Session);
                AuditLogger.LogAdminAction(
                    user?.Username ?? "admin",
                    "Logs Search",
                    "Report",
                    GetClientIpAddress(),
                    $"Found {dtResults.Rows.Count} records"
                );
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error searching logs", ex);
                lblResultCount.Text = "Search error occurred";
            }
        }

        /// <summary>
        /// Clear log search form
        /// </summary>
        protected void btnClearLogSearch_Click(object sender, EventArgs e)
        {
            txtLogFrom.Text = "";
            txtLogTo.Text = "";
            txtLogCouponId.Text = "";
            chkFoundCoupon.Checked = true;
            chkRedeemAction.Checked = false;
            pnlResults.Visible = false;
            ViewState["SearchResults"] = null;
        }

        #endregion

        #region GridView Events

        /// <summary>
        /// Handle GridView paging
        /// </summary>
        protected void gvResults_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvResults.PageIndex = e.NewPageIndex;

            if (ViewState["SearchResults"] != null)
            {
                gvResults.DataSource = (DataTable)ViewState["SearchResults"];
                gvResults.DataBind();
            }
        }

        /// <summary>
        /// Handle GridView sorting
        /// </summary>
        protected void gvResults_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (ViewState["SearchResults"] != null)
            {
                DataTable dt = (DataTable)ViewState["SearchResults"];
                DataView dv = new DataView(dt);

                // Determine sort direction
                string sortDirection = "ASC";
                if (ViewState["SortExpression"] != null && ViewState["SortExpression"].ToString() == e.SortExpression)
                {
                    sortDirection = ViewState["SortDirection"]?.ToString() == "ASC" ? "DESC" : "ASC";
                }

                ViewState["SortExpression"] = e.SortExpression;
                ViewState["SortDirection"] = sortDirection;

                dv.Sort = $"{e.SortExpression} {sortDirection}";
                gvResults.DataSource = dv;
                gvResults.DataBind();
            }
        }

        #endregion

        #region Export Functionality

        /// <summary>
        /// Export coupons to Excel
        /// </summary>
        protected void btnExportCoupons_Click(object sender, EventArgs e)
        {
            if (ViewState["SearchResults"] != null)
            {
                DataTable dt = (DataTable)ViewState["SearchResults"];
                ExportToExcel(dt, "CouponReport");

                // Log export
                UserInfo user = AuthenticationHelper.GetCurrentUser(Session);
                AuditLogger.LogAdminAction(
                    user?.Username ?? "admin",
                    "Export Coupons",
                    "Report",
                    GetClientIpAddress(),
                    $"Exported {dt.Rows.Count} coupon records"
                );
            }
        }

        /// <summary>
        /// Export logs to Excel
        /// </summary>
        protected void btnExportLogs_Click(object sender, EventArgs e)
        {
            if (ViewState["SearchResults"] != null)
            {
                DataTable dt = (DataTable)ViewState["SearchResults"];
                ExportToExcel(dt, "AccessLogsReport");

                // Log export
                UserInfo user = AuthenticationHelper.GetCurrentUser(Session);
                AuditLogger.LogAdminAction(
                    user?.Username ?? "admin",
                    "Export Logs",
                    "Report",
                    GetClientIpAddress(),
                    $"Exported {dt.Rows.Count} log records"
                );
            }
        }

        /// <summary>
        /// Export DataTable to Excel (CSV format)
        /// </summary>
        private void ExportToExcel(DataTable dt, string filename)
        {
            try
            {
                Response.Clear();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", $"attachment;filename={filename}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                Response.Charset = "";
                Response.ContentType = "application/text";
                Response.ContentEncoding = Encoding.UTF8;
                // Write UTF-8 BOM for Excel compatibility
                Response.BinaryWrite(Encoding.UTF8.GetPreamble());

                StringBuilder sb = new StringBuilder();

                // Add column headers
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    sb.Append(EscapeCsvField(dt.Columns[i].ColumnName));
                    if (i < dt.Columns.Count - 1)
                        sb.Append(",");
                }
                sb.AppendLine();

                // Add data rows
                foreach (DataRow row in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        sb.Append(EscapeCsvField(row[i].ToString()));
                        if (i < dt.Columns.Count - 1)
                            sb.Append(",");
                    }
                    sb.AppendLine();
                }

                Response.Output.Write(sb.ToString());
                Response.Flush();
                Response.End();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error exporting to Excel", ex);
            }
        }

        /// <summary>
        /// Escape CSV field to handle commas and quotes
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        #endregion
    }
}
