using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    /// <summary>
    /// Prompt de confirmare inregistrare in Registratura, stilizat in tema documentului.
    /// </summary>
    public sealed class ConfirmareDialog : Form
    {
        public bool Confirmed { get; private set; }

        public ConfirmareDialog(string titluDoc, string codInreg, DateTime dataInreg, DocumentTheme theme)
        {
            Text = "Înregistrare";
            Size = new Size(440, 276);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = theme.Accent };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Confirmare Înregistrare Document",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 12)
            });

            // Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(22, 16, 22, 0)
            };

            int y = 16;

            pnlBody.Controls.Add(new Label
            {
                Text = "Se va genera și înregistra documentul:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 100, 130),
                AutoSize = true,
                Location = new Point(16, y)
            });
            y += 22;

            pnlBody.Controls.Add(new Label
            {
                Text = titluDoc,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 35, 55),
                AutoSize = true,
                Location = new Point(16, y)
            });
            y += 28;

            // Linie separator
            pnlBody.Controls.Add(new Panel
            {
                Left = 0,
                Top = y,
                Height = 1,
                Width = 390,
                BackColor = theme.AccentBorder
            });
            y += 12;

            // Cod înregistrare
            pnlBody.Controls.Add(new Label
            {
                Text = "Cod înregistrare:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 100, 130),
                AutoSize = true,
                Location = new Point(16, y)
            });
            pnlBody.Controls.Add(new Label
            {
                Text = codInreg,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = theme.Accent,
                AutoSize = true,
                Location = new Point(160, y - 2)
            });
            y += 24;

            // Data
            pnlBody.Controls.Add(new Label
            {
                Text = "Data înregistrare:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 100, 130),
                AutoSize = true,
                Location = new Point(16, y)
            });
            pnlBody.Controls.Add(new Label
            {
                Text = dataInreg.ToString("dd.MM.yyyy"),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 35, 55),
                AutoSize = true,
                Location = new Point(160, y)
            });

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(248, 249, 250)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(theme.AccentBorder))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var btnDa = new Button
            {
                Text = "Da, Continuă",
                Size = new Size(130, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnDa.FlatAppearance.BorderSize = 2;
            btnDa.MouseEnter += (s, e) => btnDa.BackColor = theme.AccentDark;
            btnDa.MouseLeave += (s, e) => btnDa.BackColor = theme.Accent;
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
            pnlFooter.Resize += (s, e) =>
            {
                btnDa.Left = pnlFooter.Width - btnDa.Width - 16;
                btnNu.Left = btnDa.Left - btnNu.Width - 8;
            };
            btnDa.Left = 294; btnNu.Left = 186;

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            AcceptButton = btnDa;
            CancelButton = btnNu;
        }
    }
}