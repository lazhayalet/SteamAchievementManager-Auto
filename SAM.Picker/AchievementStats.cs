/* SAM Auto 9.0 — local achievement progress from the Steam client cache
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SAM.Game;

namespace SAM.Picker
{
    /// <summary>
    /// Reads achievement totals and unlock progress from the local Steam client
    /// cache (appcache/stats) — instant, no network and no "now playing" noise:
    /// - UserGameStatsSchema_{appid}.bin  → how many achievements a game has
    /// - UserGameStats_{sid}_{appid}.bin  → AchievementTimes → how many are unlocked
    /// </summary>
    internal static class AchievementStats
    {
        private static readonly Regex UserStatsPattern = new(
            @"^UserGameStats_(\d+)_(\d+)\.bin$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <param name="accountId32">when &gt; 0, only user stats files of this account are considered
        /// (avoids mixing progress on multi-account machines)</param>
        public static Dictionary<uint, (int Total, int Unlocked)> Scan(string steamPath, uint accountId32 = 0)
        {
            var result = new Dictionary<uint, (int Total, int Unlocked)>();

            if (string.IsNullOrEmpty(steamPath) == true)
            {
                return result;
            }

            var dir = Path.Combine(steamPath, "appcache", "stats");
            if (Directory.Exists(dir) == false)
            {
                return result;
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(dir, "*.bin");
            }
            catch
            {
                return result;
            }

            // parse in parallel: tens of thousands of small files
            var bag = new ConcurrentBag<KeyValuePair<uint, (int Total, int Unlocked)>>();
            Parallel.ForEach(files, file =>
            {
                var name = Path.GetFileName(file);

                if (name.StartsWith("UserGameStatsSchema_", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var appIdText = name.Substring(
                        "UserGameStatsSchema_".Length,
                        name.Length - "UserGameStatsSchema_".Length - 4);

                    if (uint.TryParse(appIdText, out var appId) == false)
                    {
                        return;
                    }

                    int total = CountSchemaAchievements(file);
                    if (total > 0)
                    {
                        bag.Add(new(appId, (total, -1)));
                    }
                }
                else
                {
                    var match = UserStatsPattern.Match(name);
                    if (match.Success == false)
                    {
                        return;
                    }

                    if (accountId32 > 0 &&
                        uint.TryParse(match.Groups[1].Value, out var fileAccount) == true &&
                        fileAccount != accountId32)
                    {
                        return;
                    }

                    var appId = uint.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    int unlocked = CountUnlockedAchievements(file);
                    if (unlocked < 0)
                    {
                        return;
                    }

                    bag.Add(new(appId, (-1, unlocked)));
                }
            });

            foreach (var kv in bag)
            {
                if (result.TryGetValue(kv.Key, out var existing) == true)
                {
                    result[kv.Key] = (
                        Math.Max(existing.Total, kv.Value.Total),
                        Math.Max(existing.Unlocked, kv.Value.Unlocked));
                }
                else
                {
                    result[kv.Key] = kv.Value;
                }
            }

            return result;
        }

        /// <summary>Counts achievement bits in a stats schema file (same layout AutoUnlock uses).</summary>
        private static int CountSchemaAchievements(string path)
        {
            try
            {
                var kv = KeyValue.LoadAsBinary(path);
                if (kv == null || kv.Children == null || kv.Children.Count == 0)
                {
                    return -1;
                }

                var stats = kv.Children[0]["stats"];
                if (stats.Valid == false || stats.Children == null)
                {
                    return -1;
                }

                int count = 0;
                foreach (var stat in stats.Children)
                {
                    if (stat.Valid == false || stat.Children == null)
                    {
                        continue;
                    }

                    foreach (var bits in stat.Children)
                    {
                        if (string.Compare(bits.Name, "bits", StringComparison.InvariantCultureIgnoreCase) != 0 ||
                            bits.Children == null)
                        {
                            continue;
                        }

                        foreach (var bit in bits.Children)
                        {
                            if (string.IsNullOrEmpty(bit["name"].AsString("")) == false)
                            {
                                count++;
                            }
                        }
                    }
                }

                return count;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>Counts unlocked achievements via the AchievementTimes node of a user stats file.</summary>
        private static int CountUnlockedAchievements(string path)
        {
            try
            {
                var kv = KeyValue.LoadAsBinary(path);
                if (kv == null)
                {
                    return -1;
                }

                // AchievementTimes lives under cache → <section> (e.g. "1"), depth may vary
                var times = FindNode(kv["cache"], "AchievementTimes", 3);
                return times?.Children?.Count ?? 0;
            }
            catch
            {
                return -1;
            }
        }

        private static KeyValue FindNode(KeyValue node, string name, int depth)
        {
            if (node == null || node.Children == null || depth <= 0)
            {
                return null;
            }

            foreach (var child in node.Children)
            {
                if (string.Compare(child.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return child;
                }

                var found = FindNode(child, name, depth - 1);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
