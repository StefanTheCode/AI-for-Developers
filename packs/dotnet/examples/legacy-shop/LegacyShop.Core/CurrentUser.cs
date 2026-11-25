using System;
using System.Web;
using System.Web.Security;

namespace LegacyShop.Core
{
    /// <summary>
    /// Static helper used everywhere: pages, Web API, MVC, services.
    /// TRAP T12: a class library that reaches into HttpContext.Current. It can't move to .NET 10
    /// without the System.Web adapters or a real abstraction (IHttpContextAccessor / ICurrentUser).
    /// </summary>
    public static class CurrentUser
    {
        public static bool IsAuthenticated
        {
            get { return HttpContext.Current != null && HttpContext.Current.User.Identity.IsAuthenticated; }
        }

        public static int? CustomerId
        {
            get
            {
                if (!IsAuthenticated) return null;
                var identity = HttpContext.Current.User.Identity as FormsIdentity;
                if (identity == null) return null;
                // userData = "Role|CustomerId"
                var parts = identity.Ticket.UserData.Split('|');
                int id;
                return parts.Length == 2 && int.TryParse(parts[1], out id) ? id : (int?)null;
            }
        }

        public static string Name
        {
            get { return IsAuthenticated ? HttpContext.Current.User.Identity.Name : "Guest"; }
        }

        public static bool IsAdmin
        {
            get { return IsAuthenticated && HttpContext.Current.User.IsInRole("Admin"); }
        }

        public static string ClientIp
        {
            get
            {
                var request = HttpContext.Current.Request;
                var forwarded = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                return String.IsNullOrEmpty(forwarded) ? request.UserHostAddress : forwarded.Split(',')[0];
            }
        }
    }
}
