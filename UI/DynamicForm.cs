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
                PrePopulateAngajatCIFields();

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

            // Sectiunile din JSON
            foreach (var section in _def.Sections)
            {
                BuildSection(section, ref y);
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

            // Seteaza inaltimea minima de scroll astfel incat tot continutul sa fie accesibil
            int totalH = y + 20;
            _pnlBody.AutoScrollMinSize = new System.Drawing.Size(0, totalH);
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

                    // Autocomplete campurile din maps{}
                    if (field.Maps != null)
                    {
                        foreach (var map in field.Maps)
                        {
                            string targetKey = map.Key;   // key camp din formular
                            string personProp = map.Value; // proprietate din PersonInfo

                            string val = GetPersonProperty(person, personProp);

                            // Actualizeaza controlul vizual daca exista
                            if (_controls.ContainsKey(targetKey))
                                SetControlValue(_controls[targetKey], val);

                            // Salveaza in formValues
                            _formValues[targetKey] = val;
                        }
                    }

                    // Salveaza PrsnId invizibil
                    _formValues[field.Key + "_PrsnId"] = person.PrsnId;
                    _formValues[field.Key] = person.NumeComplet;
                }
            };

            pnl.Controls.Add(btn);
            return pnl;
        }

        // ── Lista dinamica ─────────────────────────────────────
        private void BuildDynamicListField(FieldDefinition field, ref int y, int sectionHeight = 0)
        {
            var rows = new List<DynamicListRow>();
            Panel panel = null;
            int panelH = sectionHeight > 0 ? sectionHeight - 42 : 200; // 42 = header + btn row
            panel = AddDynamicListSection(field.Label,
                "Adaugă " + field.Label.ToLower(),
                () => AddDynamicListRow(field, rows, panel),
                ref y, panelH);

            _dynamicLists[field.Key] = rows;
            _listPanels[field.Key] = panel;

            // Adauga randurile initiale
            int initRows = field.InitialRows > 0 ? field.InitialRows : 1;
            for (int i = 0; i < initRows; i++)
                AddDynamicListRow(field, rows, panel);
        }

        private void AddDynamicListRow(FieldDefinition field,
            List<DynamicListRow> rows, Panel panel)
        {
            var row = new DynamicListRow(field, rows.Count + 1, _theme, _persoane);
            row.Width = Math.Max(panel.Width - 2, 400);

            row.OnDelete = () =>
            {
                // Salveaza pozitia scroll a sidebar-ului principal
                var bodyScroll = _pnlBody.AutoScrollPosition;

                rows.Remove(row);
                panel.Controls.Remove(row);
                for (int i = 0; i < rows.Count; i++) rows[i].Numar = i + 1;
                RelayoutPanel(panel, rows.Cast<Control>().ToList());

                // Restaureaza pozitia scroll sidebar dupa relayout
                _pnlBody.AutoScrollPosition = new System.Drawing.Point(
                    -bodyScroll.X, -bodyScroll.Y);
            };

            rows.Add(row);
            panel.Controls.Add(row);
            RelayoutPanel(panel, rows.Cast<Control>().ToList());
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

                string val = GetControlValue(_controls[field.Key], field.Type)?.ToString() ?? string.Empty;
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
            string codReg = _common.CodInregistrare;

            using (var dlg = new ConfirmareDialog(_def.Title, codReg, dataReg, _theme))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
            }

            try
            {
                // Insert in registratura
                if (_def.Registratura && RegistraturaService.Instance != null)
                {
                    RegistraturaService.Instance.Inregistreaza(
                        codReg, dataReg,
                        _def.RegistraturaTipDocPk,
                        _def.Title,
                        _common.PrsnId);

                    // Insert in tabela specifica categoriei (Acte Aditionale / Decizii / PV)
                    // Transmitem titlul documentului prin formValues pentru tipDcz/pvType
                    formValues["_DocTitle"] = _def.Title;
                    RegistraturaService.Instance.InregistreazaTabelaSpecifica(
                        _def.Category,
                        codReg,
                        dataReg,
                        _common.PrsnId,
                        formValues);
                }

                string pdfPath = DynamicTemplateEngine.GeneratePdf(_def, formValues, _common);

                using (var dlg = new SuccessDialog(_def.Title, codReg, dataReg, _theme))
                    dlg.ShowDialog(this);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
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
                }
            }
            finally
            {
                _hooksRunning = false;
            }
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
        private void PrePopulateAngajatCIFields()
        {
            var mappings = new System.Collections.Generic.Dictionary<string, string>
            {
                { "CISeria",   _common.SerieCI   },
                { "SerieCI",   _common.SerieCI   },
                { "CINr",      _common.NrCI      },
                { "NrCI",      _common.NrCI      },
                { "Domiciliu", _common.Domiciliu },
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

        private Panel AddDynamicListSection(string titlu, string btnText,
            Action onAdd, ref int y, int itemsPanelHeight = 200)
        {
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
                Width = 160,
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
                BackColor = Color.FromArgb(248, 244, 254),
                AutoScroll = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlItems.Paint += (s, e) =>
            {
                using (var pen = new Pen(_theme.AccentBorder, 2f))
                    e.Graphics.DrawRectangle(pen, 1, 1, pnlItems.Width - 3, pnlItems.Height - 3);
                e.Graphics.FillRectangle(new SolidBrush(_theme.Accent), 0, 0, 4, pnlItems.Height);
            };
            _pnlBody.Controls.Add(pnlItems);
            _pnlBody.Resize += (s, e) =>
            {
                pnlItems.Width = _pnlBody.ClientSize.Width - _pnlBody.Padding.Horizontal;
                foreach (Control c in pnlItems.Controls)
                    c.Width = Math.Max(pnlItems.ClientSize.Width - 4, 380);
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
                return dtp.Value.Date.ToString("dd.MM.yyyy");
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
            set { _numar = value; if (_lblNumar != null) _lblNumar.Text = value + "."; }
        }

        public Action OnDelete { get; set; }

        public DynamicListRow(FieldDefinition fieldDef, int numar,
            DocumentTheme theme, List<PersonInfo> persoane)
        {
            _fieldDef = fieldDef;
            _numar = numar;
            _theme = theme;
            _persoane = persoane;

            Height = 72;
            BackColor = Color.White;
            Padding = new Padding(8, 6, 8, 6);

            Paint += (s, e) =>
            {
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
            var rows = new List<List<ItemFieldDefinition>>();
            var currentRow = new List<ItemFieldDefinition>();
            int rowSum = 0;
            foreach (var f in _fieldDef.ItemFields)
            {
                int pct = f.WidthPercent > 0 ? f.WidthPercent : 50;
                if (rowSum + pct > 100 && currentRow.Count > 0)
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

            int rowH = 50;
            int totalH = rows.Count * rowH;

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
                    else if (itemField.Type == "number")
                    {
                        var nud = new NumericUpDown
                        {
                            Minimum = 0,
                            Maximum = 9999,
                            DecimalPlaces = 0,
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
                    var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 4, 6, 4) };
                    cell.Controls.Add(ctrl);
                    cell.Controls.Add(lbl);
                    tbl.Controls.Add(cell, rowFields.IndexOf(itemField), 0);
                }

                pnlRows.Controls.Add(tbl);
                pnlRows.Resize += (s, e) => tbl.Width = pnlRows.Width;
                rowTop += rowH;
            }

            Height = totalH + 16;

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
                    result[kv.Key] = nud.Value.ToString();
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