/* SAM Auto 9.0 — Ghost theme (shared between SAM.Picker and SAM.Game)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SAM.Common
{
    internal enum GhostMode
    {
        Dark,
        Light,
    }

    /// <summary>
    /// "Ghost" theme: deep navy-black surfaces with a glowing cyan accent
    /// (dark mode) or a clean light surface with a teal accent (light mode).
    /// Provides one-call theming for WinForms; mode can be switched live.
    /// </summary>
    internal static class GhostTheme
    {
        public static event Action Changed;

        public static GhostMode Mode { get; private set; } = GhostMode.Dark;

        public static Color Background { get; private set; } = Color.FromArgb(0x0B, 0x0E, 0x14);
        public static Color Surface { get; private set; } = Color.FromArgb(0x13, 0x18, 0x22);
        public static Color SurfaceAlt { get; private set; } = Color.FromArgb(0x1A, 0x21, 0x2E);
        public static Color Input { get; private set; } = Color.FromArgb(0x0F, 0x14, 0x1D);
        public static Color Border { get; private set; } = Color.FromArgb(0x2A, 0x33, 0x45);
        public static Color Text { get; private set; } = Color.FromArgb(0xE6, 0xED, 0xF3);
        public static Color TextMuted { get; private set; } = Color.FromArgb(0x8B, 0x94, 0xA3);
        public static Color Accent { get; private set; } = Color.FromArgb(0x5F, 0xEA, 0xD4);
        public static Color AccentDark { get; private set; } = Color.FromArgb(0x14, 0x3A, 0x36);
        public static Color Danger { get; private set; } = Color.FromArgb(0xF8, 0x71, 0x71);

        public static readonly Font TitleFont = new("Segoe UI Semibold", 13f, FontStyle.Bold);
        public static readonly Font NormalFont = new("Segoe UI", 9f);
        public static readonly Font MonospaceFont = new("Consolas", 8.5f);

        public static void SetMode(GhostMode mode)
        {
            if (Mode == mode)
            {
                return;
            }

            Mode = mode;

            if (mode == GhostMode.Dark)
            {
                Background = Color.FromArgb(0x0B, 0x0E, 0x14);
                Surface = Color.FromArgb(0x13, 0x18, 0x22);
                SurfaceAlt = Color.FromArgb(0x1A, 0x21, 0x2E);
                Input = Color.FromArgb(0x0F, 0x14, 0x1D);
                Border = Color.FromArgb(0x2A, 0x33, 0x45);
                Text = Color.FromArgb(0xE6, 0xED, 0xF3);
                TextMuted = Color.FromArgb(0x8B, 0x94, 0xA3);
                Accent = Color.FromArgb(0x5F, 0xEA, 0xD4);
                AccentDark = Color.FromArgb(0x14, 0x3A, 0x36);
                Danger = Color.FromArgb(0xF8, 0x71, 0x71);
            }
            else
            {
                Background = Color.FromArgb(0xF3, 0xF5, 0xF7);
                Surface = Color.FromArgb(0xFF, 0xFF, 0xFF);
                SurfaceAlt = Color.FromArgb(0xE8, 0xED, 0xF2);
                Input = Color.FromArgb(0xFF, 0xFF, 0xFF);
                Border = Color.FromArgb(0xC5, 0xCE, 0xD8);
                Text = Color.FromArgb(0x1F, 0x23, 0x28);
                TextMuted = Color.FromArgb(0x5A, 0x64, 0x72);
                Accent = Color.FromArgb(0x0E, 0x74, 0x84);
                AccentDark = Color.FromArgb(0xC9, 0xF0, 0xEA);
                Danger = Color.FromArgb(0xD9, 0x26, 0x26);
            }

            Changed?.Invoke();
        }

        #region Win32 dark mode

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        /// <summary>Makes the window title bar follow the current theme mode (Windows 10 1809+ / 11).</summary>
        public static void EnableDarkTitleBar(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                int dark = Mode == GhostMode.Dark ? 1 : 0;
                // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (20H1+), legacy value = 19
                if (DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(handle, 19, ref dark, sizeof(int));
                }
            }
            catch
            {
                // older Windows without dwmapi support — ignore
            }
        }

        /// <summary>Enables the dark Explorer visual style (dark scrollbars/headers) for a control.</summary>
        public static void EnableDarkControlTheme(Control control, string appName = "DarkMode_Explorer")
        {
            if (control == null || control.IsHandleCreated == false)
            {
                return;
            }

            try
            {
                SetWindowTheme(control.Handle, appName, null);
            }
            catch
            {
                // not critical
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>Themes a ListView's column header dark (Windows 10 1809+).</summary>
        private static void EnableDarkListViewHeader(ListView listView)
        {
            try
            {
                var header = SendMessage(listView.Handle, 0x101F /* LVM_GETHEADER */, IntPtr.Zero, IntPtr.Zero);
                if (header != IntPtr.Zero)
                {
                    SetWindowTheme(header, "DarkMode_ItemsView", null);
                }
            }
            catch
            {
                // not critical
            }
        }

        #endregion

        /// <summary>Applies the ghost theme to a form and every control inside it.</summary>
        public static void Apply(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = Text;
            form.Font = NormalFont;

            if (form.IsHandleCreated == true)
            {
                EnableDarkTitleBar(form.Handle);
                ApplyHandleDependentThemes(form.Controls);
            }
            else
            {
                form.HandleCreated += (_, _) =>
                {
                    EnableDarkTitleBar(form.Handle);
                    ApplyHandleDependentThemes(form.Controls);
                };
            }

            ApplyToControls(form.Controls);
        }

        /// <summary>
        /// Dark visual styles that require a created handle (scrollbars etc.).
        /// Runs once the form handle exists.
        /// </summary>
        private static void ApplyHandleDependentThemes(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case ListView listView:
                        EnableDarkControlTheme(listView);
                        EnableDarkListViewHeader(listView);
                        break;
                    case TreeView treeView:
                        EnableDarkControlTheme(treeView);
                        break;
                    case DataGridView grid:
                        EnableDarkControlTheme(grid);
                        break;
                }

                if (control.HasChildren == true)
                {
                    ApplyHandleDependentThemes(control.Controls);
                }
            }
        }

        public static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                ApplyToControl(control);
            }
        }

        public static void ApplyToControl(Control control)
        {
            switch (control)
            {
                case ToolStrip toolStrip:
                {
                    toolStrip.Renderer = new GhostToolStripRenderer();
                    toolStrip.BackColor = Surface;
                    toolStrip.ForeColor = Text;
                    toolStrip.GripStyle = ToolStripGripStyle.Hidden;
                    break;
                }

                case ListView listView:
                {
                    listView.BackColor = Background;
                    listView.ForeColor = Text;
                    listView.BorderStyle = BorderStyle.None;
                    break;
                }

                case TreeView treeView:
                {
                    treeView.BackColor = Background;
                    treeView.ForeColor = Text;
                    break;
                }

                case DataGridView grid:
                {
                    grid.EnableHeadersVisualStyles = false;
                    grid.BackgroundColor = Background;
                    grid.GridColor = Border;
                    grid.BorderStyle = BorderStyle.None;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = Accent;
                    grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceAlt;
                    grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Accent;
                    grid.RowHeadersDefaultCellStyle.BackColor = Surface;
                    grid.RowHeadersDefaultCellStyle.ForeColor = TextMuted;
                    grid.RowHeadersDefaultCellStyle.SelectionBackColor = AccentDark;
                    grid.RowHeadersDefaultCellStyle.SelectionForeColor = Text;
                    grid.DefaultCellStyle.BackColor = Background;
                    grid.DefaultCellStyle.ForeColor = Text;
                    grid.DefaultCellStyle.SelectionBackColor = AccentDark;
                    grid.DefaultCellStyle.SelectionForeColor = Text;
                    break;
                }

                case Button button:
                {
                    StyleButton(button);
                    break;
                }

                case TextBox textBox:
                {
                    textBox.BackColor = Input;
                    textBox.ForeColor = Text;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                }

                case ComboBox comboBox:
                {
                    comboBox.BackColor = Input;
                    comboBox.ForeColor = Text;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    break;
                }

                case ListBox listBox:
                {
                    listBox.BackColor = Input;
                    listBox.ForeColor = Text;
                    listBox.BorderStyle = BorderStyle.None;
                    break;
                }

                case NumericUpDown numeric:
                {
                    numeric.BackColor = Input;
                    numeric.ForeColor = Text;
                    break;
                }

                case TabControl tabControl:
                {
                    if (tabControl is GhostTabControl == false)
                    {
                        tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
                        tabControl.DrawItem -= OnDrawTab; // guard against double subscription on re-apply
                        tabControl.DrawItem += OnDrawTab;
                    }

                    break;
                }

                case TabPage tabPage:
                {
                    tabPage.BackColor = Background;
                    tabPage.ForeColor = Text;
                    break;
                }

                case GroupBox groupBox:
                {
                    groupBox.BackColor = Background;
                    groupBox.ForeColor = Accent;
                    break;
                }

                case CheckBox checkBox:
                {
                    checkBox.BackColor = Color.Transparent;
                    checkBox.ForeColor = Text;
                    break;
                }

                case RadioButton radio:
                {
                    radio.BackColor = Color.Transparent;
                    radio.ForeColor = Text;
                    break;
                }

                case LinkLabel link:
                {
                    link.LinkColor = Accent;
                    link.ActiveLinkColor = Accent;
                    link.VisitedLinkColor = Accent;
                    break;
                }

                case Label label:
                {
                    label.ForeColor = Text;
                    label.BackColor = Color.Transparent;
                    break;
                }

                case ProgressBar:
                case PictureBox:
                case Panel:
                case SplitContainer:
                default:
                {
                    control.BackColor = control is Panel or SplitContainer
                        ? Background
                        : control.BackColor;
                    control.ForeColor = Text;
                    break;
                }
            }

            // user controls and containers keep their own background if explicitly set
            if (control.HasChildren == true)
            {
                ApplyToControls(control.Controls);
            }
        }

        /// <summary>Standard ghost button: dark surface, thin border, cyan hover.</summary>
        public static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = SurfaceAlt;
            button.ForeColor = Text;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = AccentDark;
            button.FlatAppearance.MouseDownBackColor = AccentDark;
        }

        /// <summary>Primary action button: glowing cyan outline and text.</summary>
        public static void StyleAccentButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = AccentDark;
            button.ForeColor = Accent;
            button.FlatAppearance.BorderColor = Accent;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(AccentDark, 0.15f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(AccentDark, 0.15f);
        }

        /// <summary>Danger button: red-tinted ghost style.</summary>
        public static void StyleDangerButton(Button button)
        {
            var dangerBg = Mode == GhostMode.Dark
                ? Color.FromArgb(0x3A, 0x1B, 0x1B)
                : Color.FromArgb(0xFB, 0xE3, 0xE3);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = dangerBg;
            button.ForeColor = Danger;
            button.FlatAppearance.BorderColor = Danger;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(dangerBg, 0.1f);
        }

        private static void OnDrawTab(object sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl || e.Index < 0 || e.Index >= tabControl.TabCount)
            {
                return;
            }

            var tab = tabControl.TabPages[e.Index];
            bool selected = (tabControl.SelectedIndex == e.Index);

            using (var brush = new SolidBrush(selected ? SurfaceAlt : Surface))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            if (selected == true)
            {
                using (var pen = new Pen(Accent, 2))
                {
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 2, e.Bounds.Right, e.Bounds.Bottom - 2);
                }
            }

            TextRenderer.DrawText(
                e.Graphics,
                tab.Text,
                NormalFont,
                e.Bounds,
                selected ? Accent : TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    /// <summary>
    /// A TabControl that fully paints itself ghost-dark (including the strip
    /// behind the tab headers, which the stock control always paints light).
    /// </summary>
    internal sealed class GhostTabControl : TabControl
    {
        public GhostTabControl()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;

            using (var brush = new SolidBrush(GhostTheme.Background))
            {
                g.FillRectangle(brush, this.ClientRectangle);
            }

            for (int i = 0; i < this.TabCount; i++)
            {
                this.DrawTab(g, i);
            }

            // content frame border
            var display = this.DisplayRectangle;
            using (var pen = new Pen(GhostTheme.Border))
            {
                g.DrawRectangle(pen, display.Left - 2, display.Top - 2, display.Width + 3, display.Height + 3);
            }
        }

        private void DrawTab(Graphics g, int index)
        {
            var bounds = this.GetTabRect(index);
            bool selected = this.SelectedIndex == index;

            using (var brush = new SolidBrush(selected ? GhostTheme.SurfaceAlt : GhostTheme.Surface))
            {
                g.FillRectangle(brush, bounds);
            }

            if (selected == true)
            {
                using var pen = new Pen(GhostTheme.Accent, 2);
                g.DrawLine(pen, bounds.Left, bounds.Bottom - 2, bounds.Right, bounds.Bottom - 2);
            }

            TextRenderer.DrawText(
                g,
                this.TabPages[index].Text,
                GhostTheme.NormalFont,
                bounds,
                selected ? GhostTheme.Accent : GhostTheme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    internal sealed class GhostColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => GhostTheme.Surface;
        public override Color ToolStripGradientMiddle => GhostTheme.Surface;
        public override Color ToolStripGradientEnd => GhostTheme.Surface;
        public override Color ToolStripBorder => GhostTheme.Border;
        public override Color ToolStripPanelGradientBegin => GhostTheme.Surface;
        public override Color ToolStripPanelGradientEnd => GhostTheme.Surface;
        public override Color ToolStripContentPanelGradientBegin => GhostTheme.Background;
        public override Color ToolStripContentPanelGradientEnd => GhostTheme.Background;
        public override Color ToolStripDropDownBackground => GhostTheme.Surface;
        public override Color MenuStripGradientBegin => GhostTheme.Surface;
        public override Color MenuStripGradientEnd => GhostTheme.Surface;
        public override Color MenuBorder => GhostTheme.Border;
        public override Color MenuItemSelected => GhostTheme.AccentDark;
        public override Color MenuItemSelectedGradientBegin => GhostTheme.AccentDark;
        public override Color MenuItemSelectedGradientEnd => GhostTheme.AccentDark;
        public override Color MenuItemBorder => GhostTheme.Accent;
        public override Color MenuItemPressedGradientBegin => GhostTheme.SurfaceAlt;
        public override Color MenuItemPressedGradientEnd => GhostTheme.SurfaceAlt;
        public override Color ImageMarginGradientBegin => GhostTheme.Surface;
        public override Color ImageMarginGradientMiddle => GhostTheme.Surface;
        public override Color ImageMarginGradientEnd => GhostTheme.Surface;
        public override Color SeparatorDark => GhostTheme.Border;
        public override Color SeparatorLight => GhostTheme.Border;
        public override Color StatusStripGradientBegin => GhostTheme.Surface;
        public override Color StatusStripGradientEnd => GhostTheme.Surface;
        public override Color OverflowButtonGradientBegin => GhostTheme.SurfaceAlt;
        public override Color OverflowButtonGradientMiddle => GhostTheme.SurfaceAlt;
        public override Color OverflowButtonGradientEnd => GhostTheme.SurfaceAlt;
        public override Color ButtonSelectedHighlight => GhostTheme.AccentDark;
        public override Color ButtonSelectedHighlightBorder => GhostTheme.Accent;
        public override Color ButtonSelectedBorder => GhostTheme.Accent;
        public override Color ButtonSelectedGradientBegin => GhostTheme.AccentDark;
        public override Color ButtonSelectedGradientMiddle => GhostTheme.AccentDark;
        public override Color ButtonSelectedGradientEnd => GhostTheme.AccentDark;
        public override Color ButtonPressedGradientBegin => GhostTheme.AccentDark;
        public override Color ButtonPressedGradientMiddle => GhostTheme.AccentDark;
        public override Color ButtonPressedGradientEnd => GhostTheme.AccentDark;
        public override Color ButtonPressedHighlight => GhostTheme.AccentDark;
        public override Color ButtonPressedHighlightBorder => GhostTheme.Accent;
        public override Color ButtonCheckedGradientBegin => GhostTheme.AccentDark;
        public override Color ButtonCheckedGradientMiddle => GhostTheme.AccentDark;
        public override Color ButtonCheckedGradientEnd => GhostTheme.AccentDark;
        public override Color ButtonCheckedHighlight => GhostTheme.AccentDark;
        public override Color ButtonCheckedHighlightBorder => GhostTheme.Accent;
        public override Color GripDark => GhostTheme.Border;
        public override Color GripLight => GhostTheme.Border;
        public override Color RaftingContainerGradientBegin => GhostTheme.Surface;
        public override Color RaftingContainerGradientEnd => GhostTheme.Surface;
    }

    internal sealed class GhostToolStripRenderer : ToolStripProfessionalRenderer
    {
        public GhostToolStripRenderer()
            : base(new GhostColorTable())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Enabled == false)
            {
                e.TextColor = GhostTheme.TextMuted;
            }
            else if (e.Item.ForeColor == SystemColors.ControlText || e.Item.ForeColor == Color.Black)
            {
                // items that never set an explicit color get the theme text color;
                // items with a custom color (e.g. accent buttons) keep it
                e.TextColor = GhostTheme.Text;
            }

            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item != null && e.Item.Selected ? GhostTheme.Accent : GhostTheme.TextMuted;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is StatusStrip)
            {
                return; // flat status strip, no border
            }

            using (var pen = new Pen(GhostTheme.Border))
            {
                var rect = e.ToolStrip.ClientRectangle;
                e.Graphics.DrawLine(pen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
            }
        }
    }
}
