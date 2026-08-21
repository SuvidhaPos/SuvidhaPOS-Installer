using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

internal static class UiPolish
{
    private static readonly Color Bg = Color.FromArgb(4, 12, 25);
    private static readonly Color SidebarBg = Color.FromArgb(3, 14, 31);
    private static readonly Color CardBg = Color.FromArgb(6, 24, 50);
    private static readonly Color Border = Color.FromArgb(18, 65, 111);
    private static readonly Color Text = Color.FromArgb(244, 247, 252);
    private static readonly Color Muted = Color.FromArgb(166, 184, 207);
    private static readonly Color Blue = Color.FromArgb(0, 166, 255);
    private static readonly Color Green = Color.FromArgb(48, 224, 119);
    private static readonly Color ButtonBack = Color.FromArgb(12, 30, 52);
    private static readonly Color ButtonHover = Color.FromArgb(20, 55, 88);

    private static bool refreshPending;

    public static void Apply(MainForm form)
    {
        form.Shown += (_, _) =>
        {
            ConfigureForm(form);
            WireRefresh(form);
            ApplyReferenceLayout(form);
        };
    }

    private static void ConfigureForm(MainForm form)
    {
        form.MinimumSize = new Size(1024, 768);
        form.Size = new Size(1366, 768);
        form.BackColor = Bg;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 10F);

        var shell = Field<TableLayoutPanel>(form, "shellRoot");
        var sidebar = Field<FlowLayoutPanel>(form, "sidebar");
        var content = Field<Panel>(form, "content");
        if (shell == null || sidebar == null || content == null) return;

        if (shell.RowCount == 2)
        {
            var header = shell.GetControlFromPosition(0, 0);
            if (header != null) shell.Controls.Remove(header);
            shell.RowCount = 1;
            shell.RowStyles.Clear();
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.SetCellPosition(sidebar, new TableLayoutPanelCellPosition(0, 0));
            shell.SetCellPosition(content, new TableLayoutPanelCellPosition(1, 0));
        }

        shell.ColumnStyles.Clear();
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.BackColor = SidebarBg;
        sidebar.Padding = new Padding(14, 14, 14, 14);
        sidebar.AutoScroll = true;
        content.BackColor = Bg;
        content.Padding = new Padding(12, 10, 12, 10);

        foreach (Control c in sidebar.Controls)
        {
            if (c.GetType().Name == "StepItem")
            {
                c.Width = 250;
                c.Height = 78;
                c.Margin = new Padding(0, 0, 0, 8);
            }
            else if (c.GetType().Name == "HelpCard")
            {
                c.Width = 250;
                c.Height = 122;
                c.Margin = new Padding(0, 10, 0, 0);
                ReplaceSupportNumber(c);
            }
        }
        StyleButtons(form);
    }

    private static void WireRefresh(MainForm form)
    {
        var content = Field<Panel>(form, "content");
        if (content == null) return;
        content.ControlAdded += (_, _) => ScheduleRefresh(form);
        content.Resize += (_, _) => ScheduleRefresh(form);
        form.Resize += (_, _) => ScheduleRefresh(form);
    }

    private static void ScheduleRefresh(MainForm form)
    {
        if (refreshPending || form.IsDisposed) return;
        refreshPending = true;
        form.BeginInvoke(new Action(() =>
        {
            refreshPending = false;
            if (!form.IsDisposed) ApplyReferenceLayout(form);
        }));
    }

    private static void ApplyReferenceLayout(MainForm form)
    {
        ConfigureFormOnce(form);
        var pageBody = Field<Panel>(form, "pageBody");
        if (pageBody == null || pageBody.IsDisposed) return;
        int step = GetStep(form);
        pageBody.BackColor = Color.Transparent;
        pageBody.Padding = Padding.Empty;
        pageBody.AutoScroll = true;

        if (step > 0)
        {
            EnsurePageHeader(pageBody, step);
            var header = pageBody.Controls.Cast<Control>().FirstOrDefault(c => Equals(c.Tag, "ReferencePageHeader"));
            if (header != null) LayoutPageBody(pageBody, header);
        }
        else
        {
            RemoveReferenceHeader(pageBody);
        }

        StylePageControls(pageBody);
        StyleButtons(form);
        UpdateFooter(form, step);
    }

    private static void ConfigureFormOnce(MainForm form)
    {
        var shell = Field<TableLayoutPanel>(form, "shellRoot");
        var sidebar = Field<FlowLayoutPanel>(form, "sidebar");
        if (shell == null || sidebar == null || shell.ColumnStyles.Count < 2) return;
        shell.ColumnStyles[0].Width = Math.Clamp((int)Math.Round(form.ClientSize.Width * 0.19), 260, 300);
        int inner = Math.Max(220, (int)shell.ColumnStyles[0].Width - sidebar.Padding.Horizontal);
        foreach (Control c in sidebar.Controls)
        {
            if (c.GetType().Name == "StepItem")
            {
                c.Width = inner;
                c.Height = form.ClientSize.Height < 820 ? 76 : 82;
            }
            else if (c.GetType().Name == "HelpCard")
            {
                c.Width = inner;
                c.Height = form.ClientSize.Height < 820 ? 112 : 124;
            }
        }
    }

    private static void EnsurePageHeader(Panel pageBody, int step)
    {
        const string marker = "ReferencePageHeader";
        foreach (Control c in pageBody.Controls)
            if (Equals(c.Tag, marker)) return;

        string pageTitle = step switch
        {
            1 => "Terms & Conditions",
            2 => "Components",
            3 => "Download",
            4 => "Install",
            5 => "Database Setup",
            6 => "Finish",
            _ => "Suvidha POS Installer"
        };
        string subtitle = step switch
        {
            1 => "Please read the following terms and conditions carefully.",
            2 => "Choose the components you want to install.",
            3 => "Please wait while we download the required files.",
            4 => "Please wait while we install the selected components.",
            5 => "Please configure the database connection settings.",
            6 => "Installation complete.",
            _ => ""
        };

        var header = new Panel
        {
            Name = marker,
            Tag = marker,
            Height = 138,
            Dock = DockStyle.Top,
            BackColor = CardBg,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 12)
        };

        var logo = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 190,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(4, 4, 16, 4)
        };
        try
        {
            var p = Path.Combine(AppContext.BaseDirectory, "Assets", "SuvidhaPOS.png");
            if (File.Exists(p)) logo.Image = Image.FromFile(p);
        }
        catch { }
        header.Controls.Add(logo);
        header.Controls.Add(new Label { Text = pageTitle, Font = new Font("Segoe UI Semibold", 25F), ForeColor = Text, Dock = DockStyle.Top, Height = 50, AutoEllipsis = true });
        header.Controls.Add(new Label { Text = subtitle, Font = new Font("Segoe UI", 11F), ForeColor = Muted, Dock = DockStyle.Top, Height = 34, AutoEllipsis = true });
        header.Controls.Add(new Label
        {
            Text = step == 3 ? @"Download location: D:\Suvidha Pos\Software" :
                   step == 4 ? @"Installation location: D:\Suvidha Pos\Software" :
                   step == 5 ? "SQL Server is installed and ready to use." :
                   "Suvidha POS Installer",
            Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Green,
            Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft
        });

        pageBody.Controls.Add(header);
        pageBody.Controls.SetChildIndex(header, 0);
        LayoutPageBody(pageBody, header);
    }

    private static void LayoutPageBody(Panel pageBody, Control header)
    {
        foreach (Control c in pageBody.Controls)
        {
            if (ReferenceEquals(c, header)) continue;
            c.Dock = DockStyle.Top;
            c.Height = Math.Max(120, pageBody.ClientSize.Height - header.Height - 16);
            c.Margin = new Padding(0, 0, 0, 8);
        }
    }

    private static void RemoveReferenceHeader(Panel pageBody)
    {
        var headers = pageBody.Controls.Cast<Control>().Where(c => Equals(c.Tag, "ReferencePageHeader")).ToList();
        foreach (var h in headers) pageBody.Controls.Remove(h);
    }

    private static void StylePageControls(Panel pageBody)
    {
        foreach (Control root in pageBody.Controls)
        {
            if (Equals(root.Tag, "ReferencePageHeader")) continue;
            StyleControlTree(root);
        }
    }

    private static void StyleControlTree(Control root)
    {
        if (root is Panel panel && panel.BorderStyle == BorderStyle.FixedSingle)
            panel.BackColor = CardBg;
        if (root is TextBox box)
        {
            box.BackColor = Color.FromArgb(8, 22, 40);
            box.ForeColor = Text;
            box.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (root is RichTextBox rich)
        {
            rich.BackColor = Color.FromArgb(3, 14, 28);
            rich.ForeColor = Text;
            rich.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (root is CheckBox check)
        {
            check.ForeColor = Text;
            check.Font = new Font("Segoe UI Semibold", 10F);
        }
        else if (root is RadioButton radio)
        {
            radio.ForeColor = Text;
        }
        foreach (Control child in root.Controls)
            StyleControlTree(child);
    }

    private static void StyleButtons(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Border;
                button.Font = new Font("Segoe UI Semibold", 10.5F);
                button.Cursor = Cursors.Hand;
                button.UseVisualStyleBackColor = false;
                bool primary = button.Text.Contains("Next", StringComparison.OrdinalIgnoreCase) || button.Text.Contains("Install", StringComparison.OrdinalIgnoreCase) || button.Text.Contains("Finish", StringComparison.OrdinalIgnoreCase);
                button.BackColor = primary ? Blue : ButtonBack;
                button.ForeColor = Color.White;
                if (!Equals(button.Tag, "ReferenceButtonStyled"))
                {
                    button.Tag = "ReferenceButtonStyled";
                    button.MouseEnter += (_, _) => { button.FlatAppearance.BorderColor = Blue; if (button.Enabled) button.BackColor = ButtonHover; };
                    button.MouseLeave += (_, _) => { button.FlatAppearance.BorderColor = Border; button.BackColor = primary ? Blue : ButtonBack; };
                }
            }
            if (control.HasChildren) StyleButtons(control);
        }
    }

    private static void UpdateFooter(MainForm form, int step)
    {
        var footerStep = Field<Label>(form, "footerStep");
        var progress = Field<ProgressBar>(form, "footerProgress");
        var back = Field<Button>(form, "backButton");
        if (footerStep != null) footerStep.Text = $"Step {step + 1} of 7";
        if (progress != null) progress.Value = Math.Clamp((int)Math.Round(step / 6.0 * 100), 0, 100);
        if (back != null) back.Enabled = step > 0;
    }

    private static void ReplaceSupportNumber(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is Label label && label.Text.Contains("+91"))
            {
                label.Text = "+91 827171 8844";
                label.ForeColor = Blue;
                label.Font = new Font("Segoe UI Semibold", 10F);
            }
            if (c.HasChildren) ReplaceSupportNumber(c);
        }
    }

    private static int GetStep(MainForm form)
    {
        var f = form.GetType().GetField("step", BindingFlags.Instance | BindingFlags.NonPublic);
        return f?.GetValue(form) is int value ? Math.Clamp(value, 0, 6) : 0;
    }

    private static T? Field<T>(object target, string name) where T : class
    {
        return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as T;
    }
}
