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

namespace Coupon
{
    public partial class Lotus : Page
    {
        private string databaseType = ConfigurationSettings.AppSettings["DatabaseType"].ToString().ToLower();
        private string connectionString = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Get appropriate connection string based on database type
            if (databaseType == "postgresql")
            {
                connectionString = ConfigurationSettings.AppSettings["PostgreSQLConnectionString"].ToString();
            }
            else
            {
                connectionString = ConfigurationSettings.AppSettings["MSSQLConnectionString"].ToString();
            }

            if (ConfigurationSettings.AppSettings["CutSlashfromRedirect"].ToString().ToLower() == "yes")
            {
                string url = HttpContext.Current.Request.Url.AbsoluteUri;
                string[] code = url.Split('/');
                if (code[code.Length - 1].Length == Convert.ToInt32(ConfigurationSettings.AppSettings["NumberofCode"]))
                {
                    Response.Redirect("./?id=" + code[code.Length - 1]);
                }
            }

            string id = Request.QueryString["id"];
            DataTable dtCode = DatabaseQuery(connectionString, "SELECT * FROM " + GetTableName("coupon") + " WHERE " + GetColumnName("ID") + " = '" + id + "'");

            try
            {
                string logQuery = "INSERT INTO " + GetTableName("Logs") + " (" + GetColumnName("Created_DateTime") + ", " + GetColumnName("Machine_Name") + ", " + GetColumnName("IP_Address") + ", " + GetColumnName("Browser") + ", " + GetColumnName("Access_URL") + ", " + GetColumnName("Found_Coupon") + ") " +
                                 "VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'" + System.Net.Dns.GetHostEntry(HttpContext.Current.Request.UserHostName.ToString()).HostName + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + dtCode.Rows.Count + "')";
                DatabaseInsert(connectionString, logQuery);
            }
            catch
            {
                string logQuery = "INSERT INTO " + GetTableName("Logs") + " (" + GetColumnName("Created_DateTime") + ", " + GetColumnName("Machine_Name") + ", " + GetColumnName("IP_Address") + ", " + GetColumnName("Browser") + ", " + GetColumnName("Access_URL") + ", " + GetColumnName("Found_Coupon") + ") " +
                                 "VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + dtCode.Rows.Count + "')";
                DatabaseInsert(connectionString, logQuery);
            }

            try
            {
                string action = Request.QueryString["action"];
                if (action == "Redeem")
                {
                    string updateQuery = "UPDATE " + GetTableName("Coupon") + " SET " + GetColumnName("Date_Use") + " = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "' , " + GetColumnName("Used_Status") + " = " + GetBooleanValue(true) + " WHERE " + GetColumnName("ID") + " = '" + id + "'";
                    DatabaseInsert(connectionString, updateQuery);

                    System.Drawing.Image img;
                    string path = System.Configuration.ConfigurationSettings.AppSettings["Path"].ToString() + id + ".jpg";
                    GetBarcode(140, 450, BarcodeLib.TYPE.CODE128, dtCode.Rows[0][GetColumnName("code")].ToString(), out img, path);
                    Image1.ImageUrl = "./Barcode/" + id + ".jpg";
                    Image1.DataBind();

                    Panel1.Visible = false;
                    Panel2.Visible = true;
                }
            }
            catch { }

            string logsQuery = "SELECT distinct " + GetColumnName("Access_URL") + ", " + GetColumnName("IP_Address") + " FROM " + GetTableName("Logs") +
                             " WHERE " + GetColumnName("Created_DateTime") + " > '" + DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss") +
                             "' AND " + GetColumnName("Found_Coupon") + " = " + GetBooleanValue(false) +
                             " AND " + GetColumnName("IP_Address") + " = N'" + HttpContext.Current.Request.UserHostName.ToString() +
                             "' GROUP BY " + GetColumnName("IP_Address") + ", " + GetColumnName("Access_URL");

            DataTable dtLogs = DatabaseQuery(connectionString, logsQuery);

            if (dtLogs.Rows.Count <= 10)
            {
                try
                {
                    string value = dtCode.Rows[0][GetColumnName("Value")].ToString();
                    if (value == "200")
                    {
                        Image3.ImageUrl = "./Images/Lotus/200.png";
                        Image3.DataBind();
                        Image2.ImageUrl = "./Images/Lotus/200.png";
                        Image2.DataBind();
                    }
                    else if (value == "300")
                    {
                        Image3.ImageUrl = "./Images/Lotus/300.png";
                        Image3.DataBind();
                        Image2.ImageUrl = "./Images/Lotus/300.png";
                        Image2.DataBind();
                    }

                    Label3.Text = value;
                }
                catch { }

                DateTime expireDate = DateTime.Now;
                try { expireDate = Convert.ToDateTime(dtCode.Rows[0][GetColumnName("Expire_DateTime")].ToString()); }
                catch { }

                if (DateTime.Now > expireDate)
                {
                    Label1.Text = "คูปองหมดอายุแล้ว";
                    Label2.Text = "คูปองหมดอายุแล้ว";
                    Button1.Enabled = false;
                    Button1.Text = "คูปองหมดอายุ";
                }

                if (dtCode.Rows.Count > 0 && DateTime.Now <= expireDate)
                {
                    string Month = expireDate.Month.ToString();
                    if (Month.Length == 1)
                    {
                        Month = "0" + Month;
                    }

                    if (Month == "01") { Month = "มกราคม"; }
                    else if (Month == "02") { Month = "กุมภาพันธ์"; }
                    else if (Month == "03") { Month = "มีนาคม"; }
                    else if (Month == "04") { Month = "เมษายน"; }
                    else if (Month == "05") { Month = "พฤษภาคม"; }
                    else if (Month == "06") { Month = "มิถุนายน"; }
                    else if (Month == "07") { Month = "กรกฎาคม"; }
                    else if (Month == "08") { Month = "สิงหาคม"; }
                    else if (Month == "09") { Month = "กันยายน"; }
                    else if (Month == "10") { Month = "ตุลาคม"; }
                    else if (Month == "11") { Month = "พฤศจิกายน"; }
                    else if (Month == "12") { Month = "ธันวาคม"; }

                    string year = (Convert.ToInt32(expireDate.Year.ToString()) + 543).ToString();

                    Label1.Text = expireDate.Day + " " + Month + " " + year;
                    Label2.Text = expireDate.Day + " " + Month + " " + year;
                }
                else if (dtCode.Rows.Count > 0 && (dtCode.Rows[0][GetColumnName("Used_Status")].ToString() == "False" || dtCode.Rows[0][GetColumnName("Used_Status")].ToString().ToLower() == "false") && DateTime.Now > expireDate)
                {
                    Button1.Enabled = false;
                    ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('คูปองหมดอายุแล้ว');", true);
                }
                else if (dtCode.Rows.Count > 0 && (dtCode.Rows[0][GetColumnName("Used_Status")].ToString() == "True" || dtCode.Rows[0][GetColumnName("Used_Status")].ToString().ToLower() == "true"))
                {
                    //Button1.Enabled = false;
                    //Button1.Text = "คูปองถูกใช้ไปแล้ว";
                }
                else
                {
                    Button1.Enabled = false;
                    ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('รหัสคูปองไม่ถูกต้อง');", true);
                }
            }
            else
            {
                ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('คุณมีความเสี่ยงที่จะโจมตีระบบ กรุณาติดต่อผู้ให้บริการ');", true);
            }
        }

        private string GetTableName(string tableName)
        {
            if (databaseType == "postgresql")
            {
                return tableName.ToLower();
            }
            return "[" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[" + tableName + "]";
        }

        private string GetColumnName(string columnName)
        {
            if (databaseType == "postgresql")
            {
                return columnName.ToLower();
            }
            return "[" + columnName + "]";
        }

        private string GetBooleanValue(bool value)
        {
            if (databaseType == "postgresql")
            {
                return value.ToString().ToLower();
            }
            return value ? "1" : "0";
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

        public DataTable DatabaseQuery(string connStr, string cmd)
        {
            DataTable dt = new DataTable();

            try
            {
                cmd = cmd.Replace("&amp;", "&");
                cmd = cmd.Replace("&#39;", "''");
                cmd = cmd.Replace("&#160;", "");

                if (databaseType == "postgresql")
                {
                    using (NpgsqlConnection con = new NpgsqlConnection(connStr))
                    {
                        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd, con);
                        adapter.Fill(dt);
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd, con);
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex) { }

            return dt;
        }

        public void DatabaseInsert(string connStr, string cmd)
        {
            try
            {
                if (databaseType == "postgresql")
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(connStr))
                    {
                        using (NpgsqlCommand command = new NpgsqlCommand(cmd, connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        using (SqlCommand command = new SqlCommand(cmd, connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception ex) { }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Response.Redirect(HttpContext.Current.Request.Url.AbsoluteUri.Replace("Lotus.aspx", "") + "&action=Redeem");
        }
    }
}