using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;
using PdfiumViewer;

namespace ActAditionalPlugin.UI
{
    // ══════════════════════════════════════════════════════════
    //  DYNAMIC FORM
    //  Forma universala generata automat din DocumentDefinition.
    //  Inlocuieste toate formele specifice per document.
    // ══════════════════════════════════════════════════════════
    public sealed class DynamicForm : Form
    {
        // ── Definitia documentului ─────────────────────────────
        private readonly DocumentDefinition _def;
        private readonly CommonDocumentValues _common;
        private readonly List<PersonInfo> _persoane;  // toti angajatii activi

        // ── Valorile din formular ──────────────────────────────
        // key = field.Key din JSON, value = valoarea curenta
        private readonly Dictionary<string, object> _formValues
            = new Dictionary<string, object>();

        // ── Referinte la controale (pentru validare + colectare) ─
        private readonly Dictionary<string, Control> _controls
            = new Dictionary<string, Control>();

        // ── Liste dinamice: key → lista de randuri ─────────────
        private readonly Dictionary<string, List<DynamicListRow>> _dynamicLists
            = new Dictionary<string, List<DynamicListRow>>();

        // ── Panouri liste dinamice (pentru relayout) ───────────
        private readonly Dictionary<string, Panel> _listPanels
            = new Dictionary<string, Panel>();

        // ── Mentiuni (toggle) — panel fix deasupra footer-ului ─
        private Panel _pnlMentiuni;
        private Panel _pnlMentiuniWrapper; // containerul fix din split.Panel1
        private CheckBox _chkMentiuni;

        // ── Controale clauze Act Aditional ─────────────────────
        private readonly Dictionary<string, List<PunctModificareControl>> _clauzeControls
            = new Dictionary<string, List<PunctModificareControl>>();
        private readonly Dictionary<string, Panel> _clauzeItemsPanel
            = new Dictionary<string, Panel>();

        // ── Tema ──────────────────────────────────────────────
        private readonly DocumentTheme _theme;

        // ── Split layout ───────────────────────────────────────
        private PriorityScrollPanel _pnlBody;
        private PdfViewer _pdfViewer;
        private Label _lblPlaceholder;
        private Button _btnActualizeaza;
        private string _currentPdfPath;
        private bool _previewDone;
        private TextBox _txtCodInregistrare;
        private TextBox _txtMentiuni;
        private bool _hooksRunning; // guard re-intrare RunHooks

        // ── Constante vizuale ──────────────────────────────────
        private static readonly Color FundalForm = Color.FromArgb(242, 245, 250);
        private static readonly Color TextPrincipal = Color.FromArgb(25, 35, 55);
        private static readonly Color TextSecundar = Color.FromArgb(80, 100, 130);
        private static readonly Font FLabel = new Font("Segoe UI Semibold", 10f);
        private static readonly Font FInput = new Font("Segoe UI", 10f, FontStyle.Bold);
        private static readonly Font FSectiune = new Font("Segoe UI", 8f, FontStyle.Bold);
        private const int MultilineHeight = 140; // inaltime standard pentru toate campurile multiline

        // ══════════════════════════════════════════════════════
        //  Constructor
        // ══════════════════════════════════════════════════════
        public DynamicForm(
            DocumentDefinition def,
            CommonDocumentValues common,
            List<PersonInfo> persoane)
        {
            _def = def;
            _common = common;
            _persoane = persoane ?? new List<PersonInfo>();
            _theme = DocumentTheme.ForCategory(def.Category);

            Text = def.Title;
            Size = new Size(860, 680);
            MinimumSize = new Size(720, 520);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10f);
            BackColor = FundalForm;

            BuildShell();
            BuildBody();

            Shown += (s, e) =>
            {
                SplitContainer split = null;
                foreach (Control c in Controls)
                    if (c is SplitContainer sc) { split = sc; break; }
                if (split != null)
                    split.SplitterDistance = (int)(split.Width * 0.40);

                // Calculeaza codul de inregistrare initial
                RecalcCodInregistrare();

                // Precompletare campuri CI/Domiciliu din datele angajatului principal
                // (daca JSON-ul are campuri cu aceste key-uri si sunt inca goale)
                PrePopulateReadonlyCommonFields();

                // Ruleaza hooks on_open
                RunHooks("on_open", null);
            };

            FormClosed += (s, e) =>
            {
                try { _pdfViewer?.Document?.Dispose(); } catch { }
                TryDeleteFile(_currentPdfPath);
            };
        }

        // ══════════════════════════════════════════════════════
        //  SHELL (split 40/60, fara header separat)
        //  Titlul + angajat sunt primul element din _pnlBody.
        //  split.Panel1 = doar footer(Bottom) + mentiuni(Bottom) + body(Fill)
        // ══════════════════════════════════════════════════════
        private void BuildShell()
        {
            // Split
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = true,
                BackColor = Color.FromArgb(210, 220, 235),
                SplitterWidth = 3
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
                using (var pen = new Pen(_theme.AccentBorder))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var btnInapoi = MakeFooterButton("  Anulare", Properties.Resources.back_arrow,
                Color.FromArgb(255, 220, 220), Color.FromArgb(160, 40, 40));
            btnInapoi.Left = 12;
            btnInapoi.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            btnInapoi.FlatAppearance.BorderSize = 1;
            btnInapoi.FlatAppearance.BorderColor = Color.FromArgb(220, 150, 150);
            btnInapoi.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _btnActualizeaza = MakeFooterButton(" Previzualizează", Properties.Resources.refreshPreview,
                Color.FromArgb(255, 243, 176), Color.FromArgb(120, 90, 10));
            _btnActualizeaza.Width = 200;
            _btnActualizeaza.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _btnActualizeaza.FlatAppearance.BorderSize = 3;
            _btnActualizeaza.FlatAppearance.BorderColor = Color.FromArgb(240, 200, 80);
            _btnActualizeaza.Click += OnPreview;

            var btnGenereaza = MakeFooterButton("  Generează PDF", Properties.Resources.documentOK,
                _theme.Accent, Color.White);
            btnGenereaza.Width = 180;
            btnGenereaza.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnGenereaza.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnGenereaza.FlatAppearance.BorderSize = 3;
            btnGenereaza.FlatAppearance.BorderColor = _theme.AccentDark;
            btnGenereaza.MouseEnter += (s, e) => btnGenereaza.BackColor = _theme.AccentDark;
            btnGenereaza.MouseLeave += (s, e) => btnGenereaza.BackColor = _theme.Accent;
            btnGenereaza.Click += OnGenerate;

            pnlFooter.Controls.AddRange(new Control[] { btnInapoi, _btnActualizeaza, btnGenereaza });
            pnlFooter.Resize += (s, e) =>
            {
                btnGenereaza.Left = pnlFooter.Width - btnGenereaza.Width - 12;
                _btnActualizeaza.Left = btnGenereaza.Left - _btnActualizeaza.Width - 10;
            };

            // ── Body scrollabil ────────────────────────────────
            _pnlBody = new PriorityScrollPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(12, 8, 0, 0),
                BackColor = FundalForm
            };

            // ── Panel fix mentiuni ─────────────────────────────
            _pnlMentiuniWrapper = BuildMentiuniWrapper();

            // ── SplitContainer vertical intern: sus=body, jos=footer+mentiuni ──
            // Folosim SplitContainer pentru izolare perfecta — scrollbarul body-ului
            // nu poate depasi niciodata zona de jos (footer + mentiuni).
            var innerSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                IsSplitterFixed = false,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(210, 220, 235),
                Panel1MinSize = 100,
                Panel2MinSize = 56
            };

            innerSplit.Panel1.Controls.Add(_pnlBody);

            // Panel jos: mentiuni (Bottom) + footer (Bottom) + spatiu Fill gol
            var pnlJos = new Panel { Dock = DockStyle.Fill, BackColor = FundalForm };
            pnlJos.Controls.Add(pnlFooter);          // Bottom
            pnlJos.Controls.Add(_pnlMentiuniWrapper); // Bottom (deasupra footer)

            innerSplit.Panel2.Controls.Add(pnlJos);

            // Inaltimea panel-ului jos = footer (56) + mentiuni (34 initial)
            // Se ajusteaza cand mentiunile sunt activate
            innerSplit.SplitterDistance = 9999; // va fi recalculat la Shown
            _pnlMentiuniWrapper.SizeChanged += (s, e) =>
            {
                int josH = pnlFooter.Height + _pnlMentiuniWrapper.Height;
                if (innerSplit.Height > josH + innerSplit.Panel1MinSize)
                    innerSplit.SplitterDistance = innerSplit.Height - josH - innerSplit.SplitterWidth;
            };
            innerSplit.Resize += (s, e) =>
            {
                int josH = pnlFooter.Height + _pnlMentiuniWrapper.Height;
                if (innerSplit.Height > josH + innerSplit.Panel1MinSize)
                    innerSplit.SplitterDistance = innerSplit.Height - josH - innerSplit.SplitterWidth;
            };

            split.Panel1.Controls.Add(innerSplit);

            // ── PDF viewer dreapta ─────────────────────────────
            _lblPlaceholder = new Label
            {
                Text = "↺  Apasă «Previzualizează» pentru a vedea documentul",
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(150, 160, 180),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(230, 232, 238)
            };

            _pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(230, 232, 238),
                ShowToolbar = true,
                ShowBookmarks = false,
                Visible = false
            };

            split.Panel2.Controls.Add(_pdfViewer);
            split.Panel2.Controls.Add(_lblPlaceholder);

            Controls.Add(split);

            split.Resize += (s, e) =>
            {
                if (split.Width > 0)
                    split.SplitterDistance = (int)(split.Width * 0.40);
            };
        }

        // ── Sectiunea angajat extinsa (readonly, mereu vizibila) ──
        // ── Titlu document + Cod inregistrare (in body, scrollabil) ──
        private void BuildTitluSection(ref int y)
        {
            int initW = Math.Max(_pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal, 400);

            var pnl = new Panel
            {
                Left = 0,
                Top = y,
                Height = 46,
                Width = initW,
                BackColor = _theme.Accent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            pnl.Controls.Add(new Label
            {
                Text = _def.Title,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(14, 10)
            });

            // Cod inregistrare dreapta
            var lblCod = new Label
            {
                Text = "Cod înregistrare:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(200, 225, 255),
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _txtCodInregistrare = new TextBox
            {
                ReadOnly = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(50, 75, 120),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center,
                Width = 130,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            pnl.Controls.AddRange(new Control[] { lblCod, _txtCodInregistrare });

            Action pos = () =>
            {
                if (pnl.Width == 0) return;
                _txtCodInregistrare.Left = pnl.Width - _txtCodInregistrare.Width - 14;
                _txtCodInregistrare.Top = (pnl.Height - _txtCodInregistrare.Height) / 2;
                lblCod.Left = _txtCodInregistrare.Left - lblCod.Width - 6;
                lblCod.Top = _txtCodInregistrare.Top + (_txtCodInregistrare.Height - lblCod.Height) / 2;
            };
            pnl.Resize += (s, e) => pos();
            pnl.Paint += (s, e) => pos();

            _pnlBody.Controls.Add(pnl);
            _pnlBody.Resize += (s, e) =>
                pnl.Width = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
            y += 50;
        }

        // ── Date angajat in body (scrollabil) ─────────────────
        private void BuildAngajatInBody(ref int y)
        {
            int initW = Math.Max(_pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal, 400);

            var pnl = new Panel
            {
                Left = 0,
                Top = y,
                Height = 128,
                Width = initW,
                BackColor = Color.White,
                Padding = new Padding(12, 4, 12, 4),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnl.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 220, 235)))
                    e.Graphics.DrawLine(pen, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
                e.Graphics.FillRectangle(new SolidBrush(_theme.Accent), 0, 0, 4, pnl.Height);
            };

            pnl.Controls.Add(new Label
            {
                Text = "DATE ANGAJAT",
                Font = FSectiune,
                ForeColor = _theme.Accent,
                AutoSize = true,
                Location = new Point(14, 4)
            });

            // Rand 1: Angajat | CNP | Functie
            var tbl1 = new TableLayoutPanel
            {
                Left = 14,
                Top = 22,
                Height = 44,
                RowCount = 2,
                ColumnCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            tbl1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tbl1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tbl1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tbl1.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            tbl1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tbl1.Controls.Add(MakeAngajatLabel("Angajat"), 0, 0);
            tbl1.Controls.Add(MakeAngajatLabel("CNP"), 1, 0);
            tbl1.Controls.Add(MakeAngajatLabel("Funcție"), 2, 0);
            tbl1.Controls.Add(MakeAngajatField(_common.NumeSalariat), 0, 1);
            tbl1.Controls.Add(MakeAngajatField(_common.CNP), 1, 1);
            tbl1.Controls.Add(MakeAngajatField(_common.Functie), 2, 1);

            // Rand 2: Nr. CIM | Data CIM | Departament
            var tbl2 = new TableLayoutPanel
            {
                Left = 14,
                Top = 70,
                Height = 44,
                RowCount = 2,
                ColumnCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            tbl2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl2.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            tbl2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tbl2.Controls.Add(MakeAngajatLabel("Nr. CIM"), 0, 0);
            tbl2.Controls.Add(MakeAngajatLabel("Data CIM"), 1, 0);
            tbl2.Controls.Add(MakeAngajatLabel("Departament"), 2, 0);
            tbl2.Controls.Add(MakeAngajatField(_common.NrCim), 0, 1);
            tbl2.Controls.Add(MakeAngajatField(
                _common.DataCim != DateTime.MinValue
                    ? _common.DataCim.ToString("dd.MM.yyyy") : string.Empty), 1, 1);
            tbl2.Controls.Add(MakeAngajatField(_common.NumeDepartament), 2, 1);

            pnl.Controls.Add(tbl1);
            pnl.Controls.Add(tbl2);
            pnl.Resize += (s, e) =>
            {
                tbl1.Width = pnl.ClientSize.Width - 28;
                tbl2.Width = pnl.ClientSize.Width - 28;
            };

            _pnlBody.Controls.Add(pnl);
            _pnlBody.Resize += (s, e) =>
                pnl.Width = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
            y += 132;
        }

        // ── Sectiunea angajat (folosita anterior, pastrata pentru compatibilitate) ──
        private Panel BuildAngajatSection()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 170,
                BackColor = Color.White,
                Padding = new Padding(12, 6, 12, 6)
            };
            pnl.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 220, 235)))
                    e.Graphics.DrawLine(pen, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
                // Bara accent stanga
                e.Graphics.FillRectangle(new SolidBrush(_theme.Accent), 0, 0, 4, pnl.Height);
            };

            // Label sectiune
            var lblSect = new Label
            {
                Text = "DATE ANGAJAT",
                Font = FSectiune,
                ForeColor = _theme.Accent,
                AutoSize = true,
                Location = new Point(14, 4)
            };
            pnl.Controls.Add(lblSect);

            // Rand 1: Angajat | CNP | Functie
            var tbl1 = new TableLayoutPanel
            {
                Left = 14,
                Top = 24,
                Height = 44,
                RowCount = 2,
                ColumnCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            tbl1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tbl1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tbl1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tbl1.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            tbl1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tbl1.Controls.Add(MakeAngajatLabel("Angajat"), 0, 0);
            tbl1.Controls.Add(MakeAngajatLabel("CNP"), 1, 0);
            tbl1.Controls.Add(MakeAngajatLabel("Funcție"), 2, 0);
            tbl1.Controls.Add(MakeAngajatField(_common.NumeSalariat), 0, 1);
            tbl1.Controls.Add(MakeAngajatField(_common.CNP), 1, 1);
            tbl1.Controls.Add(MakeAngajatField(_common.Functie), 2, 1);

            // Rand 2: Nr. CIM | Data CIM | Departament
            var tbl2 = new TableLayoutPanel
            {
                Left = 14,
                Top = 72,
                Height = 44,
                RowCount = 2,
                ColumnCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            tbl2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl2.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            tbl2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tbl2.Controls.Add(MakeAngajatLabel("Nr. CIM"), 0, 0);
            tbl2.Controls.Add(MakeAngajatLabel("Data CIM"), 1, 0);
            tbl2.Controls.Add(MakeAngajatLabel("Departament"), 2, 0);
            tbl2.Controls.Add(MakeAngajatField(_common.NrCim), 0, 1);
            tbl2.Controls.Add(MakeAngajatField(
                _common.DataCim != DateTime.MinValue
                    ? _common.DataCim.ToString("dd.MM.yyyy")
                    : string.Empty), 1, 1);
            tbl2.Controls.Add(MakeAngajatField(_common.NumeDepartament), 2, 1);

            pnl.Controls.Add(tbl1);
            pnl.Controls.Add(tbl2);
            pnl.Resize += (s, e) =>
            {
                tbl1.Width = pnl.ClientSize.Width - 28;
                tbl2.Width = pnl.ClientSize.Width - 28;
            };

            return pnl;
        }

        private static Label MakeAngajatLabel(string text) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextSecundar,
            Dock = DockStyle.Fill
        };

        // Camp readonly cu border — pentru header angajat
        private static TextBox MakeAngajatField(string value) => new TextBox
        {
            Text = value ?? string.Empty,
            ReadOnly = true,
            BackColor = Color.FromArgb(208, 213, 226),
            ForeColor = Color.FromArgb(25, 35, 55),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 6, 0)
        };

        // Pastrat pentru compatibilitate cu alte locuri
        private static Label MakeAngajatValue(string text) => new Label
        {
            Text = text ?? string.Empty,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = TextPrincipal,
            Dock = DockStyle.Fill,
            AutoEllipsis = true
        };

        // ══════════════════════════════════════════════════════
        //  BUILD BODY — genereaza sectiunile din JSON
        // ══════════════════════════════════════════════════════
        private void BuildBody()
        {
            int y = 0;

            // ── Titlu + Cod inregistrare (primul element scrollabil) ──
            BuildTitluSection(ref y);

            // ── Date angajat (scrollabil, parte din body) ─────
            BuildAngajatInBody(ref y);

            // Sectiunile din JSON — fiecare e retinuta ca bloc (controale + offset-uri),
            // ca sa putem ascunde/afisa sectiuni si sa reasezam formularul dinamic.
            _blocksStartY = y;
            foreach (var section in _def.Sections)
            {
                int blkStartY = y;
                int blkStartIdx = _pnlBody.Controls.Count;
                BuildSection(section, ref y);

                var blk = new SectionBlock { Def = section, Height = y - blkStartY };
                for (int ci = blkStartIdx; ci < _pnlBody.Controls.Count; ci++)
                {
                    var bc = _pnlBody.Controls[ci];
                    blk.Controls.Add(bc);
                    blk.Offsets.Add(bc.Top - blkStartY);
                }
                var autoList = section.Fields.FirstOrDefault(
                    f => f.Type == "dynamic_list" && f.AutoHeight);
                if (autoList != null && _listPanels.ContainsKey(autoList.Key))
                    blk.AutoListPanel = _listPanels[autoList.Key];
                _blocks.Add(blk);
            }

            // Recalc cod la schimbarea primei date din formular
            var firstDateField = _def.Sections
                .SelectMany(s => s.Fields)
                .FirstOrDefault(f => f.Type == "date");
            if (firstDateField != null && _controls.ContainsKey(firstDateField.Key))
            {
                var dtp = _controls[firstDateField.Key] as DateTimePicker;
                if (dtp != null)
                    dtp.ValueChanged += (s, e) => RecalcCodInregistrare(dtp.Value.Date);
            }

            // Aseaza sectiunile (vizibilitate + inaltimi) si seteaza inaltimea de scroll
            if (_blocks.Count > 0)
            {
                ReflowSections();
            }
            else
            {
                int totalH = y + 20;
                _pnlBody.AutoScrollMinSize = new System.Drawing.Size(0, totalH);
            }
        }

        // ══════════════════════════════════════════════════════
        //  BLOCURI DE SECTIUNE + REFLOW
        // ══════════════════════════════════════════════════════
        private class SectionBlock
        {
            public SectionDefinition Def;
            public List<Control> Controls = new List<Control>();
            public List<int> Offsets = new List<int>();   // Top relativ la inceputul blocului
            public int Height;
            public Panel AutoListPanel;                   // lista cu auto_height (ultimul control)
            public bool Visible = true;
        }

        private readonly List<SectionBlock> _blocks = new List<SectionBlock>();
        private int _blocksStartY;

        private bool IsBlockVisible(SectionBlock blk)
        {
            return blk.Def == null || IsDependencyActive(blk.Def.DependsOn, blk.Def.DependsValue);
        }

        private void ReflowSections()
        {
            if (_blocks.Count == 0) return;

            int scrollY = -_pnlBody.AutoScrollPosition.Y;
            _pnlBody.SuspendLayout();
            _pnlBody.AutoScrollPosition = new System.Drawing.Point(0, 0);

            int y = _blocksStartY;
            foreach (var blk in _blocks)
            {
                bool vis = IsBlockVisible(blk);
                blk.Visible = vis;

                int h = blk.Height;
                if (blk.AutoListPanel != null)
                {
                    int idx = blk.Controls.IndexOf(blk.AutoListPanel);
                    if (idx >= 0) h = blk.Offsets[idx] + blk.AutoListPanel.Height + 40; // spatiu sub lista
                }

                for (int i = 0; i < blk.Controls.Count; i++)
                {
                    blk.Controls[i].Visible = vis;
                    blk.Controls[i].Top = y + blk.Offsets[i];
                }
                if (vis) y += h;
            }

            _pnlBody.AutoScrollMinSize = new System.Drawing.Size(0, y + 20);
            _pnlBody.AutoScrollPosition = new System.Drawing.Point(0, scrollY);
            _pnlBody.ResumeLayout(true);
        }

        // Apelat dupa on_open / on_change: reaseaza doar daca s-a schimbat ceva
        private void UpdateSectionVisibility()
        {
            bool changed = false;
            foreach (var blk in _blocks)
                if (IsBlockVisible(blk) != blk.Visible) { changed = true; break; }
            if (changed) ReflowSections();
        }

        // Lista cu auto_height: inaltimea panoului urmeaza continutul
        private void UpdateListHeight(FieldDefinition field)
        {
            if (!field.AutoHeight) return;
            if (!_dynamicLists.ContainsKey(field.Key) || !_listPanels.ContainsKey(field.Key)) return;

            int content = 8;
            List<Control> dialogItems;
            if (_dialogListItems.TryGetValue(field.Key, out dialogItems))
            {
                // Lista "dialog": in panou sunt cardurile (sau mesajul de lista goala)
                // margine sus + carduri cu spatiu intre ele + margine jos
                content = 2 * DialogListPadY - DialogListGap;
                foreach (var c in dialogItems) content += c.Height + DialogListGap;
            }
            else
            {
                foreach (var r in _dynamicLists[field.Key]) content += r.Height + 4;
            }
            int max = field.MaxHeight > 0 ? field.MaxHeight : 560;
            // Lista "dialog" urmeaza exact cardurile (fara banda goala dedesubt)
            int min = _dialogListItems.ContainsKey(field.Key) ? 40 : 60;
            _listPanels[field.Key].Height = Math.Max(Math.Min(content, max), min);
            ReflowSections();
        }

        private void BuildSection(SectionDefinition section, ref int y)
        {
            // Calculeaza height-ul sectiunii
            int height = CalcSectionHeight(section);
            bool hasClauzeEditor = section.Fields.Any(f => f.Type == "clauze_editor");
            bool hasDynamicList = section.Fields.Any(f => f.Type == "dynamic_list");

            if (hasClauzeEditor)
            {
                BuildClauzeEditorSection(section, ref y);
                return;
            }

            if (hasDynamicList)
            {
                // Afiseaza titlul sectiunii inainte de campurile listei
                if (!string.IsNullOrWhiteSpace(section.Title))
                    AddSectiuneHeader(section.Title, ref y);

                foreach (var field in section.Fields)
                {
                    if (field.Type == "dynamic_list")
                        BuildDynamicListField(field, ref y, section.Height);
                    else
                        BuildRegularFieldInline(field, ref y);
                }
            }
            else
            {
                var pnl = AddSectiune(section.Title, ref y, height);
                BuildFieldsInPanel(section.Fields, pnl);
            }
        }

        // Chei care sunt deja afisate in headerul angajat — nu le mai afisam in body
        private static readonly HashSet<string> HeaderFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "NrCim", "DataCim", "NumeDepartament"
        };

        private void BuildFieldsInPanel(List<FieldDefinition> fields, Panel pnl)
        {
            // Grupeaza campurile in randuri (folosim width_percent)
            // Campurile fara width_percent specificat → fiecare pe randul lui (100%)
            // Campurile din header (NrCim, DataCim, NumeDepartament) sunt sarite
            var queue = new Queue<FieldDefinition>(fields.Where(f => !HeaderFields.Contains(f.Key)));

            while (queue.Count > 0)
            {
                var rowFields = new List<FieldDefinition>();
                int totalPercent = 0;

                // Acumuleaza campuri pana umplem 100%
                while (queue.Count > 0)
                {
                    var f = queue.Peek();
                    int w = f.LabelWidthPercent > 0 ? f.LabelWidthPercent : 100;
                    if (totalPercent + w <= 100)
                    {
                        rowFields.Add(queue.Dequeue());
                        totalPercent += w;
                        if (totalPercent >= 100) break;
                    }
                    else break;
                }

                if (rowFields.Count == 0) { queue.Dequeue(); continue; }

                var percentList = rowFields.Select(f =>
                    f.LabelWidthPercent > 0 ? f.LabelWidthPercent : 100).ToList();

                // Daca randul nu umple 100%, adauga coloana goala pentru rest
                int sumPercent = 0;
                foreach (var p in percentList) sumPercent += p;
                if (sumPercent < 100)
                    percentList.Add(100 - sumPercent);

                int rowHeight = rowFields.Max(FieldControlHeight) + 28;
                var tbl = AddRow(pnl, percentList.ToArray(), rowHeight);

                for (int i = 0; i < rowFields.Count; i++)
                {
                    var field = rowFields[i];
                    var ctrl = BuildFieldControl(field);
                    if (ctrl != null)
                    {
                        AddLabeledInput(tbl, i, field.Label, ctrl, field.Required);
                        _controls[field.Key] = ctrl;
                    }
                }
            }
        }

        // ── Construieste controlul pentru un camp ──────────────
        private Control BuildFieldControl(FieldDefinition field)
        {
            switch (field.Type)
            {
                case "readonly":
                    var roTb = MakeReadonly();
                    if (!string.IsNullOrEmpty(field.Default))
                        roTb.Text = field.Default;
                    return roTb;

                case "date":
                    var dtp = MakeDtp();
                    dtp.ValueChanged += (s, e) => RunHooks("on_change", null);
                    return dtp;

                case "datetime":
                    var dtpFull = new DateTimePicker
                    {
                        Format = DateTimePickerFormat.Custom,
                        CustomFormat = "dd.MM.yyyy HH:mm",
                        Value = DateTime.Today,
                        Font = FInput,
                        Height = 26
                    };
                    dtpFull.ValueChanged += (s, e) => RunHooks("on_change", null);
                    return dtpFull;

                case "multiline":
                    var ml = MakeMultiline(MultilineHeight);
                    if (!string.IsNullOrEmpty(field.Placeholder))
                        SetPlaceholder(ml, field.Placeholder);
                    return ml;

                case "combo":
                    var cmb = MakeCombo();
                    if (field.Options != null)
                        foreach (var opt in field.Options) cmb.Items.Add(opt);
                    if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
                    cmb.SelectedIndexChanged += (s, e) => RunHooks("on_change", null);
                    return cmb;

                case "person_picker":
                    return BuildPersonPickerControl(field);

                case "number":
                    var nud = new NumericUpDown
                    {
                        Minimum = 0,
                        Maximum = 999,
                        DecimalPlaces = 0,
                        Font = FInput,
                        Height = 26,
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    nud.ValueChanged += (s, e) => RunHooks("on_change", null);
                    return nud;

                case "text":
                default:
                    var tb = MakeInput();
                    if (!string.IsNullOrEmpty(field.Default))
                        tb.Text = field.Default;
                    else if (!string.IsNullOrEmpty(field.Placeholder))
                        SetPlaceholder(tb, field.Placeholder);
                    // Daca acest camp e referit ca cnp_field intr-un hook on_change,
                    // triggheruim on_change la fiecare modificare a textului
                    bool isOnChangeField = _def.Hooks != null && _def.Hooks.Any(h =>
                        h.On == "on_change" && h.Params != null &&
                        h.Params.Values.Any(v => v == field.Key));
                    if (isOnChangeField)
                        tb.TextChanged += (s, e) => { if (tb.ForeColor != Color.Gray) RunHooks("on_change", null); };
                    return tb;
            }
        }

        // ── Person picker control ──────────────────────────────
        // Buton cu eticheta care deschide PersonPickerDialog
        // si autocompleteaza campurile din maps{}
        private Control BuildPersonPickerControl(FieldDefinition field)
        {
            var pnl = new Panel
            {
                Height = 26,
                BackColor = Color.Transparent
            };

            var btn = new Button
            {
                Text = "Selectează persoana...",
                Dock = DockStyle.Fill,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 240, 255),
                ForeColor = _theme.AccentDark,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderColor = _theme.AccentBorder;
            btn.FlatAppearance.BorderSize = 1;

            btn.Click += (s, e) =>
            {
                using (var dlg = new PersonPickerDialog(_persoane,
                    "Selectare " + field.Label,
                    "Selectează persoana pentru câmpul: " + field.Label))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    var person = dlg.SelectedPerson;
                    btn.Text = person.NumeComplet;
                    btn.ForeColor = TextPrincipal;

                    // Valoarea implicita a cheii pickerului = numele complet.
                    // Se seteaza INAINTE de maps{}, ca un map pe aceeasi cheie
                    // (ex. "NumeComplet:upper") sa castige.
                    _formValues[field.Key] = person.NumeComplet;

                    // Autocomplete campurile din maps{}
                    if (field.Maps != null)
                    {
                        foreach (var map in field.Maps)
                        {
                            string targetKey = map.Key;   // key camp din formular
                            string personProp = map.Value; // proprietate din PersonInfo

                            // Sufix ":upper" -> transforma valoarea in majuscule
                            // Ex: "NumePrenumeInlocuitor": "NumeComplet:upper"
                            bool upper = personProp.EndsWith(":upper", StringComparison.OrdinalIgnoreCase);
                            if (upper) personProp = personProp.Substring(0, personProp.Length - ":upper".Length);

                            string val = GetPersonProperty(person, personProp);
                            if (upper && val != null)
                                val = val.ToUpper(new System.Globalization.CultureInfo("ro-RO"));

                            // Actualizeaza controlul vizual daca exista
                            if (_controls.ContainsKey(targetKey))
                                SetControlValue(_controls[targetKey], val);

                            // Salveaza in formValues
                            _formValues[targetKey] = val;
                        }
                    }

                    // Salveaza PrsnId invizibil
                    _formValues[field.Key + "_PrsnId"] = person.PrsnId;
                }
            };

            pnl.Controls.Add(btn);
            return pnl;
        }

        // ── Lista dinamica ─────────────────────────────────────
        private void BuildDynamicListField(FieldDefinition field, ref int y, int sectionHeight = 0)
        {
            if (string.Equals(field.EditMode, "dialog", StringComparison.OrdinalIgnoreCase))
            {
                BuildDialogListField(field, ref y, sectionHeight);
                return;
            }

            var rows = new List<DynamicListRow>();
            Panel panel = null;
            int panelH = sectionHeight > 0 ? sectionHeight - 42 : 200; // 42 = header + btn row
            panel = AddDynamicListSection(field.HideHeader ? string.Empty : field.Label,
                string.IsNullOrEmpty(field.AddButtonLabel)
                    ? "Adaugă " + field.Label.ToLower()
                    : field.AddButtonLabel,
                () => AddDynamicListRow(field, rows, panel),
                ref y, panelH);

            _dynamicLists[field.Key] = rows;
            _listPanels[field.Key] = panel;

            // Adauga randurile initiale (initial_items precompletate au prioritate)
            if (field.InitialItems != null && field.InitialItems.Count > 0)
            {
                foreach (var init in field.InitialItems)
                {
                    AddDynamicListRow(field, rows, panel);
                    rows[rows.Count - 1].SetFieldValues(init);
                }
                return;
            }
            int initRows = field.InitialRows > 0 ? field.InitialRows : 1;
            for (int i = 0; i < initRows; i++)
                AddDynamicListRow(field, rows, panel);
        }

        // ── Lista dinamica cu edit_mode "dialog" ───────────────
        // Datele stau in DynamicListRow-uri (editoare) care NU se afiseaza in
        // formular; in panou apar doar carduri needitabile (ListItemCard).
        // Adaugare / editare = ListItemDialog cu o copie a editorului.
        private readonly Dictionary<string, List<Control>> _dialogListItems
            = new Dictionary<string, List<Control>>();

        private void BuildDialogListField(FieldDefinition field, ref int y, int sectionHeight)
        {
            var rows = new List<DynamicListRow>();
            var cards = new List<ListItemCard>();
            var items = new List<Control>();   // ce e afisat in panou (pentru inaltime)
            Panel panel = null;
            int panelH = sectionHeight > 0 ? sectionHeight - 42 : 200;
            string addText = string.IsNullOrEmpty(field.AddButtonLabel)
                ? "Adaugă " + field.Label.ToLower()
                : field.AddButtonLabel;

            var lblEmpty = new Label
            {
                Text = "Nu a fost adăugat niciun element. Apasă „+ " + addText + "”.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = TextSecundar,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 44,
                Left = 2,
                Top = 4,
                BackColor = Color.Transparent
            };

            panel = AddDynamicListSection(field.HideHeader ? string.Empty : field.Label,
                addText,
                () => OpenListItemDialog(field, rows, cards, items, lblEmpty, panel, null),
                ref y, panelH, true);
            panel.Controls.Add(lblEmpty);
            lblEmpty.Width = Math.Max(panel.ClientSize.Width - 2 * DialogListPadX, 380);

            _dynamicLists[field.Key] = rows;
            _listPanels[field.Key] = panel;
            _dialogListItems[field.Key] = items;

            // Lista porneste goala, cu exceptia elementelor din initial_items
            if (field.InitialItems != null)
            {
                foreach (var init in field.InitialItems)
                {
                    var editor = new DynamicListRow(field, rows.Count + 1, _theme, _persoane, true);
                    editor.TokenResolver = ResolveCommonTokens;
                    editor.SetFieldValues(init);
                    AddDialogCard(field, rows, cards, items, lblEmpty, panel, editor);
                }
            }

            RelayoutDialogList(field, rows, cards, items, lblEmpty, panel);
        }

        // Card nou in lista "dialog" pentru un editor deja completat
        private void AddDialogCard(FieldDefinition field,
            List<DynamicListRow> rows, List<ListItemCard> cards, List<Control> items,
            Label lblEmpty, Panel panel, DynamicListRow editor)
        {
            rows.Add(editor);
            var card = new ListItemCard(editor, _theme);
            card.OnEdit = () => OpenListItemDialog(field, rows, cards, items, lblEmpty, panel, card);
            card.OnDelete = () =>
            {
                if (!ConfirmareStergereDialog.Intreaba(this,
                    string.Format("Ștergi elementul nr. {0} din lista „{1}”?", card.Numar, field.Label),
                    _theme)) return;
                rows.Remove(card.Row);
                cards.Remove(card);
                panel.Controls.Remove(card);
                card.Row.Dispose();
                card.Dispose();
                RelayoutDialogList(field, rows, cards, items, lblEmpty, panel);
            };
            card.OnLayoutChanged = () => RelayoutDialogList(field, rows, cards, items, lblEmpty, panel);
            cards.Add(card);
            panel.Controls.Add(card);
        }

        // Inlocuieste token-urile {Functie}, {DataCim} etc. cu datele angajatului.
        // Folosit de initial_items si on_select (liste dinamice).
        private string ResolveCommonTokens(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('{') < 0) return value;
            var tokens = new Dictionary<string, string>
            {
                { "{NumeSalariat}",    _common.NumeSalariat },
                { "{CNP}",             _common.CNP },
                { "{Functie}",         _common.Functie },
                { "{NumeDepartament}", _common.NumeDepartament },
                { "{NrCim}",           _common.NrCim },
                { "{DataCim}",         _common.DataCim != DateTime.MinValue
                                           ? _common.DataCim.ToString("dd.MM.yyyy") : string.Empty },
                { "{SerieCI}",         _common.SerieCI },
                { "{NrCI}",            _common.NrCI },
                { "{Domiciliu}",       _common.Domiciliu },
                { "{today}",           DateTime.Today.ToString("dd.MM.yyyy") },
            };
            foreach (var kv in tokens)
                value = value.Replace(kv.Key, kv.Value ?? string.Empty);
            return value;
        }

        private void OpenListItemDialog(FieldDefinition field,
            List<DynamicListRow> rows, List<ListItemCard> cards, List<Control> items,
            Label lblEmpty, Panel panel, ListItemCard existing)
        {
            int numar = existing != null ? rows.IndexOf(existing.Row) + 1 : rows.Count + 1;
            var editor = new DynamicListRow(field, numar, _theme, _persoane, true);
            editor.TokenResolver = ResolveCommonTokens;
            if (existing != null)
                editor.CopyStateFrom(existing.Row);
            else
                editor.ApplyOnSelectAll();   // element nou: regulile pentru optiunile implicite

            string titlu = string.IsNullOrEmpty(field.DialogTitle) ? field.Label : field.DialogTitle;
            titlu = (existing != null ? "Editare — " : "Adăugare — ") + titlu
                + (existing != null ? " (nr. " + numar + ")" : string.Empty);

            using (var dlg = new ListItemDialog(titlu, editor, _theme))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    editor.Dispose();
                    return;
                }
            }

            if (existing == null)
            {
                AddDialogCard(field, rows, cards, items, lblEmpty, panel, editor);
            }
            else
            {
                // Inlocuim datele cardului cu copia editata
                int idx = rows.IndexOf(existing.Row);
                var old = existing.Row;
                if (idx >= 0) rows[idx] = editor;
                existing.Row = editor;
                old.Dispose();
            }

            RelayoutDialogList(field, rows, cards, items, lblEmpty, panel);
        }

        private void RelayoutDialogList(FieldDefinition field,
            List<DynamicListRow> rows, List<ListItemCard> cards, List<Control> items,
            Label lblEmpty, Panel panel)
        {
            for (int i = 0; i < rows.Count; i++) rows[i].Numar = i + 1;
            for (int i = 0; i < cards.Count; i++) cards[i].Numar = i + 1;

            items.Clear();
            lblEmpty.Visible = cards.Count == 0;
            if (cards.Count == 0) items.Add(lblEmpty);
            else items.AddRange(cards.Cast<Control>());

            // Cardurile in interiorul casetei, cu margini egale
            var scrollPos = panel.AutoScrollPosition;
            int w = Math.Max(panel.ClientSize.Width - 2 * DialogListPadX, 380);
            int yy = DialogListPadY;
            foreach (var c in items)
            {
                c.Width = w;
                c.Left = DialogListPadX;
                c.Top = yy;
                yy += c.Height + DialogListGap;
            }
            panel.AutoScrollPosition = new System.Drawing.Point(-scrollPos.X, -scrollPos.Y);
            UpdateListHeight(field);
        }

        private void AddDynamicListRow(FieldDefinition field,
            List<DynamicListRow> rows, Panel panel)
        {
            var row = new DynamicListRow(field, rows.Count + 1, _theme, _persoane);
            row.TokenResolver = ResolveCommonTokens;
            row.ApplyOnSelectAll();
            row.Width = Math.Max(panel.Width - 2, 400);

            // Restrangere / extindere rand (liste "collapsible")
            row.OnLayoutChanged = () =>
            {
                RelayoutPanel(panel, rows.Cast<Control>().ToList());
                UpdateListHeight(field);
            };

            row.OnDelete = () =>
            {
                if (!ConfirmareStergereDialog.Intreaba(this,
                    string.Format("Ștergi elementul nr. {0} din lista „{1}”?", row.Numar, field.Label),
                    _theme)) return;

                // Salveaza pozitia scroll a sidebar-ului principal
                var bodyScroll = _pnlBody.AutoScrollPosition;

                rows.Remove(row);
                panel.Controls.Remove(row);
                for (int i = 0; i < rows.Count; i++) rows[i].Numar = i + 1;
                RelayoutPanel(panel, rows.Cast<Control>().ToList());
                UpdateListHeight(field);

                // Restaureaza pozitia scroll sidebar dupa relayout
                _pnlBody.AutoScrollPosition = new System.Drawing.Point(
                    -bodyScroll.X, -bodyScroll.Y);
            };

            rows.Add(row);
            panel.Controls.Add(row);

            // Acordeon: la adaugarea unui rand nou, celelalte se restrang
            if (field.Collapsible)
                foreach (var other in rows)
                    if (other != row) other.SetExpanded(false, false);

            RelayoutPanel(panel, rows.Cast<Control>().ToList());
            UpdateListHeight(field);
        }

        // ── Sectiunea speciala pentru clauze Act Aditional ───
        private void BuildClauzeEditorSection(SectionDefinition section, ref int y, int sectionHeight = 0)
        {
            var fieldDef = section.Fields.FirstOrDefault(f => f.Type == "clauze_editor");
            if (fieldDef == null) return;
            int itemsPanelH = sectionHeight > 0 ? sectionHeight : 280;

            // Incarca clauze din fisier
            var config = ActAditionalPlugin.Services.ClauzeService.Load();
            var clauze = config.GetTipSelectat()?.Clauze ?? new System.Collections.Generic.List<ActAditionalPlugin.Models.ClauzeActAditional>();
            var puncte = new System.Collections.Generic.List<PunctModificareControl>();

            int initW = Math.Max(_pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal, 400);

            // Header sectiune cu butoane
            var pnlHdr = new Panel
            {
                Left = 0,
                Top = y,
                Height = 34,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Width = initW
            };
            pnlHdr.Paint += (s, e) =>
                e.Graphics.FillRectangle(new SolidBrush(_theme.Accent), 0, 0, 4, pnlHdr.Height);
            pnlHdr.Controls.Add(new Label
            {
                Text = section.Title,
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = _theme.AccentDark,
                AutoSize = true,
                Location = new Point(8, 8)
            });

            // Buton Editor Clauze
            var btnEditor = new Button
            {
                Text = "⚙ Editor Clauze",
                Height = 26,
                Width = 130,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 173, 78),
                ForeColor = Color.FromArgb(60, 40, 10),
                Cursor = Cursors.Hand,
                Top = 4,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnEditor.FlatAppearance.BorderSize = 1;

            // Buton Adauga Punct
            var btnAdd = new Button
            {
                Text = "+ Adaugă clauză",
                Height = 26,
                Width = 140,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = _theme.Accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Top = 4,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnAdd.FlatAppearance.BorderSize = 1;
            btnAdd.FlatAppearance.BorderColor = _theme.AccentDark;

            pnlHdr.Controls.AddRange(new Control[] { btnEditor, btnAdd });
            pnlHdr.Resize += (s, e) =>
            {
                btnAdd.Left = pnlHdr.Width - btnAdd.Width;
                btnEditor.Left = btnAdd.Left - btnEditor.Width - 6;
            };
            btnAdd.Left = initW - btnAdd.Width;
            btnEditor.Left = btnAdd.Left - btnEditor.Width - 6;

            _pnlBody.Controls.Add(pnlHdr);
            _pnlBody.Resize += (s, e) => pnlHdr.Width = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
            y += 38;

            // Panel iteme clauze — se redimensioneaza la resize pentru a umple spatiul ramas
            var pnlItems = new Panel
            {
                Left = 0,
                Top = y,
                Height = itemsPanelH,
                Width = initW,
                BackColor = Color.FromArgb(240, 245, 255),
                AutoScroll = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _pnlBody.Controls.Add(pnlItems);
            pnlItems.Paint += (s, e) =>
            {
                using (var pen = new Pen(_theme.AccentBorder, 2f))
                    e.Graphics.DrawRectangle(pen, 1, 1, pnlItems.Width - 3, pnlItems.Height - 3);
                e.Graphics.FillRectangle(new SolidBrush(_theme.Accent), 0, 0, 4, pnlItems.Height);
            };
            int clauzeTop = y; // captureaza y-ul la care incepe panelul
            _pnlBody.Resize += (s, e) =>
            {
                pnlItems.Width = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
                // Redimensioneaza inaltimea sa umple spatiul ramas din body
                int available = _pnlBody.ClientSize.Height - clauzeTop - 8;
                if (available > 100) pnlItems.Height = available;
                foreach (Control c in pnlItems.Controls)
                    c.Width = Math.Max(pnlItems.ClientSize.Width - 4, 380);
            };
            y += itemsPanelH + 8;

            // Stocheaza referinta la puncte pentru CollectFormValues
            _dynamicLists[fieldDef.Key] = new System.Collections.Generic.List<DynamicListRow>();
            _clauzeControls[fieldDef.Key] = puncte;
            _clauzeItemsPanel[fieldDef.Key] = pnlItems;

            // Handler add clauza
            Action addPunct = () =>
            {
                var ctrl = new PunctModificareControl(puncte.Count + 1, clauze);
                ctrl.Width = Math.Max(pnlItems.ClientSize.Width - 4, 380);
                ctrl.OnDelete = () =>
                {
                    if (!ConfirmareStergereDialog.Intreaba(this,
                        string.Format("Ștergi clauza nr. {0}?", ctrl.Numar), _theme)) return;
                    puncte.Remove(ctrl);
                    pnlItems.Controls.Remove(ctrl);
                    for (int i = 0; i < puncte.Count; i++) puncte[i].Numar = i + 1;
                    RelayoutPunctModificare(pnlItems, puncte);
                };
                ctrl.OnHeightChanged = () => RelayoutPunctModificare(pnlItems, puncte);
                puncte.Add(ctrl);
                pnlItems.Controls.Add(ctrl);
                RelayoutPunctModificare(pnlItems, puncte);
            };

            btnAdd.Click += (s, e) => addPunct();

            // Handler editor clauze
            btnEditor.Click += (s, e) =>
            {
                using (var dlg = new ClauzeEditorDialog())
                    dlg.ShowDialog(this);
                // Reincarca clauze dupa editare
                var cfg2 = ActAditionalPlugin.Services.ClauzeService.Load();
                var clauzeNoi = cfg2.GetTipSelectat()?.Clauze
                    ?? new System.Collections.Generic.List<ActAditionalPlugin.Models.ClauzeActAditional>();
                clauze.Clear(); clauze.AddRange(clauzeNoi);
                foreach (var p in puncte) p.SetClauze(clauze);
            };

            // Adauga 2 clauze initiale goale
            addPunct();
            addPunct();
        }

        private void RelayoutPunctModificare(Panel pnl,
            System.Collections.Generic.List<PunctModificareControl> items)
        {
            int w = Math.Max(pnl.ClientSize.Width - 4, 380);
            int y2 = 4;
            foreach (var c in items) { c.Width = w; c.Left = 2; c.Top = y2; y2 += c.Height + 4; }
        }

        private void BuildRegularFieldInline(FieldDefinition field, ref int y)
        {
            // Camp simplu adaugat inline (fara sectiune proprie)
            // folosit pentru campuri mixte in sectiuni cu dynamic_list
            int rowHeight = FieldControlHeight(field) + 28;
            var pnl = AddSectiune(field.Label, ref y, rowHeight + 8);
            var ctrl = BuildFieldControl(field);
            if (ctrl == null) return;
            var tbl = AddRow(pnl, new[] { 100 }, rowHeight);
            AddLabeledInput(tbl, 0, field.Label, ctrl, field.Required);
            _controls[field.Key] = ctrl;
        }



        // ══════════════════════════════════════════════════════
        //  MENTIUNI — panel fix ancorat deasupra footer-ului
        //  Dock = Bottom in split.Panel1.
        //  La bifare apare panoul cu textarea deasupra bifei.
        // ══════════════════════════════════════════════════════
        private Panel BuildMentiuniWrapper()
        {
            const int ChkH = 34;
            const int MentiuniH = 116; // header 24 + textarea 80 + padding 12

            var wrapper = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = ChkH,
                BackColor = Color.FromArgb(248, 249, 252)
            };
            wrapper.Paint += (s, e) =>
            {
                using (var pen = new Pen(_theme.AccentBorder))
                    e.Graphics.DrawLine(pen, 0, 0, wrapper.Width, 0);
            };

            // ── Panel mentiuni (deasupra bifei, initial ascuns) ─
            _pnlMentiuni = new Panel
            {
                Left = 0,
                Top = 0,
                Height = MentiuniH,
                BackColor = Color.White,
                Visible = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _pnlMentiuni.Paint += (s, e) =>
            {
                using (var pen = new Pen(_theme.AccentBorder))
                    e.Graphics.DrawLine(pen, 0, 0, _pnlMentiuni.Width, 0);
                e.Graphics.FillRectangle(new SolidBrush(_theme.Accent), 0, 0, 4, _pnlMentiuni.Height);
            };
            _pnlMentiuni.Controls.Add(new Label
            {
                Text = "MENȚIUNI / OBSERVAȚII",
                Font = new Font("Segoe UI Semibold", 9f),
                ForeColor = _theme.AccentDark,
                AutoSize = true,
                Location = new Point(14, 4)
            });
            _txtMentiuni = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.White,
                ForeColor = TextPrincipal,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(14, 26),
                Height = 80,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _pnlMentiuni.Controls.Add(_txtMentiuni);
            _pnlMentiuni.Resize += (s, e) =>
                _txtMentiuni.Width = _pnlMentiuni.ClientSize.Width - 28;

            // ── Bifa (mereu vizibila, jos in wrapper) ──────────
            _chkMentiuni = new CheckBox
            {
                Text = "Adaugă mențiuni (nu apar în PDF)",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextSecundar,
                AutoSize = true,
                Left = 12,
                Top = 8,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            wrapper.Controls.Add(_pnlMentiuni);
            wrapper.Controls.Add(_chkMentiuni);

            // ── La resize, mentiuni ocupa toata latimea ─────────
            wrapper.Resize += (s, e) =>
            {
                _pnlMentiuni.Width = wrapper.ClientSize.Width;
                _chkMentiuni.Top = wrapper.ClientSize.Height - ChkH + 8;
            };

            _chkMentiuni.CheckedChanged += (s, e) =>
            {
                bool on = _chkMentiuni.Checked;
                _pnlMentiuni.Visible = on;
                wrapper.Height = on ? ChkH + MentiuniH : ChkH;
                // Repoziționeaza bifa la baza wrapper-ului
                _chkMentiuni.Top = wrapper.ClientSize.Height - ChkH + 8;
                if (!on && _txtMentiuni != null)
                    _txtMentiuni.Text = string.Empty;
            };

            return wrapper;
        }

        // ══════════════════════════════════════════════════════
        //  COLECTARE VALORI DIN FORMULAR
        // ══════════════════════════════════════════════════════
        private Dictionary<string, object> CollectFormValues()
        {
            var values = new Dictionary<string, object>(_formValues);

            // Campuri simple
            foreach (var def in _def.Sections.SelectMany(s => s.Fields)
                .Where(f => f.Type != "dynamic_list" && f.Type != "person_picker"))
            {
                if (!_controls.ContainsKey(def.Key)) continue;
                values[def.Key] = GetControlValue(_controls[def.Key], def.Type);
            }

            // Liste dinamice
            foreach (var kv in _dynamicLists)
            {
                // Sare listele de clauze (sunt tratate separat)
                if (_clauzeControls.ContainsKey(kv.Key)) continue;

                var rowValues = kv.Value
                    .Where(r => r.IsValid())
                    .Select(r => r.GetValues())
                    .ToList();
                values[kv.Key] = rowValues;
            }

            // Clauze Act Aditional
            foreach (var kv in _clauzeControls)
            {
                var rowValues = new List<Dictionary<string, string>>();
                int nr = 1;
                foreach (var ctrl in kv.Value)
                {
                    var punct = ctrl.GetPunct();
                    if (punct == null) continue;
                    rowValues.Add(new Dictionary<string, string>
                    {
                        { "ModificareNr", nr.ToString() },
                        { "ModificareReferinta", punct.Referinta ?? string.Empty },
                        { "ModificareText", punct.TextModificare ?? string.Empty }
                    });
                    nr++;
                }
                values[kv.Key] = rowValues;
            }

            // Mentiuni
            values["MentiuniDocument"] = _txtMentiuni != null
                ? GetTextValue(_txtMentiuni) : string.Empty;

            return values;
        }

        // ══════════════════════════════════════════════════════
        //  VALIDARE
        // ══════════════════════════════════════════════════════
        private bool ValidateForPreview()
        {
            if (string.IsNullOrWhiteSpace(_common.CodInregistrare))
            {
                MessageBox.Show("Codul de înregistrare nu a putut fi calculat.",
                    "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private bool ValidateForGenerate()
        {
            if (!ValidateForPreview()) return false;

            foreach (var field in _def.Sections.SelectMany(s => s.Fields))
            {
                if (!field.Required) continue;
                if (!_controls.ContainsKey(field.Key)) continue;
                if (!string.IsNullOrEmpty(field.DependsOn) && !IsDependencyActive(field)) continue;

                string val;
                if (field.Type == "person_picker")
                {
                    // Valoarea pickerului e in _formValues (controlul e doar un buton)
                    object pv;
                    val = _formValues.TryGetValue(field.Key, out pv) && pv != null
                        ? pv.ToString() : string.Empty;
                }
                else
                    val = GetControlValue(_controls[field.Key], field.Type)?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(val))
                {
                    MessageBox.Show(
                        string.Format("Câmpul \"{0}\" este obligatoriu.", field.Label),
                        "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _controls[field.Key].Focus();
                    return false;
                }
            }

            // Valideaza liste dinamice obligatorii
            foreach (var field in _def.Sections.SelectMany(s => s.Fields)
                .Where(f => f.Type == "dynamic_list" && f.Required))
            {
                if (!_dynamicLists.ContainsKey(field.Key)) continue;
                if (!_dynamicLists[field.Key].Any(r => r.IsValid()))
                {
                    MessageBox.Show(
                        string.Format("Adaugă cel puțin un element în \"{0}\".", field.Label),
                        "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            return true;
        }

        // ══════════════════════════════════════════════════════
        //  PREVIEW
        // ══════════════════════════════════════════════════════
        private void OnPreview(object sender, EventArgs e)
        {
            if (!ValidateForPreview()) return;

            Cursor = Cursors.WaitCursor;
            try
            {
                var formValues = CollectFormValues();

                // Ruleaza hooks on_generate (pentru preview)
                RunHooks("on_generate", formValues);

                // Inchide viewer curent
                if (_pdfViewer.Document != null)
                {
                    _pdfViewer.Document.Dispose();
                    _pdfViewer.Document = null;
                }
                TryDeleteFile(_currentPdfPath);

                // Genereaza DOCX preview
                string tempDocx = DynamicTemplateEngine.GeneratePreviewDocx(
                    _def, formValues, _common);

                // Converite la PDF
                string tempPdf = Path.ChangeExtension(tempDocx, ".pdf");
                WordHelper.ConvertToPdf(tempDocx, tempPdf);
                TryDeleteFile(tempDocx);

                _currentPdfPath = tempPdf;
                _pdfViewer.Document = PdfDocument.Load(tempPdf);
                _pdfViewer.Visible = true;
                _pdfViewer.BringToFront();
                _lblPlaceholder.Visible = false;

                if (!_previewDone)
                {
                    _previewDone = true;
                    _btnActualizeaza.Text = " Actualizează Previzualizarea";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la previzualizare:\n" + ex.Message,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        // ══════════════════════════════════════════════════════
        //  GENERATE PDF FINAL
        // ══════════════════════════════════════════════════════
        private void OnGenerate(object sender, EventArgs e)
        {
            if (!ValidateForGenerate()) return;

            var formValues = CollectFormValues();

            // Ruleaza hooks on_generate
            RunHooks("on_generate", formValues);

            // Dialog confirmare + registratura
            DateTime dataReg = GetRegistraturaDate(formValues);
            bool cuRegistratura = _def.Registratura && RegistraturaService.Instance != null;

            // Cod proaspat (altcineva poate fi inregistrat un document intre timp)
            if (cuRegistratura) RecalcCodInregistrare(dataReg);

            using (var dlg = new ConfirmareDialog(_def.Title, _common.CodInregistrare, dataReg, _theme))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
            }

            Cursor = Cursors.WaitCursor;
            string pdfPath = null;
            try
            {
                // 1. PDF-ul se genereaza INAINTE de inregistrare: daca generarea
                //    esueaza, nu ramane niciun rand in registratura.
                //    Codul se recalculeaza chiar inainte de generare si se verifica
                //    ca e inca liber; daca intre timp a fost luat, se regenereaza
                //    PDF-ul cu codul urmator.
                const int MaxIncercari = 3;
                for (int incercare = 1; ; incercare++)
                {
                    if (cuRegistratura) RecalcCodInregistrare(dataReg);
                    pdfPath = DynamicTemplateEngine.GeneratePdf(_def, formValues, _common);

                    if (!cuRegistratura
                        || !RegistraturaService.Instance.CodExista(_common.CodInregistrare))
                        break;

                    TryDeleteFile(pdfPath);
                    pdfPath = null;
                    if (incercare >= MaxIncercari)
                        throw new InvalidOperationException(
                            "Nu s-a putut obține un cod de înregistrare liber. Încearcă din nou.");
                }

                // 2. Inregistrare. Daca esueaza, PDF-ul generat se sterge, ca sa nu
                //    ramana un document cu un cod neinregistrat.
                if (cuRegistratura)
                {
                    try
                    {
                        RegistraturaService.Instance.Inregistreaza(
                            _common.CodInregistrare, dataReg,
                            _def.RegistraturaTipDocPk,
                            _def.Title,
                            _common.PrsnId);
                    }
                    catch
                    {
                        TryDeleteFile(pdfPath);
                        pdfPath = null;
                        throw;
                    }

                    // Insert in tabela specifica categoriei (Acte Aditionale / Decizii / PV)
                    // Transmitem titlul documentului prin formValues pentru tipDcz/pvType
                    formValues["_DocTitle"] = _def.Title;
                    RegistraturaService.Instance.InregistreazaTabelaSpecifica(
                        _def.Category,
                        _common.CodInregistrare,
                        dataReg,
                        _common.PrsnId,
                        formValues);
                }

                Cursor = Cursors.Default;
                using (var dlg = new SuccessDialog(_def.Title, _common.CodInregistrare, dataReg, _theme))
                    dlg.ShowDialog(this);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                // Codul afisat in antet se reactualizeaza pentru urmatoarea incercare
                if (cuRegistratura)
                {
                    try { RecalcCodInregistrare(dataReg); } catch { }
                }
                MessageBox.Show("Eroare la generare PDF:\n" + ex.Message,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════
        //  HOOKS
        // ══════════════════════════════════════════════════════
        private void RunHooks(string onEvent, Dictionary<string, object> formValues)
        {
            if (_hooksRunning) return;
            _hooksRunning = true;
            try
            {
                // Colecteaza valorile curente din controale pentru on_change
                if (onEvent == "on_change" && formValues == null)
                {
                    foreach (var def2 in _def.Sections.SelectMany(s => s.Fields))
                    {
                        // person_picker: controlul (Panel) nu are valoare proprie —
                        // valoarea e deja in _formValues, nu o suprascriem cu ""
                        if (def2.Type == "person_picker") continue;
                        if (_controls.ContainsKey(def2.Key))
                            _formValues[def2.Key] = GetControlValue(_controls[def2.Key], def2.Type);
                    }
                }

                var ctx = new HookContext
                {
                    Definition = _def,
                    FormValues = formValues ?? _formValues,
                    Common = _common,
                    XSupport = BulkContext.XSupport
                };
                HookRegistry.RunHooks(onEvent, ctx);

                // Dupa on_open sau on_change, actualizeaza controalele
                if ((onEvent == "on_open" || onEvent == "on_change") && formValues == null)
                {
                    var keys = new List<string>(_formValues.Keys);
                    foreach (var key in keys)
                    {
                        if (_controls.ContainsKey(key))
                            SetControlValue(_controls[key], _formValues[key]?.ToString() ?? string.Empty);
                    }
                    ApplyFieldDependencies();
                }
            }
            finally
            {
                _hooksRunning = false;
            }
        }

        // ══════════════════════════════════════════════════════
        //  DEPENDS_ON — activeaza/dezactiveaza campuri in functie
        //  de valoarea altui camp (ex. selector cu variante).
        //  Campurile raman mereu vizibile; doar editabilitatea
        //  comuta (readonly pentru text, enabled pentru restul).
        // ══════════════════════════════════════════════════════
        // Suporta conditii multiple (AND) separate prin "|", ex:
        // depends_on = "TipCerere|TipRecuperareInvoire"
        // depends_value = "Învoire|Cu zile/ore de recuperare"
        private bool IsDependencyActive(FieldDefinition field)
        {
            return IsDependencyActive(field.DependsOn, field.DependsValue);
        }

        private bool IsDependencyActive(string dependsOn, string dependsValue)
        {
            if (string.IsNullOrEmpty(dependsOn)) return true;

            var onKeys = dependsOn.Split('|');
            var onValues = (dependsValue ?? string.Empty).Split('|');
            bool active = onKeys.Length == onValues.Length;
            for (int i = 0; active && i < onKeys.Length; i++)
            {
                object curObj;
                string cur = _formValues.TryGetValue(onKeys[i], out curObj)
                    ? (curObj != null ? curObj.ToString() : string.Empty)
                    : string.Empty;
                active = string.Equals(cur, onValues[i], StringComparison.Ordinal);
            }
            return active;
        }

        private void ApplyFieldDependencies()
        {
            foreach (var field in _def.Sections.SelectMany(s => s.Fields))
            {
                if (string.IsNullOrEmpty(field.DependsOn)) continue;
                if (!_controls.ContainsKey(field.Key)) continue;

                bool active = IsDependencyActive(field);
                var ctrl = _controls[field.Key];
                var tb = ctrl as TextBox;
                if (tb != null)
                {
                    tb.ReadOnly = !active;
                    tb.BackColor = active ? Color.White : Color.FromArgb(208, 213, 226);
                }
                else if (ctrl is DateTimePicker || ctrl is NumericUpDown || ctrl is ComboBox)
                {
                    ctrl.Enabled = active;
                }
            }

            // Sectiuni cu depends_on: ascunse / afisate in formular
            UpdateSectionVisibility();
        }

        // ══════════════════════════════════════════════════════
        //  COD INREGISTRARE
        // ══════════════════════════════════════════════════════
        /// <summary>
        /// Precompletare automata a campurilor de CI si Domiciliu ale angajatului principal
        /// (PRIMITOR), daca JSON-ul are campuri cu key-urile cunoscute si acestea sunt goale.
        /// Mapare: "CISeria" / "SerieCI" -> _common.SerieCI
        ///         "CINr"    / "NrCI"    -> _common.NrCI
        ///         "Domiciliu"           -> _common.Domiciliu
        /// </summary>
        // ══════════════════════════════════════════════════════
        //  Precompleteaza orice camp readonly din formular al
        //  carui key se potriveste cu o valoare cunoscuta din
        //  `common` (angajat + companie). Util pentru documente
        //  fara input de la operator, unde formularul e doar o
        //  confirmare vizuala a datelor care vor fi inserate
        //  (ex. Acord Semnatura Electronica).
        //  Extinde ce inainte era doar CI/Domiciliu.
        // ══════════════════════════════════════════════════════
        private void PrePopulateReadonlyCommonFields()
        {
            var mappings = new System.Collections.Generic.Dictionary<string, string>
            {
                // ── Angajat ──────────────────────────────────────
                { "CISeria",         _common.SerieCI          },
                { "SerieCI",         _common.SerieCI          },
                { "CINr",            _common.NrCI              },
                { "NrCI",            _common.NrCI              },
                { "Domiciliu",       _common.Domiciliu         },
                { "NumeSalariat",    _common.NumeSalariat      },
                { "CNP",             _common.CNP               },
                { "Functie",         _common.Functie           },
                { "NumeDepartament", _common.NumeDepartament   },
                { "NrCim",           _common.NrCim             },
                { "NumarTelefonSalariat", _common.TelefonSalariat },
                { "EmailSalariat",   _common.EmailSalariat     },

                // ── Companie ─────────────────────────────────────
                { "NumeAngajator",     _common.NumeAngajator     },
                { "AdresaCompanie",    _common.AdresaCompanie    },
                { "NrRegComertului",   _common.NrRegComertului   },
                { "CIFAngajator",      _common.CIFAngajator      },
                { "ReprezentantLegal", _common.ReprezentantLegal },
            };

            foreach (var kv in mappings)
            {
                if (string.IsNullOrWhiteSpace(kv.Value)) continue;
                if (!_controls.ContainsKey(kv.Key)) continue;

                // Precompletam doar daca campul e inca gol
                object existing;
                if (_formValues.TryGetValue(kv.Key, out existing)
                    && !string.IsNullOrWhiteSpace(existing?.ToString())) continue;

                _formValues[kv.Key] = kv.Value;
                SetControlValue(_controls[kv.Key], kv.Value);
            }
        }

        private void RecalcCodInregistrare(DateTime? data = null)
        {
            if (RegistraturaService.Instance == null) return;
            DateTime dataRef = data ?? DateTime.Today;
            string cod = RegistraturaService.Instance.CalculateCod(dataRef);
            _common.CodInregistrare = cod;
            if (_txtCodInregistrare != null)
                _txtCodInregistrare.Text = cod;
        }

        private DateTime GetRegistraturaDate(Dictionary<string, object> formValues)
        {
            if (!string.IsNullOrEmpty(_def.RegistraturaDateField)
                && formValues.ContainsKey(_def.RegistraturaDateField))
            {
                DateTime d;
                if (DateTime.TryParse(
                    formValues[_def.RegistraturaDateField]?.ToString(), out d))
                    return d;
            }
            return DateTime.Today;
        }

        // ══════════════════════════════════════════════════════
        //  LAYOUT HELPERS (din FormBase, replicate)
        // ══════════════════════════════════════════════════════
        private void AddSectiuneHeader(string titlu, ref int y)
        {
            var pnlHdr = new Panel
            {
                Left = 0,
                Top = y,
                Height = 30,
                BackColor = _theme.AccentPal,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Width = Math.Max(_pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal, 400)
            };
            pnlHdr.Paint += (s, e) =>
                e.Graphics.FillRectangle(new SolidBrush(_theme.Accent), 0, 0, 4, pnlHdr.Height);
            pnlHdr.Controls.Add(new Label
            {
                Text = titlu,
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = _theme.AccentDark,
                AutoSize = true,
                Location = new Point(14, 6)
            });
            _pnlBody.Controls.Add(pnlHdr);
            _pnlBody.Resize += (s, e) =>
                pnlHdr.Width = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
            y += 34;
        }

        private Panel AddSectiune(string titlu, ref int y, int height)
        {
            AddSectiuneHeader(titlu, ref y);

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Left = 0,
                Top = y,
                Height = height,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8),
                AutoSize = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Width = Math.Max(_pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal, 400)
            };
            flow.Paint += PaintBorder;
            _pnlBody.Controls.Add(flow);
            _pnlBody.Resize += (s, e) =>
            {
                int w = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
                flow.Width = w;
                foreach (Control c in flow.Controls)
                    c.Width = flow.ClientSize.Width - flow.Padding.Horizontal;
            };
            y += height + 14;
            return flow;
        }

        // Margini pentru panoul listelor "dialog" (caseta alba cu carduri in interior)
        private const int DialogListPadX = 12;
        private const int DialogListPadY = 10;
        private const int DialogListGap = 8;

        private Panel AddDynamicListSection(string titlu, string btnText,
            Action onAdd, ref int y, int itemsPanelHeight = 200, bool dialogStyle = false)
        {
            if (!string.IsNullOrEmpty(titlu))
                AddSectiuneHeader(titlu, ref y);

            int initW = Math.Max(_pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal, 400);

            // Header cu buton add
            var pnlHdr2 = new Panel
            {
                Left = 0,
                Top = y,
                Height = 34,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Width = initW
            };
            var btnAdd = new Button
            {
                Text = "+ " + btnText,
                Height = 26,
                Width = Math.Max(160, TextRenderer.MeasureText("+ " + btnText,
                    new Font("Segoe UI", 9f, FontStyle.Bold)).Width + 32),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = _theme.Accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Top = 4,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnAdd.FlatAppearance.BorderColor = _theme.AccentDark;
            btnAdd.FlatAppearance.BorderSize = 1;
            btnAdd.MouseEnter += (s, e) => btnAdd.BackColor = _theme.AccentDark;
            btnAdd.MouseLeave += (s, e) => btnAdd.BackColor = _theme.Accent;
            btnAdd.Click += (s, e) => onAdd();
            pnlHdr2.Controls.Add(btnAdd);
            pnlHdr2.Resize += (s, e) => btnAdd.Left = pnlHdr2.Width - btnAdd.Width;
            btnAdd.Left = initW - btnAdd.Width;
            _pnlBody.Controls.Add(pnlHdr2);
            _pnlBody.Resize += (s, e) => pnlHdr2.Width = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
            y += 38;

            var pnlItems = new Panel
            {
                Left = 0,
                Top = y,
                Height = itemsPanelHeight,
                Width = initW,
                BackColor = dialogStyle ? Color.White : Color.FromArgb(248, 244, 254),
                AutoScroll = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            if (dialogStyle)
            {
                // Caseta alba cu chenar subtire, ca la celelalte sectiuni; cardurile
                // stau in interior cu margini egale (fara chenar dublu).
                pnlItems.Paint += PaintBorder;
            }
            else
            {
                pnlItems.Paint += (s, e) =>
                {
                    using (var pen = new Pen(_theme.AccentBorder, 2f))
                        e.Graphics.DrawRectangle(pen, 1, 1, pnlItems.Width - 3, pnlItems.Height - 3);
                    e.Graphics.FillRectangle(new SolidBrush(_theme.Accent), 0, 0, 4, pnlItems.Height);
                };
            }
            int itemMargin = dialogStyle ? DialogListPadX : 2;
            _pnlBody.Controls.Add(pnlItems);
            _pnlBody.Resize += (s, e) =>
            {
                pnlItems.Width = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
                foreach (Control c in pnlItems.Controls)
                    c.Width = Math.Max(pnlItems.ClientSize.Width - 2 * itemMargin, 380);
            };
            y += itemsPanelHeight + 8;
            return pnlItems;
        }

        // Inaltimea controlului in functie de tipul campului (multiline e mai mare)
        private static int FieldControlHeight(FieldDefinition f) =>
            f.Type == "multiline" ? MultilineHeight : 26;

        private TableLayoutPanel AddRow(Panel parent, int[] percents, int height = 54)
        {
            var tbl = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = percents.Length,
                Height = height,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0, 4, 0, 0),
                Width = Math.Max(parent.ClientSize.Width - parent.Padding.Horizontal, 400)
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            foreach (int p in percents)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, p));
            parent.Controls.Add(tbl);
            parent.Resize += (s, e) =>
                tbl.Width = Math.Max(parent.ClientSize.Width - parent.Padding.Horizontal, 400);
            return tbl;
        }

        private static void AddLabeledInput(TableLayoutPanel tbl, int col,
            string labelText, Control ctrl, bool required = false)
        {
            var cell = new Panel
            {
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 8, 0)
            };
            var pnlLbl = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text = labelText,
                Font = FLabel,
                ForeColor = Color.FromArgb(55, 75, 105),
                AutoSize = true,
                Location = new Point(0, 2)
            };
            pnlLbl.Controls.Add(lbl);
            if (required)
            {
                var star = new Label
                {
                    Text = " *",
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(200, 50, 40),
                    AutoSize = true,
                    Location = new Point(lbl.PreferredWidth, 2)
                };
                lbl.SizeChanged += (s, e) => star.Left = lbl.Right;
                pnlLbl.Controls.Add(star);
            }
            ctrl.Dock = DockStyle.Top;
            if (!(ctrl is TextBox tbHeight && tbHeight.Multiline))
                ctrl.Height = 26;
            cell.Controls.Add(ctrl);
            cell.Controls.Add(pnlLbl);
            tbl.Controls.Add(cell, col, 0);
        }

        // ── Factory controale ──────────────────────────────────
        private static TextBox MakeReadonly() => new TextBox
        {
            ReadOnly = true,
            BackColor = Color.FromArgb(208, 213, 226),
            ForeColor = Color.FromArgb(60, 75, 100),
            Font = FInput,
            BorderStyle = BorderStyle.FixedSingle
        };

        private static TextBox MakeInput() => new TextBox
        {
            BackColor = Color.White,
            ForeColor = TextPrincipal,
            Font = FInput,
            BorderStyle = BorderStyle.FixedSingle
        };

        private TextBox MakeMultiline(int height = 88) => new TextBox
        {
            Multiline = true,
            Height = height,
            BackColor = Color.White,
            ForeColor = TextPrincipal,
            Font = FInput,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical
        };

        private static DateTimePicker MakeDtp() => new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today,
            Font = FInput,
            Height = 26
        };

        private NoScrollComboBox MakeCombo()
        {
            var cmb = new NoScrollComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FInput,
                BackColor = Color.White,
                ForeColor = TextPrincipal
            };
            return cmb;
        }

        private static readonly Font FInputRegular = new Font("Segoe UI", 10f);

        private static void SetPlaceholder(TextBox tb, string ph)
        {
            tb.Text = ph;
            tb.ForeColor = Color.Gray;
            tb.Font = FInputRegular;
            tb.GotFocus += (s, e) =>
            {
                if (tb.ForeColor == Color.Gray)
                {
                    tb.Text = string.Empty;
                    tb.ForeColor = TextPrincipal;
                    tb.Font = FInput;
                }
            };
            tb.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = ph;
                    tb.ForeColor = Color.Gray;
                    tb.Font = FInputRegular;
                }
            };
        }

        // ── Valori controale ───────────────────────────────────
        private static string GetControlValue(Control ctrl, string type)
        {
            if (ctrl is DateTimePicker dtp)
                return type == "datetime"
                    ? dtp.Value.ToString("dd.MM.yyyy HH:mm")
                    : dtp.Value.Date.ToString("dd.MM.yyyy");
            if (ctrl is ComboBox cmb)
                return cmb.SelectedItem?.ToString() ?? string.Empty;
            if (ctrl is NumericUpDown nud2)
                return ((int)nud2.Value).ToString();
            if (ctrl is TextBox tb)
                return tb.ForeColor == Color.Gray ? string.Empty : tb.Text.Trim();
            if (ctrl is Panel pnl)  // person_picker wrapper
                return string.Empty; // valoarea e in _formValues
            return string.Empty;
        }

        private static void SetControlValue(Control ctrl, string val)
        {
            if (ctrl is NumericUpDown nud2)
            {
                int v; if (int.TryParse(val, out v)) nud2.Value = v;
            }
            else if (ctrl is TextBox tb)
            {
                tb.Text = val;
                tb.ForeColor = TextPrincipal;
            }
            else if (ctrl is DateTimePicker dtp)
            {
                DateTime d;
                if (DateTime.TryParse(val, out d)) dtp.Value = d;
            }
        }

        private static string GetTextValue(TextBox tb)
            => tb.ForeColor == Color.Gray ? string.Empty : tb.Text.Trim();

        // ── PersonInfo property getter ─────────────────────────
        private static string GetPersonProperty(PersonInfo p, string propName)
        {
            switch (propName)
            {
                case "NumeComplet": return p.NumeComplet;
                case "Nume": return p.Nume;
                case "Prenume": return p.Prenume;
                case "CNP": return p.CNP;
                case "Functie": return p.Functie;
                case "CodCor": return p.CodCor;
                case "NrCim": return p.NrCim;
                case "NumeDepartament": return p.NumeDepartament;
                case "DataCim": return p.DataCimFormatata;
                case "SerieCI": return p.SerieCI;
                case "NrCI": return p.NrCI;
                case "Domiciliu": return p.Domiciliu;
                case "NumeComplet_Functie":
                    return string.IsNullOrEmpty(p.Functie)
                        ? p.NumeComplet
                        : p.NumeComplet + " — " + p.Functie;
                default: return string.Empty;
            }
        }

        // ── Recalculeaza AutoScrollMinSize dupa modificari dinamice ─
        private void UpdateBodyScrollHeight()
        {
            if (_pnlBody == null) return;
            int maxBottom = 0;
            foreach (Control c in _pnlBody.Controls)
            {
                int bottom = c.Top + c.Height;
                if (bottom > maxBottom) maxBottom = bottom;
            }
            _pnlBody.AutoScrollMinSize = new System.Drawing.Size(0, maxBottom + 80);
        }

        // ── Repoziționează toate controalele din _pnlBody în ordine ──
        // Apelat când un pnlItems crește (lista dinamică adaugă iteme).
        // Sortează controalele după Top curent și le reașează secvențial,
        // astfel că cele de sub lista crescută coboară automat.
        private void ReflowBodyControls()
        {
            if (_pnlBody == null) return;

            // Sortam controalele dupa pozitia Top curenta
            var sorted = _pnlBody.Controls.Cast<Control>()
                .OrderBy(c => c.Top)
                .ToList();

            int y = 0;
            foreach (var c in sorted)
            {
                c.Top = y;
                y += c.Height + (c.Height > 0 ? 8 : 0);
            }

            _pnlBody.AutoScrollMinSize = new System.Drawing.Size(0, y + 80);
        }

        // ── Calcul height sectiune ─────────────────────────────
        private static int CalcSectionHeight(SectionDefinition section)
        {
            // Daca JSON specifica inaltimea explicit, o folosim direct
            if (section.Height > 0) return section.Height;

            int h = 16; // padding
            foreach (var f in section.Fields.Where(f => f.Type != "dynamic_list"))
            {
                h += f.Type == "multiline" ? MultilineHeight + 32 : 58;
            }
            return Math.Max(h, 62);
        }

        // ── Static helpers ─────────────────────────────────────
        private void RelayoutPanel(Panel pnl, List<Control> items)
        {
            var scrollPos = pnl.AutoScrollPosition;
            int w = Math.Max(pnl.ClientSize.Width - 4, 380);
            int y = 4;
            foreach (var c in items) { c.Width = w; c.Left = 2; c.Top = y; y += c.Height + 4; }
            if (pnl.AutoScroll)
                pnl.AutoScrollPosition = new System.Drawing.Point(-scrollPos.X, -scrollPos.Y);
        }

        private static Button MakeFooterButton(string text, Image icon, Color bg, Color fg)
        {
            var img = ResizeImage(icon, 16, 16);
            var btn = new Button
            {
                Text = string.Empty,
                Height = 36,
                Width = 130,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 10
            };
            btn.FlatAppearance.BorderSize = 0; // desenam borderul manual

            bool hovered = false;
            bool pressed = false;

            btn.MouseEnter += (s, e) => { hovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { hovered = false; pressed = false; btn.Invalidate(); };
            btn.MouseDown += (s, e) => { pressed = true; btn.Invalidate(); };
            btn.MouseUp += (s, e) => { pressed = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                var b = (Button)s;
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Culoare fundal cu hover/press
                Color drawBg = pressed
                    ? ControlPaint.Dark(bg, 0.25f)
                    : hovered
                        ? ControlPaint.Dark(bg, 0.10f)
                        : bg;
                g.Clear(drawBg);

                // Border 2px - mai inchis la hover/press
                Color borderColor = b.FlatAppearance.BorderColor == Color.Empty
                    ? ControlPaint.Dark(bg, 0.2f)
                    : b.FlatAppearance.BorderColor;
                if (hovered || pressed)
                    borderColor = ControlPaint.Dark(borderColor, 0.2f);
                using (var pen = new Pen(borderColor, 2f))
                    g.DrawRectangle(pen, 1, 1, b.Width - 3, b.Height - 3);

                // Icoana + text centrate
                const int iconW = 16, iconH = 16, gap = 6;
                string label = text.TrimStart();
                int textW = (int)g.MeasureString(label, b.Font).Width;
                int totalW = iconW + gap + textW;
                int startX = (b.Width - totalW) / 2;
                int iconY = (b.Height - iconH) / 2;

                g.DrawImage(img, startX, iconY, iconW, iconH);

                var sf = new System.Drawing.StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(label, b.Font, new SolidBrush(fg),
                    new RectangleF(startX + iconW + gap, 0, textW + 4, b.Height), sf);
            };
            return btn;
        }

        private static void PaintBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using (var pen = new Pen(Color.FromArgb(200, 215, 235)))
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        private static Image ResizeImage(Image img, int w, int h)
        {
            var bmp = new System.Drawing.Bitmap(w, h);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, w, h);
            }
            return bmp;
        }

        private static void TryDeleteFile(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                try { File.Delete(path); } catch { }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  PANEL CU SCROLL PRIORITAR
    //  Intercepteaza WM_MOUSEWHEEL inaintea unui control copil cu
    //  focus (NumericUpDown, ComboBox, DateTimePicker) care altfel
    //  ar consuma scroll-ul in loc sa lase panelul sa derulze.
    // ══════════════════════════════════════════════════════════
    public class PriorityScrollPanel : Panel, IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x020A;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct NativePoint { public int X; public int Y; }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePoint pt);

        public PriorityScrollPanel()
        {
            AutoScroll = true;
            HandleCreated += (s, e) => Application.AddMessageFilter(this);
            HandleDestroyed += (s, e) => Application.RemoveMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL || !IsHandleCreated || !Visible)
                return false;

            var screenPos = new Point(unchecked((short)(m.LParam.ToInt64() & 0xFFFF)),
                                       unchecked((short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
            if (!RectangleToScreen(ClientRectangle).Contains(screenPos))
                return false;

            // Fereastra de sub cursor trebuie sa fie panelul sau un copil al lui.
            // Altfel (ex. un dialog modal — PersonPickerDialog — deschis peste
            // formular) lasam mesajul sa ajunga la dialog, ca lista lui sa derulze.
            var under = Control.FromChildHandle(
                WindowFromPoint(new NativePoint { X = screenPos.X, Y = screenPos.Y }));
            if (under == null || (under != this && !Contains(under)))
                return false;

            int delta = unchecked((short)((m.WParam.ToInt64() >> 16) & 0xFFFF));
            int newY = Math.Max(0, -AutoScrollPosition.Y - delta);
            AutoScrollPosition = new Point(-AutoScrollPosition.X, newY);
            return true; // consuma mesajul - nu mai ajunge la controlul cu focus
        }
    }

    // ══════════════════════════════════════════════════════════
    //  COMBO BOX FARA SCROLL (previne scroll accidental)
    // ══════════════════════════════════════════════════════════
    public class NoScrollComboBox : ComboBox
    {
        private const int WM_MOUSEWHEEL = 0x020A;

        protected override void WndProc(ref Message m)
        {
            // Ignora scroll-ul mouse-ului cand dropdown-ul e inchis
            if (m.Msg == WM_MOUSEWHEEL && !DroppedDown)
                return;
            base.WndProc(ref m);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  CHECK + TEXT (item_field type "check_text")
    //  Bifa; textul se poate completa doar cand e bifata.
    //  Valoare: textul daca e bifata, altfel "".
    // ══════════════════════════════════════════════════════════
    public class CheckTextBox : Panel
    {
        private readonly CheckBox _chk;
        private readonly TextBox _txt;

        public CheckTextBox(string checkLabel)
        {
            BackColor = Color.Transparent;
            string lbl = string.IsNullOrEmpty(checkLabel) ? "Da" : checkLabel;
            _chk = new CheckBox
            {
                Text = lbl,
                Dock = DockStyle.Left,
                Width = TextRenderer.MeasureText(lbl, new Font("Segoe UI", 9f)).Width + 28,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _txt = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };
            _chk.CheckedChanged += (s, e) =>
            {
                _txt.Enabled = _chk.Checked;
                if (_chk.Checked) _txt.Focus();
            };
            // Fill primul, apoi Left (ultimul adaugat se aseaza primul la margine)
            Controls.Add(_txt);
            Controls.Add(_chk);
        }

        public string Value
        {
            get { return _chk.Checked ? _txt.Text.Trim() : string.Empty; }
        }

        // Valoare nevida -> bifat + text; valoare goala -> nebifat
        public void SetValue(string value)
        {
            bool on = !string.IsNullOrEmpty(value);
            _txt.Text = on ? value : string.Empty;
            _chk.Checked = on;
            _txt.Enabled = on;
        }

        // Copiaza starea (bifa + text) dintr-un alt control (editare in dialog)
        public void CopyFrom(CheckTextBox other)
        {
            if (other == null) return;
            _txt.Text = other._txt.Text;
            _chk.Checked = other._chk.Checked;
            _txt.Enabled = _chk.Checked;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  INTERVAL DE DATE (item_field type "date_range")
    //  Doua date; valoare: "dd.MM.yyyy - dd.MM.yyyy"
    // ══════════════════════════════════════════════════════════
    public class DateRangeBox : Panel
    {
        private readonly DateTimePicker _a;
        private readonly DateTimePicker _b;
        private readonly Label _sep;

        public DateRangeBox()
        {
            BackColor = Color.Transparent;
            _a = MakePicker();
            _b = MakePicker();
            _sep = new Label
            {
                Text = "–",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            // Daca data de inceput depaseste sfarsitul, il aliniem
            _a.ValueChanged += (s, e) => { if (_b.Value.Date < _a.Value.Date) _b.Value = _a.Value; };
            Controls.Add(_a);
            Controls.Add(_sep);
            Controls.Add(_b);
        }

        private static DateTimePicker MakePicker()
        {
            return new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy",
                Value = DateTime.Today,
                Font = new Font("Segoe UI", 10f)
            };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int gap = 22;
            int w = Math.Max((Width - gap) / 2, 80);
            _a.SetBounds(0, 0, w, Height);
            _sep.SetBounds(w, 0, gap, Height);
            _b.SetBounds(w + gap, 0, w, Height);
        }

        public string Value
        {
            get
            {
                return _a.Value.Date.ToString("dd.MM.yyyy") + " - " + _b.Value.Date.ToString("dd.MM.yyyy");
            }
        }

        // Copiaza intervalul dintr-un alt control (editare in dialog)
        public void CopyFrom(DateRangeBox other)
        {
            if (other == null) return;
            _a.Value = other._a.Value;
            _b.Value = other._b.Value;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  DYNAMIC LIST ROW
    //  Un rand din lista dinamica, construit din item_fields[]
    // ══════════════════════════════════════════════════════════
    public class DynamicListRow : Panel
    {
        private static readonly Color AccentViolet = Color.FromArgb(120, 60, 170);

        private Label _lblNumar;
        private int _numar;
        private readonly Dictionary<string, Control> _fields = new Dictionary<string, Control>();
        // Valori mapate din person_picker care nu au un item_field/control propriu
        // (ex. "NumeMembruSemnatura" folosit doar de un hook ConcatList, fara camp vizibil in UI)
        private readonly Dictionary<string, string> _extraValues = new Dictionary<string, string>();
        private readonly FieldDefinition _fieldDef;
        private readonly DocumentTheme _theme;
        private readonly List<PersonInfo> _persoane;

        public int Numar
        {
            get { return _numar; }
            set
            {
                _numar = value;
                if (_lblNumar != null) _lblNumar.Text = value + ".";
                if (_pnlHeader != null) _pnlHeader.Invalidate();
            }
        }

        public Action OnDelete { get; set; }

        // Apelat cand se schimba inaltimea randului (restrangere / extindere)
        public Action OnLayoutChanged { get; set; }

        // ── Stare rand restrangibil (liste cu "collapsible") ──
        private const int HdrH = 32;
        private Panel _pnlHeader;
        private Panel _pnlRowsRef;
        private bool _expanded = true;
        private int _bodyH;

        public void SetExpanded(bool expanded, bool notify)
        {
            if (_pnlHeader == null) return;
            _expanded = expanded;
            ApplyExpanded();
            if (notify && OnLayoutChanged != null) OnLayoutChanged();
        }

        private void ApplyExpanded()
        {
            _pnlRowsRef.Visible = _expanded;
            Height = _expanded ? HdrH + 6 + _bodyH : HdrH + 2;
            _pnlHeader.Invalidate();
        }

        private string BuildSummary()
        {
            if (_fieldDef.SummaryFields == null || _fieldDef.SummaryFields.Count == 0)
                return string.Empty;
            var vals = GetValues();
            var parts = new List<string>();
            foreach (var k in _fieldDef.SummaryFields)
            {
                string v;
                if (vals.TryGetValue(k, out v) && !string.IsNullOrWhiteSpace(v)) parts.Add(v);
            }
            return string.Join("  ·  ", parts);
        }

        private void BuildCollapsibleChrome(Button btnDelete, Panel pnlRows, int totalH)
        {
            _bodyH = totalH + 14;
            _pnlRowsRef = pnlRows;

            _pnlHeader = new Panel
            {
                Left = 4,
                Top = 0,
                Height = HdrH,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(240, 233, 250)
            };
            _pnlHeader.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var brAccent = new SolidBrush(_theme.AccentDark))
                using (var fChev = new Font("Segoe UI", 8f))
                using (var fNum = new Font("Segoe UI Semibold", 10f))
                {
                    g.DrawString(_expanded ? "▼" : "▶", fChev, brAccent, 10, 10);
                    g.DrawString(_numar + ".", fNum, brAccent, 28, 6);
                }
                using (var fSum = new Font("Segoe UI", 9.5f))
                using (var brSum = new SolidBrush(Color.FromArgb(55, 75, 105)))
                using (var sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                })
                {
                    g.DrawString(BuildSummary(), fSum, brSum,
                        new RectangleF(58, 8, Math.Max(_pnlHeader.Width - 58 - 44, 40), 20), sf);
                }
            };
            _pnlHeader.Click += (s, e) => SetExpanded(!_expanded, true);

            btnDelete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDelete.Top = 2;
            _pnlHeader.Controls.Add(btnDelete);

            pnlRows.Top = HdrH + 6;
            pnlRows.Left = 14;
            pnlRows.Height = totalH;

            Controls.Add(pnlRows);
            Controls.Add(_pnlHeader);

            Resize += (s, e) =>
            {
                _pnlHeader.Width = Math.Max(Width - 4, 100);
                btnDelete.Left = _pnlHeader.Width - btnDelete.Width - 6;
                pnlRows.Left = 14;
                pnlRows.Width = Math.Max(Width - 14 - 12, 100);
            };

            // Antetul (sumarul) se actualizeaza live cand se editeaza campurile
            foreach (var c in _fields.Values)
            {
                if (c is TextBox) ((TextBox)c).TextChanged += (s, e) => _pnlHeader.Invalidate();
                else if (c is ComboBox) ((ComboBox)c).SelectedIndexChanged += (s, e) => _pnlHeader.Invalidate();
                else if (c is DateTimePicker) ((DateTimePicker)c).ValueChanged += (s, e) => _pnlHeader.Invalidate();
                else if (c is NumericUpDown) ((NumericUpDown)c).ValueChanged += (s, e) => _pnlHeader.Invalidate();
            }

            ApplyExpanded();
        }

        // editorMode = true: randul e folosit ca formular in ListItemDialog
        // (fara chenar, numar si buton de stergere; spatiere mai larga).
        private readonly bool _editorMode;

        public FieldDefinition FieldDef { get { return _fieldDef; } }

        public DynamicListRow(FieldDefinition fieldDef, int numar,
            DocumentTheme theme, List<PersonInfo> persoane, bool editorMode = false)
        {
            _fieldDef = fieldDef;
            _numar = numar;
            _theme = theme;
            _persoane = persoane;
            _editorMode = editorMode;

            Height = 72;
            BackColor = Color.White;
            Padding = editorMode ? new Padding(0) : new Padding(8, 6, 8, 6);

            Paint += (s, e) =>
            {
                if (_editorMode) return;
                using (var pen = new Pen(Color.FromArgb(195, 165, 225)))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                e.Graphics.FillRectangle(new SolidBrush(theme.Accent), 0, 0, 4, Height);
            };

            BuildLayout();
        }

        private void BuildLayout()
        {
            _lblNumar = new Label
            {
                Text = _numar + ".",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = _theme.Accent,
                AutoSize = true,
                Location = new Point(12, 8)
            };

            var btnDelete = new Button
            {
                Text = "✕",
                Width = 28,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 50, 40),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Top = 8
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) => { if (OnDelete != null) OnDelete(); };

            // Grupeaza campurile in randuri (sum width_percent pana la 100)
            var rows = GroupRows(_fieldDef.ItemFields);

            int rowH = _editorMode ? 58 : 50;
            int capH = _editorMode ? 28 : 22;   // inaltimea titlului de grup
            int capCount = rows.Count(r => !string.IsNullOrEmpty(r[0].Group));
            int totalH = rows.Count * rowH + capCount * capH;

            // Panel container pentru toate randurile
            var pnlRows = new Panel
            {
                Dock = DockStyle.None,
                BackColor = Color.Transparent,
                Height = totalH
            };

            int rowTop = 0;
            foreach (var rowFields in rows)
            {
                // Titlu de grup (mic, cu linie separatoare) deasupra primului camp din grup
                string groupCaption = rowFields[0].Group;
                if (!string.IsNullOrEmpty(groupCaption))
                {
                    var lblGroup = new Label
                    {
                        Text = groupCaption.ToUpper(),
                        Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                        ForeColor = _theme.AccentDark,
                        AutoSize = false,
                        Left = 0,
                        Top = rowTop + 2,
                        Height = 16,
                        TextAlign = ContentAlignment.MiddleLeft,
                        BackColor = Color.Transparent
                    };
                    var lineGroup = new Panel
                    {
                        Left = 0,
                        Top = rowTop + capH - 3,
                        Height = 1,
                        BackColor = Color.FromArgb(220, 205, 240)
                    };
                    pnlRows.Controls.Add(lblGroup);
                    pnlRows.Controls.Add(lineGroup);
                    pnlRows.Resize += (sg, eg) => { lblGroup.Width = pnlRows.Width; lineGroup.Width = pnlRows.Width; };
                    rowTop += capH;
                }

                var tbl = new TableLayoutPanel
                {
                    RowCount = 1,
                    ColumnCount = rowFields.Count,
                    Left = 0,
                    Top = rowTop,
                    Height = rowH,
                    BackColor = Color.Transparent,
                    Padding = new Padding(0),
                    Margin = new Padding(0),
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                };

                int totalPct = rowFields.Sum(f => f.WidthPercent > 0 ? f.WidthPercent : 0);
                int rem = rowFields.Count(f => f.WidthPercent == 0);
                int remPct = rem > 0 ? (100 - totalPct) / rem : 0;

                foreach (var itemField in rowFields)
                {
                    int pct = itemField.WidthPercent > 0 ? itemField.WidthPercent : remPct;
                    tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, pct));

                    Control ctrl;
                    if (itemField.Type == "person_picker")
                        ctrl = BuildPersonPickerCell(itemField);
                    else if (itemField.Type == "combo")
                    {
                        // NoScrollComboBox: stil standard (cu chenar) si fara schimbare
                        // accidentala a valorii la scroll cu rotita
                        var cmbCell = new NoScrollComboBox
                        {
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            Font = new Font("Segoe UI", 10f),
                            Dock = DockStyle.Top,
                            BackColor = Color.White
                        };
                        if (itemField.Options != null)
                            foreach (var opt in itemField.Options) cmbCell.Items.Add(opt);
                        int defIdx = string.IsNullOrEmpty(itemField.Default)
                            ? 0 : cmbCell.Items.IndexOf(itemField.Default);
                        if (cmbCell.Items.Count > 0)
                            cmbCell.SelectedIndex = defIdx >= 0 ? defIdx : 0;
                        // on_select: la schimbarea optiunii se completeaza alte campuri
                        var itfSel = itemField;
                        cmbCell.SelectedIndexChanged += (s2, e2) => ApplyOnSelect(itfSel);
                        ctrl = cmbCell;
                    }
                    else if (itemField.Type == "date")
                    {
                        ctrl = new DateTimePicker
                        {
                            Format = DateTimePickerFormat.Custom,
                            CustomFormat = "dd.MM.yyyy",
                            Value = DateTime.Today,
                            Font = new Font("Segoe UI", 10f),
                            Dock = DockStyle.Top
                        };
                    }
                    else if (itemField.Type == "date_range")
                        ctrl = new DateRangeBox();
                    else if (itemField.Type == "check_text")
                        ctrl = new CheckTextBox(itemField.CheckLabel);
                    else if (itemField.Type == "number")
                    {
                        var nud = new NumericUpDown
                        {
                            Minimum = 0,
                            Maximum = itemField.Max > 0 ? itemField.Max : 9999,
                            DecimalPlaces = itemField.Decimals > 0 ? itemField.Decimals : 0,
                            Font = new Font("Segoe UI", 10f),
                            Dock = DockStyle.Top,
                            Height = 26
                        };
                        ctrl = nud;
                    }
                    else
                    {
                        var tb = new TextBox
                        {
                            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                            Dock = DockStyle.Top,
                            Height = 26,
                            BorderStyle = BorderStyle.FixedSingle
                        };
                        if (string.IsNullOrEmpty(itemField.Placeholder) && !string.IsNullOrEmpty(itemField.Default))
                            tb.Text = itemField.Default;
                        if (!string.IsNullOrEmpty(itemField.Placeholder))
                        {
                            tb.Text = itemField.Placeholder;
                            tb.ForeColor = Color.Gray;
                            tb.Font = new Font("Segoe UI", 10f);
                            tb.GotFocus += (s2, e2) =>
                            {
                                if (tb.ForeColor == Color.Gray)
                                {
                                    tb.Text = string.Empty;
                                    tb.ForeColor = Color.Black;
                                    tb.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                                }
                            };
                            tb.LostFocus += (s2, e2) =>
                            {
                                if (string.IsNullOrWhiteSpace(tb.Text))
                                {
                                    tb.Text = itemField.Placeholder;
                                    tb.ForeColor = Color.Gray;
                                    tb.Font = new Font("Segoe UI", 10f);
                                }
                            };
                        }
                        ctrl = tb;
                    }

                    _fields[itemField.Key] = ctrl;


                    var lbl = new Label
                    {
                        Text = itemField.Label,
                        Font = new Font("Segoe UI", 9f),
                        ForeColor = Color.FromArgb(55, 75, 105),
                        AutoSize = false,
                        Height = 16,
                        Dock = DockStyle.Top
                    };
                    ctrl.Dock = DockStyle.Top;
                    ctrl.Height = 26;

                    // Dock=Top: primul adaugat e cel mai jos, deci ctrl primul, lbl al doilea
                    var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent,
                        Padding = _editorMode ? new Padding(0, 4, 14, 4) : new Padding(0, 4, 6, 4) };
                    cell.Controls.Add(ctrl);
                    cell.Controls.Add(lbl);
                    tbl.Controls.Add(cell, rowFields.IndexOf(itemField), 0);
                }

                pnlRows.Controls.Add(tbl);
                pnlRows.Resize += (s, e) => tbl.Width = pnlRows.Width;
                rowTop += rowH;
            }

            Height = totalH + 16;

            if (_editorMode)
            {
                // Formular in ListItemDialog: doar campurile, pe toata latimea
                Height = totalH;
                pnlRows.Left = 0;
                pnlRows.Top = 0;
                Controls.Add(pnlRows);
                Resize += (s, e) =>
                {
                    pnlRows.Width = Width;
                    pnlRows.Height = totalH;
                };
                return;
            }

            if (_fieldDef.Collapsible)
            {
                BuildCollapsibleChrome(btnDelete, pnlRows, totalH);
                return;
            }

            Controls.Add(_lblNumar);
            Controls.Add(btnDelete);
            Controls.Add(pnlRows);

            Resize += (s, e) =>
            {
                btnDelete.Left = Width - btnDelete.Width - 8;
                pnlRows.Left = 32;
                pnlRows.Top = 6;
                pnlRows.Width = Width - 32 - btnDelete.Width - 14;
                pnlRows.Height = totalH;
            };
        }

        private Control BuildPersonPickerCell(ItemFieldDefinition itemField)
        {
            var btn = new Button
            {
                Text = "Selectează...",
                Dock = DockStyle.Top,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 240, 255),
                ForeColor = _theme.AccentDark,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderColor = _theme.AccentBorder;
            btn.FlatAppearance.BorderSize = 1;

            btn.Click += (s, e) =>
            {
                using (var dlg = new PersonPickerDialog(_persoane,
                    "Selectare " + itemField.Label,
                    "Selectează persoana"))
                {
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    var person = dlg.SelectedPerson;

                    // Autocomplete campuri din maps{}
                    if (itemField.Maps != null)
                    {
                        foreach (var map in itemField.Maps)
                        {
                            string val = GetPersonProperty(person, map.Value);
                            if (_fields.ContainsKey(map.Key))
                            {
                                var targetCtrl = _fields[map.Key];
                                if (targetCtrl is TextBox tb2)
                                {
                                    tb2.Text = val;
                                    tb2.ForeColor = Color.FromArgb(25, 35, 55);
                                    tb2.BackColor = Color.FromArgb(235, 240, 255);
                                }
                                else if (targetCtrl is Button btnTarget)
                                    btnTarget.Text = val;
                            }
                            else
                            {
                                // Nu exista control/item_field pentru aceasta cheie
                                // (ex. NumeMembruSemnatura folosit doar de un hook ConcatList) —
                                // salvam valoarea oricum, ca sa fie disponibila in GetValues().
                                _extraValues[map.Key] = val;
                            }
                        }
                    }

                    // Butonul arata persoana selectata
                    btn.Text = "✓ " + person.NumeComplet;
                    btn.BackColor = Color.FromArgb(220, 235, 255);
                    btn.ForeColor = Color.FromArgb(25, 50, 120);
                    _fields[itemField.Key] = btn;
                }
            };

            return btn;
        }

        // Grupeaza item_fields in randuri vizuale: suma width_percent pana la 100;
        // un camp cu "group" incepe mereu un rand nou. Folosit si de ListItemCard.
        public static List<List<ItemFieldDefinition>> GroupRows(IEnumerable<ItemFieldDefinition> fields)
        {
            var rows = new List<List<ItemFieldDefinition>>();
            var currentRow = new List<ItemFieldDefinition>();
            int rowSum = 0;
            foreach (var f in fields)
            {
                int pct = f.WidthPercent > 0 ? f.WidthPercent : 50;
                if ((rowSum + pct > 100 || !string.IsNullOrEmpty(f.Group)) && currentRow.Count > 0)
                {
                    rows.Add(currentRow);
                    currentRow = new List<ItemFieldDefinition>();
                    rowSum = 0;
                }
                currentRow.Add(f);
                rowSum += pct;
                if (rowSum >= 100) { rows.Add(currentRow); currentRow = new List<ItemFieldDefinition>(); rowSum = 0; }
            }
            if (currentRow.Count > 0) rows.Add(currentRow);
            return rows;
        }

        // Copiaza valorile din alt rand al aceleiasi liste (aceleasi item_fields).
        // Folosit la "Editeaza": se lucreaza pe o copie, iar originalul ramane
        // neatins daca utilizatorul apasa "Anuleaza".
        // ── Precompletare / reguli on_select ──────────────────
        // Inlocuieste token-urile {Functie}, {DataCim} etc. (setat de DynamicForm)
        public Func<string, string> TokenResolver { get; set; }

        // true cat timp setam valori programatic (fara sa declansam on_select)
        private bool _suppressOnSelect;

        private string Resolve(string value)
        {
            return TokenResolver != null ? TokenResolver(value) : value;
        }

        // Seteaza mai multe campuri odata (initial_items). Nu declanseaza on_select.
        public void SetFieldValues(Dictionary<string, string> values)
        {
            if (values == null) return;
            bool prev = _suppressOnSelect;
            _suppressOnSelect = true;
            try
            {
                foreach (var kv in values)
                    SetFieldValue(kv.Key, Resolve(kv.Value));
            }
            finally { _suppressOnSelect = prev; }
        }

        // Seteaza valoarea unui camp dupa tipul controlului.
        // Valorile care nu se potrivesc (optiune inexistenta, data invalida) sunt ignorate.
        public void SetFieldValue(string key, string value)
        {
            if (value == null) return;
            Control c;
            if (!_fields.TryGetValue(key, out c))
            {
                _extraValues[key] = value;
                return;
            }
            var ro = new System.Globalization.CultureInfo("ro-RO");

            if (c is ComboBox)
            {
                var cmb = (ComboBox)c;
                int idx = cmb.Items.IndexOf(value);
                if (idx >= 0) cmb.SelectedIndex = idx;
            }
            else if (c is DateTimePicker)
            {
                DateTime d;
                if (DateTime.TryParseExact(value, "dd.MM.yyyy", ro,
                        System.Globalization.DateTimeStyles.None, out d))
                    ((DateTimePicker)c).Value = d;
            }
            else if (c is NumericUpDown)
            {
                var nud = (NumericUpDown)c;
                decimal m;
                if (decimal.TryParse(value, System.Globalization.NumberStyles.Number, ro, out m))
                    nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, m));
            }
            else if (c is CheckTextBox)
                ((CheckTextBox)c).SetValue(value);
            else if (c is TextBox)
            {
                if (value.Length == 0) return;   // pastram placeholder-ul gri
                var tb = (TextBox)c;
                tb.Text = value;
                tb.ForeColor = Color.FromArgb(25, 35, 55);
                tb.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            }
        }

        private bool IsFieldEmpty(string key)
        {
            Control c;
            if (!_fields.TryGetValue(key, out c)) return true;
            if (c is TextBox) { var tb = (TextBox)c; return tb.ForeColor == Color.Gray || string.IsNullOrWhiteSpace(tb.Text); }
            if (c is NumericUpDown) return ((NumericUpDown)c).Value == 0;
            if (c is CheckTextBox) return string.IsNullOrEmpty(((CheckTextBox)c).Value);
            return false;
        }

        // Aplica regula on_select pentru optiunea curenta a unui combo.
        // Combo-uri si date: se seteaza mereu; text / numar / check_text: doar daca sunt goale.
        private void ApplyOnSelect(ItemFieldDefinition itemField)
        {
            if (_suppressOnSelect || itemField.OnSelect == null) return;
            Control c;
            if (!_fields.TryGetValue(itemField.Key, out c)) return;
            var cmb = c as ComboBox;
            string sel = cmb != null && cmb.SelectedItem != null ? cmb.SelectedItem.ToString() : null;
            Dictionary<string, string> sets;
            if (sel == null || !itemField.OnSelect.TryGetValue(sel, out sets) || sets == null) return;

            _suppressOnSelect = true;
            try
            {
                foreach (var kv in sets)
                {
                    Control t;
                    if (!_fields.TryGetValue(kv.Key, out t)) continue;
                    bool doarDacaGol = t is TextBox || t is NumericUpDown || t is CheckTextBox;
                    if (doarDacaGol && !IsFieldEmpty(kv.Key)) continue;
                    SetFieldValue(kv.Key, Resolve(kv.Value));
                }
            }
            finally { _suppressOnSelect = false; }
        }

        // Aplica on_select pentru toate combo-urile (element nou, optiuni implicite)
        public void ApplyOnSelectAll()
        {
            foreach (var f in _fieldDef.ItemFields)
                if (f.Type == "combo" && f.OnSelect != null)
                    ApplyOnSelect(f);
        }

        public void CopyStateFrom(DynamicListRow other)
        {
            if (other == null) return;
            bool prevSuppress = _suppressOnSelect;
            _suppressOnSelect = true;
            try { CopyStateFromCore(other); }
            finally { _suppressOnSelect = prevSuppress; }
        }

        private void CopyStateFromCore(DynamicListRow other)
        {
            foreach (var kv in _fields)
            {
                Control src;
                if (!other._fields.TryGetValue(kv.Key, out src)) continue;
                var dst = kv.Value;

                if (dst is TextBox && src is TextBox)
                {
                    var t = (TextBox)dst; var s = (TextBox)src;
                    t.Text = s.Text; t.ForeColor = s.ForeColor; t.Font = s.Font; t.BackColor = s.BackColor;
                }
                else if (dst is NumericUpDown && src is NumericUpDown)
                    ((NumericUpDown)dst).Value = ((NumericUpDown)src).Value;
                else if (dst is ComboBox && src is ComboBox)
                    ((ComboBox)dst).SelectedIndex = ((ComboBox)src).SelectedIndex;
                else if (dst is DateTimePicker && src is DateTimePicker)
                    ((DateTimePicker)dst).Value = ((DateTimePicker)src).Value;
                else if (dst is DateRangeBox && src is DateRangeBox)
                    ((DateRangeBox)dst).CopyFrom((DateRangeBox)src);
                else if (dst is CheckTextBox && src is CheckTextBox)
                    ((CheckTextBox)dst).CopyFrom((CheckTextBox)src);
                else if (dst is Button && src is Button)
                {
                    var b = (Button)dst; var s = (Button)src;
                    b.Text = s.Text; b.BackColor = s.BackColor; b.ForeColor = s.ForeColor;
                }
            }
            foreach (var kv in other._extraValues)
                _extraValues[kv.Key] = kv.Value;
        }

        public bool IsValid()
        {
            // Randul e valid daca cel putin primul camp are valoare
            if (_fields.Count == 0) return false;
            var first = _fields.Values.First();
            if (first is TextBox tb)
                return !string.IsNullOrWhiteSpace(tb.Text) && tb.ForeColor != Color.Gray;
            if (first is Button btn)
                return btn.Text != "Selectează..." && !string.IsNullOrWhiteSpace(btn.Text);
            return true;
        }

        public Dictionary<string, string> GetValues()
        {
            var result = new Dictionary<string, string>();
            foreach (var kv in _fields)
            {
                if (kv.Value is TextBox tb)
                    result[kv.Key] = tb.ForeColor == Color.Gray ? string.Empty : tb.Text.Trim();
                else if (kv.Value is NumericUpDown nud)
                {
                    var nf = _fieldDef.ItemFields.FirstOrDefault(f => f.Key == kv.Key);
                    result[kv.Key] = (nf != null && !string.IsNullOrEmpty(nf.Format))
                        ? nud.Value.ToString(nf.Format, new System.Globalization.CultureInfo("ro-RO"))
                        : nud.Value.ToString();
                }
                else if (kv.Value is ComboBox cmbVal)
                    result[kv.Key] = cmbVal.SelectedItem != null ? cmbVal.SelectedItem.ToString() : string.Empty;
                else if (kv.Value is DateTimePicker dtpVal)
                    result[kv.Key] = dtpVal.Value.Date.ToString("dd.MM.yyyy");
                else if (kv.Value is DateRangeBox drbVal)
                    result[kv.Key] = drbVal.Value;
                else if (kv.Value is CheckTextBox ctbVal)
                    result[kv.Key] = ctbVal.Value;
                else if (kv.Value is Button btn)
                {
                    string txt = btn.Text == "Selectează..." ? string.Empty : btn.Text;
                    // Elimina prefixul "✓ " adaugat la selectia persoanei
                    if (txt.StartsWith("✓ ")) txt = txt.Substring(2);
                    result[kv.Key] = txt;
                }
                else
                    result[kv.Key] = string.Empty;
            }

            // Adauga valorile mapate care nu au un control propriu (ex. NumeMembruSemnatura)
            foreach (var kv in _extraValues)
            {
                if (!result.ContainsKey(kv.Key))
                    result[kv.Key] = kv.Value;
            }

            // Campuri "date" cu maps{}: impart data in bucati (an / luna / zi etc.),
            // fiecare disponibila ca placeholder separat in randul clonat.
            //   "maps": { "AnulMutatie": "yyyy", "LunaMutatie": "MM", "ZiMutatie": "dd" }
            foreach (var itf in _fieldDef.ItemFields)
            {
                if (itf.Type != "date" || itf.Maps == null || itf.Maps.Count == 0) continue;
                Control dctrl;
                var dtpSplit = _fields.TryGetValue(itf.Key, out dctrl) ? dctrl as DateTimePicker : null;
                if (dtpSplit == null) continue;
                var ro = new System.Globalization.CultureInfo("ro-RO");
                foreach (var map in itf.Maps)
                    result[map.Key] = dtpSplit.Value.Date.ToString(map.Value, ro);
            }

            // Numarul randului — pentru {{NrCrt}} in tabele
            if (!result.ContainsKey("NrCrt"))
                result["NrCrt"] = _numar.ToString();

            return result;
        }

        private static string GetPersonProperty(PersonInfo p, string propName)
        {
            switch (propName)
            {
                case "NumeComplet": return p.NumeComplet;
                case "Nume": return p.Nume;
                case "Prenume": return p.Prenume;
                case "CNP": return p.CNP;
                case "Functie": return p.Functie;
                case "CodCor": return p.CodCor;
                case "NrCim": return p.NrCim;
                case "NumeDepartament": return p.NumeDepartament;
                case "DataCim": return p.DataCimFormatata;
                case "SerieCI": return p.SerieCI;
                case "NrCI": return p.NrCI;
                case "Domiciliu": return p.Domiciliu;
                case "NumeComplet_Functie":
                    return string.IsNullOrEmpty(p.Functie)
                        ? p.NumeComplet
                        : p.NumeComplet + " — " + p.Functie;
                default: return string.Empty;
            }
        }
    }
}