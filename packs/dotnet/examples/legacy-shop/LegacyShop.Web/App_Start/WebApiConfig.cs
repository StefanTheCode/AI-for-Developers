using System.Web.Http;
using Newtonsoft.Json.Serialization;

namespace LegacyShop.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // TRAP T19: Web API 2 attribute routing + convention routes on GlobalConfiguration.
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            // TRAP T22: Newtonsoft.Json formatter settings. The XML formatter stays on, so
            // "Accept: application/xml" returns XML. ASP.NET Core returns JSON only by default.
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        }
    }
}
