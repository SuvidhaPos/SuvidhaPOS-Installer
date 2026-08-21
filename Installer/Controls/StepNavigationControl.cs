using System.Drawing;
using System.Windows.Forms;

namespace SuvidhaPOSInstaller.DynamicUI.Controls;

public sealed class StepNavigationControl : UserControl
{
    public event EventHandler? NextClicked;
    public event EventHandler? BackClicked;
    public event EventHandler? CancelClicked;

    private readonly Button next = new();
    private readonly Button back = new();
    private readonly Button cancel = new();
    private readonly Label step = new();
    private readonly ProgressBar progress = new();

    public StepNavigationControl()
    {
        Height = 72;
        Dock = DockStyle.Bottom;
        BackColor = Color.FromArgb(3, 13, 24);
        Padding = new Padding(14, 8, 14, 8);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
        Controls.Add(table);

        step.ForeColor = Color.WhiteSmoke;
        step.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        step.Dock = DockStyle.Fill;
        step.TextAlign = ContentAlignment.MiddleLeft;
        step.AutoEllipsis = true;
        table.Controls.Add(step, 0, 0);

        progress.Minimum = 0;
        progress.Maximum = 100;
        progress.Dock = DockStyle.Fill;
        progress.Margin = new Padding(6, 12, 6, 12);
        table.Controls.Add(progress, 1, 0);

        cancel.Text = "Cancel";
        back.Text = "‹  Back";
        next.Text = "Next  →";
        foreach (var b in new[] { cancel, back, next })
        {
            b.Dock = DockStyle.Fill;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(12, 68, 112);
            b.BackColor = Color.FromArgb(5, 20, 34);
            b.ForeColor = Color.WhiteSmoke;
            b.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.Margin = new Padding(4, 3, 4, 3);
        }
        next.BackColor = Color.FromArgb(0, 122, 255);

        table.Controls.Add(new Label { Dock = DockStyle.Fill }, 2, 0);
        table.Controls.Add(cancel, 3, 0);
        table.Controls.Add(back, 4, 0);
        table.Controls.Add(next, 5, 0);

        cancel.Click += (_, _) => CancelClicked?.Invoke(this, EventArgs.Empty);
        back.Click += (_, _) => BackClicked?.Invoke(this, EventArgs.Empty);
        next.Click += (_, _) => NextClicked?.Invoke(this, EventArgs.Empty);
    }

    public void SetStep(int current, int total)
    {
        step.Text = $"Step {current} of {total}";
        progress.Value = Math.Clamp(current * 100 / Math.Max(1, total), 0, 100);
        back.Enabled = current > 1;
        next.Text = current == total ? "✓  Finish" : "Next  →";
    }

    public void SetBusy(bool busy) => next.Enabled = back.Enabled = cancel.Enabled = !busy;
}