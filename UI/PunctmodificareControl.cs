using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public class PunctModificareControl : Panel
    {
        // ── Tipuri disponibile ────────────────────────────────
        public enum TipModificare
        {
            Salariu = 0,
            Functie = 1,
            ProgramLucru = 2,
            LocMunca = 3,
            DurataContract = 4,
            Concediu = 5,
            ConcediuSuplimentar = 6,
            Sporuri = 7,
            Indemnizatii = 8,
            DataPlata = 9,
            PreavizConcediere = 10,
            PreavizDemisie = 11,
            FormareProfesionala = 12,
            DrepturiObligatii = 13,
            TextLiber = 14
        }

        private static readonly string[] TipuriDisplay = new[]
        {
            "Salariu de baza (Lit. I, pct. 1)",
            "Functie / COR (Lit. F)",
            "Program de lucru (Lit. G, pct. 1)",
            "Loc de munca (Lit. E, pct. 1)",
            "Durata contract (Lit. C)",
            "Concediu de baza (Lit. H)",
            "Concediu suplimentar (Lit. H)",
            "Sporuri (Lit. I, pct. 2, lit. a)",
            "Indemnizatii (Lit. I, pct. 2, lit. b)",
            "Data plata salariu (Lit. I, pct. 5)",
            "Preaviz concediere (Lit. J, lit. a)",
            "Preaviz demisie (Lit. J, lit. b)",
            "Formare profesionala (Lit. O)",
            "Drepturi si obligatii (Lit. R)",
            "Text liber"
        };

        // ── Culori (reutilizam din DocumentFormBase) ──────────
        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        private static readonly Color AlbastruDes = Color.FromArgb(235, 241, 251);
        private static readonly Color TextPrincipal = Color.FromArgb(25, 35, 55);
        private static readonly Color TextSecundar = Color.FromArgb(90, 110, 140);
        private static readonly Color Verde = Color.FromArgb(34, 130, 84);
        private static readonly Color VerdeDes = Color.FromArgb(235, 248, 240);

        // ── Controale ─────────────────────────────────────────
        private Label _lblNumar;
        private ComboBox _cmbTip;
        private Button _btnDelete;
        private Panel _pnlCampuri;

        // Preview
        private Panel _pnlPreview;
        private Label _lblPreviewRef;
        private Label _lblPreviewText;
        private Button _btnEdit;

        // Editare manuala
        private Panel _pnlEdit;
        private TextBox _txtEditRef;
        private TextBox _txtEditText;
        private Button _btnSaveEdit;
        private Button _btnCancelEdit;
        private bool _editMode = false;
        private bool _manuallyEdited = false;  // marcam daca userul a modificat manual

        // Campuri dinamice
        private TextBox _txtVal1;
        private TextBox _txtVal2;
        private TextBox _txtArea;
        private ComboBox _cmbVal1;
        private DateTimePicker _dtpVal;

        private int _numar;

        public int Numar
        {
            get { return _numar; }
            set { _numar = value; if (_lblNumar != null) _lblNumar.Text = value + "."; }
        }

        public Action OnDelete { get; set; }

        // ══════════════════════════════════════════════════════
        public PunctModificareControl(int numar)
        {
            _numar = numar;
            AutoSize = false;
            BackColor = Color.White;
            Padding = new Padding(10, 8, 10, 8);
            Margin = new Padding(0, 0, 0, 8);
            Height = 54;

            Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 220, 235), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                // Bara laterala colorata
                e.Graphics.FillRectangle(new SolidBrush(Albastru), 0, 0, 4, Height);
            };

            BuildLayout();
            Resize += (s, e) => RepositionAll();
        }

        private void BuildLayout()
        {
            // Numar
            _lblNumar = new Label
            {
                Text = _numar + ".",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Albastru,
                AutoSize = true,
                Location = new Point(14, 10)
            };

            // Dropdown tip
            _cmbTip = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Width = 320,
                Location = new Point(38, 7)
            };
            foreach (string tip in TipuriDisplay)
                _cmbTip.Items.Add(tip);
            _cmbTip.SelectedIndexChanged += CmbTip_Changed;

            // Buton stergere
            _btnDelete = new Button
            {
                Text = "✕",
                Width = 28,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 50, 40),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += (s, e) => { if (OnDelete != null) OnDelete(); };

            // Panoul campuri dinamice (structurate)
            _pnlCampuri = new Panel { Height = 0, BackColor = Color.White };

            // ── PANOU PREVIEW ─────────────────────────────────
            _pnlPreview = new Panel
            {
                Height = 0,
                BackColor = VerdeDes,
                Padding = new Padding(8, 6, 8, 6)
            };

            _lblPreviewRef = new Label
            {
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(50, 100, 60),
                AutoSize = false,
                Location = new Point(8, 6),
                Height = 16
            };

            _lblPreviewText = new Label
            {
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 80, 40),
                AutoSize = false,
                Location = new Point(8, 24)
            };

            _btnEdit = new Button
            {
                Text = "✎ Editează",
                Height = 24,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Albastru,
                Font = new Font("Segoe UI", 8f),
                Cursor = Cursors.Hand
            };
            _btnEdit.FlatAppearance.BorderSize = 1;
            _btnEdit.FlatAppearance.BorderColor = Albastru;
            _btnEdit.Click += BtnEdit_Click;

            _pnlPreview.Controls.AddRange(new Control[] { _lblPreviewRef, _lblPreviewText, _btnEdit });

            // ── PANOU EDITARE MANUALA ─────────────────────────
            _pnlEdit = new Panel
            {
                Height = 0,
                BackColor = AlbastruDes,
                Padding = new Padding(8, 6, 8, 6),
                Visible = false
            };

            // Folosim FlowLayoutPanel vertical — zero pozitionare manuala
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(8, 6, 8, 6),
                BackColor = Color.Transparent,
                AutoSize = false
            };

            // Referinta clauza
            var lblEditRef = new Label
            {
                Text = "Referință clauză:",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextSecundar,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };
            _txtEditRef = new TextBox
            {
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = TextPrincipal,
                Height = 24,
                Margin = new Padding(0, 0, 0, 8)
            };
            _txtEditRef.Anchor = AnchorStyles.Left | AnchorStyles.Right;

            // Text modificare
            var lblEditText = new Label
            {
                Text = "Text modificare:",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextSecundar,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };
            _txtEditText = new TextBox
            {
                Multiline = true,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = TextPrincipal,
                Height = 60,
                ScrollBars = ScrollBars.Vertical,
                Margin = new Padding(0, 0, 0, 8)
            };

            // Butoane
            var pnlBtns = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            _btnCancelEdit = new Button
            {
                Text = "Anulează",
                Height = 26,
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = TextSecundar,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            _btnCancelEdit.FlatAppearance.BorderSize = 0;
            _btnCancelEdit.Click += BtnCancelEdit_Click;

            _btnSaveEdit = new Button
            {
                Text = "✓ Salvează",
                Height = 26,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                BackColor = Verde,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            _btnSaveEdit.FlatAppearance.BorderSize = 0;
            _btnSaveEdit.Click += BtnSaveEdit_Click;

            pnlBtns.Controls.Add(_btnCancelEdit);
            pnlBtns.Controls.Add(_btnSaveEdit);

            flow.Controls.AddRange(new Control[]
            {
                lblEditRef, _txtEditRef, lblEditText, _txtEditText, pnlBtns
            });
            _pnlEdit.Controls.Add(flow);

            // Actualizam latimea campurilor la resize
            _pnlEdit.Resize += (s, e) =>
            {
                int w = _pnlEdit.ClientSize.Width - 16;
                _txtEditRef.Width = w;
                _txtEditText.Width = w;
                pnlBtns.Width = w;
            };

            Controls.AddRange(new Control[]
            {
                _lblNumar, _cmbTip, _btnDelete,
                _pnlCampuri, _pnlPreview, _pnlEdit
            });

            RepositionAll();
        }

        // ══════════════════════════════════════════════════════
        //  POZITIONARE
        // ══════════════════════════════════════════════════════
        private void RepositionAll()
        {
            if (_btnDelete != null)
                _btnDelete.Location = new Point(Width - _btnDelete.Width - 10, 7);

            int innerW = Width - 24;

            if (_pnlCampuri != null)
            {
                _pnlCampuri.Left = 12;
                _pnlCampuri.Top = 42;
                _pnlCampuri.Width = innerW;
            }

            int previewTop = 42 + (_pnlCampuri != null ? _pnlCampuri.Height : 0) + (_pnlCampuri?.Height > 0 ? 4 : 0);

            if (_pnlPreview != null)
            {
                _pnlPreview.Left = 12;
                _pnlPreview.Top = previewTop;
                _pnlPreview.Width = innerW;

                if (_lblPreviewRef != null) _lblPreviewRef.Width = innerW - 100 - 16;
                if (_lblPreviewText != null) _lblPreviewText.Width = innerW - 16;
                if (_btnEdit != null)
                {
                    _btnEdit.Top = 6;
                    _btnEdit.Left = innerW - _btnEdit.Width - 8;
                }
            }

            int editTop = previewTop + (_pnlPreview != null ? _pnlPreview.Height : 0) + (_pnlPreview?.Height > 0 ? 4 : 0);

            if (_pnlEdit != null)
            {
                _pnlEdit.Left = 12;
                _pnlEdit.Top = editTop;
                _pnlEdit.Width = innerW;

                if (_txtEditRef != null)
                {
                    _txtEditRef.Width = innerW - 16;
                    _txtEditText.Width = innerW - 16;
                    _btnSaveEdit.Location = new Point(innerW - 16 - 80 - 104, 130);
                    _btnCancelEdit.Location = new Point(innerW - 16 - 80, 130);
                }
            }

            RecalcHeight();
        }

        private void RecalcHeight()
        {
            int h = 42; // header
            if (_pnlCampuri != null && _pnlCampuri.Height > 0) h += _pnlCampuri.Height + 4;
            if (_pnlPreview != null && _pnlPreview.Height > 0) h += _pnlPreview.Height + 4;
            if (_pnlEdit != null && _pnlEdit.Height > 0) h += _pnlEdit.Height + 4;
            h += 8;
            Height = Math.Max(h, 50);
        }

        // ══════════════════════════════════════════════════════
        //  SCHIMBARE TIP
        // ══════════════════════════════════════════════════════
        private void CmbTip_Changed(object sender, EventArgs e)
        {
            _pnlCampuri.Controls.Clear();
            _txtVal1 = null; _txtVal2 = null; _txtArea = null;
            _cmbVal1 = null; _dtpVal = null;
            _pnlCampuri.Height = 0;
            _pnlPreview.Height = 0;
            _pnlEdit.Height = 0;
            _pnlEdit.Visible = false;
            _editMode = false;
            _manuallyEdited = false;

            if (_cmbTip.SelectedIndex < 0) { RecalcHeight(); return; }

            switch ((TipModificare)_cmbTip.SelectedIndex)
            {
                case TipModificare.Salariu: BuildSalariu(); break;
                case TipModificare.Functie: BuildFunctie(); break;
                case TipModificare.ProgramLucru: BuildTextArea("Descrieti programul de lucru..."); break;
                case TipModificare.LocMunca: BuildTextArea("ex. Activitatea se desfasoara la punctul de lucru: ..."); break;
                case TipModificare.DurataContract: BuildDurata(); break;
                case TipModificare.Concediu: BuildConcediu(); break;
                case TipModificare.ConcediuSuplimentar: BuildConcediuSup(); break;
                case TipModificare.Sporuri: BuildTextArea("Descrieti sporul..."); break;
                case TipModificare.Indemnizatii: BuildTextArea("Descrieti indemnizatia..."); break;
                case TipModificare.DataPlata: BuildDataPlata(); break;
                case TipModificare.PreavizConcediere: BuildPreaviz(); break;
                case TipModificare.PreavizDemisie: BuildPreaviz(); break;
                case TipModificare.FormareProfesionala: BuildTextArea("Descrieti conditiile de formare profesionala..."); break;
                case TipModificare.DrepturiObligatii: BuildTextArea("Descrieti modificarea drepturilor si obligatiilor..."); break;
                case TipModificare.TextLiber: BuildTextArea("Introduceti textul clauzei..."); break;
            }

            RepositionAll();
        }

        // ══════════════════════════════════════════════════════
        //  EDIT / SAVE / CANCEL
        // ══════════════════════════════════════════════════════
        private void BtnEdit_Click(object sender, EventArgs e)
        {
            // Precompletam campurile de editare cu valorile curente
            var punct = GetPunctFromFields();
            if (punct != null)
            {
                _txtEditRef.Text = punct.Referinta ?? string.Empty;
                _txtEditText.Text = punct.TextModificare ?? string.Empty;
            }

            _editMode = true;
            _pnlEdit.Height = 178;
            _pnlEdit.Visible = true;
            RepositionAll();
            _txtEditText.Focus();
        }

        private void BtnSaveEdit_Click(object sender, EventArgs e)
        {
            _manuallyEdited = true;
            _editMode = false;
            _pnlEdit.Visible = false;
            _pnlEdit.Height = 0;

            // Actualizam preview cu valorile editate manual
            UpdatePreview(_txtEditRef.Text.Trim(), _txtEditText.Text.Trim());
            RepositionAll();
        }

        private void BtnCancelEdit_Click(object sender, EventArgs e)
        {
            _editMode = false;
            _pnlEdit.Visible = false;
            _pnlEdit.Height = 0;
            _manuallyEdited = false;
            RepositionAll();
        }

        // ══════════════════════════════════════════════════════
        //  UPDATE PREVIEW
        // ══════════════════════════════════════════════════════
        private void UpdatePreview(string referinta, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _pnlPreview.Height = 0;
                RecalcHeight();
                return;
            }

            _lblPreviewRef.Text = string.IsNullOrWhiteSpace(referinta)
                ? "" : referinta + " se modifica si va avea urmatorul cuprins:";
            _lblPreviewText.Text = text;

            // Calculam inaltimea necesara
            int refH = string.IsNullOrWhiteSpace(referinta) ? 0 : 20;
            int textH = 0;
            using (var g = _lblPreviewText.CreateGraphics())
            {
                var sz = g.MeasureString(text, _lblPreviewText.Font,
                    Math.Max(_pnlPreview.Width - 20, 200));
                textH = (int)sz.Height + 4;
            }

            _lblPreviewRef.Top = 6;
            _lblPreviewRef.Height = refH;
            _lblPreviewText.Top = 6 + refH + (refH > 0 ? 2 : 0);
            _lblPreviewText.Height = textH;

            _pnlPreview.Height = 6 + refH + (refH > 0 ? 2 : 0) + textH + _btnEdit.Height + 10;
            RecalcHeight();
        }

        private void TriggerPreviewUpdate()
        {
            if (_manuallyEdited) return; // nu suprascrie editarea manuala
            var punct = GetPunctFromFields();
            if (punct != null)
                UpdatePreview(punct.Referinta, punct.TextModificare);
            else
            {
                _pnlPreview.Height = 0;
                RecalcHeight();
            }
        }

        // ══════════════════════════════════════════════════════
        //  GET PUNCT (apelat de form la generare)
        // ══════════════════════════════════════════════════════
        public PunctModificare GetPunct()
        {
            // Daca a fost editat manual, returnam valorile editate
            if (_manuallyEdited)
            {
                string ref_ = _txtEditRef.Text.Trim();
                string txt = _txtEditText.Text.Trim();
                if (string.IsNullOrWhiteSpace(txt)) return null;
                return new PunctModificare { Referinta = ref_, TextModificare = txt };
            }

            return GetPunctFromFields();
        }

        private PunctModificare GetPunctFromFields()
        {
            if (_cmbTip.SelectedIndex < 0) return null;

            try
            {
                var tip = (TipModificare)_cmbTip.SelectedIndex;

                // Tipuri cu textarea libera
                if (tip == TipModificare.ProgramLucru || tip == TipModificare.LocMunca ||
                    tip == TipModificare.Sporuri || tip == TipModificare.Indemnizatii ||
                    tip == TipModificare.FormareProfesionala || tip == TipModificare.DrepturiObligatii ||
                    tip == TipModificare.TextLiber)
                {
                    if (_txtArea == null || string.IsNullOrWhiteSpace(_txtArea.Text)
                        || _txtArea.ForeColor == Color.Gray) return null;

                    switch (tip)
                    {
                        case TipModificare.ProgramLucru: return ClauzeTextBuilder.BuildProgram(_txtArea.Text);
                        case TipModificare.LocMunca: return ClauzeTextBuilder.BuildLocMunca(_txtArea.Text);
                        case TipModificare.Sporuri: return ClauzeTextBuilder.BuildSporuri(_txtArea.Text);
                        case TipModificare.Indemnizatii: return ClauzeTextBuilder.BuildIndemnizatii(_txtArea.Text);
                        case TipModificare.FormareProfesionala: return ClauzeTextBuilder.BuildFormare(_txtArea.Text);
                        case TipModificare.DrepturiObligatii: return ClauzeTextBuilder.BuildDrepturi(_txtArea.Text);
                        case TipModificare.TextLiber: return ClauzeTextBuilder.BuildTextLiber(_txtArea.Text);
                    }
                }

                // Tipuri structurate
                switch (tip)
                {
                    case TipModificare.Salariu:
                        if (_txtVal1 == null || string.IsNullOrWhiteSpace(_txtVal1.Text)) return null;
                        decimal sal;
                        if (!decimal.TryParse(_txtVal1.Text, out sal)) return null;
                        return ClauzeTextBuilder.BuildSalariu(sal);

                    case TipModificare.Functie:
                        if (_txtVal1 == null || string.IsNullOrWhiteSpace(_txtVal1.Text)
                            || _txtVal1.ForeColor == Color.Gray) return null;
                        return ClauzeTextBuilder.BuildFunctie(
                            _txtVal1.Text,
                            _txtVal2 != null && _txtVal2.ForeColor != Color.Gray ? _txtVal2.Text : string.Empty);

                    case TipModificare.DurataContract:
                        if (_cmbVal1 == null) return null;
                        bool det = _cmbVal1.SelectedItem != null &&
                                   _cmbVal1.SelectedItem.ToString() == "determinata";
                        DateTime? dataExp = det && _dtpVal != null ? (DateTime?)_dtpVal.Value : null;
                        return ClauzeTextBuilder.BuildDurata(det, dataExp);

                    case TipModificare.Concediu:
                        if (_txtVal1 == null || string.IsNullOrWhiteSpace(_txtVal1.Text)) return null;
                        int zb; if (!int.TryParse(_txtVal1.Text, out zb)) return null;
                        return ClauzeTextBuilder.BuildConcediu(zb);

                    case TipModificare.ConcediuSuplimentar:
                        if (_txtVal1 == null || string.IsNullOrWhiteSpace(_txtVal1.Text)) return null;
                        int zs; if (!int.TryParse(_txtVal1.Text, out zs)) return null;
                        return ClauzeTextBuilder.BuildConcediuSuplimentar(zs);

                    case TipModificare.DataPlata:
                        if (_txtVal1 == null || string.IsNullOrWhiteSpace(_txtVal1.Text)) return null;
                        int zi; if (!int.TryParse(_txtVal1.Text, out zi) || zi < 1 || zi > 31) return null;
                        return ClauzeTextBuilder.BuildDataPlata(zi);

                    case TipModificare.PreavizConcediere:
                        if (_txtVal1 == null || string.IsNullOrWhiteSpace(_txtVal1.Text)) return null;
                        int pc; if (!int.TryParse(_txtVal1.Text, out pc)) return null;
                        return ClauzeTextBuilder.BuildPreavizConcediere(pc);

                    case TipModificare.PreavizDemisie:
                        if (_txtVal1 == null || string.IsNullOrWhiteSpace(_txtVal1.Text)) return null;
                        int pd; if (!int.TryParse(_txtVal1.Text, out pd)) return null;
                        return ClauzeTextBuilder.BuildPreavizDemisie(pd);
                }
            }
            catch { }
            return null;
        }

        // ══════════════════════════════════════════════════════
        //  CONSTRUCTORI CAMPURI
        // ══════════════════════════════════════════════════════
        private void BuildSalariu()
        {
            _txtVal1 = MakeNumericBox();
            _txtVal1.TextChanged += (s, e) => TriggerPreviewUpdate();
            _pnlCampuri.Controls.Add(LabeledCtrl("Salariu nou (RON brut)", _txtVal1, 0, 200));
            _pnlCampuri.Height = 46;
        }

        private void BuildFunctie()
        {
            _txtVal1 = MakeTextBox("ex. BRUTAR");
            _txtVal2 = MakeTextBox("ex. 751201");
            _txtVal1.TextChanged += (s, e) => TriggerPreviewUpdate();
            _txtVal2.TextChanged += (s, e) => TriggerPreviewUpdate();
            _pnlCampuri.Controls.Add(LabeledCtrl("Functie noua", _txtVal1, 0, 240));
            _pnlCampuri.Controls.Add(LabeledCtrl("Cod COR", _txtVal2, 250, 120));
            _pnlCampuri.Height = 46;
        }

        private void BuildTextArea(string placeholder)
        {
            _txtArea = new TextBox
            {
                Multiline = true,
                Height = 60,
                Width = 500,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetPlaceholder(_txtArea, placeholder);
            _txtArea.TextChanged += (s, e) =>
            {
                if (_txtArea.ForeColor != Color.Gray) TriggerPreviewUpdate();
            };
            var wrap = new Panel { Left = 0, Top = 0, Width = 510, Height = 68, BackColor = Color.White };
            wrap.Controls.Add(new Label
            {
                Text = "Text",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextSecundar,
                Width = 510,
                Height = 14,
                Location = new Point(0, 0)
            });
            _txtArea.Location = new Point(0, 16);
            wrap.Controls.Add(_txtArea);
            _pnlCampuri.Controls.Add(wrap);
            _pnlCampuri.Height = 72;
        }

        private void BuildDurata()
        {
            _cmbVal1 = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Font = new Font("Segoe UI", 9.5f)
            };
            _cmbVal1.Items.AddRange(new[] { "nedeterminata", "determinata" });
            _cmbVal1.SelectedIndex = 0;
            _cmbVal1.SelectedIndexChanged += (s, e) => TriggerPreviewUpdate();

            _dtpVal = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 130,
                Font = new Font("Segoe UI", 9.5f)
            };
            _dtpVal.ValueChanged += (s, e) => TriggerPreviewUpdate();

            _pnlCampuri.Controls.Add(LabeledCtrl("Tip durata", _cmbVal1, 0, 170));
            _pnlCampuri.Controls.Add(LabeledCtrl("Data expirare", _dtpVal, 180, 140));
            _pnlCampuri.Height = 46;
        }

        private void BuildConcediu()
        {
            _txtVal1 = MakeNumericBox("21");
            _txtVal1.TextChanged += (s, e) => TriggerPreviewUpdate();
            _pnlCampuri.Controls.Add(LabeledCtrl("Zile concediu de baza", _txtVal1, 0, 180));
            _pnlCampuri.Height = 46;
        }

        private void BuildConcediuSup()
        {
            _txtVal1 = MakeNumericBox("0");
            _txtVal1.TextChanged += (s, e) => TriggerPreviewUpdate();
            _pnlCampuri.Controls.Add(LabeledCtrl("Zile concediu suplimentar", _txtVal1, 0, 200));
            _pnlCampuri.Height = 46;
        }

        private void BuildDataPlata()
        {
            _txtVal1 = MakeNumericBox("15");
            _txtVal1.TextChanged += (s, e) => TriggerPreviewUpdate();
            _pnlCampuri.Controls.Add(LabeledCtrl("Ziua de plata (1-31)", _txtVal1, 0, 170));
            _pnlCampuri.Height = 46;
        }

        private void BuildPreaviz()
        {
            _txtVal1 = MakeNumericBox("20");
            _txtVal1.TextChanged += (s, e) => TriggerPreviewUpdate();
            _pnlCampuri.Controls.Add(LabeledCtrl("Zile preaviz", _txtVal1, 0, 150));
            _pnlCampuri.Height = 46;
        }

        // ══════════════════════════════════════════════════════
        //  HELPERS UI
        // ══════════════════════════════════════════════════════
        private TextBox MakeTextBox(string placeholder)
        {
            var tb = new TextBox { Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            SetPlaceholder(tb, placeholder);
            return tb;
        }

        private static TextBox MakeNumericBox(string defaultVal = "")
        {
            var tb = new TextBox
            {
                Text = defaultVal,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            tb.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b') e.Handled = true;
            };
            return tb;
        }

        private static readonly Font FInputBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font FInputRegular = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        private static void SetPlaceholder(TextBox tb, string placeholder)
        {
            tb.Text = placeholder;
            tb.ForeColor = Color.Gray;
            tb.Font = FInputRegular;
            tb.GotFocus += (s, e) =>
            {
                if (tb.ForeColor == Color.Gray)
                {
                    tb.Text = string.Empty;
                    tb.ForeColor = Color.FromArgb(25, 35, 55);
                    tb.Font = FInputBold;
                }
            };
            tb.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = placeholder;
                    tb.ForeColor = Color.Gray;
                    tb.Font = FInputRegular;
                }
            };
        }

        private static Panel LabeledCtrl(string labelText, Control ctrl, int left, int width)
        {
            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(90, 110, 140),
                AutoSize = false,
                Width = width,
                Height = 14,
                Left = 0,
                Top = 0
            };
            ctrl.Left = 0;
            ctrl.Top = 16;
            ctrl.Width = width;
            ctrl.Height = 24;

            var wrap = new Panel { Left = left, Top = 0, Width = width + 4, Height = 44, BackColor = Color.White };
            wrap.Controls.Add(lbl);
            wrap.Controls.Add(ctrl);
            return wrap;
        }
    }
}