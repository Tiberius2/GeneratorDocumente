using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    /// <summary>
    /// Selector unificat cu 4 categorii cromatice:
    /// Acte Adiționale (Albastru) | Decizii Suspendare (Teal) | Decizii Încetare (Rose) | Procese Verbale (Amber).
    /// </summary>
    public sealed class SelectorDialog : Form
    {
        public DocumentSelection Selection { get; private set; }

        // ── Definitii carduri ──────────────────────────────────
        private static readonly (string Cat, string Titlu, string Sub, DocumentSelection Sel, DocumentTheme Theme)[] Entries =
        {
            // Acte Adiționale
            ("ACTE ADIȚIONALE",
             "Act Adițional", "Modificare clauze contract individual de muncă",
             new DocSelection(TipDocument.ActAditional), DocumentTheme.Acte),

            // Decizii — Suspendare
            ("DECIZII — SUSPENDARE",
             "Creștere copil", "Art. 51 alin. 1 lit. a Codul Muncii",
             new DocSelection(TipDocument.SuspendareCresterecopil), DocumentTheme.Suspendare),
            ("DECIZII — SUSPENDARE",
             "Creștere copil handicap", "Art. 51 alin. 1 lit. b",
             new DocSelection(TipDocument.SuspendareCresterecopilHandicap), DocumentTheme.Suspendare),
            ("DECIZII — SUSPENDARE",
             "Absențe nemotivate", "Art. 51 alin. 2",
             new DocSelection(TipDocument.SuspendareAbsenteNemotivate), DocumentTheme.Suspendare),
            ("DECIZII — SUSPENDARE",
             "Acordul părților", "Art. 54",
             new DocSelection(TipDocument.SuspendareAcordParti), DocumentTheme.Suspendare),
            ("DECIZII — SUSPENDARE",
             "Suspendare + Încetare", "Suspendare cu clauză de încetare",
             new DocSelection(TipDocument.SuspendareSiIncetareSuspendare), DocumentTheme.Suspendare),

            // Decizii — Încetare
            ("DECIZII — ÎNCETARE",
             "Încetare suspendare", "Reluare activitate",
             new DocSelection(TipDocument.IncetareSuspendare), DocumentTheme.Incetare),
            ("DECIZII — ÎNCETARE",
             "Demisie", "Art. 81 Codul Muncii",
             new DocSelection(TipDocument.IncetareDemisie), DocumentTheme.Incetare),
            ("DECIZII — ÎNCETARE",
             "Expirare termen", "CIM pe durată determinată",
             new DocSelection(TipDocument.IncetareExpirare), DocumentTheme.Incetare),
            ("DECIZII — ÎNCETARE",
             "Disciplinar", "Art. 61 lit. a Codul Muncii",
             new DocSelection(TipDocument.IncetareDisciplinar), DocumentTheme.Incetare),
            ("DECIZII — ÎNCETARE",
             "Perioadă de probă", "Art. 31 alin. 3-4 Codul Muncii",
             new DocSelection(TipDocument.IncetarePerioadaProba), DocumentTheme.Incetare),

            // Procese Verbale
            ("PROCESE VERBALE",
             "Echipamente", "Uniformă, unelte, echipament de protecție",
             new PvSelection(TipPV.Echipamente), DocumentTheme.Pv),
            ("PROCESE VERBALE",
             "Electronice", "Laptop, telefon, tabletă, accesorii",
             new PvSelection(TipPV.Electronice), DocumentTheme.Pv),
            ("PROCESE VERBALE",
             "Autovehicul", "Predare-primire vehicul de serviciu",
             new PvSelection(TipPV.Autovehicul), DocumentTheme.Pv),
        };

        private int _selectedIndex = -1;
        private Button[] _cards;
        private Button _btnContinua;

        public SelectorDialog()
        {
            Text = "Generator Documente HR";
            Size = new Size(1400, 800);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(860, 680);
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(242, 245, 250);
            Font = new Font("Segoe UI", 10f);
            BuildUI();
        }

        private void BuildUI()
        {
            // Header neutru (nu e asociat niciunei categorii)
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(66, 76, 103) };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Generator Documente HR",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 10)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Selectează tipul documentului",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 195, 215),
                AutoSize = true,
                Location = new Point(20, 44)
            });

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 220, 235)))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            _btnContinua = new Button
            {
                Text = "Continuă  →",
                Size = new Size(140, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = ButtonPalettes.PrimaryDisabledBack,
                ForeColor = ButtonPalettes.PrimaryDisabledFore,
                Font = new Font("Segoe UI", 10f),
                Enabled = false,
                Top = 10
            };
            _btnContinua.FlatAppearance.BorderSize = 0;
            _btnContinua.MouseEnter += (s, e) => { if (_btnContinua.Enabled) _btnContinua.BackColor = ButtonPalettes.Primary.Hover; };
            _btnContinua.MouseLeave += (s, e) => { if (_btnContinua.Enabled) _btnContinua.BackColor = ButtonPalettes.Primary.Background; };
            _btnContinua.Click += (s, e) => Confirma();

            var btnAnuleaza = new Button
            {
                Text = "Anulează",
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 10f),
                Top = 10
            };
            ButtonPalettes.Secondary.ApplyTo(btnAnuleaza);
            btnAnuleaza.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnContinua, btnAnuleaza });
            pnlFooter.Resize += (s, e) =>
            {
                _btnContinua.Left = pnlFooter.Width - _btnContinua.Width - 16;
                btnAnuleaza.Left = _btnContinua.Left - btnAnuleaza.Width - 8;
            };
            _btnContinua.Left = 688; btnAnuleaza.Left = 580;

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 8),
                BackColor = Color.FromArgb(242, 245, 250)
            };

            _cards = new Button[Entries.Length];
            BuildCards(scroll);

            Controls.Add(scroll);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
            AcceptButton = _btnContinua;
            CancelButton = btnAnuleaza;
        }
        private void BuildCards(Panel scroll)
        {
            const int SepW = 3;
            const int CardH = 78;
            const int CardGap = 5;
            const int HeaderH = 44;
            const int Pad = 10;

            // Colectam categoriile in ordine
            var cats = new System.Collections.Generic.List<string>();
            foreach (var e in Entries)
                if (!cats.Contains(e.Cat)) cats.Add(e.Cat);

            int nCols = cats.Count;

            // Un panel per coloana + separatori intre ele
            var colPanels = new Panel[nCols];

            for (int ci = 0; ci < nCols; ci++)
            {
                string cat = cats[ci];
                var catTheme = Entries.First(e => e.Cat == cat).Theme;
                var catEntries = Entries.Select((e, idx) => (e, idx)).Where(x => x.e.Cat == cat).ToList();

                var col = new Panel { BackColor = Color.FromArgb(242, 245, 250) };

                // Header coloana
                var pnlHeader = new Panel
                {
                    Left = 0,
                    Top = 0,
                    Height = HeaderH,
                    BackColor = Color.White,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                };
                pnlHeader.Paint += (s, e) =>
                {
                    // bara colorata jos
                    e.Graphics.FillRectangle(new SolidBrush(catTheme.Accent), 0, pnlHeader.Height - 3, pnlHeader.Width, 3);
                };

                var lblCat = new Label
                {
                    Text = cat,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = catTheme.AccentDark,
                    AutoSize = false,
                    Location = new Point(Pad, 12),
                    Height = 20,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                };
                pnlHeader.Controls.Add(lblCat);
                pnlHeader.Resize += (s, e) => lblCat.Width = pnlHeader.Width - Pad * 2;
                col.Controls.Add(pnlHeader);

                // Cards
                int cardTop = HeaderH + 6;
                foreach (var (entry, globalIdx) in catEntries)
                {
                    var btn = BuildColumnCard(globalIdx, CardH, catTheme);
                    btn.Top = cardTop;
                    btn.Left = Pad;
                    btn.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                    col.Controls.Add(btn);
                    _cards[globalIdx] = btn;
                    cardTop += CardH + CardGap;
                }

                col.Height = cardTop + 8;
                colPanels[ci] = col;
                scroll.Controls.Add(col);
            }

            // Separatori verticali tra coloane (bara colorata stanga fiecarei coloane, din a 2-a)
            // Implementati ca border stang pe fiecare coloana via Paint

            Action relayout = () =>
            {
                int totalW = scroll.ClientSize.Width - scroll.Padding.Horizontal;
                int sepTotal = SepW * (nCols - 1);
                int colW = Math.Max((totalW - sepTotal) / nCols, 150);
                int x = 0;

                for (int ci = 0; ci < nCols; ci++)
                {
                    if (ci > 0) x += SepW;
                    var col = colPanels[ci];
                    col.SetBounds(x, 8, colW, scroll.ClientSize.Height - 16);

                    // Actualizam latimea cardurilor
                    foreach (Control c in col.Controls)
                    {
                        if (c is Panel hdr) { hdr.Width = colW; }
                        if (c is Button btn) btn.Width = colW - Pad * 2;
                    }

                    x += colW;
                }

                // Redesenam separatori
                scroll.Invalidate();
            };

            // Deseneaza separatorii intre coloane direct pe scroll
            scroll.Paint += (s, e) =>
            {
                if (colPanels[0] == null) return;
                int totalW = scroll.ClientSize.Width - scroll.Padding.Horizontal;
                int sepTotal = SepW * (nCols - 1);
                int colW = Math.Max((totalW - sepTotal) / nCols, 150);
                int x = colW;

                for (int ci = 1; ci < nCols; ci++)
                {
                    var theme = Entries.First(en => en.Cat == cats[ci]).Theme;
                    e.Graphics.FillRectangle(new SolidBrush(theme.Accent), x, 8, SepW, scroll.ClientSize.Height - 16);
                    x += SepW + colW;
                }
            };

            scroll.Resize += (s, e) => relayout();
            relayout();
        }

        private Button BuildColumnCard(int index, int cardH, DocumentTheme theme)
        {
            var (_, titlu, sub, _, _) = Entries[index];
            var btn = new Button
            {
                Height = cardH,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Tag = index
            };
            btn.FlatAppearance.BorderColor = theme.AccentBorder;
            btn.FlatAppearance.BorderSize = 1;
            btn.Paint += (s, e) => DrawCard(e.Graphics, btn, titlu, sub, theme);
            btn.Click += CardClick;
            btn.MouseEnter += (s, e) => { if ((int)btn.Tag != _selectedIndex) { btn.BackColor = theme.AccentPal; btn.Invalidate(); } };
            btn.MouseLeave += (s, e) => { if ((int)btn.Tag != _selectedIndex) { btn.BackColor = Color.White; btn.Invalidate(); } };
            btn.DoubleClick += (s, e) => { CardClick(s, e); Confirma(); };
            return btn;
        }


        private static void DrawCard(Graphics g, Button btn, string titlu, string sub, DocumentTheme theme)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool sel = (int)btn.Tag == ((SelectorDialog)btn.FindForm())._selectedIndex;

            // Bara accent sus (3px)
            g.FillRectangle(new SolidBrush(sel ? theme.Accent : theme.AccentBorder), 0, 0, btn.Width, 3);

            // Border selectie
            if (sel)
                using (var pen = new Pen(theme.Accent, 2))
                    g.DrawRectangle(pen, 1, 1, btn.Width - 3, btn.Height - 3);

            // Icon document
            int ix = 14, iy = 18;
            g.FillRectangle(new SolidBrush(theme.AccentPal), ix, iy, 20, 26);
            using (var pen = new Pen(theme.AccentBorder))
            {
                g.DrawRectangle(pen, ix, iy, 20, 26);
                for (int li = 0; li < 3; li++)
                    g.DrawLine(pen, ix + 3, iy + 7 + li * 6, ix + 17, iy + 7 + li * 6);
            }

            // Text
            using (var brMain = new SolidBrush(Color.FromArgb(25, 35, 55)))
                g.DrawString(titlu, new Font("Segoe UI", 10f, FontStyle.Bold), brMain,
                    new RectangleF(42, 10, btn.Width - 50, 22));
            using (var brSub = new SolidBrush(Color.FromArgb(90, 105, 130)))
                g.DrawString(sub, new Font("Segoe UI", 8.5f), brSub,
                    new RectangleF(42, 33, btn.Width - 50, 34));

            // Checkmark selectie
            if (sel)
            {
                g.FillEllipse(new SolidBrush(theme.Accent), btn.Width - 22, btn.Height - 22, 16, 16);
                using (var pen = new Pen(Color.White, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLines(pen, new[]
                    {
                        new Point(btn.Width - 18, btn.Height - 13),
                        new Point(btn.Width - 15, btn.Height - 10),
                        new Point(btn.Width - 9,  btn.Height - 16)
                    });
            }
        }

        private void CardClick(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            _selectedIndex = idx;
            Selection = Entries[idx].Sel;
            var theme = Entries[idx].Theme;

            foreach (var btn in _cards)
            {
                bool isSel = (int)btn.Tag == idx;
                btn.BackColor = isSel ? Entries[(int)btn.Tag].Theme.AccentPal : Color.White;
                btn.Invalidate();
            }

            _btnContinua.Enabled = true;
            _btnContinua.BackColor = ButtonPalettes.Primary.Background;
            _btnContinua.ForeColor = ButtonPalettes.Primary.Foreground;
            _btnContinua.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _btnContinua.Cursor = Cursors.Hand;
        }

        private void Confirma()
        {
            if (Selection == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}