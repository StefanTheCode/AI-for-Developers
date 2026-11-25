using System.Web.Optimization;

namespace LegacyShop.Web
{
    // TRAP T15: System.Web.Optimization bundling does not exist in ASP.NET Core.
    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/js").Include("~/Scripts/site.js"));
            bundles.Add(new StyleBundle("~/bundles/css").Include("~/Content/site.css"));
        }
    }
}
