using System;
using System.Web;
using System.Web.UI;
using Coupon.Helpers;

namespace Coupon.Admin
{
    public partial class Logout : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Get username before clearing session
            string username = Session["Username"]?.ToString() ?? "Unknown";
            string ipAddress = GetClientIpAddress();

            // Clear session
            Session.Clear();
            Session.Abandon();

            // Clear authentication cookies
            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                HttpCookie sessionCookie = new HttpCookie("ASP.NET_SessionId");
                sessionCookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(sessionCookie);
            }

            // Log logout
            AuditLogger.LogUserActivity(
                username,
                "User Logout",
                "User logged out",
                $"IP: {ipAddress}",
                ipAddress
            );

            // Redirect to login page
            Response.Redirect("~/Admin/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private string GetClientIpAddress()
        {
            string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            }
            else
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length > 0)
                {
                    ipAddress = addresses[0].Trim();
                }
            }

            return ipAddress ?? "Unknown";
        }
    }
}
