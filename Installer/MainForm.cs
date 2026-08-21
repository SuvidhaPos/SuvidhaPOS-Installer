using System.Diagnostics;
using System.Drawing.Drawing2D;
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
        ClientSize = new Size(1366, 768);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        AutoScaleMode = AutoScaleMode.None;
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
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Bg,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shellRoot = root;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.FromArgb(4, 16, 34),
            Margin = Padding.Empty,
            Padding = new Padding(16, 8, 16, 8)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 8, 2)
        };
        try
        {
            var img = Path.Combine(AppContext.BaseDirectory, "Assets", "SuvidhaPOS.png");
            if (File.Exists(img)) logo.Image = Image.FromFile(img);
        }
        catch { }
        header.Controls.Add(logo, 0, 0);

        var brand = new Label
        {
            Text = "Suvidha POS  |  Installer",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 18F),
            ForeColor = TextColor,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        header.Controls.Add(brand, 1, 0);

        var pageInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        pageInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        pageInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        header.Controls.Add(pageInfo, 2, 0);

        headerTitle = new Label
        {
            Text = "Welcome",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = TextColor,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true
        };
        headerSub = new Label
        {
            Text = "Guided installation",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Muted,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true
        };
        pageInfo.Controls.Add(headerTitle, 0, 0);
        pageInfo.Controls.Add(headerSub, 0, 1);

        sidebar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = SidebarBg,
            Padding = new Padding(10, 12, 10, 12),
            Margin = Padding.Empty
        };
        root.Controls.Add(sidebar, 0, 1);

        string[] names = { "Welcome", "Terms & Conditions", "Components", "Download", "Install", "Database Setup", "Finish" };
        string[] subs = { "Welcome to Installer", "Read important terms", "Select components", "Download installation files", "Install all components", "Database setup", "Installation complete" };

        for (int i = 0; i < names.Length; i++)
        {
            int targetStep = i;
            var item = new StepItem(i + 1, names[i], subs[i])
            {
                Width = 230,
                Height = 78,
                Tag = targetStep,
                Margin = new Padding(0, 0, 0, 7)
            };
            item.Click += (_, _) => { if (!busy && targetStep <= step) ShowStep(targetStep); };
            sidebar.Controls.Add(item);
        }

        var help = new HelpCard
        {
            Width = 230,
            Height = 120,
            Margin = new Padding(0, 7, 0, 0)
        };
        sidebar.Controls.Add(help);

        content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            Padding = new Padding(12, 10, 12, 10),
            Margin = Padding.Empty
        };
        root.Controls.Add(content, 1, 1);

        ApplyResponsiveShell();
    }

    private void ApplyResponsiveShell()
    {
        if (shellRoot == null || sidebar == null || content == null) return;

        shellRoot.ColumnStyles[0].Width = 250;
        shellRoot.RowStyles[0].Height = 76;

        int innerWidth = 230;
        foreach (Control c in sidebar.Controls)
        {
            if (c is StepItem item)
            {
                item.Width = innerWidth;
                item.Height = 78;
            }
            else if (c is HelpCard help)
            {
                help.Width = innerWidth;
                help.Height = 120;
            }
        }
    }

    private void BuildFooter(Panel host, string action)
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = Color.FromArgb(4, 14, 28),
            Padding = new Padding(10, 8, 10, 8),
            Margin = Padding.Empty
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 144));
        host.Controls.Add(footer);

        footerStep = new Label
        {
            Text = $"Step {step + 1} of 7",
            ForeColor = Muted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        footer.Controls.Add(footerStep, 0, 0);

        footerProgress = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = (int)Math.Round(step / 6.0 * 100),
            Style = ProgressBarStyle.Continuous,
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 12, 6, 12)
        };
        footer.Controls.Add(footerProgress, 1, 0);

        footerPercent = new Label
        {
            Text = $"{(int)Math.Round(step / 6.0 * 100)}%",
            ForeColor = Muted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        footer.Controls.Add(footerPercent, 2, 0);

        cancelButton = MakeButton("Cancel", 88, Color.FromArgb(20, 36, 58));
        backButton = MakeButton("‹  Back", 98, Color.FromArgb(20, 36, 58));
        nextButton = MakeGradientButton(action, 138);

        cancelButton.Dock = DockStyle.Fill;
        backButton.Dock = DockStyle.Fill;
        nextButton.Dock = DockStyle.Fill;
        footer.Controls.Add(cancelButton, 3, 0);
        footer.Controls.Add(backButton, 4, 0);
        footer.Controls.Add(nextButton, 5, 0);

        cancelButton.Click += (_, _) => Close();
        backButton.Click += (_, _) => { if (!busy && step > 0) ShowStep(step - 1); };
        nextButton.Click += NextClicked;

        AcceptButton = nextButton;
    }

    private Button MakeButton(string text, int width, Color back)
    {
        var b = new Button { Text = text, Width = width, Height = 44, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 10F), Cursor = Cursors.Hand, TabStop = false };
        b.FlatAppearance.BorderColor = Border; b.FlatAppearance.BorderSize = 1;
        return b;
    }

    private Button MakeGradientButton(string text, int width)
    {
        var b = new Button
        {
            Text = text,
            Width = width,
            Height = 46,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(92, 76, 220),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10.5F),
            Cursor = Cursors.Hand,
            TabStop = false,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(125, 109, 240);
        b.FlatAppearance.BorderSize = 1;
        return b;
    }

    private void ShowStep(int index)
    {
        step = Math.Clamp(index, 0, 6);
        state.Step = step;
        SaveState();
        AcceptButton = null;
        foreach (Control child in content.Controls)
            child.Dispose();
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
        headerTitle.Text = title;
        headerSub.Text = subtitle;
        content.Controls.Clear();
        content.AutoScroll = false;

        pageBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4),
            AutoScroll = true,
            Margin = Padding.Empty
        };
        content.Controls.Add(pageBody);
        BuildFooter(content, action);
    }

    private void BuildWelcome()
    {
        StartPage("Welcome", "Welcome to Installer", "Next  →");

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Height = 526
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        pageBody.Controls.Add(stack);
        stack.Width = Math.Max(1, pageBody.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);

        var hero = new RoundedCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18)
        };
        stack.Controls.Add(hero, 0, 0);

        var heroGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        heroGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        heroGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heroGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 68));
        heroGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 32));
        hero.Controls.Add(heroGrid);

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(4, 8, 14, 8)
        };
        try
        {
            var p = Path.Combine(AppContext.BaseDirectory, "Assets", "SuvidhaPOS.png");
            if (File.Exists(p)) logo.Image = Image.FromFile(p);
        }
        catch { }
        heroGrid.Controls.Add(logo, 0, 0);
        heroGrid.SetRowSpan(logo, 2);

        var titlePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };
        heroGrid.Controls.Add(titlePanel, 1, 0);

        var title1 = new Label
        {
            Text = "Welcome to Suvidha POS",
            Font = new Font("Segoe UI Semibold", 25F),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 42,
            AutoEllipsis = true
        };
        titlePanel.Controls.Add(title1);

        var desc = new Label
        {
            Text = "Install the required components safely and step-by-step.",
            Font = new Font("Segoe UI", 11F),
            ForeColor = Muted,
            Dock = DockStyle.Top,
            Height = 42,
            AutoEllipsis = true
        };
        titlePanel.Controls.Add(desc);

        var sourceHint = new Label
        {
            Text = $"Files are kept in: {SoftwareFolder}",
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = Green,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        heroGrid.Controls.Add(sourceHint, 1, 1);

        var featureGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 10),
            Padding = Padding.Empty
        };
        for (int i = 0; i < 4; i++)
            featureGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        stack.Controls.Add(featureGrid, 0, 1);

        string[] icons = { "✓", "ϟ", "●", "⚙" };
        string[] titles = { "Safe & Secure", "Automatic", "Fast & Easy", "Smart Setup" };
        string[] subs = { "Verified files", "Guided process", "One-click flow", "Detect & configure" };
        Color[] colors = { Cyan, Purple, Green, Orange };

        for (int i = 0; i < 4; i++)
        {
            var feature = new FeatureCard
            {
                Dock = DockStyle.Fill,
                Accent = colors[i],
                Margin = new Padding(i == 0 ? 0 : 4, 0, i == 3 ? 0 : 4, 0)
            };
            featureGrid.Controls.Add(feature, i, 0);

            feature.Controls.Add(new Label
            {
                Text = icons[i],
                Font = new Font("Segoe UI Symbol", 18F, FontStyle.Bold),
                ForeColor = colors[i],
                Dock = DockStyle.Left,
                Width = 42,
                TextAlign = ContentAlignment.MiddleCenter
            });
            feature.Controls.Add(new Label
            {
                Text = titles[i] + Environment.NewLine + subs[i],
                Font = new Font("Segoe UI Semibold", 8.5F),
                ForeColor = TextColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            });
        }

        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        stack.Controls.Add(bottom, 0, 2);

        var installed = CreateChecklistCard("What will be installed?", new[]
        {
            "SQL Server 2019",
            "SQL Server Management Studio",
            "Crystal Reports Runtime",
            "Suvidha POS Application",
            "Database Backup Restore"
        });
        installed.Dock = DockStyle.Fill;
        installed.Margin = new Padding(0, 0, 5, 0);
        bottom.Controls.Add(installed, 0, 0);

        var requirements = CreateChecklistCard("System Requirements", new[]
        {
            "Windows 10 / 11 (64-bit)",
            "4 GB RAM or more",
            "10 GB free disk space",
            "Internet connection for downloads",
            "Administrator privileges"
        });
        requirements.Dock = DockStyle.Fill;
        requirements.Margin = new Padding(5, 0, 0, 0);
        bottom.Controls.Add(requirements, 1, 0);

        pageBody.Resize += (_, _) =>
        {
            stack.Width = Math.Max(1, pageBody.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
            bool compact = stack.Width < 720;
            heroGrid.ColumnStyles[0].Width = compact ? 140 : 175;
            title1.Font = new Font("Segoe UI Semibold", compact ? 19F : 22F);
            desc.Font = new Font("Segoe UI", compact ? 9F : 10F);
        };
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

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        pageBody.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = "SUVIDHA POS INSTALLER – TERMS AND CONDITIONS",
            Font = new Font("Segoe UI Semibold", 10.5F),
            ForeColor = TextColor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 0);

        var text = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(3, 14, 28),
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 9.5F),
            Text = TermsText(),
            DetectUrls = true
        };
        layout.Controls.Add(text, 0, 1);

        terms = new CheckBox
        {
            Text = "I accept the terms and conditions",
            AutoSize = true,
            ForeColor = TextColor,
            Font = new Font("Segoe UI Semibold", 10F),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(4, 0, 0, 0)
        };
        layout.Controls.Add(terms, 0, 2);

        if (state.Completed.Contains("terms")) terms.Checked = true;
    }

    private static string TermsText() => string.Join(Environment.NewLine,
        "By using this installer, you agree to the following terms and conditions.", "", 
        "1. The installer will download and launch third-party software packages required for Suvidha POS.",
        "2. Administrator privileges are required for protected Windows and SQL Server operations.",
        "3. SQL Server and supporting components are installed automatically using predefined Suvidha POS settings.",
        "4. Database restore can overwrite an existing database. Keep a separate copy of your backup before restoring.",
        "5. The installer does not upload your database to Suvidha POS.",
        "6. You are responsible for software licensing, compatibility, disk space and the backup file you select.",
        "7. By accepting these terms you confirm that you understand and authorize the installation operations.");

    private void BuildComponents()
    {
        StartPage("Components", "Select the components you want to install.", "Next  →");

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };
        pageBody.Controls.Add(grid);

        foreach (var c in components)
        {
            var card = new ComponentSelectCard(c)
            {
                Dock = DockStyle.Top,
                Height = 74,
                Margin = new Padding(0, 0, 0, 8)
            };
            c.Check = card.Check;
            grid.Controls.Add(card);
        }

        grid.Controls.Add(new Label
        {
            Text = "All downloaded files are stored directly in D:\\Suvidha Pos\\Software. No component subfolders are created.",
            ForeColor = Muted,
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(4, 8, 4, 0),
            AutoEllipsis = true
        });
    }

    private void BuildDownload()
    {
        StartPage("Download", "Download installation files to the fixed software folder.", "Next  →");
        Directory.CreateDirectory(SoftwareFolder);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };
        pageBody.Controls.Add(grid);

        downloadSummary = new Label
        {
            Text = $"Ready. Target folder: {SoftwareFolder}",
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(4, 4, 4, 0),
            AutoEllipsis = true
        };
        grid.Controls.Add(downloadSummary);

        foreach (var c in components.Where(x => x.Selected))
        {
            var card = CreateProgressCard(c, true);
            card.Dock = DockStyle.Top;
            card.Height = 96;
            card.Margin = new Padding(0, 0, 0, 8);
            grid.Controls.Add(card);
        }

        var total = new RoundedCard
        {
            Dock = DockStyle.Top,
            Height = 84,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(16, 10, 16, 10)
        };
        grid.Controls.Add(total);
        total.Controls.Add(new Label
        {
            Text = "Overall Download Progress",
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 24
        });
        downloadOverall = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous,
            Dock = DockStyle.Top,
            Height = 12,
            Margin = new Padding(0, 6, 0, 0)
        };
        total.Controls.Add(downloadOverall);

        var local = FindLocalMsi();
        if (local != null) files["Suvidha POS Application"] = local;
        if (FindLocalVcRedist() is { } vc) files["Microsoft Visual C++ Redistributable"] = vc;
        if (FindLocalBackup() is { } bak) state.BackupPath = bak;
        if (state.Completed.Contains("downloads"))
            downloadSummary.Text = "Downloads already completed. Existing files in the target folder will be reused.";
    }

    private Control CreateProgressCard(ComponentItem c, bool download)
    {
        var card = new RoundedCard
        {
            Dock = DockStyle.Top,
            Height = 96,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0)
        };

        card.Controls.Add(new Label
        {
            Text = c.Kind == ComponentKind.Msi ? "▣" : c.Kind == ComponentKind.Local ? "▰" : "▤",
            Font = new Font("Segoe UI Symbol", 17F),
            ForeColor = c.Kind == ComponentKind.Local ? Green : Blue,
            Dock = DockStyle.Left,
            Width = 38,
            TextAlign = ContentAlignment.MiddleCenter
        });

        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(6, 0, 0, 0)
        };
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(statusPanel);

        statusPanel.Controls.Add(new Label
        {
            Text = c.Name,
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = TextColor,
            Dock = DockStyle.Fill,
            AutoEllipsis = true
        }, 0, 0);

        var status = new Label
        {
            Text = c.Status,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Muted,
            Dock = DockStyle.Fill,
            AutoEllipsis = true
        };
        statusPanel.Controls.Add(status, 0, 1);
        c.StatusLabel = status;

        var pb = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 0)
        };
        statusPanel.Controls.Add(pb, 0, 2);
        c.Progress = pb;

        return card;
    }

    private void BuildInstall()
    {
        StartPage("Install", "Install selected components one-by-one.", "Install  →");

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };
        pageBody.Controls.Add(grid);

        installSummary = new Label
        {
            Text = "Click Install to begin.",
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(4, 4, 4, 0),
            AutoEllipsis = true
        };
        grid.Controls.Add(installSummary);

        foreach (var c in components.Where(x => x.Selected))
        {
            var card = CreateInstallCard(c);
            card.Dock = DockStyle.Top;
            card.Height = 92;
            card.Margin = new Padding(0, 0, 0, 8);
            grid.Controls.Add(card);
        }

        var total = new RoundedCard
        {
            Dock = DockStyle.Top,
            Height = 84,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(16, 10, 16, 10)
        };
        grid.Controls.Add(total);
        total.Controls.Add(new Label
        {
            Text = "Overall Installation Progress",
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = TextColor,
            Dock = DockStyle.Top,
            Height = 24
        });
        installOverall = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous,
            Dock = DockStyle.Top,
            Height = 12,
            Margin = new Padding(0, 6, 0, 0)
        };
        total.Controls.Add(installOverall);

        if (state.Completed.Contains("installation"))
            installSummary.Text = "Installation was completed. Click Next to continue.";
    }

    private Control CreateInstallCard(ComponentItem c)
    {
        var card = new RoundedCard
        {
            Dock = DockStyle.Top,
            Height = 92,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0)
        };

        card.Controls.Add(new Label
        {
            Text = "○",
            Font = new Font("Segoe UI", 17F),
            ForeColor = Muted,
            Dock = DockStyle.Left,
            Width = 38,
            TextAlign = ContentAlignment.MiddleCenter
        });

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(6, 0, 0, 0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 19));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = c.Name,
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = TextColor,
            Dock = DockStyle.Fill,
            AutoEllipsis = true
        }, 0, 0);

        var status = new Label
        {
            Text = c.Status,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Muted,
            Dock = DockStyle.Fill,
            AutoEllipsis = true
        };
        layout.Controls.Add(status, 0, 1);
        c.StatusLabel = status;

        c.Progress = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 0)
        };
        layout.Controls.Add(c.Progress, 0, 2);
        return card;
    }

    private void BuildSetupAndBackup()
    {
        StartPage("Database Setup", "Database setup, restore and application configuration.", "Save & Continue  →");

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        pageBody.Controls.Add(grid);

        var backup = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 5, 8), Padding = new Padding(16) };
        grid.Controls.Add(backup, 0, 0);
        AddSectionTitle(backup, "Backup File", "Select a .bak/.backup file from the fixed software folder or browse.", 14);

        var backupLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent, Margin = new Padding(0, 12, 0, 0) };
        backupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        backupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        backupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        backupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        backup.Controls.Add(backupLayout);

        backupBox = new TextBox { Dock = DockStyle.Fill, Text = state.BackupPath ?? FindLocalBackup() ?? "" };
        backupLayout.Controls.Add(backupBox, 0, 0);
        var browse = MakeButton("Browse", 92, Color.FromArgb(7, 54, 108));
        browse.Dock = DockStyle.Fill;
        browse.Click += (_, _) => BrowseBackup();
        backupLayout.Controls.Add(browse, 1, 0);

        restoreBox = new CheckBox
        {
            Text = "Restore database after installation",
            AutoSize = true,
            ForeColor = TextColor,
            Checked = !string.IsNullOrWhiteSpace(backupBox.Text)
        };
        backupLayout.Controls.Add(restoreBox, 0, 1);
        restoreStatus = new Label { Text = "", ForeColor = Muted, Dock = DockStyle.Fill, AutoEllipsis = true };
        backupLayout.Controls.Add(restoreStatus, 1, 1);

        var info = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 8), Padding = new Padding(16) };
        grid.Controls.Add(info, 1, 0);
        AddSectionTitle(info, "Database Information", "Windows authentication", 14);

        var infoLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent, Margin = new Padding(0, 12, 0, 0) };
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        infoLayout.Controls.Add(new Label { Text = "Server Name", ForeColor = Muted, Dock = DockStyle.Top, Height = 22 }, 0, 0);
        infoLayout.Controls.Add(new Label { Text = "Database Name", ForeColor = Muted, Dock = DockStyle.Top, Height = 22 }, 1, 0);
        serverBox = new TextBox { Dock = DockStyle.Top, Text = state.ServerName ?? "localhost" };
        databaseBox = new TextBox { Dock = DockStyle.Top, Text = state.DatabaseName ?? "SuvidhaPOS" };
        infoLayout.Controls.Add(serverBox, 0, 1);
        infoLayout.Controls.Add(databaseBox, 1, 1);
        info.Controls.Add(infoLayout);

        var restoreCard = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 5, 8), Padding = new Padding(16) };
        grid.Controls.Add(restoreCard, 0, 1);
        AddSectionTitle(restoreCard, "Database Restore", "Restore the selected backup with replacement.", 14);

        var restoreButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };
        restoreCard.Controls.Add(restoreButtons);
        var restoreButton = MakeGradientButton("Restore Database", 170);
        var testButton = MakeButton("Test Connection", 150, Color.FromArgb(7, 54, 108));
        restoreButtons.Controls.Add(restoreButton);
        restoreButtons.Controls.Add(testButton);
        restoreButton.Click += async (_, _) => await RestoreOnlyAsync();
        testButton.Click += async (_, _) => await TestConnectionAsync();

        restoreCard.Controls.Add(new Label
        {
            Text = "SQL Server setup screens remain interactive. Use Default Instance if required.",
            ForeColor = Muted,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
            AutoEllipsis = true
        });

        var config = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 8), Padding = new Padding(16) };
        grid.Controls.Add(config, 1, 1);
        AddSectionTitle(config, "Suvidha POS Configuration", "Updates SuvidhaPos.exe.config or RetailPos.exe.config.", 14);

        var configButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };
        config.Controls.Add(configButtons);
        var findButton = MakeButton("Detect Application", 170, Color.FromArgb(7, 54, 108));
        var saveButton = MakeGradientButton("Save SQL Config", 160);
        configButtons.Controls.Add(findButton);
        configButtons.Controls.Add(saveButton);
        findButton.Click += (_, _) => DetectConfig();
        saveButton.Click += (_, _) => SaveConfigFromStep();

        configStatus = new Label { Text = "", ForeColor = Muted, Dock = DockStyle.Fill, AutoEllipsis = true, Padding = new Padding(0, 8, 0, 0) };
        config.Controls.Add(configStatus);

        var note = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 5, 0), Padding = new Padding(16) };
        grid.Controls.Add(note, 0, 2);
        AddSectionTitle(note, "Resume Protection", "Progress is saved automatically.", 14);
        note.Controls.Add(new Label
        {
            Text = "If Windows restarts, the installer can reopen at the saved step and reuse completed files.",
            ForeColor = TextColor,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
            AutoEllipsis = true
        });

        var finishNote = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0), Padding = new Padding(16) };
        grid.Controls.Add(finishNote, 1, 2);
        AddSectionTitle(finishNote, "Ready", "After configuration, continue to Finish.", 14);
        finishNote.Controls.Add(new Label
        {
            Text = "Suvidha POS will be available from the Finish screen.",
            ForeColor = Green,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
            AutoEllipsis = true
        });

        DetectConfig();
    }

    private void BuildFinish()
    {
        StartPage("Finish", "Installation complete.", "Finish  ✓");
        setupFinished = true;
        state.SetupCompleted = true;
        SaveState();
        RemoveResumeTask();

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 2)
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        pageBody.Controls.Add(stack);

        var hero = new RoundedCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10), Padding = new Padding(20) };
        stack.Controls.Add(hero, 0, 0);

        var heroGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent };
        heroGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        heroGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heroGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        heroGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        hero.Controls.Add(heroGrid);

        var check = new Label
        {
            Text = "✓",
            Font = new Font("Segoe UI", 42F, FontStyle.Bold),
            ForeColor = Green,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        heroGrid.Controls.Add(check, 0, 0);
        heroGrid.SetRowSpan(check, 2);

        heroGrid.Controls.Add(new Label
        {
            Text = "Installation completed successfully!",
            Font = new Font("Segoe UI Semibold", 20F),
            ForeColor = TextColor,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        }, 1, 0);

        var launch = MakeGradientButton("Launch Suvidha POS", 200);
        launch.Anchor = AnchorStyles.Left;
        heroGrid.Controls.Add(launch, 1, 1);
        launch.Click += (_, _) => LaunchPos();

        var list = new RoundedCard { Dock = DockStyle.Fill, Padding = new Padding(18), Margin = Padding.Empty };
        stack.Controls.Add(list, 0, 1);
        AddSectionTitle(list, "Installation Summary", "Selected components", 14);

        var rows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 38, 0, 0)
        };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        list.Controls.Add(rows);

        int row = 0;
        foreach (var c in components.Where(x => x.Selected))
        {
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            rows.Controls.Add(new Label { Text = "●  " + c.Name, ForeColor = TextColor, Dock = DockStyle.Fill, AutoEllipsis = true }, 0, row);
            rows.Controls.Add(new Label { Text = "Installed", ForeColor = Green, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 1, row);
            row++;
        }
    }

    private async void NextClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        if (step == 6) { Close(); return; }

        if (step == 0)
        {
            ShowStep(1);
            return;
        }
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
        busy = true; SetButtons(false); Directory.CreateDirectory(SoftwareFolder);
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
                    var target = Path.Combine(SoftwareFolder, SafeFileName(c.Name) + ext);
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
        Directory.CreateDirectory(SoftwareFolder);
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
        string fileName = Path.GetFileName(path);
        ProcessStartInfo psi;

        if (kind == ComponentKind.Msi)
        {
            psi = new ProcessStartInfo("msiexec.exe")
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
            psi.ArgumentList.Add("/i");
            psi.ArgumentList.Add(path);
            psi.ArgumentList.Add("/qn");
            psi.ArgumentList.Add("/norestart");
        }
        else if (fileName.Contains("SQL Server 2019", StringComparison.OrdinalIgnoreCase))
        {
            if (IsSqlServerInstancePresent())
            {
                installSummary.Text = "SQL Server instance already installed — continuing.";
                return;
            }

            psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };

            foreach (string arg in new[]
            {
                "/Q",
                "/ACTION=Install",
                "/FEATURES=SQLEngine",
                "/INSTANCENAME=SQLEXPRESS",
                "/SQLSVCSTARTUPTYPE=Automatic",
                "/ADDCURRENTUSERASSQLADMIN=True",
                "/TCPENABLED=1",
                "/IACCEPTSQLSERVERLICENSETERMS",
                "/SUPPRESSPRIVACYSTATEMENTNOTICE",
                "/UpdateEnabled=False",
                "/INDICATEPROGRESS"
            })
                psi.ArgumentList.Add(arg);
        }
        else if (fileName.Contains("SSMS", StringComparison.OrdinalIgnoreCase))
        {
            psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
            psi.ArgumentList.Add("--quiet");
            psi.ArgumentList.Add("--wait");
            psi.ArgumentList.Add("--norestart");
        }
        else
        {
            psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
            psi.ArgumentList.Add("/quiet");
            psi.ArgumentList.Add("/norestart");
        }

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Windows could not start the installer.");
        await p.WaitForExitAsync();
        if (p.ExitCode != 0 && p.ExitCode != 3010 && p.ExitCode != 1641)
            throw new InvalidOperationException($"Installer exited with code {p.ExitCode}.");
    }

    private static bool IsSqlServerInstancePresent()
    {
        foreach (var view in new[]
        {
            Microsoft.Win32.RegistryView.Registry64,
            Microsoft.Win32.RegistryView.Registry32
        })
        {
            try
            {
                using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                if (key == null) continue;
                if (key.GetValue("SQLEXPRESS") != null || key.GetValue("MSSQLSERVER") != null) return true;
            }
            catch { }
        }
        return false;
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

    private string? FindLocalMsi()
    {
        if (!Directory.Exists(SoftwareFolder)) return null;
        var msis = Directory.EnumerateFiles(SoftwareFolder, "*.msi", SearchOption.TopDirectoryOnly).OrderBy(x => x).ToList();
        return msis.FirstOrDefault(x => Path.GetFileName(x).Contains("suvidha", StringComparison.OrdinalIgnoreCase))
            ?? msis.FirstOrDefault();
    }
    private string? FindLocalVcRedist() => Directory.Exists(SoftwareFolder) ? Directory.EnumerateFiles(SoftwareFolder, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault(x => { var n = Path.GetFileName(x); return n.Contains("vcredist", StringComparison.OrdinalIgnoreCase) || n.Contains("vc_redist", StringComparison.OrdinalIgnoreCase); }) : null;
    private string? FindLocalBackup() => Directory.Exists(SoftwareFolder) ? Directory.EnumerateFiles(SoftwareFolder, "*.*", SearchOption.TopDirectoryOnly).FirstOrDefault(x => x.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".backup", StringComparison.OrdinalIgnoreCase)) : null;

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
            BackColor = Color.FromArgb(6, 24, 50);
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(10);
            Margin = Padding.Empty;
        }
    }

    private sealed class FeatureCard : Panel
    {
        public Color Accent { get; set; } = Color.FromArgb(0, 166, 255);

        public FeatureCard()
        {
            BackColor = Color.FromArgb(5, 27, 55);
            BorderStyle = BorderStyle.FixedSingle;
            DoubleBuffered = true;
        }
    }

    private sealed class StepItem : Panel
    {
        private readonly Label number, title, sub;
        private bool active, done, compact;

        public bool Active { get => active; set { active = value; Invalidate(); } }
        public bool Done
        {
            get => done;
            set { done = value; number.Text = done ? "✓" : number.Tag?.ToString() ?? ""; Invalidate(); }
        }

        public bool Compact
        {
            get => compact;
            set
            {
                compact = value;
                title.Visible = !value;
                sub.Visible = !value;
                number.Size = value ? new Size(42, 42) : new Size(38, 38);
                number.Location = value ? new Point((Width - 42) / 2, 10) : new Point(12, 15);
                Invalidate();
            }
        }

        public StepItem(int n, string name, string subtitle)
        {
            DoubleBuffered = true;
            number = new Label
            {
                Text = n.ToString(),
                Tag = n.ToString(),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(38, 38),
                Location = new Point(12, 15),
                Font = new Font("Segoe UI Semibold", 13F),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            title = new Label
            {
                Text = name,
                AutoSize = false,
                Font = new Font("Segoe UI Semibold", 9.5F),
                ForeColor = Color.White,
                Location = new Point(60, 11),
                Width = 135,
                Height = 22,
                AutoEllipsis = true
            };
            sub = new Label
            {
                Text = subtitle,
                AutoSize = false,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = MutedStatic,
                Location = new Point(60, 34),
                Width = 135,
                Height = 18,
                AutoEllipsis = true
            };
            Controls.Add(number);
            Controls.Add(title);
            Controls.Add(sub);

            Resize += (_, _) =>
            {
                if (!compact)
                {
                    title.Width = Math.Max(70, Width - 68);
                    sub.Width = Math.Max(70, Width - 68);
                }
            };
        }

        private static Color MutedStatic => Color.FromArgb(154, 177, 202);

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new SolidBrush(
                active ? Color.FromArgb(25, 23, 100, 170) : Color.FromArgb(7, 23, 44));
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = new Pen(active ? Color.FromArgb(0, 180, 255) : Color.FromArgb(22, 60, 96));
            e.Graphics.DrawRectangle(p, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            var size = compact ? 42 : 38;
            var x = compact ? Math.Max(0, (Width - size) / 2) : 12;
            var y = compact ? 10 : 15;
            using var c = new SolidBrush(done ? Color.FromArgb(29, 190, 110) : active ? Color.FromArgb(12, 119, 225) : Color.FromArgb(20, 40, 68));
            e.Graphics.FillEllipse(c, x, y, size, size);
            base.OnPaint(e);
        }
    }

    private sealed class HelpCard : Panel
    {
        private readonly Label icon, title, text, contact;
        private bool compact;

        public bool Compact
        {
            get => compact;
            set
            {
                compact = value;
                title.Visible = !value;
                text.Visible = !value;
                contact.Visible = !value;
                icon.Text = value ? "?" : "◉";
                icon.Font = new Font("Segoe UI Symbol", value ? 18F : 24F, FontStyle.Bold);
                icon.Dock = value ? DockStyle.Fill : DockStyle.Top;
                icon.Height = value ? 0 : 30;
                icon.TextAlign = value ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
            }
        }

        public HelpCard()
        {
            BackColor = Color.FromArgb(6, 24, 50);
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(10);
            DoubleBuffered = true;

            icon = new Label
            {
                Text = "◉",
                Font = new Font("Segoe UI Symbol", 24F),
                ForeColor = Color.FromArgb(72, 184, 255),
                AutoSize = false,
                Height = 30,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft
            };
            title = new Label
            {
                Text = "Need Help?",
                Font = new Font("Segoe UI Semibold", 12F),
                ForeColor = Color.FromArgb(0, 190, 255),
                AutoSize = false,
                Height = 22,
                Dock = DockStyle.Top
            };
            text = new Label
            {
                Text = "Support is available if you need help.",
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = TextColor,
                AutoSize = false,
                Height = 28,
                Dock = DockStyle.Top,
                AutoEllipsis = true
            };
            contact = new Label
            {
                Text = "+91 827171 8844",
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = Color.FromArgb(0, 190, 255),
                AutoSize = false,
                Height = 22,
                Dock = DockStyle.Top
            };

            Controls.Add(contact);
            Controls.Add(text);
            Controls.Add(title);
            Controls.Add(icon);
        }
    }

    private sealed class ComponentSelectCard : RoundedCard
    {
        public CheckBox Check { get; }

        public ComponentSelectCard(ComponentItem c)
        {
            Padding = new Padding(14, 10, 14, 10);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            var icon = new Label
            {
                Text = c.Kind == ComponentKind.Msi ? "▣" : c.Kind == ComponentKind.Local ? "▰" : "▤",
                Font = new Font("Segoe UI Symbol", 17F),
                ForeColor = c.Kind == ComponentKind.Local ? Green : Blue,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(icon, 0, 0);
            layout.SetRowSpan(icon, 2);

            layout.Controls.Add(new Label
            {
                Text = c.Name,
                Font = new Font("Segoe UI Semibold", 9.5F),
                ForeColor = TextColor,
                Dock = DockStyle.Fill,
                AutoEllipsis = true
            }, 1, 0);
            layout.Controls.Add(new Label
            {
                Text = c.Description,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Muted,
                Dock = DockStyle.Fill,
                AutoEllipsis = true
            }, 1, 1);

            Check = new CheckBox
            {
                Checked = c.Selected,
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 5, 2, 2)
            };
            layout.Controls.Add(Check, 2, 0);
            layout.SetRowSpan(Check, 2);
            Check.CheckedChanged += (_, _) => c.Selected = Check.Checked;
        }
    }



}
