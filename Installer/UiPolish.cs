using System.Reflection;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

internal static class UiPolish
{
    private static readonly Color Card = Color.FromArgb(7, 24, 48);
    private static readonly Color Border = Color.FromArgb(27, 78, 124);
    private static readonly Color Text = Color.FromArgb(244, 247, 252);

    public static void Apply(MainForm form)
    {
        form.Resize += (_, _) => Layout(form);
        form.Shown += (_, _) => Layout(form);

        // Pages are rebuilt when Next/Back is clicked. A short UI watcher
        // reapplies the responsive measurements after every page rebuild.
        var timer = new System.Windows.Forms.Timer { Interval = 250 };
        timer.Tick += (_, _) => Layout(form);
        timer.Start();
        form.FormClosed += (_, _) => { timer.Stop(); timer.Dispose(); };
    }

    private static void Layout(MainForm form)
    {
        if (form.IsDisposed) return;

        var pageBody = GetField<Panel>(form, "pageBody");
        var content = GetField<Panel>(form, "content");
        var sidebar = GetField<FlowLayoutPanel>(form, "sidebar");
        if (pageBody == null || content == null || sidebar == null) return;

        form.SuspendLayout();
        try
        {
            // The old screenshot shows a horizontal scrollbar. That is the
            // main reason the first characters of cards/buttons are clipped.
            content.HorizontalScroll.Enabled = false;
            content.HorizontalScroll.Visible = false;
            pageBody.HorizontalScroll.Enabled = false;
            pageBody.HorizontalScroll.Visible = false;

            foreach (Control c in sidebar.Controls)
            {
                if (c.GetType().Name is "StepItem" or "HelpCard")
                    c.Width = Math.Max(180, sidebar.ClientSize.Width - sidebar.Padding.Horizontal - 2);
            }

            foreach (Control c in pageBody.Controls)
                LayoutPageControl(c, Math.Max(260, pageBody.ClientSize.Width - 6));

            StyleButtons(form);
        }
        finally
        {
            form.ResumeLayout(true);
        }
    }

    private static void LayoutPageControl(Control control, int width)
    {
        if (control is FlowLayoutPanel flow)
        {
            flow.HorizontalScroll.Enabled = false;
            flow.HorizontalScroll.Visible = false;
            flow.WrapContents = false;
            flow.FlowDirection = FlowDirection.TopDown;

            foreach (Control child in flow.Controls)
            {
                child.Width = Math.Max(260, flow.ClientSize.Width - flow.Padding.Horizontal - 4);
                LayoutCard(child, child.Width);
            }
        }
        else if (control is TableLayoutPanel table)
        {
            table.AutoScroll = false;
            foreach (Control child in table.Controls)
                LayoutCard(child, Math.Max(260, child.Parent?.ClientSize.Width ?? width));
        }
        else
        {
            LayoutCard(control, width);
        }
    }

    private static void LayoutCard(Control card, int width)
    {
        if (card.GetType().Name == "RoundedCard")
        {
            card.Width = Math.Max(260, width);
            card.BackColor = Card;
        }

        var progress = card.Controls.OfType<ProgressBar>().FirstOrDefault();
        var labels = card.Controls.OfType<Label>().ToList();

        if (progress != null)
        {
            progress.Left = 54;
            progress.Width = Math.Max(120, card.ClientSize.Width - 72);
            progress.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            var nameLabel = labels.FirstOrDefault(l =>
                l.Font.Size >= 9.5f &&
                l.Text != "Overall Download Progress" &&
                l.Text != "Overall Installation Progress");

            if (nameLabel != null)
            {
                nameLabel.AutoSize = false;
                nameLabel.Left = 54;
                nameLabel.Width = Math.Max(150, card.ClientSize.Width - 74);
                nameLabel.Height = 25;
                nameLabel.MaximumSize = Size.Empty;
                nameLabel.AutoEllipsis = true;
            }

            foreach (var label in labels.Where(l => l != nameLabel))
            {
                if (label.Top >= 28 && label.Top <= 50)
                {
                    label.AutoSize = false;
                    label.Left = 54;
                    label.Width = Math.Max(100, card.ClientSize.Width - 74);
                    label.Height = 22;
                    label.AutoEllipsis = true;
                }
            }
        }

        foreach (Control child in card.Controls)
        {
            if (child is Label label && label.Text.Length > 24)
                label.MaximumSize = new Size(
                    Math.Max(120, card.ClientSize.Width - label.Left - 18), 0);
        }
    }

    private static void StyleButtons(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Border;
                b.Font = new Font("Segoe UI Semibold", 10f);
                b.Cursor = Cursors.Hand;
                b.UseVisualStyleBackColor = false;

                if (b.GetType().Name != "GradientButton")
                {
                    b.BackColor = Color.FromArgb(14, 36, 62);
                    b.ForeColor = Text;
                }

                if (b.Tag as string != "UiPolish")
                {
                    b.Tag = "UiPolish";

                    b.MouseEnter += (_, _) =>
                    {
                        b.FlatAppearance.BorderColor = Color.FromArgb(0, 170, 255);
                        if (b.GetType().Name != "GradientButton")
                            b.BackColor = Color.FromArgb(20, 55, 88);
                        b.Padding = new Padding(0, 0, 0, 2);
                    };

                    b.MouseLeave += (_, _) =>
                    {
                        b.FlatAppearance.BorderColor = Border;
                        if (b.GetType().Name != "GradientButton")
                            b.BackColor = Color.FromArgb(14, 36, 62);
                        b.Padding = Padding.Empty;
                    };

                    // Small press offset gives the normal buttons a subtle
                    // 3D/pressed feel without requiring a custom renderer.
                    b.MouseDown += (_, _) => b.Padding = new Padding(0, 2, 0, 0);
                    b.MouseUp += (_, _) => b.Padding = Padding.Empty;
                }
            }

            if (c.HasChildren)
                StyleButtons(c);
        }
    }

    private static T? GetField<T>(object obj, string name) where T : class =>
        obj.GetType()
           .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?
           .GetValue(obj) as T;
}
