using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    // ══════════════════════════════════════════════════════════
    //  CONFIRMARE STERGERE
    //  Dialog Da / Nu (texte fixe in romana, independent de limba
    //  Windows) inainte de stergerea unui element dintr-o lista.
    //  "Nu" e butonul implicit (Enter / Esc nu sterg nimic).
    //  Utilizare: if (!ConfirmareStergereDialog.Intreaba(this, "...", _theme)) return;
    // ══════════════════════════════════════════════════════════
    public sealed class ConfirmareStergereDialog : Form
    {
        public static bool Intreaba(IWin32Window owner, string mesaj, DocumentTheme theme)
        {
            using (var dlg = new ConfirmareStergereDialog(mesaj, theme))
                return dlg.ShowDialog(owner) == DialogResult.Yes;
        }

        private ConfirmareStergereDialog(string mesaj, DocumentTheme theme)
        {
            Text = "Confirmare ștergere";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(440, 170);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(192, 72, 68) };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Confirmare ștergere",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 11)
            });

            var lblMesaj = new Label
            {
                Text = mesaj,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(25, 35, 55),
                AutoSize = false,
                Location = new Point(20, 58),
                Size = new Size(400, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.FromArgb(248, 249, 252) };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(theme != null ? theme.AccentBorder : Color.FromArgb(210, 220, 235)))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var btnDa = new Button
            {
                Text = "Da",
                Size = new Size(100, 34),
                Location = new Point(ClientSize.Width - 100 - 16, 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(192, 72, 68),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Yes
            };
            btnDa.FlatAppearance.BorderColor = Color.FromArgb(158, 50, 47);
            btnDa.FlatAppearance.BorderSize = 1;

            var btnNu = new Button
            {
                Text = "Nu",
                Size = new Size(100, 34),
                Location = new Point(btnDa.Left - 100 - 10, 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.No
            };
            btnNu.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnNu.FlatAppearance.BorderSize = 1;

            pnlFooter.Controls.Add(btnDa);
            pnlFooter.Controls.Add(btnNu);

            Controls.Add(lblMesaj);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            AcceptButton = btnNu;   // Enter = Nu (nu sterge din greseala)
            CancelButton = btnNu;   // Esc = Nu
            Shown += (s, e) => btnNu.Focus();
        }
    }
}
