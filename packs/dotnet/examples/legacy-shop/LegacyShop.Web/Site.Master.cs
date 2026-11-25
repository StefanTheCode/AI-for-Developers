using System;
using System.Web.Security;
using System.Web.UI;
using LegacyShop.Core;

namespace LegacyShop.Web
{
    // TRAP T05: master page with logic in code-behind (user name, admin link, sign out).
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            UserLabel.Text = "Hello, " + Server.HtmlEncode(CurrentUser.Name);
            LoginLink.Visible = !CurrentUser.IsAuthenticated;
            LogoutButton.Visible = CurrentUser.IsAuthenticated;
            AdminLink.Visible = CurrentUser.IsAdmin;
        }

        protected void LogoutButton_Click(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            Response.Redirect("~/Default.aspx");
        }
    }
}
