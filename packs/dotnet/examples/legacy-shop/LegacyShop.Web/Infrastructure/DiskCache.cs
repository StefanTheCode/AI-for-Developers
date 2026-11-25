using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Hosting;
using LegacyShop.Core;

namespace LegacyShop.Web.Infrastructure
{
    /// <summary>
    /// TRAP T14: BinaryFormatter. Insecure, obsolete, and removed in .NET 9+ (throws PlatformNotSupportedException).
    /// </summary>
    public static class DiskCache
    {
        public static T GetOrAdd<T>(string key, Func<T> factory) where T : class
        {
            var folder = HostingEnvironment.MapPath("~/" + AppSettings.CacheFolder.Replace('\\', '/'));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, Sanitize(key) + ".bin");

            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    var cached = new BinaryFormatter().Deserialize(stream) as T;
                    if (cached != null) return cached;
                }
            }

            var value = factory();
            using (var stream = File.Create(path))
            {
                new BinaryFormatter().Serialize(stream, value);
            }
            return value;
        }

        private static string Sanitize(string key)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                key = key.Replace(c, '_');
            }
            return key;
        }
    }
}
