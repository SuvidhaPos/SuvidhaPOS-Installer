using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

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

    private const string SoftwareFolder = @"D:\Suvidha Pos\Software";
    private const string UserVcDriveId = "1v90y9MXcOirG_mlev-IsLrlEVuFa3AIK";
    private const string OfficialVcX64Url = "https://aka.ms/vc14/vc_redist.x64.exe";
    private const string VcFileName = "vcredist.x64.exe";

    private static readonly HttpClient Http = CreateHttpClient();
    private static Task? vcPrefetchTask;
    private static bool refreshPending;

    private static readonly FieldInfo ShellField = Field("shellRoot");
    private static readonly FieldInfo SidebarField = Field("sidebar");
    private static readonly FieldInfo ContentField = Field("content");
    private static readonly FieldInfo HeaderTitleField = Field("headerTitle");
    private static readonly FieldInfo HeaderSubField = Field("headerSub");
    private static readonly FieldInfo StepField = Field("step");
    private static readonly FieldInfo NextField = Field("nextButton");
    private static readonly FieldInfo BusyField = Field("busy");

    public static void Apply(MainForm form)
    {
        // The shell is laid out in logical pixels, so do not let WinForms scale fixed heights twice.
        form.AutoScaleMode = AutoScaleMode.None;
        form.MinimumSize = new Size(1024, 768);
        form.ClientSize = new Size(1366, 768);
        form.FormBorderStyle = FormBorderStyle.Sizable;
        form.MaximizeBox = true;
        form.MinimizeBox = true;
        form.BackColor = Bg;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        form.Padding = Padding.Empty;

        var shell = BuildShell(form);
        ShellField.SetValue(form, shell);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            Padding = new Padding(14, 10, 14, 10),
            Margin = Padding.Empty,
            AutoScroll = false
        };
        ContentField.SetValue(form, content);
        shell.Controls.Add(content, 1, 1);

        var sidebar = BuildSidebar();
        SidebarField.SetValue(form, sidebar);
        shell.Controls.Add(sidebar, 0, 1);

        var (title, sub) = BuildHeader(shell);
        HeaderTitleField.SetValue(form, title);
        HeaderSubField.SetValue(form, sub);

        form.Resize += (_, _) => ScheduleRefresh(form);
        content.ControlAdded += (_, _) => ScheduleRefresh(form);

        WireSidebar(form, sidebar);
        Refresh(form);
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
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
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
            Padding = new Padding(16, 6, 16, 6),
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        shell.Controls.Add(header, 0, 0);
        shell.SetColumnSpan(header, 2);

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
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
            Font = new Font("Segoe UI Semibold", 19F),
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
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        header.Controls.Add(info, 2, 0);

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Text,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        var sub = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5F),
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
            Padding = new Padding(10, 10, 10, 10),
            Margin = Padding.Empty
        };

        string[] names = { "Welcome", "Terms & Conditions", "Components", "Download", "Install", "Database Setup", "Finish" };
        string[] subs = { "Welcome to Installer", "Read important terms", "Select components", "Download installation files", "Install all components", "Configure database & backup", "Installation complete" };
        for (int i = 0; i < names.Length; i++)
        {
            sidebar.Controls.Add(new StepButton(i, names[i], subs[i])
            {
                Tag = i,
                Width = 244,
                Height = 74,
                Margin = new Padding(0, 0, 0, 7)
            });
        }

        sidebar.Controls.Add(new HelpCard
        {
            Width = 244,
            Height = 108,
            Margin = new Padding(0, 7, 0, 0)
        });
        return sidebar;
    }

    private static void WireSidebar(MainForm form, FlowLayoutPanel sidebar)
    {
        foreach (var button in sidebar.Controls.OfType<StepButton>())
        {
            button.Click += (_, _) =>
            {
                if (BusyField.GetValue(form) is true) return;
                var target = (int)button.Tag!;
                if (target <= GetStep(form))
                {
                    InvokePrivate(form, "ShowStep", target);
                    Refresh(form);
                }
            };
        }
    }

    private static void Refresh(MainForm form)
    {
        var shell = ShellField.GetValue(form) as TableLayoutPanel;
        var sidebar = SidebarField.GetValue(form) as FlowLayoutPanel;
        var content = ContentField.GetValue(form) as Panel;
        if (shell == null || sidebar == null || content == null) return;

        int w = Math.Max(form.ClientSize.Width, 1024);
        int h = Math.Max(form.ClientSize.Height, 768);
        int sidebarWidth = Math.Clamp((int)Math.Round(w * 0.205), 250, 286);
        int headerHeight = Math.Clamp((int)Math.Round(h * 0.105), 78, 88);

        shell.SuspendLayout();
        shell.ColumnStyles[0].Width = sidebarWidth;
        shell.RowStyles[0].Height = headerHeight;
        shell.ResumeLayout(true);

        int itemWidth = Math.Max(220, sidebarWidth - sidebar.Padding.Horizontal);
        int itemHeight = h < 820 ? 70 : 74;
        foreach (Control child in sidebar.Controls)
        {
            if (child is StepButton step)
            {
                step.Width = itemWidth;
                step.Height = itemHeight;
            }
            else if (child is HelpCard help)
            {
                help.Width = itemWidth;
                help.Height = h < 820 ? 100 : 108;
            }
        }

        var headerTitle = HeaderTitleField.GetValue(form) as Label;
        var headerSub = HeaderSubField.GetValue(form) as Label;
        if (headerTitle != null) headerTitle.Text = GetHeaderTitle(GetStep(form));
        if (headerSub != null) headerSub.Text = GetHeaderSubtitle(GetStep(form));

        var next = NextField.GetValue(form) as Button;
        if (next != null)
        {
            next.FlatStyle = FlatStyle.Flat;
            next.FlatAppearance.BorderSize = 1;
            next.FlatAppearance.BorderColor = Blue;
            next.BackColor = Blue;
            next.ForeColor = Color.White;
            next.Font = new Font("Segoe UI Semibold", 10.5F);
            next.AutoEllipsis = true;
            next.Dock = DockStyle.Fill;
            next.UseVisualStyleBackColor = false;
            if (!Equals(next.Tag, "FreshUiNext"))
            {
                next.Tag = "FreshUiNext";
                next.Click += (_, _) =>
                {
                    if (GetStep(form) == 0 && BusyField.GetValue(form) is not true)
                    {
                        InvokePrivate(form, "ShowStep", 1);
                        ScheduleRefresh(form);
                    }
                };
            }
        }

        foreach (var child in AllControls(content).OfType<Label>())
        {
            child.AutoEllipsis = true;
            child.UseMnemonic = false;
        }

        UpdateStepVisuals(form, sidebar);
        ManageVcPrefetch(form);
    }

    private static void ManageVcPrefetch(MainForm form)
    {
        if (GetStep(form) != 2) return;
        var next = NextField.GetValue(form) as Button;
        if (next == null) return;

        vcPrefetchTask ??= EnsureVcRedistAsync();
        next.Enabled = vcPrefetchTask.IsCompleted;
        if (vcPrefetchTask.IsCompleted) return;

        _ = vcPrefetchTask.ContinueWith(_ =>
        {
            if (form.IsDisposed || !form.IsHandleCreated) return;
            form.BeginInvoke(new Action(() =>
            {
                if (GetStep(form) != 2) return;
                if (NextField.GetValue(form) is Button currentNext) currentNext.Enabled = true;
            }));
        }, TaskScheduler.Default);
    }

    private static async Task EnsureVcRedistAsync()
    {
        Directory.CreateDirectory(SoftwareFolder);
        var target = Path.Combine(SoftwareFolder, VcFileName);
        if (IsGoodInstaller(target)) return;

        try
        {
            await DownloadGoogleDriveAsync(UserVcDriveId, target);
            if (IsGoodInstaller(target)) return;
        }
        catch
        {
            TryDelete(target);
        }

        await DownloadHttpFileAsync(OfficialVcX64Url, target);
        if (!IsGoodInstaller(target))
            throw new InvalidOperationException("Microsoft Visual C++ Redistributable download was not a valid x64 installer.");
    }

    private static async Task DownloadGoogleDriveAsync(string fileId, string target)
    {
        using var first = await Http.GetAsync(
            $"https://drive.usercontent.google.com/download?id={Uri.EscapeDataString(fileId)}&export=download&confirm=t",
            HttpCompletionOption.ResponseHeadersRead);
        first.EnsureSuccessStatusCode();

        var media = first.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!media.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            await SaveResponseAsync(first, target);
            return;
        }

        var html = await first.Content.ReadAsStringAsync();
        var match = Regex.Match(html, @"confirm=([0-9A-Za-z_-]+)", RegexOptions.IgnoreCase);
        if (!match.Success) throw new InvalidOperationException("Google Drive returned a download page instead of the VC++ installer.");

        using var second = await Http.GetAsync(
            $"https://drive.usercontent.google.com/download?id={Uri.EscapeDataString(fileId)}&export=download&confirm={Uri.EscapeDataString(match.Groups[1].Value)}",
            HttpCompletionOption.ResponseHeadersRead);
        second.EnsureSuccessStatusCode();
        await SaveResponseAsync(second, target);
    }

    private static async Task DownloadHttpFileAsync(string url, string target)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await SaveResponseAsync(response, target);
    }

    private static async Task SaveResponseAsync(HttpResponseMessage response, string target)
    {
        var media = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (media.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Download endpoint returned HTML instead of an installer file.");

        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 131072, useAsync: true);
        await input.CopyToAsync(output);
    }

    private static bool IsGoodInstaller(string path) => File.Exists(path) && new FileInfo(path).Length > 500_000;
    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SuvidhaPOS-Installer/2.3");
        return client;
    }

    private static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in AllControls(child)) yield return nested;
        }
    }

    private static void UpdateStepVisuals(MainForm form, FlowLayoutPanel sidebar)
    {
        int current = GetStep(form);
        foreach (Control c in sidebar.Controls)
            if (c is StepButton item && item.Tag is int index)
                item.Active = index == current;
    }

    private static string GetHeaderTitle(int step) => step switch
    {
        0 => "Welcome",
        1 => "Terms & Conditions",
        2 => "Components",
        3 => "Download",
        4 => "Install",
        5 => "Database Setup",
        6 => "Finish",
        _ => "Suvidha POS Installer"
    };

    private static string GetHeaderSubtitle(int step) => step switch
    {
        0 => "Welcome to Installer",
        1 => "Read important terms",
        2 => "Select components",
        3 => "Download installation files",
        4 => "Install all components",
        5 => "Configure database & backup",
        6 => "Installation complete",
        _ => string.Empty
    };

    private static int GetStep(MainForm form) => StepField.GetValue(form) is int value ? Math.Clamp(value, 0, 6) : 0;
    private static FieldInfo Field(string name) => typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException(typeof(MainForm).FullName, name);
    private static void InvokePrivate(MainForm form, string method, params object[] args) => typeof(MainForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(form, args);

    private sealed class StepButton : Panel
    {
        private bool active;
        public bool Active { get => active; set { active = value; Invalidate(); } }

        public StepButton(int index, string title, string subtitle)
        {
            DoubleBuffered = true;
            Padding = new Padding(9, 7, 9, 7);
            Cursor = Cursors.Hand;
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 46));

            var number = new Label
            {
                Text = (index + 1).ToString(),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 12F),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = Padding.Empty
            };
            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(8, 0, 0, 0)
            };
            var subLabel = new Label
            {
                Text = subtitle,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(154, 177, 202),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(8, 0, 0, 0)
            };

            grid.Controls.Add(number, 0, 0);
            grid.SetRowSpan(number, 2);
            grid.Controls.Add(titleLabel, 1, 0);
            grid.Controls.Add(subLabel, 1, 1);
            Controls.Add(grid);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new SolidBrush(active ? Color.FromArgb(24, 20, 105, 170) : Color.FromArgb(7, 23, 44));
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(active ? Blue : Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            const int size = 36;
            int y = Math.Max(6, (Height - size) / 2);
            using var fill = new SolidBrush(active ? Color.FromArgb(12, 119, 225) : Color.FromArgb(20, 40, 68));
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(fill, 12, y, size, size);
        }
    }

    private sealed class HelpCard : Panel
    {
        public HelpCard()
        {
            BackColor = CardBg;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(10);
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.Controls.Add(new Label { Text = "◉  Need Help?", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11F), ForeColor = Blue, AutoEllipsis = true }, 0, 0);
            grid.Controls.Add(new Label { Text = "Support is available if you need help.", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8F), ForeColor = Text, AutoEllipsis = true }, 0, 1);
            grid.Controls.Add(new Label { Dock = DockStyle.Fill }, 0, 2);
            grid.Controls.Add(new Label { Text = "+91 827171 8844", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F), ForeColor = Blue, AutoEllipsis = true }, 0, 3);
            Controls.Add(grid);
        }
    }

    private static void ScheduleRefresh(MainForm form)
    {
        if (refreshPending || form.IsDisposed || !form.IsHandleCreated) return;
        refreshPending = true;
        form.BeginInvoke(new Action(() =>
        {
            refreshPending = false;
            if (!form.IsDisposed) Refresh(form);
        }));
    }
}
