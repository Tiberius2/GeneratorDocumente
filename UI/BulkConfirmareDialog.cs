using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public sealed class BulkConfirmareDialog : Form
    {
        public bool Confirmed { get; private set; }

        public sealed class BulkRow
        {
            public AngajatPickerDialog.AngajatItem Angajat { get; set; }
            public string CodEstimat { get; set; }
        }

        private readonly List<BulkRow> _rows;
        private readonly DateTime _dataDoc;
        private readonly DocumentTheme _theme;

        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);

        public BulkConfirmareDialog(List<BulkRow> rows, DateTime dataDoc, DocumentTheme theme)
        {
            _rows = rows;
            _dataDoc = dataDoc;
            _theme = theme;

            Text = "Confirmare generare în masă";
            Size = new Size(680, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);

            BuildUI();
        }

        private void BuildUI()
        {
            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = _theme.Accent };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Confirmare Generare în Masă — Act Adițional",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 12)
            });

            // Info band
            var pnlInfo = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(235, 243, 252), Padding = new Padding(16, 8, 16, 8) };
            pnlInfo.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(180, 210, 240)))
                    e.Graphics.DrawLine(pen, 0, pnlInfo.Height - 1, pnlInfo.Width, pnlInfo.Height - 1);
            };
            pnlInfo.Controls.Add(new Label
            {
                Text = string.Format(
                    "Se vor genera {0} acte adiționale cu data {1}.\r\n" +
                    "Codurile de înregistrare sunt estimative — pot diferi dacă alte documente sunt înregistrate între timp.",
                    _rows.Count, _dataDoc.ToString("dd.MM.yyyy")),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(30, 70, 130),
                Dock = DockStyle.Fill,
                AutoSize = false
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
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 60, 100);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);

            var colNr = new DataGridViewTextBoxColumn { HeaderText = "#", Width = 40, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            var colNume = new DataGridViewTextBoxColumn { HeaderText = "Angajat", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            var colCod = new DataGridViewTextBoxColumn { HeaderText = "Cod înregistrare (estimat)", Width = 180, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            var colData = new DataGridViewTextBoxColumn { HeaderText = "Data doc.", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            grid.Columns.AddRange(colNr, colNume, colCod, colData);

            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                grid.Rows.Add(i + 1, r.Angajat.Name, r.CodEstimat, _dataDoc.ToString("dd.MM.yyyy"));
            }

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.FromArgb(248, 249, 250) };
            pnlFooter.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(210, 220, 235))) e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0); };

            var btnDa = new Button
            {
                Text = string.Format("✓  Generează {0} documente", _rows.Count),
                Size = new Size(200, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(34, 130, 84),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnDa.FlatAppearance.BorderSize = 2;
            btnDa.MouseEnter += (s, e) => btnDa.BackColor = Color.FromArgb(24, 110, 70);
            btnDa.MouseLeave += (s, e) => btnDa.BackColor = Color.FromArgb(34, 130, 84);
            btnDa.Click += (s, e) => { Confirmed = true; DialogResult = DialogResult.Yes; Close(); };

            var btnNu = new Button
            {
                Text = "Anulează",
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnNu.FlatAppearance.BorderSize = 2;
            btnNu.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnNu.Click += (s, e) => { DialogResult = DialogResult.No; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { btnDa, btnNu });
            pnlFooter.Resize += (s, e) => { btnDa.Left = pnlFooter.Width - btnDa.Width - 16; btnNu.Left = btnDa.Left - btnNu.Width - 8; };

            Controls.Add(grid);
            Controls.Add(pnlInfo);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }
    }
}