using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public class ActAditionalForm : DocumentFormBase
    {
        private readonly ActAditionalModel _aa;
        private TextBox _txtCodInregistrare;
        private DateTimePicker _dtpDataAct;
        private DateTimePicker _dtpDataVig;
        private Panel _pnlModificari;
        private readonly List<PunctModificareControl> _puncte = new List<PunctModificareControl>();
        private List<ClauzeActAditional> _clauze;

        public ActAditionalForm(ActAditionalModel model)
            : base(model, "Act Adițional — CIM")
        {
            _aa = model;
            _clauze = LoadClauzeActive();
            BuildBody();

            // Activeaza butonul de generare in masa daca contextul e disponibil
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
            CodInregistrareField = _txtCodInregistrare;
            if (!string.IsNullOrEmpty(_model.CodInregistrare))
                _txtCodInregistrare.Text = _model.CodInregistrare;
            _dtpDataAct = MakeDtp();
            _dtpDataVig = MakeDtp();
            var tbl = AddRow(pnlDoc, 8, new[] { 25, 37, 38 });
            AddLabeledInput(tbl, 0, "Cod înregistrare", _txtCodInregistrare, required: true);
            AddLabeledInput(tbl, 1, "Data actului", _dtpDataAct);
            AddLabeledInput(tbl, 2, "Intrare în vigoare", _dtpDataVig);

            AddMentiuniSection(ref y);

            // ── MODIFICARI ────────────────────────────────────
            // Header modificari
            var pnlModHeader = new Panel
            {
                Left = 0,
                Top = y,
                Height = 34,
                BackColor = FundalForm,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            PnlBody.Controls.Add(pnlModHeader);
            PnlBody.Resize += (s, e) =>
                pnlModHeader.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
            pnlModHeader.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);

            pnlModHeader.Controls.Add(new Label
            {
                Text = "MODIFICĂRI — Art. I",
                Font = FSectiune,
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(0, 8)
            });

            var btnAdd = new Button
            {
                Text = "+ Adaugă modificare",
                Height = 28,
                Width = 180,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Top = 3,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            ButtonPalettes.Primary.ApplyTo(btnAdd);
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) => AddPunct();

            var btnEditorClauze = new Button
            {
                Text = "⚙ Editează clauze",
                Height = 28,
                Width = 150,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Top = 3,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 211, 137),   // amber
                ForeColor = Color.FromArgb(60, 40, 10),
                Cursor = Cursors.Hand
            };
            btnEditorClauze.FlatAppearance.BorderSize = 2;
            btnEditorClauze.FlatAppearance.BorderColor = Color.FromArgb(240, 173, 78);
            btnEditorClauze.Click += (s, e) =>
            {
                using (var dlg = new ClauzeEditorDialog())
                {
                    dlg.ShowDialog(this);
                    // Reincarcam clauze indiferent de rezultat
                    _clauze = LoadClauzeActive();
                    foreach (var p in _puncte) p.SetClauze(_clauze);
                }
            };
            var btnBulk = new Button
            {
                Text = "⊕ Generare în masă",
                Height = 28,
                Width = 170,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Top = 3,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 173, 78),
                ForeColor = Color.FromArgb(60, 40, 10),
                Cursor = Cursors.Hand
            };
            btnBulk.FlatAppearance.BorderSize = 0;
            btnBulk.Visible = false;
            btnBulk.Click += (s, e) => DoBulkGenerate();

            pnlModHeader.Controls.Add(btnBulk);
            pnlModHeader.Resize += (s, e) =>
            {
                btnAdd.Left = pnlModHeader.Width - btnAdd.Width;
                btnEditorClauze.Left = btnAdd.Left - btnEditorClauze.Width - 8;
                btnBulk.Left = btnEditorClauze.Left - btnBulk.Width - 8;
            };
            btnBulk.Left = btnEditorClauze.Left - btnBulk.Width - 8;

            pnlModHeader.Controls.Add(btnAdd);
            pnlModHeader.Controls.Add(btnEditorClauze);
            pnlModHeader.Resize += (s, e) =>
            {
                btnAdd.Left = pnlModHeader.Width - btnAdd.Width;
                btnEditorClauze.Left = btnAdd.Left - btnEditorClauze.Width - 8;
            };
            btnAdd.Left = pnlModHeader.Width - btnAdd.Width;
            btnEditorClauze.Left = btnAdd.Left - btnEditorClauze.Width - 8;

            y += 42;

            // Panel scroll modificari
            _pnlModificari = new Panel
            {
                Left = 0,
                Top = y,
                BackColor = FundalForm,
                AutoScroll = true,  // scroll intern cand punctele depasesc inaltimea panoului
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
            };
            PnlBody.Controls.Add(_pnlModificari);
            PnlBody.Resize += (s, e) =>
            {
                _pnlModificari.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                _pnlModificari.Height = Math.Max(PnlBody.ClientSize.Height - y - 4, 80);
                RelayoutPuncte();
            };
            _pnlModificari.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);
            _pnlModificari.Height = 300;
        }

        private void AddPunct()
        {
            var c = new PunctModificareControl(_puncte.Count + 1, _clauze);
            c.OnDelete = () => { _puncte.Remove(c); _pnlModificari.Controls.Remove(c); Renumber(); RelayoutPuncte(); };
            _puncte.Add(c);
            _pnlModificari.Controls.Add(c);
            RelayoutPuncte();
        }

        private void Renumber()
        {
            for (int i = 0; i < _puncte.Count; i++) _puncte[i].Numar = i + 1;
        }

        private void RelayoutPuncte()
        {
            int w = Math.Max(_pnlModificari.Width - 2, 400);
            int y = 0;
            foreach (var c in _puncte)
            {
                c.Width = w; c.Left = 0; c.Top = y;
                y += c.Height + 6;
            }
        }

        protected override bool ValidateForm()
        {
            if (!RequireText(_txtCodInregistrare, "Cod înregistrare")) return false;
            if (!ValidateCodInregistrare(_txtCodInregistrare)) return false;
            if (_puncte.Count == 0)
            { MessageBox.Show("Adăugați cel puțin o modificare.", "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            for (int i = 0; i < _puncte.Count; i++)
                if (_puncte[i].GetPunct() == null)
                { MessageBox.Show(string.Format("Modificarea {0} este incompletă.", i + 1), "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            return true;
        }

        protected override void PopulateModel()
        {
            FillAngajator(_aa);
            _aa.CodInregistrare = GetText(_txtCodInregistrare);
            _aa.DataEmitereAct = GetDate(_dtpDataAct);
            _aa.DataVigoare = GetDate(_dtpDataVig);
            _aa.Modificari = new List<PunctModificare>();
            foreach (var p in _puncte) _aa.Modificari.Add(p.GetPunct());
        }

        protected override string GetTemplatePath()
            => PluginConfig.GetTemplatePath(TipDocument.ActAditional);

        protected override DateTime GetRegistraturaDate()
            => _dtpDataAct != null ? _dtpDataAct.Value.Date : base.GetRegistraturaDate();

        private static List<ClauzeActAditional> LoadClauzeActive()
        {
            var cfg = ActAditionalPlugin.Services.ClauzeService.Load();
            var tip = cfg.GetTipSelectat();
            return tip != null
                ? tip.Clauze.Where(c => c.Activ).ToList()
                : new List<ClauzeActAditional>();
        }

        // ══════════════════════════════════════════════════════
        //  GENERARE IN MASA
        // ══════════════════════════════════════════════════════
        internal void SetupBulkButton(Button btn)
        {
            // Butonul e creat in FormBase footer — il wireaza din afara
            btn.Click += (s, e) => DoBulkGenerate();
        }

        private void DoBulkGenerate()
        {
            if (!ValidateForm()) return;
            PopulateModel();
            PopulateMentiuni();

            if (!ActAditionalPlugin.Services.BulkContext.IsAvailable)
            {
                MessageBox.Show("Lista de angajați nu este disponibilă.", "Eroare",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ctx = ActAditionalPlugin.Services.BulkContext.Angajati;

            // 1. Selectare angajati
            List<AngajatPickerDialog.AngajatItem> selected;
            using (var picker = new AngajatMultiPickerDialog(ctx))
            {
                if (picker.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                selected = picker.SelectedAngajati;
            }
            if (selected.Count == 0) return;

            // 2. Pre-calculeaza codurile estimative
            var svc = ActAditionalPlugin.Services.RegistraturaService.Instance;
            DateTime docDate = GetRegistraturaDate();
            string baseCode = svc.CalculateCod(docDate);
            var parts = baseCode.Split('/');
            string prefix = parts[0];
            int baseNr = 0;
            int.TryParse(parts.Length > 1 ? parts[1] : "1", out baseNr);

            var rows = new List<BulkConfirmareDialog.BulkRow>();
            for (int i = 0; i < selected.Count; i++)
                rows.Add(new BulkConfirmareDialog.BulkRow
                {
                    Angajat = selected[i],
                    CodEstimat = string.Format("{0}/{1}", prefix, baseNr + i)
                });

            // 3. Dialog confirmare cu preview coduri
            using (var dlgConf = new BulkConfirmareDialog(rows, docDate, Theme))
            {
                if (dlgConf.ShowDialog(this) != System.Windows.Forms.DialogResult.Yes) return;
            }

            // 4. Generare efectiva
            var rezultate = new List<BulkRezultateDialog.RezultatRow>();
            string templatePath = GetTemplatePath();
            int tipDocPK = ActAditionalPlugin.Services.RegistraturaService.GetTipDocPK(TipDocument.ActAditional);
            string titlu = ActAditionalPlugin.Services.RegistraturaService.GetTitluDoc(TipDocument.ActAditional);

            foreach (var ang in selected)
            {
                string cod = string.Empty;
                try
                {
                    // Clonam modelul cu datele angajatului curent
                    var m = CloneModelForAngajat(ang);

                    // Inregistrare in registratura
                    cod = svc.CalculateCod(docDate);
                    svc.Inregistreaza(cod, docDate, tipDocPK, titlu, ang.PrsnId);
                    m.CodInregistrare = cod;

                    // Generare PDF
                    ActAditionalPlugin.Services.TemplateEngine.GeneratePdf(m, templatePath);

                    // Adauga in tabela CCCACTEADITIONALE
                    if (ActAditionalPlugin.UI.DocumentFormBase.OnDocumentGenerated != null)
                        ActAditionalPlugin.UI.DocumentFormBase.OnDocumentGenerated(m);

                    rezultate.Add(new BulkRezultateDialog.RezultatRow
                    {
                        NumeAngajat = ang.Name,
                        Success = true,
                        Cod = cod,
                        Mesaj = "Generat cu succes"
                    });
                }
                catch (System.Exception ex)
                {
                    rezultate.Add(new BulkRezultateDialog.RezultatRow
                    {
                        NumeAngajat = ang.Name,
                        Success = false,
                        Cod = cod,
                        Mesaj = ex.Message.Length > 80 ? ex.Message.Substring(0, 80) + "..." : ex.Message
                    });
                }
            }

            // 5. Dialog rezultate
            using (var dlgRez = new BulkRezultateDialog(rezultate, Theme))
                dlgRez.ShowDialog(this);
        }

        private ActAditionalModel CloneModelForAngajat(AngajatPickerDialog.AngajatItem ang)
        {
            var ctx = ActAditionalPlugin.Services.BulkContext.GetCimData;
            var cim = ctx != null ? ctx(ang.PrsnId) : new ActAditionalPlugin.Services.ErpCimData();

            var m = new ActAditionalModel();
            // Date angajat
            m.PrsnId = ang.PrsnId;
            m.NumeSalariat = ang.Name;
            m.CNP = ang.CNP;
            m.Functie = ang.Functie;
            m.NrCim = cim.NrCim;
            m.DataCim = cim.DataCim;

            // Date companie (identice pentru toți)
            var comp = ActAditionalPlugin.Services.BulkContext.CompanyData;
            if (comp != null) ApplyCompanyDataPublic(m, comp);

            // Date document (identice)
            m.DataEmitereAct = _aa.DataEmitereAct;
            m.DataVigoare = _aa.DataVigoare;
            m.MentiuniDocument = _aa.MentiuniDocument;
            m.Modificari = _aa.Modificari; // aceleasi clauze
            m.CodInregistrare = string.Empty;   // se va seta dupa Inregistreaza

            return m;
        }

        private static void ApplyCompanyDataPublic(ActAditionalPlugin.Models.DocumentModelBase m,
            ActAditionalPlugin.Services.ErpCompanyData c)
        {
            m.NumeAngajator = c.NumeAngajator;
            m.CIFAngajator = c.CIFAngajator;
            m.ReprezentantLegal = c.ReprezentantLegal;
            m.AdresaCompanie = c.AdresaCompanie;
            m.ZipCompanie = c.ZipCompanie;
            m.NrRegComertului = c.NrRegComertului;
            m.IbanCompanie = c.IbanCompanie;
            m.NrTelefonCompanie = c.NrTelefonCompanie;
            m.EmailCompanie = c.EmailCompanie;
            m.WebsiteCompanie = c.WebsiteCompanie;
        }
    }
}