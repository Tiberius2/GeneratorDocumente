using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public class ReferatDisciplinarForm : DocumentFormBase
    {
        private readonly ReferatDisciplinarModel _m;

        private TextBox _txtCodInregistrare;
        private DateTimePicker _dtpDataReferat;

        private TextBox _txtNumeAutor;
        private TextBox _txtFunctieAutor;

        private ComboBox _cmbLocMunca;

        private TextBox _txtDescriereFapta;
        private TextBox _txtConsecinteAbateri;
        private TextBox _txtTemeiLegal;

        public ReferatDisciplinarForm(ReferatDisciplinarModel model)
            : base(model, "Referat disciplinar")
        {
            _m = model;
            BuildBody();
        }

        private void BuildBody()
        {
            int y = 0;

            // ── DATE DOCUMENT ─────────────────────────────────
            var pnlDoc = AddSectiune("DATE DOCUMENT", ref y, 78);
            _txtCodInregistrare = MakeReadonly();
            CodInregistrareField = _txtCodInregistrare;
            if (!string.IsNullOrEmpty(_model.CodInregistrare))
                _txtCodInregistrare.Text = _model.CodInregistrare;
            _dtpDataReferat = MakeDtp();

            var tblDoc = AddRow(pnlDoc, new[] { 35, 65 });
            AddLabeledInput(tblDoc, 0, "Cod înregistrare", _txtCodInregistrare, required: true);
            AddLabeledInput(tblDoc, 1, "Data referatului", _dtpDataReferat);

            // ── AUTOR REFERAT ─────────────────────────────────
            var pnlAutor = AddSectiune("AUTOR REFERAT", ref y, 78);
            _txtNumeAutor = MakeInput("Nume și prenume autor");
            _txtFunctieAutor = MakeInput("Funcția autorului");

            var tblAutor = AddRow(pnlAutor, new[] { 50, 50 });
            AddLabeledInput(tblAutor, 0, "Nume autor", _txtNumeAutor, required: true);
            AddLabeledInput(tblAutor, 1, "Funcție autor", _txtFunctieAutor, required: true);

            // ── LOC MUNCA — ComboBox full width ───────────────
            var pnlLoc = AddSectiune("LOCUL DE MUNCĂ", ref y, 62);
            var lblLoc = new Label
            {
                Text = "Punct de lucru / magazin",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(55, 75, 105),
                AutoSize = true
            };
            _cmbLocMunca = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 28,
                Font = new Font("Segoe UI", 10f),
                Width = pnlLoc.ClientSize.Width - pnlLoc.Padding.Horizontal
            };
            pnlLoc.Controls.Add(lblLoc);
            pnlLoc.Controls.Add(_cmbLocMunca);
            pnlLoc.Resize += (s, e) =>
                _cmbLocMunca.Width = pnlLoc.ClientSize.Width - pnlLoc.Padding.Horizontal;

            LoadWorkAreas();

            // ── DESCRIERE ABATERE ─────────────────────────────
            var pnlDescr = AddSectiune("DESCRIERE ABATERE", ref y, 224);

            var lblFapta = new Label { Text = "Descrierea faptei", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true };
            _txtDescriereFapta = MakeMultiline(88);
            _txtDescriereFapta.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;

            var lblConsec = new Label { Text = "Consecințele abaterii", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(55, 75, 105), AutoSize = true };
            _txtConsecinteAbateri = MakeMultiline(88);
            _txtConsecinteAbateri.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;

            pnlDescr.Controls.AddRange(new Control[] { lblFapta, _txtDescriereFapta, lblConsec, _txtConsecinteAbateri });
            pnlDescr.Resize += (s, e) =>
            {
                _txtDescriereFapta.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;
                _txtConsecinteAbateri.Width = pnlDescr.ClientSize.Width - pnlDescr.Padding.Horizontal;
            };

            // ── TEMEI LEGAL ───────────────────────────────────
            var pnlTemei = AddSectiune("TEMEI LEGAL", ref y, 112);
            _txtTemeiLegal = MakeMultiline(88);
            _txtTemeiLegal.Width = pnlTemei.ClientSize.Width - pnlTemei.Padding.Horizontal;
            pnlTemei.Controls.Add(_txtTemeiLegal);
            pnlTemei.Resize += (s, e) =>
                _txtTemeiLegal.Width = pnlTemei.ClientSize.Width - pnlTemei.Padding.Horizontal;

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

                int companyId = BulkContext.CompanyData != null ? xs.ConnectionInfo.CompanyId : 2000;
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
            catch { /* SQL failure — dropdown ramas gol */ }
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
            if (!RequireText(_txtNumeAutor, "Nume autor")) return false;
            if (!RequireText(_txtFunctieAutor, "Funcție autor")) return false;
            if (!RequireText(_txtDescriereFapta, "Descrierea faptei")) return false;
            return true;
        }

        protected override void PopulateModel()
        {
            FillAngajator(_m);
            _m.CodInregistrare = GetText(_txtCodInregistrare);
            _m.DataReferat = GetDate(_dtpDataReferat);
            _m.NumeAutorReferat = GetText(_txtNumeAutor);
            _m.FunctieAutorReferat = GetText(_txtFunctieAutor);

            // LocMunca — ignoram primul item "-- Selectează --"
            string loc = _cmbLocMunca.SelectedIndex > 0
                ? _cmbLocMunca.SelectedItem.ToString()
                : string.Empty;
            _m.LocMunca = loc;

            _m.DescriereFapta = GetText(_txtDescriereFapta);
            _m.ConsecinteAbateri = GetText(_txtConsecinteAbateri);
            _m.TemeiLegal = GetText(_txtTemeiLegal);
        }

        protected override string GetTemplatePath()
            => PluginConfig.GetTemplatePath(TipDocument.ReferatDisciplinar);

        protected override DateTime GetRegistraturaDate()
            => _dtpDataReferat != null ? _dtpDataReferat.Value.Date : base.GetRegistraturaDate();
    }
}