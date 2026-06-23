using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    internal static class PhHelper
    {
        internal static void SetPh(TextBox tb, string ph)
        {
            tb.Text = ph;
            tb.ForeColor = Color.Gray;
            tb.GotFocus += (s, e) => { if (tb.ForeColor == Color.Gray) { tb.Text = string.Empty; tb.ForeColor = SystemColors.WindowText; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = ph; tb.ForeColor = Color.Gray; } };
        }

        internal static void RelayoutPanel(Panel pnl, List<Control> items)
        {
            int w = Math.Max(pnl.ClientSize.Width - 4, 380);
            int y = 4;
            foreach (var c in items) { c.Width = w; c.Left = 2; c.Top = y; y += c.Height + 4; }
        }
    }
    // ══════════════════════════════════════════════════════════
    //  CONTROL ITEM — REFERAT SURSA
    // ══════════════════════════════════════════════════════════
    public class ReferatSursaControl : Panel
    {
        private static readonly Color AccentViolet = Color.FromArgb(120, 60, 170);
        private Label _lblNumar;
        private TextBox _txtCodSiData;
        private TextBox _txtIntocmitor;
        private Button _btnDelete;
        private int _numar;

        public int Numar
        {
            get { return _numar; }
            set { _numar = value; if (_lblNumar != null) _lblNumar.Text = value + "."; }
        }

        public Action OnDelete { get; set; }

        public ReferatSursaControl(int numar)
        {
            _numar = numar;
            Height = 72;
            BackColor = Color.White;
            Padding = new Padding(8, 6, 8, 6);
            Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(195, 165, 225)))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                e.Graphics.FillRectangle(new SolidBrush(AccentViolet), 0, 0, 4, Height);
            };
            BuildLayout();
        }

        private void BuildLayout()
        {
            _lblNumar = new Label
            {
                Text = _numar + ".",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentViolet,
                AutoSize = true,
                Location = new Point(12, 8)
            };

            _btnDelete = new Button
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
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += (s, e) => { if (OnDelete != null) OnDelete(); };

            var tbl = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 2,
                Dock = DockStyle.None,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0),
                Height = 54
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

            _txtCodSiData = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Dock = DockStyle.Top,
                Height = 26
            };
            PhHelper.SetPh(_txtCodSiData, "ex. 168/14.04.2025");
            _txtIntocmitor = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Dock = DockStyle.Top,
                Height = 26
            };
            PhHelper.SetPh(_txtIntocmitor, "Nume, Prenume — Funcție");

            var cell0 = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 6, 0) };
            cell0.Controls.Add(_txtCodSiData);
            cell0.Controls.Add(new Label { Text = "Cod înregistrare / Data", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true });

            var cell1 = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0) };
            cell1.Controls.Add(_txtIntocmitor);
            cell1.Controls.Add(new Label { Text = "Întocmitor referat (Nume, Prenume — Funcție)", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true });

            tbl.Controls.Add(cell0, 0, 0);
            tbl.Controls.Add(cell1, 1, 0);

            Controls.Add(_lblNumar);
            Controls.Add(_btnDelete);
            Controls.Add(tbl);

            Resize += (s, e) =>
            {
                _btnDelete.Left = Width - _btnDelete.Width - 8;
                tbl.Left = 32;
                tbl.Top = 6;
                tbl.Width = Width - 32 - _btnDelete.Width - 14;
            };
        }

        public bool IsValid()
        {
            string v = _txtCodSiData.Text.Trim();
            return !string.IsNullOrWhiteSpace(v) && _txtCodSiData.ForeColor != Color.Gray;
        }

        public ReferatSursaItem GetItem()
        {
            if (!IsValid()) return null;
            return new ReferatSursaItem
            {
                CodSiData = _txtCodSiData.Text.Trim(),
                Intocmitor = _txtIntocmitor.ForeColor == Color.Gray ? string.Empty : _txtIntocmitor.Text.Trim()
            };
        }
    }

    // ══════════════════════════════════════════════════════════
    //  CONTROL ITEM — MEMBRU COMISIE
    // ══════════════════════════════════════════════════════════
    public class MembruComisieControl : Panel
    {
        private static readonly Color AccentViolet = Color.FromArgb(120, 60, 170);
        private Label _lblNumar;
        private TextBox _txtNume;
        private TextBox _txtFunctie;
        private Button _btnDelete;
        private int _numar;

        public int Numar
        {
            get { return _numar; }
            set { _numar = value; if (_lblNumar != null) _lblNumar.Text = value + "."; }
        }

        public Action OnDelete { get; set; }

        public MembruComisieControl(int numar)
        {
            _numar = numar;
            Height = 72;
            BackColor = Color.White;
            Padding = new Padding(8, 6, 8, 6);
            Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(195, 165, 225)))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                e.Graphics.FillRectangle(new SolidBrush(AccentViolet), 0, 0, 4, Height);
            };
            BuildLayout();
        }

        private void BuildLayout()
        {
            _lblNumar = new Label
            {
                Text = _numar + ".",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentViolet,
                AutoSize = true,
                Location = new Point(12, 8)
            };

            _btnDelete = new Button
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
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += (s, e) => { if (OnDelete != null) OnDelete(); };

            var tbl = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 2,
                Dock = DockStyle.None,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0),
                Height = 54
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            _txtNume = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Dock = DockStyle.Top,
                Height = 26
            };
            PhHelper.SetPh(_txtNume, "Nume și prenume");
            _txtFunctie = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Dock = DockStyle.Top,
                Height = 26
            };
            PhHelper.SetPh(_txtFunctie, "Funcția");

            var cell0 = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 6, 0) };
            cell0.Controls.Add(_txtNume);
            cell0.Controls.Add(new Label { Text = "Nume și Prenume", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true });

            var cell1 = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0) };
            cell1.Controls.Add(_txtFunctie);
            cell1.Controls.Add(new Label { Text = "Funcție", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true });

            tbl.Controls.Add(cell0, 0, 0);
            tbl.Controls.Add(cell1, 1, 0);

            Controls.Add(_lblNumar);
            Controls.Add(_btnDelete);
            Controls.Add(tbl);

            Resize += (s, e) =>
            {
                _btnDelete.Left = Width - _btnDelete.Width - 8;
                tbl.Left = 32;
                tbl.Top = 6;
                tbl.Width = Width - 32 - _btnDelete.Width - 14;
            };
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(_txtNume.Text) && _txtNume.ForeColor != Color.Gray;
        }

        public MembruComisieItem GetItem()
        {
            if (!IsValid()) return null;
            return new MembruComisieItem
            {
                Nume = _txtNume.Text.Trim(),
                Functie = _txtFunctie.ForeColor == Color.Gray ? string.Empty : _txtFunctie.Text.Trim()
            };
        }
    }

    // ══════════════════════════════════════════════════════════
    //  DECIZIE CONSTITUIRE COMISIE CERCETARE DISCIPLINARA
    // ══════════════════════════════════════════════════════════
    public class DecizieConstituireComisieForm : DocumentFormBase
    {
        private readonly DecizieConstituireComisieModel _m;

        // Date decizie
        private TextBox _txtCodInregistrare;
        private DateTimePicker _dtpDataDecizie;
        private TextBox _txtNumeIntocmitorHr;

        // Nota explicativa
        private DateTimePicker _dtpDataNotaExplicativa;

        // CCM / ITM
        private TextBox _txtIntervalAniCCM;
        private TextBox _txtCodInregistrareITM;

        // Descriere abatere
        private TextBox _txtDescriereAbatere;

        // Liste dinamice
        private Panel _pnlReferate;
        private readonly List<ReferatSursaControl> _referate = new List<ReferatSursaControl>();

        private TextBox _txtNumePresedinte;
        private TextBox _txtFunctiePresedinte;
        private Panel _pnlMembri;
        private readonly List<MembruComisieControl> _membri = new List<MembruComisieControl>();
        private TextBox _txtNumeObservator;
        private TextBox _txtFunctieObservator;

        // Perioada cercetare
        private DateTimePicker _dtpDataInceput;
        private DateTimePicker _dtpDataSfarsit;

        // Intocmit de
        private TextBox _txtNumeIntocmit;
        private TextBox _txtFunctieIntocmit;

        public DecizieConstituireComisieForm(DecizieConstituireComisieModel model)
            : base(model, "Decizie constituire comisie cercetare disciplinară")
        {
            _m = model;
            BuildBody();
            Shown += (s, e) =>
            {
                // Forteaza width corect pe panourile dinamice la prima afisare
                if (_pnlReferate != null)
                {
                    _pnlReferate.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                    PhHelper.RelayoutPanel(_pnlReferate, _referate.Cast<Control>().ToList());
                }
                if (_pnlMembri != null)
                {
                    _pnlMembri.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                    PhHelper.RelayoutPanel(_pnlMembri, _membri.Cast<Control>().ToList());
                }
            };
        }

        private void BuildBody()
        {
            int y = 0;

            // ── DATE DECIZIE ──────────────────────────────────
            var pnlDoc = AddSectiune("DATE DECIZIE", ref y, 78);
            _txtCodInregistrare = MakeReadonly();
            CodInregistrareField = _txtCodInregistrare;
            if (!string.IsNullOrEmpty(_model.CodInregistrare))
                _txtCodInregistrare.Text = _model.CodInregistrare;
            _dtpDataDecizie = MakeDtp();
            _txtNumeIntocmitorHr = MakeInput();
            _txtNumeIntocmitorHr.Text = _m.NumeIntocmitorHr;
            var tbl1 = AddRow(pnlDoc, new[] { 40, 30, 30 });
            AddLabeledInput(tbl1, 0, "Cod înregistrare", _txtCodInregistrare, required: true);
            AddLabeledInput(tbl1, 1, "Data deciziei", _dtpDataDecizie);
            AddLabeledInput(tbl1, 2, "Întocmit de (HR)", _txtNumeIntocmitorHr);

            // ── REFERATE SURSA (PV-style: header + AutoScroll panel direct in PnlBody) ──
            _pnlReferate = AddDynamicListSection("REFERATE SURSĂ", "Adaugă referat",
                () => AddReferat(), ref y);

            // ── NOTA EXPLICATIVA ──────────────────────────────
            var pnlNota = AddSectiune("NOTA EXPLICATIVĂ", ref y, 62);
            _dtpDataNotaExplicativa = MakeDtp();
            var tblNota = AddRow(pnlNota, new[] { 50, 50 });
            AddLabeledInput(tblNota, 0, "Data notei explicative", _dtpDataNotaExplicativa);

            // ── DESCRIERE ABATERE ─────────────────────────────
            var pnlDescr = AddSectiune("DESCRIERE ABATERE", ref y, 112);
            _txtDescriereAbatere = MakeMultiline(88);
            _txtDescriereAbatere.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;
            pnlDescr.Controls.Add(_txtDescriereAbatere);
            pnlDescr.Resize += (s, e) => _txtDescriereAbatere.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;

            // ── COMISIA — PRESEDINTE ──────────────────────────
            var pnlPres = AddSectiune("COMPONENȚA COMISIEI — PREȘEDINTE", ref y, 78);
            _txtNumePresedinte = MakeInput("Nume și prenume");
            _txtFunctiePresedinte = MakeInput("Funcția");
            var tblPres = AddRow(pnlPres, new[] { 50, 50 });
            AddLabeledInput(tblPres, 0, "Nume Președinte", _txtNumePresedinte, required: true);
            AddLabeledInput(tblPres, 1, "Funcție Președinte", _txtFunctiePresedinte, required: true);

            // ── COMISIA — MEMBRI (PV-style) ───────────────────
            _pnlMembri = AddDynamicListSection("COMPONENȚA COMISIEI — MEMBRI", "Adaugă membru",
                () => AddMembru(), ref y);

            // ── OBSERVATOR ────────────────────────────────────
            var pnlObs = AddSectiune("OBSERVATOR (SUPERIOR DIRECT)", ref y, 78);
            _txtNumeObservator = MakeInput("Nume și prenume");
            _txtFunctieObservator = MakeInput("Funcția");
            var tblObs = AddRow(pnlObs, new[] { 50, 50 });
            AddLabeledInput(tblObs, 0, "Nume Observator", _txtNumeObservator);
            AddLabeledInput(tblObs, 1, "Funcție Observator", _txtFunctieObservator);

            // ── DATE CONTRACT COLECTIV / ITM ──────────────────
            var pnlCCM = AddSectiune("DATE CONTRACT COLECTIV / ITM", ref y, 78);
            _txtIntervalAniCCM = MakeInput();
            _txtIntervalAniCCM.Text = _m.IntervalAniCCM;
            _txtCodInregistrareITM = MakeInput();
            _txtCodInregistrareITM.Text = _m.CodInregistrareITM;
            var tblCCM = AddRow(pnlCCM, new[] { 30, 70 });
            AddLabeledInput(tblCCM, 0, "Interval ani CCM", _txtIntervalAniCCM);
            AddLabeledInput(tblCCM, 1, "Cod înregistrare ITM", _txtCodInregistrareITM);

            // ── PERIOADA CERCETARE ────────────────────────────
            var pnlPer = AddSectiune("PERIOADA CERCETĂRII", ref y, 78);
            _dtpDataInceput = MakeDtp();
            _dtpDataSfarsit = MakeDtp();
            var tblPer = AddRow(pnlPer, new[] { 50, 50 });
            AddLabeledInput(tblPer, 0, "Data început", _dtpDataInceput);
            AddLabeledInput(tblPer, 1, "Data sfârșit", _dtpDataSfarsit);

            // ── MENȚIUNI ──────────────────────────────────────
            AddMentiuniSection(ref y);

            // Adauga initial 1 referat si 2 membri
            AddReferat();
            AddMembru();
            AddMembru();
        }

        // ── Referate ──────────────────────────────────────────
        private void AddReferat()
        {
            var ctrl = new ReferatSursaControl(_referate.Count + 1);
            ctrl.Width = Math.Max(_pnlReferate.Width - 2, 400);
            ctrl.OnDelete = () =>
            {
                _referate.Remove(ctrl);
                _pnlReferate.Controls.Remove(ctrl);
                RenumberReferate();
                PhHelper.RelayoutPanel(_pnlReferate, _referate.ConvertAll(x => (Control)x));
            };
            _referate.Add(ctrl);
            _pnlReferate.Controls.Add(ctrl);
            PhHelper.RelayoutPanel(_pnlReferate, _referate.ConvertAll(x => (Control)x));
        }

        private void RenumberReferate()
        {
            for (int i = 0; i < _referate.Count; i++) _referate[i].Numar = i + 1;
        }

        // ── Membri ────────────────────────────────────────────
        private void AddMembru()
        {
            var ctrl = new MembruComisieControl(_membri.Count + 1);
            ctrl.Width = Math.Max(_pnlMembri.Width - 2, 400);
            ctrl.OnDelete = () =>
            {
                _membri.Remove(ctrl);
                _pnlMembri.Controls.Remove(ctrl);
                RenumberMembri();
                PhHelper.RelayoutPanel(_pnlMembri, _membri.ConvertAll(x => (Control)x));
            };
            _membri.Add(ctrl);
            _pnlMembri.Controls.Add(ctrl);
            PhHelper.RelayoutPanel(_pnlMembri, _membri.ConvertAll(x => (Control)x));
        }

        private void RenumberMembri()
        {
            for (int i = 0; i < _membri.Count; i++) _membri[i].Numar = i + 1;
        }

        // ── Validate ──────────────────────────────────────────
        protected override bool ValidateFormForPreview()
        {
            if (!RequireText(_txtCodInregistrare, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCodInregistrare)) return false;
            return true;
        }

        protected override bool ValidateForm()
        {
            if (!RequireText(_txtCodInregistrare, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCodInregistrare)) return false;
            if (!RequireText(_txtNumePresedinte, "Nume Președinte")) return false;
            if (!RequireText(_txtFunctiePresedinte, "Funcție Președinte")) return false;
            if (_referate.Count == 0 || !_referate[0].IsValid())
            {
                MessageBox.Show("Adaugă cel puțin un referat sursă.", "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        // ── Populate model ────────────────────────────────────
        protected override void PopulateModel()
        {
            FillAngajator(_m);
            _m.CodInregistrare = GetText(_txtCodInregistrare);
            _m.DataDecizie = GetDate(_dtpDataDecizie);
            _m.NumeIntocmitorHr = GetText(_txtNumeIntocmitorHr);
            _m.DataNotaExplicativa = GetDate(_dtpDataNotaExplicativa);
            _m.DescriereAbatere = GetText(_txtDescriereAbatere);
            _m.IntervalAniCCM = GetText(_txtIntervalAniCCM);
            _m.CodInregistrareITM = GetText(_txtCodInregistrareITM);
            _m.NumePresedinte = GetText(_txtNumePresedinte);
            _m.FunctiePresedinte = GetText(_txtFunctiePresedinte);
            _m.NumeObservator = GetText(_txtNumeObservator);
            _m.FunctieObservator = GetText(_txtFunctieObservator);
            _m.DataInceputCercetare = GetDate(_dtpDataInceput);
            _m.DataSfarsitCercetare = GetDate(_dtpDataSfarsit);

            _m.Referate.Clear();
            foreach (var r in _referate) { var item = r.GetItem(); if (item != null) _m.Referate.Add(item); }

            _m.Membri.Clear();
            foreach (var mb in _membri) { var item = mb.GetItem(); if (item != null) _m.Membri.Add(item); }
        }

        protected override string GetTemplatePath()
            => PluginConfig.GetTemplatePath(TipDocument.DecizieConstituireComisie);

        protected override DateTime GetRegistraturaDate()
            => _dtpDataDecizie != null ? _dtpDataDecizie.Value.Date : base.GetRegistraturaDate();
    }

    // ══════════════════════════════════════════════════════════
    //  CONVOCARE CERCETARE DISCIPLINARA
    // ══════════════════════════════════════════════════════════
    public class ConvocareCercetareForm : DocumentFormBase
    {
        private readonly ConvocareCercetareModel _m;

        private TextBox _txtCodInregistrare;
        private DateTimePicker _dtpDataConvocare;
        private TextBox _txtNumeIntocmitorHr;
        private TextBox _txtCodCor;

        private Panel _pnlReferate;
        private readonly List<ReferatSursaControl> _referate = new List<ReferatSursaControl>();
        private DateTimePicker _dtpDataNotaExplicativa;
        private TextBox _txtDescriereAbatere;

        private TextBox _txtIntervalAniCCM;
        private TextBox _txtCodInregistrareITM;

        private TextBox _txtLocCercetare;
        private DateTimePicker _dtpDataCercetare;
        private TextBox _txtOraConvocare;

        private TextBox _txtNrDecizieComisie;
        private DateTimePicker _dtpDataDecizieComisie;

        private Panel _pnlMembri;
        private readonly List<MembruComisieControl> _membri = new List<MembruComisieControl>();

        public ConvocareCercetareForm(ConvocareCercetareModel model)
            : base(model, "Convocare cercetare disciplinară prealabilă")
        {
            _m = model;
            BuildBody();
            Shown += (s, e) =>
            {
                if (_pnlReferate != null)
                {
                    _pnlReferate.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                    PhHelper.RelayoutPanel(_pnlReferate, _referate.Cast<Control>().ToList());
                }
                if (_pnlMembri != null)
                {
                    _pnlMembri.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                    PhHelper.RelayoutPanel(_pnlMembri, _membri.Cast<Control>().ToList());
                }
            };
        }

        private void BuildBody()
        {
            int y = 0;

            // ── DATE CONVOCARE ────────────────────────────────
            var pnlDoc = AddSectiune("DATE CONVOCARE", ref y, 78);
            _txtCodInregistrare = MakeReadonly();
            CodInregistrareField = _txtCodInregistrare;
            if (!string.IsNullOrEmpty(_model.CodInregistrare))
                _txtCodInregistrare.Text = _model.CodInregistrare;
            _dtpDataConvocare = MakeDtp();
            _txtNumeIntocmitorHr = MakeInput();
            _txtNumeIntocmitorHr.Text = _m.NumeIntocmitorHr;
            var tbl1 = AddRow(pnlDoc, new[] { 40, 30, 30 });
            AddLabeledInput(tbl1, 0, "Cod înregistrare", _txtCodInregistrare, required: true);
            AddLabeledInput(tbl1, 1, "Data convocării", _dtpDataConvocare);
            AddLabeledInput(tbl1, 2, "Întocmit de (HR)", _txtNumeIntocmitorHr);

            // ── DATE ANGAJAT SUPLIMENTAR: COR ─────────────────
            var pnlCor = AddSectiune("COD COR", ref y, 62);
            _txtCodCor = MakeInput();
            if (!string.IsNullOrEmpty(_m.CodCor))
                _txtCodCor.Text = _m.CodCor;
            else
                SetPlaceholder(_txtCodCor, "ex. 332203");
            var tblCor = AddRow(pnlCor, new[] { 50, 50 });
            AddLabeledInput(tblCor, 0, "Cod COR funcție", _txtCodCor);

            // ── REFERATE SURSA ────────────────────────────────
            _pnlReferate = AddDynamicListSection("REFERATE SURSĂ", "Adaugă referat",
                () => AddReferat(), ref y);

            // ── NOTA EXPLICATIVA + DESCRIERE ──────────────────
            var pnlNota = AddSectiune("NOTA EXPLICATIVĂ ȘI DESCRIERE ABATERE", ref y, 190);
            _dtpDataNotaExplicativa = MakeDtp();
            var tblNota = AddRow(pnlNota, new[] { 50, 50 });
            AddLabeledInput(tblNota, 0, "Data notei explicative", _dtpDataNotaExplicativa);

            _txtDescriereAbatere = MakeMultiline(110);
            _txtDescriereAbatere.Width = pnlNota.ClientSize.Width - pnlNota.Padding.Horizontal;
            pnlNota.Controls.Add(_txtDescriereAbatere);
            pnlNota.Resize += (s, e) => _txtDescriereAbatere.Width = pnlNota.ClientSize.Width - pnlNota.Padding.Horizontal;

            // ── PRECOMPLETATE EDITABILE ───────────────────────
            var pnlPre = AddSectiune("DATE CONTRACT COLECTIV / ITM", ref y, 78);
            _txtIntervalAniCCM = MakeInput();
            _txtIntervalAniCCM.Text = _m.IntervalAniCCM;
            _txtCodInregistrareITM = MakeInput();
            _txtCodInregistrareITM.Text = _m.CodInregistrareITM;
            var tblPre = AddRow(pnlPre, new[] { 30, 70 });
            AddLabeledInput(tblPre, 0, "Interval ani CCM", _txtIntervalAniCCM);
            AddLabeledInput(tblPre, 1, "Cod înregistrare ITM", _txtCodInregistrareITM);

            // ── DETALII CERCETARE ─────────────────────────────
            var pnlCerc = AddSectiune("DATA, ORA ȘI LOCUL CERCETĂRII", ref y, 130);
            _dtpDataCercetare = MakeDtp();
            _txtOraConvocare = MakeInput("ex. 12:30");
            var tblCerc = AddRow(pnlCerc, new[] { 40, 30, 30 });
            AddLabeledInput(tblCerc, 0, "Data cercetării", _dtpDataCercetare, required: true);
            AddLabeledInput(tblCerc, 1, "Ora", _txtOraConvocare, required: true);

            _txtLocCercetare = MakeInput();
            _txtLocCercetare.Text = _m.LocCercetare;
            var tblLoc = AddRow(pnlCerc, new[] { 100 });
            AddLabeledInput(tblLoc, 0, "Locul cercetării", _txtLocCercetare);

            // ── DECIZIA COMISIEI ──────────────────────────────
            var pnlDec = AddSectiune("DECIZIA DE CONSTITUIRE A COMISIEI", ref y, 78);
            _txtNrDecizieComisie = MakeInput("ex. 135");
            _dtpDataDecizieComisie = MakeDtp();
            var tblDec = AddRow(pnlDec, new[] { 40, 60 });
            AddLabeledInput(tblDec, 0, "Nr. decizie comisie", _txtNrDecizieComisie, required: true);
            AddLabeledInput(tblDec, 1, "Data deciziei comisie", _dtpDataDecizieComisie);

            // ── MEMBRI COMISIE ────────────────────────────────
            _pnlMembri = AddDynamicListSection("COMPONENȚA COMISIEI", "Adaugă membru",
                () => AddMembru(), ref y);

            // ── MENȚIUNI ──────────────────────────────────────
            AddMentiuniSection(ref y);

            // Initiale
            AddReferat();
            AddMembru();
            AddMembru();
        }

        private void AddReferat()
        {
            var ctrl = new ReferatSursaControl(_referate.Count + 1);
            ctrl.Width = Math.Max(_pnlReferate.Width - 2, 400);
            ctrl.OnDelete = () =>
            {
                _referate.Remove(ctrl);
                _pnlReferate.Controls.Remove(ctrl);
                for (int i = 0; i < _referate.Count; i++) _referate[i].Numar = i + 1;
                PhHelper.RelayoutPanel(_pnlReferate, _referate.Cast<Control>().ToList());
            };
            _referate.Add(ctrl);
            _pnlReferate.Controls.Add(ctrl);
            PhHelper.RelayoutPanel(_pnlReferate, _referate.Cast<Control>().ToList());
        }

        private void AddMembru()
        {
            var ctrl = new MembruComisieControl(_membri.Count + 1);
            ctrl.Width = Math.Max(_pnlMembri.Width - 2, 400);
            ctrl.OnDelete = () =>
            {
                _membri.Remove(ctrl);
                _pnlMembri.Controls.Remove(ctrl);
                for (int i = 0; i < _membri.Count; i++) _membri[i].Numar = i + 1;
                PhHelper.RelayoutPanel(_pnlMembri, _membri.Cast<Control>().ToList());
            };
            _membri.Add(ctrl);
            _pnlMembri.Controls.Add(ctrl);
            PhHelper.RelayoutPanel(_pnlMembri, _membri.Cast<Control>().ToList());
        }

        protected override bool ValidateFormForPreview()
        {
            if (!RequireText(_txtCodInregistrare, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCodInregistrare)) return false;
            return true;
        }

        protected override bool ValidateForm()
        {
            if (!RequireText(_txtCodInregistrare, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCodInregistrare)) return false;
            if (!RequireText(_txtNrDecizieComisie, "Nr. decizie comisie")) return false;
            if (!RequireText(_txtOraConvocare, "Ora cercetării")) return false;
            return true;
        }

        protected override void PopulateModel()
        {
            FillAngajator(_m);
            _m.CodInregistrare = GetText(_txtCodInregistrare);
            _m.DataConvocare = GetDate(_dtpDataConvocare);
            _m.NumeIntocmitorHr = GetText(_txtNumeIntocmitorHr);
            _m.CodCor = GetText(_txtCodCor);
            _m.DataNotaExplicativa = GetDate(_dtpDataNotaExplicativa);
            _m.DescriereAbatere = GetText(_txtDescriereAbatere);
            _m.IntervalAniCCM = GetText(_txtIntervalAniCCM);
            _m.CodInregistrareITM = GetText(_txtCodInregistrareITM);
            _m.LocCercetare = GetText(_txtLocCercetare);
            _m.DataCercetare = GetDate(_dtpDataCercetare);
            _m.OraConvocare = GetText(_txtOraConvocare);
            _m.NrDecizieComisie = GetText(_txtNrDecizieComisie);
            _m.DataDecizieComisie = GetDate(_dtpDataDecizieComisie);
            _m.NumeIntocmitorHr = GetText(_txtNumeIntocmitorHr);

            _m.Referate.Clear();
            foreach (var r in _referate) { var item = r.GetItem(); if (item != null) _m.Referate.Add(item); }

            _m.Membri.Clear();
            foreach (var mb in _membri) { var item = mb.GetItem(); if (item != null) _m.Membri.Add(item); }
        }

        protected override string GetTemplatePath()
            => PluginConfig.GetTemplatePath(TipDocument.ConvocareCercetare);

        protected override DateTime GetRegistraturaDate()
            => _dtpDataConvocare != null ? _dtpDataConvocare.Value.Date : base.GetRegistraturaDate();
    }

    // ══════════════════════════════════════════════════════════
    //  PROCES VERBAL CERCETARE DISCIPLINARA
    // ══════════════════════════════════════════════════════════
    public class ProcesVerbalCercetareForm : DocumentFormBase
    {
        private readonly ProcesVerbalCercetareModel _m;

        private TextBox _txtCodInregistrare;
        private DateTimePicker _dtpDataCercetare;
        private TextBox _txtNrDecizieComisie;
        private DateTimePicker _dtpDataDecizieComisie;
        private TextBox _txtLocCercetare;
        private DateTimePicker _dtpDataNotaExplicativa;
        private TextBox _txtDescriereAbatere;
        private Panel _pnlMembri;
        private readonly List<MembruComisieControl> _membri = new List<MembruComisieControl>();
        private TextBox _txtConcluziiComisie;
        private TextBox _txtSanctiuneaPropusa;

        public ProcesVerbalCercetareForm(ProcesVerbalCercetareModel model)
            : base(model, "Proces verbal cercetare disciplinară prealabilă")
        {
            _m = model;
            BuildBody();
            Shown += (s, e) =>
            {
                if (_pnlMembri != null)
                {
                    _pnlMembri.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                    PhHelper.RelayoutPanel(_pnlMembri, _membri.Cast<Control>().ToList());
                }
            };
        }

        private void BuildBody()
        {
            int y = 0;

            // ── 1. DATE PROCES VERBAL ──────────────────────────
            var pnlDoc = AddSectiune("DATE PROCES VERBAL", ref y, 78);
            _txtCodInregistrare = MakeReadonly();
            CodInregistrareField = _txtCodInregistrare;
            if (!string.IsNullOrEmpty(_model.CodInregistrare))
                _txtCodInregistrare.Text = _model.CodInregistrare;
            _dtpDataCercetare = MakeDtp();
            var tbl1 = AddRow(pnlDoc, new[] { 40, 60 });
            AddLabeledInput(tbl1, 0, "Cod înregistrare", _txtCodInregistrare, required: true);
            AddLabeledInput(tbl1, 1, "Data cercetării", _dtpDataCercetare);

            // ── 2. DECIZIA DE CONSTITUIRE A COMISIEI ──────────
            var pnlDec = AddSectiune("DECIZIA DE CONSTITUIRE A COMISIEI", ref y, 78);
            _txtNrDecizieComisie = MakeInput("ex. 135");
            _dtpDataDecizieComisie = MakeDtp();
            var tbl2 = AddRow(pnlDec, new[] { 40, 60 });
            AddLabeledInput(tbl2, 0, "Nr. decizie comisie", _txtNrDecizieComisie, required: true);
            AddLabeledInput(tbl2, 1, "Data deciziei comisie", _dtpDataDecizieComisie);

            // ── 3. LOCUL DESFĂȘURĂRII ──────────────────────────
            var pnlLoc = AddSectiune("LOCUL DESFĂȘURĂRII", ref y, 62);
            _txtLocCercetare = MakeInput();
            _txtLocCercetare.Text = _m.LocCercetare;
            var tbl3 = AddRow(pnlLoc, new[] { 100 });
            AddLabeledInput(tbl3, 0, "Locul cercetării", _txtLocCercetare);

            // ── 4. COMPONENȚA COMISIEI DE DISCIPLINĂ ──────────
            _pnlMembri = AddDynamicListSection("COMPONENȚA COMISIEI DE DISCIPLINĂ", "Adaugă membru",
                () => AddMembru(), ref y);

            // ── 5. NOTA EXPLICATIVĂ ȘI DESCRIEREA ABATERII ────
            var pnlNota = AddSectiune("NOTA EXPLICATIVĂ ȘI DESCRIEREA ABATERII", ref y, 182);
            _dtpDataNotaExplicativa = MakeDtp();
            var tblNota = AddRow(pnlNota, new[] { 50, 50 });
            AddLabeledInput(tblNota, 0, "Data notei explicative", _dtpDataNotaExplicativa);
            var lblDescriereAbatere = new Label
            {
                Text = "Descriere abatere",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(55, 75, 105),
                AutoSize = true
            };
            pnlNota.Controls.Add(lblDescriereAbatere);
            _txtDescriereAbatere = MakeMultiline(110);
            _txtDescriereAbatere.Width = pnlNota.ClientSize.Width - pnlNota.Padding.Horizontal;
            pnlNota.Controls.Add(_txtDescriereAbatere);
            pnlNota.Resize += (s, e) =>
                _txtDescriereAbatere.Width = pnlNota.ClientSize.Width - pnlNota.Padding.Horizontal;

            // ── 6. CONCLUZIILE COMISIEI ────────────────────────
            var pnlConcluzii = AddSectiune("CONCLUZIILE COMISIEI", ref y, 134);
            _txtConcluziiComisie = MakeMultiline(110);
            _txtConcluziiComisie.Width = pnlConcluzii.ClientSize.Width - pnlConcluzii.Padding.Horizontal;
            pnlConcluzii.Controls.Add(_txtConcluziiComisie);
            pnlConcluzii.Resize += (s, e) =>
                _txtConcluziiComisie.Width = pnlConcluzii.ClientSize.Width - pnlConcluzii.Padding.Horizontal;

            // ── 7. SANCȚIUNEA PROPUSĂ ──────────────────────────
            var pnlSanct = AddSectiune("SANCȚIUNEA PROPUSĂ", ref y, 134);
            _txtSanctiuneaPropusa = MakeMultiline(110);
            _txtSanctiuneaPropusa.Width = pnlSanct.ClientSize.Width - pnlSanct.Padding.Horizontal;
            pnlSanct.Controls.Add(_txtSanctiuneaPropusa);
            pnlSanct.Resize += (s, e) =>
                _txtSanctiuneaPropusa.Width = pnlSanct.ClientSize.Width - pnlSanct.Padding.Horizontal;

            // ── 8. MENȚIUNI ────────────────────────────────────
            AddMentiuniSection(ref y);

            AddMembru();
            AddMembru();
            AddMembru();
        }

        private void AddMembru()
        {
            var ctrl = new MembruComisieControl(_membri.Count + 1);
            ctrl.Width = Math.Max(_pnlMembri.Width - 2, 400);
            ctrl.OnDelete = () =>
            {
                _membri.Remove(ctrl);
                _pnlMembri.Controls.Remove(ctrl);
                for (int i = 0; i < _membri.Count; i++) _membri[i].Numar = i + 1;
                PhHelper.RelayoutPanel(_pnlMembri, _membri.Cast<Control>().ToList());
            };
            _membri.Add(ctrl);
            _pnlMembri.Controls.Add(ctrl);
            PhHelper.RelayoutPanel(_pnlMembri, _membri.Cast<Control>().ToList());
        }

        protected override bool ValidateFormForPreview()
        {
            if (!RequireText(_txtCodInregistrare, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCodInregistrare)) return false;
            return true;
        }

        protected override bool ValidateForm()
        {
            if (!RequireText(_txtCodInregistrare, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCodInregistrare)) return false;
            if (!RequireText(_txtNrDecizieComisie, "Nr. decizie comisie")) return false;
            if (_membri.Count == 0 || !_membri[0].IsValid())
            {
                MessageBox.Show("Adaugă cel puțin un membru al comisiei.", "Validare",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        protected override void PopulateModel()
        {
            FillAngajator(_m);
            _m.CodInregistrare = GetText(_txtCodInregistrare);
            _m.DataCercetare = GetDate(_dtpDataCercetare);
            _m.NrDecizieComisie = GetText(_txtNrDecizieComisie);
            _m.DataDecizieComisie = GetDate(_dtpDataDecizieComisie).ToString("dd.MM.yyyy");
            _m.LocCercetare = GetText(_txtLocCercetare);
            _m.DataNotaExplicativa = GetDate(_dtpDataNotaExplicativa).ToString("dd.MM.yyyy");
            _m.DescriereAbatere = GetText(_txtDescriereAbatere);
            _m.ConcluziiComisie = GetText(_txtConcluziiComisie);
            _m.SanctiuneaPropusa = GetText(_txtSanctiuneaPropusa);

            _m.Membri.Clear();
            foreach (var mb in _membri)
            {
                var item = mb.GetItem();
                if (item != null) _m.Membri.Add(item);
            }
        }

        protected override string GetTemplatePath()
            => PluginConfig.GetTemplatePath(TipDocument.ProcesVerbalCercetare);

        protected override DateTime GetRegistraturaDate()
            => _dtpDataCercetare != null ? _dtpDataCercetare.Value.Date : base.GetRegistraturaDate();
    }
}