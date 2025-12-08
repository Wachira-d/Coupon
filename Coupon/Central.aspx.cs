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
                // SECURITY: Validate and sanitize input before processing
                string id = InputValidator.GetSafeQueryStringParameter(Request, "id", 20);

                if (string.IsNullOrEmpty(id))
                {
                    ErrorLogger.LogWarning("Central page accessed without valid coupon ID", $"URL: {Request.Url}");
                    ShowError("รหัสคูปองไม่ถูกต้อง");
                    return;
                }

                // SECURITY: Validate coupon ID format to prevent injection attacks
                int expectedLength = Convert.ToInt32(ConfigurationManager.AppSettings["NumberofCode"] ?? "4");
                if (!InputValidator.IsValidCouponId(id, expectedLength))
                {
                    ErrorLogger.LogWarning("Invalid coupon ID format", $"ID: {id}, IP: {Request.UserHostAddress}");
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

                        // Generate barcode
                        System.Drawing.Image img;
                        string basePath = ConfigurationManager.AppSettings["Path"];

                        // SECURITY: Validate path to prevent directory traversal
                        if (!InputValidator.IsSafeFilePath(id))
                        {
                            ErrorLogger.LogWarning("Unsafe file path detected", $"ID: {id}");
                            throw new Exception("Invalid coupon ID for file generation");
                        }

                        string path = System.IO.Path.Combine(basePath, id + ".jpg");
                        GetBarcode(140, 450, BarcodeLib.TYPE.CODE128, dtCode.Rows[0][_dbHelper.GetColumnName("code")].ToString(), out img, path);
                        Image1.ImageUrl = "./Barcode/" + id + ".jpg";
                        Image1.DataBind();

                        Panel1.Visible = false;
                        Panel2.Visible = true;

                        ErrorLogger.LogInfo("Coupon redeemed successfully", $"ID: {id}, IP: {Request.UserHostAddress}");
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError("Error during coupon redemption", ex, $"Coupon ID: {id}");
                        ShowError("เกิดข้อผิดพลาดในการใช้คูปอง กรุณาลองใหม่อีกครั้ง");
                    }
                }

                // SECURITY: Rate limiting - check failed access attempts from this IP
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

                    if (dtLogs.Rows.Count <= 10)
                {
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
                        ShowError("รหัสคูปองไม่ถูกต้อง");
                    }
                }
                else
                {
                    // Rate limit exceeded
                    ErrorLogger.LogWarning("Rate limit exceeded", $"IP: {Request.UserHostAddress}, Failed attempts: {dtLogs.Rows.Count}");
                    ShowError("คุณมีความเสี่ยงที่จะโจมตีระบบ กรุณาติดต่อผู้ให้บริการ");
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
                image.Save(fileSaveUrl, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            catch (Exception err)
            {
                err.ToString();
                image = null;
            }
        }


        protected void Button1_Click(object sender, EventArgs e)
        {
            Response.Redirect(HttpContext.Current.Request.Url.AbsoluteUri+"&action=Redeem");
        }
    }
}