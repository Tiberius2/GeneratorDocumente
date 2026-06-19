using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public sealed partial class ClauzeEditorDialog : Form
    {
        private ClauzeConfig _config;
        private TipContract _selectedTip;
        private ClauzeActAditional _selectedClauza;
        private bool _loading;
        private bool _syncing;

        private ListBox _lstTipuri;
        private Button _btnTipNou, _btnStergeTip, _btnSelecteaza;
        private Label _lblClauzeHeader;
        private ListBox _lstClauze;
        private Button _btnClauzeNou, _btnStergeClauza;

        private Panel _pnlHint, _pnlEditTip, _pnlEditClauza;
        private TextBox _txtNumeTip;
        private CheckBox _chkTipActiv;
        private TextBox _txtTitlu, _txtTextClauza, _txtTextDinamic;
        private CheckBox _chkClauzeActiv;
        private DataGridView _gridCampuri;

        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);

        public ClauzeEditorDialog()
        {
            Text = "Editor Clauze - Acte Aditionale";
            Size = new Size(1300, 740);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(242, 245, 250);
            Font = new Font("Segoe UI", 10f);
            _config = ClauzeService.Load();
            BuildUI();
            RefreshTipuri();
        }

        private void BuildUI()
        {
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Albastru };
            pnlHeader.Controls.Add(new Label { Text = "Editor Clauze Act Aditional", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(16, 12) });

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White };
            pnlFooter.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(210, 220, 235))) e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); };
            var btnSalveaza = MakeBtn("✓  Salveaza tot", Color.FromArgb(34, 130, 84), 150);
            btnSalveaza.Font = new Font("Segoe UI", 10f, FontStyle.Bold); btnSalveaza.Height = 36; btnSalveaza.Top = 8; btnSalveaza.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnSalveaza.Click += BtnSalveaza_Click;
            var btnInchide = MakeBtn("Inchide", Color.FromArgb(240, 242, 246), 100);
            btnInchide.ForeColor = Color.FromArgb(60, 80, 110); btnInchide.Font = new Font("Segoe UI", 10f); btnInchide.Height = 36; btnInchide.Top = 8; btnInchide.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnInchide.Click += (s, e) => Close();
            pnlFooter.Controls.AddRange(new Control[] { btnSalveaza, btnInchide });
            pnlFooter.Resize += (s, e) => { btnSalveaza.Left = pnlFooter.Width - btnSalveaza.Width - 16; btnInchide.Left = btnSalveaza.Left - btnInchide.Width - 8; };

            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            // LEFT
            var pnlLeft = new Panel { Dock = DockStyle.Left, Width = 340, BackColor = Color.White, Padding = new Padding(8) };
            pnlLeft.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(210, 220, 235))) e.Graphics.DrawRectangle(p, 0, 0, pnlLeft.Width - 1, pnlLeft.Height - 1); };

            var lblTipuri = new Label { Text = "Tipuri de contract", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Albastru, Dock = DockStyle.Top, Height = 24, Padding = new Padding(2, 4, 0, 0) };

            _lstTipuri = new ListBox { BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), Height = 155, Dock = DockStyle.Top, ItemHeight = 22, DrawMode = DrawMode.OwnerDrawFixed };
            _lstTipuri.SelectedIndexChanged += LstTipuri_Changed;
            _lstTipuri.DrawItem += DrawTipItem;

            _btnTipNou = MakeBtn("+ Tip nou", Albastru, 88); _btnTipNou.Click += BtnTipNou_Click;
            _btnStergeTip = MakeBtn("X Sterge", Color.FromArgb(192, 72, 68), 74); _btnStergeTip.Click += BtnStergeTip_Click;
            _btnSelecteaza = MakeBtn("★ Implicit", Color.FromArgb(34, 130, 84), 90); _btnSelecteaza.Click += BtnSelecteaza_Click;

            var pnlTipBtns = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 3, 0, 3)
            };
            pnlTipBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            pnlTipBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            pnlTipBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlTipBtns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _btnTipNou.Dock = DockStyle.Fill; _btnTipNou.Margin = new Padding(0, 0, 4, 0);
            _btnStergeTip.Dock = DockStyle.Fill; _btnStergeTip.Margin = new Padding(0, 0, 4, 0);
            _btnSelecteaza.Dock = DockStyle.Fill; _btnSelecteaza.Margin = new Padding(0);
            pnlTipBtns.Controls.Add(_btnTipNou, 0, 0);
            pnlTipBtns.Controls.Add(_btnStergeTip, 1, 0);
            pnlTipBtns.Controls.Add(_btnSelecteaza, 2, 0);

            var sep = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.FromArgb(230, 235, 242) };

            _lblClauzeHeader = new Label { Text = "Clauze", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Albastru, Dock = DockStyle.Top, Height = 24, Padding = new Padding(2, 4, 0, 0) };

            _lstClauze = new ListBox { BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), Dock = DockStyle.Fill, ItemHeight = 22 };
            _lstClauze.SelectedIndexChanged += LstClauze_Changed;

            // Butoane reordonare (sus/jos)
            var btnUp = new Button { Text = "▲", Width = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(240, 242, 246), ForeColor = Color.FromArgb(60, 80, 110), Dock = DockStyle.Top, Height = 28, Cursor = Cursors.Hand };
            var btnDown = new Button { Text = "▼", Width = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(240, 242, 246), ForeColor = Color.FromArgb(60, 80, 110), Dock = DockStyle.Top, Height = 28, Cursor = Cursors.Hand };
            btnUp.FlatAppearance.BorderSize = 2; btnDown.FlatAppearance.BorderSize = 2;
            btnUp.Click += (s, e) =>
            {
                if (_selectedTip == null) return;
                int idx = _lstClauze.SelectedIndex;
                if (idx <= 0) return;
                var tmp = _selectedTip.Clauze[idx]; _selectedTip.Clauze[idx] = _selectedTip.Clauze[idx - 1]; _selectedTip.Clauze[idx - 1] = tmp;
                RefreshClauze(); _lstClauze.SelectedIndex = idx - 1;
            };
            btnDown.Click += (s, e) =>
            {
                if (_selectedTip == null) return;
                int idx = _lstClauze.SelectedIndex;
                if (idx < 0 || idx >= _selectedTip.Clauze.Count - 1) return;
                var tmp = _selectedTip.Clauze[idx]; _selectedTip.Clauze[idx] = _selectedTip.Clauze[idx + 1]; _selectedTip.Clauze[idx + 1] = tmp;
                RefreshClauze(); _lstClauze.SelectedIndex = idx + 1;
            };

            var pnlArrows = new Panel { Dock = DockStyle.Right, Width = 30, BackColor = Color.White };
            pnlArrows.Controls.Add(btnDown);
            pnlArrows.Controls.Add(btnUp);

            var pnlListContainer = new Panel { Dock = DockStyle.Fill };
            pnlListContainer.Controls.Add(_lstClauze);
            pnlListContainer.Controls.Add(pnlArrows);

            _btnClauzeNou = MakeBtn("+ Clauza noua", Albastru, 115); _btnClauzeNou.Click += BtnClauzeNou_Click;
            _btnStergeClauza = MakeBtn("X Sterge", Color.FromArgb(192, 72, 68), 74); _btnStergeClauza.Click += BtnStergeClauza_Click;

            var pnlClauzeBtns = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 3, 0, 3)
            };
            pnlClauzeBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            pnlClauzeBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            pnlClauzeBtns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _btnClauzeNou.Dock = DockStyle.Fill; _btnClauzeNou.Margin = new Padding(0, 0, 4, 0);
            _btnStergeClauza.Dock = DockStyle.Fill; _btnStergeClauza.Margin = new Padding(0);
            pnlClauzeBtns.Controls.Add(_btnClauzeNou, 0, 0);
            pnlClauzeBtns.Controls.Add(_btnStergeClauza, 1, 0);

            pnlLeft.Controls.Add(pnlListContainer);
            pnlLeft.Controls.Add(pnlClauzeBtns);
            pnlLeft.Controls.Add(_lblClauzeHeader);
            pnlLeft.Controls.Add(sep);
            pnlLeft.Controls.Add(pnlTipBtns);
            pnlLeft.Controls.Add(_lstTipuri);
            pnlLeft.Controls.Add(lblTipuri);

            var splitter = new Splitter { Dock = DockStyle.Left, Width = 8, BackColor = Color.FromArgb(230, 235, 242) };

            // HINT
            _pnlHint = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _pnlHint.Controls.Add(new Label { Text = "Selecteaza un tip de contract sau o clauza din stanga pentru a edita.", Font = new Font("Segoe UI", 10f, FontStyle.Italic), ForeColor = Color.FromArgb(120, 140, 170), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });

            BuildEditTipPanel();
            BuildEditClauzePanel();

            pnlBody.Controls.Add(_pnlEditClauza);
            pnlBody.Controls.Add(_pnlEditTip);
            pnlBody.Controls.Add(_pnlHint);
            pnlBody.Controls.Add(splitter);
            pnlBody.Controls.Add(pnlLeft);

            ShowPanel("hint");
            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        private void BuildEditTipPanel()
        {
            _pnlEditTip = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 12, 16, 12), Visible = false };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.White };
            _pnlEditTip.Controls.Add(flow);

            var lblNume = new Label { Text = "Nume tip contract:", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(80, 100, 130), AutoSize = false, Height = 20, Margin = new Padding(0, 8, 0, 2) };
            _txtNumeTip = new TextBox { Height = 28, Font = new Font("Segoe UI", 10.5f), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 4) };
            _txtNumeTip.TextChanged += (s, e) =>
            {
                if (_loading || _selectedTip == null) return;
                _selectedTip.Nume = _txtNumeTip.Text;
                int idx = _lstTipuri.SelectedIndex;
                if (idx >= 0 && idx < _config.TipuriContract.Count) _lstTipuri.Invalidate();
            };
            _chkTipActiv = new CheckBox { Text = "Tip activ", Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(25, 35, 55), AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            _chkTipActiv.CheckedChanged += (s, e) => { if (!_loading && _selectedTip != null) _selectedTip.Activ = _chkTipActiv.Checked; };

            flow.Controls.AddRange(new Control[] { lblNume, _txtNumeTip, _chkTipActiv });
            flow.Resize += (s, e) => { int w = flow.ClientSize.Width - flow.Padding.Horizontal; lblNume.Width = w; _txtNumeTip.Width = w; };
        }

        private void BuildEditClauzePanel()
        {
            _pnlEditClauza = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 12, 16, 12), Visible = false };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.White };
            _pnlEditClauza.Controls.Add(flow);

            Action<string, Control> add = (lbl, ctrl) =>
            {
                var l = new Label { Text = lbl, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(80, 100, 130), AutoSize = false, Height = 20, Margin = new Padding(0, 8, 0, 2) };
                ctrl.Margin = new Padding(0, 0, 0, 4);
                flow.Controls.Add(l);
                flow.Controls.Add(ctrl);
                flow.Resize += (s, e) => { int w = flow.ClientSize.Width - flow.Padding.Horizontal; l.Width = w; ctrl.Width = w; };
                l.Width = 500; ctrl.Width = 500;
            };

            _txtTitlu = new TextBox { Height = 28, Font = new Font("Segoe UI", 10.5f), BorderStyle = BorderStyle.FixedSingle };
            _txtTitlu.TextChanged += (s, e) =>
            {
                if (_loading || _selectedClauza == null) return;
                _selectedClauza.Titlu = _txtTitlu.Text;
                int idx = _lstClauze.SelectedIndex;
                if (idx >= 0 && idx < _lstClauze.Items.Count)
                    _lstClauze.Items[idx] = (!_selectedClauza.Activ ? "[inactiv] " : "") + (_selectedClauza.Titlu ?? "(fara titlu)");
            };
            add("Titlu clauza (apare in dropdown):", _txtTitlu);

            _txtTextClauza = new TextBox { Height = 82, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 10.5f), BorderStyle = BorderStyle.FixedSingle };
            _txtTextClauza.TextChanged += (s, e) => { if (!_loading && _selectedClauza != null) _selectedClauza.TextClauza = _txtTextClauza.Text; };
            add("Text clauza (text static - referinta articolului):", _txtTextClauza);

            _txtTextDinamic = new TextBox { Height = 62, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 10.5f), BorderStyle = BorderStyle.FixedSingle };
            _txtTextDinamic.TextChanged += (s, e) => { if (!_loading && _selectedClauza != null) _selectedClauza.TextDinamic = _txtTextDinamic.Text; };
            add("Text dinamic - folositi {0}, {1}... pentru campurile de mai jos:", _txtTextDinamic);

            _chkClauzeActiv = new CheckBox { Text = "Clauza activa (apare in lista din formular)", Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(25, 35, 55), AutoSize = true, Margin = new Padding(0, 10, 0, 8) };
            _chkClauzeActiv.CheckedChanged += (s, e) => { if (!_loading && _selectedClauza != null) _selectedClauza.Activ = _chkClauzeActiv.Checked; };
            flow.Controls.Add(_chkClauzeActiv);

            var lblCampuri = new Label { Text = "Campuri de input ({0} = primul camp, {1} = al doilea, etc.):", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(80, 100, 130), AutoSize = false, Height = 20, Margin = new Padding(0, 4, 0, 2) };
            flow.Controls.Add(lblCampuri);

            _gridCampuri = new DataGridView
            {
                Height = 170,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Margin = new Padding(0, 0, 0, 8),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            _gridCampuri.ColumnHeadersHeight = 32;
            _gridCampuri.Columns.Add(new DataGridViewTextBoxColumn { Name = "Label", HeaderText = "Label camp", FillWeight = 30 });
            _gridCampuri.Columns.Add(new DataGridViewTextBoxColumn { Name = "Placeholder", HeaderText = "Placeholder", FillWeight = 35 });
            var colTip = new DataGridViewComboBoxColumn { Name = "TipCamp", HeaderText = "Tip camp", FillWeight = 20, FlatStyle = FlatStyle.Flat };
            colTip.Items.AddRange("text", "numar intreg", "numar zecimal", "data");
            _gridCampuri.Columns.Add(colTip);
            _gridCampuri.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ordine", HeaderText = "Ordine", FillWeight = 15 });
            _gridCampuri.DataError += (s, e) => e.ThrowException = false;
            _gridCampuri.CellValueChanged += (s, e) => { if (!_syncing) SyncCampuri(); };
            _gridCampuri.RowsRemoved += (s, e) => { if (!_syncing) SyncCampuri(); };
            flow.Controls.Add(_gridCampuri);

            flow.Resize += (s, e) => { int w = flow.ClientSize.Width - flow.Padding.Horizontal; lblCampuri.Width = w; _gridCampuri.Width = w; _chkClauzeActiv.MaximumSize = new Size(w, 0); };
        }

        private void RefreshTipuri(bool keepSel = true)
        {
            int prev = _lstTipuri.SelectedIndex;
            _lstTipuri.Items.Clear();
            foreach (var t in _config.TipuriContract) _lstTipuri.Items.Add(t.Id);
            if (keepSel && prev >= 0 && prev < _lstTipuri.Items.Count) _lstTipuri.SelectedIndex = prev;
        }

        private void RefreshClauze()
        {
            _lstClauze.Items.Clear();
            if (_selectedTip == null) { _lblClauzeHeader.Text = "Clauze"; return; }
            _lblClauzeHeader.Text = string.Format("Clauze — {0}", _selectedTip.Nume);
            foreach (var c in _selectedTip.Clauze)
                _lstClauze.Items.Add((!c.Activ ? "[inactiv] " : "") + (c.Titlu ?? "(fara titlu)"));
        }

        private void DrawTipItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _config.TipuriContract.Count) return;
            var tip = _config.TipuriContract[e.Index];
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isSel = tip.Id == _config.TipSelectatId;
            e.DrawBackground();
            e.Graphics.FillRectangle(new SolidBrush(sel ? SystemColors.Highlight : Color.White), e.Bounds);
            if (isSel) e.Graphics.DrawString("★", new Font("Segoe UI", 9f), new SolidBrush(Color.FromArgb(34, 130, 84)), e.Bounds.X + 2, e.Bounds.Y + 3);
            string text = (!tip.Activ ? "[inactiv] " : "") + (tip.Nume ?? "(fara nume)");
            e.Graphics.DrawString(text, new Font("Segoe UI", 9.5f, isSel ? FontStyle.Bold : FontStyle.Regular),
                new SolidBrush(sel ? Color.White : Color.FromArgb(25, 35, 55)),
                new RectangleF(e.Bounds.X + 18, e.Bounds.Y + 2, e.Bounds.Width - 20, e.Bounds.Height - 4));
        }

        private void LstTipuri_Changed(object sender, EventArgs e)
        {
            int idx = _lstTipuri.SelectedIndex;
            if (idx < 0 || idx >= _config.TipuriContract.Count) { _selectedTip = null; RefreshClauze(); ShowPanel("hint"); return; }
            _selectedTip = _config.TipuriContract[idx];
            RefreshClauze(); _selectedClauza = null;
            _loading = true; _txtNumeTip.Text = _selectedTip.Nume ?? string.Empty; _chkTipActiv.Checked = _selectedTip.Activ; _loading = false;
            ShowPanel("tip");
        }

        private void LstClauze_Changed(object sender, EventArgs e)
        {
            if (_selectedTip == null) return;
            int idx = _lstClauze.SelectedIndex;
            if (idx < 0 || idx >= _selectedTip.Clauze.Count) { _selectedClauza = null; ShowPanel("tip"); return; }
            _selectedClauza = _selectedTip.Clauze[idx];
            _loading = true;
            _txtTitlu.Text = _selectedClauza.Titlu ?? string.Empty;
            _txtTextClauza.Text = _selectedClauza.TextClauza ?? string.Empty;
            _txtTextDinamic.Text = _selectedClauza.TextDinamic ?? string.Empty;
            _chkClauzeActiv.Checked = _selectedClauza.Activ;
            _syncing = true;
            _gridCampuri.Rows.Clear();
            if (_selectedClauza.Campuri != null)
                foreach (var c in _selectedClauza.Campuri.OrderBy(x => x.Ordine))
                    _gridCampuri.Rows.Add(c.Label, c.Placeholder, NormalizeTip(c.TipCamp), c.Ordine.ToString());
            _syncing = false; _loading = false;
            ShowPanel("clauza");
        }

        private void BtnTipNou_Click(object sender, EventArgs e)
        {
            var tip = new TipContract { Nume = "Tip nou" };
            _config.TipuriContract.Add(tip);
            if (string.IsNullOrEmpty(_config.TipSelectatId)) _config.TipSelectatId = tip.Id;
            RefreshTipuri(false);
            _lstTipuri.SelectedIndex = _config.TipuriContract.Count - 1;
            _txtNumeTip.Focus(); _txtNumeTip.SelectAll();
        }

        private void BtnStergeTip_Click(object sender, EventArgs e)
        {
            if (_selectedTip == null) return;
            if (_config.TipuriContract.Count == 1) { MessageBox.Show("Trebuie sa existe cel putin un tip de contract.", "Atentie", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show(string.Format("Stergi tipul \"{0}\" si toate clauzele lui?", _selectedTip.Nume), "Confirmare", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (_config.TipSelectatId == _selectedTip.Id)
                _config.TipSelectatId = _config.TipuriContract.FirstOrDefault(t => t.Id != _selectedTip.Id)?.Id ?? string.Empty;
            _config.TipuriContract.Remove(_selectedTip);
            _selectedTip = null; _selectedClauza = null;
            RefreshTipuri(false); RefreshClauze(); ShowPanel("hint");
        }

        private void BtnSelecteaza_Click(object sender, EventArgs e)
        {
            if (_selectedTip == null) return;
            _config.TipSelectatId = _selectedTip.Id;
            _lstTipuri.Invalidate();
            MessageBox.Show(string.Format("Tipul \"{0}\" va fi folosit la generarea actelor aditionale.", _selectedTip.Nume), "Selectat", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnClauzeNou_Click(object sender, EventArgs e)
        {
            if (_selectedTip == null) { MessageBox.Show("Selecteaza mai intai un tip de contract.", "Atentie", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var c = new ClauzeActAditional { Titlu = "Clauza noua" };
            _selectedTip.Clauze.Add(c); RefreshClauze();
            _lstClauze.SelectedIndex = _selectedTip.Clauze.Count - 1;
        }

        private void BtnStergeClauza_Click(object sender, EventArgs e)
        {
            if (_selectedTip == null || _selectedClauza == null) return;
            if (MessageBox.Show(string.Format("Stergi clauza \"{0}\"?", _selectedClauza.Titlu), "Confirmare", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _selectedTip.Clauze.Remove(_selectedClauza); _selectedClauza = null;
            RefreshClauze(); ShowPanel("tip");
        }

        private void SyncCampuri()
        {
            if (_selectedClauza == null) return;
            _selectedClauza.Campuri.Clear();
            foreach (DataGridViewRow row in _gridCampuri.Rows)
            {
                if (row.IsNewRow) continue;
                string lbl = row.Cells["Label"].Value?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(lbl)) continue;
                int ord = 0; int.TryParse(row.Cells["Ordine"].Value?.ToString(), out ord);
                _selectedClauza.Campuri.Add(new CampClauza { Label = lbl, Placeholder = row.Cells["Placeholder"].Value?.ToString() ?? string.Empty, TipCamp = row.Cells["TipCamp"].Value?.ToString() ?? "text", Ordine = ord });
            }
        }

        private void BtnSalveaza_Click(object sender, EventArgs e)
        {
            SyncCampuri();
            try { ClauzeService.Save(_config); MessageBox.Show("Clauze salvate cu succes.", "Salvat", MessageBoxButtons.OK, MessageBoxIcon.Information); DialogResult = DialogResult.OK; }
            catch (Exception ex) { MessageBox.Show("Eroare la salvare: " + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ShowPanel(string which) { _pnlHint.Visible = which == "hint"; _pnlEditTip.Visible = which == "tip"; _pnlEditClauza.Visible = which == "clauza"; }

        private static Button MakeBtn(string text, Color bg, int w)
        {
            var b = new Button { Text = text, Height = 28, Width = w, FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 2; return b;
        }

        private static string NormalizeTip(string tip)
        {
            switch ((tip ?? "text").ToLower())
            {
                case "string": return "text";
                case "integer": return "numar intreg";
                case "decimal": return "numar zecimal";
                case "date": return "data";
                default: return tip ?? "text";
            }
        }
    }
}