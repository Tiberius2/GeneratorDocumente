using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    public partial class PvSelectorDialog : Form
    {
        public TipPV TipSelectat { get; private set; }

        private static readonly (string Titlu, string Subtitlu, TipPV Tip)[] Tipuri =
        {
            ("Echipamente de lucru",  "Uniformă, echipament protecție, unelte", TipPV.Echipamente),
            ("Echipamente Electronice", "Laptop, telefon, tabletă, accesorii",  TipPV.Electronice),
            ("Autovehicul",           "Predare-primire vehicul de serviciu",    TipPV.Autovehicul),
        };

        private static readonly Color CuloarePV = Color.FromArgb(63, 129, 198);

        private TipPV? _selectat = null;
        private Button[] _cardButtons;
        private Button _btnContinua;

        public PvSelectorDialog()
        {
            Text = "Generator Procese Verbale";
            Size = new Size(680, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(242, 245, 250);
            Font = new Font("Segoe UI", 10f);

            BuildUI();
        }

        private void BuildUI()
        {
            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = CuloarePV };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Generator Procese Verbale",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Selectează tipul procesului verbal",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(200, 225, 250),
                AutoSize = true,
                Location = new Point(20, 44)
            });

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(180, 205, 235)))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            _btnContinua = new Button
            {
                Text = "Continuă  →",
                Size = new Size(140, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 200, 225),
                ForeColor = Color.FromArgb(190, 195, 200),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Enabled = false,
                Top = 10
            };
            _btnContinua.FlatAppearance.BorderSize = 0;
            _btnContinua.Click += (s, e) => Confirma();

            var btnCancel = new Button
            {
                Text = "Anulează",
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Top = 10
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnContinua, btnCancel });
            pnlFooter.Resize += (s, e) =>
            {
                _btnContinua.Left = pnlFooter.Width - _btnContinua.Width - 16;
                btnCancel.Left = _btnContinua.Left - btnCancel.Width - 8;
            };
            _btnContinua.Left = 510;
            btnCancel.Left = 402;

            // Grid carduri
            var pnlScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 16, 16, 8),
                BackColor = Color.FromArgb(242, 245, 250)
            };

            _cardButtons = new Button[Tipuri.Length];
            int cardW = 190, cardH = 100, gapX = 12;

            for (int i = 0; i < Tipuri.Length; i++)
            {
                int x = 16 + i * (cardW + gapX);
                var btn = CreateCard(Tipuri[i].Titlu, Tipuri[i].Subtitlu, x, 16, cardW, cardH, i);
                _cardButtons[i] = btn;
                pnlScroll.Controls.Add(btn);
            }

            Controls.Add(pnlScroll);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
            AcceptButton = _btnContinua;
            CancelButton = btnCancel;
        }

        private Button CreateCard(string titlu, string subtitlu, int x, int y, int w, int h, int index)
        {
            var btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Tag = index
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(210, 220, 235);
            btn.FlatAppearance.BorderSize = 1;
            btn.Paint += (s, e) => DrawCard(e.Graphics, btn, titlu, subtitlu);
            btn.Click += CardClick;
            btn.MouseEnter += (s, e) => { if (!IsSelected(index)) { btn.BackColor = Color.FromArgb(245, 248, 255); btn.Invalidate(); } };
            btn.MouseLeave += (s, e) => { if (!IsSelected(index)) { btn.BackColor = Color.White; btn.Invalidate(); } };
            return btn;
        }

        private void DrawCard(Graphics g, Button btn, string titlu, string subtitlu)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool sel = IsSelected((int)btn.Tag);

            g.FillRectangle(new SolidBrush(CuloarePV), 0, 0, btn.Width, 4);

            if (sel)
                using (var pen = new Pen(CuloarePV, 2))
                    g.DrawRectangle(pen, 1, 1, btn.Width - 3, btn.Height - 3);

            // Icon
            int ix = 14, iy = 16;
            g.FillRectangle(new SolidBrush(Color.FromArgb(sel ? 30 : 15, CuloarePV.R, CuloarePV.G, CuloarePV.B)), ix, iy, 22, 28);
            g.DrawRectangle(new Pen(CuloarePV, 1), ix, iy, 22, 28);
            for (int li = 0; li < 3; li++)
                g.DrawLine(new Pen(Color.FromArgb(150, CuloarePV.R, CuloarePV.G, CuloarePV.B)),
                    ix + 4, iy + 8 + li * 6, ix + 18, iy + 8 + li * 6);

            g.DrawString(titlu, new Font("Segoe UI", 9.5f, FontStyle.Bold),
                new SolidBrush(Color.FromArgb(25, 35, 55)),
                new RectangleF(44, 14, btn.Width - 52, 22));
            g.DrawString(subtitlu, new Font("Segoe UI", 7.5f),
                new SolidBrush(Color.FromArgb(100, 110, 130)),
                new RectangleF(44, 36, btn.Width - 52, 40));

            if (sel)
            {
                g.FillEllipse(new SolidBrush(CuloarePV), btn.Width - 24, btn.Height - 24, 18, 18);
                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                    g.DrawLines(pen, new[]
                    {
                        new Point(btn.Width - 20, btn.Height - 14),
                        new Point(btn.Width - 17, btn.Height - 11),
                        new Point(btn.Width - 10, btn.Height - 18)
                    });
                }
            }
        }

        private bool IsSelected(int index)
            => _selectat.HasValue && (int)_cardButtons[index].Tag == GetSelectedIndex();

        private void CardClick(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            int idx = (int)btn.Tag;
            _selectat = Tipuri[idx].Tip;

            foreach (var b in _cardButtons)
            {
                b.BackColor = (int)b.Tag == idx ? Color.FromArgb(245, 248, 255) : Color.White;
                b.Invalidate();
            }

            _btnContinua.Enabled = true;
            _btnContinua.BackColor = CuloarePV;
            _btnContinua.ForeColor = Color.White;
            _btnContinua.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        }

        private int GetSelectedIndex()
        {
            if (!_selectat.HasValue) return -1;
            for (int i = 0; i < Tipuri.Length; i++)
                if (Tipuri[i].Tip == _selectat.Value) return i;
            return -1;
        }

        private void Confirma()
        {
            if (!_selectat.HasValue) return;
            TipSelectat = _selectat.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}