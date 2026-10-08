/* SAM Auto 9.0 — Free Games: store web session for 100%-off deals
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace SAM.Picker.FreeGames
{
    /// <summary>
    /// Uses the access token from the QR sign-in as a store.steampowered.com
    /// session (steamLoginSecure cookie) so 100%-off packages can be claimed
    /// through the store's own checkout endpoint — the same flow the Steam
    /// store website itself uses.
    /// </summary>
    internal sealed class StoreWebSession
    {
        public enum ClaimResult
        {
            Ok,
            AlreadyOwned,
            RateLimited,
            Failed,
        }

        private readonly ulong _steamId;
        private readonly string _accessToken;
        private readonly CookieContainer _cookies = new();
        private string _sessionId;

        public StoreWebSession(ulong steamId, string accessToken)
        {
            this._steamId = steamId;
            this._accessToken = accessToken;

            this._cookies.Add(new Cookie(
                "steamLoginSecure",
                $"{steamId.ToString(CultureInfo.InvariantCulture)}%7C%7C{accessToken}",
                "/",
                ".steampowered.com"));
        }

        /// <summary>Opens the store homepage once to obtain a sessionid cookie.</summary>
        public bool Initialize()
        {
            try
            {
                var request = CreateRequest("https://store.steampowered.com/", "GET");
                using (request.GetResponse())
                {
                    // response cookies land in the container
                }

                var cookie = this._cookies.GetCookies(new("https://store.steampowered.com"))["sessionid"];
                this._sessionId = cookie?.Value;
                return string.IsNullOrEmpty(this._sessionId) == false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Claims a 0-price package through the store checkout.</summary>
        public ClaimResult ClaimFreePackage(uint packageId)
        {
            try
            {
                var request = CreateRequest(
                    $"https://store.steampowered.com/freelicense/addfreelicense/{packageId.ToString(CultureInfo.InvariantCulture)}",
                    "POST");

                var body = Encoding.UTF8.GetBytes(
                    "sessionid=" + Uri.EscapeDataString(this._sessionId ?? string.Empty) + "&ajax=true");
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = body.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(body, 0, body.Length);
                }

                string responseText;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    responseText = reader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(responseText) == true)
                {
                    return ClaimResult.Failed;
                }

                var serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<Dictionary<string, object>>(responseText);
                if (data == null)
                {
                    // sometimes it answers with "[]" or plain text
                    return responseText.IndexOf("rate limited", StringComparison.OrdinalIgnoreCase) >= 0
                        ? ClaimResult.RateLimited
                        : ClaimResult.Ok;
                }

                if (data.TryGetValue("error", out var errorObj) == true &&
                    errorObj is string error &&
                    string.IsNullOrEmpty(error) == false)
                {
                    if (error.IndexOf("rate limited", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return ClaimResult.RateLimited;
                    }

                    return ClaimResult.Failed;
                }

                // purchaseresultdetail: 0 = NoDetail (success), 9 = AlreadyOwned
                if (data.TryGetValue("purchaseresultdetail", out var detailObj) == true)
                {
                    var detail = Convert.ToInt32(detailObj, CultureInfo.InvariantCulture);
                    return detail == 9 ? ClaimResult.AlreadyOwned : ClaimResult.Ok;
                }

                return ClaimResult.Ok;
            }
            catch (WebException e)
            {
                if (e.Response is HttpWebResponse response &&
                    (int)response.StatusCode == 429)
                {
                    return ClaimResult.RateLimited;
                }

                return ClaimResult.Failed;
            }
            catch
            {
                return ClaimResult.Failed;
            }
        }

        private HttpWebRequest CreateRequest(string url, string method)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.CookieContainer = this._cookies;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) SAM-Auto/9.0";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }
    }

    /// <summary>Resolves a store app to its currently free (0-price) package id.</summary>
    internal static class DealPackageResolver
    {
        /// <summary>Returns the free package id for a 100%-off app, or null when none is free right now.</summary>
        public static uint? ResolveFreePackage(uint appId)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(
                    $"https://store.steampowered.com/api/appdetails?appids={appId.ToString(CultureInfo.InvariantCulture)}&filters=packages");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) SAM-Auto/9.0";

                string json;
                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    json = reader.ReadToEnd();
                }

                var serializer = new JavaScriptSerializer { MaxJsonLength = 10 * 1024 * 1024 };
                var root = serializer.Deserialize<Dictionary<string, object>>(json);
                if (root == null ||
                    root.TryGetValue(appId.ToString(CultureInfo.InvariantCulture), out var appObj) == false ||
                    appObj is not Dictionary<string, object> app ||
                    app.TryGetValue("success", out var successObj) == false ||
                    successObj is not bool success || success == false ||
                    app.TryGetValue("data", out var dataObj) == false ||
                    dataObj is not Dictionary<string, object> data ||
                    data.TryGetValue("package_groups", out var groupsObj) == false ||
                    groupsObj is not System.Collections.ArrayList groups)
                {
                    return null;
                }

                foreach (var groupObj in groups)
                {
                    if (groupObj is not Dictionary<string, object> group ||
                        group.TryGetValue("subs", out var subsObj) == false ||
                        subsObj is not System.Collections.ArrayList subs)
                    {
                        continue;
                    }

                    foreach (var subObj in subs)
                    {
                        if (subObj is not Dictionary<string, object> sub ||
                            sub.TryGetValue("packageid", out var idObj) == false)
                        {
                            continue;
                        }

                        int priceCents = 0;
                        if (sub.TryGetValue("price_in_cents_with_discount", out var priceObj) == true &&
                            priceObj != null)
                        {
                            priceCents = Convert.ToInt32(priceObj, CultureInfo.InvariantCulture);
                        }

                        if (priceCents == 0)
                        {
                            return Convert.ToUInt32(idObj, CultureInfo.InvariantCulture);
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
