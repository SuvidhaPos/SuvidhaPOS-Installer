using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

namespace SuvidhaPosInstaller;

/// <summary>
/// Single responsive UI layer for the installer.
/// It does not create a second header or move the original page controls around.
/// </summary>
internal static class ResponsiveLayout
{
    private static readonly Color Bg = Color.FromArgb(4, 12, 25);
    private static readonly Color SidebarBg = Color.FromArgb(3, 14, 31);
    private static readonly Color Border = Color.FromArgb(18, 65, 111);
    private static readonly Color Text = Color.FromArgb(244, 247, 252);
    private static readonly Color Blue = Color.FromArgb(0, 166, 255);
    private static readonly Color ButtonBack = Color.FromArgb(20, 36, 58);

    private static bool wired;
    private static bool resizePending;

    public static void Apply(MainForm form)
    {
        if (wired) return;
        wired = true;

        ConfigureForm(form);
        var content = Field<Panel>(form, "content");
        if (content != null)
            content.ControlAdded += (_, _) => ScheduleRefresh(form);

        form.Shown += (_, _) =>
        {
            ConfigureForm(form);
            WireNextFix(form);
            Refresh(form);
        };
        form.Resize += (_, _) => ScheduleRefresh(form);
    }

    private static void ConfigureForm(MainForm form)
    {
        form.MinimumSize = new Size(1024, 768);
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.FormBorderStyle = FormBorderStyle.Sizable;
        form.MaximizeBox = true;
        form.MinimizeBox = true;
        form.BackColor = Bg;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    }

    private static void ScheduleRefresh(MainForm form)
    {
        if (resizePending || form.IsDisposed || !form.IsHandleCreated) return;
        resizePending = true;
        form.BeginInvoke(new Action(() =>
        {
            resizePending = false;
            if (!form.IsDisposed) Refresh(form);
        }));
    }

    private static void Refresh(MainForm form)
    {
        var shell = Field<TableLayoutPanel>(form, "shellRoot");
        var sidebar = Field<FlowLayoutPanel>(form, "sidebar");
        var content = Field<Panel>(form, "content");
        if (shell == null || sidebar == null || content == null) return;

        int w = Math.Max(1024, form.ClientSize.Width);
        int h = Math.Max(768, form.ClientSize.Height);
        int sidebarWidth = Math.Clamp((int)Math.Round(w * 0.205), 250, 300);
        int headerHeight = Math.Clamp((int)Math.Round(h * 0.09), 68, 82);

        shell.ColumnStyles[0].Width = sidebarWidth;
        shell.RowStyles[0].Height = headerHeight;
        sidebar.BackColor = SidebarBg;
        sidebar.Padding = new Padding(12, 12, 12, 12);
        content.BackColor = Bg;
        content.Padding = new Padding(12, 10, 12, 10);

        int itemWidth = Math.Max(210, sidebarWidth - sidebar.Padding.Horizontal);
        int itemHeight = h < 820 ? 72 : 76;
        foreach (Control child in sidebar.Controls)
        {
            if (child.GetType().Name == "StepItem")
            {
                child.Width = itemWidth;
                child.Height = itemHeight;
                child.Margin = new Padding(0, 0, 0, 7);
            }
            else if (child.GetType().Name == "HelpCard")
            {
                child.Width = itemWidth;
                child.Height = h < 820 ? 104 : 116;
                child.Margin = new Padding(0, 8, 0, 0);
                ReplaceSupportNumber(child);
            }
        }

        NormalizeStepText(form, sidebar);
        FixFeatureCards(content);
        FixFooter(content);
        StyleButtons(form);
        WireNextFix(form);
    }

    private static void NormalizeStepText(MainForm form, Control sidebar)
    {
        foreach (Control c in AllControls(sidebar))
        {
            if (c is Label label && label.Text.Equals("Setup & Backup", StringComparison.OrdinalIgnoreCase))
                label.Text = "Database Setup";
            else if (c is Label label2 && label2.Text.Equals("Database setup & backup", StringComparison.OrdinalIgnoreCase))
                label2.Text = "Configure database";
        }

        var headerTitle = Field<Label>(form, "headerTitle");
        if (headerTitle != null && headerTitle.Text.Equals("Setup & Backup", StringComparison.OrdinalIgnoreCase))
            headerTitle.Text = "Database Setup";
        var headerSub = Field<Label>(form, "headerSub");
        if (headerSub != null && headerSub.Text.Equals("Database setup & backup", StringComparison.OrdinalIgnoreCase))
            headerSub.Text = "Configure database";
    }

    private static void FixFeatureCards(Control root)
    {
        foreach (Control c in AllControls(root))
        {
            if (c.GetType().Name != "FeatureCard") continue;
            var labels = c.Controls.OfType<Label>().ToList();
            if (labels.Count < 2 || c.Controls.OfType<TableLayoutPanel>().Any()) continue;

            var icon = labels[0];
            var text = labels[1];
            c.Controls.Clear();

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = new Padding(6, 4, 6, 4)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            icon.Dock = DockStyle.Fill;
            icon.Margin = Padding.Empty;
            icon.TextAlign = ContentAlignment.MiddleCenter;
            text.Dock = DockStyle.Fill;
            text.Margin = new Padding(4, 0, 0, 0);
            text.AutoEllipsis = true;
            text.Font = new Font("Segoe UI Semibold", 9F);

            grid.Controls.Add(icon, 0, 0);
            grid.Controls.Add(text, 1, 0);
            c.Controls.Add(grid);
        }
    }

    private static void FixFooter(Control root)
    {
        var footer = root.Controls.OfType<TableLayoutPanel>().FirstOrDefault(t =>
            t.Dock == DockStyle.Bottom && t.ColumnCount == 6 && t.Controls.OfType<Button>().Count() >= 3);
        if (footer == null) return;

        int fixedWidth = 76 + 44 + 88 + 96 + 132 + footer.Padding.Horizontal;
        int progressWidth = Math.Max(120, footer.ClientSize.Width - fixedWidth);
        footer.ColumnStyles[0].Width = 76;
        footer.ColumnStyles[1].SizeType = SizeType.Absolute;
        footer.ColumnStyles[1].Width = progressWidth;
        footer.ColumnStyles[2].Width = 44;
        footer.ColumnStyles[3].Width = 88;
        footer.ColumnStyles[4].Width = 96;
        footer.ColumnStyles[5].Width = 132;

        foreach (Button b in footer.Controls.OfType<Button>())
        {
            b.Dock = DockStyle.Fill;
            b.MinimumSize = new Size(0, 42);
            b.AutoEllipsis = true;
        }
    }

    private static void StyleButtons(Control root)
    {
        foreach (Control c in AllControls(root))
        {
            if (c is not Button b) continue;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Border;
            b.UseVisualStyleBackColor = false;
            b.Font = new Font("Segoe UI Semibold", 10.5F);
            b.ForeColor = Color.White;
            b.BackColor = IsPrimary(b.Text) ? Blue : ButtonBack;
            b.Cursor = Cursors.Hand;
        }
    }

    private static bool IsPrimary(string text) =>
        text.Contains("Next", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("Install", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("Finish", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("Save & Continue", StringComparison.OrdinalIgnoreCase);

    private static void WireNextFix(MainForm form)
    {
        var next = Field<Button>(form, "nextButton");
        if (next == null || Equals(next.Tag, "StepZeroFix")) return;
        next.Tag = "StepZeroFix";
        next.Click += (_, _) =>
        {
            if (GetStep(form) == 0)
                InvokePrivate(form, "ShowStep", 1);
        };
    }

    private static void ReplaceSupportNumber(Control root)
    {
        foreach (Control c in AllControls(root))
        {
            if (c is Label label && label.Text.Contains("+91", StringComparison.Ordinal))
            {
                label.Text = "+91 827171 8844";
                label.ForeColor = Blue;
                label.AutoEllipsis = true;
            }
        }
    }

    private static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in AllControls(child)) yield return nested;
        }
    }

    private static int GetStep(MainForm form)
    {
        var field = form.GetType().GetField("step", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(form) is int value ? Math.Clamp(value, 0, 6) : 0;
    }

    private static void InvokePrivate(MainForm form, string method, params object[] args)
    {
        var m = form.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        m?.Invoke(form, args);
    }

    private static T? Field<T>(object target, string name) where T : class =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as T;
}
