using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    /// <summary>
    /// Baza pentru toate formularele de documente (Acte + Decizii).
    /// Implementeaza sectiunea angajat cu campuri CIM si PDF via TemplateEngine.
    /// </summary>
    public abstract class DocumentFormBase : FormBase
    {
        public static Action<DocumentModelBase> OnDocumentGenerated;

        protected readonly DocumentModelBase _model;
        private TextBox _txtNume, _txtCnp, _txtNrCim, _txtDataCim, _txtFunctie;

        protected DocumentFormBase(DocumentModelBase model, string titlu) : base(titlu)
        {
            _model = model;
            PopulateAngajat();
        }

        // ── Sectiunea angajat cu campuri CIM ──────────────────
        protected override Panel BuildAngajatSection()
        {
            // Campurile sunt create inainte de a fi populate (nu acceseaza _model)
            _txtNume = MakeReadonly(); _txtCnp = MakeReadonly();
            _txtNrCim = MakeReadonly(); _txtDataCim = MakeReadonly(); _txtFunctie = MakeReadonly();

            var outer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.White,
                Padding = new Padding(16, 6, 16, 6)
            };
            outer.Paint += PaintBottomLine;

            var hdr = new Label
            {
                Text = "DATE ANGAJAT",
                Font = FSectiune,
                ForeColor = Albastru,
                Dock = DockStyle.Top,
                Height = 20,
                AutoSize = false
            };
            outer.Controls.Add(hdr);

            // Rand 2: NrCim + DataCim + Functie (adaugat primul = va fi jos)
            var tbl2 = MakeRow(new[] { 33, 33, 34 });
            AddLabeledInput(tbl2, 0, "Nr. CIM", _txtNrCim);
            AddLabeledInput(tbl2, 1, "Data CIM", _txtDataCim);
            AddLabeledInput(tbl2, 2, "Funcție", _txtFunctie);
            outer.Controls.Add(tbl2);

            // Rand 1: Angajat + CNP (adaugat al doilea = va fi sus)
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
            _txtDataCim.Text = _model.DataCim != DateTime.MinValue
                ? _model.DataCim.ToString("dd.MM.yyyy") : string.Empty;
            _txtFunctie.Text = _model.Functie;
        }

        // ── PDF via TemplateEngine ─────────────────────────────
        protected override void FillDocxTemplate(string docxPath)
            => TemplateEngine.FillTemplatePublic(docxPath, _model);

        protected override string DoGenerateFinalPdf(string templatePath)
            => TemplateEngine.GeneratePdf(_model, templatePath);

        protected override void OnAfterGenerate()
            => OnDocumentGenerated?.Invoke(_model);

        /// <summary>No-op: datele angajatorului sunt setate din ERP in PluginEntry.ApplyCompanyData.</summary>
        protected void FillAngajator(DocumentModelBase m) { }
    }
}