using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.DirectoryServices.Protocols;
using System.Net;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Coupon.Admin
{
    public partial class Report : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            DataTable dtCodeAll = DatabaseQuery(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "SELECT * FROM [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Coupon]");
            DataTable dtCodeSend = DatabaseQuery(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "SELECT * FROM [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Coupon] Where Send_Status = 'True'");
            DataTable dtCodeUsed = DatabaseQuery(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "SELECT * FROM [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Coupon] Where Used_Status = 'True'");

            Label1.Text = dtCodeUsed.Rows.Count + "\\" + dtCodeAll.Rows.Count;
            Label3.Text = dtCodeSend.Rows.Count + "\\" + dtCodeAll.Rows.Count; 
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

        public bool checkEmailFormat(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            string ldap = ConfigurationManager.AppSettings["LDAP"];
            bool isValid = false;
            string[] password = ConfigurationSettings.AppSettings["ReportPassword"].ToString().Split(';');
            int check = 0;
            for (int i = 0; i < password.Length; i++)
            {
                if (TextBox1.Text.ToLower() == password[i].ToLower())
                {
                    check = 1;
                }
            }
            if (check == 1)
            {

                if (checkEmailFormat(TextBox1.Text) && Regex.IsMatch(TextBox1.Text[0].ToString(), "^[a-zA-Z]*$") && Regex.IsMatch(TextBox1.Text[TextBox1.Text.Length - 1].ToString(), "^[a-zA-Z]*$"))
                {
                    using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, ldap))
                    {
                        // validate the credentials
                        isValid = pc.ValidateCredentials(TextBox1.Text, TextBox13.Text);
                    }
                }
                else
                {

                }
                if (isValid == true)
                {
                    Panel1.Visible = true;
                    TextBox1.Visible = false;
                    TextBox13.Visible = false;
                    Button1.Visible = false;
                    try
                    {
                        DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "INSERT INTO [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Admin_Logs] ([Created_DateTime],[Machine_Name],[IP_Address],[Browser],[Access_URL],[Password],[CanAccess]) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'" + System.Net.Dns.GetHostEntry(HttpContext.Current.Request.UserHostName.ToString()).HostName + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + TextBox1.Text + "','True')");
                    }
                    catch
                    {
                        DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "INSERT INTO [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Admin_Logs] ([Created_DateTime],[Machine_Name],[IP_Address],[Browser],[Access_URL],[Password],[CanAccess]) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + TextBox1.Text + "','True')");

                    }
                }
                else
                {
                    TextBox13.Text = "Password incorrect";
                    try
                    {
                        DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "INSERT INTO [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Admin_Logs] ([Created_DateTime],[Machine_Name],[IP_Address],[Browser],[Access_URL],[Password],[CanAccess]) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'" + System.Net.Dns.GetHostEntry(HttpContext.Current.Request.UserHostName.ToString()).HostName + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + TextBox1.Text + "','False')");
                    }
                    catch
                    {
                        DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "INSERT INTO [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Admin_Logs] ([Created_DateTime],[Machine_Name],[IP_Address],[Browser],[Access_URL],[Password],[CanAccess]) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + TextBox1.Text + "','False')");

                    }
                }
            }
            else
            {
                try
                {
                    DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "INSERT INTO [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Admin_Logs] ([Created_DateTime],[Machine_Name],[IP_Address],[Browser],[Access_URL],[Password],[CanAccess]) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'" + System.Net.Dns.GetHostEntry(HttpContext.Current.Request.UserHostName.ToString()).HostName + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + TextBox1.Text + "','False')");
                }
                catch
                {
                    DatabaseInsert(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), "INSERT INTO [" + ConfigurationSettings.AppSettings["Coupon_DatabaseName"].ToString() + "].[dbo].[Admin)_Logs] ([Created_DateTime],[Machine_Name],[IP_Address],[Browser],[Access_URL],[Password],[CanAccess]) VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.UserHostName.ToString() + "','" + HttpContext.Current.Request.Browser.Browser + "',N'" + HttpContext.Current.Request.Url.AbsoluteUri + "','" + TextBox1.Text + "','False')");

                }
            }


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
        protected void Button2_Click(object sender, EventArgs e)
        {
            string cmd = "Select * from Coupon Where ";
            if(TextBox2.Text.Length > 0)
            {
                cmd += "ID like '%"+TextBox2.Text+"%' AND ";
            }
            if (TextBox3.Text.Length > 0)
            {
                cmd += "Code like '%" + TextBox3.Text + "%' AND ";
            }
            if (TextBox4.Text.Length > 0)
            {
                cmd += "Date_DataIn >= '" + Convert.ToDateTime(TextBox4.Text).ToString("yyyy-MM-dd") + "' AND ";
            }
            if (TextBox5.Text.Length > 0)
            {
                cmd += "Date_DataIn <= '" + Convert.ToDateTime(TextBox5.Text).ToString("yyyy-MM-dd") + "' AND ";
            }
            if (TextBox6.Text.Length > 0)
            {
                cmd += "Date_Send >= '" + Convert.ToDateTime(TextBox6.Text).ToString("yyyy-MM-dd") + "' AND ";
            }
            if (TextBox7.Text.Length > 0)
            {
                cmd += "Date_Send <= '" + Convert.ToDateTime(TextBox7.Text).ToString("yyyy-MM-dd") + "' AND ";
            }
            if (TextBox8.Text.Length > 0)
            {
                cmd += "Date_Use >= '" + Convert.ToDateTime(TextBox8.Text).ToString("yyyy-MM-dd") + "' AND ";
            }
            if (TextBox9.Text.Length > 0)
            {
                cmd += "Date_Use <= '" + Convert.ToDateTime(TextBox9.Text).ToString("yyyy-MM-dd") + "' AND ";
            }
            if(DropDownList1.SelectedIndex > 0)
            {
                cmd += "Send_Status = '" + DropDownList1.SelectedValue+ "' AND ";
            }
            if (DropDownList2.SelectedIndex > 0)
            {
                cmd += "Used_Status = '" + DropDownList2.SelectedValue + "' AND ";
            }
            


            cmd = cmd.Substring(0, cmd.Length - 4);
            DataTable dtResult = DatabaseQuery(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), cmd);
            GridView1.DataSource = dtResult;
            GridView1.DataBind();
            Label2.Text = dtResult.Rows.Count+" rows";

        }

        protected void Button3_Click(object sender, EventArgs e)
        {
            GridView1.DataSource = null;
            GridView1.DataBind();
            Label2.Text = "";

        }

        protected void Button4_Click(object sender, EventArgs e)
        {
            string cmd = "Select * from Logs Where ";
            if (TextBox12.Text.Length > 0)
            {
                cmd += "Access_URL like '%" + TextBox12.Text + "%' AND ";
            }
            if (CheckBox4.Checked)
            {
                cmd += "Access_URL like '%Redeem%' AND ";
            }
            if (TextBox10.Text.Length > 0)
            {
                cmd += "Created_DateTime >= '" + Convert.ToDateTime(TextBox10.Text).ToString("yyyy-MM-dd") + "' AND ";
            }
            if (TextBox11.Text.Length > 0)
            {
                cmd += "Created_DateTime <= '" + Convert.ToDateTime(TextBox11.Text).ToString("yyyy-MM-dd") + "' AND ";
            }
            cmd += "Found_Coupon = '" + CheckBox3.Checked + "' AND ";

            cmd = cmd.Substring(0, cmd.Length - 4);
            DataTable dtResult = DatabaseQuery(ConfigurationSettings.AppSettings["ConnectionString"].ToString(), cmd);
            GridView1.DataSource = dtResult;
            GridView1.DataBind();
            Label2.Text = dtResult.Rows.Count + " rows";
        }
    }
}