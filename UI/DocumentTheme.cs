using System.Drawing;

namespace ActAditionalPlugin.Models
{
    /// <summary>Paleta de culori per categorie de document. Folosita in formulare si selector.</summary>
    public sealed class DocumentTheme
    {
        public Color Accent { get; }
        public Color AccentPal { get; }   // fundal deschis
        public Color AccentBorder { get; }   // bordura
        public Color AccentDark { get; }   // hover / activ

        private DocumentTheme(Color accent, Color accentPal, Color accentBorder, Color accentDark)
        {
            Accent = accent; AccentPal = accentPal;
            AccentBorder = accentBorder; AccentDark = accentDark;
        }

        // ── Palete ────────────────────────────────────────────
        /// <summary>Albastru — Acte Adiționale</summary>
        public static readonly DocumentTheme Acte = new DocumentTheme(
            Color.FromArgb(63, 129, 198),
            Color.FromArgb(235, 241, 251),
            Color.FromArgb(180, 205, 235),
            Color.FromArgb(44, 103, 168));

        /// <summary>Teal — Decizii Suspendare</summary>
        public static readonly DocumentTheme Suspendare = new DocumentTheme(
            Color.FromArgb(32, 158, 145),
            Color.FromArgb(228, 246, 244),
            Color.FromArgb(160, 215, 210),
            Color.FromArgb(22, 128, 117));

        /// <summary>Rose — Decizii Încetare</summary>
        public static readonly DocumentTheme Incetare = new DocumentTheme(
            Color.FromArgb(192, 72, 68),
            Color.FromArgb(252, 234, 234),
            Color.FromArgb(220, 175, 175),
            Color.FromArgb(158, 50, 47));

        /// <summary>Amber — Procese Verbale</summary>
        public static readonly DocumentTheme Pv = new DocumentTheme(
            Color.FromArgb(192, 120, 30),
            Color.FromArgb(254, 243, 226),
            Color.FromArgb(225, 190, 140),
            Color.FromArgb(158, 94, 18));

        // ── Factory ───────────────────────────────────────────
        public static DocumentTheme For(TipDocument tip)
        {
            switch (tip)
            {
                case TipDocument.ActAditional:
                    return Acte;

                case TipDocument.SuspendareCresterecopil:
                case TipDocument.SuspendareCresterecopilHandicap:
                case TipDocument.SuspendareAbsenteNemotivate:
                case TipDocument.SuspendareAcordParti:
                case TipDocument.SuspendareSiIncetareSuspendare:
                    return Suspendare;

                default: // toate IncetareX
                    return Incetare;
            }
        }

        public static DocumentTheme For(TipPV tip) => Pv;
    }
}