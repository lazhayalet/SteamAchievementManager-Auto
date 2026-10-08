namespace SAM.Picker
{
    partial class GamePicker
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ToolStripSeparator _ToolStripSeparator1;
            System.Windows.Forms.ToolStripSeparator _ToolStripSeparator2;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GamePicker));
            this._LogoImageList = new System.Windows.Forms.ImageList(this.components);
            this._CallbackTimer = new System.Windows.Forms.Timer(this.components);
            this._PickerToolStrip = new System.Windows.Forms.ToolStrip();
            this._RefreshGamesButton = new System.Windows.Forms.ToolStripButton();
            this._AddGameTextBox = new System.Windows.Forms.ToolStripTextBox();
            this._AddGameButton = new System.Windows.Forms.ToolStripButton();
            this._UnlockAllButton = new System.Windows.Forms.ToolStripButton();
            this._UnlockSelectedButton = new System.Windows.Forms.ToolStripButton();
            this._FreeGamesButton = new System.Windows.Forms.ToolStripButton();
            this._PauseUnlockButton = new System.Windows.Forms.ToolStripButton();
            this._StopUnlockButton = new System.Windows.Forms.ToolStripButton();
            this._SortDropDown = new System.Windows.Forms.ToolStripDropDownButton();
            this._SortNameItem = new System.Windows.Forms.ToolStripMenuItem();
            this._SortLockedFirstItem = new System.Windows.Forms.ToolStripMenuItem();
            this._SortUnlockedFirstItem = new System.Windows.Forms.ToolStripMenuItem();
            this._LanguageDropDown = new System.Windows.Forms.ToolStripDropDownButton();
            this._LangTurkishItem = new System.Windows.Forms.ToolStripMenuItem();
            this._LangEnglishItem = new System.Windows.Forms.ToolStripMenuItem();
            this._ThemeDropDown = new System.Windows.Forms.ToolStripDropDownButton();
            this._ThemeDarkItem = new System.Windows.Forms.ToolStripMenuItem();
            this._ThemeLightItem = new System.Windows.Forms.ToolStripMenuItem();
            this._FindGamesLabel = new System.Windows.Forms.ToolStripLabel();
            this._SearchGameTextBox = new System.Windows.Forms.ToolStripTextBox();
            this._FilterDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this._FilterGamesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._FilterDemosMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._FilterModsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._FilterJunkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._GameListView = new SAM.Picker.MyListView();
            this._PickerStatusStrip = new System.Windows.Forms.StatusStrip();
            this._PickerStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._DownloadStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._LogoWorker = new System.ComponentModel.BackgroundWorker();
            this._ListWorker = new System.ComponentModel.BackgroundWorker();
            this._UnlockWorker = new System.ComponentModel.BackgroundWorker();
            _ToolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            _ToolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this._PickerToolStrip.SuspendLayout();
            this._PickerStatusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // _ToolStripSeparator1
            //
            _ToolStripSeparator1.Name = "_ToolStripSeparator1";
            _ToolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            //
            // _ToolStripSeparator2
            //
            _ToolStripSeparator2.Name = "_ToolStripSeparator2";
            _ToolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            //
            // _LogoImageList
            //
            this._LogoImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
            this._LogoImageList.ImageSize = new System.Drawing.Size(184, 69);
            this._LogoImageList.TransparentColor = System.Drawing.Color.Transparent;
            //
            // _CallbackTimer
            //
            this._CallbackTimer.Enabled = true;
            this._CallbackTimer.Tick += new System.EventHandler(this.OnTimer);
            //
            // _PickerToolStrip
            //
            this._PickerToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._RefreshGamesButton,
            _ToolStripSeparator1,
            this._AddGameTextBox,
            this._AddGameButton,
            this._UnlockAllButton,
            this._UnlockSelectedButton,
            this._FreeGamesButton,
            this._PauseUnlockButton,
            this._StopUnlockButton,
            _ToolStripSeparator2,
            this._FindGamesLabel,
            this._SearchGameTextBox,
            this._FilterDropDownButton,
            this._SortDropDown,
            this._LanguageDropDown,
            this._ThemeDropDown});
            this._PickerToolStrip.Location = new System.Drawing.Point(0, 0);
            this._PickerToolStrip.Name = "_PickerToolStrip";
            this._PickerToolStrip.Size = new System.Drawing.Size(742, 25);
            this._PickerToolStrip.TabIndex = 1;
            this._PickerToolStrip.Text = "toolStrip1";
            //
            // _RefreshGamesButton
            //
            this._RefreshGamesButton.Image = global::SAM.Picker.Resources.Refresh;
            this._RefreshGamesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._RefreshGamesButton.Name = "_RefreshGamesButton";
            this._RefreshGamesButton.Size = new System.Drawing.Size(105, 22);
            this._RefreshGamesButton.Text = "Refresh Games";
            this._RefreshGamesButton.Click += new System.EventHandler(this.OnRefresh);
            //
            // _AddGameTextBox
            //
            this._AddGameTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._AddGameTextBox.Name = "_AddGameTextBox";
            this._AddGameTextBox.Size = new System.Drawing.Size(100, 25);
            //
            // _AddGameButton
            //
            this._AddGameButton.Image = global::SAM.Picker.Resources.Search;
            this._AddGameButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._AddGameButton.Name = "_AddGameButton";
            this._AddGameButton.Size = new System.Drawing.Size(83, 22);
            this._AddGameButton.Text = "Add Game";
            this._AddGameButton.Click += new System.EventHandler(this.OnAddGame);
            //
            // _UnlockAllButton
            //
            this._UnlockAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._UnlockAllButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._UnlockAllButton.Name = "_UnlockAllButton";
            this._UnlockAllButton.Size = new System.Drawing.Size(85, 22);
            this._UnlockAllButton.Text = "🔓 Unlock All";
            this._UnlockAllButton.Click += new System.EventHandler(this.OnUnlockAll);
            //
            // _UnlockSelectedButton
            //
            this._UnlockSelectedButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._UnlockSelectedButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._UnlockSelectedButton.Name = "_UnlockSelectedButton";
            this._UnlockSelectedButton.Size = new System.Drawing.Size(120, 22);
            this._UnlockSelectedButton.Text = "Unlock Selected";
            this._UnlockSelectedButton.Click += new System.EventHandler(this.OnUnlockSelected);
            //
            // _FreeGamesButton
            //
            this._FreeGamesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._FreeGamesButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._FreeGamesButton.ForeColor = System.Drawing.Color.FromArgb(0x5F, 0xEA, 0xD4);
            this._FreeGamesButton.Name = "_FreeGamesButton";
            this._FreeGamesButton.Size = new System.Drawing.Size(85, 22);
            this._FreeGamesButton.Text = "👻 Free Games";
            this._FreeGamesButton.Click += new System.EventHandler(this.OnFreeGames);
            //
            // _PauseUnlockButton
            //
            this._PauseUnlockButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._PauseUnlockButton.Enabled = false;
            this._PauseUnlockButton.Name = "_PauseUnlockButton";
            this._PauseUnlockButton.Size = new System.Drawing.Size(60, 22);
            this._PauseUnlockButton.Text = "⏸ Pause";
            this._PauseUnlockButton.Click += new System.EventHandler(this.OnPauseUnlock);
            //
            // _StopUnlockButton
            //
            this._StopUnlockButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._StopUnlockButton.Enabled = false;
            this._StopUnlockButton.ForeColor = System.Drawing.Color.FromArgb(0xF8, 0x71, 0x71);
            this._StopUnlockButton.Name = "_StopUnlockButton";
            this._StopUnlockButton.Size = new System.Drawing.Size(50, 22);
            this._StopUnlockButton.Text = "⏹ Stop";
            this._StopUnlockButton.Click += new System.EventHandler(this.OnStopUnlock);
            //
            // _SortDropDown
            //
            this._SortDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._SortDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._SortNameItem,
            this._SortLockedFirstItem,
            this._SortUnlockedFirstItem});
            this._SortDropDown.Name = "_SortDropDown";
            this._SortDropDown.Size = new System.Drawing.Size(50, 22);
            this._SortDropDown.Text = "⇅ Sort";
            this._SortDropDown.ToolTipText = "Sort games";
            //
            // _SortNameItem
            //
            this._SortNameItem.Checked = true;
            this._SortNameItem.Name = "_SortNameItem";
            this._SortNameItem.Size = new System.Drawing.Size(160, 22);
            this._SortNameItem.Text = "By name";
            this._SortNameItem.Click += new System.EventHandler(this.OnSortMode);
            //
            // _SortLockedFirstItem
            //
            this._SortLockedFirstItem.Name = "_SortLockedFirstItem";
            this._SortLockedFirstItem.Size = new System.Drawing.Size(160, 22);
            this._SortLockedFirstItem.Text = "Locked first";
            this._SortLockedFirstItem.Click += new System.EventHandler(this.OnSortMode);
            //
            // _SortUnlockedFirstItem
            //
            this._SortUnlockedFirstItem.Name = "_SortUnlockedFirstItem";
            this._SortUnlockedFirstItem.Size = new System.Drawing.Size(160, 22);
            this._SortUnlockedFirstItem.Text = "Unlocked first";
            this._SortUnlockedFirstItem.Click += new System.EventHandler(this.OnSortMode);
            //
            // _LanguageDropDown
            //
            this._LanguageDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._LanguageDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._LangTurkishItem,
            this._LangEnglishItem});
            this._LanguageDropDown.Name = "_LanguageDropDown";
            this._LanguageDropDown.Size = new System.Drawing.Size(40, 22);
            this._LanguageDropDown.Text = "🌐";
            this._LanguageDropDown.ToolTipText = "Language / Dil";
            //
            // _LangTurkishItem
            //
            this._LangTurkishItem.Name = "_LangTurkishItem";
            this._LangTurkishItem.Size = new System.Drawing.Size(120, 22);
            this._LangTurkishItem.Text = "Türkçe";
            this._LangTurkishItem.Click += new System.EventHandler(this.OnLangTurkish);
            //
            // _LangEnglishItem
            //
            this._LangEnglishItem.Name = "_LangEnglishItem";
            this._LangEnglishItem.Size = new System.Drawing.Size(120, 22);
            this._LangEnglishItem.Text = "English";
            this._LangEnglishItem.Click += new System.EventHandler(this.OnLangEnglish);
            //
            // _ThemeDropDown
            //
            this._ThemeDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._ThemeDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._ThemeDarkItem,
            this._ThemeLightItem});
            this._ThemeDropDown.Name = "_ThemeDropDown";
            this._ThemeDropDown.Size = new System.Drawing.Size(55, 22);
            this._ThemeDropDown.Text = "🎨 Tema";
            this._ThemeDropDown.ToolTipText = "Theme / Tema";
            //
            // _ThemeDarkItem
            //
            this._ThemeDarkItem.Checked = true;
            this._ThemeDarkItem.Name = "_ThemeDarkItem";
            this._ThemeDarkItem.Size = new System.Drawing.Size(160, 22);
            this._ThemeDarkItem.Text = "Koyu (Hayalet)";
            this._ThemeDarkItem.Click += new System.EventHandler(this.OnThemeDark);
            //
            // _ThemeLightItem
            //
            this._ThemeLightItem.Name = "_ThemeLightItem";
            this._ThemeLightItem.Size = new System.Drawing.Size(160, 22);
            this._ThemeLightItem.Text = "Açık";
            this._ThemeLightItem.Click += new System.EventHandler(this.OnThemeLight);
            //
            // _FindGamesLabel
            //
            this._FindGamesLabel.Name = "_FindGamesLabel";
            this._FindGamesLabel.Size = new System.Drawing.Size(33, 22);
            this._FindGamesLabel.Text = "Filter";
            //
            // _SearchGameTextBox
            //
            this._SearchGameTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._SearchGameTextBox.Name = "_SearchGameTextBox";
            this._SearchGameTextBox.Size = new System.Drawing.Size(100, 25);
            this._SearchGameTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnFilterUpdate);
            //
            // _FilterDropDownButton
            //
            this._FilterDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._FilterDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._FilterGamesMenuItem,
            this._FilterDemosMenuItem,
            this._FilterModsMenuItem,
            this._FilterJunkMenuItem});
            this._FilterDropDownButton.Image = global::SAM.Picker.Resources.Filter;
            this._FilterDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._FilterDropDownButton.Name = "_FilterDropDownButton";
            this._FilterDropDownButton.Size = new System.Drawing.Size(29, 22);
            this._FilterDropDownButton.Text = "Game filtering";
            //
            // _FilterGamesMenuItem
            //
            this._FilterGamesMenuItem.Checked = true;
            this._FilterGamesMenuItem.CheckOnClick = true;
            this._FilterGamesMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this._FilterGamesMenuItem.Name = "_FilterGamesMenuItem";
            this._FilterGamesMenuItem.Size = new System.Drawing.Size(180, 22);
            this._FilterGamesMenuItem.Text = "Show &games";
            this._FilterGamesMenuItem.CheckedChanged += new System.EventHandler(this.OnFilterUpdate);
            //
            // _FilterDemosMenuItem
            //
            this._FilterDemosMenuItem.CheckOnClick = true;
            this._FilterDemosMenuItem.Name = "_FilterDemosMenuItem";
            this._FilterDemosMenuItem.Size = new System.Drawing.Size(180, 22);
            this._FilterDemosMenuItem.Text = "Show &demos";
            this._FilterDemosMenuItem.CheckedChanged += new System.EventHandler(this.OnFilterUpdate);
            //
            // _FilterModsMenuItem
            //
            this._FilterModsMenuItem.CheckOnClick = true;
            this._FilterModsMenuItem.Name = "_FilterModsMenuItem";
            this._FilterModsMenuItem.Size = new System.Drawing.Size(180, 22);
            this._FilterModsMenuItem.Text = "Show &mods";
            this._FilterModsMenuItem.CheckedChanged += new System.EventHandler(this.OnFilterUpdate);
            //
            // _FilterJunkMenuItem
            //
            this._FilterJunkMenuItem.CheckOnClick = true;
            this._FilterJunkMenuItem.Name = "_FilterJunkMenuItem";
            this._FilterJunkMenuItem.Size = new System.Drawing.Size(180, 22);
            this._FilterJunkMenuItem.Text = "Show &junk";
            this._FilterJunkMenuItem.CheckedChanged += new System.EventHandler(this.OnFilterUpdate);
            //
            // _GameListView
            //
            this._GameListView.BackColor = System.Drawing.Color.Black;
            this._GameListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._GameListView.ForeColor = System.Drawing.Color.White;
            this._GameListView.HideSelection = false;
            this._GameListView.LargeImageList = this._LogoImageList;
            this._GameListView.Location = new System.Drawing.Point(0, 25);
            this._GameListView.MultiSelect = true;
            this._GameListView.Name = "_GameListView";
            this._GameListView.OwnerDraw = true;
            this._GameListView.Size = new System.Drawing.Size(742, 245);
            this._GameListView.SmallImageList = this._LogoImageList;
            this._GameListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this._GameListView.TabIndex = 0;
            this._GameListView.TileSize = new System.Drawing.Size(184, 69);
            this._GameListView.UseCompatibleStateImageBehavior = false;
            this._GameListView.VirtualMode = true;
            this._GameListView.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.OnGameListViewDrawItem);
            this._GameListView.ItemActivate += new System.EventHandler(this.OnActivateGame);
            this._GameListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.OnGameListViewRetrieveVirtualItem);
            this._GameListView.SearchForVirtualItem += new System.Windows.Forms.SearchForVirtualItemEventHandler(this.OnGameListViewSearchForVirtualItem);
            //
            // _PickerStatusStrip
            //
            this._PickerStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._PickerStatusLabel,
            this._DownloadStatusLabel});
            this._PickerStatusStrip.Location = new System.Drawing.Point(0, 270);
            this._PickerStatusStrip.Name = "_PickerStatusStrip";
            this._PickerStatusStrip.Size = new System.Drawing.Size(742, 22);
            this._PickerStatusStrip.TabIndex = 2;
            this._PickerStatusStrip.Text = "statusStrip";
            //
            // _PickerStatusLabel
            //
            this._PickerStatusLabel.Name = "_PickerStatusLabel";
            this._PickerStatusLabel.Size = new System.Drawing.Size(727, 17);
            this._PickerStatusLabel.Spring = true;
            this._PickerStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _DownloadStatusLabel
            //
            this._DownloadStatusLabel.Image = global::SAM.Picker.Resources.Download;
            this._DownloadStatusLabel.Name = "_DownloadStatusLabel";
            this._DownloadStatusLabel.Size = new System.Drawing.Size(111, 17);
            this._DownloadStatusLabel.Text = "Download status";
            this._DownloadStatusLabel.Visible = false;
            //
            // _LogoWorker
            //
            this._LogoWorker.WorkerSupportsCancellation = true;
            this._LogoWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.DoDownloadLogo);
            this._LogoWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.OnDownloadLogo);
            //
            // _ListWorker
            //
            this._ListWorker.WorkerSupportsCancellation = true;
            this._ListWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.DoDownloadList);
            this._ListWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.OnDownloadList);
            //
            // _UnlockWorker
            //
            this._UnlockWorker.WorkerSupportsCancellation = true;
            this._UnlockWorker.WorkerReportsProgress = true;
            this._UnlockWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.DoUnlock);
            this._UnlockWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.OnUnlockProgress);
            this._UnlockWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.OnUnlockCompleted);
            //
            // GamePicker
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(980, 560);
            this.Controls.Add(this._GameListView);
            this.Controls.Add(this._PickerStatusStrip);
            this.Controls.Add(this._PickerToolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GamePicker";
            this.Text = "Steam Achievement Manager Auto 9.0 | Pick a game... Any game...";
            this._PickerToolStrip.ResumeLayout(false);
            this._PickerToolStrip.PerformLayout();
            this._PickerStatusStrip.ResumeLayout(false);
            this._PickerStatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private MyListView _GameListView;
        private System.Windows.Forms.ImageList _LogoImageList;
        private System.Windows.Forms.Timer _CallbackTimer;
        private System.Windows.Forms.ToolStripButton _UnlockAllButton;
        private System.Windows.Forms.ToolStripButton _UnlockSelectedButton;
        private System.Windows.Forms.ToolStripButton _FreeGamesButton;
        private System.Windows.Forms.ToolStripButton _PauseUnlockButton;
        private System.Windows.Forms.ToolStripButton _StopUnlockButton;
        private System.Windows.Forms.ToolStripDropDownButton _SortDropDown;
        private System.Windows.Forms.ToolStripMenuItem _SortNameItem;
        private System.Windows.Forms.ToolStripMenuItem _SortLockedFirstItem;
        private System.Windows.Forms.ToolStripMenuItem _SortUnlockedFirstItem;
        private System.Windows.Forms.ToolStripDropDownButton _LanguageDropDown;
        private System.Windows.Forms.ToolStripMenuItem _LangTurkishItem;
        private System.Windows.Forms.ToolStripMenuItem _LangEnglishItem;
        private System.Windows.Forms.ToolStripDropDownButton _ThemeDropDown;
        private System.Windows.Forms.ToolStripMenuItem _ThemeDarkItem;
        private System.Windows.Forms.ToolStripMenuItem _ThemeLightItem;
        private System.ComponentModel.BackgroundWorker _UnlockWorker;
        private System.Windows.Forms.ToolStrip _PickerToolStrip;
        private System.Windows.Forms.ToolStripButton _RefreshGamesButton;
        private System.Windows.Forms.ToolStripTextBox _AddGameTextBox;
        private System.Windows.Forms.ToolStripButton _AddGameButton;
        private System.Windows.Forms.ToolStripDropDownButton _FilterDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem _FilterGamesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _FilterJunkMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _FilterDemosMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _FilterModsMenuItem;
        private System.Windows.Forms.StatusStrip _PickerStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _DownloadStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel _PickerStatusLabel;
        private System.ComponentModel.BackgroundWorker _LogoWorker;
        private System.ComponentModel.BackgroundWorker _ListWorker;
        private System.Windows.Forms.ToolStripTextBox _SearchGameTextBox;
        private System.Windows.Forms.ToolStripLabel _FindGamesLabel;

        #endregion
    }
}
