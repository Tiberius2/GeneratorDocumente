using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    public sealed class BulkRezultateDialog : Form
    {
        public sealed class RezultatRow
        {
            public string NumeAngajat { get; set; }
            public bool Success { get; set; }
            public string Cod { get; set; }
            public string Mesaj { get; set; }
        }

        private readonly List<RezultatRow> _rows;
        private readonly DocumentTheme _theme;

        public BulkRezultateDialog(List<RezultatRow> rows, DocumentTheme theme)
        {
            _rows = rows;
            _theme = theme;

            int ok = 0;
            foreach (var r in rows) if (r.Success) ok++;

            Text = string.Format("Rezultate generare — {0}/{1} reușite", ok, rows.Count);
            Size = new Size(700, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);

            BuildUI(ok);
        }

        private void BuildUI(int ok)
        {
            int total = _rows.Count;
            bool allOk = ok == total;

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = allOk ? Color.FromArgb(34, 130, 84) : Color.FromArgb(192, 72, 68) };
            pnlHeader.Controls.Add(new Label
            {
                Text = allOk
                    ? string.Format("✓  Toate cele {0} documente au fost generate cu succes!", total)
                    : string.Format("⚠  {0} din {1} documente generate cu succes", ok, total),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 12)
            });

            // Grid
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(220, 230, 245),
                Font = new Font("Segoe UI", 9.5f),
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            grid.ColumnHeadersHeight = 34;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);

            var colStatus = new DataGridViewTextBoxColumn { HeaderText = "Status", Width = 60, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            var colNume = new DataGridViewTextBoxColumn { HeaderText = "Angajat", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            var colCod = new DataGridViewTextBoxColumn { HeaderText = "Cod înregistrare", Width = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            var colMesaj = new DataGridViewTextBoxColumn { HeaderText = "Detalii", Width = 200, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            grid.Columns.AddRange(colStatus, colNume, colCod, colMesaj);

            foreach (var r in _rows)
            {
                int rowIdx = grid.Rows.Add(r.Success ? "✓" : "✗", r.NumeAngajat, r.Cod ?? "-", r.Mesaj ?? string.Empty);
                grid.Rows[rowIdx].DefaultCellStyle.ForeColor = r.Success
                    ? Color.FromArgb(25, 100, 50)
                    : Color.FromArgb(160, 40, 40);
            }

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(248, 249, 250) };
            pnlFooter.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(210, 220, 235))) e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0); };

            var btnClose = new Button
            {
                Text = "Închide",
                Size = new Size(120, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = _theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 8,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Resize += (s, e) => btnClose.Left = pnlFooter.Width - btnClose.Width - 16;

            Controls.Add(grid);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }
    }
}