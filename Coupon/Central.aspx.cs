using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using BarcodeLib;
using BarcodeStandard;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data.SqlClient;
using Npgsql;
using System.Configuration;
using Coupon.Helpers;

namespace Coupon
{
    public partial class Central : Page
    {
        private DatabaseHelper _dbHelper = new DatabaseHelper();
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                string ipAddress = Request.UserHostAddress;

                // SECURITY: Check rate limiting before processing
                var rateLimitConfig = SecureConfig.GetRateLimitConfig();
                if (!RateLimiter.IsAllowed(ipAddress, rateLimitConfig.MaxAttempts, rateLimitConfig.WindowMinutes))
                {
                    ErrorLogger.LogWarning("Rate limit exceeded", $"IP: {ipAddress}, URL: {Request.Url}");
                    AuditLogger.LogSecurityEvent(
                        "Rate Limit Exceeded",
                        ipAddress,
                        $"URL: {Request.Url}, Max attempts: {rateLimitConfig.MaxAttempts}",
                        SecuritySeverity.Medium
                    );
                    ShowError("คุณมีความเสี่ยงที่จะโจมตีระบบ กรุณาติดต่อผู้ให้บริการ");
                    return;
                }

                // SECURITY: Validate and sanitize input before processing
                string id = InputValidator.GetSafeQueryStringParameter(Request, "id", 20);

                if (string.IsNullOrEmpty(id))
                {
                    ErrorLogger.LogWarning("Central page accessed without valid coupon ID", $"URL: {Request.Url}");
                    RateLimiter.RecordAttempt(ipAddress); // Record failed attempt
                    ShowError("รหัสคูปองไม่ถูกต้อง");
                    return;
                }

                // SECURITY: Validate coupon ID format to prevent injection attacks
                int expectedLength = SecureConfig.GetCouponIdLength();
                if (!InputValidator.IsValidCouponId(id, expectedLength))
                {
                    ErrorLogger.LogWarning("Invalid coupon ID format", $"ID: {id}, IP: {ipAddress}");
                    RateLimiter.RecordAttempt(ipAddress); // Record failed attempt
                    AuditLogger.LogSecurityEvent(
                        "Invalid Coupon ID Format",
                        ipAddress,
                        $"Attempted ID: {id}",
                        SecuritySeverity.Low
                    );
                    ShowError("รหัสคูปองไม่ถูกต้อง");
                    return;
                }

                // SECURITY: Use parameterized query to prevent SQL injection
                string query = $"SELECT * FROM {_dbHelper.GetTableName("coupon")} WHERE {_dbHelper.GetColumnName("ID")} = @id";
                var parameter = _dbHelper.CreateParameter("@id", id);
                DataTable dtCode = _dbHelper.ExecuteQuery(query, parameter);

                // SECURITY: Log access with parameterized query
                LogAccess(id, dtCode.Rows.Count);

                // Check if redemption action is requested
                string action = InputValidator.GetSafeQueryStringParameter(Request, "action", 20);
                if (action == "Redeem" && dtCode.Rows.Count > 0)
                {
                    try
                    {
                        // SECURITY: Use parameterized query for UPDATE
                        string updateQuery = $"UPDATE {_dbHelper.GetTableName("Coupon")} SET {_dbHelper.GetColumnName("Date_Use")} = @dateUse, {_dbHelper.GetColumnName("Used_Status")} = @usedStatus WHERE {_dbHelper.GetColumnName("ID")} = @id";

                        var updateParams = new[]
                        {
                            _dbHelper.CreateParameter("@dateUse", DateTime.Now.ToString("yyyy-MM-dd HH:mm")),
                            _dbHelper.CreateParameter("@usedStatus", _dbHelper.GetBooleanValue(true)),
                            _dbHelper.CreateParameter("@id", id)
                        };

                        _dbHelper.ExecuteNonQuery(updateQuery, updateParams);

                        // SECURITY: Log coupon redemption to audit log
                        string couponValue = dtCode.Rows[0]["Value"]?.ToString() ?? "Unknown";
                        AuditLogger.LogCouponRedemption(id, "Central", int.TryParse(couponValue, out int val) ? val : 0, ipAddress);

                        // Generate barcode
                        System.Drawing.Image img;

                        // Use Server.MapPath to get physical path relative to web app
                        // This ensures it works correctly regardless of deployment location
                        string barcodePath = Server.MapPath("~/Barcode");

                        // Create directory if it doesn't exist
                        if (!System.IO.Directory.Exists(barcodePath))
                        {
                            System.IO.Directory.CreateDirectory(barcodePath);
                        }

                        // SECURITY: Validate path to prevent directory traversal
                        if (!InputValidator.IsSafeFilePath(id))
                        {
                            ErrorLogger.LogWarning("Unsafe file path detected", $"ID: {id}");
                            throw new Exception("Invalid coupon ID for file generation");
                        }

                        string path = System.IO.Path.Combine(barcodePath, id + ".jpg");

                        // Get barcode value - try different column names (case-insensitive)
                        string barcodeValue = GetColumnValue(dtCode.Rows[0], "code", "Code", "Barcode", "CouponCode");
                        if (string.IsNullOrEmpty(barcodeValue))
                        {
                            ErrorLogger.LogError("Barcode column not found on Central page", null,
                                $"Available columns: {string.Join(", ", GetColumnNames(dtCode))}, Coupon ID: {id}");
                            throw new Exception("Cannot find barcode column in database");
                        }

                        GetBarcode(140, 450, BarcodeLib.TYPE.CODE128, barcodeValue, out img, path);
                        Image1.ImageUrl = ResolveUrl("~/Barcode/" + id + ".jpg");
                        Image1.DataBind();

                        Panel1.Visible = false;
                        Panel2.Visible = true;

                        ErrorLogger.LogInfo("Coupon redeemed successfully", $"ID: {id}, IP: {ipAddress}");
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError("Error during coupon redemption", ex, $"Coupon ID: {id}");
                        AuditLogger.LogSecurityEvent(
                            "Coupon Redemption Failed",
                            ipAddress,
                            $"Coupon ID: {id}, Error: {ex.Message}",
                            SecuritySeverity.Low
                        );
                        ShowError("เกิดข้อผิดพลาดในการใช้คูปอง กรุณาลองใหม่อีกครั้ง");
                    }
                }

                // Process valid coupon
                if (dtCode.Rows.Count > 0)
                {
                    try
                    {
                        string couponValue = dtCode.Rows[0]["Value"].ToString();
                        if (couponValue == "200")
                        {
                            Image3.ImageUrl = "./Images/Central/200.png";
                            Image3.DataBind();
                            Image2.ImageUrl = "./Images/Central/200.png";
                            Image2.DataBind();
                        }
                        else if (couponValue == "300")
                        {
                            Image3.ImageUrl = "./Images/Central/300.png";
                            Image3.DataBind();
                            Image2.ImageUrl = "./Images/Central/300.png";
                            Image2.DataBind();
                        }

                        Label3.Text = couponValue;
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError("Error loading coupon image", ex, $"Coupon ID: {id}");
                    }
                }

                // Check expiration date
                DateTime expireDate = DateTime.Now;
                try
                {
                    if (dtCode.Rows.Count > 0 && dtCode.Rows[0]["Expire_DateTime"] != DBNull.Value)
                    {
                        expireDate = Convert.ToDateTime(dtCode.Rows[0]["Expire_DateTime"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError("Error parsing expiration date", ex, $"Coupon ID: {id}");
                }

                // Validate coupon status
                if (DateTime.Now > expireDate)
                {
                    Button1.Enabled = false;
                    Button1.Text = "คูปองหมดอายุ";
                }
                else if (dtCode.Rows.Count > 0 && DateTime.Now <= expireDate)
                {
                    // Coupon is valid - date display logic (currently commented out in original)
                    string Month = expireDate.Month.ToString().PadLeft(2, '0');
                    Month = GetThaiMonth(Month);
                    string year = (expireDate.Year + 543).ToString();
                    // Label1.Text = expireDate.Day + " " + Month + " " + year;
                    // Label2.Text = expireDate.Day + " " + Month + " " + year;
                }

                if (dtCode.Rows.Count > 0)
                {
                    string usedStatus = dtCode.Rows[0]["Used_Status"].ToString().ToLower();
                    bool isUsed = usedStatus == "1" || usedStatus == "true";

                    if (!isUsed && DateTime.Now > expireDate)
                    {
                        Button1.Enabled = false;
                        ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('คูปองหมดอายุแล้ว');", true);
                    }
                    else if (isUsed)
                    {
                        // Coupon already used - optionally disable button
                        // Button1.Enabled = false;
                        // Button1.Text = "คูปองถูกใช้ไปแล้ว";
                    }
                }
                else
                {
                    Button1.Enabled = false;
                    RateLimiter.RecordAttempt(ipAddress); // Record failed attempt for invalid coupon
                    ShowError("รหัสคูปองไม่ถูกต้อง");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error in Central Page_Load", ex);
                ShowError("เกิดข้อผิดพลาดในการโหลดหน้า กรุณาลองใหม่อีกครั้ง");
            }
        }
        
        /// <summary>
        /// Log access attempt to database
        /// </summary>
        private void LogAccess(string couponId, int foundCouponCount)
        {
            try
            {
                string machineName = Request.UserHostAddress; // Default to IP
                try
                {
                    machineName = System.Net.Dns.GetHostEntry(Request.UserHostAddress).HostName;
                }
                catch
                {
                    // If DNS lookup fails, use IP address
                }

                string logQuery = $"INSERT INTO {_dbHelper.GetTableName("Logs")} " +
                                $"({_dbHelper.GetColumnName("Created_DateTime")}, {_dbHelper.GetColumnName("Machine_Name")}, " +
                                $"{_dbHelper.GetColumnName("IP_Address")}, {_dbHelper.GetColumnName("Browser")}, " +
                                $"{_dbHelper.GetColumnName("Access_URL")}, {_dbHelper.GetColumnName("Found_Coupon")}) " +
                                $"VALUES(@createdDateTime, @machineName, @ipAddress, @browser, @accessUrl, @foundCoupon)";

                var logParams = new[]
                {
                    _dbHelper.CreateParameter("@createdDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    _dbHelper.CreateParameter("@machineName", machineName),
                    _dbHelper.CreateParameter("@ipAddress", Request.UserHostAddress),
                    _dbHelper.CreateParameter("@browser", Request.Browser.Browser),
                    _dbHelper.CreateParameter("@accessUrl", Request.Url.AbsoluteUri),
                    _dbHelper.CreateParameter("@foundCoupon", foundCouponCount)
                };

                _dbHelper.ExecuteNonQuery(logQuery, logParams);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error logging access", ex, $"Coupon ID: {couponId}");
            }
        }

        /// <summary>
        /// Show error message to user
        /// </summary>
        private void ShowError(string message)
        {
            ClientScript.RegisterStartupScript(this.GetType(), "myalert", $"alert('{message}');", true);
        }

        /// <summary>
        /// Convert month number to Thai month name
        /// </summary>
        private string GetThaiMonth(string month)
        {
            switch (month)
            {
                case "01": return "มกราคม";
                case "02": return "กุมภาพันธ์";
                case "03": return "มีนาคม";
                case "04": return "เมษายน";
                case "05": return "พฤษภาคม";
                case "06": return "มิถุนายน";
                case "07": return "กรกฎาคม";
                case "08": return "สิงหาคม";
                case "09": return "กันยายน";
                case "10": return "ตุลาคม";
                case "11": return "พฤศจิกายน";
                case "12": return "ธันวาคม";
                default: return month;
            }
        }

        public static void GetBarcode(int height, int width, BarcodeLib.TYPE type, string code, out System.Drawing.Image image, string fileSaveUrl)
        {
            try
            {
                image = null;

                // Ensure directory exists before saving
                string directory = System.IO.Path.GetDirectoryName(fileSaveUrl);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(directory);
                        ErrorLogger.LogInfo("Created barcode directory", $"Path: {directory}");
                    }
                    catch (Exception dirEx)
                    {
                        ErrorLogger.LogError("Failed to create barcode directory", dirEx, $"Path: {directory}");
                        throw new Exception($"Cannot create directory: {directory}", dirEx);
                    }
                }

                BarcodeLib.Barcode b = new BarcodeLib.Barcode();
                b.BackColor = System.Drawing.Color.White;
                b.ForeColor = System.Drawing.Color.Black;
                b.IncludeLabel = true;
                b.Alignment = BarcodeLib.AlignmentPositions.CENTER;
                b.LabelPosition = BarcodeLib.LabelPositions.BOTTOMCENTER;
                b.ImageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
                System.Drawing.Font font = new System.Drawing.Font("verdana", 10f);
                b.LabelFont = font;
                b.Height = height;
                b.Width = width;

                image = b.Encode(type, code);

                // Save with detailed error logging
                try
                {
                    image.Save(fileSaveUrl, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                catch (Exception saveEx)
                {
                    ErrorLogger.LogError("Failed to save barcode image", saveEx,
                        $"Path: {fileSaveUrl}, Code: {code}, Directory Exists: {System.IO.Directory.Exists(directory)}");
                    throw;
                }
            }
            catch (Exception err)
            {
                ErrorLogger.LogError("Error generating barcode", err, $"Path: {fileSaveUrl}, Code: {code}");
                image = null;
            }
        }


        protected void Button1_Click(object sender, EventArgs e)
        {
            // Use helper method that supports CutSlashfromRedirect config
            string url = HttpContext.Current.Request.Url.AbsoluteUri;

            // FEATURE: Support CutSlashfromRedirect config
            string cutSlash = System.Configuration.ConfigurationManager.AppSettings["CutSlashfromRedirect"];
            if (!string.IsNullOrEmpty(cutSlash) &&
                (cutSlash.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                 cutSlash.Equals("True", StringComparison.OrdinalIgnoreCase)))
            {
                // Remove trailing slash from URL before query string
                // Example: /Central.aspx/?id=123 -> /Central.aspx?id=123
                int queryStart = url.IndexOf('?');
                if (queryStart > 0 && url[queryStart - 1] == '/')
                {
                    url = url.Substring(0, queryStart - 1) + url.Substring(queryStart);
                }
            }

            Response.Redirect(url + "&action=Redeem");
        }

        /// <summary>
        /// Get column value from DataRow, trying multiple column name variations
        /// </summary>
        private string GetColumnValue(DataRow row, params string[] possibleColumnNames)
        {
            foreach (string columnName in possibleColumnNames)
            {
                // Try exact match first
                if (row.Table.Columns.Contains(columnName))
                {
                    object value = row[columnName];
                    if (value != null && value != DBNull.Value)
                    {
                        return value.ToString();
                    }
                }

                // Try case-insensitive match
                foreach (DataColumn column in row.Table.Columns)
                {
                    if (column.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        object value = row[column.ColumnName];
                        if (value != null && value != DBNull.Value)
                        {
                            return value.ToString();
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get all column names from DataTable for debugging
        /// </summary>
        private string[] GetColumnNames(DataTable table)
        {
            string[] columnNames = new string[table.Columns.Count];
            for (int i = 0; i < table.Columns.Count; i++)
            {
                columnNames[i] = table.Columns[i].ColumnName;
            }
            return columnNames;
        }
    }
}