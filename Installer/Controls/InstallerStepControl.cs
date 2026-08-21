using System.Drawing;
using System.Windows.Forms;

namespace SuvidhaPOSInstaller.DynamicUI.Controls;

public abstract class InstallerStepControl : UserControl
{
    protected static readonly Color Bg = Color.FromArgb(3, 13, 24);
    protected static readonly Color PanelBg = Color.FromArgb(5, 20, 34);
    protected static readonly Color Border = Color.FromArgb(12, 68, 112);
    protected static readonly Color Blue = Color.FromArgb(0, 122, 255);
    protected static readonly Color Green = Color.FromArgb(40, 220, 100);
    protected static readonly Color White = Color.WhiteSmoke;
    protected static readonly Color Muted = Color.FromArgb(190, 205, 220);

    protected InstallerStepControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Bg;
        AutoScroll = true;
        Padding = new Padding(0, 6, 0, 6);
        Margin = Padding.Empty;
    }

    protected Label Label(string text, int size = 12, bool bold = false)
    {
        return new Label
        {
            Text = text,
            ForeColor = White,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = Math.Max(28, size + 16),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6),
            AutoEllipsis = true
        };
    }

    protected Panel Card()
    {
        return new Panel
        {
            BackColor = PanelBg,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 12),
            Dock = DockStyle.Top
        };
    }

    protected void StyleButton(Button b, bool primary = true)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = primary ? Blue : Border;
        b.BackColor = primary ? Blue : PanelBg;
        b.ForeColor = White;
        b.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        b.Height = 42;
        b.Cursor = Cursors.Hand;
        b.UseVisualStyleBackColor = false;
    }
}