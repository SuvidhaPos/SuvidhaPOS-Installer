using System.Reflection;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

internal static class UiPolish
{
    private static readonly Color Card = Color.FromArgb(7, 24, 48);
    private static readonly Color Border = Color.FromArgb(27, 78, 124);
    private static readonly Color Text = Color.FromArgb(244, 247, 252);
    private static readonly Color Muted = Color.FromArgb(166, 184, 207);
    private static readonly Color Cyan = Color.FromArgb(0, 211, 255);

    public static void Apply(MainForm form)
    {
        form.Resize += (_, _) => Layout(form);
        form.Shown += (_, _) => Layout(form);
        var timer = new System.Windows.Forms.Timer { Interval = 180 };
        timer.Tick += (_, _) => Layout(form);
        timer.Start();
        form.FormClosed += (_, _) => { timer.Stop(); timer.Dispose(); };
    }

    private static void Layout(MainForm form)
    {
        if (form.IsDisposed || !form.IsHandleCreated) return;
        var pageBody = GetField<Panel>(form, "pageBody");
        var content = GetField<Panel>(form, "content");
        var sidebar = GetField<FlowLayoutPanel>(form, "sidebar");
        if (pageBody == null || content == null || sidebar == null) return;
        form.SuspendLayout();
        try
        {
            SetHorizontalScroll(content, false);
            SetHorizontalScroll(pageBody, false);
            LayoutSidebar(sidebar);
            foreach (Control c in pageBody.Controls)
            {
                if (c is FlowLayoutPanel flow) LayoutFlow(flow);
                else if (c is TableLayoutPanel table) LayoutTable(table);
                else if (c.GetType().Name == "RoundedCard") LayoutCard(c);
            }
            StyleButtons(form);
        }
        finally { form.ResumeLayout(true); }
    }

    private static void LayoutSidebar(FlowLayoutPanel sidebar)
    {
        var w = Math.Max(180, sidebar.ClientSize.Width - sidebar.Padding.Horizontal - 2);
        var compact = sidebar.ClientSize.Height < 720;
        var stepHeight = compact ? 62 : 74;
        var helpHeight = compact ? 116 : 136;
        foreach (Control c in sidebar.Controls)
        {
            if (c.GetType().Name == "StepItem") { c.Width = w; c.Height = stepHeight; LayoutStepItem(c); }
            else if (c.GetType().Name == "HelpCard") { c.Width = w; c.Height = helpHeight; LayoutHelpCard(c); }
        }
    }

    private static void LayoutStepItem(Control item)
    {
        var number = item.Controls.OfType<Label>().FirstOrDefault(l => l.Tag is string);
        var labels = item.Controls.OfType<Label>().Where(l => l != number).ToList();
        var title = labels.ElementAtOrDefault(0);
        var sub = labels.ElementAtOrDefault(1);
        var h = item.ClientSize.Height;
        if (number != null) { var s = Math.Clamp(h - 22, 36, 46); number.Bounds = new Rectangle(10, (h - s) / 2, s, s); }
        if (title != null) { title.AutoSize = false; title.Location = new Point(number?.Right + 12 ?? 58, 9); title.Width = Math.Max(90, item.ClientSize.Width - title.Left - 10); title.Height = 25; title.AutoEllipsis = true; }
        if (sub != null) { sub.AutoSize = false; sub.Location = new Point(number?.Right + 12 ?? 58, 34); sub.Width = Math.Max(90, item.ClientSize.Width - sub.Left - 10); sub.Height = 20; sub.AutoEllipsis = true; }
    }

    private static void LayoutHelpCard(Control card)
    {
        var labels = card.Controls.OfType<Label>().ToList();
        var y = 10;
        foreach (var label in labels)
        {
            label.AutoSize = false;
            label.Left = 14;
            label.Width = Math.Max(100, card.ClientSize.Width - 28);
            label.AutoEllipsis = true;
            label.Height = label.Font.Size >= 12 ? 25 : 20;
            label.Top = y;
            y += label.Height + 2;
        }
    }

    private static void LayoutFlow(FlowLayoutPanel flow)
    {
        flow.HorizontalScroll.Enabled = false;
        flow.HorizontalScroll.Visible = false;
        flow.WrapContents = false;
        flow.FlowDirection = FlowDirection.TopDown;
        var width = Math.Max(260, flow.ClientSize.Width - flow.Padding.Horizontal - 4);
        foreach (Control child in flow.Controls)
        {
            child.Width = width;
            if (child.GetType().Name == "RoundedCard") LayoutCard(child);
            else if (child.GetType().Name == "ComponentSelectCard") LayoutComponentCard(child);
        }
    }

    private static void LayoutTable(TableLayoutPanel table)
    {
        table.AutoScroll = false;
        table.HorizontalScroll.Enabled = false;
        table.HorizontalScroll.Visible = false;
        var narrow = table.ClientSize.Width < 760;
        if (table.Controls.Count == 2 && table.ColumnCount == 2 && table.RowCount == 1)
        {
            var a = table.Controls[0];
            var b = table.Controls[1];
            table.Controls.Remove(a); table.Controls.Remove(b);
            table.ColumnStyles.Clear(); table.RowStyles.Clear();
            if (narrow)
            {
                table.ColumnCount = 1; table.RowCount = 2;
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                table.Controls.Add(a, 0, 0); table.Controls.Add(b, 0, 1);
            }
            else
            {
                table.ColumnCount = 2; table.RowCount = 1;
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                table.Controls.Add(a, 0, 0); table.Controls.Add(b, 1, 0);
            }
        }
        foreach (Control child in table.Controls) if (child.GetType().Name == "RoundedCard") LayoutCard(child);
    }

    private static void LayoutCard(Control card)
    {
        card.BackColor = Card;
        SetHorizontalScroll(card, false);
        if (IsWelcomeHero(card)) { LayoutWelcomeHero(card); return; }
        if (IsFinishHero(card)) { LayoutFinishHero(card); return; }
        var width = Math.Max(260, card.ClientSize.Width);
        foreach (Control child in card.Controls)
            if (child is Label label) { label.MaximumSize = new Size(Math.Max(120, width - Math.Max(0, label.Left) - 18), 0); label.AutoEllipsis = true; }
        var progress = card.Controls.OfType<ProgressBar>().FirstOrDefault();
        if (progress != null) { progress.Left = 54; progress.Width = Math.Max(100, card.ClientSize.Width - 72); progress.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top; }
    }

    private static bool IsWelcomeHero(Control card) => card.Controls.OfType<Label>().Any(l => l.Text == "Welcome to");
    private static bool IsFinishHero(Control card) => card.Controls.OfType<Label>().Any(l => l.Text.Contains("Installation completed successfully", StringComparison.OrdinalIgnoreCase));

    private static void LayoutWelcomeHero(Control hero)
    {
        var logo = hero.Controls.OfType<PictureBox>().FirstOrDefault();
        var labels = hero.Controls.OfType<Label>().ToList();
        var title1 = labels.FirstOrDefault(l => l.Text == "Welcome to");
        var title2 = labels.FirstOrDefault(l => l.Text == "Suvidha POS Installer");
        var desc = labels.FirstOrDefault(l => l.Text.StartsWith("Install Suvidha POS", StringComparison.OrdinalIgnoreCase));
        var features = hero.Controls.Cast<Control>().Where(c => c.GetType().Name == "FeatureCard").ToList();
        var w = Math.Max(320, hero.ClientSize.Width);
        var narrow = w < 820;
        var pad = narrow ? 16 : 22;
        var titleLeft = narrow ? pad : 210;
        hero.Height = narrow ? 350 : 280;
        if (logo != null) logo.Bounds = narrow ? new Rectangle(pad, 16, 130, 72) : new Rectangle(pad, 18, 165, 82);
        if (title1 != null) { title1.AutoSize = false; title1.Bounds = new Rectangle(titleLeft, 18, Math.Max(180, w - titleLeft - pad), 36); title1.Font = new Font("Segoe UI Semibold", narrow ? 20f : Math.Clamp(w / 42f, 23f, 31f)); }
        if (title2 != null) { title2.AutoSize = false; title2.Bounds = new Rectangle(titleLeft, 55, Math.Max(180, w - titleLeft - pad), 44); title2.Font = new Font("Segoe UI Semibold", narrow ? 23f : Math.Clamp(w / 38f, 25f, 34f)); title2.AutoEllipsis = true; }
        if (desc != null) { desc.AutoSize = false; desc.Bounds = new Rectangle(titleLeft, narrow ? 102 : 104, Math.Max(180, w - titleLeft - pad), narrow ? 44 : 30); desc.Font = new Font("Segoe UI", narrow ? 9.5f : 10.5f); desc.AutoEllipsis = true; }
        var gap = 8; var columns = narrow ? 2 : 4; var inner = w - pad * 2; var featureW = Math.Max(120, (inner - gap * (columns - 1)) / columns); var featureH = 72;
        for (int i = 0; i < features.Count; i++) { var row = i / columns; var col = i % columns; features[i].Bounds = new Rectangle(pad + col * (featureW + gap), 160 + row * (featureH + gap), featureW, featureH); LayoutFeature(features[i]); }
    }

    private static void LayoutFeature(Control card)
    {
        var labels = card.Controls.OfType<Label>().ToList();
        var icon = labels.ElementAtOrDefault(0); var title = labels.ElementAtOrDefault(1); var sub = labels.ElementAtOrDefault(2);
        if (icon != null) icon.Bounds = new Rectangle(10, 14, 36, 40);
        if (title != null) { title.AutoSize = false; title.Bounds = new Rectangle(50, 9, Math.Max(60, card.ClientSize.Width - 58), 24); title.AutoEllipsis = true; }
        if (sub != null) { sub.AutoSize = false; sub.Bounds = new Rectangle(50, 35, Math.Max(60, card.ClientSize.Width - 58), 22); sub.AutoEllipsis = true; }
    }

    private static void LayoutFinishHero(Control hero)
    {
        var w = Math.Max(320, hero.ClientSize.Width);
        var labels = hero.Controls.OfType<Label>().ToList();
        var check = labels.FirstOrDefault(l => l.Text == "✓");
        var title = labels.FirstOrDefault(l => l.Text.Contains("Installation completed successfully", StringComparison.OrdinalIgnoreCase));
        var desc = labels.FirstOrDefault(l => l.Text.Contains("All selected components", StringComparison.OrdinalIgnoreCase));
        var button = hero.Controls.OfType<Button>().FirstOrDefault();
        hero.Height = 220;
        if (check != null) check.Bounds = new Rectangle(24, 30, 72, 72);
        if (title != null) { title.AutoSize = false; title.Bounds = new Rectangle(110, 32, Math.Max(160, w - 130), 38); title.AutoEllipsis = true; }
        if (desc != null) { desc.AutoSize = false; desc.Bounds = new Rectangle(112, 78, Math.Max(160, w - 132), 38); desc.AutoEllipsis = true; }
        if (button != null) button.Location = new Point(112, 130);
    }

    private static void LayoutComponentCard(Control card)
    {
        card.Width = Math.Max(260, card.Parent?.ClientSize.Width ?? card.Width);
        var labels = card.Controls.OfType<Label>().ToList();
        var check = card.Controls.OfType<CheckBox>().FirstOrDefault();
        var icon = labels.ElementAtOrDefault(0); var title = labels.ElementAtOrDefault(1); var desc = labels.ElementAtOrDefault(2);
        var h = Math.Max(72, card.Height);
        if (icon != null) icon.Location = new Point(16, (h - 28) / 2);
        if (title != null) { title.AutoSize = false; title.Bounds = new Rectangle(58, 13, Math.Max(120, card.ClientSize.Width - 112), 24); title.AutoEllipsis = true; }
        if (desc != null) { desc.AutoSize = false; desc.Bounds = new Rectangle(58, 40, Math.Max(120, card.ClientSize.Width - 112), 24); desc.AutoEllipsis = true; }
        if (check != null) check.Location = new Point(Math.Max(20, card.ClientSize.Width - 34), (h - 18) / 2);
    }

    private static void StyleButtons(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = b.GetType().Name == "GradientButton" ? 0 : 1;
                b.FlatAppearance.BorderColor = Border;
                b.Font = new Font("Segoe UI Semibold", 10f);
                b.Cursor = Cursors.Hand;
                b.UseVisualStyleBackColor = false;
                if (b.GetType().Name != "GradientButton") { b.BackColor = Color.FromArgb(14, 36, 62); b.ForeColor = Text; }
                if (b.Tag as string != "UiPolish")
                {
                    b.Tag = "UiPolish";
                    b.MouseEnter += (_, _) => { b.FlatAppearance.BorderColor = Cyan; if (b.GetType().Name != "GradientButton") b.BackColor = Color.FromArgb(20, 55, 88); };
                    b.MouseLeave += (_, _) => { b.FlatAppearance.BorderColor = Border; if (b.GetType().Name != "GradientButton") b.BackColor = Color.FromArgb(14, 36, 62); };
                    b.MouseDown += (_, _) => b.Padding = new Padding(0, 2, 0, 0);
                    b.MouseUp += (_, _) => b.Padding = Padding.Empty;
                }
            }
            if (c.HasChildren) StyleButtons(c);
        }
    }

    private static void SetHorizontalScroll(Control control, bool enabled)
    {
        if (control is ScrollableControl scrollable)
        {
            scrollable.HorizontalScroll.Enabled = enabled;
            scrollable.HorizontalScroll.Visible = enabled;
            if (!enabled) scrollable.AutoScrollMinSize = Size.Empty;
        }
    }

    private static T? GetField<T>(object obj, string name) where T : class =>
        obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj) as T;
}
