using System.Web.Mvc;
using System.Web.Routing;

namespace LegacyShop.Web
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{resource}.aspx/{*pathInfo}");
            routes.IgnoreRoute("{resource}.ashx/{*pathInfo}");

            // Only the reports are MVC. "/" stays Default.aspx (IIS default document).
            routes.MapRoute(
                name: "Reports",
                url: "Reports/{action}/{id}",
                defaults: new { controller = "Reports", action = "Index", id = UrlParameter.Optional });
        }
    }
}
