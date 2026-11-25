using System;
using System.Diagnostics;
using System.Web;

namespace LegacyShop.Web.Modules
{
    /// <summary>
    /// TRAP T10: an IHttpModule registered in Web.config (system.webServer/modules).
    /// In ASP.NET Core this becomes middleware.
    /// </summary>
    public class RequestTimingModule : IHttpModule
    {
        private const string Key = "LegacyShop.Stopwatch";

        public void Init(HttpApplication app)
        {
            app.BeginRequest += (s, e) => app.Context.Items[Key] = Stopwatch.StartNew();
            app.PreSendRequestHeaders += (s, e) =>
            {
                var watch = app.Context.Items[Key] as Stopwatch;
                if (watch != null)
                {
                    app.Context.Response.Headers["X-Response-Time-ms"] = watch.ElapsedMilliseconds.ToString();
                }
            };
        }

        public void Dispose()
        {
        }
    }
}
