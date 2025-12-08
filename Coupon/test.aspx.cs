using System;
using System.Web.UI.WebControls;
using Coupon.Helpers;

namespace Coupon
{
    /// <summary>
    /// Test/generic coupon redemption page
    /// SECURITY: Uses parameterized queries and input validation via CouponPageBase
    /// WARNING: This page should be removed or disabled in production environments
    /// </summary>
    public partial class test : CouponPageBase
    {
        protected override void InitializeBrandSettings()
        {
            BrandName = "Test";
            ImagePath = ""; // Uses root Images folder (./Images/200.png)
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // WARNING: Log that test page is being accessed
            ErrorLogger.LogWarning("Test page accessed - should be disabled in production", $"IP: {Request.UserHostAddress}, URL: {Request.Url}");

            ProcessCouponPage(Image1, Image2, Image3, Label3, Button1, Panel1, Panel2);
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            RedirectToRedemption();
        }
    }
}
