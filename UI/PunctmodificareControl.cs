using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    public class PunctModificareControl : Panel
    {
        // ── Constante UI ──────────────────────────────────────
        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        private static readonly Color AlbastruDes = Color.FromArgb(235, 241, 251);
        private static readonly Color TextPrincipal = Color.FromArgb(25, 35, 55);
        private static readonly Color TextSecundar = Color.FromArgb(90, 110, 140);
        private static readonly Color Verde = Color.FromArgb(34, 130, 84);
        private static readonly Color VerdeDes = Color.FromArgb(235, 248, 240);

        // ── State ─────────────────────────────────────────────
        private List<ClauzeActAditional> _clauze;
        private ClauzeActAditional _clauzeSelected; // null = Text liber
        private readonly List<Control> _campInputs = new List<Control>();
        private bool _manuallyEdited;
        private int _numar;

        // ── Controale ─────────────────────────────────────────
        private Label _lblNumar;
        private ComboBox _cmbTip;
        private Button _btnDelete;
        private Panel _pnlCampuri;
        private Panel _pnlPreview;
        private Label _lblPreviewRef;
        private Label _lblPreviewText;
        private Button _btnEdit;
        private Panel _pnlEdit;
        private TextBox _txtEditRef;
        private TextBox _txtEditText;

        public int Numar
        {
            get { return _numar; }
            set { _numar = value; if (_lblNumar != null) _lblNumar.Text = value + "."; }
        }

        public Action OnDelete { get; set; }

        // ══════════════════════════════════════════════════════
        public PunctModificareControl(int numar, List<ClauzeActAditional> clauze)
        {
            _numar = numar;
            _clauze = clauze ?? new List<ClauzeActAditional>();

            AutoSize = false;
            BackColor = Color.White;
            Padding = new Padding(10, 8, 10, 8);
            Height = 54;

            Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 220, 235), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                e.Graphics.FillRectangle(new SolidBrush(Albastru), 0, 0, 4, Height);
            };

            BuildLayout();
            Resize += (s, e) => RepositionAll();
        }

        // ══════════════════════════════════════════════════════
        //  REFRESH CLAUZE (apelat dupa editor)
        // ══════════════════════════════════════════════════════
        public void SetClauze(List<ClauzeActAditional> clauze)
        {
            _clauze = clauze ?? new List<ClauzeActAditional>();
            string prevTitlu = _cmbTip.SelectedItem?.ToString();
            PopulateDropdown();
            // Re-selectam daca titlul mai exista
            if (prevTitlu != null)
            {
                int idx = _cmbTip.FindStringExact(prevTitlu);
                if (idx >= 0) _cmbTip.SelectedIndex = idx;
            }
        }

        // ══════════════════════════════════════════════════════
        //  BUILD LAYOUT
        // ══════════════════════════════════════════════════════
        private void BuildLayout()
        {
            _lblNumar = new Label
            {
                Text = _numar + ".",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Albastru,
                AutoSize = true,
                Location = new Point(14, 10)
            };

            _cmbTip = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Width = 320,
                Location = new Point(38, 7)
            };
            PopulateDropdown();
            _cmbTip.SelectedIndexChanged += CmbTip_Changed;

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
            _btnDelete.FlatAppearance.BorderSize = 2;
            _btnDelete.Click += (s, e) => { if (OnDelete != null) OnDelete(); };

            _pnlCampuri = new Panel { Height = 0, BackColor = Color.White };

            // Preview
            _pnlPreview = new Panel { Height = 0, BackColor = VerdeDes, Padding = new Padding(8, 6, 8, 6) };
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
                Height = 30,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Albastru,
                Font = new Font("Segoe UI", 8f),
                Cursor = Cursors.Hand
            };
            _btnEdit.FlatAppearance.BorderSize = 2;
            _btnEdit.FlatAppearance.BorderColor = Albastru;
            _btnEdit.Click += BtnEdit_Click;
            _pnlPreview.Controls.AddRange(new Control[] { _lblPreviewRef, _lblPreviewText, _btnEdit });

            // Edit manual panel
            _pnlEdit = new Panel { Height = 0, BackColor = AlbastruDes, Padding = new Padding(8, 6, 8, 6), Visible = false };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(4),
                BackColor = Color.Transparent,
                AutoSize = false
            };

            var lblRef = new Label { Text = "Referință clauză:", Font = new Font("Segoe UI", 8f), ForeColor = TextSecundar, AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
            _txtEditRef = new TextBox { Font = new Font("Segoe UI", 9f), BorderStyle = BorderStyle.FixedSingle, Height = 24, Margin = new Padding(0, 0, 0, 8) };
            var lblTxt = new Label { Text = "Text modificare:", Font = new Font("Segoe UI", 8f), ForeColor = TextSecundar, AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
            _txtEditText = new TextBox { Multiline = true, Font = new Font("Segoe UI", 9f), BorderStyle = BorderStyle.FixedSingle, Height = 60, ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 8) };

            var pnlBtns = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0) };
            var btnCancel = new Button { Text = "Anulează", Height = 26, Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(240, 242, 246), ForeColor = TextSecundar, Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand, Margin = new Padding(4, 0, 0, 0) };
            btnCancel.FlatAppearance.BorderSize = 2;
            btnCancel.Click += (s, e) => { _manuallyEdited = false; _pnlEdit.Visible = false; _pnlEdit.Height = 0; RepositionAll(); };
            var btnSave = new Button { Text = "✓ Salvează", Height = 26, Width = 100, FlatStyle = FlatStyle.Flat, BackColor = Verde, ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0) };
            btnSave.FlatAppearance.BorderSize = 2;
            btnSave.Click += (s, e) => { _manuallyEdited = true; _pnlEdit.Visible = false; _pnlEdit.Height = 0; UpdatePreview(_txtEditRef.Text.Trim(), _txtEditText.Text.Trim()); RepositionAll(); };
            pnlBtns.Controls.Add(btnCancel);
            pnlBtns.Controls.Add(btnSave);

            flow.Controls.AddRange(new Control[] { lblRef, _txtEditRef, lblTxt, _txtEditText, pnlBtns });
            _pnlEdit.Controls.Add(flow);
            _pnlEdit.Resize += (s, e) =>
            {
                int w = _pnlEdit.ClientSize.Width - 16;
                _txtEditRef.Width = w;
                _txtEditText.Width = w;
                pnlBtns.Width = w;
            };

            Controls.AddRange(new Control[] { _lblNumar, _cmbTip, _btnDelete, _pnlCampuri, _pnlPreview, _pnlEdit });
            RepositionAll();
        }

        private void PopulateDropdown()
        {
            _cmbTip.Items.Clear();
            foreach (var c in _clauze.Where(c => c.Activ))
                _cmbTip.Items.Add(c.Titlu ?? "(fara titlu)");
            _cmbTip.Items.Add("Text liber");
        }

        // ══════════════════════════════════════════════════════
        //  SCHIMBARE TIP
        // ══════════════════════════════════════════════════════
        private void CmbTip_Changed(object sender, EventArgs e)
        {
            _pnlCampuri.Controls.Clear();
            _campInputs.Clear();
            _pnlCampuri.Height = 0;
            _pnlPreview.Height = 0;
            _pnlEdit.Height = 0;
            _pnlEdit.Visible = false;
            _manuallyEdited = false;

            int idx = _cmbTip.SelectedIndex;
            if (idx < 0) { RecalcHeight(); return; }

            var activ = _clauze.Where(c => c.Activ).ToList();

            // Text liber — last item
            if (idx >= activ.Count)
            {
                _clauzeSelected = null;
                BuildTextLiber();
            }
            else
            {
                _clauzeSelected = activ[idx];
                BuildDynamic(_clauzeSelected);
            }

            RepositionAll();
        }

        private void BuildDynamic(ClauzeActAditional clauza)
        {
            if (clauza.Campuri == null || clauza.Campuri.Count == 0)
            {
                UpdatePreview(clauza.TextClauza, clauza.TextDinamic);
                return;
            }

            var campuri = clauza.Campuri.OrderBy(c => c.Ordine).ToList();
            int x = 0;

            foreach (var camp in campuri)
            {
                int w = Math.Min(240, Math.Max(120, (Width - 60) / campuri.Count));
                var wrap = new Panel { Left = x, Top = 0, Width = w + 4, Height = 46, BackColor = Color.White };

                var lbl = new Label
                {
                    Text = camp.Label ?? string.Empty,
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = TextSecundar,
                    Width = w,
                    Height = 14,
                    Left = 0,
                    Top = 0
                };

                Control ctrl = BuildCampControl(camp, w);
                _campInputs.Add(ctrl);
                wrap.Controls.Add(lbl);
                wrap.Controls.Add(ctrl);
                _pnlCampuri.Controls.Add(wrap);
                x += w + 8;
            }

            _pnlCampuri.Height = 46;
        }

        private Control BuildCampControl(CampClauza camp, int width)
        {
            string tip = (camp.TipCamp ?? "string").ToLower();

            if (tip == "data")
            {
                var dtp = new DateTimePicker
                {
                    Left = 0,
                    Top = 16,
                    Width = width,
                    Height = 24,
                    Format = DateTimePickerFormat.Short,
                    Font = new Font("Segoe UI", 9.5f)
                };
                dtp.ValueChanged += (s, e) => TriggerPreviewUpdate();
                return dtp;
            }

            var tb = new TextBox
            {
                Left = 0,
                Top = 16,
                Width = width,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            if (tip == "numar intreg")
                tb.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b') e.Handled = true; };
            else if (tip == "numar zecimal")
                tb.KeyPress += (s, e) =>
                {
                    bool hasDot = tb.Text.Contains('.') || tb.Text.Contains(',');
                    if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b' &&
                        !(!hasDot && (e.KeyChar == '.' || e.KeyChar == ','))) e.Handled = true;
                };

            if (!string.IsNullOrEmpty(camp.Placeholder))
                SetPlaceholder(tb, camp.Placeholder);

            tb.TextChanged += (s, e) => { if (tb.ForeColor != Color.Gray) TriggerPreviewUpdate(); };
            return tb;
        }

        private void BuildTextLiber()
        {
            var tb = new TextBox
            {
                Multiline = true,
                Height = 60,
                Width = 500,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetPlaceholder(tb, "Introduceti textul clauzei...");
            tb.TextChanged += (s, e) =>
            {
                if (tb.ForeColor != Color.Gray)
                    UpdatePreview(string.Empty, tb.Text);
            };

            var wrap = new Panel { Left = 0, Top = 0, Width = 510, Height = 68, BackColor = Color.White };
            wrap.Controls.Add(new Label { Text = "Text", Font = new Font("Segoe UI", 8f), ForeColor = TextSecundar, Width = 510, Height = 14, Location = new Point(0, 0) });
            tb.Location = new Point(0, 16);
            wrap.Controls.Add(tb);
            _pnlCampuri.Controls.Add(wrap);
            _pnlCampuri.Height = 72;
            _campInputs.Add(tb);
        }

        // ══════════════════════════════════════════════════════
        //  PREVIEW
        // ══════════════════════════════════════════════════════
        private void TriggerPreviewUpdate()
        {
            if (_manuallyEdited) return;
            if (_clauzeSelected == null) return; // text liber se actualizeaza direct in BuildTextLiber

            var values = _campInputs
                .Select(c => (object)GetCampValue(c))
                .ToArray();

            string text;
            try { text = values.Length > 0 ? string.Format(_clauzeSelected.TextDinamic ?? string.Empty, values) : (_clauzeSelected.TextDinamic ?? string.Empty); }
            catch { text = _clauzeSelected.TextDinamic ?? string.Empty; }

            UpdatePreview(_clauzeSelected.TextClauza, text);
        }

        private void UpdatePreview(string referinta, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { _pnlPreview.Height = 0; RecalcHeight(); return; }

            _lblPreviewRef.Text = string.IsNullOrWhiteSpace(referinta) ? string.Empty : referinta;
            _lblPreviewText.Text = text;

            int refH = string.IsNullOrWhiteSpace(referinta) ? 0 : 18;
            int textH = 0;
            using (var g = _lblPreviewText.CreateGraphics())
            {
                var sz = g.MeasureString(text, _lblPreviewText.Font, Math.Max(_pnlPreview.Width - 24, 200));
                textH = (int)sz.Height + 4;
            }

            _lblPreviewRef.Top = 6; _lblPreviewRef.Height = refH;
            _lblPreviewText.Top = 6 + refH + (refH > 0 ? 2 : 0);
            _lblPreviewText.Height = textH;
            _pnlPreview.Height = 6 + refH + (refH > 0 ? 2 : 0) + textH + _btnEdit.Height + 12;
            RecalcHeight();
        }

        // ══════════════════════════════════════════════════════
        //  EDIT MANUAL
        // ══════════════════════════════════════════════════════
        private string GetCampValue(Control c)
        {
            var dtp = c as DateTimePicker;
            if (dtp != null) return dtp.Value.ToString("dd.MM.yyyy");
            var tb = c as TextBox;
            if (tb != null) return tb.ForeColor == Color.Gray ? string.Empty : tb.Text;
            return c.Text;
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            _txtEditRef.Text = _lblPreviewRef.Text;
            _txtEditText.Text = _lblPreviewText.Text;
            _pnlEdit.Height = 178;
            _pnlEdit.Visible = true;
            RepositionAll();
            _txtEditText.Focus();
        }

        // ══════════════════════════════════════════════════════
        //  GET PUNCT
        // ══════════════════════════════════════════════════════
        public PunctModificare GetPunct()
        {
            if (_manuallyEdited)
            {
                string txt = _txtEditText.Text.Trim();
                if (string.IsNullOrWhiteSpace(txt)) return null;
                return new PunctModificare { Referinta = _txtEditRef.Text.Trim(), TextModificare = txt };
            }

            if (_cmbTip.SelectedIndex < 0) return null;

            // Text liber
            if (_clauzeSelected == null)
            {
                if (_campInputs.Count == 0) return null;
                string t = GetCampValue(_campInputs[0]).Trim();
                if (string.IsNullOrWhiteSpace(t)) return null;
                return new PunctModificare { Referinta = string.Empty, TextModificare = t };
            }

            // JSON-driven
            if (_clauzeSelected.Campuri == null || _clauzeSelected.Campuri.Count == 0)
                return new PunctModificare { Referinta = _clauzeSelected.TextClauza, TextModificare = _clauzeSelected.TextDinamic ?? string.Empty };

            var values = _campInputs
                .Select(c => (object)GetCampValue(c))
                .ToArray();

            if (values.All(v => string.IsNullOrWhiteSpace(v?.ToString()))) return null;

            string textFinal;
            try { textFinal = string.Format(_clauzeSelected.TextDinamic ?? string.Empty, values); }
            catch { textFinal = _clauzeSelected.TextDinamic ?? string.Empty; }

            return new PunctModificare { Referinta = _clauzeSelected.TextClauza ?? string.Empty, TextModificare = textFinal };
        }

        // ══════════════════════════════════════════════════════
        //  POZITIONARE
        // ══════════════════════════════════════════════════════
        private void RepositionAll()
        {
            if (_btnDelete != null)
                _btnDelete.Location = new Point(Width - _btnDelete.Width - 10, 7);

            int innerW = Width - 24;

            if (_pnlCampuri != null) { _pnlCampuri.Left = 12; _pnlCampuri.Top = 42; _pnlCampuri.Width = innerW; }

            int previewTop = 42 + (_pnlCampuri?.Height ?? 0) + (_pnlCampuri?.Height > 0 ? 4 : 0);
            if (_pnlPreview != null)
            {
                _pnlPreview.Left = 12; _pnlPreview.Top = previewTop; _pnlPreview.Width = innerW;
                if (_lblPreviewRef != null) _lblPreviewRef.Width = innerW - 110;
                if (_lblPreviewText != null) _lblPreviewText.Width = innerW - 16;
                if (_btnEdit != null) { _btnEdit.Top = 6; _btnEdit.Left = innerW - _btnEdit.Width - 8; }
            }

            int editTop = previewTop + (_pnlPreview?.Height ?? 0) + (_pnlPreview?.Height > 0 ? 4 : 0);
            if (_pnlEdit != null)
            {
                _pnlEdit.Left = 12; _pnlEdit.Top = editTop; _pnlEdit.Width = innerW;
                if (_txtEditRef != null) { _txtEditRef.Width = innerW - 16; _txtEditText.Width = innerW - 16; }
            }

            RecalcHeight();
        }

        private void RecalcHeight()
        {
            int h = 42;
            if (_pnlCampuri?.Height > 0) h += _pnlCampuri.Height + 4;
            if (_pnlPreview?.Height > 0) h += _pnlPreview.Height + 4;
            if (_pnlEdit?.Height > 0) h += _pnlEdit.Height + 4;
            h += 8;
            Height = Math.Max(h, 50);
        }

        // ── Helpers ───────────────────────────────────────────
        private static readonly Font FInputBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font FInputRegular = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        private static void SetPlaceholder(TextBox tb, string placeholder)
        {
            tb.Text = placeholder; tb.ForeColor = Color.Gray; tb.Font = FInputRegular;
            tb.GotFocus += (s, e) =>
            {
                if (tb.ForeColor != Color.Gray) return;
                tb.Text = string.Empty; tb.ForeColor = Color.FromArgb(25, 35, 55); tb.Font = FInputBold;
            };
            tb.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = placeholder; tb.ForeColor = Color.Gray; tb.Font = FInputRegular; }
            };
        }
    }
}