using System.Drawing;
using System.Windows.Forms;

namespace ActAditionalPlugin.UI
{
    public class ButtonTheme
    {
        public Color Background { get; }
        public Color Foreground { get; }
        public Color Border { get; }
        public Color Hover { get; }
        public Color Pressed { get; }

        public ButtonTheme(Color bg, Color fg, Color border, Color hover, Color pressed)
        {
            Background = bg; Foreground = fg; Border = border; Hover = hover; Pressed = pressed;
        }

        /// <summary>Applies palette + hover/press handlers to a button. Call once per button.</summary>
        public void ApplyTo(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Background;
            btn.ForeColor = Foreground;
            btn.Cursor = Cursors.Hand;
            if (Border == Color.Transparent)
            {
                btn.FlatAppearance.BorderSize = 0;
            }
            else
            {
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.BorderColor = Border;
            }
            btn.MouseEnter += (s, e) => ((Button)s).BackColor = Hover;
            btn.MouseLeave += (s, e) => ((Button)s).BackColor = Background;
            btn.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) ((Button)s).BackColor = Pressed; };
            btn.MouseUp += (s, e) =>
                ((Button)s).BackColor = ((Button)s).ClientRectangle.Contains(e.Location) ? Hover : Background;
        }
    }

    public static class ButtonPalettes
    {
        /// <summary>Dark navy — main actions: Generate, Continue, Add.</summary>
        public static readonly ButtonTheme Primary = new ButtonTheme(
            bg: Color.FromArgb(38, 74, 120),
            fg: Color.White,
            border: Color.Transparent,
            hover: Color.FromArgb(55, 96, 152),
            pressed: Color.FromArgb(28, 54, 92)
        );

        /// <summary>Soft blue-gray — Preview, Cancel, Back.</summary>
        public static readonly ButtonTheme Secondary = new ButtonTheme(
            bg: Color.FromArgb(232, 237, 248),
            fg: Color.FromArgb(38, 74, 120),
            border: Color.FromArgb(190, 205, 228),
            hover: Color.FromArgb(212, 220, 240),
            pressed: Color.FromArgb(195, 208, 232)
        );

        /// <summary>Ghost — text-only, no fill.</summary>
        public static readonly ButtonTheme Tertiary = new ButtonTheme(
            bg: Color.Transparent,
            fg: Color.FromArgb(38, 74, 120),
            border: Color.Transparent,
            hover: Color.FromArgb(232, 237, 248),
            pressed: Color.FromArgb(212, 220, 240)
        );

        // Disabled appearance for _btnContinua before a card is selected
        public static readonly Color PrimaryDisabledBack = Color.FromArgb(178, 198, 222);
        public static readonly Color PrimaryDisabledFore = Color.FromArgb(148, 168, 192);
    }
}