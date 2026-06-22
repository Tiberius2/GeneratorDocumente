using ActAditionalPlugin.Models;
using ActAditionalPlugin.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ActAditionalPlugin.UI
{
    // ══════════════════════════════════════════════════════════
    //  PV BUNURI — unifica Echipamente + Electronice
    //  Diferenta intre cele doua: titlul formularului, label-ul
    //  sectiunii de bunuri si GetTemplatePath(). Restul identic.
    // ══════════════════════════════════════════════════════════
    public class PvBunuriForm : PvFormBase
    {
        private readonly IPvBunuriModel _m;
        private readonly string _sectionLabel;

        private TextBox _txtCod;
        private DateTimePicker _dtpData;
        private ComboBox _cmbTipPredare;
        private Panel _pnlItems;
        private readonly System.Collections.Generic.List<EchipamentItemControl> _items
            = new System.Collections.Generic.List<EchipamentItemControl>();

        public PvBunuriForm(IPvBunuriModel model, string titluForm, string sectionLabel,
                            Action<PvModelBase> onPdfGenerated = null)
            : base((PvModelBase)model, titluForm, onPdfGenerated)
        {
            _m = model;
            _sectionLabel = sectionLabel;
            Build();
        }

        private void Build()
        {
            int y = 0;

            var pnlDoc = AddSectiune("DATE DOCUMENT", ref y, 78);
            _txtCod = MakeReadonly();
            CodInregistrareField = _txtCod;
            if (!string.IsNullOrEmpty(((PvModelBase)_m).CodInregistrare))
                _txtCod.Text = ((PvModelBase)_m).CodInregistrare;
            _dtpData = MakeDtp();
            _dtpData.ValueChanged += (s, e) =>
            {
                var svc = ActAditionalPlugin.Services.RegistraturaService.Instance;
                if (svc != null && CodInregistrareField != null)
                    CodInregistrareField.Text = svc.CalculateCod(_dtpData.Value.Date);
            };
            _cmbTipPredare = MakeTipPredareCombo();
            var tbl1 = AddRow(pnlDoc, new[] { 30, 30, 40 });
            AddLabeledInput(tbl1, 0, "Cod înregistrare", _txtCod, required: true);
            AddLabeledInput(tbl1, 1, "Data PV", _dtpData);
            AddLabeledInput(tbl1, 2, "Tip predare-primire", _cmbTipPredare);

            // Header sectiune bunuri cu buton adaugare
            var pnlHeader = new Panel
            {
                Left = 0,
                Top = y,
                Height = 34,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlHeader.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);
            PnlBody.Controls.Add(pnlHeader);
            PnlBody.Resize += (s, e) => pnlHeader.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;

            pnlHeader.Controls.Add(new Label
            {
                Text = _sectionLabel,
                Font = FSectiune,
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(0, 8)
            });

            var btnAdd = new Button
            {
                Text = "+ Adaugă echipament",
                Height = 28,
                Width = 190,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Top = 3,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            ButtonPalettes.Primary.ApplyTo(btnAdd);
            btnAdd.FlatAppearance.BorderSize = 3;
            btnAdd.Click += (s, e) => AddItem();

            pnlHeader.Controls.Add(btnAdd);
            pnlHeader.Resize += (s, e) => btnAdd.Left = pnlHeader.Width - btnAdd.Width;
            btnAdd.Left = pnlHeader.Width - btnAdd.Width;
            y += 42;

            _pnlItems = new Panel
            {
                Left = 0,
                Top = y,
                Height = 260,
                BackColor = Color.FromArgb(242, 245, 250),
                AutoScroll = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _pnlItems.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);
            PnlBody.Controls.Add(_pnlItems);
            PnlBody.Resize += (s, e) =>
            {
                _pnlItems.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                RelayoutItems();
            };
            y += 268;

            var pnlMent = AddSectiune("MENȚIUNI", ref y, 102);
            pnlMent.Controls.Add(new Label
            {
                Text = "Mențiuni suplimentare (opțional):",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextSecundar,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 2)
            });
            _txtMentiuni = MakeMultiline(88);
            _txtMentiuni.Width = Math.Max(pnlMent.ClientSize.Width - pnlMent.Padding.Horizontal, 300);
            pnlMent.Controls.Add(_txtMentiuni);
            pnlMent.Resize += (s, e) => _txtMentiuni.Width = pnlMent.ClientSize.Width - pnlMent.Padding.Horizontal;
        }

        private void AddItem()
        {
            var ctrl = new EchipamentItemControl(_items.Count + 1);
            ctrl.Width = Math.Max(_pnlItems.Width - 2, 400);
            ctrl.OnDelete = () => { _items.Remove(ctrl); _pnlItems.Controls.Remove(ctrl); Renumber(); RelayoutItems(); };
            _items.Add(ctrl);
            _pnlItems.Controls.Add(ctrl);
            RelayoutItems();
        }

        private void Renumber() { for (int i = 0; i < _items.Count; i++) _items[i].Numar = i + 1; }

        private void RelayoutItems()
        {
            int w = Math.Max(_pnlItems.ClientSize.Width - 2, 400);
            int y = 0;
            foreach (var c in _items) { c.Width = w; c.Left = 0; c.Top = y; y += c.Height + 6; }
        }

        protected override bool ValidateForm()
        {
            if (!RequireText(_txtCod, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCod)) return false;
            if (_items.Count == 0)
            { MessageBox.Show("Adăugați cel puțin un echipament.", "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            for (int i = 0; i < _items.Count; i++)
                if (!_items[i].IsValid())
                { MessageBox.Show(string.Format("Echipamentul {0} nu are denumire completată.", i + 1), "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            return true;
        }

        protected override bool ValidateFormForPreview()
        {
            if (!RequireText(_txtCod, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCod)) return false;
            return true;
        }

        protected override void PopulateModel()
        {
            FillAngajatorFields((PvModelBase)_m);
            _m.CodInregistrare = GetText(_txtCod);
            _m.DataPV = GetDate(_dtpData);
            _m.TipPredare = GetTipPredare(_cmbTipPredare);
            _m.Bunuri = new System.Collections.Generic.List<ActAditionalPlugin.Models.PvBunItem>();
            foreach (var item in _items) { var bun = item.GetBunItem(); if (bun != null) _m.Bunuri.Add(bun); }
        }

        protected override string GetTemplatePath() => _m.GetTemplatePath();
        protected override DateTime GetRegistraturaDate()
            => _dtpData != null ? _dtpData.Value.Date : base.GetRegistraturaDate();
    }

    // ══════════════════════════════════════════════════════════
    //  PV AUTOVEHICUL — neschimbat
    // ══════════════════════════════════════════════════════════
    public class PvAutovehiculForm : PvFormBase
    {
        private readonly PvAutovehiculModel _m;
        private TextBox _txtCod;
        private DateTimePicker _dtpData;
        private ComboBox _cmbTipPredare;

        // ── Predator 2 — Art. 2 ───────────────────────────────
        private TextBox _txtNumePredator2, _txtFunctiePredator2;
        private TextBox _txtCNPPredator2, _txtCISeriaPredator2, _txtCINrPredator2;
        private TextBox _txtDomiciliuPredator2;

        // ── Primitor extra — Art. 3 ───────────────────────────
        private TextBox _txtCISeria, _txtCINr, _txtDomiciliu;

        // ── Date vehicul ──────────────────────────────────────
        private TextBox _txtMarca, _txtNrInmatr, _txtSerieSasiu, _txtAnFab, _txtKm;
        private TextBox _txtStareFunct, _txtAvarii;
        private TextBox _txtAnvFata, _txtAnvSpate, _txtUzura;

        // ── Dotari ────────────────────────────────────────────
        private ComboBox _cmbTrusa, _cmbExtinctor, _cmbTriunghi, _cmbCric, _cmbCheie, _cmbVesta, _cmbRoata;
        private TextBox _txtUzuraRoata;

        // ── Documente ─────────────────────────────────────────
        private ComboBox _cmbCertif, _cmbRCA, _cmbRovinieta;

        public PvAutovehiculForm(PvAutovehiculModel model, Action<PvModelBase> onPdfGenerated = null)
            : base(model, "Proces Verbal — Autovehicul", onPdfGenerated) { _m = model; Build(); }

        private void Build()
        {
            int y = 0;

            var pnlDoc = AddSectiune("DATE DOCUMENT", ref y, 78);
            _txtCod = MakeReadonly();
            CodInregistrareField = _txtCod;
            if (!string.IsNullOrEmpty(_m.CodInregistrare))
                _txtCod.Text = _m.CodInregistrare;
            _dtpData = MakeDtp();
            _dtpData.ValueChanged += (s, e) =>
            {
                var svc = ActAditionalPlugin.Services.RegistraturaService.Instance;
                if (svc != null && CodInregistrareField != null)
                    CodInregistrareField.Text = svc.CalculateCod(_dtpData.Value.Date);
            };
            _cmbTipPredare = MakeTipPredareCombo();
            var tbl1 = AddRow(pnlDoc, new[] { 30, 30, 40 });
            AddLabeledInput(tbl1, 0, "Cod înregistrare", _txtCod, required: true);
            AddLabeledInput(tbl1, 1, "Data PV", _dtpData);
            AddLabeledInput(tbl1, 2, "Tip predare-primire", _cmbTipPredare);

            var pnlPred2 = AddSectiune("AL DOILEA PREDATOR (opțional) — Art. 2", ref y, 190);
            _txtNumePredator2 = MakeInput("ex. CHIRILA DUMITRU");
            _txtFunctiePredator2 = MakeInput("ex. DIRECTOR VANZARI");
            var tblP1 = AddRow(pnlPred2, new[] { 50, 50 });
            AddLabeledInput(tblP1, 0, "Nume predator 2", _txtNumePredator2);
            AddLabeledInput(tblP1, 1, "Funcție predator 2", _txtFunctiePredator2);

            _txtCNPPredator2 = MakeInput("ex. 1721016070023");
            _txtCISeriaPredator2 = MakeInput("ex. XT");
            _txtCINrPredator2 = MakeInput("ex. 766727");
            var tblP2 = AddRow(pnlPred2, new[] { 40, 15, 25, 20 });
            AddLabeledInput(tblP2, 0, "CNP predator 2", _txtCNPPredator2);
            AddLabeledInput(tblP2, 1, "CI Seria", _txtCISeriaPredator2);
            AddLabeledInput(tblP2, 2, "CI Nr.", _txtCINrPredator2);

            _txtDomiciliuPredator2 = MakeInput("ex. BOTOSANI, Str. Zambilelor, Nr. 23");
            var tblP3 = AddRow(pnlPred2, new[] { 100 });
            AddLabeledInput(tblP3, 0, "Domiciliu predator 2", _txtDomiciliuPredator2);

            var pnlPrim = AddSectiune("DATE SUPLIMENTARE PRIMITOR — Art. 3", ref y, 78);
            _txtCISeria = MakeInput("ex. XT");
            _txtCINr = MakeInput("ex. 929646");
            _txtDomiciliu = MakeInput("ex. BOTOSANI, Str. ..., Nr. ...");
            if (!string.IsNullOrWhiteSpace(_m.Domiciliu))
            {
                _txtDomiciliu.Text = _m.Domiciliu;
                _txtDomiciliu.ForeColor = TextPrincipal;
                _txtDomiciliu.Font = FInput;
            }
            var tbl3 = AddRow(pnlPrim, new[] { 15, 20, 65 });
            AddLabeledInput(tbl3, 0, "CI Seria", _txtCISeria);
            AddLabeledInput(tbl3, 1, "CI Nr.", _txtCINr);
            AddLabeledInput(tbl3, 2, "Domiciliu", _txtDomiciliu);

            var pnlVeh = AddSectiune("DATE AUTOVEHICUL", ref y, 134);
            _txtMarca = MakeInput("ex. IVECO");
            _txtNrInmatr = MakeInput("ex. BT 30 ESP");
            _txtSerieSasiu = MakeInput("ex. ZCFCC35A305232370");
            _txtAnFab = MakeInput("ex. 2018");
            _txtKm = MakeInput("ex. 239560");
            var tblV1 = AddRow(pnlVeh, new[] { 30, 25, 45 });
            AddLabeledInput(tblV1, 0, "Marcă", _txtMarca, required: true);
            AddLabeledInput(tblV1, 1, "Nr. înmatriculare", _txtNrInmatr, required: true);
            AddLabeledInput(tblV1, 2, "Serie șasiu", _txtSerieSasiu);
            var tblV2 = AddRow(pnlVeh, new[] { 20, 30, 50 });
            AddLabeledInput(tblV2, 0, "An fabricație", _txtAnFab);
            AddLabeledInput(tblV2, 1, "Kilometri", _txtKm);

            var pnlStare = AddSectiune("STARE TEHNICĂ", ref y, 134);
            _txtStareFunct = MakeInput("ex. buna");
            _txtAvarii = MakeInput("ex. -");
            _txtAnvFata = MakeInput("ex. 225/65/R16");
            _txtAnvSpate = MakeInput("ex. 225/65/R16");
            _txtUzura = MakeInput("0");
            var tblS1 = AddRow(pnlStare, new[] { 40, 60 });
            AddLabeledInput(tblS1, 0, "Stare funcționare", _txtStareFunct);
            AddLabeledInput(tblS1, 1, "Avarii", _txtAvarii);
            var tblS2 = AddRow(pnlStare, new[] { 33, 33, 34 });
            AddLabeledInput(tblS2, 0, "Anvelope față", _txtAnvFata);
            AddLabeledInput(tblS2, 1, "Anvelope spate", _txtAnvSpate);
            AddLabeledInput(tblS2, 2, "Uzură anvelope (%)", _txtUzura);

            var pnlDot = AddSectiune("DOTĂRI (DA/NU)", ref y, 134);
            _cmbTrusa = MakeDaNu(); _cmbExtinctor = MakeDaNu();
            _cmbTriunghi = MakeDaNu(); _cmbCric = MakeDaNu();
            var tblD1 = AddRow(pnlDot, new[] { 25, 25, 25, 25 });
            AddLabeledInput(tblD1, 0, "Trusă sanitară", _cmbTrusa);
            AddLabeledInput(tblD1, 1, "Extinctor", _cmbExtinctor);
            AddLabeledInput(tblD1, 2, "Triunghi reflectorizant", _cmbTriunghi);
            AddLabeledInput(tblD1, 3, "Cric", _cmbCric);

            _cmbCheie = MakeDaNu(); _cmbVesta = MakeDaNu();
            _cmbRoata = MakeDaNu();
            _txtUzuraRoata = MakeInput("0");
            var tblD2 = AddRow(pnlDot, new[] { 25, 25, 25, 25 });
            AddLabeledInput(tblD2, 0, "Cheie roți", _cmbCheie);
            AddLabeledInput(tblD2, 1, "Vestă reflectorizantă", _cmbVesta);
            AddLabeledInput(tblD2, 2, "Roată rezervă (DA/NU)", _cmbRoata);
            AddLabeledInput(tblD2, 3, "Uzură roată rezervă (%)", _txtUzuraRoata);

            var pnlDocs = AddSectiune("DOCUMENTE VEHICUL (DA/NU)", ref y, 78);
            _cmbCertif = MakeDaNu(); _cmbRCA = MakeDaNu(); _cmbRovinieta = MakeDaNu();
            var tblDocs = AddRow(pnlDocs, new[] { 33, 33, 34 });
            AddLabeledInput(tblDocs, 0, "Certificat înmatriculare", _cmbCertif);
            AddLabeledInput(tblDocs, 1, "Asigurare RCA", _cmbRCA);
            AddLabeledInput(tblDocs, 2, "Rovinieta", _cmbRovinieta);

            AddMentiuniSection(ref y);
        }

        private static ComboBox MakeDaNu()
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = FInput };
            c.Items.AddRange(new[] { "DA", "NU" });
            c.SelectedIndex = 0;
            return c;
        }

        protected override bool ValidateForm()
        {
            if (!RequireText(_txtCod, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCod)) return false;
            if (!RequireText(_txtMarca, "Marcă")) return false;
            if (!RequireText(_txtNrInmatr, "Nr. înmatriculare")) return false;
            return true;
        }

        protected override void PopulateModel()
        {
            FillAngajatorFields(_m);
            _m.CodInregistrare = GetText(_txtCod);
            _m.DataPV = GetDate(_dtpData);
            _m.TipPredare = GetTipPredare(_cmbTipPredare);

            _m.NumePredator2 = GetText(_txtNumePredator2);
            _m.FunctiePredator2 = GetText(_txtFunctiePredator2);
            _m.CNPPredator2 = GetText(_txtCNPPredator2);
            _m.CISeriaPredator2 = GetText(_txtCISeriaPredator2);
            _m.CINrPredator2 = GetText(_txtCINrPredator2);
            _m.DomiciliuPredator2 = GetText(_txtDomiciliuPredator2);

            _m.CISeria = GetText(_txtCISeria);
            _m.CINr = GetText(_txtCINr);
            _m.Domiciliu = GetText(_txtDomiciliu);

            _m.MarcaAuto = GetText(_txtMarca);
            _m.NrInmatriculare = GetText(_txtNrInmatr);
            _m.SerieSasiu = GetText(_txtSerieSasiu);
            _m.AnFabricatie = GetText(_txtAnFab);
            _m.Kilometri = GetText(_txtKm);
            _m.StareFunctionare = GetText(_txtStareFunct);
            _m.Avarii = GetText(_txtAvarii);
            _m.AnvelopeFata = GetText(_txtAnvFata);
            _m.AnvelopeSpate = GetText(_txtAnvSpate);
            _m.UzuraAnvelope = GetText(_txtUzura);

            _m.TrusaSanitara = _cmbTrusa.SelectedItem?.ToString() ?? "DA";
            _m.Extinctor = _cmbExtinctor.SelectedItem?.ToString() ?? "DA";
            _m.TriunghiReflectorizant = _cmbTriunghi.SelectedItem?.ToString() ?? "DA";
            _m.Cric = _cmbCric.SelectedItem?.ToString() ?? "DA";
            _m.CheieRoti = _cmbCheie.SelectedItem?.ToString() ?? "DA";
            _m.VestaReflectorizanta = _cmbVesta.SelectedItem?.ToString() ?? "DA";
            _m.RoataRezervа = _cmbRoata.SelectedItem?.ToString() ?? "DA";
            _m.UzuraRoataRezervа = GetText(_txtUzuraRoata);

            _m.CertificatInmatriculare = _cmbCertif.SelectedItem?.ToString() ?? "DA";
            _m.AsigurareRCA = _cmbRCA.SelectedItem?.ToString() ?? "DA";
            _m.Rovinieta = _cmbRovinieta.SelectedItem?.ToString() ?? "DA";
        }

        protected override string GetTemplatePath() => PluginConfig.GetTemplatePath(TipPV.Autovehicul);
        protected override DateTime GetRegistraturaDate()
            => _dtpData != null ? _dtpData.Value.Date : base.GetRegistraturaDate();
    }
}