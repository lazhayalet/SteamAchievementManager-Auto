/* SAM Auto 9.0 — Free Games: local cache of the free list
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

namespace SAM.Picker.FreeGames
{
    /// <summary>
    /// Caches the fetched free list on disk so the window can show totals
    /// instantly and the store isn't hammered on every open.
    /// </summary>
    internal static class FreeAppsCache
    {
        private static readonly TimeSpan MaxAge = TimeSpan.FromHours(12);

        private static string CacheFile => Path.Combine(Lang.DataDirectory, "freeapps-cache.json");

        public static bool TryLoad(out List<FreeAppInfo> items, out DateTime fetchedAt)
        {
            items = null;
            fetchedAt = default;

            try
            {
                if (File.Exists(CacheFile) == false)
                {
                    return false;
                }

                var json = File.ReadAllText(CacheFile);
                var serializer = new JavaScriptSerializer { MaxJsonLength = 50 * 1024 * 1024 };
                var data = serializer.Deserialize<Dictionary<string, object>>(json);
                if (data == null ||
                    data.TryGetValue("fetchedAt", out var tsObj) == false ||
                    data.TryGetValue("items", out var itemsObj) == false ||
                    itemsObj is not System.Collections.ArrayList rawItems)
                {
                    return false;
                }

                if (long.TryParse(Convert.ToString(tsObj, CultureInfo.InvariantCulture), out var ticks) == false)
                {
                    return false;
                }

                fetchedAt = new DateTime(ticks, DateTimeKind.Utc);
                if (DateTime.UtcNow - fetchedAt > MaxAge)
                {
                    return false;
                }

                items = new();
                foreach (var rawItem in rawItems)
                {
                    if (rawItem is not Dictionary<string, object> item)
                    {
                        continue;
                    }

                    items.Add(new()
                    {
                        AppId = Convert.ToUInt32(item["appId"], CultureInfo.InvariantCulture),
                        Name = Convert.ToString(item["name"], CultureInfo.InvariantCulture),
                        CategoryKey = Convert.ToString(item["cat"], CultureInfo.InvariantCulture),
                    });
                }

                return items.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public static void Save(List<FreeAppInfo> items)
        {
            try
            {
                var rawItems = new List<Dictionary<string, object>>(items.Count);
                foreach (var item in items)
                {
                    rawItems.Add(new()
                    {
                        ["appId"] = item.AppId,
                        ["name"] = item.Name,
                        ["cat"] = item.CategoryKey,
                    });
                }

                var data = new Dictionary<string, object>
                {
                    ["fetchedAt"] = DateTime.UtcNow.Ticks,
                    ["items"] = rawItems,
                };

                var serializer = new JavaScriptSerializer { MaxJsonLength = 50 * 1024 * 1024 };
                File.WriteAllText(CacheFile, serializer.Serialize(data));
            }
            catch
            {
                // best effort
            }
        }
    }
}
