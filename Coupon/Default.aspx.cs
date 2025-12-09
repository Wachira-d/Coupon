using System;
using System.Web;

namespace Coupon
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Server-side redirect to Central.aspx
            Response.Redirect("~/Central.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
