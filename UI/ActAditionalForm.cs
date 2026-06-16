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
        }

        private static List<ClauzeActAditional> LoadClauzeActive()
        {
            var cfg = ClauzeService.Load();
            var tip = cfg.GetTipSelectat();
            return tip != null
                ? tip.Clauze.Where(c => c.Activ).ToList()
                : new List<ClauzeActAditional>();
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
    }
}