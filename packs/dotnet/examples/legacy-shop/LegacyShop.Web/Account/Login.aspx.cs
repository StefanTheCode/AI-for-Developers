using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using LegacyShop.Data;

namespace LegacyShop.Web.Account
{
    // TRAP T17 + T18: Forms authentication with a hand-made ticket (role + customer id in userData),
    // encrypted with the machineKey from Web.config. ASP.NET Core can't read this cookie on its own.
    public partial class Login : Page
    {
        protected void LoginButton_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var customer = new CustomerRepository().Validate(EmailBox.Text.Trim(), PasswordBox.Text);
            if (customer == null)
            {
                ErrorLabel.Text = "Wrong email or password.";
                return;
            }

            var ticket = new FormsAuthenticationTicket(
                1,
                customer.DisplayName,
                DateTime.Now,
                DateTime.Now.AddHours(8),
                RememberMe.Checked,
                customer.Role + "|" + customer.Id);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Path = FormsAuthentication.FormsCookiePath
            };
            if (RememberMe.Checked) cookie.Expires = ticket.Expiration;
            Response.Cookies.Add(cookie);

            Response.Redirect(FormsAuthentication.GetRedirectUrl(customer.DisplayName, RememberMe.Checked));
        }
    }
}
