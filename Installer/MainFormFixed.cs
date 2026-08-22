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
    private static readonly Color Border = Color.FromArgb(42, 65, 92);
    private static readonly Color Blue = Color.FromArgb(28, 132, 255);
    private static readonly Color Green = Color.FromArgb(38, 190, 82);
    private static readonly Color TextColor = Color.FromArgb(245, 248, 252);
    private static readonly Color Muted = Color.FromArgb(166, 182, 201);

    private readonly string[] titles = { "Welcome", "Terms", "Components", "Download", "Install", "Database", "Finish" };
    private readonly string[] subtitles = { "Installation overview", "Review the agreement", "Choose what to install", "Prepare required files", "Install selected components", "Configure the database", "Installation complete" };
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
    private TextBox server = new();
    private TextBox database = new();
    private ComboBox authentication = new();
    private int step;

    public MainForm()
    {
        Text = "Suvidha POS Installer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1200, 760);
        BackColor = Bg;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;
        BuildShell();
        ShowStep(0);
    }

    private void BuildShell()
    {
        root.Dock = DockStyle.Fill;
        root.Margin = Padding.Empty;
        root.Padding = Padding.Empty;
        root.BackColor = Bg;
        root.ColumnCount = 2;
        root.RowCount = 3;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        Controls.Add(root);

        var header = BuildHeader();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);
        root.Controls.Add(BuildSidebar(), 0, 1);

        contentHost.Dock = DockStyle.Fill;
        contentHost.BackColor = Bg;
        contentHost.Padding = new Padding(28, 20, 28, 16);
        root.Controls.Add(contentHost, 1, 1);

        var footer = BuildFooter();
        root.Controls.Add(footer, 0, 2);
        root.SetColumnSpan(footer, 2);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = HeaderBg, ColumnCount = 3, RowCount = 1, Padding = new Padding(18, 10, 18, 10), Margin = Padding.Empty };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        header.Controls.Add(new Label { Text = "S", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = Color.White, BackColor = Blue, Margin = Padding.Empty }, 0, 0);
        header.Controls.Add(new Label { Text = "Suvidha POS", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 21F, FontStyle.Bold), ForeColor = TextColor, Padding = new Padding(14, 0, 8, 0), AutoEllipsis = true, Margin = Padding.Empty }, 1, 0);
        header.Controls.Add(new Label { Text = "v3.0.0", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(105, 190, 255), Margin = Padding.Empty }, 2, 0);
        return header;
    }

    private Control BuildSidebar()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = SidebarBg, ColumnCount = 1, RowCount = 2, Padding = new Padding(14, 14, 14, 12), Margin = Padding.Empty };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = "INSTALLATION", Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty }, 0, 0);

        var list = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 7, AutoSize = true, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.Transparent };
        for (int i = 0; i < 7; i++)
        {
            list.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            int index = i;
            var b = new Button { Text = $"{i + 1}   {titles[i]}", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 8, 0), Margin = new Padding(0, 0, 0, 6), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, BackColor = CardBg, ForeColor = Muted, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Border;
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
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = HeaderBg, ColumnCount = 3, RowCount = 1, Padding = new Padding(16, 10, 16, 10), Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        status.Text = "Step 1 of 7";
        status.Dock = DockStyle.Fill;
        status.TextAlign = ContentAlignment.MiddleLeft;
        status.ForeColor = Muted;
        status.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        footer.Controls.Add(status, 0, 0);
        progress.Dock = DockStyle.Fill;
        progress.Minimum = 0;
        progress.Maximum = 100;
        progress.Value = 0;
        progress.Margin = new Padding(0, 12, 16, 12);
        footer.Controls.Add(progress, 1, 0);

        var nav = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
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
        next.Click += (_, _) => NextStep();
        StyleButton(next, true);
        nav.Controls.Add(next, 1, 0);
        footer.Controls.Add(nav, 2, 0);
        return footer;
    }

    private void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = primary ? Blue : Border;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = primary ? Blue : CardBg;
        button.ForeColor = TextColor;
        button.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.Margin = Padding.Empty;
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
        for (int i = 0; i < 7; i++)
        {
            bool active = i == index;
            stepButtons[i].BackColor = active ? Blue : CardBg;
            stepButtons[i].ForeColor = active ? Color.White : Muted;
            stepButtons[i].FlatAppearance.BorderColor = active ? Blue : Border;
        }
        contentHost.Controls.Clear();
        contentHost.Controls.Add(BuildPage(index));
    }

    private void NextStep()
    {
        if (step == 6) { Close(); return; }
        if (step == 1 && !acceptTerms.Checked) return;
        ShowStep(step + 1);
    }

    private Control BuildPage(int index)
    {
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Bg, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        pageTitle.Text = titles[index];
        pageTitle.Dock = DockStyle.Fill;
        pageTitle.ForeColor = TextColor;
        pageTitle.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
        pageTitle.Margin = Padding.Empty;
        pageSubtitle.Text = subtitles[index];
        pageSubtitle.Dock = DockStyle.Fill;
        pageSubtitle.ForeColor = Muted;
        pageSubtitle.Font = new Font("Segoe UI", 10.5F);
        pageSubtitle.Margin = Padding.Empty;
        heading.Controls.Add(pageTitle, 0, 0);
        heading.Controls.Add(pageSubtitle, 0, 1);
        page.Controls.Add(heading, 0, 0);
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Bg, AutoScroll = true, Margin = Padding.Empty };
        body.Controls.Add(BuildBody(index));
        page.Controls.Add(body, 0, 1);
        return page;
    }

    private Control BuildBody(int index) => index switch
    {
        0 => WelcomePage(),
        1 => TermsPage(),
        2 => ComponentsPage(),
        3 => DownloadPage(),
        4 => InstallPage(),
        5 => DatabasePage(),
        _ => FinishPage()
    };

    private TableLayoutPanel BodyGrid(int rows)
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = rows, BackColor = Bg, Padding = new Padding(0, 0, 8, 8), Margin = Padding.Empty };
        return grid;
    }

    private Control Card(string title, string text)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = CardBg, Padding = new Padding(18), Margin = new Padding(0, 0, 0, 12) };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, ForeColor = TextColor, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Margin = Padding.Empty }, 0, 0);
        card.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 10F), AutoEllipsis = false, Margin = Padding.Empty }, 0, 1);
        return card;
    }

    private Control WelcomePage()
    {
        var grid = BodyGrid(3);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        grid.Controls.Add(Card("Welcome to Suvidha POS", "A clean, responsive installer designed for Windows 10/11 and high-DPI displays."), 0, 0);
        grid.Controls.Add(Card("Installation plan", "Review terms • Select components • Prepare downloads • Install • Configure database • Finish"), 0, 1);
        grid.Controls.Add(Card("Ready to begin", "Click Next to continue. The window can be resized without crushing the page content."), 0, 2);
        return grid;
    }

    private Control TermsPage()
    {
        var grid = BodyGrid(2);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 360));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var box = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, BackColor = InputBg, ForeColor = TextColor, Font = new Font("Segoe UI", 10.5F), ScrollBars = RichTextBoxScrollBars.Vertical, WordWrap = true, Text = "SUVIDHA POS SOFTWARE LICENSE AGREEMENT\n\nThe software is licensed, not sold. You may use it only for lawful purposes and in accordance with the installation configuration you select.\n\nYou may not modify, distribute, sell, lease, or reverse engineer any protected part of the software without written permission.\n\nInstallation may download third-party components. Their respective licenses and terms remain applicable.\n\nData and configuration are stored locally unless the application configuration specifies otherwise.\n\nTHE SOFTWARE IS PROVIDED AS-IS. USE OF THE SOFTWARE IS AT YOUR OWN RISK." };
        grid.Controls.Add(box, 0, 0);
        acceptTerms.Text = "I have read and accept the terms and conditions";
        acceptTerms.Dock = DockStyle.Fill;
        acceptTerms.ForeColor = TextColor;
        acceptTerms.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        acceptTerms.CheckedChanged += (_, _) => next.Enabled = acceptTerms.Checked;
        grid.Controls.Add(acceptTerms, 0, 1);
        return grid;
    }

    private Control ComponentsPage()
    {
        var grid = BodyGrid(2);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = CardBg, Padding = new Padding(18), Margin = new Padding(0, 0, 0, 12) };
        string[] names = { "Suvidha POS Application", "SQL Server 2019", "SQL Server Management Studio", "Crystal Reports Runtime", "Database Backup / Restore Tools" };
        for (int i = 0; i < names.Length; i++)
        {
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            var cb = new CheckBox { Text = names[i], Checked = true, Dock = DockStyle.Fill, ForeColor = TextColor, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = Padding.Empty };
            components.Add(cb);
            card.Controls.Add(cb, 0, i);
        }
        grid.Controls.Add(card, 0, 0);
        grid.Controls.Add(Card("Selection", "Uncheck optional components if they are already installed on this machine."), 0, 1);
        return grid;
    }

    private Control DownloadPage() => Card("Download", "Required installer files are prepared here. Continue to the Install step when the package is ready.");

    private Control InstallPage() => Card("Install", "Selected components will be installed using the configured installation package. The destination and progress are shown here.");

    private Control DatabasePage()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 2, BackColor = Bg, Margin = Padding.Empty };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = CardBg, Padding = new Padding(20), Margin = new Padding(0, 0, 0, 12) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        server = Input("localhost\\SQLEXPRESS");
        database = Input("SuvidhaPOS");
        authentication = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = InputBg, ForeColor = TextColor, Font = new Font("Segoe UI", 10F), FlatStyle = FlatStyle.Flat };
        authentication.Items.AddRange(new object[] { "Windows Authentication", "SQL Server Authentication" });
        authentication.SelectedIndex = 0;
        AddField(form, 0, "Server / Instance", server);
        AddField(form, 1, "Database", database);
        AddField(form, 2, "Authentication", authentication);
        grid.Controls.Add(form, 0, 0);
        grid.Controls.Add(Card("Database configuration", "These fields are editable. Your selected values can be used by the installer when database setup is implemented."), 0, 1);
        return grid;
    }

    private TextBox Input(string value) => new() { Text = value, Dock = DockStyle.Fill, BackColor = InputBg, ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10F), Margin = Padding.Empty };

    private void AddField(TableLayoutPanel form, int row, string label, Control input)
    {
        form.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = TextColor, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = Padding.Empty }, 0, row);
        form.Controls.Add(input, 1, row);
    }

    private Control FinishPage()
    {
        var grid = BodyGrid(3);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        grid.Controls.Add(Card("Installation complete", "Suvidha POS setup has completed successfully."), 0, 0);
        grid.Controls.Add(Card("Next step", "Use the installed Suvidha POS application from the Start Menu or desktop shortcut."), 0, 1);
        grid.Controls.Add(Card("Thank you", "You can close this installer now."), 0, 2);
        return grid;
    }
}
