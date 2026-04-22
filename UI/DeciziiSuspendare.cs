using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    // ══════════════════════════════════════════════════════════
    //  SUSPENDARE — crestere copil
    // ══════════════════════════════════════════════════════════
    public class SuspendareCresterecopilForm : DecizieCuCerereBase
    {
        private readonly SuspendareCresterecopilModel _m;
        private DateTimePicker _dtpStart;
        private TextBox _txtPerioad, _txtNumeCopil, _txtSerie, _txtNrCert;

        public SuspendareCresterecopilForm(SuspendareCresterecopilModel model)
            : base(model, "Suspendare CIM — creștere copil") { _m = model; Build(); }

        private void Build()
        {
            int y = 0;

            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 66);
            AddRandDecizie(pnlDec, 8);

            var pnlCer = AddSectiune("DATE CERERE & SUSPENDARE", ref y, 118);
            AddRandCerere(pnlCer, 8);
            _dtpStart = MakeDtp();
            _txtPerioad = MakeInput("ex. 2 ani");
            var tbl2 = AddRow(pnlCer, 56, new[] { 50, 50 });
            AddLabeledInput(tbl2, 0, "Data start suspendare", _dtpStart);
            AddLabeledInput(tbl2, 1, "Perioada suspendare", _txtPerioad);

            var pnlCopil = AddSectiune("DATE COPIL", ref y, 66);
            _txtNumeCopil = MakeInput("Numele copilului");
            _txtSerie = MakeInput("ex. ISN");
            _txtNrCert = MakeInput("ex. 2510724");
            var tblC = AddRow(pnlCopil, 8, new[] { 50, 20, 30 });
            AddLabeledInput(tblC, 0, "Numele copilului", _txtNumeCopil);
            AddLabeledInput(tblC, 1, "Seria cert.", _txtSerie);
            AddLabeledInput(tblC, 2, "Nr. certificat", _txtNrCert);
        }

        protected override bool ValidateForm() => ValidateCerere();
        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.SuspendareCresterecopil);
        protected override void PopulateModel()
        {
            PopulateCerere(_m);
            _m.DataStartSuspendare = GetDate(_dtpStart);
            _m.PerioadaSuspendare = GetText(_txtPerioad);
            _m.NumeCopil = GetText(_txtNumeCopil);
            _m.SerieCertificat = GetText(_txtSerie);
            _m.NrCertificat = GetText(_txtNrCert);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SUSPENDARE — crestere copil cu handicap
    // ══════════════════════════════════════════════════════════
    public class SuspendareCresterecopilHandicapForm : DecizieCuCerereBase
    {
        private readonly SuspendareCresterecopilHandicapModel _m;
        private DateTimePicker _dtpStart, _dtpEnd, _dtpCertHand;
        private TextBox _txtPerioad, _txtNumeCopil, _txtSerie, _txtNrCert;
        private TextBox _txtGrad, _txtNrCertHand;

        public SuspendareCresterecopilHandicapForm(SuspendareCresterecopilHandicapModel model)
            : base(model, "Suspendare CIM — creștere copil cu handicap") { _m = model; Build(); }

        private void Build()
        {
            int y = 0;

            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 66);
            AddRandDecizie(pnlDec, 8);

            var pnlCer = AddSectiune("DATE CERERE & SUSPENDARE", ref y, 118);
            AddRandCerere(pnlCer, 8);
            _dtpStart = MakeDtp();
            _dtpEnd = MakeDtp();
            _txtPerioad = MakeInput("ex. 1 an");
            var tbl2 = AddRow(pnlCer, 56, new[] { 33, 33, 34 });
            AddLabeledInput(tbl2, 0, "Data start", _dtpStart);
            AddLabeledInput(tbl2, 1, "Data end", _dtpEnd);
            AddLabeledInput(tbl2, 2, "Perioada", _txtPerioad);

            var pnlCopil = AddSectiune("DATE COPIL", ref y, 66);
            _txtNumeCopil = MakeInput("Numele copilului");
            _txtSerie = MakeInput("ex. N.14");
            _txtNrCert = MakeInput("ex. 219574");
            var tblC = AddRow(pnlCopil, 8, new[] { 50, 20, 30 });
            AddLabeledInput(tblC, 0, "Numele copilului", _txtNumeCopil);
            AddLabeledInput(tblC, 1, "Seria cert.", _txtSerie);
            AddLabeledInput(tblC, 2, "Nr. certificat", _txtNrCert);

            var pnlHand = AddSectiune("CERTIFICAT HANDICAP", ref y, 66);
            _txtGrad = MakeInput("ex. mediu");
            _txtNrCertHand = MakeInput("ex. 1517");
            _dtpCertHand = MakeDtp();
            var tblH = AddRow(pnlHand, 8, new[] { 30, 30, 40 });
            AddLabeledInput(tblH, 0, "Grad handicap", _txtGrad);
            AddLabeledInput(tblH, 1, "Nr. certificat", _txtNrCertHand);
            AddLabeledInput(tblH, 2, "Data certificat", _dtpCertHand);
        }

        protected override bool ValidateForm() => ValidateCerere();
        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.SuspendareCresterecopilHandicap);
        protected override void PopulateModel()
        {
            PopulateCerere(_m);
            _m.DataStartSuspendare = GetDate(_dtpStart);
            _m.DataEndSuspendare = GetDate(_dtpEnd);
            _m.PerioadaSuspendare = GetText(_txtPerioad);
            _m.NumeCopil = GetText(_txtNumeCopil);
            _m.SerieCertificat = GetText(_txtSerie);
            _m.NrCertificat = GetText(_txtNrCert);
            _m.GradHandicap = GetText(_txtGrad);
            _m.NrCertificatHandicap = GetText(_txtNrCertHand);
            _m.DataCertificatHandicap = GetDate(_dtpCertHand);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SUSPENDARE — absente nemotivate
    // ══════════════════════════════════════════════════════════
    public class SuspendareAbsenteForm : DecizieFormBase
    {
        private readonly SuspendareAbsenteNemotivateModel _m;
        private TextBox _txtNrRef, _txtIntocmit;
        private DateTimePicker _dtpDataRef, _dtpStart, _dtpEnd, _dtpIncetare;
        private CheckBox _chkIncludeIncetare;
        private Panel _pnlIncetareWrapper;

        public SuspendareAbsenteForm(SuspendareAbsenteNemotivateModel model)
            : base(model, "Suspendare CIM — absențe nemotivate")
        {
            _m = model;
            Build();
            // Prepopulam cu numele userului logat daca e disponibil
            if (!string.IsNullOrWhiteSpace(model.IntocmitDe))
            {
                _txtIntocmit.Text = model.IntocmitDe;
                _txtIntocmit.ForeColor = System.Drawing.Color.FromArgb(25, 35, 55);
                _txtIntocmit.Font = FInput;
            }
        }

        private void Build()
        {
            int y = 0;

            // DATE DECIZIE
            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 66);
            AddRandDecizie(pnlDec, 8);

            // DATE REFERAT
            var pnlRef = AddSectiune("DATE REFERAT", ref y, 118);
            _txtNrRef = MakeInput("ex. 182");
            _dtpDataRef = MakeDtp();
            _txtIntocmit = MakeInput("ex. Inginer Muraru Mirela");
            var tbl1 = AddRow(pnlRef, 8, new[] { 25, 35, 40 });
            AddLabeledInput(tbl1, 0, "Nr. referat", _txtNrRef, required: true);
            AddLabeledInput(tbl1, 1, "Data referat", _dtpDataRef);
            AddLabeledInput(tbl1, 2, "Întocmit de", _txtIntocmit);

            _dtpStart = MakeDtp();
            _dtpEnd = MakeDtp();
            var tbl2 = AddRow(pnlRef, 8, new[] { 50, 50 });
            AddLabeledInput(tbl2, 0, "Data start suspendare", _dtpStart);
            AddLabeledInput(tbl2, 1, "Data end suspendare", _dtpEnd);

            // CHECKBOX
            var pnlChk = new Panel
            {
                Left = 0,
                Top = y,
                Height = 34,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlChk.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);
            PnlBody.Controls.Add(pnlChk);
            PnlBody.Resize += (s, e) =>
                pnlChk.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
            y += 40;

            _chkIncludeIncetare = new CheckBox
            {
                Text = "Include încetare suspendare (Art.2)",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(25, 35, 55),
                Checked = true,
                AutoSize = true,
                Location = new Point(2, 7),
                Cursor = Cursors.Hand
            };
            pnlChk.Controls.Add(_chkIncludeIncetare);

            // SECTIUNE INCETARE
            var pnlIncLabel = new Label
            {
                Text = "DATA ÎNCETARE SUSPENDARE",
                Font = FSectiune,
                ForeColor = Albastru,
                AutoSize = true,
                Left = 0,
                Top = y
            };
            PnlBody.Controls.Add(pnlIncLabel);
            y += 22;

            var pnlIncInner = new Panel
            {
                Left = 0,
                Top = y,
                Height = 66,
                BackColor = Color.White,
                Padding = new Padding(14, 8, 14, 8),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlIncInner.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(Color.FromArgb(200, 215, 235)))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlIncInner.Width - 1, pnlIncInner.Height - 1);
            };
            pnlIncInner.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);
            PnlBody.Controls.Add(pnlIncInner);
            PnlBody.Resize += (s, e2) =>
                pnlIncInner.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;

            _dtpIncetare = MakeDtp();
            var tbl3 = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 2,
                Height = 54,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0),
                Width = 500
            };
            tbl3.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tbl3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tbl3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlIncInner.Controls.Add(tbl3);
            pnlIncInner.Resize += (s, e2) =>
                tbl3.Width = pnlIncInner.ClientSize.Width - pnlIncInner.Padding.Horizontal;
            AddLabeledInput(tbl3, 0, "Data încetare suspendare", _dtpIncetare);

            // Wrapper pentru toggle vizibilitate
            _pnlIncetareWrapper = pnlIncInner;

            // Toggle la schimbare checkbox
            _chkIncludeIncetare.CheckedChanged += (s, e) =>
            {
                pnlIncLabel.Visible = _chkIncludeIncetare.Checked;
                _pnlIncetareWrapper.Visible = _chkIncludeIncetare.Checked;
            };
        }

        protected override bool ValidateForm() => ValidateDecizie();

        protected override string GetTemplatePath()
        {
            if (_chkIncludeIncetare.Checked)
                return PluginConfig.GetTemplatePath(TipDocument.SuspendareAbsenteNemotivate);

            return System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(
                    PluginConfig.GetTemplatePath(TipDocument.SuspendareAbsenteNemotivate)),
                "template_suspendare_absente_nemotivate_fara_incetare.docx");
        }

        protected override void PopulateModel()
        {
            PopulateDecizie(_m);
            _m.NrReferat = GetText(_txtNrRef);
            _m.DataReferat = GetDate(_dtpDataRef);
            _m.IntocmitDe = GetText(_txtIntocmit);
            _m.DataStartSuspendare = GetDate(_dtpStart);
            _m.DataEndSuspendare = GetDate(_dtpEnd);
            _m.IncludeIncetare = _chkIncludeIncetare.Checked;
            _m.DataIncetareSuspendare = _chkIncludeIncetare.Checked
                ? GetDate(_dtpIncetare)
                : DateTime.MinValue;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SUSPENDARE — acordul partilor
    // ══════════════════════════════════════════════════════════
    public class SuspendareAcordPartiForm : DecizieCuCerereBase
    {
        private readonly SuspendareAcordPartiModel _m;
        private DateTimePicker _dtpStart, _dtpEnd;

        public SuspendareAcordPartiForm(SuspendareAcordPartiModel model)
            : base(model, "Suspendare CIM — acordul părților") { _m = model; Build(); }

        private void Build()
        {
            int y = 0;
            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 66);
            AddRandDecizie(pnlDec, 8);

            var pnlCer = AddSectiune("DATE CERERE & SUSPENDARE", ref y, 118);
            AddRandCerere(pnlCer, 8);
            _dtpStart = MakeDtp();
            _dtpEnd = MakeDtp();
            var tbl2 = AddRow(pnlCer, 56, new[] { 50, 50 });
            AddLabeledInput(tbl2, 0, "Data start suspendare", _dtpStart);
            AddLabeledInput(tbl2, 1, "Data end suspendare", _dtpEnd);
        }

        protected override bool ValidateForm() => ValidateCerere();
        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.SuspendareAcordParti);
        protected override void PopulateModel()
        {
            PopulateCerere(_m);
            _m.DataStartSuspendare = GetDate(_dtpStart);
            _m.DataEndSuspendare = GetDate(_dtpEnd);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SUSPENDARE + INCETARE SUSPENDARE
    // ══════════════════════════════════════════════════════════
    public class SuspendareSiIncetareSuspendareForm : DecizieCuCerereBase
    {
        private readonly SuspendareSiIncetareSuspendareModel _m;
        private DateTimePicker _dtpStart, _dtpEnd, _dtpIncetare;

        public SuspendareSiIncetareSuspendareForm(SuspendareSiIncetareSuspendareModel model)
            : base(model, "Suspendare + Încetare suspendare CIM") { _m = model; Build(); }

        private void Build()
        {
            int y = 0;
            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 66);
            AddRandDecizie(pnlDec, 8);

            var pnlCer = AddSectiune("DATE CERERE & SUSPENDARE", ref y, 118);
            AddRandCerere(pnlCer, 8);
            _dtpStart = MakeDtp();
            _dtpEnd = MakeDtp();
            _dtpIncetare = MakeDtp();
            var tbl2 = AddRow(pnlCer, 56, new[] { 33, 33, 34 });
            AddLabeledInput(tbl2, 0, "Data start suspendare", _dtpStart);
            AddLabeledInput(tbl2, 1, "Data end suspendare", _dtpEnd);
            AddLabeledInput(tbl2, 2, "Data încetare suspendare", _dtpIncetare);
        }

        protected override bool ValidateForm() => ValidateCerere();
        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.SuspendareSiIncetareSuspendare);
        protected override void PopulateModel()
        {
            PopulateCerere(_m);
            _m.DataStartSuspendare = GetDate(_dtpStart);
            _m.DataEndSuspendare = GetDate(_dtpEnd);
            _m.DataIncetareSuspendare = GetDate(_dtpIncetare);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  INCETARE SUSPENDARE
    // ══════════════════════════════════════════════════════════
    public class IncetareSuspendareForm : DecizieCuCerereBase
    {
        private readonly IncetareSuspendareModel _m;
        private DateTimePicker _dtpIncetare;

        public IncetareSuspendareForm(IncetareSuspendareModel model)
            : base(model, "Încetare suspendare CIM") { _m = model; Build(); }

        private void Build()
        {
            int y = 0;
            var pnlDec = AddSectiune("DATE DECIZIE", ref y, 66);
            AddRandDecizie(pnlDec, 8);

            var pnlCer = AddSectiune("DATE CERERE & ÎNCETARE", ref y, 118);
            AddRandCerere(pnlCer, 8);
            _dtpIncetare = MakeDtp();
            var tbl2 = AddRow(pnlCer, 56, new[] { 50, 50 });
            AddLabeledInput(tbl2, 0, "Data încetare suspendare", _dtpIncetare);
        }

        protected override bool ValidateForm() => ValidateCerere();
        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipDocument.IncetareSuspendare);
        protected override void PopulateModel()
        {
            PopulateCerere(_m);
            _m.DataIncetareSuspendare = GetDate(_dtpIncetare);
        }
    }
}