using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public class AvertismentDisciplinarForm : DocumentFormBase
    {
        private readonly AvertismentDisciplinarModel _m;

        private TextBox _txtCodInregistrare;
        private DateTimePicker _dtpDataDecizie;

        private TextBox _txtNrReferat;
        private DateTimePicker _dtpDataReferat;

        private TextBox _txtNumeAutor;
        private TextBox _txtFunctieAutor;

        private ComboBox _cmbLocMunca;

        private TextBox _txtDescriereAbateriDetaliat;
        private TextBox _txtDescriereAbateri;
        private DateTimePicker _dtpDataComunicare;

        public AvertismentDisciplinarForm(AvertismentDisciplinarModel model)
            : base(model, "Decizie sanctionare disciplinara — Avertisment")
        {
            _m = model;
            BuildBody();
        }

        private void BuildBody()
        {
            int y = 0;

            // ── DATE DECIZIE ──────────────────────────────────
            var pnlDecizie = AddSectiune("DATE DECIZIE", ref y, 78);
            _txtCodInregistrare = MakeReadonly();
            CodInregistrareField = _txtCodInregistrare;
            if (!string.IsNullOrEmpty(_model.CodInregistrare))
                _txtCodInregistrare.Text = _model.CodInregistrare;
            _dtpDataDecizie = MakeDtp();

            var tblDecizie = AddRow(pnlDecizie, new[] { 40, 60 });
            AddLabeledInput(tblDecizie, 0, "Cod înregistrare", _txtCodInregistrare, required: true);
            AddLabeledInput(tblDecizie, 1, "Data deciziei", _dtpDataDecizie);

            // ── REFERAT SURSA ─────────────────────────────────
            var pnlReferat = AddSectiune("REFERAT SURSĂ", ref y, 78);
            _txtNrReferat = MakeInput("ex. 345");
            _dtpDataReferat = MakeDtp();

            var tblReferat = AddRow(pnlReferat, new[] { 50, 50 });
            AddLabeledInput(tblReferat, 0, "Nr. referat", _txtNrReferat, required: true);
            AddLabeledInput(tblReferat, 1, "Data referat", _dtpDataReferat);

            // ── AUTOR REFERAT ─────────────────────────────────
            var pnlAutor = AddSectiune("AUTOR REFERAT", ref y, 78);
            _txtNumeAutor = MakeInput("Nume și prenume autor");
            _txtFunctieAutor = MakeInput("Funcția autorului");

            var tblAutor = AddRow(pnlAutor, new[] { 50, 50 });
            AddLabeledInput(tblAutor, 0, "Nume autor", _txtNumeAutor, required: true);
            AddLabeledInput(tblAutor, 1, "Funcție autor", _txtFunctieAutor, required: true);

            // ── LOC MUNCA ─────────────────────────────────────
            var pnlLoc = AddSectiune("LOCUL DE MUNCĂ", ref y, 62);
            _cmbLocMunca = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 28,
                Font = new Font("Segoe UI", 10f),
                Width = pnlLoc.ClientSize.Width - pnlLoc.Padding.Horizontal
            };
            pnlLoc.Controls.Add(new Label { Text = "Punct de lucru / magazin", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true });
            pnlLoc.Controls.Add(_cmbLocMunca);
            pnlLoc.Resize += (s, e) => _cmbLocMunca.Width = pnlLoc.ClientSize.Width - pnlLoc.Padding.Horizontal;
            LoadWorkAreas();

            // ── DESCRIERE ABATERI ─────────────────────────────
            var pnlDescr = AddSectiune("DESCRIEREA ABATERII", ref y, 224);

            var lblDetaliat = new Label { Text = "Descriere detaliată (intro)", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true };
            _txtDescriereAbateriDetaliat = MakeMultiline(88);
            _txtDescriereAbateriDetaliat.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;

            var lblScurt = new Label { Text = "Descriere scurtă (Art. 2)", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true };
            _txtDescriereAbateri = MakeMultiline(88);
            _txtDescriereAbateri.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;

            pnlDescr.Controls.AddRange(new Control[] { lblDetaliat, _txtDescriereAbateriDetaliat, lblScurt, _txtDescriereAbateri });
            pnlDescr.Resize += (s, e) =>
            {
                _txtDescriereAbateriDetaliat.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;
                _txtDescriereAbateri.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;
            };

            // ── DATA COMUNICARE ───────────────────────────────
            var pnlComun = AddSectiune("DATA COMUNICĂRII", ref y, 62);
            _dtpDataComunicare = MakeDtp();
            var tblComun = AddRow(pnlComun, new[] { 50, 50 });
            AddLabeledInput(tblComun, 0, "Data comunicării deciziei", _dtpDataComunicare);

            // ── MENȚIUNI ──────────────────────────────────────
            AddMentiuniSection(ref y);
        }

        private void LoadWorkAreas()
        {
            _cmbLocMunca.Items.Clear();
            _cmbLocMunca.Items.Add("-- Selectează --");
            _cmbLocMunca.SelectedIndex = 0;
            try
            {
                var xs = BulkContext.XSupport;
                if (xs == null) return;
                int companyId = xs.ConnectionInfo.CompanyId;
                string sql = string.Format(
                    "SELECT DISTINCT NAME, ADDRESS FROM WORKAREA WHERE COMPANY = {0} AND ISACTIVE = 1 ORDER BY NAME",
                    companyId);
                var ds = xs.GetSQLDataSet(sql);
                if (ds == null) return;
                for (int i = 0; i < ds.Count; i++)
                {
                    string name = (ds[i, "NAME"] ?? string.Empty).ToString().Trim();
                    string addr = (ds[i, "ADDRESS"] ?? string.Empty).ToString().Trim();
                    string display = string.IsNullOrEmpty(addr) ? name : name + " - " + addr;
                    if (!string.IsNullOrEmpty(display))
                        _cmbLocMunca.Items.Add(display);
                }
            }
            catch { }
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
            if (!RequireText(_txtNrReferat, "Nr. referat")) return false;
            if (!RequireText(_txtNumeAutor, "Nume autor")) return false;
            if (!RequireText(_txtFunctieAutor, "Funcție autor")) return false;
            if (!RequireText(_txtDescriereAbateriDetaliat, "Descriere detaliată")) return false;
            if (!RequireText(_txtDescriereAbateri, "Descriere scurtă (Art. 2)")) return false;
            return true;
        }

        protected override void PopulateModel()
        {
            FillAngajator(_m);
            _m.CodInregistrare = GetText(_txtCodInregistrare);
            _m.DataDecizie = GetDate(_dtpDataDecizie);
            _m.NrReferat = GetText(_txtNrReferat);
            _m.DataReferat = GetDate(_dtpDataReferat);
            _m.NumeAutorReferat = GetText(_txtNumeAutor);
            _m.FunctieAutorReferat = GetText(_txtFunctieAutor);
            _m.LocMunca = _cmbLocMunca.SelectedIndex > 0
                ? _cmbLocMunca.SelectedItem.ToString() : string.Empty;
            _m.DescriereAbateriDetaliat = GetText(_txtDescriereAbateriDetaliat);
            _m.DescriereAbateri = GetText(_txtDescriereAbateri);
            _m.DataComunicare = GetDate(_dtpDataComunicare);
        }

        protected override string GetTemplatePath()
            => PluginConfig.GetTemplatePath(TipDocument.AvertismentDisciplinar);

        protected override DateTime GetRegistraturaDate()
            => _dtpDataDecizie != null ? _dtpDataDecizie.Value.Date : base.GetRegistraturaDate();
    }
}