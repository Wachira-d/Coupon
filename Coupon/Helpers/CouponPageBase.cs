using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using BarcodeLib;

namespace Coupon.Helpers
{
    /// <summary>
    /// Base class for coupon redemption pages (Central, Lotus, Starbucks)
    /// Provides common functionality and reduces code duplication
    /// </summary>
    public abstract class CouponPageBase : Page
    {
        protected DatabaseHelper _dbHelper;
        protected string BrandName { get; set; }
        protected string ImagePath { get; set; }

        protected CouponPageBase()
        {
            _dbHelper = new DatabaseHelper();
        }

        /// <summary>
        /// Override in derived classes to set brand-specific properties
        /// </summary>
        protected abstract void InitializeBrandSettings();

        /// <summary>
        /// Common Page_Load logic for all coupon pages
        /// </summary>
        protected void ProcessCouponPage(System.Web.UI.WebControls.Image image1, System.Web.UI.WebControls.Image image2, System.Web.UI.WebControls.Image image3, Label label3, Button button1, Panel panel1, Panel panel2, Label label1 = null, Label label2 = null)
        {
            try
            {
                // Initialize brand-specific settings
                InitializeBrandSettings();
                ErrorLogger.LogInfo($"{BrandName} page loading", $"URL: {Request.Url}");

                // SECURITY: Validate and sanitize input
                string id = InputValidator.GetSafeQueryStringParameter(Request, "id", 20);

                if (string.IsNullOrEmpty(id))
                {
                    ErrorLogger.LogWarning($"{BrandName} page accessed without valid coupon ID", $"URL: {Request.Url}");
                    ShowError("รหัสคูปองไม่ถูกต้อง");
                    return;
                }

                ErrorLogger.LogInfo($"{BrandName} processing coupon", $"ID: {id}");

                // SECURITY: Validate coupon ID format
                int expectedLength = Convert.ToInt32(ConfigurationManager.AppSettings["NumberofCode"] ?? "4");
                if (!InputValidator.IsValidCouponId(id, expectedLength))
                {
                    ErrorLogger.LogWarning($"Invalid coupon ID format on {BrandName}", $"ID: {id}, IP: {Request.UserHostAddress}");
                    ShowError("รหัสคูปองไม่ถูกต้อง");
                    return;
                }

                // SECURITY: Use parameterized query
                string query = $"SELECT * FROM {_dbHelper.GetTableName("coupon")} WHERE {_dbHelper.GetColumnName("ID")} = @id";
                var parameter = _dbHelper.CreateParameter("@id", id);

                ErrorLogger.LogInfo($"{BrandName} executing query", $"Query: {query}, ID: {id}");
                DataTable dtCode = _dbHelper.ExecuteQuery(query, parameter);
                ErrorLogger.LogInfo($"{BrandName} query result", $"Rows found: {dtCode.Rows.Count}");

                // Log access attempt
                LogAccess(id, dtCode.Rows.Count);

                // Process redemption if requested
                string action = InputValidator.GetSafeQueryStringParameter(Request, "action", 20);
                if (action == "Redeem" && dtCode.Rows.Count > 0)
                {
                    ProcessRedemption(id, dtCode, image1, panel1, panel2);
                }

                // Rate limiting check
                if (!CheckRateLimits())
                {
                    ErrorLogger.LogWarning($"Rate limit exceeded on {BrandName}", $"IP: {Request.UserHostAddress}");
                    ShowError("คุณมีความเสี่ยงที่จะโจมตีระบบ กรุณาติดต่อผู้ให้บริการ");
                    return;
                }

                // Process coupon display
                if (dtCode.Rows.Count > 0)
                {
                    DisplayCoupon(dtCode, image2, image3, label3, button1, label1, label2);
                }
                else
                {
                    button1.Enabled = false;
                    ShowError("รหัสคูปองไม่ถูกต้อง");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"CRITICAL ERROR in {BrandName} Page_Load", ex,
                    $"URL: {Request?.Url?.ToString() ?? "Unknown"}, " +
                    $"QueryString: {Request?.QueryString?.ToString() ?? "None"}, " +
                    $"IP: {Request?.UserHostAddress ?? "Unknown"}");

                // Show detailed error in development, generic in production
                if (System.Configuration.ConfigurationManager.AppSettings["Environment"] == "Development")
                {
                    ShowError($"Error: {ex.Message}");
                }
                else
                {
                    ShowError("เกิดข้อผิดพลาดในการโหลดหน้า กรุณาลองใหม่อีกครั้ง");
                }
            }
        }

        /// <summary>
        /// Process coupon redemption
        /// </summary>
        private void ProcessRedemption(string id, DataTable dtCode, System.Web.UI.WebControls.Image image1, Panel panel1, Panel panel2)
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

                // Generate barcode
                System.Drawing.Image img;
                string basePath = ConfigurationManager.AppSettings["Path"];

                // IMPORTANT: Barcode images must be saved in Barcode subfolder
                // because Image URL uses "./Barcode/" prefix
                string barcodePath = System.IO.Path.Combine(basePath, "Barcode");

                // SECURITY: Validate path
                if (!InputValidator.IsSafeFilePath(id))
                {
                    ErrorLogger.LogWarning($"Unsafe file path detected on {BrandName}", $"ID: {id}");
                    throw new Exception("Invalid coupon ID for file generation");
                }

                string path = System.IO.Path.Combine(barcodePath, id + ".jpg");

                // Get barcode value - try different column names (case-insensitive)
                string barcodeValue = GetColumnValue(dtCode.Rows[0], "code", "Code", "Barcode", "CouponCode");
                if (string.IsNullOrEmpty(barcodeValue))
                {
                    ErrorLogger.LogError($"Barcode column not found on {BrandName}", null,
                        $"Available columns: {string.Join(", ", GetColumnNames(dtCode))}, Coupon ID: {id}");
                    throw new Exception("Cannot find barcode column in database");
                }

                GetBarcode(140, 450, BarcodeLib.TYPE.CODE128, barcodeValue, out img, path);
                image1.ImageUrl = "./Barcode/" + id + ".jpg";
                image1.DataBind();

                panel1.Visible = false;
                panel2.Visible = true;

                ErrorLogger.LogInfo($"Coupon redeemed successfully on {BrandName}", $"ID: {id}, IP: {Request.UserHostAddress}");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error during coupon redemption on {BrandName}", ex, $"Coupon ID: {id}");
                ShowError("เกิดข้อผิดพลาดในการใช้คูปอง กรุณาลองใหม่อีกครั้ง");
            }
        }

        /// <summary>
        /// Display coupon information
        /// </summary>
        private void DisplayCoupon(DataTable dtCode, System.Web.UI.WebControls.Image image2, System.Web.UI.WebControls.Image image3, Label label3, Button button1, Label label1 = null, Label label2 = null)
        {
            try
            {
                // Get coupon value with case-insensitive column search
                string couponValue = GetColumnValue(dtCode.Rows[0], "Value", "value", "CouponValue", "coupon_value");
                if (string.IsNullOrEmpty(couponValue))
                {
                    ErrorLogger.LogError($"Value column not found on {BrandName}", null,
                        $"Available columns: {string.Join(", ", GetColumnNames(dtCode))}");
                    ShowError("ไม่พบข้อมูลมูลค่าคูปอง");
                    return;
                }

                string imagePath200 = $"./Images/{ImagePath}/200.png";
                string imagePath300 = $"./Images/{ImagePath}/300.png";

                if (couponValue == "200")
                {
                    image3.ImageUrl = imagePath200;
                    image3.DataBind();
                    image2.ImageUrl = imagePath200;
                    image2.DataBind();
                }
                else if (couponValue == "300")
                {
                    image3.ImageUrl = imagePath300;
                    image3.DataBind();
                    image2.ImageUrl = imagePath300;
                    image2.DataBind();
                }

                label3.Text = couponValue;

                // Check expiration date
                DateTime expireDate = DateTime.Now;
                try
                {
                    string expireDateStr = GetColumnValue(dtCode.Rows[0], "Expire_DateTime", "expire_datetime", "ExpireDate", "expire_date");
                    if (!string.IsNullOrEmpty(expireDateStr))
                    {
                        expireDate = Convert.ToDateTime(expireDateStr);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError($"Error parsing expiration date on {BrandName}", ex);
                }

                // Validate coupon status
                if (DateTime.Now > expireDate)
                {
                    button1.Enabled = false;
                    button1.Text = "คูปองหมดอายุ";
                }
                else if (DateTime.Now <= expireDate)
                {
                    // Coupon is valid - display expiration date
                    string Month = expireDate.Month.ToString().PadLeft(2, '0');
                    Month = GetThaiMonth(Month);
                    string year = (expireDate.Year + 543).ToString();
                    string displayDate = $"{expireDate.Day} {Month} {year}";

                    // Set date labels if provided
                    if (label1 != null)
                    {
                        label1.Text = displayDate;
                    }
                    if (label2 != null)
                    {
                        label2.Text = displayDate;
                    }
                }

                // Check used status
                string usedStatus = GetColumnValue(dtCode.Rows[0], "Used_Status", "used_status", "UsedStatus", "IsUsed", "is_used");
                bool isUsed = false;
                if (!string.IsNullOrEmpty(usedStatus))
                {
                    usedStatus = usedStatus.ToLower();
                    isUsed = usedStatus == "1" || usedStatus == "true" || usedStatus == "t";
                }

                if (!isUsed && DateTime.Now > expireDate)
                {
                    button1.Enabled = false;
                    ShowError("คูปองหมดอายุแล้ว");
                }
                else if (isUsed)
                {
                    // Coupon already used - optionally disable button
                    // button1.Enabled = false;
                    // button1.Text = "คูปองถูกใช้ไปแล้ว";
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error loading coupon image on {BrandName}", ex);
            }
        }

        /// <summary>
        /// Check rate limits for failed access attempts
        /// </summary>
        private bool CheckRateLimits()
        {
            try
            {
                string logsQuery = $"SELECT distinct {_dbHelper.GetColumnName("Access_URL")}, {_dbHelper.GetColumnName("IP_Address")} FROM {_dbHelper.GetTableName("Logs")} " +
                                 $"WHERE {_dbHelper.GetColumnName("Created_DateTime")} > @checkTime " +
                                 $"AND {_dbHelper.GetColumnName("Found_Coupon")} = @foundCoupon " +
                                 $"AND {_dbHelper.GetColumnName("IP_Address")} = @ipAddress " +
                                 $"GROUP BY {_dbHelper.GetColumnName("IP_Address")}, {_dbHelper.GetColumnName("Access_URL")}";

                var logParams = new[]
                {
                    _dbHelper.CreateParameter("@checkTime", DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss")),
                    _dbHelper.CreateParameter("@foundCoupon", _dbHelper.GetBooleanValue(false)),
                    _dbHelper.CreateParameter("@ipAddress", Request.UserHostAddress)
                };

                DataTable dtLogs = _dbHelper.ExecuteQuery(logsQuery, logParams);
                return dtLogs.Rows.Count <= 10;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Error checking rate limits on {BrandName}", ex);
                return true; // Allow access if rate limit check fails
            }
        }

        /// <summary>
        /// Log access attempt to database
        /// </summary>
        protected void LogAccess(string couponId, int foundCouponCount)
        {
            try
            {
                string machineName = Request.UserHostAddress;
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
                ErrorLogger.LogError($"Error logging access on {BrandName}", ex, $"Coupon ID: {couponId}");
            }
        }

        /// <summary>
        /// Show error message to user
        /// </summary>
        protected void ShowError(string message)
        {
            ClientScript.RegisterStartupScript(this.GetType(), "myalert", $"alert('{message}');", true);
        }

        /// <summary>
        /// Convert month number to Thai month name
        /// </summary>
        protected string GetThaiMonth(string month)
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

        /// <summary>
        /// Generate barcode image
        /// </summary>
        protected static void GetBarcode(int height, int width, BarcodeLib.TYPE type, string code, out System.Drawing.Image image, string fileSaveUrl)
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

        /// <summary>
        /// Handle redirect with redemption action
        /// Supports CutSlashfromRedirect config to remove trailing slashes
        /// </summary>
        protected void RedirectToRedemption()
        {
            string url = HttpContext.Current.Request.Url.AbsoluteUri;

            // FEATURE: Support CutSlashfromRedirect config
            // If enabled, remove trailing slash before adding action parameter
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
