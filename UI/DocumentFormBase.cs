using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public abstract class DocumentFormBase : FormBase
    {
        public static Action<DocumentModelBase> OnDocumentGenerated;

        protected readonly DocumentModelBase _model;
        private TextBox _txtNume, _txtCnp, _txtNrCim, _txtDataCim, _txtFunctie;

        protected DocumentFormBase(DocumentModelBase model, string titlu)
            : base(titlu, DocumentTheme.For(model.TipDocument))
        {
            _model = model;
            PopulateAngajat();
        }

        // ── Sectiunea angajat ─────────────────────────────────
        protected override Panel BuildAngajatSection()
        {
            _txtNume = MakeReadonly(); _txtCnp = MakeReadonly();
            _txtNrCim = MakeReadonly(); _txtDataCim = MakeReadonly(); _txtFunctie = MakeReadonly();

            var outer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Theme.AccentPal,
                Padding = new Padding(16, 6, 16, 6)
            };
            outer.Paint += PaintBottomLine;

            var hdr = new Label { Text = "DATE ANGAJAT", Font = FSectiune, ForeColor = Theme.Accent, Dock = DockStyle.Top, Height = 20, AutoSize = false };
            outer.Controls.Add(hdr);

            var tbl2 = MakeRow(new[] { 33, 33, 34 });
            AddLabeledInput(tbl2, 0, "Nr. CIM", _txtNrCim);
            AddLabeledInput(tbl2, 1, "Data CIM", _txtDataCim);
            AddLabeledInput(tbl2, 2, "Funcție", _txtFunctie);
            outer.Controls.Add(tbl2);

            var tbl1 = MakeRow(new[] { 60, 40 });
            AddLabeledInput(tbl1, 0, "Angajat", _txtNume);
            AddLabeledInput(tbl1, 1, "CNP", _txtCnp);
            outer.Controls.Add(tbl1);

            return outer;
        }

        private void PopulateAngajat()
        {
            _txtNume.Text = _model.NumeSalariat;
            _txtCnp.Text = _model.CNP;
            _txtNrCim.Text = _model.NrCim;
            _txtDataCim.Text = _model.DataCim != DateTime.MinValue ? _model.DataCim.ToString("dd.MM.yyyy") : string.Empty;
            _txtFunctie.Text = _model.Functie;
        }

        // ── PDF ───────────────────────────────────────────────
        protected override void FillDocxTemplate(string docxPath) => TemplateEngine.FillTemplatePublic(docxPath, _model);
        protected override string DoGenerateFinalPdf(string templatePath) => TemplateEngine.GeneratePdf(_model, templatePath);
        protected override void OnAfterGenerate() => OnDocumentGenerated?.Invoke(_model);

        // ── Registratura — confirmare + INSERT inainte de generate ──
        protected override bool OnBeforeGenerate()
        {
            var svc = RegistraturaService.Instance;
            if (svc == null) return true;

            DateTime docDate = GetRegistraturaDate();
            string cod = svc.CalculateCod(docDate);
            string titlu = RegistraturaService.GetTitluDoc(_model.TipDocument);
            int tipDocPK = RegistraturaService.GetTipDocPK(_model.TipDocument);

            using (var dlg = new ConfirmareDialog(titlu, cod, docDate, Theme))
            {
                dlg.ShowDialog(this);
                if (!dlg.Confirmed) return false;
            }

            svc.Inregistreaza(cod, docDate, tipDocPK, titlu, _model.PrsnId);
            _model.CodInregistrare = cod;
            if (CodInregistrareField != null) CodInregistrareField.Text = cod;
            _codGenerat = cod;
            _titluDocGenerat = titlu;

            return true;
        }

        protected void FillAngajator(DocumentModelBase m) { } // No-op

        protected override void PopulateMentiuni()
        {
            _model.MentiuniDocument = _txtMentiuni != null ? _txtMentiuni.Text.Trim() : string.Empty;
        }
    }
}