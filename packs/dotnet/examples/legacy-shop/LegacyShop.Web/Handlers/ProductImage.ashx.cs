using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using LegacyShop.Web.Infrastructure;

namespace LegacyShop.Web.Handlers
{
    /// <summary>
    /// TRAP T11: an .ashx IHttpHandler that draws images with System.Drawing (GDI+).
    /// On .NET 6+ System.Drawing.Common is Windows-only - this blocks Linux containers.
    /// </summary>
    public class ProductImage : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            int id;
            if (!int.TryParse(context.Request.QueryString["id"], out id) || id <= 0)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var bytes = DiskCache.GetOrAdd("product-image-" + id, () => Render(id));
            context.Response.ContentType = "image/png";
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.AddHours(1));
            context.Response.BinaryWrite(bytes);
        }

        private static byte[] Render(int id)
        {
            var palette = new[] { Color.FromArgb(124, 77, 255), Color.FromArgb(247, 168, 30), Color.FromArgb(74, 222, 128), Color.FromArgb(255, 107, 107) };
            using (var bitmap = new Bitmap(240, 160))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var font = new Font("Segoe UI", 28, FontStyle.Bold))
            using (var stream = new MemoryStream())
            {
                graphics.Clear(palette[id % palette.Length]);
                graphics.DrawString("#" + id, font, Brushes.White, 20, 50);
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}
