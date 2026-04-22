using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    public class DocumentSelectorDialog : Form
    {
        public TipDocument TipSelectat { get; private set; }

        // ── Definitii carduri ─────────────────────────────────
        private static readonly (string Titlu, string Subtitlu, TipDocument Tip, string Categorie)[] Documente =
        {
            ("Act Adițional",          "Modificare clauze CIM",              TipDocument.ActAditional,                    "Act"),
            ("Creștere copil",         "Suspendare CIM — art. 51 alin. 1",   TipDocument.SuspendareCresterecopil,         "Suspendare"),
            ("Creștere copil handicap","Suspendare CIM — copil cu dizabilități", TipDocument.SuspendareCresterecopilHandicap, "Suspendare"),
            ("Absențe nemotivate",     "Suspendare CIM — art. 51 alin. 2",   TipDocument.SuspendareAbsenteNemotivate,     "Suspendare"),
            ("Acordul părților",       "Suspendare CIM — art. 54",           TipDocument.SuspendareAcordParti,            "Suspendare"),
            ("Suspendare + Încetare",  "Suspendare și încetare suspendare",  TipDocument.SuspendareSiIncetareSuspendare,  "Suspendare"),
            ("Încetare suspendare",    "Reluare activitate",                 TipDocument.IncetareSuspendare,              "Încetare"),
            ("Demisie",                "Încetare CIM — art. 81",             TipDocument.IncetareDemisie,                 "Încetare"),
            ("Expirare termen",        "Încetare CIM — art. 56",             TipDocument.IncetareExpirare,                "Încetare"),
            ("Concediere disciplinară","Încetare CIM — art. 61 lit. a",      TipDocument.IncetareDisciplinar,             "Încetare"),
        };

        private static readonly Color CuloareAct = Color.FromArgb(63, 129, 198);
        private static readonly Color CuloareSuspendare = Color.FromArgb(34, 120, 74);
        private static readonly Color CuloareIncetare = Color.FromArgb(160, 55, 35);

        private TipDocument? _selectat = null;
        private Button[] _cardButtons;
        private Button _btnContinua;

        public DocumentSelectorDialog()
        {
            Text = "Generator documente HR";
            Size = new Size(780, 580);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(242, 245, 250);
            Font = new Font("Segoe UI", 10f);

            BuildUI();
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(63, 129, 198)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Generator documente HR",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Selectează tipul documentului pe care vrei să îl generezi",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 210, 240),
                AutoSize = true,
                Location = new Point(20, 44)
            });

            // ── Footer ────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White
            };
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
                BackColor = Color.FromArgb(180, 200, 225),
                ForeColor = Color.FromArgb(190, 195, 200),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Enabled = false,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnContinua.FlatAppearance.BorderSize = 0;
            _btnContinua.Click += (s, e) => Confirma();

            var btnCancel = new Button
            {
                Text = "Anulează",
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnContinua, btnCancel });
            pnlFooter.Resize += (s, e) =>
            {
                _btnContinua.Left = pnlFooter.Width - _btnContinua.Width - 16;
                btnCancel.Left = _btnContinua.Left - btnCancel.Width - 8;
            };
            _btnContinua.Left = 608;
            btnCancel.Left = 500;

            // ── Grid carduri ──────────────────────────────────
            var pnlScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.FromArgb(242, 245, 250)
            };

            _cardButtons = new Button[Documente.Length];
            int cardW = 218;
            int cardH = 88;
            int cols = 3;
            int gapX = 12;
            int gapY = 12;

            for (int i = 0; i < Documente.Length; i++)
            {
                var doc = Documente[i];
                int col = i % cols;
                int row = i / cols;
                int x = 16 + col * (cardW + gapX);
                int y = 12 + row * (cardH + gapY);

                var btn = CreateCard(doc.Titlu, doc.Subtitlu, doc.Categorie, x, y, cardW, cardH, i);
                _cardButtons[i] = btn;
                pnlScroll.Controls.Add(btn);
            }

            Controls.Add(pnlScroll);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            AcceptButton = _btnContinua;
            CancelButton = btnCancel;
        }

        private Button CreateCard(string titlu, string subtitlu, string categorie,
            int x, int y, int w, int h, int index)
        {
            Color catColor = categorie == "Act" ? CuloareAct
                           : categorie == "Suspendare" ? CuloareSuspendare
                                                       : CuloareIncetare;

            var btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(25, 35, 55),
                Cursor = Cursors.Hand,
                Tag = index,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(210, 220, 235);
            btn.FlatAppearance.BorderSize = 1;

            btn.Paint += (s, e) => DrawCard(e.Graphics, btn, titlu, subtitlu, categorie, catColor);
            btn.Click += CardClick;
            btn.MouseEnter += (s, e) => { if (_selectat == null || (int)btn.Tag != GetSelectedIndex()) { btn.BackColor = Color.FromArgb(245, 248, 255); btn.Invalidate(); } };
            btn.MouseLeave += (s, e) => { if (_selectat == null || (int)btn.Tag != GetSelectedIndex()) { btn.BackColor = Color.White; btn.Invalidate(); } };

            return btn;
        }

        private void DrawCard(Graphics g, Button btn, string titlu, string subtitlu,
            string categorie, Color catColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool sel = _selectat.HasValue && (int)btn.Tag == GetSelectedIndex();

            // Bordura selectata
            if (sel)
            {
                using (var pen = new Pen(catColor, 2))
                    g.DrawRectangle(pen, 1, 1, btn.Width - 3, btn.Height - 3);
            }

            // Bara colorata sus (categorie)
            g.FillRectangle(new SolidBrush(catColor), 0, 0, btn.Width, 4);

            // Badge categorie — colț dreapta jos, sub conținut
            var badgeFont = new Font("Segoe UI", 7f, FontStyle.Bold);
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
            var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.DrawString(titlu, titleFont, new SolidBrush(Color.FromArgb(25, 35, 55)),
                new RectangleF(44, 14, btn.Width - 52, 22));

            // Subtitlu
            var subFont = new Font("Segoe UI", 8f);
            g.DrawString(subtitlu, subFont, new SolidBrush(Color.FromArgb(100, 110, 130)),
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

        private void CardClick(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            int idx = (int)btn.Tag;
            _selectat = Documente[idx].Tip;

            // Redesenam toate cardurile
            foreach (var b in _cardButtons)
            {
                b.BackColor = (int)b.Tag == idx ? Color.FromArgb(245, 248, 255) : Color.White;
                b.Invalidate();
            }

            _btnContinua.Enabled = true;
            _btnContinua.BackColor = Color.FromArgb(63, 129, 198);
            _btnContinua.ForeColor = Color.White;
            _btnContinua.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        }

        private int GetSelectedIndex()
        {
            if (!_selectat.HasValue) return -1;
            for (int i = 0; i < Documente.Length; i++)
                if (Documente[i].Tip == _selectat.Value) return i;
            return -1;
        }

        private void Confirma()
        {
            if (!_selectat.HasValue) return;
            TipSelectat = _selectat.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}