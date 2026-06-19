using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ActAditionalPlugin.UI
{
    public sealed class AngajatMultiPickerDialog : Form
    {
        public List<AngajatPickerDialog.AngajatItem> SelectedAngajati { get; private set; }
            = new List<AngajatPickerDialog.AngajatItem>();

        private readonly List<AngajatPickerDialog.AngajatItem> _all;
        private List<AngajatPickerDialog.AngajatItem> _filtered;

        private TextBox _txtSearch;
        private CheckedListBox _lst;
        private Label _lblCount;
        private Button _btnOk;

        private static readonly Color DarkBg = Color.FromArgb(30, 40, 60);
        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        private static readonly Color TextDes = Color.FromArgb(200, 215, 235);

        public AngajatMultiPickerDialog(List<AngajatPickerDialog.AngajatItem> employees)
        {
            _all = employees ?? new List<AngajatPickerDialog.AngajatItem>();
            _filtered = new List<AngajatPickerDialog.AngajatItem>(_all);

            Text = "Selectare angajați — Generare în masă";
            Size = new Size(560, 580);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = DarkBg;
            Font = new Font("Segoe UI", 10f);

            BuildUI();
            PopulateList();
        }

        private void BuildUI()
        {
            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(22, 32, 50) };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Generare Act Adițional — în masă",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 10)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Selectează angajații pentru care se va genera actul adițional",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextDes,
                AutoSize = true,
                Location = new Point(16, 34)
            });

            // Search + Sel all row
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = DarkBg, Padding = new Padding(10, 6, 10, 4) };

            _txtSearch = new TextBox
            {
                Left = 10,
                Top = 6,
                Height = 28,
                Width = 380,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(50, 65, 90),
                ForeColor = Color.White,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            SetPlaceholder(_txtSearch, "Caută angajat...");
            _txtSearch.TextChanged += (s, e) => FilterList();
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Down && _lst.Items.Count > 0) { _lst.Focus(); e.Handled = true; } };

            var btnAll = MakeSmallBtn("✓ Selectează tot", 128);
            btnAll.Left = 10; btnAll.Top = 42;
            btnAll.Click += (s, e) => { for (int i = 0; i < _lst.Items.Count; i++) _lst.SetItemChecked(i, true); UpdateCount(); };

            var btnNone = MakeSmallBtn("✗ Deselectează tot", 140);
            btnNone.Left = 146; btnNone.Top = 42;
            btnNone.Click += (s, e) => { for (int i = 0; i < _lst.Items.Count; i++) _lst.SetItemChecked(i, false); UpdateCount(); };

            pnlTop.Controls.AddRange(new Control[] { _txtSearch, btnAll, btnNone });
            pnlTop.Resize += (s, e) => _txtSearch.Width = pnlTop.Width - 20;

            // List
            _lst = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(38, 50, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                CheckOnClick = true,
                IntegralHeight = false
            };
            _lst.ItemCheck += (s, e) => BeginInvoke(new Action(UpdateCount));

            // Counter
            var pnlCount = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(22, 32, 50) };
            _lblCount = new Label
            {
                Text = "0 angajați selectați",
                ForeColor = TextDes,
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            pnlCount.Controls.Add(_lblCount);

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(22, 32, 50) };
            pnlFooter.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(45, 60, 85))) e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); };

            _btnOk = new Button
            {
                Text = "Continuă  →",
                Size = new Size(150, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Albastru,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 8,
                Enabled = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnOk.FlatAppearance.BorderSize = 2;
            _btnOk.Click += (s, e) => Confirma();

            var btnCancel = new Button
            {
                Text = "Anulează",
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 70, 95),
                ForeColor = TextDes,
                Font = new Font("Segoe UI", 10f),
                Top = 8,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCancel.FlatAppearance.BorderSize = 2;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnOk, btnCancel });
            pnlFooter.Resize += (s, e) => { _btnOk.Left = pnlFooter.Width - _btnOk.Width - 14; btnCancel.Left = _btnOk.Left - btnCancel.Width - 8; };

            Controls.Add(_lst);
            Controls.Add(pnlTop);
            Controls.Add(pnlCount);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
            AcceptButton = _btnOk; CancelButton = btnCancel;
        }

        private void PopulateList()
        {
            _lst.Items.Clear();
            foreach (var a in _filtered)
                _lst.Items.Add(string.Format("{0} - {1}", a.PrsnId, a.Name), false);
            UpdateCount();
        }

        private void FilterList()
        {
            string q = _txtSearch.ForeColor == Color.Gray ? string.Empty : _txtSearch.Text.Trim().ToLower();
            _filtered = string.IsNullOrEmpty(q)
                ? new List<AngajatPickerDialog.AngajatItem>(_all)
                : _all.Where(a => a.Name.ToLower().Contains(q) || a.PrsnId.ToString().Contains(q)).ToList();
            PopulateList();
        }

        private void UpdateCount()
        {
            int n = _lst.CheckedIndices.Count;
            _lblCount.Text = n == 0 ? "Niciun angajat selectat"
                : n == 1 ? "1 angajat selectat"
                : string.Format("{0} angajați selectați", n);
            _btnOk.Enabled = n > 0;
            _btnOk.Text = n > 0
                ? string.Format("Continuă ({0})  →", n)
                : "Continuă  →";
        }

        private void Confirma()
        {
            SelectedAngajati = new List<AngajatPickerDialog.AngajatItem>();
            foreach (int idx in _lst.CheckedIndices)
                if (idx < _filtered.Count)
                    SelectedAngajati.Add(_filtered[idx]);
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Button MakeSmallBtn(string text, int w)
        {
            var b = new Button
            {
                Text = text,
                Width = w,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 70, 95),
                ForeColor = TextDes,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 2;
            return b;
        }

        private static void SetPlaceholder(TextBox tb, string ph)
        {
            tb.Text = ph; tb.ForeColor = Color.Gray;
            tb.GotFocus += (s, e) => { if (tb.ForeColor == Color.Gray) { tb.Text = string.Empty; tb.ForeColor = Color.White; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = ph; tb.ForeColor = Color.Gray; } };
        }
    }
}