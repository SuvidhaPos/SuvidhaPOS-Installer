using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace SuvidhaPosInstaller;

public sealed class MainForm : Form
{
    private enum ComponentKind { Exe, Msi, Local }

    private sealed class ComponentItem
    {
        public ComponentItem(string name, string desc, string id, ComponentKind kind, bool selected = true)
        { Name = name; Description = desc; DriveId = id; Kind = kind; Selected = selected; }
        public string Name { get; }
        public string Description { get; }
        public string DriveId { get; }
        public ComponentKind Kind { get; }
        public bool Selected { get; set; }
        public string Status { get; set; } = "Waiting";
        public string? Error { get; set; }
        public CheckBox? Check { get; set; }
        public Label? StatusLabel { get; set; }
        public ProgressBar? Progress { get; set; }
    }

    private sealed class ResumeState
    {
        public int Step { get; set; }
        public HashSet<string> Completed { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string? BackupPath { get; set; }
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
        public bool SetupCompleted { get; set; }
    }

    private const string ResumeTaskName = "SuvidhaPOS Installer Resume";
    private static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SuvidhaPOS", "Installer");
    private static readonly string ResumeFile = Path.Combine(DataDir, "resume.json");
    private static readonly string DownloadDir = Path.Combine(DataDir, "Downloads");
    private const string SoftwareFolder = @"D:\Suvidha Pos\Software";
    private const string SqlDriveId = "1y5d9D1tkOn81dN6I7aPtkd4DRQJCtmhT";
    private const string SsMsDriveId = "1QFaDEaOb-qGhqLIfqpXrHYhDeCp19gPT";
    private const string CrystalDriveId = "1u3YBZqVdx5tIOPh8RU19FrOjWC-Np05v";

    private static readonly Color Bg = Color.FromArgb(4, 12, 25);
    private static readonly Color SidebarBg = Color.FromArgb(3, 14, 31);
    private static readonly Color CardBg = Color.FromArgb(6, 23, 48);
    private static readonly Color CardBg2 = Color.FromArgb(7, 28, 58);
    private static readonly Color Border = Color.FromArgb(18, 65, 111);
    private static readonly Color TextColor = Color.FromArgb(244, 247, 252);
    private static readonly Color Muted = Color.FromArgb(166, 184, 207);
    private static readonly Color Blue = Color.FromArgb(0, 166, 255);
    private static readonly Color Cyan = Color.FromArgb(0, 211, 255);
    private static readonly Color Purple = Color.FromArgb(155, 58, 255);
    private static readonly Color Pink = Color.FromArgb(241, 42, 139);
    private static readonly Color Orange = Color.FromArgb(255, 139, 28);
    private static readonly Color Green = Color.FromArgb(48, 224, 119);
    private static readonly Color Red = Color.FromArgb(255, 83, 98);

    private readonly HttpClient http = new(new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.All });
    private readonly List<ComponentItem> components = new();
    private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
    private ResumeState state = new();
    private int step;
    private bool busy;
    private bool setupFinished;

    private TableLayoutPanel shellRoot = null!;
    private Panel content = null!;
    private FlowLayoutPanel sidebar = null!;
    private Label headerTitle = null!;
    private Label headerSub = null!;
    private Label footerStep = null!;
    private Label footerPercent = null!;
    private ProgressBar footerProgress = null!;
    private Button backButton = null!;
    private Button nextButton = null!;
    private Button cancelButton = null!;
    private CheckBox terms = null!;
    private Label downloadSummary = null!;
    private ProgressBar downloadOverall = null!;
    private Label installSummary = null!;
    private ProgressBar installOverall = null!;
    private TextBox backupBox = null!;
    private TextBox serverBox = null!;
    private TextBox databaseBox = null!;
    private CheckBox restoreBox = null!;
    private Label restoreStatus = null!;
    private Label configStatus = null!;
    private Panel pageBody = null!;

    public MainForm()
    {
        LoadState();
        CreateResumeTask();

        components.Add(new ComponentItem("SQL Server 2019", "Core database engine required for Suvidha POS", SqlDriveId, ComponentKind.Exe));
        components.Add(new ComponentItem("SQL Server Management Studio (SSMS)", "Database management and administration tool", SsMsDriveId, ComponentKind.Exe));
        components.Add(new ComponentItem("Crystal Reports Runtime (64-bit)", "Reports runtime required by Suvidha POS", CrystalDriveId, ComponentKind.Msi));
        components.Add(new ComponentItem("Microsoft Visual C++ Redistributable", "Required Windows runtime libraries", "LOCAL_VC", ComponentKind.Local));
        components.Add(new ComponentItem("Suvidha POS Application", "Main Suvidha POS desktop application", "LOCAL_POS", ComponentKind.Local));

        Text = "Suvidha POS Installer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 600);
        Size = new Size(1500, 920);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Bg;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10F);
        DoubleBuffered = true;
        Icon = LoadIcon();

        BuildShell();
        step = Math.Clamp(state.Step, 0, 6);
        ShowStep(step);
    }

    private Icon? LoadIcon()
    {
        try
        {
            var p = Path.Combine(AppContext.BaseDirectory, "Assets", "SuvidhaPOS.ico");
            return File.Exists(p) ? new Icon(p) : null;
        }
        catch { return null; }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SaveState();
        http.Dispose();
        base.OnFormClosed(e);
    }

    private void LoadState()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            if (File.Exists(ResumeFile))
                state = JsonSerializer.Deserialize<ResumeState>(File.ReadAllText(ResumeFile)) ?? new ResumeState();
        }
        catch { state = new ResumeState(); }
        setupFinished = state.SetupCompleted;
    }

    private void SaveState()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            File.WriteAllText(ResumeFile, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private void MarkDone(string key)
    {
        state.Completed.Add(key);
        SaveState();
    }

    private void BuildShell()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Bg, Padding = Padding.Empty, Margin = Padding.Empty };
        shellRoot = root;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 328));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new GradientPanel { Dock = DockStyle.Fill, Padding = new Padding(28, 10, 26, 6), StartColor = Color.FromArgb(4, 17, 36), EndColor = Color.FromArgb(4, 9, 22) };
        root.Controls.Add(header, 0, 0); root.SetColumnSpan(header, 2);
        header.Paint += (_, e) => { using var p = new Pen(Color.FromArgb(25, 80, 128)); e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1); };

        var logo = new PictureBox { Size = new Size(52, 52), Location = new Point(26, 9), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
        try { var img = Path.Combine(AppContext.BaseDirectory, "Assets", "SuvidhaPOS.png"); if (File.Exists(img)) logo.Image = Image.FromFile(img); } catch { }
        header.Controls.Add(logo);
        var brand = new Label { Text = "Suvidha POS", Font = new Font("Segoe UI Semibold", 22F), ForeColor = TextColor, AutoSize = true, Location = new Point(90, 14) };
        header.Controls.Add(brand);
        var installer = new Label { Text = "Installer", Font = new Font("Segoe UI Semibold", 22F), ForeColor = Color.FromArgb(0, 171, 255), AutoSize = true, Location = new Point(224, 14) };
        header.Controls.Add(installer);
        headerTitle = new Label { Text = "Welcome", Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = TextColor, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        headerSub = new Label { Text = "Guided installation", Font = new Font("Segoe UI", 8.5F), ForeColor = Muted, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        header.Controls.Add(headerTitle); header.Controls.Add(headerSub);
        header.Resize += (_, _) => { headerTitle.Left = header.ClientSize.Width - headerTitle.Width - 26; headerSub.Left = header.ClientSize.Width - headerSub.Width - 26; };

        sidebar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = SidebarBg, Padding = new Padding(18, 14, 18, 14), Margin = Padding.Empty };
        root.Controls.Add(sidebar, 0, 1);
        string[] names = { "Welcome", "Terms & Conditions", "Components", "Download", "Install", "Setup & Backup", "Finish" };
        string[] subs = { "Welcome to Installer", "Read important terms", "Select components", "Download installation files", "Install all components", "Database setup & backup", "Installation complete" };
        for (int i = 0; i < names.Length; i++)
        {
            var item = new StepItem(i + 1, names[i], subs[i]) { Width = 292, Height = 88, Tag = i };
            item.Click += (_, _) => { if (!busy && i <= step) ShowStep(i); };
            sidebar.Controls.Add(item);
        }

        var help = new HelpCard { Width = 292, Height = 150, Margin = new Padding(0, 14, 0, 0) };
        sidebar.Controls.Add(help);

        content = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Padding = new Padding(12, 10, 12, 10), AutoScroll = true };
        root.Controls.Add(content, 1, 1);

        // Responsive shell: preserve the visual proportions while allowing the
        // installer to run from 800x600 through large 4K/ultrawide displays.
        Resize += (_, _) => ApplyResponsiveShell();
        ApplyResponsiveShell();
    }

    private void ApplyResponsiveShell()
    {
        if (shellRoot == null || sidebar == null || content == null) return;
        var w = ClientSize.Width;
        var h = ClientSize.Height;
        var sidebarWidth = Math.Clamp((int)Math.Round(w * 0.2187), 220, 328);
        var headerHeight = Math.Clamp((int)Math.Round(h * 0.078), 58, 72);
        shellRoot.ColumnStyles[0].Width = sidebarWidth;
        shellRoot.RowStyles[0].Height = headerHeight;
        sidebar.Padding = new Padding(Math.Clamp(sidebarWidth / 18, 10, 18), 12, Math.Clamp(sidebarWidth / 18, 10, 18), 12);
        foreach (Control c in sidebar.Controls)
        {
            if (c is StepItem item)
            {
                item.Width = Math.Max(180, sidebarWidth - sidebar.Padding.Horizontal);
                item.Height = Math.Clamp((int)Math.Round(h * 0.095), 70, 88);
            }
            else if (c is HelpCard help)
            {
                help.Width = Math.Max(180, sidebarWidth - sidebar.Padding.Horizontal);
            }
        }
    }

    private void BuildFooter(Panel host, string action)
    {
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 76, BackColor = Color.FromArgb(4, 14, 28), Padding = new Padding(18, 11, 18, 10) };
        host.Controls.Add(footer);
        footerStep = new Label { Text = $"Step {step + 1} of 7", ForeColor = Muted, AutoSize = true, Location = new Point(4, 24) };
        footer.Controls.Add(footerStep);
        footerProgress = new ProgressBar { Minimum = 0, Maximum = 100, Value = (int)Math.Round(step / 6.0 * 100), Style = ProgressBarStyle.Continuous, Location = new Point(88, 29), Size = new Size(360, 10), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        footer.Controls.Add(footerProgress);
        footerPercent = new Label { Text = $"{(int)Math.Round(step / 6.0 * 100)}%", ForeColor = Muted, AutoSize = true, Location = new Point(462, 24) };
        footer.Controls.Add(footerPercent);

        cancelButton = MakeButton("Cancel", 112, Color.FromArgb(20, 36, 58));
        backButton = MakeButton("‹  Back", 110, Color.FromArgb(20, 36, 58));
        nextButton = MakeGradientButton(action, 160);
        footer.Controls.Add(cancelButton); footer.Controls.Add(backButton); footer.Controls.Add(nextButton);
        footer.Resize += (_, _) =>
        {
            nextButton.Left = footer.ClientSize.Width - nextButton.Width - 12;
            backButton.Left = Math.Max(8, nextButton.Left - backButton.Width - 10);
            cancelButton.Left = Math.Max(8, backButton.Left - cancelButton.Width - 10);
            cancelButton.Top = backButton.Top = nextButton.Top = 10;
            var left = 88;
            var right = Math.Max(left + 80, cancelButton.Left - 18);
            footerProgress.Left = left;
            footerProgress.Width = Math.Max(80, right - left - 70);
            footerPercent.Left = footerProgress.Right + 10;
        };
        nextButton.Left = footer.ClientSize.Width - nextButton.Width - 12;
        backButton.Left = Math.Max(8, nextButton.Left - backButton.Width - 10);
        cancelButton.Left = Math.Max(8, backButton.Left - cancelButton.Width - 10);
        cancelButton.Top = backButton.Top = nextButton.Top = 10;
        footerProgress.Left = 88;
        footerProgress.Width = Math.Max(80, cancelButton.Left - footerProgress.Left - 88);
        footerPercent.Left = footerProgress.Right + 10;
        cancelButton.Click += (_, _) => Close();
        backButton.Click += (_, _) => { if (!busy && step > 0) ShowStep(step - 1); };
        nextButton.Click += NextClicked;
    }

    private Button MakeButton(string text, int width, Color back)
    {
        var b = new Button { Text = text, Width = width, Height = 44, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 10F), Cursor = Cursors.Hand, TabStop = false };
        b.FlatAppearance.BorderColor = Border; b.FlatAppearance.BorderSize = 1;
        return b;
    }

    private Button MakeGradientButton(string text, int width)
    {
        var b = new GradientButton { Text = text, Width = width, Height = 46, StartColor = Color.FromArgb(151, 23, 255), EndColor = Color.FromArgb(255, 139, 22), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 11F), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabStop = false };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private void ShowStep(int index)
    {
        step = Math.Clamp(index, 0, 6);
        state.Step = step;
        SaveState();
        content.Controls.Clear();
        busy = false;
        UpdateSidebar();
        switch (step)
        {
            case 0: BuildWelcome(); break;
            case 1: BuildTerms(); break;
            case 2: BuildComponents(); break;
            case 3: BuildDownload(); break;
            case 4: BuildInstall(); break;
            case 5: BuildSetupAndBackup(); break;
            case 6: BuildFinish(); break;
        }
    }

    private void UpdateSidebar()
    {
        foreach (Control c in sidebar.Controls)
            if (c is StepItem s && s.Tag is int i) { s.Active = i == step; s.Done = i < step || (setupFinished && i == 6); }
    }

    private void StartPage(string title, string subtitle, string action)
    {
        headerTitle.Text = title; headerSub.Text = subtitle;
        content.Controls.Clear();
        pageBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 4), AutoScroll = true };
        content.Controls.Add(pageBody);
        BuildFooter(content, action);
    }

    private void BuildWelcome()
    {
        StartPage("Welcome", "Welcome to Installer", "Next  →");
        var hero = new RoundedCard { Dock = DockStyle.Top, Height = 348, Margin = new Padding(0, 0, 0, 14) };
        pageBody.Controls.Add(hero); hero.BringToFront();

        var logo = new PictureBox { Size = new Size(230, 150), Location = new Point(30, 28), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
        try { var p = Path.Combine(AppContext.BaseDirectory, "Assets", "SuvidhaPOS.png"); if (File.Exists(p)) logo.Image = Image.FromFile(p); } catch { }
        hero.Controls.Add(logo);
        var title1 = new Label { Text = "Welcome to", Font = new Font("Segoe UI Semibold", 30F), ForeColor = TextColor, AutoSize = true, Location = new Point(265, 35) }; hero.Controls.Add(title1);
        var title2 = new GradientLabel { Text = "Suvidha POS Installer", Font = new Font("Segoe UI Semibold", 36F), AutoSize = true, Location = new Point(265, 80) }; hero.Controls.Add(title2);
        var desc = new Label { Text = "This installer will download and install all required components\nfor Suvidha POS on your computer automatically.", Font = new Font("Segoe UI", 13F), ForeColor = TextColor, AutoSize = true, Location = new Point(268, 143) }; hero.Controls.Add(desc);
        var features = new[]
        {
            AddFeature(hero, 28, 224, "◈", "Safe & Secure", "100% Verified", Cyan),
            AddFeature(hero, 220, 224, "ϟ", "Automatic", "No Manual Steps", Purple),
            AddFeature(hero, 412, 224, "◉", "Fast & Easy", "One Click Install", Green),
            AddFeature(hero, 604, 224, "⚙", "Smart Setup", "Detect & Configure", Orange)
        };
        hero.Resize += (_, _) =>
        {
            var w = hero.ClientSize.Width;
            if (w >= 1000)
            {
                hero.Height = 348; logo.Bounds = new Rectangle(30, 28, 230, 150);
                title1.Location = new Point(265, 35); title1.Font = new Font("Segoe UI Semibold", 30F);
                title2.Location = new Point(265, 80); title2.Font = new Font("Segoe UI Semibold", 36F);
                desc.Location = new Point(268, 143); desc.Font = new Font("Segoe UI", 13F);
                int[] xs = { 28, 220, 412, 604 };
                for (int i = 0; i < features.Length; i++) features[i].Bounds = new Rectangle(xs[i], 224, 175, 84);
            }
            else
            {
                hero.Height = 390;
                var pad = 18; var innerW = Math.Max(300, w - pad * 2); var logoW = Math.Min(180, innerW / 3);
                logo.Bounds = new Rectangle(pad, 12, logoW, 105);
                var tx = pad + logoW + 14;
                title1.Location = new Point(tx, 25); title1.Font = new Font("Segoe UI Semibold", 22F);
                title2.Location = new Point(tx, 60); title2.Font = new Font("Segoe UI Semibold", 25F);
                desc.Location = new Point(pad, 125); desc.Font = new Font("Segoe UI", 10F);
                var gap = 8; var cardW = Math.Max(130, (innerW - gap) / 2);
                for (int i = 0; i < features.Length; i++)
                {
                    var row = i / 2; var col = i % 2;
                    features[i].Bounds = new Rectangle(pad + col * (cardW + gap), 190 + row * 90, cardW, 82);
                }
            }
        };
        hero.PerformLayout();

        var source = new RoundedCard { Dock = DockStyle.Top, Height = 112, Margin = new Padding(0, 0, 0, 14) }; pageBody.Controls.Add(source); source.BringToFront();
        AddSectionTitle(source, "Source Folder", "All software files will be used from this location", 20);
        var folder = new Label { Text = SoftwareFolder, Font = new Font("Segoe UI Semibold", 12F), ForeColor = Color.FromArgb(73, 255, 71), AutoSize = true, Location = new Point(125, 59) }; source.Controls.Add(folder);
        var open = MakeButton("📁  Open Folder", 170, Color.FromArgb(7, 54, 108)); open.Anchor = AnchorStyles.Top | AnchorStyles.Right; source.Controls.Add(open);
        source.Resize += (_, _) =>
        {
            if (source.ClientSize.Width < 650) { source.Height = 132; folder.Location = new Point(20, 72); open.Location = new Point(Math.Max(20, source.ClientSize.Width - open.Width - 18), 30); }
            else { source.Height = 112; folder.Location = new Point(125, 59); open.Location = new Point(Math.Max(20, source.ClientSize.Width - open.Width - 22), 31); }
        };
        open.Location = new Point(Math.Max(20, source.ClientSize.Width - open.Width - 22), 31);
        open.Click += (_, _) => { try { Directory.CreateDirectory(SoftwareFolder); Process.Start("explorer.exe", SoftwareFolder); } catch { } };

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); pageBody.Controls.Add(bottom); bottom.BringToFront();
        bottom.Controls.Add(CreateChecklistCard("What will be installed?", new[] { "SQL Server 2019", "SQL Server Management Studio (SSMS)", "Crystal Reports Runtime", "Suvidha POS Application", "Database Backup & Restore" }), 0, 0);
        bottom.Controls.Add(CreateChecklistCard("System Requirements", new[] { "Windows 10 / 11 (64-bit)", "4 GB RAM or more", "10 GB Free Disk Space", "Internet Connection (For Download)", "Administrator Privileges" }), 1, 0);
    }

    private FeatureCard AddFeature(Control parent, int x, int y, string icon, string title, string sub, Color color)
    {
        var c = new FeatureCard { Size = new Size(175, 84), Location = new Point(x, y), Accent = color }; parent.Controls.Add(c);
        c.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI Symbol", 25F), ForeColor = color, AutoSize = true, Location = new Point(12, 12) });
        c.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 10F), ForeColor = TextColor, AutoSize = true, Location = new Point(54, 15) });
        c.Controls.Add(new Label { Text = sub, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(62, 255, 88), AutoSize = true, Location = new Point(54, 43) });
        return c;
    }

    private Control CreateChecklistCard(string title, string[] lines)
    {
        var card = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 7, 0), Padding = new Padding(24, 18, 20, 10) };
        AddSectionTitle(card, title, "", 15);
        int y = 58;
        foreach (var line in lines)
        {
            card.Controls.Add(new Label { Text = "●", Font = new Font("Segoe UI", 10F), ForeColor = Blue, AutoSize = true, Location = new Point(26, y) });
            card.Controls.Add(new Label { Text = line, Font = new Font("Segoe UI", 9.5F), ForeColor = TextColor, AutoSize = true, Location = new Point(50, y) }); y += 28;
        }
        return card;
    }

    private void BuildTerms()
    {
        StartPage("Terms & Conditions", "Please read the following terms and conditions carefully.", "Next  →");
        var box = new RoundedCard { Dock = DockStyle.Fill, Padding = new Padding(20, 18, 20, 18) }; pageBody.Controls.Add(box); box.BringToFront();
        var title = new Label { Text = "SUVIDHA POS INSTALLER – TERMS AND CONDITIONS", Font = new Font("Segoe UI Semibold", 11F), ForeColor = TextColor, AutoSize = true, Location = new Point(24, 18) }; box.Controls.Add(title);
        var text = new RichTextBox { Location = new Point(20, 52), Size = new Size(900, 500), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(3, 14, 28), ForeColor = TextColor, Font = new Font("Segoe UI", 10F), Text = TermsText() }; box.Controls.Add(text);
        terms = new CheckBox { Text = "I accept the terms and conditions", AutoSize = true, ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 10F), Location = new Point(24, 570), Anchor = AnchorStyles.Bottom | AnchorStyles.Left }; box.Controls.Add(terms);
        if (state.Completed.Contains("terms")) terms.Checked = true;
    }

    private static string TermsText() => string.Join(Environment.NewLine,
        "By using this installer, you agree to the following terms and conditions.", "", 
        "1. The installer will download and launch third-party software packages required for Suvidha POS.",
        "2. Administrator privileges are required for protected Windows and SQL Server operations.",
        "3. SQL Server setup remains interactive so you can choose Default Instance, authentication and other Microsoft setup options.",
        "4. Database restore can overwrite an existing database. Keep a separate copy of your backup before restoring.",
        "5. The installer does not upload your database to Suvidha POS.",
        "6. You are responsible for software licensing, compatibility, disk space and the backup file you select.",
        "7. By accepting these terms you confirm that you understand and authorize the installation operations.");

    private void BuildComponents()
    {
        StartPage("Components", "Select components to download and install.", "Next  →");
        var host = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 4, 0, 0), BackColor = Color.Transparent }; pageBody.Controls.Add(host); host.BringToFront();
        foreach (var c in components)
        {
            var card = new ComponentSelectCard(c) { Width = Math.Max(300, content.ClientSize.Width - 35), Height = 78, Margin = new Padding(0, 0, 0, 9) };
            c.Check = card.Check; host.Controls.Add(card);
        }
        var note = new Label { Text = "SQL Server and SSMS use their normal Microsoft setup screens, so you can select Default Instance and other setup options during installation.", ForeColor = Muted, AutoSize = true, MaximumSize = new Size(1000, 40), Margin = new Padding(6, 8, 0, 0) }; host.Controls.Add(note);
    }

    private void BuildDownload()
    {
        StartPage("Download", "Download installation files.", "Next  →");
        var host = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) }; pageBody.Controls.Add(host); host.BringToFront();
        downloadSummary = new Label { Text = "Ready to download selected files...", Font = new Font("Segoe UI Semibold", 11F), ForeColor = TextColor, AutoSize = true, Margin = new Padding(4, 5, 0, 12) }; host.Controls.Add(downloadSummary);
        foreach (var c in components.Where(x => x.Selected)) host.Controls.Add(CreateProgressCard(c, true));
        var total = new RoundedCard { Width = Math.Max(300, content.ClientSize.Width - 35), Height = 94, Margin = new Padding(0, 8, 0, 0) }; host.Controls.Add(total);
        total.Controls.Add(new Label { Text = "Overall Download Progress", Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = TextColor, AutoSize = true, Location = new Point(18, 12) });
        downloadOverall = new ProgressBar { Location = new Point(18, 43), Size = new Size(total.Width - 36, 12), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Style = ProgressBarStyle.Continuous }; total.Controls.Add(downloadOverall);
        var local = FindLocalMsi(); if (local != null) files["Suvidha POS Application"] = local;
        if (FindLocalVcRedist() is { } vc) files["Microsoft Visual C++ Redistributable"] = vc;
        if (FindLocalBackup() is { } bak) state.BackupPath = bak;
        if (state.Completed.Contains("downloads")) downloadSummary.Text = "Downloads already completed. Existing files will be reused.";
    }

    private Control CreateProgressCard(ComponentItem c, bool download)
    {
        var card = new RoundedCard { Width = Math.Max(300, content.ClientSize.Width - 35), Height = 82, Margin = new Padding(0, 0, 0, 8) };
        var icon = new Label { Text = c.Kind == ComponentKind.Msi ? "▣" : c.Kind == ComponentKind.Local ? "▰" : "▤", Font = new Font("Segoe UI Symbol", 19F), ForeColor = Blue, AutoSize = true, Location = new Point(16, 12) }; card.Controls.Add(icon);
        card.Controls.Add(new Label { Text = c.Name, Font = new Font("Segoe UI Semibold", 10F), ForeColor = TextColor, AutoSize = true, Location = new Point(54, 10) });
        var status = new Label { Text = c.Status, Font = new Font("Segoe UI", 8.5F), ForeColor = Muted, AutoSize = true, Location = new Point(54, 37) }; card.Controls.Add(status); c.StatusLabel = status;
        var pb = new ProgressBar { Location = new Point(54, 57), Size = new Size(420, 8), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Style = ProgressBarStyle.Continuous }; card.Controls.Add(pb); c.Progress = pb;
        return card;
    }

    private void BuildInstall()
    {
        StartPage("Install", "Install all selected components one-by-one.", "Install  →");
        var host = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) }; pageBody.Controls.Add(host); host.BringToFront();
        installSummary = new Label { Text = "Click Install to begin.", Font = new Font("Segoe UI Semibold", 11F), ForeColor = TextColor, AutoSize = true, Margin = new Padding(4, 5, 0, 12) }; host.Controls.Add(installSummary);
        foreach (var c in components.Where(x => x.Selected)) host.Controls.Add(CreateInstallCard(c));
        var total = new RoundedCard { Width = Math.Max(300, content.ClientSize.Width - 35), Height = 94, Margin = new Padding(0, 8, 0, 0) }; host.Controls.Add(total);
        total.Controls.Add(new Label { Text = "Overall Installation Progress", Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = TextColor, AutoSize = true, Location = new Point(18, 12) });
        installOverall = new ProgressBar { Location = new Point(18, 43), Size = new Size(total.Width - 36, 12), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Style = ProgressBarStyle.Continuous }; total.Controls.Add(installOverall);
        if (state.Completed.Contains("installation")) installSummary.Text = "Installation was completed. Click Next to continue.";
    }

    private Control CreateInstallCard(ComponentItem c)
    {
        var card = new RoundedCard { Width = Math.Max(300, content.ClientSize.Width - 35), Height = 74, Margin = new Padding(0, 0, 0, 8) };
        var icon = new Label { Text = "○", Font = new Font("Segoe UI", 18F), ForeColor = Muted, AutoSize = true, Location = new Point(18, 15) }; card.Controls.Add(icon);
        card.Controls.Add(new Label { Text = c.Name, Font = new Font("Segoe UI Semibold", 10F), ForeColor = TextColor, AutoSize = true, Location = new Point(55, 10) });
        var status = new Label { Text = c.Status, Font = new Font("Segoe UI", 8.5F), ForeColor = Muted, AutoSize = true, Location = new Point(55, 38) }; card.Controls.Add(status); c.StatusLabel = status;
        c.Progress = new ProgressBar { Location = new Point(330, 20), Size = new Size(330, 9), Style = ProgressBarStyle.Continuous, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; card.Controls.Add(c.Progress);
        return card;
    }

    private void BuildSetupAndBackup()
    {
        StartPage("Setup & Backup", "Database setup & backup restore.", "Save & Continue  →");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) }; grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42)); grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 150)); grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 260)); grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); pageBody.Controls.Add(grid); grid.BringToFront();
        var backup = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 8) }; grid.Controls.Add(backup, 0, 0); AddSectionTitle(backup, "Backup File", "Select backup file to restore database", 18);
        backupBox = new TextBox { Location = new Point(20, 73), Width = 440, Height = 30, Text = state.BackupPath ?? FindLocalBackup() ?? "" }; backup.Controls.Add(backupBox);
        var browse = MakeButton("Browse", 92, Color.FromArgb(7, 54, 108)); browse.Location = new Point(470, 70); browse.Anchor = AnchorStyles.Top | AnchorStyles.Right; backup.Controls.Add(browse); browse.Click += (_, _) => BrowseBackup();
        restoreBox = new CheckBox { Text = "Restore database after installation", AutoSize = true, ForeColor = TextColor, Checked = !string.IsNullOrWhiteSpace(backupBox.Text), Location = new Point(20, 113) }; backup.Controls.Add(restoreBox);
        restoreStatus = new Label { Text = "", ForeColor = Muted, AutoSize = true, Location = new Point(20, 137) }; backup.Controls.Add(restoreStatus);

        var info = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 8) }; grid.Controls.Add(info, 1, 0); AddSectionTitle(info, "Database Information", "Windows authentication", 18);
        info.Controls.Add(new Label { Text = "Server Name", ForeColor = Muted, AutoSize = true, Location = new Point(20, 64) });
        serverBox = new TextBox { Location = new Point(20, 88), Width = 250, Text = state.ServerName ?? "localhost" }; info.Controls.Add(serverBox);
        info.Controls.Add(new Label { Text = "Database Name", ForeColor = Muted, AutoSize = true, Location = new Point(20, 122) });
        databaseBox = new TextBox { Location = new Point(20, 146), Width = 250, Text = state.DatabaseName ?? "SuvidhaPOS" }; info.Controls.Add(databaseBox);

        var restoreCard = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 8) }; grid.Controls.Add(restoreCard, 0, 1); AddSectionTitle(restoreCard, "Database Restore", "The backup will be restored with replacement if the database already exists.", 18);
        var restoreButton = MakeGradientButton("Restore Database", 190); restoreButton.Location = new Point(24, 70); restoreCard.Controls.Add(restoreButton); restoreButton.Click += async (_, _) => await RestoreOnlyAsync();
        var testButton = MakeButton("Test Connection", 160, Color.FromArgb(7, 54, 108)); testButton.Location = new Point(230, 71); restoreCard.Controls.Add(testButton); testButton.Click += async (_, _) => await TestConnectionAsync();
        restoreCard.Controls.Add(new Label { Text = "The SQL Server setup screens remain interactive. Use Default Instance if that is your required deployment option.", ForeColor = Muted, AutoSize = true, MaximumSize = new Size(560, 70), Location = new Point(24, 122) });

        var config = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 8) }; grid.Controls.Add(config, 1, 1); AddSectionTitle(config, "Suvidha POS Configuration", "Updates SuvidhaPos.exe.config or RetailPos.exe.config", 18);
        var findButton = MakeButton("Detect Application", 180, Color.FromArgb(7, 54, 108)); findButton.Location = new Point(20, 68); config.Controls.Add(findButton); findButton.Click += (_, _) => DetectConfig();
        var saveButton = MakeGradientButton("Save SQL Config", 170); saveButton.Location = new Point(20, 116); config.Controls.Add(saveButton); saveButton.Click += (_, _) => SaveConfigFromStep();
        configStatus = new Label { Text = "", ForeColor = Muted, AutoSize = true, MaximumSize = new Size(350, 80), Location = new Point(20, 170) }; config.Controls.Add(configStatus);

        var note = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) }; grid.Controls.Add(note, 0, 2); AddSectionTitle(note, "Resume Protection", "Your progress is saved automatically.", 18);
        note.Controls.Add(new Label { Text = "If Windows restarts or the PC is switched off, the installer will reopen at the same step after login. Completed files are reused.", ForeColor = TextColor, AutoSize = true, MaximumSize = new Size(620, 70), Location = new Point(24, 66) });
        var finishNote = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) }; grid.Controls.Add(finishNote, 1, 2); AddSectionTitle(finishNote, "Ready", "After configuration, click Save & Continue.", 18); finishNote.Controls.Add(new Label { Text = "Suvidha POS will be available from the Finish screen.", ForeColor = Green, AutoSize = true, Location = new Point(24, 66) });
        DetectConfig();
    }

    private void BuildFinish()
    {
        StartPage("Finish", "Installation complete.", "Finish  ✓");
        setupFinished = true; state.SetupCompleted = true; SaveState(); RemoveResumeTask();
        var hero = new RoundedCard { Dock = DockStyle.Top, Height = 240, Margin = new Padding(0, 0, 0, 14) }; pageBody.Controls.Add(hero); hero.BringToFront();
        hero.Controls.Add(new Label { Text = "✓", Font = new Font("Segoe UI", 60F, FontStyle.Bold), ForeColor = Green, AutoSize = true, Location = new Point(35, 42) });
        hero.Controls.Add(new Label { Text = "Installation completed successfully!", Font = new Font("Segoe UI Semibold", 23F), ForeColor = TextColor, AutoSize = true, Location = new Point(135, 48) });
        hero.Controls.Add(new Label { Text = "All selected components are installed successfully on your computer.", Font = new Font("Segoe UI", 11F), ForeColor = Muted, AutoSize = true, Location = new Point(137, 92) });
        var launch = MakeGradientButton("Launch Suvidha POS", 205); launch.Location = new Point(137, 137); hero.Controls.Add(launch); launch.Click += (_, _) => LaunchPos();
        var list = new RoundedCard { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 10) }; pageBody.Controls.Add(list); list.BringToFront(); AddSectionTitle(list, "Installation Summary", "Completed components", 15);
        int y = 60; foreach (var c in components.Where(x => x.Selected)) { list.Controls.Add(new Label { Text = "●", ForeColor = Green, Font = new Font("Segoe UI", 11F), AutoSize = true, Location = new Point(24, y) }); list.Controls.Add(new Label { Text = c.Name, ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 10F), AutoSize = true, Location = new Point(50, y) }); list.Controls.Add(new Label { Text = "Installed", ForeColor = Green, AutoSize = true, Location = new Point(500, y) }); y += 34; }
    }

    private async void NextClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        if (step == 6) { Close(); return; }
        if (step == 1)
        {
            if (!terms.Checked) { MessageBox.Show(this, "Please accept the Terms & Conditions first.", "Suvidha POS Installer", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            MarkDone("terms"); ShowStep(2); return;
        }
        if (step == 2)
        {
            if (!components.Any(x => x.Selected)) { MessageBox.Show(this, "Select at least one component.", "Suvidha POS Installer", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            ShowStep(3); await DownloadAllAsync(); return;
        }
        if (step == 3)
        {
            if (!AllDownloadsReady()) { MessageBox.Show(this, "Please wait until all selected downloads are ready.", "Download", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            ShowStep(4); return;
        }
        if (step == 4)
        {
            await InstallAllAsync();
            if (!busy && components.Where(x => x.Selected).All(x => x.Status == "Installed" || x.Status == "Ready")) ShowStep(5);
            return;
        }
        if (step == 5)
        {
            state.BackupPath = backupBox.Text.Trim(); state.ServerName = serverBox.Text.Trim(); state.DatabaseName = databaseBox.Text.Trim(); SaveState();
            if (restoreBox.Checked && !string.IsNullOrWhiteSpace(backupBox.Text)) await RestoreOnlyAsync();
            if (busy) return;
            if (!SaveConfigFromStep()) return;
            MarkDone("setup"); ShowStep(6); return;
        }
    }

    private async Task DownloadAllAsync()
    {
        busy = true; SetButtons(false); Directory.CreateDirectory(DownloadDir);
        var selected = components.Where(x => x.Selected).ToList();
        int doneCount = 0;
        foreach (var c in selected)
        {
            try
            {
                string? local = c.Name switch { "Suvidha POS Application" => FindLocalMsi(), "Microsoft Visual C++ Redistributable" => FindLocalVcRedist(), _ => null };
                if (c.Kind == ComponentKind.Local)
                {
                    if (local == null) throw new FileNotFoundException($"{c.Name} was not found in {SoftwareFolder}.");
                    files[c.Name] = local; c.Status = "Ready"; UpdateComponent(c, 100);
                }
                else
                {
                    var ext = c.Kind == ComponentKind.Msi ? ".msi" : ".exe";
                    var target = Path.Combine(DownloadDir, SafeFileName(c.Name) + ext);
                    if (!File.Exists(target) || new FileInfo(target).Length < 1024 * 100) await DownloadDriveFileAsync(c, target);
                    files[c.Name] = target; c.Status = "Downloaded"; UpdateComponent(c, 100);
                }
                doneCount++; downloadOverall.Value = doneCount * 100 / selected.Count; downloadSummary.Text = $"{doneCount} of {selected.Count} files ready.";
            }
            catch (Exception ex) { c.Status = "Failed"; c.Error = ex.Message; UpdateComponent(c, 0); downloadSummary.Text = $"Failed: {c.Name} — {ex.Message}"; busy = false; SetButtons(true); return; }
        }
        MarkDone("downloads"); downloadSummary.Text = "All selected files are ready. Click Next to continue."; busy = false; SetButtons(true);
    }

    private async Task DownloadDriveFileAsync(ComponentItem c, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var url = $"https://drive.usercontent.google.com/download?id={Uri.EscapeDataString(c.DriveId)}&export=download&confirm=t";
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var media = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(media) && media.Contains("text/html", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Google Drive returned a confirmation page instead of the installer file.");
        var total = response.Content.Headers.ContentLength ?? -1;
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 131072, true);
        var buffer = new byte[131072]; long done = 0; int read;
        while ((read = await input.ReadAsync(buffer)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read)); done += read;
            var pct = total > 0 ? (int)Math.Clamp(done * 100 / total, 0, 100) : 0; UpdateComponent(c, pct); downloadSummary.Text = $"Downloading {c.Name} — {pct}%";
        }
    }

    private async Task InstallAllAsync()
    {
        busy = true; SetButtons(false);
        var selected = components.Where(x => x.Selected).ToList();
        int done = 0;
        foreach (var c in selected)
        {
            if (c.Status == "Installed" || c.Status == "Ready" && c.Kind == ComponentKind.Local && c.Name == "Suvidha POS Application") { done++; installOverall.Value = done * 100 / selected.Count; continue; }
            if (!files.TryGetValue(c.Name, out var path) || !File.Exists(path)) { c.Status = "Failed"; c.Error = "Installer file not found."; installSummary.Text = c.Error; busy = false; SetButtons(true); return; }
            c.Status = "Installing"; UpdateComponent(c, 10); installSummary.Text = $"Installing {c.Name}...";
            try
            {
                MarkDone("started:" + c.Name);
                await RunInstallerAsync(path, c.Kind);
                c.Status = "Installed"; UpdateComponent(c, 100);
                done++; installOverall.Value = done * 100 / selected.Count;
            }
            catch (Exception ex) { c.Status = "Failed"; c.Error = ex.Message; installSummary.Text = $"Failed: {c.Name} — {ex.Message}"; busy = false; SetButtons(true); return; }
        }
        MarkDone("installation"); installSummary.Text = "All selected components have finished. Click Next for database setup."; busy = false; SetButtons(true);
    }

    private static async Task RunInstallerAsync(string path, ComponentKind kind)
    {
        ProcessStartInfo psi = kind == ComponentKind.Msi
            ? new ProcessStartInfo("msiexec.exe", $"/i \"{path}\"") { UseShellExecute = true, Verb = "runas", WorkingDirectory = Path.GetDirectoryName(path)! }
            : new ProcessStartInfo(path) { UseShellExecute = true, Verb = "runas", WorkingDirectory = Path.GetDirectoryName(path)! };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Windows could not start the installer.");
        await p.WaitForExitAsync();
        if (p.ExitCode != 0 && p.ExitCode != 3010 && p.ExitCode != 1641) throw new InvalidOperationException($"Installer exited with code {p.ExitCode}.");
    }

    private async Task RestoreOnlyAsync()
    {
        if (!restoreBox.Checked) { restoreStatus.Text = "Restore is disabled."; restoreStatus.ForeColor = Muted; return; }
        var backup = backupBox.Text.Trim(); var server = serverBox.Text.Trim(); var db = databaseBox.Text.Trim();
        if (!File.Exists(backup)) { restoreStatus.Text = "Backup file not found."; restoreStatus.ForeColor = Red; return; }
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(db)) { restoreStatus.Text = "Server Name and Database Name are required."; restoreStatus.ForeColor = Red; return; }
        busy = true; SetButtons(false); restoreStatus.Text = "Restoring database..."; restoreStatus.ForeColor = Muted;
        try
        {
            var cs = new SqlConnectionStringBuilder { DataSource = server, IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 15 }.ConnectionString;
            await using var cn = new SqlConnection(cs); await cn.OpenAsync();
            var escapedDb = db.Replace("]", "]]", StringComparison.Ordinal); var escapedPath = backup.Replace("'", "''", StringComparison.Ordinal);
            var sql = $"IF DB_ID(N'{db.Replace("'", "''", StringComparison.Ordinal)}') IS NOT NULL BEGIN ALTER DATABASE [{escapedDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; END; RESTORE DATABASE [{escapedDb}] FROM DISK = N'{escapedPath}' WITH REPLACE; ALTER DATABASE [{escapedDb}] SET MULTI_USER;";
            await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = 1800 }; await cmd.ExecuteNonQueryAsync();
            restoreStatus.Text = "Database restore completed successfully ✓"; restoreStatus.ForeColor = Green; MarkDone("restore");
        }
        catch (Exception ex) { restoreStatus.Text = "Restore failed: " + ex.Message; restoreStatus.ForeColor = Red; }
        finally { busy = false; SetButtons(true); }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            var cs = new SqlConnectionStringBuilder { DataSource = serverBox.Text.Trim(), InitialCatalog = "master", IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 10 }.ConnectionString;
            await using var cn = new SqlConnection(cs); await cn.OpenAsync(); restoreStatus.Text = "SQL Server connection successful ✓"; restoreStatus.ForeColor = Green;
        }
        catch (Exception ex) { restoreStatus.Text = "Connection failed: " + ex.Message; restoreStatus.ForeColor = Red; }
    }

    private bool SaveConfigFromStep()
    {
        if (string.IsNullOrWhiteSpace(serverBox.Text) || string.IsNullOrWhiteSpace(databaseBox.Text)) { configStatus.Text = "Server Name and Database Name are required."; configStatus.ForeColor = Red; return false; }
        var found = FindInstalledPos();
        if (found == null) { configStatus.Text = "SuvidhaPos.exe.config / RetailPos.exe.config was not found. Click Detect Application after the POS MSI finishes installing."; configStatus.ForeColor = Red; return false; }
        try
        {
            var doc = XDocument.Load(found.Value.ConfigPath, LoadOptions.PreserveWhitespace);
            var add = doc.Descendants("add").FirstOrDefault(x => string.Equals((string?)x.Attribute("key"), "sqlKey", StringComparison.OrdinalIgnoreCase));
            if (add == null) { configStatus.Text = "sqlKey entry was not found in the configuration file."; configStatus.ForeColor = Red; return false; }
            add.SetAttributeValue("value", $"Data Source={serverBox.Text.Trim()};Initial Catalog={databaseBox.Text.Trim()};Integrated Security=True");
            doc.Save(found.Value.ConfigPath);
            state.ServerName = serverBox.Text.Trim(); state.DatabaseName = databaseBox.Text.Trim(); SaveState();
            configStatus.Text = $"Saved to {Path.GetFileName(found.Value.ConfigPath)} ✓"; configStatus.ForeColor = Green; return true;
        }
        catch (Exception ex) { configStatus.Text = "Could not save configuration: " + ex.Message; configStatus.ForeColor = Red; return false; }
    }

    private void DetectConfig()
    {
        var found = FindInstalledPos();
        configStatus.Text = found == null ? "Suvidha POS configuration file not found yet." : $"Detected: {found.Value.ConfigPath}";
        configStatus.ForeColor = found == null ? Muted : Green;
    }

    private (string ExePath, string ConfigPath)? FindInstalledPos()
    {
        var roots = new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SuvidhaPOS"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SuvidhaPOS") }.Where(Directory.Exists);
        foreach (var root in roots)
        {
            try
            {
                foreach (var exeName in new[] { "SuvidhaPos.exe", "RetailPos.exe" })
                {
                    var exe = Directory.EnumerateFiles(root, exeName, SearchOption.AllDirectories).FirstOrDefault();
                    if (exe != null && File.Exists(exe + ".config")) return (exe, exe + ".config");
                }
            }
            catch { }
        }
        return null;
    }

    private void BrowseBackup()
    {
        using var d = new OpenFileDialog { Filter = "SQL Backup (*.bak;*.backup)|*.bak;*.backup|All files (*.*)|*.*", InitialDirectory = Directory.Exists(SoftwareFolder) ? SoftwareFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyComputer) };
        if (d.ShowDialog(this) == DialogResult.OK) { backupBox.Text = d.FileName; state.BackupPath = d.FileName; SaveState(); }
    }

    private void LaunchPos()
    {
        var found = FindInstalledPos();
        if (found == null) { MessageBox.Show(this, "SuvidhaPos.exe / RetailPos.exe was not found.", "Suvidha POS", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try { Process.Start(new ProcessStartInfo(found.Value.ExePath) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(found.Value.ExePath)! }); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Suvidha POS", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private string? FindLocalMsi() => Directory.Exists(SoftwareFolder) ? Directory.EnumerateFiles(SoftwareFolder, "*.msi", SearchOption.TopDirectoryOnly).OrderBy(x => x).FirstOrDefault() : null;
    private string? FindLocalVcRedist() => Directory.Exists(SoftwareFolder) ? Directory.EnumerateFiles(SoftwareFolder, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault(x => { var n = Path.GetFileName(x); return n.Contains("vcredist", StringComparison.OrdinalIgnoreCase) || n.Contains("vc_redist", StringComparison.OrdinalIgnoreCase); }) : null;
    private string? FindLocalBackup() => Directory.Exists(SoftwareFolder) ? Directory.EnumerateFiles(SoftwareFolder, "*.*", SearchOption.AllDirectories).FirstOrDefault(x => x.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".backup", StringComparison.OrdinalIgnoreCase)) : null;

    private bool AllDownloadsReady() => components.Where(x => x.Selected).All(x => files.ContainsKey(x.Name) && File.Exists(files[x.Name]));
    private static string SafeFileName(string s) { foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s; }
    private void UpdateComponent(ComponentItem c, int pct) { if (c.Progress != null) c.Progress.Value = Math.Clamp(pct, 0, 100); if (c.StatusLabel != null) { c.StatusLabel.Text = c.Status; c.StatusLabel.ForeColor = c.Status is "Installed" or "Downloaded" or "Ready" ? Green : Muted; } }
    private void SetButtons(bool enabled) { if (backButton == null) return; backButton.Enabled = enabled && step > 0; cancelButton.Enabled = enabled; nextButton.Enabled = enabled; }

    private void AddSectionTitle(Control p, string title, string subtitle, int y)
    {
        p.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 12F), ForeColor = TextColor, AutoSize = true, Location = new Point(20, y) });
        if (!string.IsNullOrWhiteSpace(subtitle)) p.Controls.Add(new Label { Text = subtitle, Font = new Font("Segoe UI", 8.5F), ForeColor = Muted, AutoSize = true, Location = new Point(20, y + 25) });
    }

    private void CreateResumeTask()
    {
        if (state.SetupCompleted) return;
        try
        {
            var taskXml = $"<Task xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\" version=\"1.2\"><Triggers><LogonTrigger><Enabled>true</Enabled></LogonTrigger></Triggers><Principals><Principal id=\"Author\"><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals><Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><StartWhenAvailable>true</StartWhenAvailable><ExecutionTimeLimit>PT0S</ExecutionTimeLimit></Settings><Actions Context=\"Author\"><Exec><Command>{System.Security.SecurityElement.Escape(Application.ExecutablePath)}</Command></Exec></Actions></Task>";
            var temp = Path.Combine(Path.GetTempPath(), "SuvidhaPOS-Installer-Resume.xml"); File.WriteAllText(temp, taskXml, Encoding.UTF8);
            using var p = Process.Start(new ProcessStartInfo("schtasks.exe", $"/Create /TN \"{ResumeTaskName}\" /XML \"{temp}\" /F") { UseShellExecute = true, Verb = "runas", CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }); p?.WaitForExit(10000); try { File.Delete(temp); } catch { }
        }
        catch { }
    }

    private void RemoveResumeTask()
    {
        try { using var p = Process.Start(new ProcessStartInfo("schtasks.exe", $"/Delete /TN \"{ResumeTaskName}\" /F") { UseShellExecute = true, Verb = "runas", CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }); p?.WaitForExit(5000); } catch { }
    }

    private class RoundedCard : Panel
    {
        public RoundedCard()
        {
            BackColor = CardBg;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(10);
            Margin = new Padding(0);
            DoubleBuffered = true;
        }
    }

    private sealed class FeatureCard : Panel
    {
        public Color Accent { get; set; } = Blue;

        public FeatureCard()
        {
            BackColor = CardBg2;
            BorderStyle = BorderStyle.FixedSingle;
            DoubleBuffered = true;
        }
    }

    private sealed class StepItem : Panel
    {
        private readonly Label number;
        private readonly Label title;
        private readonly Label sub;
        private bool active;
        private bool done;

        public bool Active
        {
            get => active;
            set { active = value; ApplyStyle(); }
        }

        public bool Done
        {
            get => done;
            set
            {
                done = value;
                number.Text = done ? "✓" : number.Tag?.ToString() ?? "";
                ApplyStyle();
            }
        }

        public StepItem(int n, string name, string subtitle)
        {
            BackColor = SidebarBg;
            BorderStyle = BorderStyle.FixedSingle;
            DoubleBuffered = true;

            number = new Label
            {
                Text = n.ToString(),
                Tag = n.ToString(),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(42, 42),
                Location = new Point(14, 22),
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White
            };
            Controls.Add(number);

            title = new Label
            {
                Text = name,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5F),
                ForeColor = TextColor,
                Location = new Point(70, 18)
            };
            Controls.Add(title);

            sub = new Label
            {
                Text = subtitle,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Muted,
                Location = new Point(70, 47)
            };
            Controls.Add(sub);

            ApplyStyle();
        }

        private void ApplyStyle()
        {
            BackColor = active ? Color.FromArgb(12, 38, 70) : SidebarBg;
            BorderStyle = BorderStyle.FixedSingle;
            number.BackColor = done
                ? Green
                : active ? Blue : Color.FromArgb(20, 40, 68);
            number.ForeColor = Color.White;
            title.ForeColor = TextColor;
            sub.ForeColor = Muted;
        }
    }

    private sealed class HelpCard : Panel
    {
        public HelpCard()
        {
            BackColor = CardBg;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(18);
            DoubleBuffered = true;

            Controls.Add(new Label
            {
                Text = "Need Help?",
                Font = new Font("Segoe UI Semibold", 13F),
                ForeColor = Cyan,
                AutoSize = true,
                Location = new Point(18, 16)
            });
            Controls.Add(new Label
            {
                Text = "We're here to help you",
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(18, 48)
            });
            Controls.Add(new Label
            {
                Text = "+91 70042 52545",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = Cyan,
                AutoSize = true,
                Location = new Point(18, 78)
            });
            Controls.Add(new Label
            {
                Text = "support@suvidhapos.com",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(18, 108)
            });
        }
    }

    private sealed class ComponentSelectCard : RoundedCard
    {
        public CheckBox Check { get; }
        public ComponentSelectCard(ComponentItem c)
        {
            Check = new CheckBox { Checked = c.Selected, AutoSize = true, Location = new Point(Width - 38, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right }; Controls.Add(Check); Check.CheckedChanged += (_, _) => c.Selected = Check.Checked;
            Controls.Add(new Label { Text = c.Kind == ComponentKind.Msi ? "▣" : c.Kind == ComponentKind.Local ? "▰" : "▤", Font = new Font("Segoe UI Symbol", 20F), ForeColor = c.Kind == ComponentKind.Local ? Green : Blue, AutoSize = true, Location = new Point(18, 18) });
            Controls.Add(new Label { Text = c.Name, Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = TextColor, AutoSize = true, Location = new Point(58, 13) });
            Controls.Add(new Label { Text = c.Description, Font = new Font("Segoe UI", 8.5F), ForeColor = Muted, AutoSize = true, Location = new Point(58, 41) });
        }
    }

    private sealed class GradientPanel : Panel
    {
        public Color StartColor { get; set; } = Color.Black;
        public Color EndColor { get; set; } = Color.Black;

        public GradientPanel()
        {
            BackColor = Bg;
            DoubleBuffered = true;
        }
    }

    private sealed class GradientLabel : Label
    {
        public GradientLabel()
        {
            ForeColor = Color.FromArgb(194, 124, 255);
            AutoEllipsis = true;
        }
    }

    private sealed class GradientButton : Button
    {
        public Color StartColor { get; set; } = Blue;
        public Color EndColor { get; set; } = Blue;

        public GradientButton()
        {
            BackColor = Blue;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
        }
    }

}
