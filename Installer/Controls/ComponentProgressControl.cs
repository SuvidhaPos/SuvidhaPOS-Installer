using System.Drawing;
using System.Windows.Forms;
using SuvidhaPOSInstaller.DynamicUI.Helpers;
using SuvidhaPOSInstaller.DynamicUI.Models;

namespace SuvidhaPOSInstaller.DynamicUI.Controls;

public sealed class ComponentProgressControl : UserControl
{
    private readonly Label name = new();
    private readonly Label description = new();
    private readonly Label size = new();
    private readonly Label status = new();
    private readonly Label percent = new();
    private readonly ProgressBar progress = new();

    public ComponentProgressControl()
    {
        Height = 78;
        Dock = DockStyle.Top;
        BackColor = Color.FromArgb(4, 17, 29);
        Padding = new Padding(12);
        Margin = new Padding(0, 0, 0, 6);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(grid);

        name.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        name.ForeColor = Color.WhiteSmoke;
        name.Dock = DockStyle.Fill;
        name.AutoEllipsis = true;
        grid.Controls.Add(name, 0, 0);
        grid.SetColumnSpan(name, 2);

        size.ForeColor = Color.WhiteSmoke;
        size.Dock = DockStyle.Fill;
        size.TextAlign = ContentAlignment.MiddleRight;
        size.AutoEllipsis = true;
        grid.Controls.Add(size, 2, 0);

        status.ForeColor = Color.FromArgb(0, 150, 255);
        status.Dock = DockStyle.Fill;
        status.AutoEllipsis = true;
        grid.Controls.Add(status, 3, 0);

        percent.ForeColor = Color.FromArgb(0, 150, 255);
        percent.Dock = DockStyle.Fill;
        percent.TextAlign = ContentAlignment.MiddleRight;
        grid.Controls.Add(percent, 4, 0);

        description.Font = new Font("Segoe UI", 9);
        description.ForeColor = Color.FromArgb(190, 205, 220);
        description.Dock = DockStyle.Fill;
        description.AutoEllipsis = true;
        grid.Controls.Add(description, 0, 1);
        grid.SetColumnSpan(description, 2);

        progress.Minimum = 0;
        progress.Maximum = 100;
        progress.Dock = DockStyle.Fill;
        progress.Margin = new Padding(8, 4, 0, 4);
        grid.Controls.Add(progress, 2, 1);
        grid.SetColumnSpan(progress, 3);
    }

    public void SetData(InstallerComponent item)
    {
        name.Text = item.Name;
        description.Text = item.Description;
        size.Text = FormatHelper.Size(item.SizeBytes);
        status.Text = item.Status;
        percent.Text = $"{item.Progress}%";
        progress.Value = Math.Clamp(item.Progress, 0, 100);
        var ok = item.Status is "Installed" or "Downloaded" or "Ready";
        status.ForeColor = ok ? Color.LimeGreen : Color.FromArgb(0, 150, 255);
        percent.ForeColor = ok ? Color.LimeGreen : Color.FromArgb(0, 150, 255);
    }
}