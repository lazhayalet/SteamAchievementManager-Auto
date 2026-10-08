/* SAM Auto 9.0 — Free Games: store search fetcher
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace SAM.Picker.FreeGames
{
    internal sealed class FreeAppInfo
    {
        public uint AppId;
        public string Name;
        public string CategoryKey;
    }

    /// <summary>
    /// Fetches permanently-free store items per category from the public
    /// store search endpoint (no authentication needed).
    /// Handles store rate limiting (HTTP 429) with automatic backoff.
    /// </summary>
    internal static class FreeAppsFetcher
    {
        internal sealed class Category
        {
            public string Key;
            public string LabelKey;
            public string QueryParam;

            public Category(string key, string labelKey, string queryParam)
            {
                this.Key = key;
                this.LabelKey = labelKey;
                this.QueryParam = queryParam;
            }
        }

        public static readonly Category[] Categories =
        {
            new("games", "fg.catGames", "category1=998"),
            new("dlc", "fg.catDlc", "category1=21"),
            new("software", "fg.catSoftware", "category1=994"),
            new("demos", "fg.catDemos", "category1=10"),
            new("mods", "fg.catMods", "category1=997"),
            new("deals", "fg.catDeals", "specials=1"), // 100%-off (free to keep) promos
        };

        private static readonly Regex AppIdPattern = new(@"/apps/(\d+)/", RegexOptions.Compiled);

        private const int PageSize = 25; // the endpoint ignores count and returns 25 per page
        private const int MaxPages = 600; // safety cap (15k items per category)
        private static readonly TimeSpan BetweenPages = TimeSpan.FromMilliseconds(400);
        private static readonly TimeSpan RateLimitBackoff = TimeSpan.FromSeconds(10);
        private const int MaxRateLimitRetries = 6;

        /// <summary>
        /// Downloads the full free list for one category.
        /// </summary>
        /// <param name="onPage">called after every page with the 1-based page number</param>
        /// <param name="onRateLimit">called when the store rate-limits us (waiting + retrying)</param>
        public static List<FreeAppInfo> FetchCategory(
            Category category,
            Action<int> onPage,
            Action onRateLimit,
            CancellationToken cancellation)
        {
            var results = new List<FreeAppInfo>();
            var seen = new HashSet<uint>();
            var serializer = new JavaScriptSerializer { MaxJsonLength = 50 * 1024 * 1024 };

            for (int page = 0; page < MaxPages; page++)
            {
                cancellation.ThrowIfCancellationRequested();
                onPage?.Invoke(page + 1);

                var url = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://store.steampowered.com/search/results/?query&start={0}&count={1}&json=1&maxprice=free&{2}",
                    page * PageSize,
                    PageSize,
                    category.QueryParam);

                string json = null;
                for (int attempt = 0; ; attempt++)
                {
                    try
                    {
                        using (var client = CreateClient())
                        {
                            json = client.DownloadString(url);
                        }

                        break;
                    }
                    catch (WebException e) when (IsRateLimited(e) && attempt < MaxRateLimitRetries)
                    {
                        onRateLimit?.Invoke();
                        cancellation.WaitHandle.WaitOne(RateLimitBackoff);
                        cancellation.ThrowIfCancellationRequested();
                    }
                }

                if (json == null)
                {
                    break; // gave up on this page; return what we have
                }

                var data = serializer.Deserialize<Dictionary<string, object>>(json);
                if (data == null ||
                    data.TryGetValue("items", out var itemsObj) == false ||
                    itemsObj is not System.Collections.ArrayList items ||
                    items.Count == 0)
                {
                    break;
                }

                foreach (var itemObj in items)
                {
                    if (itemObj is not Dictionary<string, object> item)
                    {
                        continue;
                    }

                    var name = item.TryGetValue("name", out var nameObj) ? nameObj as string : null;
                    var logo = item.TryGetValue("logo", out var logoObj) ? logoObj as string : null;
                    if (string.IsNullOrEmpty(logo) == true)
                    {
                        continue;
                    }

                    var match = AppIdPattern.Match(logo);
                    if (match.Success == false)
                    {
                        continue;
                    }

                    var appId = uint.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (seen.Add(appId) == false)
                    {
                        continue;
                    }

                    results.Add(new()
                    {
                        AppId = appId,
                        Name = WebUtility.HtmlDecode(name ?? appId.ToString(CultureInfo.InvariantCulture)),
                        CategoryKey = category.Key,
                    });
                }

                if (items.Count < PageSize)
                {
                    break; // last page
                }

                cancellation.WaitHandle.WaitOne(BetweenPages); // be polite to the store
            }

            return results;
        }

        private static bool IsRateLimited(WebException e)
        {
            return e.Response is HttpWebResponse response &&
                   (response.StatusCode == (HttpStatusCode)429 || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }

        private static WebClient CreateClient()
        {
            var client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) SAM-Auto/9.0";
            client.Encoding = System.Text.Encoding.UTF8;
            return client;
        }
    }
}
