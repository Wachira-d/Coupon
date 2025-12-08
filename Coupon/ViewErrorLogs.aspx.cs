using System;
using System.Data;
using System.Text;
using System.Web.UI;
using Coupon.Helpers;

namespace Coupon
{
    public partial class ViewErrorLogs : Page
    {
        private DatabaseHelper _dbHelper = new DatabaseHelper();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadStatistics();
                LoadLogs();
            }
        }

        private void LoadStatistics()
        {
            try
            {
                // Get counts by log level
                string queryError = $"SELECT COUNT(*) FROM {_dbHelper.GetTableName("ErrorLogs")} WHERE {_dbHelper.GetColumnName("LogLevel")} = 'ERROR'";
                string queryWarning = $"SELECT COUNT(*) FROM {_dbHelper.GetTableName("ErrorLogs")} WHERE {_dbHelper.GetColumnName("LogLevel")} = 'WARNING'";
                string queryInfo = $"SELECT COUNT(*) FROM {_dbHelper.GetTableName("ErrorLogs")} WHERE {_dbHelper.GetColumnName("LogLevel")} = 'INFO'";
                string queryTotal = $"SELECT COUNT(*) FROM {_dbHelper.GetTableName("ErrorLogs")}";

                lblErrorCount.Text = _dbHelper.ExecuteScalar(queryError)?.ToString() ?? "0";
                lblWarningCount.Text = _dbHelper.ExecuteScalar(queryWarning)?.ToString() ?? "0";
                lblInfoCount.Text = _dbHelper.ExecuteScalar(queryInfo)?.ToString() ?? "0";
                lblTotalCount.Text = _dbHelper.ExecuteScalar(queryTotal)?.ToString() ?? "0";
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error loading statistics", ex);
                litResults.Text = "<div class='no-data'>Error loading statistics. Please check if ErrorLogs table exists.</div>";
            }
        }

        private void LoadLogs()
        {
            try
            {
                // Build query with filters
                StringBuilder query = new StringBuilder();
                query.Append($"SELECT TOP {ddlRecordCount.SelectedValue} * FROM {_dbHelper.GetTableName("ErrorLogs")} WHERE 1=1");

                // Add log level filter
                if (!string.IsNullOrEmpty(ddlLogLevel.SelectedValue))
                {
                    query.Append($" AND {_dbHelper.GetColumnName("LogLevel")} = '{ddlLogLevel.SelectedValue}'");
                }

                // Add time range filter
                if (ddlTimeRange.SelectedValue != "0")
                {
                    int hours = int.Parse(ddlTimeRange.SelectedValue);
                    DateTime cutoffTime = DateTime.Now.AddHours(-hours);
                    query.Append($" AND {_dbHelper.GetColumnName("CreatedDateTime")} >= '{cutoffTime:yyyy-MM-dd HH:mm:ss}'");
                }

                // Add IP filter
                if (!string.IsNullOrEmpty(txtIPFilter.Text.Trim()))
                {
                    query.Append($" AND {_dbHelper.GetColumnName("UserIP")} LIKE '%{txtIPFilter.Text.Trim()}%'");
                }

                query.Append($" ORDER BY {_dbHelper.GetColumnName("CreatedDateTime")} DESC");

                DataTable dt = _dbHelper.ExecuteQuery(query.ToString());

                if (dt.Rows.Count == 0)
                {
                    litResults.Text = "<div class='no-data'>No logs found matching the selected filters.</div>";
                    return;
                }

                // Build HTML table
                StringBuilder html = new StringBuilder();
                html.Append("<table class='log-table'>");
                html.Append("<thead><tr>");
                html.Append("<th>ID</th>");
                html.Append("<th>Level</th>");
                html.Append("<th>Time</th>");
                html.Append("<th>Message</th>");
                html.Append("<th>IP</th>");
                html.Append("<th>Details</th>");
                html.Append("</tr></thead>");
                html.Append("<tbody>");

                foreach (DataRow row in dt.Rows)
                {
                    string logId = row["LogId"].ToString();
                    string logLevel = row["LogLevel"].ToString();
                    string createdDateTime = Convert.ToDateTime(row["CreatedDateTime"]).ToString("yyyy-MM-dd HH:mm:ss");
                    string message = System.Web.HttpUtility.HtmlEncode(row["Message"].ToString());
                    string userIP = row["UserIP"]?.ToString() ?? "-";

                    html.Append("<tr>");
                    html.Append($"<td>{logId}</td>");
                    html.Append($"<td><span class='log-level {logLevel}'>{logLevel}</span></td>");
                    html.Append($"<td>{createdDateTime}</td>");
                    html.Append($"<td>{TruncateWithEllipsis(message, 80)}</td>");
                    html.Append($"<td>{userIP}</td>");
                    html.Append($"<td><span class='expandable' onclick='toggleDetails({logId})'>View Details</span></td>");
                    html.Append("</tr>");

                    // Details row (hidden by default)
                    html.Append("<tr>");
                    html.Append($"<td colspan='6'><div class='details' id='details_{logId}'>");
                    html.Append("<pre><strong>Message:</strong> " + message + "\n");

                    if (!string.IsNullOrEmpty(row["ExceptionType"]?.ToString()))
                    {
                        html.Append("<strong>Exception Type:</strong> " + System.Web.HttpUtility.HtmlEncode(row["ExceptionType"].ToString()) + "\n");
                    }

                    if (!string.IsNullOrEmpty(row["ExceptionMessage"]?.ToString()))
                    {
                        html.Append("<strong>Exception Message:</strong> " + System.Web.HttpUtility.HtmlEncode(row["ExceptionMessage"].ToString()) + "\n");
                    }

                    if (!string.IsNullOrEmpty(row["StackTrace"]?.ToString()))
                    {
                        html.Append("<strong>Stack Trace:</strong>\n" + System.Web.HttpUtility.HtmlEncode(row["StackTrace"].ToString()) + "\n");
                    }

                    if (!string.IsNullOrEmpty(row["InnerException"]?.ToString()))
                    {
                        html.Append("<strong>Inner Exception:</strong> " + System.Web.HttpUtility.HtmlEncode(row["InnerException"].ToString()) + "\n");
                    }

                    if (!string.IsNullOrEmpty(row["AdditionalInfo"]?.ToString()))
                    {
                        html.Append("<strong>Additional Info:</strong> " + System.Web.HttpUtility.HtmlEncode(row["AdditionalInfo"].ToString()) + "\n");
                    }

                    if (!string.IsNullOrEmpty(row["RequestUrl"]?.ToString()))
                    {
                        html.Append("<strong>Request URL:</strong> " + System.Web.HttpUtility.HtmlEncode(row["RequestUrl"].ToString()) + "\n");
                    }

                    if (!string.IsNullOrEmpty(row["UserAgent"]?.ToString()))
                    {
                        html.Append("<strong>User Agent:</strong> " + System.Web.HttpUtility.HtmlEncode(row["UserAgent"].ToString()) + "\n");
                    }

                    if (!string.IsNullOrEmpty(row["ServerName"]?.ToString()))
                    {
                        html.Append("<strong>Server:</strong> " + System.Web.HttpUtility.HtmlEncode(row["ServerName"].ToString()) + "\n");
                    }

                    html.Append("</pre></div></td>");
                    html.Append("</tr>");
                }

                html.Append("</tbody>");
                html.Append("</table>");

                litResults.Text = html.ToString();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error loading logs", ex);
                litResults.Text = $"<div class='no-data'>Error loading logs: {System.Web.HttpUtility.HtmlEncode(ex.Message)}<br>Please ensure the ErrorLogs table exists in the database.</div>";
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            LoadStatistics();
            LoadLogs();
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            // Reset filters
            ddlLogLevel.SelectedIndex = 0;
            ddlTimeRange.SelectedIndex = 0;
            txtIPFilter.Text = "";
            ddlRecordCount.SelectedIndex = 0;

            LoadStatistics();
            LoadLogs();
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                // Build query (same as LoadLogs but no TOP limit for export)
                StringBuilder query = new StringBuilder();
                query.Append($"SELECT * FROM {_dbHelper.GetTableName("ErrorLogs")} WHERE 1=1");

                if (!string.IsNullOrEmpty(ddlLogLevel.SelectedValue))
                {
                    query.Append($" AND {_dbHelper.GetColumnName("LogLevel")} = '{ddlLogLevel.SelectedValue}'");
                }

                if (ddlTimeRange.SelectedValue != "0")
                {
                    int hours = int.Parse(ddlTimeRange.SelectedValue);
                    DateTime cutoffTime = DateTime.Now.AddHours(-hours);
                    query.Append($" AND {_dbHelper.GetColumnName("CreatedDateTime")} >= '{cutoffTime:yyyy-MM-dd HH:mm:ss}'");
                }

                if (!string.IsNullOrEmpty(txtIPFilter.Text.Trim()))
                {
                    query.Append($" AND {_dbHelper.GetColumnName("UserIP")} LIKE '%{txtIPFilter.Text.Trim()}%'");
                }

                query.Append($" ORDER BY {_dbHelper.GetColumnName("CreatedDateTime")} DESC");

                DataTable dt = _dbHelper.ExecuteQuery(query.ToString());

                // Create CSV
                StringBuilder csv = new StringBuilder();

                // Header
                csv.AppendLine("LogId,LogLevel,CreatedDateTime,Message,ExceptionType,ExceptionMessage,UserIP,RequestUrl");

                // Data
                foreach (DataRow row in dt.Rows)
                {
                    csv.Append(CsvEncode(row["LogId"].ToString()) + ",");
                    csv.Append(CsvEncode(row["LogLevel"].ToString()) + ",");
                    csv.Append(CsvEncode(row["CreatedDateTime"].ToString()) + ",");
                    csv.Append(CsvEncode(row["Message"].ToString()) + ",");
                    csv.Append(CsvEncode(row["ExceptionType"]?.ToString()) + ",");
                    csv.Append(CsvEncode(row["ExceptionMessage"]?.ToString()) + ",");
                    csv.Append(CsvEncode(row["UserIP"]?.ToString()) + ",");
                    csv.Append(CsvEncode(row["RequestUrl"]?.ToString()));
                    csv.AppendLine();
                }

                // Download
                Response.Clear();
                Response.ContentType = "text/csv";
                Response.AddHeader("Content-Disposition", $"attachment;filename=ErrorLogs_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                Response.Write(csv.ToString());
                Response.End();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error exporting logs", ex);
                ClientScript.RegisterStartupScript(this.GetType(), "alert", $"alert('Export failed: {ex.Message}');", true);
            }
        }

        protected void btnDeleteOld_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime cutoffDate = DateTime.Now.AddDays(-30);
                string deleteQuery = $"DELETE FROM {_dbHelper.GetTableName("ErrorLogs")} WHERE {_dbHelper.GetColumnName("CreatedDateTime")} < '{cutoffDate:yyyy-MM-dd HH:mm:ss}'";

                int deletedCount = _dbHelper.ExecuteNonQuery(deleteQuery);

                LoadStatistics();
                LoadLogs();

                ClientScript.RegisterStartupScript(this.GetType(), "alert", $"alert('Successfully deleted {deletedCount} old log records.');", true);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error deleting old logs", ex);
                ClientScript.RegisterStartupScript(this.GetType(), "alert", $"alert('Delete failed: {ex.Message}');", true);
            }
        }

        private string TruncateWithEllipsis(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }

        private string CsvEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            // Escape quotes and wrap in quotes
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }
    }
}
