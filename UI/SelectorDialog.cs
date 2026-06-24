using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    // ══════════════════════════════════════════════════════════
    //  SELECTOR DIALOG
    //  Construit dinamic din DocumentRegistry.GetCategories().
    //  Nu mai are nicio referinta hardcodata la tipuri de documente.
    // ══════════════════════════════════════════════════════════
    public sealed class SelectorDialog : Form
    {
        // ── Output ────────────────────────────────────────────
        public DocumentDefinition SelectedDocument { get; private set; }
        public PersonInfo SelectedPerson { get; private set; }

        // ── State intern ──────────────────────────────────────
        private readonly List<PersonInfo> _persoane;
        private readonly int _currentPrsnId;
        private Button _btnAngajat;
        private Button _btnContinua;
        private Button _selectedCard;
        private readonly List<Button> _allCards = new List<Button>();

        // ── Culori ────────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(66, 76, 103);
        private static readonly Color BgForm = Color.FromArgb(242, 245, 250);

        // ══════════════════════════════════════════════════════
        //  Constructor
        // ══════════════════════════════════════════════════════
        public SelectorDialog(List<PersonInfo> persoane, int currentPrsnId = 0)
        {
            _persoane = persoane ?? new List<PersonInfo>();
            _currentPrsnId = currentPrsnId;

            // Pre-selecteaza angajatul curent
            SelectedPerson = _persoane.FirstOrDefault(p => p.PrsnId == currentPrsnId);

            Text = "Generator Documente HR";
            Size = new Size(1400, 800);
            MinimumSize = new Size(900, 600);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgForm;
            Font = new Font("Segoe UI", 10f);

            BuildUI();
        }

        // ══════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = BgDark
            };

            var lblAlege = new Label
            {
                Text = "Selectează Angajatul:",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Left = 20,
                Top = 18,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            pnlHeader.Controls.Add(lblAlege);

            _btnAngajat = new Button
            {
                Text = SelectedPerson != null
                    ? string.Format("  {0}  ▾", SelectedPerson.NumeComplet)
                    : "Alege...  ▾",
                Height = 32,
                Width = 320,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 62, 88),
                ForeColor = Color.FromArgb(200, 220, 255),
                Font = new Font("Segoe UI", 11f),
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            _btnAngajat.FlatAppearance.BorderSize = 2;
            _btnAngajat.FlatAppearance.BorderColor = Color.FromArgb(233, 239, 247);
            _btnAngajat.Click += OnSelectAngajat;
            pnlHeader.Controls.Add(_btnAngajat);

            // Pozitioneaza butonul langa label
            Action pozBtn = () =>
            {
                _btnAngajat.Left = lblAlege.Right + 12;
                _btnAngajat.Top = lblAlege.Top + (lblAlege.Height - _btnAngajat.Height) / 2;
            };
            pnlHeader.HandleCreated += (s, e) => pozBtn();
            lblAlege.SizeChanged += (s, e) => pozBtn();

            // Titlu dreapta
            var lblTitlu = new Label
            {
                Text = "Generator Documente HR",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            var lblSub = new Label
            {
                Text = "Selectează tipul documentului",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 195, 215),
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            pnlHeader.Controls.Add(lblTitlu);
            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Resize += (s, e) =>
            {
                lblTitlu.Left = pnlHeader.Width - lblTitlu.Width - 20; lblTitlu.Top = 10;
                lblSub.Left = pnlHeader.Width - lblSub.Width - 20; lblSub.Top = 44;
            };
            pnlHeader.HandleCreated += (s, e) =>
            {
                lblTitlu.Left = pnlHeader.Width - lblTitlu.Width - 20; lblTitlu.Top = 10;
                lblSub.Left = pnlHeader.Width - lblSub.Width - 20; lblSub.Top = 44;
            };

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
                BackColor = Color.FromArgb(180, 190, 210),
                ForeColor = Color.FromArgb(100, 110, 130),
                Font = new Font("Segoe UI", 10f),
                Enabled = false,
                Top = 10
            };
            _btnContinua.FlatAppearance.BorderSize = 2;
            _btnContinua.FlatAppearance.BorderColor = Color.FromArgb(160, 175, 200);
            _btnContinua.Click += (s, e) => Confirma();

            var btnAnuleaza = new Button
            {
                Text = "Anulează",
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 248),
                ForeColor = Color.FromArgb(80, 95, 120),
                Font = new Font("Segoe UI", 10f),
                Top = 10
            };
            btnAnuleaza.FlatAppearance.BorderSize = 1;
            btnAnuleaza.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnAnuleaza.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnContinua, btnAnuleaza });
            pnlFooter.Resize += (s, e) =>
            {
                _btnContinua.Left = pnlFooter.Width - _btnContinua.Width - 16;
                btnAnuleaza.Left = _btnContinua.Left - btnAnuleaza.Width - 8;
            };

            // ── Scroll area cu carduri ─────────────────────────
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 8),
                BackColor = BgForm
            };

            BuildCards(scroll);

            Controls.Add(scroll);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            AcceptButton = _btnContinua;
            CancelButton = btnAnuleaza;
        }

        // ══════════════════════════════════════════════════════
        //  BUILD CARDS — dinamic din DocumentRegistry
        // ══════════════════════════════════════════════════════
        private void BuildCards(Panel scroll)
        {
            const int SepW = 3;
            const int CardH = 78;
            const int CardGap = 5;
            const int HeaderH = 44;
            const int Pad = 10;

            var categories = DocumentRegistry.GetCategories();
            if (categories.Count == 0) return;

            int nCols = categories.Count;
            var colPanels = new Panel[nCols];

            for (int ci = 0; ci < nCols; ci++)
            {
                var cat = categories[ci];
                var catTheme = DocumentTheme.ForCategory(cat.Name);
                var col = new Panel { BackColor = BgForm };

                // Header coloana
                var pnlHdr = new Panel
                {
                    Left = 0,
                    Top = 0,
                    Height = HeaderH,
                    BackColor = Color.White,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                };
                pnlHdr.Paint += (s, e) =>
                    e.Graphics.FillRectangle(new SolidBrush(catTheme.Accent),
                        0, pnlHdr.Height - 3, pnlHdr.Width, 3);

                var lblCat = new Label
                {
                    Text = cat.Name.ToUpper(),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = catTheme.AccentDark,
                    AutoSize = false,
                    Location = new Point(Pad, 12),
                    Height = 20,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                };
                pnlHdr.Controls.Add(lblCat);
                pnlHdr.Resize += (s, e) => lblCat.Width = pnlHdr.Width - Pad * 2;
                col.Controls.Add(pnlHdr);

                // Carduri
                int cardTop = HeaderH + 6;
                foreach (var doc in cat.Documents)
                {
                    var btn = BuildCard(doc, catTheme, CardH);
                    btn.Top = cardTop;
                    btn.Left = Pad;
                    btn.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                    col.Controls.Add(btn);
                    _allCards.Add(btn);
                    cardTop += CardH + CardGap;
                }

                col.Height = cardTop + 8;
                colPanels[ci] = col;
                scroll.Controls.Add(col);
            }

            // Relayout coloane
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

                    foreach (Control c in col.Controls)
                    {
                        if (c is Panel hdr) hdr.Width = colW;
                        if (c is Button btn) btn.Width = colW - Pad * 2;
                    }
                    x += colW;
                }
                scroll.Invalidate();
            };

            // Separatori verticali
            scroll.Paint += (s, e) =>
            {
                int totalW = scroll.ClientSize.Width - scroll.Padding.Horizontal;
                int sepTotal = SepW * (nCols - 1);
                int colW = Math.Max((totalW - sepTotal) / nCols, 150);
                int x = colW;

                for (int ci = 1; ci < nCols; ci++)
                {
                    var theme = DocumentTheme.ForCategory(categories[ci].Name);
                    e.Graphics.FillRectangle(new SolidBrush(theme.Accent),
                        x, 8, SepW, scroll.ClientSize.Height - 16);
                    x += SepW + colW;
                }
            };

            scroll.Resize += (s, e) => relayout();
            relayout();
        }

        private Button BuildCard(DocumentDefinition doc, DocumentTheme theme, int cardH)
        {
            var btn = new Button
            {
                Height = cardH,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Tag = doc
            };
            btn.FlatAppearance.BorderColor = theme.AccentBorder;
            btn.FlatAppearance.BorderSize = 2;

            btn.Paint += (s, e) => DrawCard(e.Graphics, btn, doc.Title, theme);
            btn.Click += OnCardClick;
            btn.DoubleClick += (s, e) => { OnCardClick(s, e); Confirma(); };
            btn.MouseEnter += (s, e) =>
            {
                if (btn != _selectedCard) btn.BackColor = theme.AccentPal;
            };
            btn.MouseLeave += (s, e) =>
            {
                if (btn != _selectedCard) btn.BackColor = Color.White;
            };

            return btn;
        }

        private static void DrawCard(Graphics g, Button btn, string titlu, DocumentTheme theme)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var dlg = btn.FindForm() as SelectorDialog;
            bool sel = dlg != null && btn == dlg._selectedCard;

            // Bara accent sus
            g.FillRectangle(new SolidBrush(sel ? theme.Accent : theme.AccentBorder),
                0, 0, btn.Width, 3);

            // Border selectie
            if (sel)
                using (var pen = new Pen(theme.Accent, 2))
                    g.DrawRectangle(pen, 1, 1, btn.Width - 3, btn.Height - 3);

            // Icon document
            int ix = 14, iy = 16;
            g.FillRectangle(new SolidBrush(theme.AccentPal), ix, iy, 20, 26);
            using (var pen = new Pen(theme.AccentBorder))
            {
                g.DrawRectangle(pen, ix, iy, 20, 26);
                for (int li = 0; li < 3; li++)
                    g.DrawLine(pen, ix + 3, iy + 7 + li * 6, ix + 17, iy + 7 + li * 6);
            }

            // Titlu
            using (var br = new SolidBrush(Color.FromArgb(25, 35, 55)))
                g.DrawString(titlu,
                    new Font("Segoe UI", 10f, FontStyle.Bold), br,
                    new RectangleF(44, 18, btn.Width - 52, btn.Height - 20));

            // Checkmark
            if (sel)
            {
                g.FillEllipse(new SolidBrush(theme.Accent),
                    btn.Width - 22, btn.Height - 22, 16, 16);
                using (var pen = new Pen(Color.White, 1.8f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLines(pen, new[]
                    {
                        new Point(btn.Width - 18, btn.Height - 13),
                        new Point(btn.Width - 15, btn.Height - 10),
                        new Point(btn.Width - 9,  btn.Height - 16)
                    });
            }
        }

        private void OnCardClick(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var doc = btn.Tag as DocumentDefinition;
            if (doc == null) return;

            // Deselect old
            if (_selectedCard != null)
            {
                _selectedCard.BackColor = Color.White;
                _selectedCard.Invalidate();
            }

            _selectedCard = btn;
            SelectedDocument = doc;
            btn.BackColor = DocumentTheme.ForCategory(doc.Category).AccentPal;
            btn.Invalidate();

            // Activa butonul Continua
            _btnContinua.Enabled = true;
            _btnContinua.BackColor = Color.FromArgb(63, 129, 198);
            _btnContinua.ForeColor = Color.White;
            _btnContinua.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _btnContinua.Cursor = Cursors.Hand;
        }

        private void OnSelectAngajat(object sender, EventArgs e)
        {
            using (var dlg = new PersonPickerDialog(_persoane,
                "Selectare angajat",
                "Selectează angajatul pentru care generezi documentul"))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                SelectedPerson = dlg.SelectedPerson;
                _btnAngajat.Text = string.Format("  {0}  ▾", SelectedPerson.NumeComplet);
            }
        }

        private void Confirma()
        {
            if (SelectedDocument == null) return;
            if (SelectedPerson == null)
            {
                MessageBox.Show("Selectează un angajat înainte de a continua.",
                    "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}