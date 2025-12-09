using System;
using System.IO;
using System.Web.UI;
using System.Text;
using Coupon.Helpers;

namespace Coupon
{
    public partial class TestLogs : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Check if this is localhost or development
                if (!Request.IsLocal && SecureConfig.IsProduction())
                {
                    Response.Write("This page is only available in development environment.");
                    Response.End();
                }
            }
        }

        #region Error Logging Tests

        protected void btnTestError_Click(object sender, EventArgs e)
        {
            try
            {
                ErrorLogger.LogError("Test Error Message",
                    new Exception("This is a test exception from TestLogs.aspx"),
                    $"Test IP: {Request.UserHostAddress}");

                lblErrorResult.Text = "[OK] SUCCESS: Error log written successfully!\n\n";
                lblErrorResult.Text += "Check log file at:\n";
                lblErrorResult.Text += GetLogPath("Error");
                lblErrorResult.CssClass = "result success";
                lblErrorResult.Visible = true;
            }
            catch (Exception ex)
            {
                lblErrorResult.Text = "[FAILED]: " + ex.Message + "\n\n";
                lblErrorResult.Text += ex.StackTrace;
                lblErrorResult.CssClass = "result error";
                lblErrorResult.Visible = true;
            }
        }

        protected void btnTestWarning_Click(object sender, EventArgs e)
        {
            try
            {
                ErrorLogger.LogWarning("Test Warning Message",
                    $"Test warning from TestLogs.aspx, IP: {Request.UserHostAddress}");

                lblErrorResult.Text = "[OK] SUCCESS: Warning log written successfully!\n\n";
                lblErrorResult.Text += "Check log file at:\n";
                lblErrorResult.Text += GetLogPath("Warning");
                lblErrorResult.CssClass = "result success";
                lblErrorResult.Visible = true;
            }
            catch (Exception ex)
            {
                lblErrorResult.Text = "[FAILED]: " + ex.Message + "\n\n";
                lblErrorResult.Text += ex.StackTrace;
                lblErrorResult.CssClass = "result error";
                lblErrorResult.Visible = true;
            }
        }

        protected void btnTestInfo_Click(object sender, EventArgs e)
        {
            try
            {
                ErrorLogger.LogInfo("Test Info Message",
                    $"Test info log from TestLogs.aspx, IP: {Request.UserHostAddress}");

                lblErrorResult.Text = "[OK] SUCCESS: Info log written successfully!\n\n";
                lblErrorResult.Text += "Check log file at:\n";
                lblErrorResult.Text += GetLogPath("Info");
                lblErrorResult.CssClass = "result success";
                lblErrorResult.Visible = true;
            }
            catch (Exception ex)
            {
                lblErrorResult.Text = "[FAILED]: " + ex.Message + "\n\n";
                lblErrorResult.Text += ex.StackTrace;
                lblErrorResult.CssClass = "result error";
                lblErrorResult.Visible = true;
            }
        }

        #endregion

        #region Audit Logging Tests

        protected void btnTestAuth_Click(object sender, EventArgs e)
        {
            try
            {
                AuditLogger.LogAuthentication(
                    "test.user@example.com",
                    true,
                    Request.UserHostAddress,
                    "Test authentication log from TestLogs.aspx");

                lblAuditResult.Text = "[OK] SUCCESS: Authentication log written successfully!\n\n";
                lblAuditResult.Text += "Check log file at:\n";
                lblAuditResult.Text += GetAuditLogPath();
                lblAuditResult.CssClass = "result success";
                lblAuditResult.Visible = true;
            }
            catch (Exception ex)
            {
                lblAuditResult.Text = "[FAILED]: " + ex.Message + "\n\n";
                lblAuditResult.Text += ex.StackTrace;
                lblAuditResult.CssClass = "result error";
                lblAuditResult.Visible = true;
            }
        }

        protected void btnTestSecurity_Click(object sender, EventArgs e)
        {
            try
            {
                AuditLogger.LogSecurityEvent(
                    "Test Security Event",
                    Request.UserHostAddress,
                    "This is a test security event from TestLogs.aspx",
                    SecuritySeverity.Medium);

                lblAuditResult.Text = "[OK] SUCCESS: Security event log written successfully!\n\n";
                lblAuditResult.Text += "Check log file at:\n";
                lblAuditResult.Text += GetAuditLogPath();
                lblAuditResult.CssClass = "result success";
                lblAuditResult.Visible = true;
            }
            catch (Exception ex)
            {
                lblAuditResult.Text = "[FAILED]: " + ex.Message + "\n\n";
                lblAuditResult.Text += ex.StackTrace;
                lblAuditResult.CssClass = "result error";
                lblAuditResult.Visible = true;
            }
        }

        protected void btnTestCoupon_Click(object sender, EventArgs e)
        {
            try
            {
                AuditLogger.LogCouponRedemption(
                    "TEST123",
                    "Central",
                    200,
                    Request.UserHostAddress);

                lblAuditResult.Text = "[OK] SUCCESS: Coupon redemption log written successfully!\n\n";
                lblAuditResult.Text += "Check log file at:\n";
                lblAuditResult.Text += GetAuditLogPath();
                lblAuditResult.CssClass = "result success";
                lblAuditResult.Visible = true;
            }
            catch (Exception ex)
            {
                lblAuditResult.Text = "[FAILED]: " + ex.Message + "\n\n";
                lblAuditResult.Text += ex.StackTrace;
                lblAuditResult.CssClass = "result error";
                lblAuditResult.Visible = true;
            }
        }

        #endregion

        #region Folder Check

        protected void btnCheckFolders_Click(object sender, EventArgs e)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("FOLDER STATUS CHECK\n");
            result.AppendLine("═══════════════════════════════════════\n");

            string basePath = Server.MapPath("~/App_Data");
            string logsPath = Path.Combine(basePath, "Logs");
            string auditLogsPath = Path.Combine(basePath, "AuditLogs");

            // Check App_Data
            CheckFolder(result, "App_Data", basePath);

            // Check Logs
            CheckFolder(result, "Logs", logsPath);

            // Check AuditLogs
            CheckFolder(result, "AuditLogs", auditLogsPath);

            // Check Barcode folder
            string barcodePath = Server.MapPath("~/Barcode");
            CheckFolder(result, "Barcode", barcodePath);

            // Try to create test file
            result.AppendLine("\nWRITE PERMISSION TEST\n");
            result.AppendLine("═══════════════════════════════════════\n");

            try
            {
                string testFile = Path.Combine(logsPath, "test_write.txt");
                File.WriteAllText(testFile, "Test write at " + DateTime.Now);
                File.Delete(testFile);
                result.AppendLine("[OK] Logs folder: WRITABLE");
            }
            catch (Exception ex)
            {
                result.AppendLine("[ERROR] Logs folder: NOT WRITABLE");
                result.AppendLine("   Error: " + ex.Message);
            }

            try
            {
                string testFile = Path.Combine(auditLogsPath, "test_write.txt");
                File.WriteAllText(testFile, "Test write at " + DateTime.Now);
                File.Delete(testFile);
                result.AppendLine("[OK] AuditLogs folder: WRITABLE");
            }
            catch (Exception ex)
            {
                result.AppendLine("[ERROR] AuditLogs folder: NOT WRITABLE");
                result.AppendLine("   Error: " + ex.Message);
            }

            lblFolderResult.Text = result.ToString();
            lblFolderResult.CssClass = "result info";
            lblFolderResult.Visible = true;
        }

        private void CheckFolder(StringBuilder result, string name, string path)
        {
            result.Append($"[{name}]: ");

            if (Directory.Exists(path))
            {
                result.AppendLine("[OK] EXISTS");
                result.AppendLine($"   Path: {path}");

                // List files
                var files = Directory.GetFiles(path, "*.log");
                result.AppendLine($"   Files: {files.Length} log files");

                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        var fi = new FileInfo(file);
                        result.AppendLine($"   - {Path.GetFileName(file)} ({fi.Length} bytes, {fi.LastWriteTime})");
                    }
                }
            }
            else
            {
                result.AppendLine("[MISSING]");
                result.AppendLine($"   Expected path: {path}");

                // Try to create
                try
                {
                    Directory.CreateDirectory(path);
                    result.AppendLine("   [OK] Created successfully!");
                }
                catch (Exception ex)
                {
                    result.AppendLine($"   [ERROR] Cannot create: {ex.Message}");
                }
            }

            result.AppendLine();
        }

        #endregion

        #region Log Viewer

        protected void btnViewError_Click(object sender, EventArgs e)
        {
            ViewLogFile("Error");
        }

        protected void btnViewAudit_Click(object sender, EventArgs e)
        {
            ViewAuditLogFile();
        }

        private void ViewLogFile(string logType)
        {
            try
            {
                string logPath = GetLogPath(logType);

                if (File.Exists(logPath))
                {
                    string[] lines = File.ReadAllLines(logPath);
                    int linesToShow = Math.Min(50, lines.Length);

                    StringBuilder content = new StringBuilder();
                    content.AppendLine($"=== Last {linesToShow} lines of {logType} log ===\n");
                    content.AppendLine($"File: {logPath}\n");
                    content.AppendLine($"Total lines: {lines.Length}\n");
                    content.AppendLine("═══════════════════════════════════════════════════\n");

                    for (int i = Math.Max(0, lines.Length - linesToShow); i < lines.Length; i++)
                    {
                        content.AppendLine(lines[i]);
                    }

                    lblLogViewer.Text = content.ToString();
                    lblLogViewer.Visible = true;
                }
                else
                {
                    lblLogViewer.Text = $"[ERROR] Log file not found: {logPath}";
                    lblLogViewer.Visible = true;
                }
            }
            catch (Exception ex)
            {
                lblLogViewer.Text = $"[ERROR] Error reading log file: {ex.Message}";
                lblLogViewer.Visible = true;
            }
        }

        private void ViewAuditLogFile()
        {
            try
            {
                string logPath = GetAuditLogPath();

                if (File.Exists(logPath))
                {
                    string[] lines = File.ReadAllLines(logPath);
                    int linesToShow = Math.Min(50, lines.Length);

                    StringBuilder content = new StringBuilder();
                    content.AppendLine($"=== Last {linesToShow} lines of Audit log ===\n");
                    content.AppendLine($"File: {logPath}\n");
                    content.AppendLine($"Total lines: {lines.Length}\n");
                    content.AppendLine("═══════════════════════════════════════════════════\n");

                    for (int i = Math.Max(0, lines.Length - linesToShow); i < lines.Length; i++)
                    {
                        content.AppendLine(lines[i]);
                    }

                    lblLogViewer.Text = content.ToString();
                    lblLogViewer.Visible = true;
                }
                else
                {
                    lblLogViewer.Text = $"[ERROR] Log file not found: {logPath}";
                    lblLogViewer.Visible = true;
                }
            }
            catch (Exception ex)
            {
                lblLogViewer.Text = $"[ERROR] Error reading log file: {ex.Message}";
                lblLogViewer.Visible = true;
            }
        }

        #endregion

        #region System Information

        protected void btnSystemInfo_Click(object sender, EventArgs e)
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine("🖥️ SYSTEM INFORMATION\n");
            info.AppendLine("═══════════════════════════════════════\n");

            info.AppendLine($"Server Time: {DateTime.Now}");
            info.AppendLine($"Machine Name: {Environment.MachineName}");
            info.AppendLine($"OS Version: {Environment.OSVersion}");
            info.AppendLine($".NET Version: {Environment.Version}");
            info.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            info.AppendLine($"Application Path: {Server.MapPath("~/")}");
            info.AppendLine($"Is 64-bit: {Environment.Is64BitOperatingSystem}");
            info.AppendLine($"Processor Count: {Environment.ProcessorCount}");
            info.AppendLine();

            info.AppendLine("📍 REQUEST INFORMATION\n");
            info.AppendLine("═══════════════════════════════════════\n");
            info.AppendLine($"IP Address: {Request.UserHostAddress}");
            info.AppendLine($"User Agent: {Request.UserAgent}");
            info.AppendLine($"Request URL: {Request.Url}");
            info.AppendLine($"Is Local: {Request.IsLocal}");
            info.AppendLine($"Is Secure: {Request.IsSecureConnection}");
            info.AppendLine();

            info.AppendLine("⚙️ CONFIGURATION\n");
            info.AppendLine("═══════════════════════════════════════\n");
            try
            {
                info.AppendLine($"Database Type: {SecureConfig.GetDatabaseType()}");
                info.AppendLine($"Is Production: {SecureConfig.IsProduction()}");
                info.AppendLine($"Session Timeout: {SecureConfig.GetSessionTimeoutMinutes()} minutes");
                var rateLimit = SecureConfig.GetRateLimitConfig();
                info.AppendLine($"Rate Limit: {rateLimit.MaxAttempts} attempts / {rateLimit.WindowMinutes} minutes");
            }
            catch (Exception ex)
            {
                info.AppendLine($"Error reading config: {ex.Message}");
            }

            lblSystemInfo.Text = info.ToString();
            lblSystemInfo.CssClass = "result info";
            lblSystemInfo.Visible = true;
        }

        #endregion

        #region Helper Methods

        private string GetLogPath(string logType)
        {
            string basePath = Server.MapPath("~/App_Data/Logs");
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(basePath, $"{logType}_{dateStr}.log");
        }

        private string GetAuditLogPath()
        {
            string basePath = Server.MapPath("~/App_Data/AuditLogs");
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(basePath, $"Audit_{dateStr}.log");
        }

        #endregion
    }
}
