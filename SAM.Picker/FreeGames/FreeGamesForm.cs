/* SAM Auto 9.0 — Free Games window
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QRCoder;
using SAM.Common;
using SteamKit2;

namespace SAM.Picker.FreeGames
{
    /// <summary>
    /// "Free Games" window: sign in with the Steam mobile app (QR),
    /// pick content categories, fetch the free list and claim everything
    /// with live progress and ETA.
    /// </summary>
    internal sealed class FreeGamesForm : Form
    {
        private const int BatchSize = 50;
        private static readonly TimeSpan BetweenBatches = TimeSpan.FromMilliseconds(1200);
        private static readonly TimeSpan RateLimitWait = TimeSpan.FromSeconds(65);
        private const int MaxRateLimitRetries = 60;

        private readonly SteamFreeClient _client = new();
        private readonly List<FreeAppInfo> _items = new();

        private CancellationTokenSource _cts;
        private bool _busy;

        #region Controls

        private Label _titleLabel;
        private Label _subtitleLabel;

        private GroupBox _signInGroup;
        private Label _signInStatus;
        private Button _connectButton;
        private Button _disconnectButton;
        private CheckBox _rememberCheck;
        private LinkLabel _securityLink;
        private PictureBox _qrPicture;

        private GroupBox _categoriesGroup;
        private CheckBox[] _categoryChecks;
        private Button _fetchButton;
        private Label _fetchStatus;
        private Label _totalLabel;

        private GroupBox _claimGroup;
        private Button _claimButton;
        private Button _cancelButton;
        private ProgressBar _progress;
        private Label _claimStatus;

        private GroupBox _logGroup;
        private TextBox _logBox;

        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripStatusLabel _etaLabel;

        #endregion

        public FreeGamesForm()
        {
            this.InitializeUi();
            GhostTheme.Apply(this);
            this.ApplyTexts();

            this._client.StateChanged += this.OnClientStateChanged;
            Lang.Changed += this.ApplyTexts;
            GhostTheme.Changed += this.OnThemeChanged;

            this._connectButton.Click += this.OnConnectClick;
            this._disconnectButton.Click += (_, _) => this.Disconnect();
            this._securityLink.Click += (_, _) => MessageBox.Show(
                this,
                Lang.T("fg.securityBody"),
                Lang.T("fg.securityTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            this._fetchButton.Click += this.OnFetchClick;
            this._claimButton.Click += this.OnClaimClick;
            this._cancelButton.Click += (_, _) => this._cts?.Cancel();
            this.FormClosing += this.OnFormClosing;

            this.Shown += (_, _) =>
            {
                this.LogLine(Lang.T("fg.note"));
                this.LoadCachedList();
                this.TryAutoLogin();
            };
        }

        #region UI setup

        private void InitializeUi()
        {
            this.Text = "Free Games";
            this.ClientSize = new(540, 700);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            this._titleLabel = new()
            {
                Location = new(16, 12),
                Size = new(508, 24),
                Font = GhostTheme.TitleFont,
                ForeColor = GhostTheme.Accent,
            };

            this._subtitleLabel = new()
            {
                Location = new(16, 38),
                Size = new(508, 32),
                ForeColor = GhostTheme.TextMuted,
            };

            // --- sign-in group ---
            this._signInGroup = new()
            {
                Location = new(12, 76),
                Size = new(516, 132),
            };

            this._signInStatus = new()
            {
                Location = new(12, 22),
                Size = new(300, 20),
            };

            this._connectButton = new()
            {
                Location = new(12, 48),
                Size = new(180, 30),
            };
            GhostTheme.StyleAccentButton(this._connectButton);

            this._disconnectButton = new()
            {
                Location = new(200, 48),
                Size = new(130, 30),
                Enabled = false,
            };
            GhostTheme.StyleButton(this._disconnectButton);

            this._rememberCheck = new()
            {
                Location = new(12, 84),
                Size = new(220, 22),
                Checked = true,
            };

            this._securityLink = new()
            {
                Location = new(232, 86),
                Size = new(130, 20),
                TabStop = false,
            };

            this._qrPicture = new()
            {
                Location = new(370, 18),
                Size = new(132, 104),
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom,
                Visible = false,
            };

            this._signInGroup.Controls.Add(this._signInStatus);
            this._signInGroup.Controls.Add(this._connectButton);
            this._signInGroup.Controls.Add(this._disconnectButton);
            this._signInGroup.Controls.Add(this._rememberCheck);
            this._signInGroup.Controls.Add(this._securityLink);
            this._signInGroup.Controls.Add(this._qrPicture);

            // --- categories group ---
            this._categoriesGroup = new()
            {
                Location = new(12, 214),
                Size = new(516, 128),
            };

            this._categoryChecks = new CheckBox[FreeAppsFetcher.Categories.Length];
            for (int i = 0; i < FreeAppsFetcher.Categories.Length; i++)
            {
                var check = new CheckBox
                {
                    Location = new(12 + i * 84, 22),
                    Size = new(82, 22),
                    Checked = i is < 3 or 5, // games + dlc + software + deals by default
                    AutoEllipsis = true,
                };
                this._categoryChecks[i] = check;
                this._categoriesGroup.Controls.Add(check);
            }

            this._fetchButton = new()
            {
                Location = new(12, 52),
                Size = new(180, 30),
            };
            GhostTheme.StyleButton(this._fetchButton);

            this._fetchStatus = new()
            {
                Location = new(200, 56),
                Size = new(304, 40),
                ForeColor = GhostTheme.TextMuted,
            };

            this._totalLabel = new()
            {
                Location = new(12, 92),
                Size = new(492, 30),
                ForeColor = GhostTheme.Accent,
            };

            this._categoriesGroup.Controls.Add(this._fetchButton);
            this._categoriesGroup.Controls.Add(this._fetchStatus);
            this._categoriesGroup.Controls.Add(this._totalLabel);

            // --- claim group ---
            this._claimGroup = new()
            {
                Location = new(12, 348),
                Size = new(516, 118),
            };

            this._claimButton = new()
            {
                Location = new(12, 22),
                Size = new(220, 34),
                Font = new("Segoe UI Semibold", 10f, FontStyle.Bold),
            };
            GhostTheme.StyleAccentButton(this._claimButton);

            this._cancelButton = new()
            {
                Location = new(240, 22),
                Size = new(100, 34),
                Enabled = false,
            };
            GhostTheme.StyleDangerButton(this._cancelButton);

            this._progress = new()
            {
                Location = new(12, 66),
                Size = new(492, 14),
            };

            this._claimStatus = new()
            {
                Location = new(12, 86),
                Size = new(492, 26),
                ForeColor = GhostTheme.TextMuted,
            };

            this._claimGroup.Controls.Add(this._claimButton);
            this._claimGroup.Controls.Add(this._cancelButton);
            this._claimGroup.Controls.Add(this._progress);
            this._claimGroup.Controls.Add(this._claimStatus);

            // --- log group ---
            this._logGroup = new()
            {
                Location = new(12, 472),
                Size = new(516, 196),
            };

            this._logBox = new()
            {
                Location = new(12, 20),
                Size = new(492, 164),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = GhostTheme.MonospaceFont,
            };

            this._logGroup.Controls.Add(this._logBox);

            // --- status strip ---
            this._statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            this._etaLabel = new() { ForeColor = GhostTheme.Accent };
            this._statusStrip = new();
            this._statusStrip.Items.Add(this._statusLabel);
            this._statusStrip.Items.Add(this._etaLabel);

            this.Controls.Add(this._titleLabel);
            this.Controls.Add(this._subtitleLabel);
            this.Controls.Add(this._signInGroup);
            this.Controls.Add(this._categoriesGroup);
            this.Controls.Add(this._claimGroup);
            this.Controls.Add(this._logGroup);
            this.Controls.Add(this._statusStrip);
        }

        private void ApplyTexts()
        {
            this.Text = Lang.T("fg.title");
            this._titleLabel.Text = "👻 " + Lang.T("fg.header");
            this._subtitleLabel.Text = Lang.T("fg.subtitle");
            this._signInGroup.Text = Lang.T("fg.signin");
            this._categoriesGroup.Text = Lang.T("fg.categories");
            this._claimGroup.Text = "3. " + Lang.T("fg.claim");
            this._logGroup.Text = Lang.T("fg.log");
            this._connectButton.Text = Lang.T("fg.connectQr");
            this._disconnectButton.Text = Lang.T("fg.disconnect");
            this._rememberCheck.Text = Lang.T("fg.rememberMe");
            this._securityLink.Text = Lang.T("fg.security");
            this._fetchButton.Text = Lang.T("fg.fetch");
            this._claimButton.Text = "👻 " + Lang.T("fg.claim");
            this._cancelButton.Text = Lang.T("fg.cancel");

            for (int i = 0; i < FreeAppsFetcher.Categories.Length; i++)
            {
                this._categoryChecks[i].Text = Lang.T(FreeAppsFetcher.Categories[i].LabelKey);
            }

            if (this._client.IsLoggedIn == false && this._busy == false)
            {
                this._signInStatus.Text = Lang.T("fg.notConnected");
            }
        }

        #endregion

        #region Helpers

        private void Ui(Action action)
        {
            if (this.IsDisposed == true)
            {
                return;
            }

            try
            {
                this.BeginInvoke(action);
            }
            catch
            {
                // form is closing
            }
        }

        private void LogLine(string message)
        {
            this.Ui(() =>
            {
                this._logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            });
        }

        private void SetStatus(string message)
        {
            this.Ui(() => this._statusLabel.Text = message);
        }

        private void SetEta(string message)
        {
            this.Ui(() => this._etaLabel.Text = message);
        }

        private void OnClientStateChanged(SteamFreeClient.ClientState state)
        {
            this.Ui(() =>
            {
                this._disconnectButton.Enabled = this._client.IsLoggedIn;
                if (state == SteamFreeClient.ClientState.LoggedIn)
                {
                    this._signInStatus.Text = Lang.F("fg.loggedInAs", this._client.AccountName);
                    this._qrPicture.Visible = false;
                }
            });
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            this._cts?.Cancel();
            Lang.Changed -= this.ApplyTexts;
            GhostTheme.Changed -= this.OnThemeChanged;
            this._client.Dispose();
        }

        private void OnThemeChanged()
        {
            GhostTheme.Apply(this);
            GhostTheme.StyleAccentButton(this._connectButton);
            GhostTheme.StyleButton(this._disconnectButton);
            GhostTheme.StyleButton(this._fetchButton);
            GhostTheme.StyleAccentButton(this._claimButton);
            GhostTheme.StyleDangerButton(this._cancelButton);
            this._titleLabel.ForeColor = GhostTheme.Accent;
            this._subtitleLabel.ForeColor = GhostTheme.TextMuted;
            this._fetchStatus.ForeColor = GhostTheme.TextMuted;
            this._totalLabel.ForeColor = GhostTheme.Accent;
            this._claimStatus.ForeColor = GhostTheme.TextMuted;
            this._etaLabel.ForeColor = GhostTheme.Accent;
            this.Invalidate(true);
        }

        /// <summary>Instantly shows the cached list (if fresh enough) so no fetch is needed.</summary>
        private void LoadCachedList()
        {
            if (FreeAppsCache.TryLoad(out var items, out var fetchedAt) == false)
            {
                return;
            }

            this._items.Clear();
            this._items.AddRange(items);

            var age = DateTime.UtcNow - fetchedAt;
            this._totalLabel.Text = Lang.F("fg.cacheLoaded", items.Count, Lang.FormatDuration(age));
            this._claimButton.Enabled = items.Count > 0;
            this.LogLine(Lang.F("fg.cacheLoaded", items.Count, Lang.FormatDuration(age)));
        }

        #endregion

        #region Sign-in

        private void TryAutoLogin()
        {
            if (TokenStore.TryLoad(out var accountName, out var refreshToken) == false)
            {
                return;
            }

            _ = this.AutoLoginAsync(accountName, refreshToken);
        }

        private async Task AutoLoginAsync(string accountName, string refreshToken)
        {
            this._busy = true;
            this.Ui(() =>
            {
                this._connectButton.Enabled = false;
                this._signInStatus.Text = Lang.T("fg.connecting");
            });
            this.LogLine("auto login: " + accountName);

            try
            {
                await this._client.LoginWithTokenAsync(accountName, refreshToken, CancellationToken.None)
                    .ConfigureAwait(false);
                this.LogLine("session restored");
            }
            catch (Exception e)
            {
                this.LogLine(Lang.F("fg.tokenFailed") + " (" + e.Message + ")");
                TokenStore.Clear();
                this.Ui(() =>
                {
                    this._signInStatus.Text = Lang.T("fg.tokenFailed");
                    this._connectButton.Enabled = true;
                });
            }
            finally
            {
                this._busy = false;
                this.Ui(() => this._connectButton.Enabled = this._client.IsLoggedIn == false);
            }
        }

        private void OnConnectClick(object sender, EventArgs e)
        {
            if (this._busy == true)
            {
                return;
            }

            _ = this.QrLoginAsync();
        }

        private async Task QrLoginAsync()
        {
            this._busy = true;
            this._cts?.Dispose();
            this._cts = new();
            var cancellation = this._cts.Token;

            this.Ui(() =>
            {
                this._connectButton.Enabled = false;
                this._signInStatus.Text = Lang.T("fg.connecting");
            });

            try
            {
                var challengeUrl = await this._client.BeginQrLoginAsync(cancellation).ConfigureAwait(false);
                this.LogLine("QR ready, waiting for scan");
                this.Ui(() =>
                {
                    this._qrPicture.Image = RenderQr(challengeUrl);
                    this._qrPicture.Visible = true;
                    this._signInStatus.Text = Lang.T("fg.awaitingScan");
                });

                var accountName = await this._client.CompleteQrLoginAsync(cancellation).ConfigureAwait(false);
                this.LogLine("signed in: " + accountName);

                if (this._rememberCheck.Checked == true)
                {
                    TokenStore.Save(accountName, this._client.RefreshToken);
                }
            }
            catch (OperationCanceledException)
            {
                this.LogLine("qr login cancelled");
            }
            catch (Exception ex)
            {
                this.LogLine(Lang.F("fg.loginFailed", ex.Message));
                this.Ui(() => this._signInStatus.Text = Lang.F("fg.loginFailed", ex.Message));
            }
            finally
            {
                this._busy = false;
                this.Ui(() =>
                {
                    this._connectButton.Enabled = this._client.IsLoggedIn == false;
                    if (this._client.IsLoggedIn == false)
                    {
                        this._qrPicture.Visible = false;
                    }
                });
            }
        }

        private void Disconnect()
        {
            TokenStore.Clear();
            this._client.Disconnect();
            this._signInStatus.Text = Lang.T("fg.notConnected");
            this._connectButton.Enabled = true;
            this._disconnectButton.Enabled = false;
            this._qrPicture.Visible = false;
            this.LogLine("disconnected");
        }

        private static Bitmap RenderQr(string content)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(data);
            var bytes = qrCode.GetGraphic(4);
            return new(new MemoryStream(bytes, false));
        }

        #endregion

        #region Fetch list

        private void OnFetchClick(object sender, EventArgs e)
        {
            if (this._busy == true)
            {
                return;
            }

            _ = this.FetchAsync();
        }

        private async Task FetchAsync()
        {
            var selected = new List<(FreeAppsFetcher.Category Category, CheckBox Check)>();
            foreach (var (category, check) in FreeAppsFetcher.Categories.Zip(this._categoryChecks, (c, ch) => (c, ch)))
            {
                if (check.Checked == true)
                {
                    selected.Add((category, check));
                }
            }

            if (selected.Count == 0)
            {
                return;
            }

            this._busy = true;
            this._cts?.Dispose();
            this._cts = new();
            var cancellation = this._cts.Token;

            this.Ui(() =>
            {
                this._fetchButton.Enabled = false;
                this._claimButton.Enabled = false;
                this._totalLabel.Text = string.Empty;
            });

            this._items.Clear();

            try
            {
                foreach (var (category, _) in selected)
                {
                    var progress = new Progress<int>(page =>
                    {
                        this._fetchStatus.Text = Lang.F("fg.fetching", Lang.T(category.LabelKey), page);
                    });

                    var items = await Task.Run(
                        () => FreeAppsFetcher.FetchCategory(
                            category,
                            page => ((IProgress<int>)progress).Report(page),
                            () => this.LogLine(Lang.T("fg.storeRateLimited")),
                            cancellation),
                        cancellation).ConfigureAwait(false);

                    this._items.AddRange(items);
                    this.LogLine(Lang.F("fg.fetchDone", Lang.T(category.LabelKey), items.Count));
                    this.Ui(() => this._fetchStatus.Text = Lang.F("fg.fetchDone", Lang.T(category.LabelKey), items.Count));
                }

                FreeAppsCache.Save(this._items);

                var estimate = EstimateClaimDuration(this._items.Count);
                this.Ui(() =>
                {
                    this._totalLabel.Text = Lang.F("fg.totalFound", this._items.Count, Lang.FormatDuration(estimate));
                    this._claimButton.Enabled = this._items.Count > 0;
                });
                this.LogLine(Lang.F("fg.totalFound", this._items.Count, Lang.FormatDuration(estimate)));
            }
            catch (OperationCanceledException)
            {
                this.LogLine("fetch cancelled");
            }
            catch (Exception ex)
            {
                this.LogLine(Lang.F("fg.fetchError", ex.Message));
                this.Ui(() => this._fetchStatus.Text = Lang.F("fg.fetchError", ex.Message));
            }
            finally
            {
                this._busy = false;
                this.Ui(() => this._fetchButton.Enabled = true);
            }
        }

        /// <summary>Rough upfront estimate; refined with real measurements during the run.</summary>
        private static TimeSpan EstimateClaimDuration(int itemCount)
        {
            var batches = (int)Math.Ceiling(itemCount / (double)BatchSize);
            return TimeSpan.FromSeconds(batches * 3.5);
        }

        #endregion

        #region Claim

        private void OnClaimClick(object sender, EventArgs e)
        {
            if (this._busy == true)
            {
                return;
            }

            if (this._client.IsLoggedIn == false)
            {
                MessageBox.Show(this, Lang.T("fg.needLogin"), "👻", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (this._items.Count == 0)
            {
                MessageBox.Show(this, Lang.T("fg.needFetch"), "👻", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var estimate = EstimateClaimDuration(this._items.Count);
            var result = MessageBox.Show(
                this,
                Lang.F("fg.confirmClaim", this._items.Count, Lang.FormatDuration(estimate)),
                Lang.T("fg.confirmClaimTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            _ = this.ClaimAsync();
        }

        private async Task ClaimAsync()
        {
            this._busy = true;
            this._cts?.Dispose();
            this._cts = new();
            var cancellation = this._cts.Token;

            var cmApps = this._items.Where(i => i.CategoryKey != "deals").Select(i => i.AppId).Distinct().ToArray();
            var dealApps = this._items.Where(i => i.CategoryKey == "deals").Select(i => i.AppId).Distinct().ToArray();
            var batches = cmApps
                .Select((id, index) => (id, index))
                .GroupBy(x => x.index / BatchSize)
                .Select(g => g.Select(x => x.id).ToArray())
                .ToArray();

            int granted = 0, owned = 0, failed = 0;
            var batchTimes = new List<double>();
            var watch = Stopwatch.StartNew();

            this.Ui(() =>
            {
                this._claimButton.Enabled = false;
                this._fetchButton.Enabled = false;
                this._cancelButton.Enabled = true;
                this._progress.Maximum = cmApps.Length + dealApps.Length;
                this._progress.Value = 0;
            });
            this.LogLine(Lang.F("fg.claimStart", cmApps.Length + dealApps.Length));

            var cancelled = false;

            try
            {
                for (int i = 0; i < batches.Length; i++)
            {
                if (cancellation.IsCancellationRequested == true || this._client.IsLoggedIn == false)
                {
                    cancelled = cancellation.IsCancellationRequested;
                    break;
                }

                var batch = batches[i];
                var batchWatch = Stopwatch.StartNew();
                int retries = 0;

                while (true)
                {
                    if (cancellation.IsCancellationRequested == true)
                    {
                        cancelled = true;
                        break;
                    }

                    EResult result;
                    int grantedNow;
                    try
                    {
                        (result, grantedNow) = await this._client
                            .RequestFreeLicensesAsync(batch)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result = EResult.Fail;
                        grantedNow = 0;
                        this.LogLine("batch " + (i + 1) + " error: " + ex.Message);
                    }

                    if (result == EResult.RateLimitExceeded && retries < MaxRateLimitRetries)
                    {
                        retries++;
                        this.LogLine(Lang.F("fg.rateLimited", Lang.FormatDuration(RateLimitWait)));
                        await this.CountdownWaitAsync(RateLimitWait, cancellation).ConfigureAwait(false);
                        continue;
                    }

                    batchWatch.Stop();
                    batchTimes.Add(batchWatch.Elapsed.TotalSeconds);

                    if (result == EResult.OK)
                    {
                        granted += grantedNow;
                        owned += batch.Length - grantedNow;
                    }
                    else
                    {
                        failed += batch.Length;
                        this.LogLine("batch " + (i + 1) + " failed: " + result);
                    }

                    break;
                }

                if (cancelled == true)
                {
                    break;
                }

                this.Ui(() =>
                {
                    this._progress.Value = Math.Min(this._progress.Maximum, (i + 1) * BatchSize);
                });

                // live ETA from measured average batch time
                if (batchTimes.Count > 0)
                {
                    var avg = batchTimes.Average();
                    var remaining = TimeSpan.FromSeconds(avg * (batches.Length - i - 1));
                    var text = Lang.F("fg.claimBatch", i + 1, batches.Length, granted, owned, failed, Lang.FormatDuration(remaining));
                    this.SetStatus(text);
                    this.SetEta("~" + Lang.FormatDuration(remaining));
                }

                try
                {
                    await Task.Delay(BetweenBatches, cancellation).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                }
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            // phase 2: 100%-off promo packages via the store checkout (needs the web session)
            if (cancelled == false && dealApps.Length > 0)
            {
                if (string.IsNullOrEmpty(this._client.AccessToken) == true || this._client.SteamId == 0)
                {
                    this.LogLine(Lang.T("fg.dealsSkippedNoSession"));
                }
                else
                {
                    (granted, owned, failed, cancelled) = await this
                        .ClaimDealsAsync(dealApps, granted, owned, failed, batches.Length, cancellation)
                        .ConfigureAwait(false);
                }
            }

            watch.Stop();

            var summary = cancelled
                ? Lang.F("fg.claimCancelled", granted, owned, failed)
                : Lang.F("fg.claimDone", granted, owned, failed);

            this.LogLine(summary);
            this.SetStatus(summary);
            this.SetEta(string.Empty);

            this.Ui(() =>
            {
                this._claimButton.Enabled = true;
                this._fetchButton.Enabled = true;
                this._cancelButton.Enabled = false;
                MessageBox.Show(this, summary, "👻", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            this._busy = false;
        }

        /// <summary>
        /// Phase 2: claims 100%-off promo packages through the store checkout.
        /// Each app is resolved to its currently-free package id first.
        /// </summary>
        private async Task<(int Granted, int Owned, int Failed, bool Cancelled)> ClaimDealsAsync(
            uint[] dealApps,
            int granted,
            int owned,
            int failed,
            int progressOffset,
            CancellationToken cancellation)
        {
            var web = new StoreWebSession(this._client.SteamId, this._client.AccessToken);
            if (await Task.Run(web.Initialize, cancellation).ConfigureAwait(false) == false)
            {
                this.LogLine(Lang.T("fg.dealsSkippedNoSession"));
                return (granted, owned, failed, false);
            }

            this.LogLine(Lang.F("fg.dealsStart", dealApps.Length));
            var itemTimes = new List<double>();
            var cancelled = false;

            for (int i = 0; i < dealApps.Length; i++)
            {
                if (cancellation.IsCancellationRequested == true)
                {
                    cancelled = true;
                    break;
                }

                var itemWatch = Stopwatch.StartNew();

                uint? packageId = null;
                try
                {
                    var appId = dealApps[i];
                    packageId = await Task.Run(() => DealPackageResolver.ResolveFreePackage(appId), cancellation)
                        .ConfigureAwait(false);
                    await Task.Delay(250, cancellation).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    break;
                }
                catch (Exception)
                {
                    // resolve errors fall through as failed below
                }

                if (packageId == null)
                {
                    failed++;
                    this.LogLine("deal " + dealApps[i] + ": no free package found (promo over?)");
                }
                else
                {
                    int retries = 0;
                    while (true)
                    {
                        StoreWebSession.ClaimResult result;
                        try
                        {
                            var pkg = packageId.Value;
                            result = await Task.Run(() => web.ClaimFreePackage(pkg), cancellation).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            cancelled = true;
                            break;
                        }

                        if (result == StoreWebSession.ClaimResult.RateLimited && retries < MaxRateLimitRetries)
                        {
                            retries++;
                            this.LogLine(Lang.F("fg.rateLimited", Lang.FormatDuration(RateLimitWait)));
                            await this.CountdownWaitAsync(RateLimitWait, cancellation).ConfigureAwait(false);
                            continue;
                        }

                        switch (result)
                        {
                            case StoreWebSession.ClaimResult.Ok:
                                granted++;
                                break;
                            case StoreWebSession.ClaimResult.AlreadyOwned:
                                owned++;
                                break;
                            default:
                                failed++;
                                break;
                        }

                        break;
                    }
                }

                if (cancelled == true)
                {
                    break;
                }

                itemWatch.Stop();
                itemTimes.Add(itemWatch.Elapsed.TotalSeconds);

                this.Ui(() =>
                {
                    this._progress.Value = Math.Min(this._progress.Maximum, this._progress.Value + 1);
                });

                var avg = itemTimes.Average();
                var remaining = TimeSpan.FromSeconds(avg * (dealApps.Length - i - 1));
                this.SetStatus(Lang.F("fg.dealProgress", i + 1, dealApps.Length, granted, owned, failed, Lang.FormatDuration(remaining)));
                this.SetEta("~" + Lang.FormatDuration(remaining));

                try
                {
                    await Task.Delay(500, cancellation).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
            }

            return (granted, owned, failed, cancelled);
        }

        private async Task CountdownWaitAsync(TimeSpan duration, CancellationToken cancellation)
        {
            var remaining = duration;
            while (remaining > TimeSpan.Zero)
            {
                cancellation.ThrowIfCancellationRequested();
                this.SetEta(Lang.F("fg.rateLimited", Lang.FormatDuration(remaining)));
                var slice = remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1);
                await Task.Delay(slice, cancellation).ConfigureAwait(false);
                remaining -= slice;
            }
        }

        #endregion
    }
}
