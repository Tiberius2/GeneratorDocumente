using System.Drawing;

namespace ActAditionalPlugin.UI
{
    internal static class Theme
    {
        // Form
        public static readonly Color BackgroundColor = Color.FromArgb(242, 245, 250);
        public static readonly Font FormFont = new Font("Segoe UI", 10f);

        // Header
        public static readonly Color HeaderColor = Color.FromArgb(63, 129, 198);
        public static readonly Font HeaderFont = new Font("Segoe UI", 15f, FontStyle.Bold);
        public static readonly Font HeaderSubFont = new Font("Segoe UI", 9f);
        public static readonly Color HeaderSubColor = Color.FromArgb(180, 210, 240);

        // Footer
        public static readonly Color FooterLineColor = Color.FromArgb(210, 220, 235);

        // Buttons
        public static readonly Size PrimaryButtonSize = new Size(140, 36);
        public static readonly Font PrimaryButtonFont = new Font("Segoe UI", 10f, FontStyle.Regular);
        public static readonly Color PrimaryButtonBackDisabled = Color.FromArgb(180, 200, 225);
        public static readonly Color PrimaryButtonForeDisabled = Color.FromArgb(190, 195, 200);
        public static readonly Color PrimaryButtonBack = Color.FromArgb(63, 129, 198);
        public static readonly Color PrimaryButtonFore = Color.White;

        public static readonly Size CancelButtonSize = new Size(100, 36);
        public static readonly Font CancelButtonFont = new Font("Segoe UI", 10f);
        public static readonly Color CancelButtonBack = Color.FromArgb(240, 242, 246);
        public static readonly Color CancelButtonFore = Color.FromArgb(60, 80, 110);

        // Card layout
        public const int CardWidth = 218;
        public const int CardHeight = 88;
        public const int CardCols = 3;
        public const int CardGapX = 12;
        public const int CardGapY = 12;

        // Card visuals
        public static readonly Color CardBorderColor = Color.FromArgb(210, 220, 235);
        public static readonly Color CardHoverBack = Color.FromArgb(245, 248, 255);
        public static readonly Color CardTextColor = Color.FromArgb(25, 35, 55);
        public static readonly Color CardSubtitleColor = Color.FromArgb(100, 110, 130);

        public static readonly Font CardTitleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font CardSubtitleFont = new Font("Segoe UI", 8f);
        public static readonly Font CardBadgeFont = new Font("Segoe UI", 7f, FontStyle.Bold);

        // Category colors
        public static readonly Color CardColorAct = Color.FromArgb(63, 129, 198);
        public static readonly Color CardColorSuspendare = Color.FromArgb(34, 120, 74);
        public static readonly Color CardColorIncetare = Color.FromArgb(160, 55, 35);
        public static readonly Color CardColorPv = Color.FromArgb(63, 129, 198);
    }
}
