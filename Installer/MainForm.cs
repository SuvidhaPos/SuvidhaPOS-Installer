using System.Drawing;
using System.Drawing.Drawing2D;
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
        MinimumSize = new Size(980, 650);
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
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = _bg, Margin = Padding.Empty, Padding = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 244));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        Controls.Add(root);

        var header = BuildHeader();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);

        var sidebar = BuildSidebar();
        root.Controls.Add(sidebar, 0, 1);

        _contentHost.Dock = DockStyle.Fill;
        _contentHost.BackColor = _bg;
        _contentHost.Padding = new Padding(26, 22, 26, 18);
        _contentHost.HorizontalScroll.Enabled = false;
        _contentHost.HorizontalScroll.Visible = false;
        _contentHost.VerticalScroll.Enabled = true;
        _contentHost.VerticalScroll.Visible = true;
        root.Controls.Add(_contentHost, 1, 1);

        var footer = BuildFooter();
        root.Controls.Add(footer, 0, 2);
        root.SetColumnSpan(footer, 2);
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = _panel, Padding = new Padding(28, 12, 28, 10) };
        var brand = new Label { Text = "S", Dock = DockStyle.Left, Width = 52, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.White, BackColor = _blue, Margin = new Padding(0, 4, 16, 4) };
        header.Controls.Add(brand);

        var name = new Label { Text = "Suvidha POS", Dock = DockStyle.Left, Width = 250, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = _text, AutoEllipsis = true };
        header.Controls.Add(name);

        var version = new Label { Text = "v3.0.0", Dock = DockStyle.Right, Width = 130, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(100, 190, 255) };
        header.Controls.Add(version);
        return header;
    }

    private Control BuildSidebar()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 20, 34), Padding = new Padding(16, 18, 16, 12) };
        var caption = new Label { Text = "INSTALLATION", Dock = DockStyle.Top, Height = 30, ForeColor = _muted, Font = new Font("Segoe UI", 9, FontStyle.Bold), Padding = new Padding(4, 0, 0, 0) };
        panel.Controls.Add(caption);

        var list = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false, BackColor = Color.Transparent, Padding = Padding.Empty, Margin = Padding.Empty };
        panel.Controls.Add(list);

        for (int i = 0; i < 7; i++)
        {
            var index = i;
            var b = new Button
            {
                Text = $"{i + 1}   {_titles[i]}",
                Tag = index,
                Width = 210,
                Height = 58,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(12, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = _panel,
                ForeColor = _muted,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = _line;
            b.FlatAppearance.BorderSize = 1;
            b.Click += (_, _) => ShowStep(index);
            _stepButtons[i] = b;
            list.Controls.Add(b);
        }
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = _panel, Padding = new Padding(24, 12, 24, 12) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));

        _status.Text = "Ready";
        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.ForeColor = _muted;
        _status.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        footer.Controls.Add(_status, 0, 0);

        _progress.Dock = DockStyle.Fill;
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Value = 0;
        _progress.Margin = new Padding(0, 10, 20, 10);
        footer.Controls.Add(_progress, 1, 0);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Color.Transparent, Padding = Padding.Empty };
        _next.Text = "Next  →";
        _next.Width = 112;
        _next.Height = 46;
        _next.Click += (_, _) => NextStep();
        StyleButton(_next, true);
        nav.Controls.Add(_next);

        _back.Text = "←  Back";
        _back.Width = 98;
        _back.Height = 46;
        _back.Margin = new Padding(0, 0, 8, 0);
        _back.Click += (_, _) => ShowStep(_step - 1);
        StyleButton(_back, false);
        nav.Controls.Add(_back);

        footer.Controls.Add(nav, 2, 0);
        return footer;
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index > 6) return;
        if (index == 1 && !_acceptTerms.Checked && _step > 1) return;

        _step = index;
        _pageTitle.Text = _titles[index];
        _pageSubtitle.Text = _subtitles[index];
        _status.Text = $"Step {index + 1} of 7";
        _progress.Value = index * 100 / 6;
        _back.Enabled = index > 0;
        _next.Enabled = index < 6;
        _next.Text = index == 6 ? "Close  ✓" : "Next  →";

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
    }

    private Control BuildPage(int index)
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = _bg, Padding = Padding.Empty };
        var top = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.Transparent };
        _pageTitle.Dock = DockStyle.Top;
        _pageTitle.Height = 42;
        _pageTitle.ForeColor = _text;
        _pageTitle.Font = new Font("Segoe UI", 24, FontStyle.Bold);
        _pageTitle.AutoEllipsis = true;
        _pageSubtitle.Dock = DockStyle.Top;
        _pageSubtitle.Height = 28;
        _pageSubtitle.ForeColor = _muted;
        _pageSubtitle.Font = new Font("Segoe UI", 10.5F);
        top.Controls.Add(_pageSubtitle);
        top.Controls.Add(_pageTitle);
        outer.Controls.Add(top);

        var body = new Panel { Dock = DockStyle.Fill, BackColor = _bg, AutoScroll = true, Padding = new Padding(0, 4, 12, 8) };
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
        content.Dock = DockStyle.Top;
        body.Controls.Add(content);
        return outer;
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
        var card = new Panel { BackColor = _panel, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18), Height = 360, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 14) };
        var terms = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(8, 18, 31), ForeColor = _text, Font = new Font("Segoe UI", 10.5F), DetectUrls = false, ScrollBars = RichTextBoxScrollBars.Vertical };
        terms.Text = "SUVIDHA POS SOFTWARE LICENSE AGREEMENT\n\nThe software is licensed, not sold. You may use it only for lawful purposes and in accordance with the installation configuration you select.\n\nYou may not modify, distribute, sell, lease, or reverse engineer any protected part of the software without written permission.\n\nInstallation may download third-party components. Their respective licenses and terms remain applicable.\n\nData and configuration are stored locally unless the application configuration specifies otherwise.\n\nTHE SOFTWARE IS PROVIDED AS-IS. USE OF THE SOFTWARE IS AT YOUR OWN RISK.";
        card.Controls.Add(terms);
        layout.Controls.Add(card);
        _acceptTerms.Text = "I have read and accept the terms and conditions";
        _acceptTerms.AutoSize = true;
        _acceptTerms.ForeColor = _text;
        _acceptTerms.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        _acceptTerms.Margin = new Padding(2, 8, 0, 14);
        _acceptTerms.CheckedChanged += (_, _) => _next.Enabled = _acceptTerms.Checked;
        layout.Controls.Add(_acceptTerms);
        return layout;
    }

    private Control ComponentsPage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Select components", "Choose the components you want this installation to prepare. You can change these selections before starting the install."));
        _components.Clear();
        string[] items = { "Suvidha POS Application", "SQL Server 2019", "SQL Server Management Studio", "Crystal Reports Runtime", "Database Backup / Restore Tools" };
        foreach (var item in items)
        {
            var cb = new CheckBox { Text = item, Checked = true, AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), Margin = new Padding(0, 7, 0, 7) };
            _components.Add(cb);
            layout.Controls.Add(cb);
        }
        return layout;
    }

    private Control DownloadPage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Download preparation", "The installer will use the selected components and download their required files before installation begins."));
        var status = Card("Download status", "Ready to download selected components.\n\nNo files are downloaded until you continue.");
        layout.Controls.Add(status);
        return layout;
    }

    private Control InstallPage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Installation", "Selected components will be installed in the configured destination. Progress and status are shown here."));
        layout.Controls.Add(Card("Installation directory", "D:\\Suvidha Pos\\Software"));
        layout.Controls.Add(Card("Current status", "Waiting for installation to start. This screen is designed so status text can be updated with live installer data without changing the layout."));
        return layout;
    }

    private Control DatabasePage()
    {
        var layout = Stack();
        layout.Controls.Add(Card("Database configuration", "Enter or connect to the SQL Server instance used by Suvidha POS."));
        var card = new Panel { BackColor = _panel, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18), Height = 230, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 14) };
        AddField(card, "Server / Instance", "localhost\\SQLEXPRESS", 0);
        AddField(card, "Database", "SuvidhaPOS", 1);
        AddField(card, "Authentication", "Windows Authentication", 2);
        card.Resize += (_, _) => { };
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

    private Panel Stack()
    {
        return new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = _bg, Padding = new Padding(0, 0, 4, 12) };
    }

    private Panel Card(string title, string text)
    {
        var card = new Panel { BackColor = _panel, BorderStyle = BorderStyle.FixedSingle, Height = 150, Dock = DockStyle.Top, Padding = new Padding(20), Margin = new Padding(0, 0, 0, 14) };
        var heading = new Label { Text = title, Dock = DockStyle.Top, Height = 40, ForeColor = _text, Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoEllipsis = true };
        var body = new Label { Text = text, Dock = DockStyle.Fill, ForeColor = _muted, Font = new Font("Segoe UI", 10.5F), AutoEllipsis = false };
        card.Controls.Add(body);
        card.Controls.Add(heading);
        return card;
    }

    private void AddField(Control parent, string label, string value, int row)
    {
        var y = 16 + row * 62;
        var l = new Label { Text = label, ForeColor = _muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(18, y), Size = new Size(180, 24) };
        var t = new TextBox { Text = value, BackColor = Color.FromArgb(8, 18, 31), ForeColor = _text, BorderStyle = BorderStyle.FixedSingle, Location = new Point(205, y - 2), Width = 470, Height = 30, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        parent.Controls.Add(l);
        parent.Controls.Add(t);
    }

    private void NextStep()
    {
        if (_step == 0) { ShowStep(1); return; }
        if (_step == 1 && !_acceptTerms.Checked) { MessageBox.Show("Please accept the terms before continuing.", "Suvidha POS Installer", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (_step < 6) ShowStep(_step + 1);
        else Close();
    }

    private void StyleButton(Button b, bool primary)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = primary ? _blue : _line;
        b.BackColor = primary ? _blue : _panel2;
        b.ForeColor = Color.White;
        b.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        b.Cursor = Cursors.Hand;
    }

    private void ApplyResponsiveLayout()
    {
        if (Controls.Count == 0) return;
        var root = Controls[0] as TableLayoutPanel;
        if (root == null) return;
        var compact = ClientSize.Width < 1080;
        root.ColumnStyles[0].Width = compact ? 92 : 244;
        var sidebar = root.GetControlFromPosition(0, 1) as Panel;
        if (sidebar != null)
        {
            var list = sidebar.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
            if (list != null)
            {
                foreach (var control in list.Controls.OfType<Button>())
                {
                    control.Width = compact ? 58 : Math.Max(70, sidebar.ClientSize.Width - 32);
                    control.Text = compact ? ((int)control.Tag + 1).ToString() : $"{(int)control.Tag + 1}   {_titles[(int)control.Tag]}";
                    control.TextAlign = compact ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
                    control.Padding = compact ? Padding.Empty : new Padding(12, 0, 8, 0);
                }
            }
            var caption = sidebar.Controls.OfType<Label>().FirstOrDefault();
            if (caption != null) caption.Text = compact ? "STEPS" : "INSTALLATION";
        }
        _contentHost.Padding = compact ? new Padding(18, 16, 16, 14) : new Padding(28, 22, 26, 18);
    }
}
