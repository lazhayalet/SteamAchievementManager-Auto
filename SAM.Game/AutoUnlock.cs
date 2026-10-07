/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace SAM.Game
{
    /// <summary>
    /// Headless mode: unlocks every achievement for a single game and exits.
    /// Used by the Picker's "Unlock All" / "Unlock Selected" automation.
    /// </summary>
    internal static class AutoUnlock
    {
        private static string LogFilePath
        {
            get
            {
                try
                {
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SAM_AutoUnlock.log");
                    File.AppendAllText(path, string.Empty);
                    return path;
                }
                catch
                {
                    return Path.Combine(Path.GetTempPath(), "SAM_AutoUnlock.log");
                }
            }
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    LogFilePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch
            {
                // logging must never break the automation
            }
        }

        public static int Run(long appId)
        {
            Log($"=== Starting auto-unlock for app {appId} ===");

            using (API.Client client = new())
            {
                try
                {
                    client.Initialize(appId);
                }
                catch (Exception e)
                {
                    Log($"ERROR: failed to initialize client for app {appId}: {e.Message}");
                    return 1;
                }

                bool gotResult = false;
                bool statsOk = false;

                var userStatsReceived = client.CreateAndRegisterCallback<API.Callbacks.UserStatsReceived>();
                userStatsReceived.OnRun += param =>
                {
                    gotResult = true;
                    statsOk = param.Result == 1;
                };

                try
                {
                    var steamId = client.SteamUser.GetSteamId();
                    client.SteamUserStats.RequestUserStats(steamId);
                }
                catch (Exception e)
                {
                    Log($"ERROR: failed to request stats for app {appId}: {e.Message}");
                    return 1;
                }

                var watch = Stopwatch.StartNew();
                while (gotResult == false && watch.ElapsedMilliseconds < 20000)
                {
                    client.RunCallbacks(false);
                    Thread.Sleep(50);
                }

                if (gotResult == false || statsOk == false)
                {
                    Log($"ERROR: could not retrieve stats for app {appId} (received={gotResult}).");
                    return 1;
                }

                var achievementIds = GetAchievementIds(appId, client);
                if (achievementIds.Count == 0)
                {
                    Log($"App {appId}: no achievements found in schema, nothing to unlock.");
                    return 0;
                }

                foreach (var id in achievementIds)
                {
                    try
                    {
                        client.SteamUserStats.SetAchievement(id, true);
                    }
                    catch (Exception e)
                    {
                        Log($"ERROR: failed to set achievement '{id}' for app {appId}: {e.Message}");
                    }
                }

                try
                {
                    bool stored = client.SteamUserStats.StoreStats();
                    Log($"App {appId}: unlocked {achievementIds.Count} achievements, stored={stored}.");
                }
                catch (Exception e)
                {
                    Log($"ERROR: failed to store stats for app {appId}: {e.Message}");
                    return 1;
                }

                // give Steam a moment to flush the stored stats to the backend
                watch.Restart();
                while (watch.ElapsedMilliseconds < 1000)
                {
                    client.RunCallbacks(false);
                    Thread.Sleep(50);
                }
            }

            Log($"=== Finished auto-unlock for app {appId} ===");
            return 0;
        }

        private static List<string> GetAchievementIds(long appId, API.Client client)
        {
            var ids = new List<string>();

            string path;
            try
            {
                path = API.Steam.GetInstallPath();
                if (string.IsNullOrEmpty(path) == true)
                {
                    return ids;
                }

                path = Path.Combine(path, "appcache", "stats", $"UserGameStatsSchema_{appId.ToString(CultureInfo.InvariantCulture)}.bin");
                if (File.Exists(path) == false)
                {
                    Log($"Schema file not found: {path}");
                    return ids;
                }
            }
            catch (Exception e)
            {
                Log($"ERROR: failed to locate schema for app {appId}: {e.Message}");
                return ids;
            }

            try
            {
                var kv = KeyValue.LoadAsBinary(path);
                if (kv == null)
                {
                    return ids;
                }

                var stats = kv[appId.ToString(CultureInfo.InvariantCulture)]["stats"];
                if (stats.Valid == false || stats.Children == null)
                {
                    return ids;
                }

                foreach (var stat in stats.Children)
                {
                    if (stat.Valid == false || stat.Children == null)
                    {
                        continue;
                    }

                    foreach (var bits in stat.Children.Where(
                        b => string.Compare(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase) == 0))
                    {
                        if (bits.Valid == false || bits.Children == null)
                        {
                            continue;
                        }

                        foreach (var bit in bits.Children)
                        {
                            string id = bit["name"].AsString("");
                            if (string.IsNullOrEmpty(id) == false && ids.Contains(id) == false)
                            {
                                ids.Add(id);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log($"ERROR: failed to parse schema for app {appId}: {e.Message}");
            }

            return ids;
        }
    }
}
