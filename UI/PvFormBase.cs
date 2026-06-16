using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public abstract class PvFormBase : FormBase
    {
        protected readonly PvModelBase _model;
        private readonly Action<PvModelBase> _onPdfGenerated;

        private TextBox _txtNume, _txtCnp, _txtFunctie;

        protected PvFormBase(PvModelBase model, string titlu, Action<PvModelBase> onPdfGenerated = null)
            : base(titlu, DocumentTheme.For(model.TipPV))
        {
            _model = model;
            _onPdfGenerated = onPdfGenerated;
            PopulateAngajat();
        }

        // ── Sectiunea angajat ─────────────────────────────────
        protected override Panel BuildAngajatSection()
        {
            _txtNume = MakeReadonly(); _txtCnp = MakeReadonly(); _txtFunctie = MakeReadonly();

            var outer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Theme.AccentPal,
                Padding = new Padding(16, 6, 16, 6)
            };
            outer.Paint += PaintBottomLine;

            var hdr = new Label { Text = "DATE ANGAJAT (PRIMITOR)", Font = FSectiune, ForeColor = Theme.Accent, Dock = DockStyle.Top, Height = 20, AutoSize = false };
            outer.Controls.Add(hdr);

            var tbl2 = MakeRow(new[] { 33, 33, 34 });
            AddLabeledInput(tbl2, 0, "Funcție", _txtFunctie);
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
            _txtFunctie.Text = _model.Functie;
        }

        // ── PDF ───────────────────────────────────────────────
        protected override void FillDocxTemplate(string docxPath) => PvTemplateEngine.FillTemplatePublic(docxPath, _model);
        protected override string DoGenerateFinalPdf(string templatePath) => PvTemplateEngine.GeneratePdf(_model, templatePath);
        protected override void OnAfterGenerate() => _onPdfGenerated?.Invoke(_model);

        // ── Registratura ──────────────────────────────────────
        protected override bool OnBeforeGenerate()
        {
            var svc = RegistraturaService.Instance;
            if (svc == null) return true;

            DateTime docDate = GetRegistraturaDate();
            string cod = svc.CalculateCod(docDate);
            string titlu = RegistraturaService.GetTitluDoc(_model.TipPV);
            int tipDocPK = RegistraturaService.GetTipDocPK(_model.TipPV);

            using (var dlg = new ConfirmareDialog(titlu, cod, docDate, Theme))
            {
                dlg.ShowDialog(this);
                if (!dlg.Confirmed) return false;
            }

            // Daca Inregistreaza arunca exceptie, generarea se opreste
            svc.Inregistreaza(cod, docDate, tipDocPK, titlu, _model.PrsnId);
            _model.CodInregistrare = cod;
            if (CodInregistrareField != null) CodInregistrareField.Text = cod;
            _codGenerat = cod;
            _titluDocGenerat = titlu;

            return true;
        }

        // ── Helpers PV ────────────────────────────────────────
        protected ComboBox MakeTipPredareCombo()
        {
            var cmb = MakeCombo();
            cmb.Items.AddRange(new object[]
            {
                "Predare-primire", "Predare-primire în exploatare", "Predare-primire mentenanță",
                "Predare-primire în custodie", "Predare-primire administrare",
                "Predare-primire recepție", "Predare-primire relocare",
                "Predare-primire casare / scoatere din uz"
            });
            cmb.SelectedIndex = 0;
            return cmb;
        }

        protected static TipPredare GetTipPredare(ComboBox cmb) => (TipPredare)cmb.SelectedIndex;
        protected void FillAngajatorFields(PvModelBase m) { } // No-op

        protected override void PopulateMentiuni()
        {
            _model.MentiuniDocument = _txtMentiuni != null ? _txtMentiuni.Text.Trim() : string.Empty;
        }
    }
}