using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Properties;
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
        private Button _btnDosar;
        private bool _btnDosarHovered = false;
        private bool _btnDosarPressed = false;
        private double _pulsePhase = 0;   // faza animatie badge pulsant
        private bool _btnAngajatHovered = false;
        private Button _btnContinua;
        private Button _selectedCard;
        private readonly List<Button> _allCards = new List<Button>();

        // ── Culori ────────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(66, 76, 103);
        private static readonly Color BgForm = Color.FromArgb(242, 245, 250);

        // ══════════════════════════════════════════════════════
        //  Constructor fara parametri — pentru apelul direct din
        //  Softone ca "Dll Form".
        // ══════════════════════════════════════════════════════
        public SelectorDialog() : this(InitStandaloneAndLoadPersoane(), 0)
        {
            _standaloneMode = true;
            _companyDataStandalone = _companyDataStandaloneStatic;
        }

        private static List<PersonInfo> InitStandaloneAndLoadPersoane()
        {
            try
            {
                //PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                RegistraturaService.Initialize(S1.xSupp);
                HookRegistry.RegisterAll();
                DocumentRegistry.Initialize(TemplatesRootStandalone());

                var companyData = ErpDataProvider.GetCompanyData(S1.xSupp);
                BulkContext.XSupport = S1.xSupp;
                BulkContext.CompanyData = companyData;
                _companyDataStandaloneStatic = companyData;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la inițializare:\n" + ex.Message,
                    "Generator Documente HR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return LoadPersoaneStandalone();
        }

        private static ErpCompanyData _companyDataStandaloneStatic;

        private bool _standaloneMode;
        private ErpCompanyData _companyDataStandalone;
        private ErpCimData _cimDataStandalone;
        private int _cimDataPrsnId;

        private static List<PersonInfo> LoadPersoaneStandalone()
        {
            try
            {
                return S1.xSupp != null
                    ? PersonPickerDialog.LoadFromErp(S1.xSupp)
                    : new List<PersonInfo>();
            }
            catch { return new List<PersonInfo>(); }
        }

        private static string TemplatesRootStandalone()
        {
            string configured = PluginConfig.TemplatesRoot;
            if (!string.IsNullOrWhiteSpace(configured) && System.IO.Directory.Exists(configured))
                return configured;

            string dllDir = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            return System.IO.Path.Combine(dllDir, "Templates");
        }

        private void StandaloneLoop()
        {
            if (SelectedDocument == null || SelectedPerson == null) return;

            try
            {
                if (_cimDataStandalone == null || SelectedPerson.PrsnId != _cimDataPrsnId)
                {
                    _cimDataStandalone = ErpDataProvider.GetCimData(SelectedPerson.PrsnId, S1.xSupp);
                    _cimDataPrsnId = SelectedPerson.PrsnId;
                }

                var common = CommonDocumentValues.FromErp(
                    SelectedPerson.PrsnId,
                    SelectedPerson.NumeComplet,
                    SelectedPerson.CNP,
                    SelectedPerson.Functie,
                    _cimDataStandalone,
                    _companyDataStandalone);

                common.CodInregistrare = RegistraturaService.Instance.CalculateCod(
                    RegistraturaService.Instance.GetLoginDate());

                using (var form = new DynamicForm(SelectedDocument, common, _persoane))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare:\n" + ex.Message,
                    "Generator Documente HR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ResetSelectionForNextDocument();
        }

        private void ResetSelectionForNextDocument()
        {
            SelectedDocument = null;
            if (_selectedCard != null)
            {
                _selectedCard.BackColor = Color.White;
                _selectedCard.Invalidate();
                _selectedCard = null;
            }
            _btnContinua.Enabled = false;
            _btnContinua.BackColor = Color.FromArgb(180, 190, 210);
            _btnContinua.ForeColor = Color.FromArgb(100, 110, 130);
            _btnContinua.Font = new Font("Segoe UI", 10f);
        }

        // ══════════════════════════════════════════════════════
        //  Constructor
        // ══════════════════════════════════════════════════════
        public SelectorDialog(List<PersonInfo> persoane, int currentPrsnId = 0)
        {
            _persoane = persoane ?? new List<PersonInfo>();
            _currentPrsnId = currentPrsnId;

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

            // ── Buton selectare angajat ───────────────────────
            // Desenat complet in Paint — doua stari:
            //   fara angajat → rosu, badge pulsant, label OBLIGATORIU, chevron
            //   cu angajat   → albastru, avatar initiale, nume+functie, creion
            _btnAngajat = new Button
            {
                Height = 56,
                Width = 420,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                UseVisualStyleBackColor = false,
                Text = ""   // tot continutul e desenat in Paint
            };
            _btnAngajat.FlatAppearance.BorderSize = 0;
            _btnAngajat.Paint += (s, e) => DrawAngajatButton(e.Graphics, _btnAngajat);
            _btnAngajat.Click += OnSelectAngajat;
            _btnAngajat.MouseEnter += (s, e) => { _btnAngajatHovered = true; _btnAngajat.Invalidate(); };
            _btnAngajat.MouseLeave += (s, e) => { _btnAngajatHovered = false; _btnAngajat.Invalidate(); };

            // Timer pentru animatia de puls — invalideaza butonul cat timp
            // nu e selectat niciun angajat
            var pulseTimer = new System.Windows.Forms.Timer { Interval = 30 };
            pulseTimer.Tick += (s, e) =>
            {
                _pulsePhase += 0.08;
                if (SelectedPerson == null && _btnAngajat.IsHandleCreated && !_btnAngajat.IsDisposed)
                    _btnAngajat.Invalidate();
            };
            pulseTimer.Start();

            pnlHeader.Controls.Add(_btnAngajat);

            // Centreaza vertical butonul in header
            pnlHeader.HandleCreated += (s, e) =>
                _btnAngajat.Top = (pnlHeader.Height - _btnAngajat.Height) / 2;
            pnlHeader.Resize += (s, e) =>
                _btnAngajat.Top = (pnlHeader.Height - _btnAngajat.Height) / 2;
            _btnAngajat.Left = 16;

            // ── Buton "deschide dosarul angajatului" ──────────
            // Vizibil doar cand e selectat un angajat; deschide in
            // Explorer folderul unde se salveaza documentele lui
            // (acelasi folder folosit la generare — cautat dupa
            // PrsnId, vezi DynamicTemplateEngine.ResolvePersonFolder).
            // Desenat complet in Paint, ca sa arate ca o extensie a
            // butonului de angajat (aceleasi culori/rotunjire), nu
            // ca un buton standard Windows.
            _btnDosar = new Button
            {
                Height = 56,
                Width = 190,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                UseVisualStyleBackColor = false,
                Text = "",   // tot continutul e desenat in Paint
                Visible = SelectedPerson != null
            };
            _btnDosar.FlatAppearance.BorderSize = 0;
            _btnDosar.Paint += (s, e) => DrawDosarButton(e.Graphics, _btnDosar);
            _btnDosar.Click += OnDeschideDosar;
            _btnDosar.MouseEnter += (s, e) => { _btnDosarHovered = true; _btnDosar.Invalidate(); };
            _btnDosar.MouseLeave += (s, e) => { _btnDosarHovered = false; _btnDosarPressed = false; _btnDosar.Invalidate(); };
            _btnDosar.MouseDown += (s, e) => { _btnDosarPressed = true; _btnDosar.Invalidate(); };
            _btnDosar.MouseUp += (s, e) => { _btnDosarPressed = false; _btnDosar.Invalidate(); };

            var toolTipDosar = new ToolTip();
            toolTipDosar.SetToolTip(_btnDosar, "Deschide dosarul angajatului");

            pnlHeader.Controls.Add(_btnDosar);
            pnlHeader.HandleCreated += (s, e) =>
                _btnDosar.Top = (pnlHeader.Height - _btnDosar.Height) / 2;
            pnlHeader.Resize += (s, e) =>
                _btnDosar.Top = (pnlHeader.Height - _btnDosar.Height) / 2;
            _btnDosar.Left = _btnAngajat.Left + _btnAngajat.Width + 6;

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
        //  DRAW ANGAJAT BUTTON
        //  Custom paint pentru butonul de selectie angajat.
        //  Stare 1 — fara angajat: fundal rosu, cerc pulsant stanga,
        //    label "OBLIGATORIU" mic, text "Selecteaza angajatul", chevron dreapta.
        //  Stare 2 — angajat selectat: fundal albastru, avatar circular cu
        //    initiale, label "ANGAJAT SELECTAT" mic, nume — functie, creion dreapta.
        // ══════════════════════════════════════════════════════
        private void DrawAngajatButton(Graphics g, Button btn)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = btn.Width, h = btn.Height;
            int cy = h / 2;
            const int Radius = 8;
            const float TextX = 62f;

            Color bgColor = SelectedPerson == null
                ? (_btnAngajatHovered ? Color.FromArgb(172, 58, 58) : Color.FromArgb(148, 48, 48))
                : (_btnAngajatHovered ? Color.FromArgb(48, 74, 122) : Color.FromArgb(35, 57, 98));
            Color borderColor = SelectedPerson == null
                ? Color.FromArgb(195, 78, 78)
                : Color.FromArgb(62, 100, 162);

            // Fundalul header-ului — colturile rotunjite par transparente
            g.Clear(BgDark);

            // Rounded fill + border vizibil
            using (var path = RoundedRect(new Rectangle(1, 1, w - 2, h - 2), Radius))
            {
                g.FillPath(new SolidBrush(bgColor), path);
                using (var pen = new Pen(borderColor, 1.5f))
                    g.DrawPath(pen, path);
            }

            // StringFormat cu trunchiere "..." pentru text lung
            using (var sf = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                if (SelectedPerson == null)
                {
                    // ── Cerc pulsant ──────────────────────────────────────
                    int cx = 30, baseR = 7;
                    double scale = 1.0 + 0.55 * Math.Sin(_pulsePhase);
                    int ringR = (int)(baseR * 2.2 * scale);
                    byte alpha = (byte)(55 + 55 * Math.Abs(Math.Sin(_pulsePhase)));
                    using (var br = new SolidBrush(Color.FromArgb(alpha, 240, 100, 100)))
                        g.FillEllipse(br, cx - ringR, cy - ringR, ringR * 2, ringR * 2);
                    g.FillEllipse(new SolidBrush(Color.FromArgb(245, 120, 120)),
                        cx - baseR, cy - baseR, baseR * 2, baseR * 2);

                    // ── "OBLIGATORIU" ──────────────────────────────────────
                    using (var f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    using (var br = new SolidBrush(Color.FromArgb(255, 200, 200)))
                        g.DrawString("OBLIGATORIU", f, br, TextX, cy - 17f);

                    // ── Text principal ─────────────────────────────────────
                    using (var f = new Font("Segoe UI", 10f, FontStyle.Bold))
                    using (var br = new SolidBrush(Color.FromArgb(255, 232, 232)))
                        g.DrawString("Selectează angajatul", f, br,
                            new RectangleF(TextX, cy - 1f, w - TextX - 34f, 20f), sf);

                    // ── Chevron ────────────────────────────────────────────
                    using (var f = new Font("Segoe UI", 13f))
                    using (var br = new SolidBrush(Color.FromArgb(230, 195, 195)))
                        g.DrawString("▾", f, br, w - 26f, cy - 11f);
                }
                else
                {
                    // ── Avatar circular cu initiale ────────────────────────
                    int avR = 19, cx = 36;
                    g.FillEllipse(new SolidBrush(Color.FromArgb(52, 90, 150)),
                        cx - avR, cy - avR, avR * 2, avR * 2);
                    string initiale = GetInitiale(SelectedPerson.NumeComplet);
                    using (var f = new Font("Segoe UI", 9f, FontStyle.Bold))
                    {
                        var sz = g.MeasureString(initiale, f);
                        g.DrawString(initiale, f, Brushes.White, cx - sz.Width / 2, cy - sz.Height / 2);
                    }

                    // ── "ANGAJAT SELECTAT" ─────────────────────────────────
                    using (var f = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    using (var br = new SolidBrush(Color.FromArgb(105, 150, 215)))
                        g.DrawString("ANGAJAT SELECTAT", f, br, TextX, cy - 17f);

                    // ── Nume — Functie (cu trunchiere elipsis) ─────────────
                    string linie = SelectedPerson.NumeComplet;
                    if (!string.IsNullOrWhiteSpace(SelectedPerson.Functie))
                        linie += "  —  " + SelectedPerson.Functie;
                    using (var f = new Font("Segoe UI", 10f, FontStyle.Bold))
                    using (var br = new SolidBrush(Color.White))
                        g.DrawString(linie, f, br,
                            new RectangleF(TextX, cy - 1f, w - TextX - 34f, 20f), sf);

                    // ── Creion ─────────────────────────────────────────────
                    using (var f = new Font("Segoe UI", 11f))
                    using (var br = new SolidBrush(Color.FromArgb(115, 155, 215)))
                        g.DrawString("✎", f, br, w - 28f, cy - 9f);
                }
            }
        }

        // ══════════════════════════════════════════════════════
        //  Buton "Dosar Personal" — desenat sa arate ca o extensie
        //  a butonului de angajat (aceeasi rotunjire, aceleasi
        //  culori de fundal/bordura/hover).
        // ══════════════════════════════════════════════════════
        private void DrawDosarButton(Graphics g, Button btn)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = btn.Width, h = btn.Height;
            int cy = h / 2;
            const int Radius = 8;

            // Culoare distincta de butonul de angajat (nu identica —
            // se citeste ca "actiune secundara", nu ca alt selector),
            // dar tot in gama rece/inchisa a header-ului. Bordura calda
            // (auriu-stins) preia culoarea iconitei de dosar.
            Color bgColor = _btnDosarPressed
                ? Color.FromArgb(46, 60, 56)
                : _btnDosarHovered
                    ? Color.FromArgb(78, 96, 90)
                    : Color.FromArgb(58, 74, 70);
            Color borderColor = _btnDosarPressed
                ? Color.FromArgb(158, 128, 70)
                : Color.FromArgb(191, 155, 89);

            // Efect de apasare: fundalul/bordura raman pe loc (0,0),
            // dar tot continutul (icon + text) se muta 1px in jos —
            // senzatia de "impins", ca la un buton fizic.
            int pressOffset = _btnDosarPressed ? 1 : 0;

            // Fundalul header-ului — colturile rotunjite par transparente
            g.Clear(BgDark);

            using (var path = RoundedRect(new Rectangle(1, 1, w - 2, h - 2), Radius))
            {
                g.FillPath(new SolidBrush(bgColor), path);
                using (var pen = new Pen(borderColor, 1.5f))
                    g.DrawPath(pen, path);
            }

            Image icon = Resources.dosar_personal;

            float textX = 18f;
            if (icon != null)
            {
                int iconSize = 22;
                g.DrawImage(icon, 18, cy - iconSize / 2 + pressOffset, iconSize, iconSize);
                textX = 18f + iconSize + 10f;
            }

            using (var sf = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
                LineAlignment = StringAlignment.Center
            })
            using (var f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var br = new SolidBrush(Color.FromArgb(232, 238, 248)))
                g.DrawString("Dosar Personal", f, br,
                    new RectangleF(textX, pressOffset, w - textX - 10f, h), sf);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
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

            if (_selectedCard != null)
            {
                _selectedCard.BackColor = Color.White;
                _selectedCard.Invalidate();
            }

            _selectedCard = btn;
            SelectedDocument = doc;
            btn.BackColor = DocumentTheme.ForCategory(doc.Category).AccentPal;
            btn.Invalidate();

            _btnContinua.Enabled = true;
            _btnContinua.BackColor = Color.FromArgb(63, 129, 198);
            _btnContinua.ForeColor = Color.White;
            _btnContinua.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _btnContinua.Cursor = Cursors.Hand;
        }

        // Dupa selectia unui angajat, invalideaza butonul sa se redeseneze
        private void UpdateAngajatButton()
        {
            _btnAngajat?.Invalidate();
            if (_btnDosar != null) _btnDosar.Visible = SelectedPerson != null;
        }

        private static string GetInitiale(string numeComplet)
        {
            if (string.IsNullOrWhiteSpace(numeComplet)) return "?";
            var parts = numeComplet.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpper();
        }

        private void OnDeschideDosar(object sender, EventArgs e)
        {
            if (SelectedPerson == null) return;

            try
            {
                string path = DynamicTemplateEngine.ResolvePersonFolder(
                    SelectedPerson.PrsnId, SelectedPerson.NumeComplet);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nu am putut deschide dosarul angajatului: " + ex.Message,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnSelectAngajat(object sender, EventArgs e)
        {
            using (var dlg = new PersonPickerDialog(_persoane,
                "Selectare angajat",
                "Selectează angajatul pentru care generezi documentul"))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                SelectedPerson = dlg.SelectedPerson;
                UpdateAngajatButton();
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

            if (_standaloneMode)
            {
                StandaloneLoop();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}