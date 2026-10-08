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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;
using static SAM.Picker.InvariantShorthand;
using SAM.Common;
using SAM.Picker.FreeGames;
using APITypes = SAM.API.Types;

namespace SAM.Picker
{
    internal partial class GamePicker : Form
    {
        private readonly API.Client _SteamClient;

        private readonly Dictionary<uint, GameInfo> _Games;
        private readonly List<GameInfo> _FilteredGames;

        private readonly object _LogoLock;
        private readonly HashSet<string> _LogosAttempting;
        private readonly HashSet<string> _LogosAttempted;
        private readonly ConcurrentQueue<GameInfo> _LogoQueue;

        private readonly API.Callbacks.AppDataChanged _AppDataChangedCallback;

        private enum SortMode
        {
            Name,
            LockedFirst,
            UnlockedFirst,
        }

        private SortMode _sortMode = SortMode.Name;
        private FreeGamesForm _freeGamesForm;
        private readonly ManualResetEventSlim _unlockPauseEvent = new(true); // signaled = running
        private int _unlockCompletedCount;
        private int _unlockTotalCount;

        public GamePicker(API.Client client)
        {
            this._Games = new();
            this._FilteredGames = new();
            this._LogoLock = new();
            this._LogosAttempting = new();
            this._LogosAttempted = new();
            this._LogoQueue = new();

            this.InitializeComponent();

            this.RebuildBlankLogo();

            this._SteamClient = client;

            this.ReapplyTheme();
            Lang.Changed += this.ApplyLanguage;
            this.ApplyLanguage();
            this.FormClosed += (_, _) => Lang.Changed -= this.ApplyLanguage;

            this._AppDataChangedCallback = client.CreateAndRegisterCallback<API.Callbacks.AppDataChanged>();
            this._AppDataChangedCallback.OnRun += this.OnAppDataChanged;

            this.AddGames();
        }

        private void ApplyLanguage()
        {
            this.Text = Lang.T("picker.title");
            this._RefreshGamesButton.Text = Lang.T("picker.refresh");
            this._AddGameButton.Text = Lang.T("picker.addGame");
            this._UnlockAllButton.Text = Lang.T("picker.unlockAll");
            this._UnlockSelectedButton.Text = Lang.T("picker.unlockSelected");
            this._FreeGamesButton.Text = Lang.T("picker.freeGames");
            this._FindGamesLabel.Text = Lang.T("picker.filter");
            this._FilterGamesMenuItem.Text = Lang.T("picker.showGames");
            this._FilterDemosMenuItem.Text = Lang.T("picker.showDemos");
            this._FilterModsMenuItem.Text = Lang.T("picker.showMods");
            this._FilterJunkMenuItem.Text = Lang.T("picker.showJunk");
            this._SortDropDown.Text = Lang.T("picker.sort");
            this._SortNameItem.Text = Lang.T("picker.sortName");
            this._SortLockedFirstItem.Text = Lang.T("picker.sortLockedFirst");
            this._SortUnlockedFirstItem.Text = Lang.T("picker.sortUnlockedFirst");
            this._PauseUnlockButton.Text = this._unlockPauseEvent.IsSet == true
                ? Lang.T("picker.pause")
                : Lang.T("picker.resume");
            this._StopUnlockButton.Text = Lang.T("picker.stop");
            this._ThemeDropDown.Text = Lang.T("picker.theme");
            this._ThemeDarkItem.Text = Lang.T("picker.themeDark");
            this._ThemeLightItem.Text = Lang.T("picker.themeLight");
            this._LangTurkishItem.Checked = Lang.Current == Lang.Turkish;
            this._LangEnglishItem.Checked = Lang.Current == Lang.English;

            if (this._Games.Count > 0)
            {
                this._PickerStatusLabel.Text = Lang.F(
                    "picker.displaying",
                    this._GameListView.Items.Count,
                    this._Games.Count);
            }
        }

        private void OnLangTurkish(object sender, EventArgs e)
        {
            Lang.SetLanguage(Lang.Turkish);
        }

        private void OnLangEnglish(object sender, EventArgs e)
        {
            Lang.SetLanguage(Lang.English);
        }

        private void OnThemeDark(object sender, EventArgs e)
        {
            this.SetThemeMode(GhostMode.Dark);
        }

        private void OnThemeLight(object sender, EventArgs e)
        {
            this.SetThemeMode(GhostMode.Light);
        }

        private void SetThemeMode(GhostMode mode)
        {
            Lang.SetTheme(mode == GhostMode.Dark ? "dark" : "light");
            GhostTheme.SetMode(mode); // raises Changed (Free Games window listens)
            this.ReapplyTheme();
        }

        private void ReapplyTheme()
        {
            GhostTheme.Apply(this);
            this._UnlockAllButton.ForeColor = GhostTheme.Accent;
            this._UnlockSelectedButton.ForeColor = GhostTheme.Accent;
            this._FreeGamesButton.ForeColor = GhostTheme.Accent;
            this._StopUnlockButton.ForeColor = GhostTheme.Danger;
            this._AddGameTextBox.BackColor = GhostTheme.Input;
            this._AddGameTextBox.ForeColor = GhostTheme.Text;
            this._SearchGameTextBox.BackColor = GhostTheme.Input;
            this._SearchGameTextBox.ForeColor = GhostTheme.Text;
            this._ThemeDarkItem.Checked = GhostTheme.Mode == GhostMode.Dark;
            this._ThemeLightItem.Checked = GhostTheme.Mode == GhostMode.Light;
            this.RebuildBlankLogo();
            this.RefreshGames();
        }

        private void RebuildBlankLogo()
        {
            Bitmap blank = new(this._LogoImageList.ImageSize.Width, this._LogoImageList.ImageSize.Height);
            using (var g = Graphics.FromImage(blank))
            {
                g.Clear(GhostTheme.SurfaceAlt);
            }

            if (this._LogoImageList.Images.Count == 0)
            {
                this._LogoImageList.Images.Add("Blank", blank);
            }
            else
            {
                this._LogoImageList.Images[0] = blank;
            }
        }

        private void OnFreeGames(object sender, EventArgs e)
        {
            // non-modal: the main window stays usable while Free Games is open
            if (this._freeGamesForm == null || this._freeGamesForm.IsDisposed == true)
            {
                this._freeGamesForm = new FreeGamesForm();
                this._freeGamesForm.Show(this);
            }
            else
            {
                this._freeGamesForm.WindowState = FormWindowState.Normal;
                this._freeGamesForm.Focus();
            }
        }

        private void OnSortMode(object sender, EventArgs e)
        {
            SortMode mode;
            if (sender == this._SortLockedFirstItem)
            {
                mode = SortMode.LockedFirst;
            }
            else if (sender == this._SortUnlockedFirstItem)
            {
                mode = SortMode.UnlockedFirst;
            }
            else
            {
                mode = SortMode.Name;
            }

            if (this._sortMode == mode)
            {
                return;
            }

            this._sortMode = mode;
            this._SortNameItem.Checked = mode == SortMode.Name;
            this._SortLockedFirstItem.Checked = mode == SortMode.LockedFirst;
            this._SortUnlockedFirstItem.Checked = mode == SortMode.UnlockedFirst;
            this.RefreshGames();
        }

        private static double ProgressRatioKey(GameInfo info, double unknownValue)
        {
            if (info.TotalAchievements <= 0 || info.UnlockedAchievements < 0)
            {
                return unknownValue;
            }

            return info.UnlockedAchievements / (double)info.TotalAchievements;
        }

        private void StartAchievementScan()
        {
            var steamPath = API.Steam.GetInstallPath();
            if (string.IsNullOrEmpty(steamPath) == true)
            {
                return;
            }

            this._PickerStatusLabel.Text = Lang.T("picker.scanningAchievements");

            uint accountId32 = 0;
            try
            {
                accountId32 = (uint)(this._SteamClient.SteamUser.GetSteamId() & 0xFFFFFFFF);
            }
            catch
            {
                // fall back to unfiltered scan
            }

            uint accountId = accountId32;
            Task.Run(() =>
            {
                var stats = AchievementStats.Scan(steamPath, accountId);
                try
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        foreach (var kv in stats)
                        {
                            if (this._Games.TryGetValue(kv.Key, out var info) == true)
                            {
                                info.TotalAchievements = kv.Value.Total;
                                info.UnlockedAchievements = kv.Value.Unlocked;
                            }
                        }

                        this.RefreshGames();
                    }));
                }
                catch
                {
                    // form already closed
                }
            });
        }

        private void OnAppDataChanged(APITypes.AppDataChanged param)
        {
            if (param.Result == false)
            {
                return;
            }

            if (this._Games.TryGetValue(param.Id, out var game) == false)
            {
                return;
            }

            game.Name = this._SteamClient.SteamApps001.GetAppData(game.Id, "name");

            this.AddGameToLogoQueue(game);
            this.DownloadNextLogo();
        }

        private void DoDownloadList(object sender, DoWorkEventArgs e)
        {
            this._PickerStatusLabel.Text = Lang.T("picker.downloadingList");

            byte[] bytes;
            using (WebClient downloader = new())
            {
                bytes = downloader.DownloadData(new Uri("https://gib.me/sam/games.xml"));
            }

            List<KeyValuePair<uint, string>> pairs = new();
            using (MemoryStream stream = new(bytes, false))
            {
                XPathDocument document = new(stream);
                var navigator = document.CreateNavigator();
                var nodes = navigator.Select("/games/game");
                while (nodes.MoveNext() == true)
                {
                    string type = nodes.Current.GetAttribute("type", "");
                    if (string.IsNullOrEmpty(type) == true)
                    {
                        type = "normal";
                    }
                    pairs.Add(new((uint)nodes.Current.ValueAsLong, type));
                }
            }

            this._PickerStatusLabel.Text = Lang.T("picker.checkingOwnership");
            foreach (var kv in pairs)
            {
                this.AddGame(kv.Key, kv.Value);
            }
        }

        private void OnDownloadList(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled == true)
            {
                this.AddDefaultGames();
                MessageBox.Show(e.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.RefreshGames();
            this._RefreshGamesButton.Enabled = true;
            this.DownloadNextLogo();
            this.StartAchievementScan();
        }

        private void RefreshGames()
        {
            var nameSearch = this._SearchGameTextBox.Text.Length > 0
                ? this._SearchGameTextBox.Text
                : null;

            var wantNormals = this._FilterGamesMenuItem.Checked == true;
            var wantDemos = this._FilterDemosMenuItem.Checked == true;
            var wantMods = this._FilterModsMenuItem.Checked == true;
            var wantJunk = this._FilterJunkMenuItem.Checked == true;

            var filtered = new List<GameInfo>();
            foreach (var info in this._Games.Values)
            {
                if (nameSearch != null &&
                    info.Name.IndexOf(nameSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool wanted = info.Type switch
                {
                    "normal" => wantNormals,
                    "demo" => wantDemos,
                    "mod" => wantMods,
                    "junk" => wantJunk,
                    _ => true,
                };
                if (wanted == false)
                {
                    continue;
                }

                filtered.Add(info);
            }

            IEnumerable<GameInfo> ordered = this._sortMode switch
            {
                SortMode.LockedFirst => filtered
                    .OrderBy(gi => ProgressRatioKey(gi, double.MaxValue))
                    .ThenBy(gi => gi.Name),
                SortMode.UnlockedFirst => filtered
                    .OrderByDescending(gi => ProgressRatioKey(gi, -1.0))
                    .ThenBy(gi => gi.Name),
                _ => filtered.OrderBy(gi => gi.Name),
            };

            this._FilteredGames.Clear();
            this._FilteredGames.AddRange(ordered);

            // reset to force item re-creation (label text / cell size refresh)
            this._GameListView.VirtualListSize = 0;
            this._GameListView.VirtualListSize = this._FilteredGames.Count;
            this._PickerStatusLabel.Text = Lang.F(
                "picker.displaying",
                this._GameListView.Items.Count,
                this._Games.Count);

            if (this._GameListView.Items.Count > 0)
            {
                this._GameListView.Items[0].Selected = true;
                this._GameListView.Select();
            }
        }

        private void OnGameListViewRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var info = this._FilteredGames[e.ItemIndex];
            e.Item = info.Item = new()
            {
                Text = FormatItemText(info),
                ImageIndex = info.ImageIndex,
            };
        }

        /// <summary>Two-line label (name + achievement progress) so cells grow taller.</summary>
        private static string FormatItemText(GameInfo info)
        {
            if (info.TotalAchievements <= 0)
            {
                return info.Name;
            }

            var unlocked = info.UnlockedAchievements >= 0
                ? info.UnlockedAchievements.ToString(CultureInfo.InvariantCulture)
                : "—";

            return $"{info.Name}\n{unlocked}/{info.TotalAchievements}";
        }

        private void OnGameListViewSearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            if (e.Direction != SearchDirectionHint.Down || e.IsTextSearch == false)
            {
                return;
            }

            var count = this._FilteredGames.Count;
            if (count < 2)
            {
                return;
            }

            var text = e.Text;
            int startIndex = e.StartIndex;

            Predicate<GameInfo> predicate;
            /*if (e.IsPrefixSearch == true)*/
            {
                predicate = gi => gi.Name != null && gi.Name.StartsWith(text, StringComparison.CurrentCultureIgnoreCase);
            }
            /*else
            {
                predicate = gi => gi.Name != null && string.Compare(gi.Name, text, StringComparison.CurrentCultureIgnoreCase) == 0;
            }*/

            int index;
            if (e.StartIndex >= count)
            {
                // starting from the last item in the list
                index = this._FilteredGames.FindIndex(0, startIndex - 1, predicate);
            }
            else if (startIndex <= 0)
            {
                // starting from the first item in the list
                index = this._FilteredGames.FindIndex(0, count, predicate);
            }
            else
            {
                index = this._FilteredGames.FindIndex(startIndex, count - startIndex, predicate);
                if (index < 0)
                {
                    index = this._FilteredGames.FindIndex(0, startIndex - 1, predicate);
                }
            }

            e.Index = index < 0 ? -1 : index;
        }

        private void DoDownloadLogo(object sender, DoWorkEventArgs e)
        {
            var info = (GameInfo)e.Argument;

            this._LogosAttempted.Add(info.ImageUrl);

            using (WebClient downloader = new())
            {
                try
                {
                    var data = downloader.DownloadData(new Uri(info.ImageUrl));
                    using (MemoryStream stream = new(data, false))
                    {
                        Bitmap bitmap = new(stream);
                        e.Result = new LogoInfo(info.Id, bitmap);
                    }
                }
                catch (Exception)
                {
                    e.Result = new LogoInfo(info.Id, null);
                }
            }
        }

        private void OnDownloadLogo(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled == true)
            {
                return;
            }

            if (e.Result is LogoInfo logoInfo &&
                logoInfo.Bitmap != null &&
                this._Games.TryGetValue(logoInfo.Id, out var gameInfo) == true)
            {
                this._GameListView.BeginUpdate();
                var imageIndex = this._LogoImageList.Images.Count;
                this._LogoImageList.Images.Add(gameInfo.ImageUrl, logoInfo.Bitmap);
                gameInfo.ImageIndex = imageIndex;
                this._GameListView.EndUpdate();
            }

            this.DownloadNextLogo();
        }

        private void DownloadNextLogo()
        {
            lock (this._LogoLock)
            {

                if (this._LogoWorker.IsBusy == true)
                {
                    return;
                }

                GameInfo info;
                while (true)
                {
                    if (this._LogoQueue.TryDequeue(out info) == false)
                    {
                        this._DownloadStatusLabel.Visible = false;
                        return;
                    }

                    if (info.Item == null)
                    {
                        continue;
                    }

                    if (this._FilteredGames.Contains(info) == false ||
                        info.Item.Bounds.IntersectsWith(this._GameListView.ClientRectangle) == false)
                    {
                        this._LogosAttempting.Remove(info.ImageUrl);
                        continue;
                    }

                    break;
                }

                this._DownloadStatusLabel.Text = Lang.F("picker.downloadingIcons", 1 + this._LogoQueue.Count);
                this._DownloadStatusLabel.Visible = true;

                try
                {
                    this._LogoWorker.RunWorkerAsync(info);
                }
                catch (InvalidOperationException)
                {
                    // rare race: the worker is still finishing its previous job —
                    // requeue; the next completion pass will pick it up again
                    this._LogosAttempting.Remove(info.ImageUrl);
                    this._LogoQueue.Enqueue(info);
                }
            }
        }

        private string GetGameImageUrl(uint id)
        {
            string candidate;

            var currentLanguage = this._SteamClient.SteamApps008.GetCurrentGameLanguage();

            candidate = this._SteamClient.SteamApps001.GetAppData(id, _($"small_capsule/{currentLanguage}"));
            if (string.IsNullOrEmpty(candidate) == false)
            {
                return _($"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{candidate}");
            }

            if (currentLanguage != "english")
            {
                candidate = this._SteamClient.SteamApps001.GetAppData(id, "small_capsule/english");
                if (string.IsNullOrEmpty(candidate) == false)
                {
                    return _($"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{candidate}");
                }
            }

            candidate = this._SteamClient.SteamApps001.GetAppData(id, "logo");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                return _($"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{id}/{candidate}.jpg");
            }

            return null;
        }

        private void AddGameToLogoQueue(GameInfo info)
        {
            if (info.ImageIndex > 0)
            {
                return;
            }

            var imageUrl = GetGameImageUrl(info.Id);
            if (string.IsNullOrEmpty(imageUrl) == true)
            {
                return;
            }

            info.ImageUrl = imageUrl;

            int imageIndex = this._LogoImageList.Images.IndexOfKey(imageUrl);
            if (imageIndex >= 0)
            {
                info.ImageIndex = imageIndex;
            }
            else if (
                this._LogosAttempting.Contains(imageUrl) == false &&
                this._LogosAttempted.Contains(imageUrl) == false)
            {
                this._LogosAttempting.Add(imageUrl);
                this._LogoQueue.Enqueue(info);
            }
        }

        private bool OwnsGame(uint id)
        {
            return this._SteamClient.SteamApps008.IsSubscribedApp(id);
        }

        private void AddGame(uint id, string type)
        {
            if (this._Games.ContainsKey(id) == true)
            {
                return;
            }

            if (this.OwnsGame(id) == false)
            {
                return;
            }

            GameInfo info = new(id, type);
            info.Name = this._SteamClient.SteamApps001.GetAppData(info.Id, "name");
            this._Games.Add(id, info);
        }

        private void AddGames()
        {
            this._Games.Clear();
            this._RefreshGamesButton.Enabled = false;
            this._ListWorker.RunWorkerAsync();
        }

        private void AddDefaultGames()
        {
            this.AddGame(480, "normal"); // Spacewar
        }

        private void OnTimer(object sender, EventArgs e)
        {
            this._CallbackTimer.Enabled = false;
            this._SteamClient.RunCallbacks(false);
            this._CallbackTimer.Enabled = true;
        }

        private void OnActivateGame(object sender, EventArgs e)
        {
            var focusedItem = (sender as MyListView)?.FocusedItem;
            var index = focusedItem != null ? focusedItem.Index : -1;
            if (index < 0 || index >= this._FilteredGames.Count)
            {
                return;
            }

            var info = this._FilteredGames[index];
            if (info == null)
            {
                return;
            }

            try
            {
                Process.Start("SAM.Game.exe", info.Id.ToString(CultureInfo.InvariantCulture));
            }
            catch (Win32Exception)
            {
                MessageBox.Show(
                    this,
                    "Failed to start SAM.Game.exe.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            this._AddGameTextBox.Text = "";
            this.AddGames();
        }

        private void OnUnlockAll(object sender, EventArgs e)
        {
            if (this._UnlockWorker.IsBusy == true)
            {
                return;
            }

            List<GameInfo> games = this._Games.Values
                .Where(gi => gi != null)
                .OrderBy(gi => gi.Name)
                .ToList();

            if (games.Count == 0)
            {
                MessageBox.Show(this, Lang.T("unlock.none"), Lang.T("unlock.info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                this,
                Lang.F("unlock.confirmAll", games.Count, EstimateUnlockDuration(games.Count)),
                Lang.T("unlock.confirmAllTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            this.BeginUnlockRun(games);
        }

        private void OnUnlockSelected(object sender, EventArgs e)
        {
            if (this._UnlockWorker.IsBusy == true)
            {
                return;
            }

            List<GameInfo> games = new();
            foreach (int index in this._GameListView.SelectedIndices)
            {
                if (index >= 0 && index < this._FilteredGames.Count)
                {
                    games.Add(this._FilteredGames[index]);
                }
            }

            // Fall back to the focused item when nothing is explicitly selected
            if (games.Count == 0 && this._GameListView.FocusedItem != null)
            {
                int index = this._GameListView.FocusedItem.Index;
                if (index >= 0 && index < this._FilteredGames.Count)
                {
                    games.Add(this._FilteredGames[index]);
                }
            }

            if (games.Count == 0)
            {
                MessageBox.Show(this, Lang.T("unlock.selectFirst"), Lang.T("unlock.info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                this,
                Lang.F("unlock.confirmSelected", games.Count, EstimateUnlockDuration(games.Count)),
                Lang.T("unlock.confirmSelectedTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            this.BeginUnlockRun(games);
        }

        private void BeginUnlockRun(List<GameInfo> games)
        {
            this._unlockCompletedCount = 0;
            this._unlockTotalCount = games.Count;
            this._unlockPauseEvent.Set();
            this._PauseUnlockButton.Text = Lang.T("picker.pause");
            this._PauseUnlockButton.Enabled = true;
            this._StopUnlockButton.Enabled = true;
            this._UnlockAllButton.Enabled = false;
            this._UnlockSelectedButton.Enabled = false;
            this._UnlockWorker.RunWorkerAsync(games);
        }

        private void OnPauseUnlock(object sender, EventArgs e)
        {
            if (this._unlockPauseEvent.IsSet == true)
            {
                this._unlockPauseEvent.Reset();
                this._PauseUnlockButton.Text = Lang.T("picker.resume");
                this._PickerStatusLabel.Text = Lang.T("picker.paused");
            }
            else
            {
                this._unlockPauseEvent.Set();
                this._PauseUnlockButton.Text = Lang.T("picker.pause");
            }
        }

        private void OnStopUnlock(object sender, EventArgs e)
        {
            // release a possible pause first so the worker can observe the cancel
            this._unlockPauseEvent.Set();
            this._UnlockWorker.CancelAsync();
        }

        /// <summary>Rough upfront estimate for unlocking (per-game process spawn + stats roundtrip).</summary>
        private static string EstimateUnlockDuration(int gameCount)
        {
            return Lang.FormatDuration(TimeSpan.FromSeconds(gameCount * 8.0));
        }

        private void DoUnlock(object sender, DoWorkEventArgs e)
        {
            List<GameInfo> games = (List<GameInfo>)e.Argument;
            var itemTimes = new List<double>();

            for (int i = 0; i < games.Count; i++)
            {
                // honor pause/stop between games
                this._unlockPauseEvent.Wait();
                if (this._UnlockWorker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }

                var game = games[i];

                string text;
                if (itemTimes.Count > 0)
                {
                    var remaining = TimeSpan.FromSeconds(itemTimes.Average() * (games.Count - i));
                    text = Lang.F("unlock.progressEta", i + 1, games.Count, game.Name, Lang.FormatDuration(remaining));
                }
                else
                {
                    text = Lang.F("unlock.progress", i + 1, games.Count, game.Name);
                }

                this._UnlockWorker.ReportProgress(i * 100 / games.Count, text);

                var itemWatch = Stopwatch.StartNew();
                try
                {
                    using (Process process = new())
                    {
                        process.StartInfo.FileName = "SAM.Game.exe";
                        process.StartInfo.Arguments = $"{game.Id.ToString(CultureInfo.InvariantCulture)} --unlock-all";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();

                        if (process.WaitForExit(120000) == false)
                        {
                            try { process.Kill(); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this._UnlockWorker.ReportProgress(
                        i * 100 / games.Count,
                        Lang.F("unlock.failedStart", i + 1, games.Count, game.Name, ex.Message));
                }
                finally
                {
                    itemWatch.Stop();
                    itemTimes.Add(itemWatch.Elapsed.TotalSeconds);
                    this._unlockCompletedCount++;
                }
            }

            this._UnlockWorker.ReportProgress(100, Lang.F("unlock.done", games.Count));
        }

        private void OnUnlockProgress(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string text)
            {
                this._PickerStatusLabel.Text = text;
            }
        }

        private void OnUnlockCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this._UnlockAllButton.Enabled = true;
            this._UnlockSelectedButton.Enabled = true;
            this._PauseUnlockButton.Enabled = false;
            this._StopUnlockButton.Enabled = false;
            this._unlockPauseEvent.Set();
            this._PauseUnlockButton.Text = Lang.T("picker.pause");

            if (e.Error != null)
            {
                MessageBox.Show(this, e.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (e.Cancelled == true)
            {
                MessageBox.Show(
                    this,
                    Lang.F("unlock.cancelled", this._unlockCompletedCount, this._unlockTotalCount),
                    Lang.T("unlock.info"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                this.RefreshGames();
                this.StartAchievementScan();
                return;
            }

            MessageBox.Show(
                this,
                Lang.T("unlock.doneBody"),
                Lang.T("unlock.doneTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            this.RefreshGames();
            this.StartAchievementScan(); // pick up the freshly unlocked counts
        }

        private void OnAddGame(object sender, EventArgs e)
        {
            uint id;

            if (uint.TryParse(this._AddGameTextBox.Text, out id) == false)
            {
                MessageBox.Show(
                    this,
                    "Please enter a valid game ID.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (this.OwnsGame(id) == false)
            {
                MessageBox.Show(this, "You don't own that game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            while (this._LogoQueue.TryDequeue(out var logo) == true)
            {
                // clear the download queue because we will be showing only one app
                this._LogosAttempted.Remove(logo.ImageUrl);
            }

            this._AddGameTextBox.Text = "";
            this._Games.Clear();
            this.AddGame(id, "normal");
            this._FilterGamesMenuItem.Checked = true;
            this.RefreshGames();
            this.DownloadNextLogo();
        }

        private void OnFilterUpdate(object sender, EventArgs e)
        {
            this.RefreshGames();

            // Compatibility with _GameListView SearchForVirtualItemEventHandler (otherwise _SearchGameTextBox loose focus on KeyUp)
            this._SearchGameTextBox.Focus();
        }

        private void OnGameListViewDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;

            if (e.Item.Bounds.IntersectsWith(this._GameListView.ClientRectangle) == false)
            {
                return;
            }

            var info = this._FilteredGames[e.ItemIndex];
            var bounds = e.Bounds;
            bool selected = e.Item.Selected;

            // ghost theme tile: dark cell, capsule art on top, name below
            using (var brush = new SolidBrush(selected == true ? GhostTheme.AccentDark : GhostTheme.Background))
            {
                e.Graphics.FillRectangle(brush, bounds);
            }

            int imageWidth = this._LogoImageList.ImageSize.Width;
            int imageHeight = this._LogoImageList.ImageSize.Height;
            int imageX = bounds.Left + Math.Max(0, (bounds.Width - imageWidth) / 2);
            int imageY = bounds.Top + 2;

            if (info.ImageIndex > 0 && info.ImageIndex < this._LogoImageList.Images.Count)
            {
                this._LogoImageList.Draw(e.Graphics, imageX, imageY, imageWidth, imageHeight, info.ImageIndex);
            }
            else
            {
                using (var brush = new SolidBrush(GhostTheme.SurfaceAlt))
                {
                    e.Graphics.FillRectangle(brush, imageX, imageY, imageWidth, imageHeight);
                }
            }

            if (bounds.Height > imageHeight + 20)
            {
                var nameRect = new Rectangle(
                    bounds.Left + 2,
                    imageY + imageHeight + 1,
                    bounds.Width - 4,
                    16);
                TextRenderer.DrawText(
                    e.Graphics,
                    info.Name,
                    this.Font,
                    nameRect,
                    selected == true ? GhostTheme.Text : GhostTheme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                if (info.TotalAchievements > 0 && bounds.Height > imageHeight + 36)
                {
                    var unlocked = info.UnlockedAchievements >= 0
                        ? info.UnlockedAchievements.ToString(CultureInfo.InvariantCulture)
                        : "—";
                    var progressColor = info.UnlockedAchievements == info.TotalAchievements
                        ? GhostTheme.Accent
                        : selected == true ? GhostTheme.Text : GhostTheme.TextMuted;
                    var progressRect = new Rectangle(
                        bounds.Left + 2,
                        imageY + imageHeight + 17,
                        bounds.Width - 4,
                        15);
                    TextRenderer.DrawText(
                        e.Graphics,
                        unlocked + "/" + info.TotalAchievements.ToString(CultureInfo.InvariantCulture),
                        this.Font,
                        progressRect,
                        progressColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
                }
            }

            if (selected == true)
            {
                using (var pen = new Pen(GhostTheme.Accent, 1.5f))
                {
                    e.Graphics.DrawRectangle(pen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
                }
            }

            if (info.ImageIndex <= 0)
            {
                this.AddGameToLogoQueue(info);
                this.DownloadNextLogo();
            }
        }
    }
}
