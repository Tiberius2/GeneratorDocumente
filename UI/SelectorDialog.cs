using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    /// <summary>
    /// Selector unificat pentru toate tipurile de documente HR:
    /// Acte Aditionale | Decizii Suspendare | Decizii Incetare | Procese Verbale.
    /// </summary>
    public sealed class SelectorDialog : Form
    {
        public DocumentSelection Selection { get; private set; }

        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        private static readonly Color FundalForm = Color.FromArgb(242, 245, 250);
        private static readonly Color TextGri = Color.FromArgb(80, 100, 130);

        // Toate tipurile de documente grupate pe categorii, in ordinea afisarii
        private static readonly (string Cat, string Titlu, string Sub, DocumentSelection Sel)[] Entries =
        {
            // ── Acte ──
            ("ACTE ADIȚIONALE",
             "Act Adițional", "Modificare clauze contract individual de muncă",
             new DocSelection(TipDocument.ActAditional)),

            // ── Decizii Suspendare ──
            ("DECIZII — SUSPENDARE",
             "Creștere copil", "Art. 51 alin. 1 lit. a Codul Muncii",
             new DocSelection(TipDocument.SuspendareCresterecopil)),
            ("DECIZII — SUSPENDARE",
             "Creștere copil handicap", "Art. 51 alin. 1 lit. b",
             new DocSelection(TipDocument.SuspendareCresterecopilHandicap)),
            ("DECIZII — SUSPENDARE",
             "Absențe nemotivate", "Art. 51 alin. 2",
             new DocSelection(TipDocument.SuspendareAbsenteNemotivate)),
            ("DECIZII — SUSPENDARE",
             "Acordul părților", "Art. 54",
             new DocSelection(TipDocument.SuspendareAcordParti)),
            ("DECIZII — SUSPENDARE",
             "Suspendare + Încetare", "Suspendare cu clauză de încetare",
             new DocSelection(TipDocument.SuspendareSiIncetareSuspendare)),

            // ── Decizii Incetare ──
            ("DECIZII — ÎNCETARE",
             "Încetare suspendare", "Reluare activitate",
             new DocSelection(TipDocument.IncetareSuspendare)),
            ("DECIZII — ÎNCETARE",
             "Demisie", "Art. 81 Codul Muncii",
             new DocSelection(TipDocument.IncetareDemisie)),
            ("DECIZII — ÎNCETARE",
             "Expirare termen", "CIM pe durată determinată",
             new DocSelection(TipDocument.IncetareExpirare)),
            ("DECIZII — ÎNCETARE",
             "Disciplinar", "Art. 61 lit. a Codul Muncii",
             new DocSelection(TipDocument.IncetareDisciplinar)),

            // ── Procese Verbale ──
            ("PROCESE VERBALE",
             "Echipamente", "Uniformă, unelte, echipament de protecție",
             new PvSelection(TipPV.Echipamente)),
            ("PROCESE VERBALE",
             "Electronice", "Laptop, telefon, tabletă, accesorii",
             new PvSelection(TipPV.Electronice)),
            ("PROCESE VERBALE",
             "Autovehicul", "Predare-primire vehicul de serviciu",
             new PvSelection(TipPV.Autovehicul)),
        };

        private int _selectedIndex = -1;
        private Button[] _cards;
        private Button _btnContinua;

        public SelectorDialog()
        {
            Text = "Generator Documente HR";
            Size = new Size(860, 680);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(720, 560);
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = FundalForm;
            Font = new Font("Segoe UI", 10f);
            BuildUI();
        }

        private void BuildUI()
        {
            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Albastru };
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
                ForeColor = Color.FromArgb(200, 225, 250),
                AutoSize = true,
                Location = new Point(20, 44)
            });

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(180, 205, 235)))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            _btnContinua = new Button
            {
                Text = "Continuă  →",
                Size = new Size(140, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(190, 210, 230),
                ForeColor = Color.FromArgb(160, 175, 195),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Default,
                Enabled = false,
                Top = 10
            };
            _btnContinua.FlatAppearance.BorderSize = 0;
            _btnContinua.Click += (s, e) => Confirma();

            var btnAnuleaza = new Button
            {
                Text = "Anulează",
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Top = 10
            };
            btnAnuleaza.FlatAppearance.BorderSize = 0;
            btnAnuleaza.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnContinua, btnAnuleaza });
            pnlFooter.Resize += (s, e) =>
            {
                _btnContinua.Left = pnlFooter.Width - _btnContinua.Width - 16;
                btnAnuleaza.Left = _btnContinua.Left - btnAnuleaza.Width - 8;
            };
            _btnContinua.Left = 688; btnAnuleaza.Left = 580;

            // Body scrollabil
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 8),
                BackColor = FundalForm
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
            const int CardW = 250, CardH = 90, GapX = 12, GapY = 8;
            int y = 8, i = 0;

            while (i < Entries.Length)
            {
                string cat = Entries[i].Cat;

                // Label categorie
                scroll.Controls.Add(new Label
                {
                    Text = cat,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = TextGri,
                    AutoSize = true,
                    Left = 4,
                    Top = y
                });
                y += 24;

                // Carduri 3 pe rand
                int catStart = i;
                while (i < Entries.Length && Entries[i].Cat == cat) i++;
                int catCount = i - catStart;

                for (int j = 0; j < catCount; j++)
                {
                    int idx = catStart + j;
                    int col = j % 3, row = j / 3;
                    var btn = BuildCard(idx, 4 + col * (CardW + GapX), y + row * (CardH + GapY), CardW, CardH);
                    _cards[idx] = btn;
                    scroll.Controls.Add(btn);
                }

                y += ((catCount + 2) / 3) * (CardH + GapY) + 20;
            }
        }

        private Button BuildCard(int index, int x, int y, int w, int h)
        {
            var (_, titlu, sub, _) = Entries[index];
            var btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Tag = index
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(210, 220, 235);
            btn.FlatAppearance.BorderSize = 1;
            btn.Paint += (s, e) => DrawCard(e.Graphics, btn, titlu, sub);
            btn.Click += CardClick;
            btn.MouseEnter += (s, e) => { if ((int)btn.Tag != _selectedIndex) { btn.BackColor = Color.FromArgb(248, 250, 255); btn.Invalidate(); } };
            btn.MouseLeave += (s, e) => { if ((int)btn.Tag != _selectedIndex) { btn.BackColor = Color.White; btn.Invalidate(); } };
            btn.DoubleClick += (s, e) => { CardClick(s, e); Confirma(); };
            return btn;
        }

        private void DrawCard(Graphics g, Button btn, string titlu, string sub)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool sel = (int)btn.Tag == _selectedIndex;

            // Bara accent sus
            g.FillRectangle(new SolidBrush(sel ? Albastru : Color.FromArgb(180, 205, 235)), 0, 0, btn.Width, 3);

            // Border selectie
            if (sel)
                using (var pen = new Pen(Albastru, 2))
                    g.DrawRectangle(pen, 1, 1, btn.Width - 3, btn.Height - 3);

            // Icon document (simplu)
            int ix = 14, iy = 18;
            var icoColor = Color.FromArgb(sel ? 40 : 20, Albastru.R, Albastru.G, Albastru.B);
            g.FillRectangle(new SolidBrush(icoColor), ix, iy, 20, 26);
            using (var pen = new Pen(Color.FromArgb(150, Albastru.R, Albastru.G, Albastru.B)))
            {
                g.DrawRectangle(pen, ix, iy, 20, 26);
                for (int li = 0; li < 3; li++)
                    g.DrawLine(pen, ix + 3, iy + 7 + li * 6, ix + 17, iy + 7 + li * 6);
            }

            // Text
            using (var brMain = new SolidBrush(Color.FromArgb(25, 35, 55)))
                g.DrawString(titlu, new Font("Segoe UI", 9f, FontStyle.Bold), brMain,
                    new RectangleF(42, 14, btn.Width - 50, 22));
            using (var brSub = new SolidBrush(Color.FromArgb(90, 105, 130)))
                g.DrawString(sub, new Font("Segoe UI", 7.5f), brSub,
                    new RectangleF(42, 36, btn.Width - 50, 38));

            // Checkmark daca selectat
            if (sel)
            {
                g.FillEllipse(new SolidBrush(Albastru), btn.Width - 22, btn.Height - 22, 16, 16);
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

            foreach (var btn in _cards)
            {
                btn.BackColor = (int)btn.Tag == idx ? Color.FromArgb(245, 248, 255) : Color.White;
                btn.Invalidate();
            }

            _btnContinua.Enabled = true;
            _btnContinua.BackColor = Albastru;
            _btnContinua.ForeColor = Color.White;
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