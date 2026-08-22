using System.Drawing;
using System.Windows.Forms;

namespace SuvidhaPOSInstaller;

public sealed class MainForm : Form
{
    private readonly Color _bg = Color.FromArgb(7, 15, 28);
    private readonly Color _panel = Color.FromArgb(13, 25, 43);
    private readonly Color _panel2 = Color.FromArgb(18, 34, 56);
    private readonly Color _line = Color.FromArgb(37, 62, 91);
    private readonly Color _blue = Color.FromArgb(28, 132, 255);
    private readonly Color _green = Color.FromArgb(39, 196, 116);
    private readonly Color _text = Color.FromArgb(245, 248, 252);
    private readonly Color _muted = Color.FromArgb(164, 180, 199);

    private readonly string[] _titles = { "Welcome", "Terms", "Components", "Download", "Install", "Database", "Finish" };
    private readonly string[] _subtitles = { "Installation overview", "Review the agreement", "Choose what to install", "Prepare required files", "Install selected components", "Configure the database", "Installation complete" };
    private readonly Button[] _stepButtons = new Button[7];
    private readonly FlowLayoutPanel _stepList = new();
    private readonly Label _brandName = new();
    private readonly TableLayoutPanel _root = new();
    private readonly Panel _contentHost = new();
    private readonly Label _pageTitle = new();
    private readonly Label _pageSubtitle = new();
    private readonly Label _status = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _back = new();
    private readonly Button _next = new();
    private readonly CheckBox _acceptTerms = new();
    private readonly List<CheckBox> _components = new();
    private int _step;

    public MainForm()
    {
        Text = "Suvidha POS Installer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1180, 760);
        BackColor = _bg;
        ForeColor = _text;
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
        _root.Dock = DockStyle.Fill;
        _root.BackColor = _bg;
        _root.Margin = Padding.Empty;
        _root.Padding = Padding.Empty;
        _root.ColumnCount = 2;
        _root.RowCount = 3;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 218));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        Controls.Add(_root);

        var header = BuildHeader();
        _root.Controls.Add(header, 0, 0);
        _root.SetColumnSpan(header, 2);

        var sidebar = BuildSidebar();
        _root.Controls.Add(sidebar, 0, 1);

        _contentHost.Dock = DockStyle.Fill;
        _contentHost.BackColor = _bg;
        _contentHost.Padding = new Padding(24, 18, 24, 14);
        _contentHost.Margin = Padding.Empty;
        _root.Controls.Add(_contentHost, 1, 1);

        var footer = BuildFooter();
        _root.Controls.Add(footer, 0, 2);
        _root.SetColumnSpan(footer, 2);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _panel,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(20, 10, 20, 10),
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        var brand = new Label
        {
            Text = "S",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = _blue,
            Margin = Padding.Empty
        };
        header.Controls.Add(brand, 0, 0);

        _brandName.Text = "Suvidha POS";
        _brandName.Dock = DockStyle.Fill;
        _brandName.TextAlign = ContentAlignment.MiddleLeft;
        _brandName.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        _brandName.ForeColor = _text;
        _brandName.AutoEllipsis = true;
        _brandName.Padding = new Padding(14, 0, 8, 0);
        header.Controls.Add(_brandName, 1, 0);

        var version = new Label
        {
            Text = "v3.0.0",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 190, 255)
        };
        header.Controls.Add(version, 2, 0);
        return header;
    }

    private Control BuildSidebar()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 20, 34), Padding = new Padding(12, 14, 12, 10), Margin = Padding.Empty };
        var caption = new Label
        {
            Text = "INSTALLATION",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = _muted,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Padding = new Padding(4, 0, 0, 0)
        };
        panel.Controls.Add(caption);

        _stepList.Dock = DockStyle.Fill;
        _stepList.FlowDirection = FlowDirection.TopDown;
        _stepList.WrapContents = false;
        _stepList.AutoScroll = false;
        _stepList.BackColor = Color.Transparent;
        _stepList.Padding = Padding.Empty;
        _stepList.Margin = Padding.Empty;
        panel.Controls.Add(_stepList);

        for (int i = 0; i < 7; i++)
        {
            var index = i;
            var button = new Button
            {
                Text = $"{i + 1}   {_titles[i]}",
                Tag = index,
                Height = 50,
                Width = 194,
                Margin = new Padding(0, 0, 0, 7),
                Padding = new Padding(12, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = _panel,
                ForeColor = _muted,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = false
            };
            button.FlatAppearance.BorderColor = _line;
            button.FlatAppearance.BorderSize = 1;
            button.Click += (_, _) => ShowStep(index);
            _stepButtons[i] = button;
            _stepList.Controls.Add(button);
        }
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = _panel,
            Padding = new Padding(18, 10, 18, 10),
            Margin = Padding.Empty
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205));

        _status.Dock = DockStyle.Fill;
        _status.Text = "Step 1 of 7";
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.ForeColor = _muted;
        _status.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        footer.Controls.Add(_status, 0, 0);

        _progress.Dock = DockStyle.Fill;
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Value = 0;
        _progress.Margin = new Padding(0, 10, 16, 10);
        footer.Controls.Add(_progress, 1, 0);

        var nav = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = Padding.Empty, Padding = Padding.Empty };
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _back.Text = "← Back";
        _back.Dock = DockStyle.Fill;
        _back.Margin = new Padding(0, 0, 6, 0);
        _back.Click += (_, _) => ShowStep(_step - 1);
        StyleButton(_back, false);
        nav.Controls.Add(_back, 0, 0);

        _next.Text = "Next →";
        _next.Dock = DockStyle.Fill;
        _next.Click += (_, _) => NextStep();
        StyleButton(_next, true);
        nav.Controls.Add(_next, 1, 0);
        footer.Controls.Add(nav, 2, 0);
        return footer;
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index > 6) return;
        if (index > 1 && !_acceptTerms.Checked) return;

        _step = index;
        _pageTitle.Text = _titles[index];
        _pageSubtitle.Text = _subtitles[index];
        _status.Text = $"Step {index + 1} of 7";
        _progress.Value = index * 100 / 6;
        _back.Enabled = index > 0;
        _next.Enabled = index != 1 || _acceptTerms.Checked;
        _next.Text = index == 6 ? "Close ✓" : "Next →";

        for (int i = 0; i < _stepButtons.Length; i++)
        {
            var active = i == index;
            _stepButtons[i].BackColor = active ? _blue : _panel;
            _stepButtons[i].ForeColor = active ? Color.White : _muted;
            _stepButtons[i].FlatAppearance.BorderColor = active ? _blue : _line;
        }

        _contentHost.Controls.Clear();
        var page = BuildPage(index);
        _contentHost.Controls.Add(page);
        page.BringToFront();
        ApplyPageWidth();
    }

    private Control BuildPage(int index)
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = _bg, Padding = Padding.Empty, Margin = Padding.Empty };

        var pageHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 82,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        pageHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        pageHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _pageTitle.Dock = DockStyle.Fill;
        _pageTitle.TextAlign = ContentAlignment.MiddleLeft;
        _pageTitle.ForeColor = _text;
        _pageTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        _pageTitle.AutoEllipsis = true;
        _pageTitle.Margin = Padding.Empty;
        pageHeader.Controls.Add(_pageTitle, 0, 0);

        _pageSubtitle.Dock = DockStyle.Fill;
        _pageSubtitle.TextAlign = ContentAlignment.MiddleLeft;
        _pageSubtitle.ForeColor = _muted;
        _pageSubtitle.Font = new Font("Segoe UI", 10F);
        _pageSubtitle.Margin = Padding.Empty;
        pageHeader.Controls.Add(_pageSubtitle, 0, 1);
        outer.Controls.Add(pageHeader);

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = _bg,
            Padding = new Padding(0, 4, 10, 8),
            Margin = Padding.Empty
        };
        body.HorizontalScroll.Enabled = false;
        body.HorizontalScroll.Visible = false;
        outer.Controls.Add(body);

        Control content = index switch
        {
            0 => WelcomePage(),
            1 => TermsPage(),
            2 => ComponentsPage(),
            3 => DownloadPage(),
            4 => InstallPage(),
            5 => DatabasePage(),
            _ => FinishPage()
        };
        body.Controls.Add(content);
        body.Resize += (_, _) => ResizePageContent(body);
        ResizePageContent(body);
        return outer;
    }

    private void ResizePageContent(FlowLayoutPanel body)
    {
        var width = Math.Max(360, body.ClientSize.Width - body.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);
        foreach (Control child in body.Controls) child.Width = width;
    }

    private void ApplyPageWidth()
    {
        if (_contentHost.Controls.Count == 0) return;
        if (_contentHost.Controls[0] is Panel outer)
        {
            foreach (Control c in outer.Controls)
            {
                if (c is FlowLayoutPanel body) ResizePageContent(body);
            }
        }
    }

    private void ApplyResponsiveLayout()
    {
        var compact = ClientSize.Width < 1040;
        var sidebarWidth = compact ? 176 : 218;
        _root.ColumnStyles[0].Width = sidebarWidth;
        _stepList.SuspendLayout();
        for (int i = 0; i < _stepButtons.Length; i++)
        {
            _stepButtons[i].Width = Math.Max(138, sidebarWidth - 24);
            _stepButtons[i].Height = compact ? 46 : 50;
            _stepButtons[i].Font = new Font("Segoe UI", compact ? 9.5F : 10.5F, FontStyle.Bold);
            _stepButtons[i].Text = compact ? $"{i + 1}  {_titles[i]}" : $"{i + 1}   {_titles[i]}";
        }
        _stepList.ResumeLayout(true);
        _brandName.Font = new Font("Segoe UI", compact ? 18F : 22F, FontStyle.Bold);
        _contentHost.Padding = compact ? new Padding(18, 14, 18, 10) : new Padding(24, 18, 24, 14);
        ApplyPageWidth();
    }

    private Control WelcomePage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Welcome to Suvidha POS", "A clean, guided installer designed to remain readable on different Windows screen sizes."));
        layout.Controls.Add(Card("Installation plan", "1. Review terms\n2. Select components\n3. Prepare downloads\n4. Install components\n5. Configure database\n6. Finish setup"));
        layout.Controls.Add(Card("System requirements", "Windows 10/11 64-bit\n4 GB RAM or more\n10 GB free disk space\nAdministrator privileges\nInternet connection for downloads"));
        return layout;
    }

    private Control TermsPage()
    {
        var layout = Stack();
        var card = new Panel { BackColor = _panel, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(14), Height = 330, Margin = new Padding(0, 0, 0, 12) };
        var terms = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(8, 18, 31),
            ForeColor = _text,
            Font = new Font("Segoe UI", 10.5F),
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = true,
            Multiline = true
        };
        terms.Text = "SUVIDHA POS SOFTWARE LICENSE AGREEMENT\n\nThe software is licensed, not sold. You may use it only for lawful purposes and in accordance with the installation configuration you select.\n\nYou may not modify, distribute, sell, lease, or reverse engineer any protected part of the software without written permission.\n\nInstallation may download third-party components. Their respective licenses and terms remain applicable.\n\nData and configuration are stored locally unless the application configuration specifies otherwise.\n\nTHE SOFTWARE IS PROVIDED AS-IS. USE OF THE SOFTWARE IS AT YOUR OWN RISK.";
        card.Controls.Add(terms);
        layout.Controls.Add(card);

        _acceptTerms.Text = "I have read and accept the terms and conditions";
        _acceptTerms.AutoSize = true;
        _acceptTerms.ForeColor = _text;
        _acceptTerms.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _acceptTerms.Margin = new Padding(2, 4, 0, 12);
        _acceptTerms.CheckedChanged -= AcceptTermsChanged;
        _acceptTerms.CheckedChanged += AcceptTermsChanged;
        layout.Controls.Add(_acceptTerms);
        return layout;
    }

    private void AcceptTermsChanged(object? sender, EventArgs e) => _next.Enabled = _step != 1 || _acceptTerms.Checked;

    private Control ComponentsPage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Select components", "Choose the components you want this installation to prepare. You can change these selections before starting the install."));
        _components.Clear();
        string[] items = { "Suvidha POS Application", "SQL Server 2019", "SQL Server Management Studio", "Crystal Reports Runtime", "Database Backup / Restore Tools" };
        foreach (var item in items)
        {
            var cb = new CheckBox { Text = item, Checked = true, AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = new Padding(0, 6, 0, 6) };
            _components.Add(cb);
            layout.Controls.Add(cb);
        }
        return layout;
    }

    private Control DownloadPage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Download preparation", "The installer will use the selected components and download their required files before installation begins."));
        layout.Controls.Add(Card("Download status", "Ready to download selected components.\n\nNo files are downloaded until you continue."));
        return layout;
    }

    private Control InstallPage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Installation", "Selected components will be installed in the configured destination. Progress and status are shown here."));
        layout.Controls.Add(Card("Installation directory", "D:\\Suvidha Pos\\Software"));
        layout.Controls.Add(Card("Current status", "Waiting for installation to start. Live installer data can be displayed here without changing the layout."));
        return layout;
    }

    private Control DatabasePage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Database configuration", "Enter or connect to the SQL Server instance used by Suvidha POS."));
        var card = new Panel { BackColor = _panel, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(16), Height = 220, Margin = new Padding(0, 0, 0, 12) };
        AddField(card, "Server / Instance", "localhost\\SQLEXPRESS", 0);
        AddField(card, "Database", "SuvidhaPOS", 1);
        AddField(card, "Authentication", "Windows Authentication", 2);
        layout.Controls.Add(card);
        return layout;
    }

    private Control FinishPage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Installation complete", "Suvidha POS has completed the selected installation workflow."));
        layout.Controls.Add(Card("Next steps", "Launch Suvidha POS from the desktop or Start menu.\nUse Database Backup / Restore when moving or recovering data."));
        return layout;
    }

    private FlowLayoutPanel Stack() => new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Color.Transparent,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private Panel Card(string title, string text)
    {
        var card = new Panel { BackColor = _panel, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(16), Height = 112, Margin = new Padding(0, 0, 0, 12) };
        var heading = new Label { Text = title, Dock = DockStyle.Top, Height = 30, ForeColor = _text, Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoEllipsis = true };
        var body = new Label { Text = text, Dock = DockStyle.Fill, ForeColor = _muted, Font = new Font("Segoe UI", 9.5F), AutoEllipsis = false, Padding = new Padding(0, 2, 0, 0) };
        card.Controls.Add(body);
        card.Controls.Add(heading);
        return card;
    }

    private void AddField(Control parent, string label, string value, int index)
    {
        var y = 12 + index * 64;
        var caption = new Label { Text = label, Left = 12, Top = y, Width = 170, Height = 24, ForeColor = _muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var box = new TextBox { Text = value, Left = 185, Top = y - 2, Width = 420, Height = 28, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(8, 18, 31), ForeColor = _text, BorderStyle = BorderStyle.FixedSingle };
        parent.Controls.Add(caption);
        parent.Controls.Add(box);
        parent.Resize += (_, _) => box.Width = Math.Max(220, parent.ClientSize.Width - 215 - parent.Padding.Horizontal);
    }

    private void NextStep()
    {
        if (_step == 1 && !_acceptTerms.Checked) return;
        if (_step < 6) ShowStep(_step + 1);
        else Close();
    }

    private void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? _blue : _line;
        button.BackColor = primary ? _blue : _panel;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }
}
