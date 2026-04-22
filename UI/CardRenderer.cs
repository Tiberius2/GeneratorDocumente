using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ActAditionalPlugin.UI
{
    internal static class CardRenderer
    {
        public static void Render(Graphics g, Button btn, CardItem card, bool sel)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var categorie = card?.Category ?? string.Empty;
            Color catColor = categorie == "Act" ? Theme.CardColorAct
                           : categorie == "Suspendare" ? Theme.CardColorSuspendare
                           : categorie == "Incetare" ? Theme.CardColorIncetare
                           : categorie == "Decizie" ? Theme.CardColorSuspendare
                           : categorie == "PV" ? Theme.CardColorPv
                           : Theme.CardColorAct;

            // Bordura selectata
            if (sel)
            {
                using (var pen = new Pen(catColor, 2))
                    g.DrawRectangle(pen, 1, 1, btn.Width - 3, btn.Height - 3);
            }

            // Bara colorata sus (categorie)
            g.FillRectangle(new SolidBrush(catColor), 0, 0, btn.Width, 4);

            // Badge categorie — colț dreapta jos, sub conținut
            var badgeFont = Theme.CardBadgeFont;
            var badgeSize = g.MeasureString(categorie, badgeFont);
            float bw = badgeSize.Width + 10;
            float bh = badgeSize.Height + 3;
            float bx = btn.Width - bw - 6;
            float by = btn.Height - bh - 6;
            using (var bgBrush = new SolidBrush(Color.FromArgb(25, catColor.R, catColor.G, catColor.B)))
                g.FillRectangle(bgBrush, bx, by, bw, bh);
            using (var borderPen = new Pen(Color.FromArgb(80, catColor.R, catColor.G, catColor.B), 1))
                g.DrawRectangle(borderPen, bx, by, bw, bh);
            g.DrawString(categorie, badgeFont, new SolidBrush(catColor), bx + 5, by + 2);

            // Icon document
            int ix = 14, iy = 16;
            g.FillRectangle(new SolidBrush(Color.FromArgb(sel ? 30 : 15, catColor.R, catColor.G, catColor.B)),
                ix, iy, 22, 28);
            g.DrawRectangle(new Pen(catColor, 1), ix, iy, 22, 28);
            for (int li = 0; li < 3; li++)
                g.DrawLine(new Pen(Color.FromArgb(150, catColor.R, catColor.G, catColor.B)),
                    ix + 4, iy + 8 + li * 6, ix + 18, iy + 8 + li * 6);

            // Titlu
            g.DrawString(card?.Title ?? string.Empty, Theme.CardTitleFont, new SolidBrush(Theme.CardTextColor),
                new RectangleF(44, 14, btn.Width - 52, 22));

            // Subtitlu
            g.DrawString(card?.Subtitle ?? string.Empty, Theme.CardSubtitleFont, new SolidBrush(Theme.CardSubtitleColor),
                new RectangleF(44, 38, btn.Width - 52, 36));

            // Checkmark daca selectat
            if (sel)
            {
                g.FillEllipse(new SolidBrush(catColor), btn.Width - 24, btn.Height - 24, 18, 18);
                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLines(pen, new[]
                    {
                        new Point(btn.Width - 20, btn.Height - 14),
                        new Point(btn.Width - 17, btn.Height - 11),
                        new Point(btn.Width - 10, btn.Height - 18)
                    });
                }
            }
        }
    }
}
