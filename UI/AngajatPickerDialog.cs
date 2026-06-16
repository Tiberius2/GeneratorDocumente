using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ActAditionalPlugin.UI
{
    public sealed class AngajatPickerDialog : Form
    {
        // ── Date returnate ─────────────────────────────────────
        public int SelectedPrsnId { get; private set; }
        public string SelectedName { get; private set; }
        public string SelectedCNP { get; private set; }
        public string SelectedFunctie { get; private set; }

        // ── Model intern ───────────────────────────────────────
        public sealed class AngajatItem
        {
            public int PrsnId { get; set; }
            public string Name { get; set; }
            public string CNP { get; set; }
            public string Functie { get; set; }
            public override string ToString() => Name;
        }

        private readonly List<AngajatItem> _all;
        private readonly int _currentPrsnId;
        private List<AngajatItem> _filtered;

        private TextBox _txtSearch;
        private ListBox _lst;
        private Label _lblCurrent;
        private Button _btnOk;

        private static readonly Color DarkBg = Color.FromArgb(30, 40, 60);
        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        private static readonly Color TextDes = Color.FromArgb(200, 215, 235);

        // ══════════════════════════════════════════════════════
        public AngajatPickerDialog(List<AngajatItem> employees, int currentPrsnId)
        {
            _all = employees ?? new List<AngajatItem>();
            _currentPrsnId = currentPrsnId;
            _filtered = new List<AngajatItem>(_all);

            Text = "Selectare angajat";
            Size = new Size(520, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = DarkBg;
            Font = new Font("Segoe UI", 10f);

            BuildUI();
            PopulateList();
            PreSelectCurrent();
        }

        // ══════════════════════════════════════════════════════
        //  BUILD
        // ══════════════════════════════════════════════════════
        private void BuildUI()
        {
            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(22, 32, 50) };
            var lblTitle = new Label
            {
                Text = "Angajat",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 10)
            };
            var lblSub = new Label
            {
                Text = "Selectează angajatul pentru care generezi documentul",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextDes,
                AutoSize = true,
                Location = new Point(16, 34)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Current employee indicator
            var pnlCurrent = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.FromArgb(40, 55, 80) };
            _lblCurrent = new Label
            {
                ForeColor = TextDes,
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            pnlCurrent.Controls.Add(_lblCurrent);

            // Search
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = DarkBg, Padding = new Padding(10, 6, 10, 4) };
            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(50, 65, 90),
                ForeColor = Color.White
            };
            SetSearchPlaceholder(_txtSearch, "Caută angajat...");
            _txtSearch.TextChanged += TxtSearch_Changed;
            _txtSearch.KeyDown += TxtSearch_KeyDown;
            pnlSearch.Controls.Add(_txtSearch);

            // List
            _lst = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(38, 50, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f),
                ItemHeight = 26,
                DrawMode = DrawMode.OwnerDrawFixed,
                IntegralHeight = false
            };
            _lst.DrawItem += Lst_DrawItem;
            _lst.DoubleClick += (s, e) => Confirma();
            _lst.SelectedIndexChanged += Lst_SelectionChanged;

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(22, 32, 50) };
            _btnOk = new Button
            {
                Text = "Continuă  →",
                Size = new Size(140, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Albastru,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 8,
                Enabled = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnOk.FlatAppearance.BorderSize = 0;
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
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnOk, btnCancel });
            pnlFooter.Resize += (s, e) =>
            {
                _btnOk.Left = pnlFooter.Width - _btnOk.Width - 14;
                btnCancel.Left = _btnOk.Left - btnCancel.Width - 8;
            };

            Controls.Add(_lst);
            Controls.Add(pnlSearch);
            Controls.Add(pnlCurrent);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            AcceptButton = _btnOk;
            CancelButton = btnCancel;
        }

        // ══════════════════════════════════════════════════════
        //  POPULATE / FILTER
        // ══════════════════════════════════════════════════════
        private void PopulateList()
        {
            _lst.Items.Clear();
            foreach (var a in _filtered) _lst.Items.Add(a);
        }

        private void PreSelectCurrent()
        {
            var current = _all.FirstOrDefault(a => a.PrsnId == _currentPrsnId);
            if (current != null)
            {
                _lblCurrent.Text = string.Format("  Angajat curent (din ecran):  {0}", current.Name);
                int idx = _filtered.IndexOf(current);
                if (idx >= 0)
                {
                    _lst.SelectedIndex = idx;
                    _lst.TopIndex = Math.Max(0, idx - 3);
                }
                SetSelected(current);
            }
            else
            {
                _lblCurrent.Text = "  Niciun angajat selectat din ecranul curent";
            }
        }

        private void TxtSearch_Changed(object sender, EventArgs e)
        {
            string q = _txtSearch.ForeColor == Color.Gray ? string.Empty : _txtSearch.Text.Trim().ToLower();
            _filtered = string.IsNullOrEmpty(q)
                ? new List<AngajatItem>(_all)
                : _all.Where(a => a.Name.ToLower().Contains(q)).ToList();
            PopulateList();
            if (_filtered.Count > 0) { _lst.SelectedIndex = 0; SetSelected(_filtered[0]); }
            else { _btnOk.Enabled = false; SelectedPrsnId = 0; }
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _lst.Items.Count > 0)
            {
                _lst.Focus();
                _lst.SelectedIndex = 0;
                e.Handled = true;
            }
        }

        private void Lst_SelectionChanged(object sender, EventArgs e)
        {
            var item = _lst.SelectedItem as AngajatItem;
            if (item != null) SetSelected(item);
        }

        private void SetSelected(AngajatItem a)
        {
            SelectedPrsnId = a.PrsnId;
            SelectedName = a.Name;
            SelectedCNP = a.CNP;
            SelectedFunctie = a.Functie;
            _btnOk.Enabled = true;
        }

        private void Confirma()
        {
            if (SelectedPrsnId == 0) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Custom draw list items ─────────────────────────────
        private void Lst_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _filtered.Count) return;
            var item = _filtered[e.Index];
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isCurrent = item.PrsnId == _currentPrsnId;

            var bg = sel ? Albastru : (e.Index % 2 == 0 ? Color.FromArgb(38, 50, 72) : Color.FromArgb(44, 57, 80));
            e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);

            // Bara stanga pentru angajatul curent
            if (isCurrent)
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 200, 120)), e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);

            string display = string.Format("{0} - {1}", item.PrsnId, item.Name);
            e.Graphics.DrawString(display,
                new Font("Segoe UI", 9.5f, isCurrent ? FontStyle.Bold : FontStyle.Regular),
                new SolidBrush(sel ? Color.White : (isCurrent ? Color.FromArgb(120, 220, 150) : TextDes)),
                new RectangleF(e.Bounds.X + 10, e.Bounds.Y + 4, e.Bounds.Width - 14, e.Bounds.Height - 4));
        }

        // ── Placeholder helper ────────────────────────────────
        private static void SetSearchPlaceholder(TextBox tb, string ph)
        {
            tb.Text = ph; tb.ForeColor = Color.Gray;
            tb.GotFocus += (s, e) => { if (tb.ForeColor == Color.Gray) { tb.Text = string.Empty; tb.ForeColor = Color.White; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = ph; tb.ForeColor = Color.Gray; } };
        }
    }
}