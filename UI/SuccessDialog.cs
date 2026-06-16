using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    public sealed class SuccessDialog : Form
    {
        public SuccessDialog(string titluDoc, string codInreg, DateTime dataInreg, DocumentTheme theme)
        {
            Text = "Document Generat";
            Size = new Size(400, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = theme.Accent };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Document înregistrat și generat cu succes!",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(14, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "\uE930",
                Font = new Font("Segoe MDL2 Assets", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(330, 14)
            });
            

            // Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24, 18, 24, 14)
            };

            // Titlu document
            var lblTitlu = new Label
            {
                Text = titluDoc,
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Underline),
                ForeColor = Color.FromArgb(25, 35, 55),
                AutoSize = true,
                Location = new Point(14, 8)
            };
            pnlBody.Controls.Add(lblTitlu);

            // Separator
            var sep = new Panel
            {
                Left = 14,
                Top = 40,
                Height = 1,
                Width = 340,
                BackColor = theme.AccentBorder
            };
            pnlBody.Controls.Add(sep);

            // Badge cod inregistrare
            pnlBody.Controls.Add(new Label
            {
                Text = "Cod înregistrare",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(90, 110, 140),
                AutoSize = true,
                Location = new Point(14, 54)
            });
            var pnlCod = MakeBadge(codInreg, 150, 50, 110, theme);
            pnlBody.Controls.Add(pnlCod);

            // Badge data
            pnlBody.Controls.Add(new Label
            {
                Text = "Data înregistrare",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(90, 110, 140),
                AutoSize = true,
                Location = new Point(14, 90)
            });
            var pnlData = MakeBadge(dataInreg.ToString("dd.MM.yyyy"), 150, 86, 110, theme);
            pnlBody.Controls.Add(pnlData);

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(248, 249, 250)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(theme.AccentBorder))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var btnOk = new Button
            {
                Text = "OK",
                Size = new Size(90, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 9,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.MouseEnter += (s, e) => btnOk.BackColor = theme.AccentDark;
            btnOk.MouseLeave += (s, e) => btnOk.BackColor = theme.Accent;
            btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            pnlFooter.Controls.Add(btnOk);
            pnlFooter.Resize += (s, e) => btnOk.Left = pnlFooter.Width - btnOk.Width - 16;
            btnOk.Left = 294;

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
            AcceptButton = btnOk;
        }

        private static Panel MakeBadge(string text, int x, int y, int w, DocumentTheme theme)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, 26), BackColor = theme.AccentPal };
            var t = text;
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(theme.AccentBorder))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                using (var br = new SolidBrush(theme.Accent))
                using (var f = new Font("Segoe UI Semibold", 10f))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString(t, f, br, new RectangleF(0, 0, p.Width, p.Height), sf);
                }
            };
            return p;
        }
    }
}