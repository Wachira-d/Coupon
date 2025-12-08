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
using System.Configuration;


namespace Coupon
{
    public partial class test : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string id = Request.QueryString["id"];
            DataTable dtCode = DatabaseQuery(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "SELECT * FROM ["+ ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Coupon] Where ID = '"+id+"'");
            try {
                DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "INSERT INTO ["+ ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Logs] ([Created_DateTime],[Machine_Name],[IP_Address],[Browser],[Access_URL],[Found_Coupon]) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd") + "',N'" + System.Net.Dns.GetHostEntry(HttpContext.Current.Request.UserHostName.ToString()).HostName + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'"+ HttpContext.Current.Request.Url.AbsoluteUri + "','"+dtCode.Rows.Count+"')");
            }
            catch
            {
                DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "INSERT INTO [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Logs] ([Created_DateTime],[Machine_Name],[IP_Address],[Browser],[Access_URL],[Found_Coupon]) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd") + "',N'" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + dtCode.Rows.Count + "')");
               
            }

            try {
                string action = Request.QueryString["action"];
                if (action == "Redeem")
                {
                    DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "UPDATE [dbo].[Coupon] SET [Date_Use] = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "' ,[Used_Status] = 1 WHERE ID = '" + id + "'");

                    System.Drawing.Image img;
                    string path = System.Configuration.ConfigurationSettings.AppSettings["Path"].ToString() + id + ".jpg";
                    GetBarcode(140, 450, BarcodeLib.TYPE.CODE128, dtCode.Rows[0]["code"].ToString(), out img, path);
                    Image1.ImageUrl = "./Barcode/" + id + ".jpg";
                    Image1.DataBind();

                    Panel1.Visible = false;
                    Panel2.Visible = true;
                }
            }
            catch { }
            

            DataTable dtLogs =  DatabaseQuery(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "SELECT distinct(Access_URL),IP_Address FROM ["+ ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Logs] Where Created_DateTime > '" + DateTime.Now.AddHours(-1) + "' AND Found_Coupon = 'false' AND IP_Address = N'" + HttpContext.Current.Request.UserHostName.ToString() + "' group by IP_Address,Access_URL");
            

            if (dtLogs.Rows.Count <= 10)
            {
                try
                {
                    if (dtCode.Rows[0]["Value"].ToString() == "200")
                    {
                        Image3.ImageUrl = "./Images/200.png";
                        Image3.DataBind();
                        Image2.ImageUrl = "./Images/200.png";
                        Image2.DataBind();
                    }
                    else if (dtCode.Rows[0]["Value"].ToString() == "300")
                    {
                        Image3.ImageUrl = "./Images/300.png";
                        Image3.DataBind();
                        Image2.ImageUrl = "./Images/300.png";
                        Image2.DataBind();
                    }

                    Label3.Text = dtCode.Rows[0]["Value"].ToString();
                }
                catch { }
                DateTime expireDate = DateTime.Now;
                try { expireDate = Convert.ToDateTime(dtCode.Rows[0]["Expire_DateTime"].ToString()); }
                catch { }


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

                    //Label1.Text = expireDate.Day + " " + Month + " " + year;
                    //Label2.Text = expireDate.Day + " " + Month + " " + year;

                }
                else if (dtCode.Rows.Count > 0 && (dtCode.Rows[0]["Used_Status"] == "0" || dtCode.Rows[0]["Used_Status"].ToString().ToLower() == "false") && DateTime.Now > expireDate)
                {
                    Button1.Enabled = false;
                    ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('คูปองหมดอายุแล้ว');", true);
                }
                else if (dtCode.Rows.Count > 0 && (dtCode.Rows[0]["Used_Status"] == "1" || dtCode.Rows[0]["Used_Status"].ToString().ToLower() == "true"))
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
        public static void GetBarcode(int height, int width, BarcodeLib.TYPE type, string code, out System.Drawing.Image image, string fileSaveUrl)
        {
            try
            {
                image = null;

                BarcodeLib.Barcode b = new BarcodeLib.Barcode();
                b.BackColor = System.Drawing.Color.White;//图片背景颜色
                b.ForeColor = System.Drawing.Color.Black;//条码颜色
                b.IncludeLabel = true;
                b.Alignment = BarcodeLib.AlignmentPositions.CENTER;
                b.LabelPosition = BarcodeLib.LabelPositions.BOTTOMCENTER;//code的显示位置
                b.ImageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;//图片格式
                System.Drawing.Font font = new System.Drawing.Font("verdana", 10f);//字体设置
                b.LabelFont = font;
                b.Height = height;//图片高度设置(px单位)
                b.Width = width;//图片宽度设置(px单位)

                image = b.Encode(type, code);//生成图片
                image.Save(fileSaveUrl, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            catch (Exception err)
            {
                err.ToString();
                image = null;
            }
        }

        public static DataTable DatabaseQuery(string connStr, string cmd)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                try
                {
                    cmd = cmd.Replace("&amp;", "&");
                    cmd = cmd.Replace("&#39;", "''");
                    cmd = cmd.Replace("&#160;", "");
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd, con);
                    adapter.Fill(dt);
                }
                catch (Exception ex) { }
            }
            return dt;
        }

        public void DatabaseInsert(string connStr, string cmd)
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

        protected void Button1_Click(object sender, EventArgs e)
        {
            Response.Redirect(HttpContext.Current.Request.Url.AbsoluteUri+"&action=Redeem");
        }
    }
}