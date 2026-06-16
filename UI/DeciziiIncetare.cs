using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    // ══════════════════════════════════════════════════════════
    //  INCETARE CIM — demisie
    // ══════════════════════════════════════════════════════════
    public class IncetareDemisieForm : DecizieCuCerereBase
    {
        private readonly IncetareDemisieModel _m;
        private DateTimePicker _dtpIncetare;
        private ComboBox _cmbArticol;

        public IncetareDemisieForm(IncetareDemisieModel model)
            : base(model, "Încetare CIM — demisie") { _m = model; Build(); }

        private void Build()
        {
            int y = 0;
            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 78);
            AddRandDecizie(pnlDec, 8);

            var pnlCer = AddSectiune("DATE CERERE SI ÎNCETARE", ref y, 132);
            AddRandCerere(pnlCer, 8);

            _dtpIncetare = MakeDtp();
            _cmbArticol = MakeCombo();
            _cmbArticol.Items.AddRange(new[]
            {
                "81 alin. 1 — demisie cu preaviz",
                "81 alin. 7 — demisie fără preaviz"
            });
            _cmbArticol.SelectedIndex = 0;

            var tbl2 = AddRow(pnlCer, 56, new[] { 35, 65 });
            AddLabeledInput(tbl2, 0, "Data încetare", _dtpIncetare);
            AddLabeledInput(tbl2, 1, "Articol demisie", _cmbArticol);

            AddMentiuniSection(ref y);
        }

        protected override bool ValidateForm() => ValidateCerere();
        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.IncetareDemisie);
        protected override void PopulateModel()
        {
            PopulateCerere(_m);
            _m.DataIncetare = GetDate(_dtpIncetare);
            string sel = _cmbArticol.SelectedItem?.ToString() ?? string.Empty;
            _m.ArticolDemisie = sel.Contains("—")
                ? sel.Substring(0, sel.IndexOf("—")).Trim()
                : sel;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  INCETARE CIM — expirare termen
    // ══════════════════════════════════════════════════════════
    public class IncetareExpirareForm : DecizieFormBase
    {
        private readonly IncetareExpirareModel _m;
        private DateTimePicker _dtpIncetare;

        public IncetareExpirareForm(IncetareExpirareModel model)
            : base(model, "Încetare CIM — expirare termen") { _m = model; Build(); }

        private void Build()
        {
            int y = 0;
            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 78);
            AddRandDecizie(pnlDec, 8);

            var pnlDate = AddSectiune("DATA ÎNCETARE", ref y, 78);
            _dtpIncetare = MakeDtp();
            var tbl = AddRow(pnlDate, 8, new[] { 40, 60 });
            AddLabeledInput(tbl, 0, "Data încetare contract", _dtpIncetare);

            AddMentiuniSection(ref y);
        }

        protected override bool ValidateForm() => ValidateDecizie();
        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.IncetareExpirare);
        protected override void PopulateModel()
        {
            PopulateDecizie(_m);
            _m.DataIncetare = GetDate(_dtpIncetare);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  INCETARE CIM — disciplinar
    // ══════════════════════════════════════════════════════════
    public class IncetareDisciplinarForm : DecizieFormBase
    {
        private readonly IncetareDisciplinarModel _m;
        private DateTimePicker _dtpIncetare, _dtpDataPV, _dtpDataPVEnd;
        private TextBox _txtPerioad, _txtNrPV, _txtLoc;
        private TextBox _txtMotiv, _txtImprej, _txtGrad, _txtConsec;
        private TextBox _txtNumeIntocmit, _txtFunctieIntocmit;

        public IncetareDisciplinarForm(IncetareDisciplinarModel model)
            : base(model, "Încetare CIM — concediere disciplinară")
        {
            _m = model;
            Build();
            // Prepopulam cu numele userului logat daca e disponibil
            if (!string.IsNullOrWhiteSpace(model.NumeIntocmit))
            {
                _txtNumeIntocmit.Text = model.NumeIntocmit;
                _txtNumeIntocmit.ForeColor = System.Drawing.Color.FromArgb(25, 35, 55);
                _txtNumeIntocmit.Font = FInput;
            }
        }

        private void Build()
        {
            int y = 0;

            // DATE DECIZIE
            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 78);
            AddRandDecizie(pnlDec, 8);

            // CERCETARE
            var pnlCerc = AddSectiune("CERCETARE DISCIPLINARĂ", ref y, 132);
            _dtpDataPV = MakeDtp();
            _dtpDataPVEnd = MakeDtp();
            _txtNrPV = MakeInput("ex. 298");
            _txtLoc = MakeInput("ex. Biroul administrativ din Catamarasti Deal");
            _dtpIncetare = MakeDtp();

            var tbl1 = AddRow(pnlCerc, 8, new[] { 33, 33, 34 });
            AddLabeledInput(tbl1, 0, "Data start cercetare", _dtpDataPV);
            AddLabeledInput(tbl1, 1, "Data sfarsit cercetare", _dtpDataPVEnd);
            AddLabeledInput(tbl1, 2, "Nr. proces verbal", _txtNrPV, required: true);

            var tbl2 = AddRow(pnlCerc, 8, new[] { 65, 35 });
            AddLabeledInput(tbl2, 0, "Loc cercetare", _txtLoc);
            AddLabeledInput(tbl2, 1, "Data încetare CIM", _dtpIncetare, required: true);

            // MOTIVE — Panel scrollabil separat, nu FlowLayoutPanel
            var lblMotiv = new Label
            {
                Text = "MOTIVE ȘI CIRCUMSTANȚE",
                Font = FSectiune,
                ForeColor = ButtonPalettes.Primary.Foreground,
                AutoSize = true,
                Left = 0,
                Top = y
            };
            PnlBody.Controls.Add(lblMotiv);
            y += 22;

            var pnlMotiv = new Panel
            {
                Left = 0,
                Top = y,
                Height = 380,
                BackColor = Color.White,
                Padding = new Padding(14, 8, 14, 8),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlMotiv.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(200, 215, 235)))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlMotiv.Width - 1, pnlMotiv.Height - 1);
            };
            PnlBody.Controls.Add(pnlMotiv);
            PnlBody.Resize += (s2, e2) =>
                pnlMotiv.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
            pnlMotiv.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);
            y += 380 + 10;

            _txtMotiv = MakeMultiline(72);
            _txtImprej = MakeMultiline(72);
            _txtGrad = MakeMultiline(54);
            _txtConsec = MakeMultiline(54);

            var campuri = new[]
            {
                new { Lbl = "(1) Motivele sancționării:",  Txt = _txtMotiv  },
                new { Lbl = "(2) Împrejurările faptelor:", Txt = _txtImprej },
                new { Lbl = "(3) Gradul de vinovăție:",    Txt = _txtGrad   },
                new { Lbl = "(4) Consecințele abaterii:",  Txt = _txtConsec }
            };

            int cy = 10;
            foreach (var item in campuri)
            {
                var lbl = new Label
                {
                    Text = item.Lbl,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = TextSecundar,
                    AutoSize = true,
                    Left = 14,
                    Top = cy
                };
                pnlMotiv.Controls.Add(lbl);
                cy += 20;
                item.Txt.Left = 14;
                item.Txt.Top = cy;
                item.Txt.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                pnlMotiv.Controls.Add(item.Txt);
                cy += item.Txt.Height + 10;
            }

            // Actualizam latimea textbox-urilor la resize
            pnlMotiv.Resize += (s2, e2) =>
            {
                int w = pnlMotiv.ClientSize.Width - 28;
                foreach (Control c in pnlMotiv.Controls)
                    if (c is TextBox) c.Width = w;
            };

            // INTOCMIT
            var pnlInt = AddSectiune("ÎNTOCMIT DE", ref y, 78);
            _txtNumeIntocmit = MakeInput("ex. Marin Iulia-Alina");
            _txtFunctieIntocmit = MakeInput("ex. Specialist Resurse Umane");
            var tblI = AddRow(pnlInt, 8, new[] { 50, 50 });
            AddLabeledInput(tblI, 0, "Nume", _txtNumeIntocmit);
            AddLabeledInput(tblI, 1, "Funcție", _txtFunctieIntocmit);

            AddMentiuniSection(ref y);
        }

        protected override bool ValidateForm()
        {
            if (!ValidateDecizie()) return false;
            if (!RequireText(_txtNrPV, "Nr. proces verbal")) return false;
            return true;
        }
        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.IncetareDisciplinar);
        protected override void PopulateModel()
        {
            PopulateDecizie(_m);
            _m.DataIncetare = GetDate(_dtpIncetare);
            // Construim perioada ca string din cele doua date
            _m.PerioadaCercetare = string.Format("{0} – {1}",
                _dtpDataPV.Value.ToString("dd.MM.yyyy"),
                _dtpDataPVEnd.Value.ToString("dd.MM.yyyy"));
            _m.NrProcesVerbal = GetText(_txtNrPV);
            _m.DataProcesVerbal = GetDate(_dtpDataPVEnd);
            _m.LocCercetare = GetText(_txtLoc);
            _m.MotiveleSanctionarii = _txtMotiv.Text.Trim();
            _m.ImprejurariFapte = _txtImprej.Text.Trim();
            _m.GradVinovatie = _txtGrad.Text.Trim();
            _m.ConsecinteAbateri = _txtConsec != null ? _txtConsec.Text.Trim() : string.Empty;
            _m.NumeIntocmit = GetText(_txtNumeIntocmit);
            _m.FunctieIntocmit = GetText(_txtFunctieIntocmit);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  INCETARE CIM — perioada de proba
    // ══════════════════════════════════════════════════════════
    public class IncetarePerioadaProbaForm : DecizieFormBase
    {
        private readonly IncetarePerioadaProbaModel _m;
        private DateTimePicker _dtpIncetare, _dtpNotificare;
        private TextBox _txtNrNotificare;

        public IncetarePerioadaProbaForm(IncetarePerioadaProbaModel model)
            : base(model, "Încetare CIM — perioadă de probă") { _m = model; Build(); }

        private void Build()
        {
            int y = 0;
            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 78);
            AddRandDecizie(pnlDec, 8);

            var pnlNot = AddSectiune("DATE NOTIFICARE", ref y, 132);
            _dtpIncetare = MakeDtp();
            _dtpNotificare = MakeDtp();
            _txtNrNotificare = MakeInput("ex. 42");

            var tbl = AddRow(pnlNot, 8, new[] { 35, 30, 35 });
            AddLabeledInput(tbl, 0, "Data încetare contract", _dtpIncetare);
            AddLabeledInput(tbl, 1, "Nr. notificare", _txtNrNotificare, required: true);
            AddLabeledInput(tbl, 2, "Data notificare", _dtpNotificare);

            AddMentiuniSection(ref y);
        }

        protected override bool ValidateForm()
        {
            if (!ValidateDecizie()) return false;
            if (!RequireText(_txtNrNotificare, "Nr. notificare")) return false;
            return true;
        }

        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.IncetarePerioadaProba);

        protected override void PopulateModel()
        {
            PopulateDecizie(_m);
            _m.DataIncetare = GetDate(_dtpIncetare);
            _m.NrNotificare = GetText(_txtNrNotificare);
            _m.DataNotificare = GetDate(_dtpNotificare);
        }
    }
}