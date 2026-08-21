using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

namespace SuvidhaPosInstaller;

/// <summary>
/// Rebuilds the installer shell in one place. The page/installation logic remains in MainForm,
/// but the visual shell is replaced so there is no second UI patch fighting the original layout.
/// </summary>
internal static class FreshUi
{
    private static readonly Color Bg = Color.FromArgb(4, 12, 25);
    private static readonly Color SidebarBg = Color.FromArgb(3, 14, 31);
    private static readonly Color HeaderBg = Color.FromArgb(4, 16, 34);
    private static readonly Color CardBg = Color.FromArgb(6, 24, 50);
    private static readonly Color Border = Color.FromArgb(22, 66, 108);
    private static readonly Color Text = Color.FromArgb(244, 247, 252);
    private static readonly Color Muted = Color.FromArgb(166, 184, 207);
    private static readonly Color Blue = Color.FromArgb(0, 166, 255);

    private static readonly FieldInfo ShellField = Field("shellRoot");
    private static readonly FieldInfo SidebarField = Field("sidebar");
    private static readonly FieldInfo ContentField = Field("content");
    private static readonly FieldInfo HeaderTitleField = Field("headerTitle");
    private static readonly FieldInfo HeaderSubField = Field("headerSub");
    private static readonly FieldInfo StepField = Field("step");
    private static readonly FieldInfo NextField = Field("nextButton");
    private static readonly FieldInfo BusyField = Field("busy");
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.All });
    private static bool vcPrefetchStarted;

    public static void Apply(MainForm form)
    {
        form.MinimumSize = new Size(1100, 720);
        form.ClientSize = new Size(Math.Max(1180, form.ClientSize.Width), Math.Max(760, form.ClientSize.Height));
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.FormBorderStyle = FormBorderStyle.Sizable;
        form.MaximizeBox = true;
        form.MinimizeBox = true;
        form.BackColor = Bg;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var shell = BuildShell(form);
        ShellField.SetValue(form, shell);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            Padding = new Padding(14, 12, 14, 10),
            Margin = Padding.Empty
        };
        ContentField.SetValue(form, content);
        shell.Controls.Add(content, 1, 1);

        var sidebar = BuildSidebar();
        SidebarField.SetValue(form, sidebar);
        shell.Controls.Add(sidebar, 0, 1);
        content.ControlAdded += (_, _) => { UpdateStepVisuals(form, sidebar); WireCurrentNext(form); if (GetStep(form) == 2) _ = PrefetchVcRedistAsync(form); };

        var (title, sub) = BuildHeader(shell);
        HeaderTitleField.SetValue(form, title);
        HeaderSubField.SetValue(form, sub);

        form.Resize += (_, _) => ResizeShell(form);
        ResizeShell(form);
        WireNavigation(form, sidebar);
        WireCurrentNext(form);

        InvokePrivate(form, "ShowStep", GetStep(form));
        ResizeShell(form);
    }

    private static TableLayoutPanel BuildShell(MainForm form)
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Bg,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        form.Controls.Clear();
        form.Controls.Add(shell);
        return shell;
    }

    private static (Label title, Label sub) BuildHeader(TableLayoutPanel shell)
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = HeaderBg,
            Padding = new Padding(18, 8, 18, 8),
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
        shell.Controls.Add(header, 0, 0);
        shell.SetColumnSpan(header, 2);

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 10, 2)
        };
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "SuvidhaPOS.png");
            if (File.Exists(path)) logo.Image = Image.FromFile(path);
        }
        catch { }
        header.Controls.Add(logo, 0, 0);

        header.Controls.Add(new Label
        {
            Text = "Suvidha POS  |  Installer",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 21F),
            ForeColor = Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = Padding.Empty
        }, 1, 0);

        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        header.Controls.Add(info, 2, 0);

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 12F),
            ForeColor = Text,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        var sub = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Muted,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        info.Controls.Add(title, 0, 0);
        info.Controls.Add(sub, 0, 1);
        return (title, sub);
    }

    private static FlowLayoutPanel BuildSidebar()
    {
        var sidebar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = SidebarBg,
            Padding = new Padding(12, 12, 12, 12),
            Margin = Padding.Empty
        };

        string[] names = { "Welcome", "Terms & Conditions", "Components", "Download", "Install", "Database Setup", "Finish" };
        string[] subs = { "Welcome to Installer", "Read important terms", "Select components", "Download installation files", "Install all components", "Configure database & backup", "Installation complete" };
        for (int i = 0; i < names.Length; i++)
        {
            var item = new StepButton(i, names[i], subs[i])
            {
                Tag = i,
                Width = 244,
                Height = 76,
                Margin = new Padding(0, 0, 0, 7)
            };
            sidebar.Controls.Add(item);
        }
        sidebar.Controls.Add(new HelpCard { Width = 244, Height = 112, Margin = new Padding(0, 8, 0, 0) });
        return sidebar;
    }

    private static void WireNavigation(MainForm form, FlowLayoutPanel sidebar)
    {
        foreach (Control control in sidebar.Controls)
        {
            if (control is not StepButton button) continue;
            button.Click += (_, _) =>
            {
                if (BusyField.GetValue(form) is true) return;
                int target = (int)button.Tag!;
                if (target <= GetStep(form))
                {
                    InvokePrivate(form, "ShowStep", target);
                    ResizeShell(form);
                }
            };
        }
    }

    private static void WireCurrentNext(MainForm form)
    {
        var next = NextField.GetValue(form) as Button;
        if (next == null) return;
        if (GetStep(form) == 2 && vcPrefetchStarted) next.Enabled = false;
        if (Equals(next.Tag, "FreshUiWired")) return;
        next.Tag = "FreshUiWired";
        next.Click += (_, _) =>
        {
            if (GetStep(form) == 0 && BusyField.GetValue(form) is not true)
            {
                InvokePrivate(form, "ShowStep", 1);
                ResizeShell(form);
            }
        };
    }

    private static async Task PrefetchVcRedistAsync(MainForm form)
    {
        if (vcPrefetchStarted) return;
        vcPrefetchStarted = true;
        try
        {
            const string folder = @"D:\Suvidha Pos\Software";
            Directory.CreateDirectory(folder);
            var existing = Directory.EnumerateFiles(folder, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault(x =>
            {
                var n = Path.GetFileName(x);
                return n.Contains("vcredist", StringComparison.OrdinalIgnoreCase) || n.Contains("vc_redist", StringComparison.OrdinalIgnoreCase);
            });
            if (existing != null) return;

            if (NextField.GetValue(form) is Button next) next.Enabled = false;
            const string id = "1v90y9MXcOirG_mlev-IsLrlEVuFa3AIK";
            string url = $"https://drive.usercontent.google.com/download?id={Uri.EscapeDataString(id)}&export=download&confirm=t";
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true)
                throw new InvalidOperationException("Google Drive returned a confirmation page.");
            string target = Path.Combine(folder, "Microsoft Visual C++ Redistributable.exe");
            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 131072, true);
            await input.CopyToAsync(output);
            if (new FileInfo(target).Length < 100 * 1024) throw new InvalidOperationException("Downloaded VC++ runtime file is unexpectedly small.");
        }
        catch { }
        finally
        {
            vcPrefetchStarted = false;
            if (GetStep(form) == 2 && NextField.GetValue(form) is Button next) next.Enabled = true;
        }
    }

    private static void ResizeShell(MainForm form)
    {
        var shell = ShellField.GetValue(form) as TableLayoutPanel;
        var sidebar = SidebarField.GetValue(form) as FlowLayoutPanel;
        if (shell == null || sidebar == null) return;
        int w = Math.Max(1100, form.ClientSize.Width);
        int h = Math.Max(720, form.ClientSize.Height);
        int sidebarWidth = Math.Clamp((int)Math.Round(w * 0.21), 250, 300);
        int headerHeight = Math.Clamp((int)Math.Round(h * 0.115), 82, 96);
        shell.ColumnStyles[0].Width = sidebarWidth;
        shell.RowStyles[0].Height = headerHeight;
        int itemWidth = Math.Max(224, sidebarWidth - sidebar.Padding.Horizontal);
        int itemHeight = h < 760 ? 70 : 76;
        foreach (Control c in sidebar.Controls)
        {
            if (c is StepButton step)
            {
                step.Width = itemWidth;
                step.Height = itemHeight;
            }
            else if (c is HelpCard help)
            {
                help.Width = itemWidth;
                help.Height = h < 760 ? 98 : 112;
            }
        }
        UpdateStepVisuals(form, sidebar);
    }

    private static void UpdateStepVisuals(MainForm form, FlowLayoutPanel sidebar)
    {
        int current = GetStep(form);
        foreach (Control c in sidebar.Controls)
            if (c is StepButton button && button.Tag is int i)
                button.Active = i == current;
    }

    private static int GetStep(MainForm form) => StepField.GetValue(form) is int value ? Math.Clamp(value, 0, 6) : 0;
    private static FieldInfo Field(string name) => typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException(typeof(MainForm).FullName, name);
    private static void InvokePrivate(MainForm form, string method, params object[] args) => typeof(MainForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(form, args);

    private sealed class StepButton : Panel
    {
        private bool active;
        public bool Active { get => active; set { active = value; Invalidate(); } }

        public StepButton(int index, string text, string sub)
        {
            DoubleBuffered = true;
            Padding = new Padding(10, 8, 10, 8);
            Cursor = Cursors.Hand;
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent, Margin = Padding.Empty, Padding = Padding.Empty };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
            var number = new Label { Text = (index + 1).ToString(), Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter };
            var title = new Label { Text = text, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10F), ForeColor = Color.White, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(8, 0, 0, 0) };
            var subtitle = new Label { Text = sub, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(154, 177, 202), AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(8, 0, 0, 0) };
            grid.Controls.Add(number, 0, 0);
            grid.SetRowSpan(number, 2);
            grid.Controls.Add(title, 1, 0);
            grid.Controls.Add(subtitle, 1, 1);
            Controls.Add(grid);
            foreach (Control child in grid.Controls) child.Click += (_, _) => OnClick(EventArgs.Empty);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new SolidBrush(active ? Color.FromArgb(24, 20, 105, 170) : Color.FromArgb(7, 23, 44));
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var pen = new Pen(active ? Blue : Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            int size = 38;
            int y = Math.Max(8, (Height - size) / 2);
            using var fill = new SolidBrush(active ? Color.FromArgb(12, 119, 225) : Color.FromArgb(20, 40, 68));
            e.Graphics.FillEllipse(fill, 12, y, size, size);
            base.OnPaint(e);
        }
    }

    private sealed class HelpCard : Panel
    {
        public HelpCard()
        {
            BackColor = CardBg;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(12);
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.Controls.Add(new Label { Text = "◉  Need Help?", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F), ForeColor = Blue }, 0, 0);
            grid.Controls.Add(new Label { Text = "Support is available if you need help.", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8F), ForeColor = Text, AutoEllipsis = true }, 0, 1);
            grid.Controls.Add(new Label { Text = "", Dock = DockStyle.Fill }, 0, 2);
            grid.Controls.Add(new Label { Text = "+91 827171 8844", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F), ForeColor = Blue, AutoEllipsis = true }, 0, 3);
            Controls.Add(grid);
        }
    }
}
