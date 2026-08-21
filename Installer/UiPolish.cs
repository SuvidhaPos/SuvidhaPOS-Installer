using System.Drawing;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

internal static class UiPolish
{
    private static readonly Color Border = Color.FromArgb(18, 65, 111);
    private static readonly Color Text = Color.FromArgb(244, 247, 252);
    private static readonly Color ButtonBack = Color.FromArgb(14, 36, 62);
    private static readonly Color ButtonHover = Color.FromArgb(20, 55, 88);
    private static readonly Color Cyan = Color.FromArgb(0, 211, 255);

    public static void Apply(MainForm form)
    {
        // IMPORTANT:
        // No timer.
        // No repeated control repositioning.
        // MainForm owns the responsive layout.
        form.Shown += (_, _) => StyleButtons(form);
    }

    private static void StyleButtons(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize =
                    button.GetType().Name == "GradientButton" ? 0 : 1;

                button.FlatAppearance.BorderColor = Border;
                button.Font = new Font("Segoe UI Semibold", 10F);
                button.Cursor = Cursors.Hand;
                button.UseVisualStyleBackColor = false;

                if (button.GetType().Name != "GradientButton")
                {
                    button.BackColor = ButtonBack;
                    button.ForeColor = Text;
                }

                if (!Equals(button.Tag, "UiPolishApplied"))
                {
                    button.Tag = "UiPolishApplied";

                    button.MouseEnter += (_, _) =>
                    {
                        button.FlatAppearance.BorderColor = Cyan;

                        if (button.GetType().Name != "GradientButton")
                            button.BackColor = ButtonHover;
                    };

                    button.MouseLeave += (_, _) =>
                    {
                        button.FlatAppearance.BorderColor = Border;

                        if (button.GetType().Name != "GradientButton")
                            button.BackColor = ButtonBack;
                    };
                }
            }

            if (control.HasChildren)
                StyleButtons(control);
        }
    }
}
