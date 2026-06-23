using System;
using System.Drawing;
using System.Windows.Forms;

namespace ActAditionalPlugin.UI
{
    public class EchipamentItemControl : Panel
    {
        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        private static readonly Font FInput = new Font("Segoe UI", 10f, FontStyle.Bold);
        private static readonly Font FInputReg = new Font("Segoe UI", 10f, FontStyle.Regular);
        private static readonly Font FLabel = new Font("Segoe UI", 9f);

        private Label _lblNumar;
        private TextBox _txtNume;
        private NumericUpDown _nudCantitate;
        private TextBox _txtPret;
        private Button _btnDelete;

        private int _numar;

        public int Numar
        {
            get { return _numar; }
            set { _numar = value; if (_lblNumar != null) _lblNumar.Text = value + "."; }
        }

        public Action OnDelete { get; set; }

        public EchipamentItemControl(int numar)
        {
            _numar = numar;
            Height = 66;
            BackColor = Color.White;
            Padding = new Padding(8, 6, 8, 6);

            Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 220, 235)))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                e.Graphics.FillRectangle(new SolidBrush(Albastru), 0, 0, 4, Height);
            };

            BuildLayout();
        }

        private void BuildLayout()
        {
            // Numar
            _lblNumar = new Label
            {
                Text = _numar + ".",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Albastru,
                AutoSize = true,
                Location = new Point(12, 8)
            };

            // Buton stergere
            _btnDelete = new Button
            {
                Text = "✕",
                Width = 28,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 50, 40),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Top = 8
            };
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += (s, e) => { if (OnDelete != null) OnDelete(); };

            // TableLayoutPanel cu 3 coloane: Denumire(55%) | Cantitate(15%) | Pret(30%)
            var tbl = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 3,
                Height = 54,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Top = 6,
                Left = 34  // dupa numar
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            // Campuri
            _txtNume = new TextBox
            {
                Font = FInputReg,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(25, 35, 55)
            };
            _txtNume.GotFocus += (s, e) => _txtNume.Font = FInput;
            _txtNume.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtNume.Text)) _txtNume.Font = FInputReg; };

            _nudCantitate = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = 1,
                Font = FInput,
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle
            };
            _nudCantitate.MouseWheel += (s, e) =>
            {
                ((HandledMouseEventArgs)e).Handled = true;
                // Propagam catre parent scrollabil
                var p = Parent;
                while (p != null && !(p is Panel pp && ((Panel)p).AutoScroll)) p = p.Parent;
                if (p != null)
                {
                    var scrollPanel = (Panel)p;
                    int delta = -(e.Delta / 120) * SystemInformation.MouseWheelScrollLines * 16;
                    scrollPanel.AutoScrollPosition = new System.Drawing.Point(0,
                        Math.Max(0, -scrollPanel.AutoScrollPosition.Y + delta));
                }
            };

            _txtPret = new TextBox
            {
                Font = FInputReg,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(25, 35, 55)
            };
            _txtPret.GotFocus += (s, e) => _txtPret.Font = FInput;
            _txtPret.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtPret.Text)) _txtPret.Font = FInputReg; };
            _txtPret.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',' && e.KeyChar != '\b')
                    e.Handled = true;
            };

            // Adaugam celule cu label + camp
            tbl.Controls.Add(MakeCell("Denumire", _txtNume), 0, 0);
            tbl.Controls.Add(MakeCell("Cant.", _nudCantitate), 1, 0);
            tbl.Controls.Add(MakeCell("Preț (Ron)", _txtPret), 2, 0);

            // Resize — tbl se intinde intre numar si buton delete
            Resize += (s, e) =>
            {
                _btnDelete.Left = Width - _btnDelete.Width - 8;
                tbl.Width = _btnDelete.Left - tbl.Left - 4;
            };

            Controls.Add(_lblNumar);
            Controls.Add(_btnDelete);
            Controls.Add(tbl);

            // Trigger initial
            if (Width > 0)
            {
                _btnDelete.Left = Width - _btnDelete.Width - 8;
                tbl.Width = _btnDelete.Left - tbl.Left - 4;
            }
        }

        private static Panel MakeCell(string labelText, Control ctrl)
        {
            var cell = new Panel
            {
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 8, 0)
            };

            // Label sus DockStyle.Top
            var lbl = new Label
            {
                Text = labelText,
                Font = FLabel,
                ForeColor = Color.FromArgb(80, 100, 130),
                Dock = DockStyle.Top,
                Height = 18,
                AutoSize = false,
                Padding = new Padding(0, 2, 0, 0)
            };

            // Control jos DockStyle.Top
            ctrl.Dock = DockStyle.Top;
            ctrl.Height = 26;

            // Ordinea: ctrl primul (jos), lbl al doilea (sus)
            cell.Controls.Add(ctrl);
            cell.Controls.Add(lbl);

            return cell;
        }

        // ── Date item ─────────────────────────────────────────
        public bool IsValid() => !string.IsNullOrWhiteSpace(_txtNume.Text);

        public ActAditionalPlugin.Models.PvBunItem GetBunItem()
        {
            if (!IsValid()) return null;
            return new ActAditionalPlugin.Models.PvBunItem
            {
                Nume = _txtNume.Text.Trim(),
                Cantitate = ((int)_nudCantitate.Value).ToString(),
                Pret = _txtPret.Text.Trim()
            };
        }

        // Pastrat pentru compatibilitate
        public string GetLine()
        {
            var item = GetBunItem();
            if (item == null) return string.Empty;
            return !string.IsNullOrWhiteSpace(item.Pret)
                ? string.Format("- {0} – {1} buc x {2} Ron", item.Nume, item.Cantitate, item.Pret)
                : string.Format("- {0} – {1} buc", item.Nume, item.Cantitate);
        }
    }
}