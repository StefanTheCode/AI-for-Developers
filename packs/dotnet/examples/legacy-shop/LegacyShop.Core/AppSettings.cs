using System.Configuration;
using System.Globalization;

namespace LegacyShop.Core
{
    /// <summary>
    /// TRAP T13: ConfigurationManager.AppSettings read from Web.config, straight from a library.
    /// .NET 10 uses IConfiguration + IOptions; ConfigurationManager only works with the
    /// System.Configuration.ConfigurationManager package and an app.config, not appsettings.json.
    /// </summary>
    public static class AppSettings
    {
        public static decimal VatRate
        {
            get { return decimal.Parse(ConfigurationManager.AppSettings["Shop.VatRate"] ?? "0.20", CultureInfo.InvariantCulture); }
        }

        public static decimal FreeShippingFrom
        {
            get { return decimal.Parse(ConfigurationManager.AppSettings["Shop.FreeShippingFrom"] ?? "100", CultureInfo.InvariantCulture); }
        }

        public static string InventoryServiceUrl
        {
            get { return ConfigurationManager.AppSettings["Inventory.Url"]; }
        }

        public static string CacheFolder
        {
            get { return ConfigurationManager.AppSettings["Shop.CacheFolder"] ?? "App_Data\\cache"; }
        }
    }
}
