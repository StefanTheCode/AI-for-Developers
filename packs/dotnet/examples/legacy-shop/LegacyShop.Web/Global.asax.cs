using System;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using LegacyShop.Data;

namespace LegacyShop.Web
{
    // TRAP T09: Global.asax lifecycle events + Application state. ASP.NET Core has Program.cs,
    // middleware and DI instead - and no Session_End at all.
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            Database.SetInitializer(new ShopInitializer());

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            Application["StartedAt"] = DateTime.Now;
            Application["ActiveSessions"] = 0;
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            Application.Lock();
            Application["ActiveSessions"] = (int)Application["ActiveSessions"] + 1;
            Application.UnLock();
            Session["CreatedAt"] = DateTime.Now;
        }

        protected void Session_End(object sender, EventArgs e)
        {
            // Only fires for InProc session state.
            Application.Lock();
            Application["ActiveSessions"] = Math.Max(0, (int)Application["ActiveSessions"] - 1);
            Application.UnLock();
        }

        // TRAP T17: roles live in the Forms auth ticket's userData and are turned into a principal here.
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            if (Context.User == null || !Context.User.Identity.IsAuthenticated) return;
            var identity = Context.User.Identity as FormsIdentity;
            if (identity == null) return;
            var role = identity.Ticket.UserData.Split('|')[0];
            Context.User = new GenericPrincipal(identity, new[] { role });
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            if (ex == null) return;
            Trace.TraceError(ex.ToString());
            try
            {
                var folder = HostingEnvironment.MapPath("~/App_Data");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "errors.log"),
                    DateTime.Now.ToString("s") + " " + Request.RawUrl + Environment.NewLine + ex + Environment.NewLine);
            }
            catch (IOException)
            {
                // Never let logging take the site down.
            }
        }
    }
}
