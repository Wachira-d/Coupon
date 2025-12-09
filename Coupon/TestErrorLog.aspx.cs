using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Web.UI;
using Coupon.Helpers;

namespace Coupon
{
    public partial class TestErrorLog : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                litConfig.Text = "<div class='result'>Click 'Check Config' to see LogPath configuration</div>";
            }
        }

        protected void btnCheckConfig_Click(object sender, EventArgs e)
        {
            try
            {
                string logPath = ConfigurationManager.AppSettings["LogPath"];
                string html = "<div class='result success'><h3>Configuration Details:</h3><pre>";

                html += $"LogPath from Web.config: {(string.IsNullOrEmpty(logPath) ? "(not set)" : logPath)}\n\n";

                // Check if it's a file or folder
                if (!string.IsNullOrEmpty(logPath))
                {
                    string extension = Path.GetExtension(logPath);
                    bool isFile = !string.IsNullOrEmpty(extension);

                    html += $"Type: {(isFile ? "File" : "Folder")}\n";
                    html += $"Extension: {(string.IsNullOrEmpty(extension) ? "(none)" : extension)}\n\n";

                    if (isFile)
                    {
                        string directory = Path.GetDirectoryName(logPath);
                        html += $"Directory: {directory}\n";
                        html += $"File Name: {Path.GetFileName(logPath)}\n\n";

                        // Check directory exists
                        if (!string.IsNullOrEmpty(directory))
                        {
                            html += $"Directory Exists: {Directory.Exists(directory)}\n";
                            if (!Directory.Exists(directory))
                            {
                                html += "[Warning] Directory does not exist! ErrorLogger should create it.\n";
                            }
                        }

                        // Check file exists
                        html += $"File Exists: {File.Exists(logPath)}\n";
                    }
                    else
                    {
                        html += $"Directory Exists: {Directory.Exists(logPath)}\n";
                    }
                }
                else
                {
                    html += "[Warning] LogPath not configured - will use default App_Data/Logs\n";
                    string defaultPath = Server.MapPath("~/App_Data/Logs");
                    html += $"Default Path: {defaultPath}\n";
                    html += $"Default Path Exists: {Directory.Exists(defaultPath)}\n";
                }

                // Current user context
                html += $"\nCurrent User Identity: {System.Security.Principal.WindowsIdentity.GetCurrent().Name}\n";
                html += $"Is IIS User: {Request.IsAuthenticated}\n";

                html += "</pre></div>";
                litConfig.Text = html;
            }
            catch (Exception ex)
            {
                litConfig.Text = $"<div class='result error'><h3>Error:</h3><pre>{ex.Message}\n{ex.StackTrace}</pre></div>";
            }
        }

        protected void btnTestLog_Click(object sender, EventArgs e)
        {
            try
            {
                string testMessage = $"Test log write at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                Exception testException = new Exception("This is a test exception from TestErrorLog.aspx");
                string additionalInfo = $"User: {Request.UserHostAddress}, Test: Permission Check";

                // Call ErrorLogger
                ErrorLogger.LogError(testMessage, testException, additionalInfo);

                string logPath = ConfigurationManager.AppSettings["LogPath"];
                string html = "<div class='result success'><h3>Log Write Successful!</h3><pre>";
                html += $"Called: ErrorLogger.LogError()\n";
                html += $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                html += $"Message: {testMessage}\n\n";

                if (!string.IsNullOrEmpty(logPath))
                {
                    string extension = Path.GetExtension(logPath);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        // Fixed file path
                        html += $"Expected log file: {logPath}\n";
                        if (File.Exists(logPath))
                        {
                            FileInfo fi = new FileInfo(logPath);
                            html += $"File size: {fi.Length} bytes\n";
                            html += $"Last modified: {fi.LastWriteTime}\n";
                            html += "\n[OK] File exists and was written successfully!";
                        }
                        else
                        {
                            html += "\n[Warning] Log file does not exist yet. Check permissions!";
                        }
                    }
                    else
                    {
                        // Folder path
                        string expectedFile = Path.Combine(logPath, $"Error_{DateTime.Now:yyyyMMdd}.log");
                        html += $"Expected log file: {expectedFile}\n";
                        if (File.Exists(expectedFile))
                        {
                            FileInfo fi = new FileInfo(expectedFile);
                            html += $"File size: {fi.Length} bytes\n";
                            html += $"Last modified: {fi.LastWriteTime}\n";
                            html += "\n[OK] File exists and was written successfully!";
                        }
                        else
                        {
                            html += "\n[Warning] Log file does not exist yet. Check permissions!";
                        }
                    }
                }

                html += "</pre></div>";
                litLogTest.Text = html;
            }
            catch (Exception ex)
            {
                litLogTest.Text = $"<div class='result error'><h3>Error during test:</h3><pre>{ex.Message}\n\n{ex.StackTrace}</pre></div>";
            }
        }

        protected void btnTestPermission_Click(object sender, EventArgs e)
        {
            try
            {
                string logPath = ConfigurationManager.AppSettings["LogPath"];
                string testPath = "";
                string html = "<div class='result'><h3>Permission Test Results:</h3><pre>";

                if (!string.IsNullOrEmpty(logPath))
                {
                    string extension = Path.GetExtension(logPath);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        // File path - test directory
                        testPath = Path.GetDirectoryName(logPath);
                    }
                    else
                    {
                        // Folder path
                        testPath = logPath;
                    }
                }
                else
                {
                    testPath = Server.MapPath("~/App_Data/Logs");
                }

                html += $"Testing path: {testPath}\n\n";

                // Ensure directory exists
                if (!Directory.Exists(testPath))
                {
                    html += "Creating directory...\n";
                    Directory.CreateDirectory(testPath);
                    html += "[OK] Directory created successfully\n\n";
                }
                else
                {
                    html += "[OK] Directory exists\n\n";
                }

                // Test write permission
                string testFile = Path.Combine(testPath, $"permission_test_{DateTime.Now.Ticks}.txt");
                html += $"Writing test file: {testFile}\n";

                File.WriteAllText(testFile, $"Permission test at {DateTime.Now}");
                html += "[OK] Write successful\n\n";

                // Read it back
                string content = File.ReadAllText(testFile);
                html += "[OK] Read successful\n\n";

                // Delete test file
                File.Delete(testFile);
                html += "[OK] Delete successful\n\n";

                html += "All permission tests passed!\n";
                html += "The application has full read/write access to this location.\n";

                html += "</pre></div>";
                litPermission.Text = html;
            }
            catch (UnauthorizedAccessException ex)
            {
                litPermission.Text = $"<div class='result error'><h3>Permission Denied!</h3><pre>" +
                    $"The IIS application pool identity does not have write access.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"To fix this, run this PowerShell command as Administrator:\n\n" +
                    $"$path = \"{ConfigurationManager.AppSettings["LogPath"]}\"\n" +
                    $"$directory = Split-Path $path -Parent\n" +
                    $"icacls $directory /grant \"IIS_IUSRS:(OI)(CI)M\" /T\n" +
                    $"icacls $directory /grant \"NETWORK SERVICE:(OI)(CI)M\" /T\n" +
                    $"</pre></div>";
            }
            catch (Exception ex)
            {
                litPermission.Text = $"<div class='result error'><h3>Error:</h3><pre>{ex.Message}\n\n{ex.StackTrace}</pre></div>";
            }
        }

        protected void btnReadLog_Click(object sender, EventArgs e)
        {
            try
            {
                string logPath = ConfigurationManager.AppSettings["LogPath"];
                string fileToRead = "";

                if (!string.IsNullOrEmpty(logPath))
                {
                    string extension = Path.GetExtension(logPath);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        // Fixed file path
                        fileToRead = logPath;
                    }
                    else
                    {
                        // Folder path - read today's error log
                        fileToRead = Path.Combine(logPath, $"Error_{DateTime.Now:yyyyMMdd}.log");
                    }
                }
                else
                {
                    string defaultPath = Server.MapPath("~/App_Data/Logs");
                    fileToRead = Path.Combine(defaultPath, $"Error_{DateTime.Now:yyyyMMdd}.log");
                }

                string html = $"<div class='result'><h3>Reading: {fileToRead}</h3>";

                if (!File.Exists(fileToRead))
                {
                    html += "<pre>[Warning] Log file does not exist yet.\nNo logs have been written today, or the file path is incorrect.</pre></div>";
                    litLogContent.Text = html;
                    return;
                }

                // Read last 50 lines
                string[] allLines = File.ReadAllLines(fileToRead);
                int startLine = Math.Max(0, allLines.Length - 50);
                string[] lastLines = allLines.Skip(startLine).ToArray();

                FileInfo fi = new FileInfo(fileToRead);
                html += $"<p>File Size: {fi.Length} bytes | Last Modified: {fi.LastWriteTime} | Showing last {lastLines.Length} lines</p>";
                html += "<pre>" + string.Join("\n", lastLines).Replace("<", "&lt;").Replace(">", "&gt;") + "</pre></div>";

                litLogContent.Text = html;
            }
            catch (Exception ex)
            {
                litLogContent.Text = $"<div class='result error'><h3>Error reading log:</h3><pre>{ex.Message}\n\n{ex.StackTrace}</pre></div>";
            }
        }
    }
}
