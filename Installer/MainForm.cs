using System.Drawing;
using System.Windows.Forms;

namespace SuvidhaPOSInstaller;

public sealed class MainForm : Form
{
    private static readonly Color Bg = Color.FromArgb(7, 15, 28);
    private static readonly Color HeaderBg = Color.FromArgb(13, 25, 43);
    private static readonly Color SidebarBg = Color.FromArgb(10, 20, 34);
    private static readonly Color CardBg = Color.FromArgb(15, 29, 48);
    private static readonly Color InputBg = Color.FromArgb(8, 18, 31);
    private static readonly Color Line = Color.FromArgb(42, 65, 92);
    private static readonly Color Blue = Color.FromArgb(28, 132, 255);
    private static readonly Color Green = Color.FromArgb(38, 190, 82);
    private static readonly Color Text = Color.FromArgb(245, 248, 252);
    private static readonly Color Muted = Color.FromArgb(166, 182, 201);

    private readonly string[] titles = { "Welcome", "Terms", "Components", "Download", "Install", "Database", "Finish" };
    private readonly string[] subtitles =
    {
        "Installation overview", "Review the agreement", "Choose what to install",
        "Prepare required files", "Install selected components", "Configure the database", "Installation complete"
    };

    private readonly TableLayoutPanel root = new();
    private readonly Panel contentHost = new();
    private readonly Button[] stepButtons = new Button[7];
    private readonly Label pageTitle = new();
    private readonly Label pageSubtitle = new();
    private readonly Label status = new();
    private readonly ProgressBar progress = new();
    private readonly Button back = new();
    private readonly Button next = new();
    private readonly CheckBox acceptTerms = new();
    private readonly List<CheckBox> components = new();
    private int step;

    public MainForm()
    {
        Text = "Suvidha POS Installer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(920, 620);
        Size = new Size(1240, 780);
        BackColor = Bg;
        ForeColor = Text;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        BuildShell();
        ShowStep(0);
        Resize += (_, _) => ApplyResponsiveLayout();
        ApplyResponsiveLayout();
    }

    private void BuildShell()
    {
        root.Dock = DockStyle.Fill;
        root.Margin = Padding.Empty;
        root.Padding = Padding.Empty;
        root.BackColor = Bg;
        root.ColumnCount = 2;
        root.RowCount = 3;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        Controls.Add(root);

        var header = BuildHeader();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);

        root.Controls.Add(BuildSidebar(), 0, 1);

        contentHost.Dock = DockStyle.Fill;
        contentHost.BackColor = Bg;
        contentHost.Padding = new Padding(26, 18, 26, 14);
        contentHost.Margin = Padding.Empty;
        root.Controls.Add(contentHost, 1, 1);

        var footer = BuildFooter();
        root.Controls.Add(footer, 0, 2);
        root.SetColumnSpan(footer, 2);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = HeaderBg,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(18, 10, 18, 10),
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));

        var logo = new Label
        {
            Text = "S",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Blue,
            Margin = Padding.Empty
        };
        header.Controls.Add(logo, 0, 0);

        var brand = new Label
        {
            Text = "Suvidha POS",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Text,
            Padding = new Padding(14, 0, 8, 0),
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        header.Controls.Add(brand, 1, 0);

        var version = new Label
        {
            Text = "v3.0.0",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(105, 190, 255),
            Margin = Padding.Empty
        };
        header.Controls.Add(version, 2, 0);
        return header;
    }

    private Control BuildSidebar()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SidebarBg,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12, 14, 12, 12),
            Margin = Padding.Empty
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var caption = new Label
        {
            Text = "INSTALLATION",
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };
        panel.Controls.Add(caption, 0, 0);

        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 7,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        for (int i = 0; i < 7; i++)
        {
            list.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            var index = i;
            var b = new Button
            {
                Text = $"{i + 1}   {titles[i]}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 8, 0),
                Margin = new Padding(0, 0, 0, 6),
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = CardBg,
                ForeColor = Muted,
                Cursor = Cursors.Hand,
                Tag = index
            };
            b.FlatAppearance.BorderColor = Line;
            b.FlatAppearance.BorderSize = 1;
            b.Click += (_, _) => ShowStep(index);
            stepButtons[i] = b;
            list.Controls.Add(b, 0, i);
        }
        panel.Controls.Add(list, 0, 1);
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = HeaderBg,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(16, 10, 16, 10),
            Margin = Padding.Empty
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));

        status.Text = "Step 1 of 7";
        status.Dock = DockStyle.Fill;
        status.TextAlign = ContentAlignment.MiddleLeft;
        status.ForeColor = Muted;
        status.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        status.Margin = Padding.Empty;
        footer.Controls.Add(status, 0, 0);

        progress.Dock = DockStyle.Fill;
        progress.Minimum = 0;
        progress.Maximum = 100;
        progress.Value = 0;
        progress.Style = ProgressBarStyle.Continuous;
        progress.Margin = new Padding(0, 12, 16, 12);
        footer.Controls.Add(progress, 1, 0);

        var nav = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        back.Text = "← Back";
        back.Dock = DockStyle.Fill;
        back.Margin = new Padding(0, 0, 8, 0);
        back.Click += (_, _) => ShowStep(step - 1);
        StyleButton(back, false);
        nav.Controls.Add(back, 0, 0);

        next.Text = "Next →";
        next.Dock = DockStyle.Fill;
        next.Margin = Padding.Empty;
        next.Click += (_, _) => NextStep();
        StyleButton(next, true);
        nav.Controls.Add(next, 1, 0);

        footer.Controls.Add(nav, 2, 0);
        return footer;
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index > 6) return;
        if (index > 1 && !acceptTerms.Checked) return;

        step = index;
        status.Text = $"Step {index + 1} of 7";
        progress.Value = index == 6 ? 100 : index * 100 / 6;
        back.Enabled = index > 0;
        next.Enabled = index != 1 || acceptTerms.Checked;
        next.Text = index == 6 ? "Close ✓" : "Next →";

        for (int i = 0; i < stepButtons.Length; i++)
        {
            bool active = i == index;
            stepButtons[i].BackColor = active ? Blue : CardBg;
            stepButtons[i].ForeColor = active ? Color.White : Muted;
            stepButtons[i].FlatAppearance.BorderColor = active ? Blue : Line;
        }

        contentHost.SuspendLayout();
        contentHost.Controls.Clear();
        var page = BuildPage(index);
        contentHost.Controls.Add(page);
        contentHost.ResumeLayout(true);
    }

    private Control BuildPage(int index)
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        pageTitle.Text = titles[index];
        pageTitle.Dock = DockStyle.Fill;
        pageTitle.TextAlign = ContentAlignment.BottomLeft;
        pageTitle.ForeColor = Text;
        pageTitle.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
        pageTitle.AutoEllipsis = true;
        pageTitle.Margin = Padding.Empty;
        pageSubtitle.Text = subtitles[index];
        pageSubtitle.Dock = DockStyle.Fill;
        pageSubtitle.TextAlign = ContentAlignment.TopLeft;
        pageSubtitle.ForeColor = Muted;
        pageSubtitle.Font = new Font("Segoe UI", 10.5F);
        pageSubtitle.Margin = Padding.Empty;

        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.Transparent };
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        heading.Controls.Add(pageTitle, 0, 0);
        heading.Controls.Add(pageSubtitle, 0, 1);
        page.Controls.Add(heading, 0, 0);

        var body = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Margin = Padding.Empty, Padding = Padding.Empty, AutoScroll = true };
        page.Controls.Add(body, 0, 1);
        body.Controls.Add(BuildBody(index));
        return page;
    }

    private Control BuildBody(int index)
    {
        return index switch
        {
            0 => WelcomePage(),
            1 => TermsPage(),
            2 => ComponentsPage(),
            3 => DownloadPage(),
            4 => InstallPage(),
            5 => DatabasePage(),
            _ => FinishPage()
        };
    }

    private Control WelcomePage()
    {
        var grid = BodyGrid(3);
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
        grid.Controls.Add(Card("Welcome to Suvidha POS", "A clean, guided installer that scales with the Windows window size and keeps every field readable."), 0, 0);
        grid.Controls.Add(Card("Installation plan", "Review terms  •  Select components  •  Prepare downloads  •  Install  •  Configure database  •  Finish"), 0, 1);
        grid.Controls.Add(Card("System requirements", "Windows 10/11 64-bit  •  4 GB RAM or more  •  10 GB free disk space  •  Administrator permission when required"), 0, 2);
        return grid;
    }

    private Control TermsPage()
    {
        var grid = BodyGrid(2);
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var card = new Panel { Dock = DockStyle.Fill, BackColor = CardBg, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 10) };
        var terms = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = InputBg,
            ForeColor = Text,
            Font = new Font("Segoe UI", 10.5F),
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = true,
            Multiline = true,
            Text = "SUVIDHA POS SOFTWARE LICENSE AGREEMENT\n\nThe software is licensed, not sold. You may use it only for lawful purposes and in accordance with the installation configuration you select.\n\nYou may not modify, distribute, sell, lease, or reverse engineer any protected part of the software without written permission.\n\nInstallation may download third-party components. Their respective licenses and terms remain applicable.\n\nData and configuration are stored locally unless the application configuration specifies otherwise.\n\nTHE SOFTWARE IS PROVIDED AS-IS. USE OF THE SOFTWARE IS AT YOUR OWN RISK."
        };
        card.Controls.Add(terms);
        grid.Controls.Add(card, 0, 0);

        acceptTerms.Text = "I have read and accept the terms and conditions";
        acceptTerms.Dock = DockStyle.Fill;
        acceptTerms.ForeColor = Text;
        acceptTerms.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        acceptTerms.Margin = new Padding(2, 0, 0, 0);
        acceptTerms.CheckedChanged -= AcceptTermsChanged;
        acceptTerms.CheckedChanged += AcceptTermsChanged;
        grid.Controls.Add(acceptTerms, 0, 1);
        return grid;
    }

    private void AcceptTermsChanged(object? sender, EventArgs e)
    {
        next.Enabled = step != 1 || acceptTerms.Checked;
    }

    private Control ComponentsPage()
    {
        var grid = BodyGrid(2);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.Controls.Add(Card("Select components", "Choose the components this setup should prepare. Selections remain editable until installation starts."), 0, 0);

        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(4, 4, 4, 4)
        };
        string[] items = { "Suvidha POS Application", "SQL Server 2019", "SQL Server Management Studio", "Crystal Reports Runtime", "Database Backup / Restore Tools" };
        components.Clear();
        for (int i = 0; i < items.Length; i++)
        {
            list.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            var cb = new CheckBox { Text = items[i], Checked = true, Dock = DockStyle.Fill, ForeColor = Text, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), Margin = new Padding(0, 2, 0, 2), Padding = new Padding(6, 0, 0, 0) };
            components.Add(cb);
            list.Controls.Add(cb, 0, i);
        }
        grid.Controls.Add(list, 0, 1);
        return grid;
    }

    private Control DownloadPage()
    {
        var grid = BodyGrid(2);
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.Controls.Add(Card("Download preparation", "Required files for the selected components will be prepared here. The layout is ready for live download progress and status messages."), 0, 0);
        grid.Controls.Add(Card("Download status", "Ready to download selected components.\n\nNo files are downloaded until you continue."), 0, 1);
        return grid;
    }

    private Control InstallPage()
    {
        var grid = BodyGrid(3);
        for (int i = 0; i < 3; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        grid.Controls.Add(Card("Installation", "Selected components will be installed in the configured destination. Progress and status can be updated without changing the UI layout."), 0, 0);
        grid.Controls.Add(Card("Installation directory", "D:\\Suvidha Pos\\Software"), 0, 1);
        grid.Controls.Add(Card("Current status", "Waiting for installation to start."), 0, 2);
        return grid;
    }

    private Control DatabasePage()
    {
        var grid = BodyGrid(2);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.Controls.Add(Card("Database configuration", "Enter the SQL Server instance and database used by Suvidha POS."), 0, 0);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = CardBg,
            Padding = new Padding(18),
            Margin = Padding.Empty
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        AddField(fields, "Server / Instance", "localhost\\SQLEXPRESS", 0);
        AddField(fields, "Database", "SuvidhaPOS", 1);
        AddField(fields, "Authentication", "Windows Authentication", 2);
        grid.Controls.Add(fields, 0, 1);
        return grid;
    }

    private Control FinishPage()
    {
        var grid = BodyGrid(2);
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.Controls.Add(Card("Installation complete", "Suvidha POS has completed the selected installation workflow."), 0, 0);
        grid.Controls.Add(Card("Next steps", "Launch Suvidha POS from the desktop or Start menu. Use Database Backup / Restore when moving or recovering data."), 0, 1);
        return grid;
    }

    private TableLayoutPanel BodyGrid(int rows)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = rows,
            BackColor = Bg,
            Padding = new Padding(0, 0, 4, 4),
            Margin = Padding.Empty,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return grid;
    }

    private Panel Card(string title, string text)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 10)
        };
        var heading = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 34,
            ForeColor = Text,
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        var body = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 10F),
            AutoEllipsis = false,
            Padding = new Padding(0, 3, 0, 0),
            Margin = Padding.Empty
        };
        card.Controls.Add(body);
        card.Controls.Add(heading);
        return card;
    }

    private void AddField(TableLayoutPanel parent, string label, string value, int row)
    {
        var caption = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 12, 0)
        };
        var box = new TextBox
        {
            Text = value,
            Dock = DockStyle.Fill,
            BackColor = InputBg,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(0, 8, 0, 8)
        };
        parent.Controls.Add(caption, 0, row);
        parent.Controls.Add(box, 1, row);
    }

    private void NextStep()
    {
        if (step == 1 && !acceptTerms.Checked) return;
        if (step < 6) ShowStep(step + 1);
        else Close();
    }

    private void ApplyResponsiveLayout()
    {
        bool compact = ClientSize.Width < 1050;
        int sidebarWidth = compact ? 190 : 220;
        root.ColumnStyles[0].Width = sidebarWidth;
        contentHost.Padding = compact ? new Padding(18, 14, 18, 12) : new Padding(26, 18, 26, 14);

        foreach (var b in stepButtons)
        {
            b.Font = new Font("Segoe UI", compact ? 9.5F : 10.5F, FontStyle.Bold);
            b.Text = compact ? b.Text.Replace("   ", "  ") : b.Text.Replace("  ", "   ");
        }
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Blue : Line;
        button.BackColor = primary ? Blue : CardBg;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.UseCompatibleTextRendering = false;
    }
}
