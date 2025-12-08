using System;
using System.Web.UI.WebControls;
using Coupon.Helpers;

namespace Coupon
{
    /// <summary>
    /// Lotus brand coupon redemption page
    /// SECURITY: Uses parameterized queries and input validation via CouponPageBase
    /// </summary>
    public partial class Lotus : CouponPageBase
    {
        protected override void InitializeBrandSettings()
        {
            BrandName = "Lotus";
            ImagePath = "Lotus";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ProcessCouponPage(Image1, Image2, Image3, Label3, Button1, Panel1, Panel2);
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            RedirectToRedemption();
        }
    }
}
