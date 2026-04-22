using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PdfiumViewer;

namespace ActAditionalPlugin.UI
{
    /// <summary>
    /// Fereastra de previzualizare PDF.
    /// Primeste calea catre un PDF temporar, il afiseaza si il sterge la inchidere.
    /// </summary>
    public class PdfPreviewForm : Form
    {
        private readonly string _pdfPath;
        private readonly bool _deletePdfOnClose;
        private PdfViewer _viewer;
        private Button _btnGenereaza;
        private Button _btnInchide;

        public bool UserConfirmed { get; private set; } = false;

        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);

        public PdfPreviewForm(string pdfPath, bool deletePdfOnClose = true)
        {
            _pdfPath = pdfPath;
            _deletePdfOnClose = deletePdfOnClose;

            Text = "Previzualizare document";
            Size = new Size(900, 780);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.FromArgb(242, 245, 250);

            BuildUI();
            LoadPdf();
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Albastru
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Previzualizare document generat",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(18, 12)
            });

            // ── Footer ────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(180, 205, 235)))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            _btnGenereaza = new Button
            {
                Text = "✓ Salvează PDF",
                Size = new Size(160, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Albastru,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnGenereaza.FlatAppearance.BorderSize = 0;
            _btnGenereaza.Click += (s, e) =>
            {
                UserConfirmed = true;
                Close();
            };

            _btnInchide = new Button
            {
                Text = "✕ Închide",
                Size = new Size(120, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnInchide.FlatAppearance.BorderSize = 1;
            _btnInchide.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            _btnInchide.Click += (s, e) => Close();

            pnlFooter.Controls.AddRange(new Control[] { _btnGenereaza, _btnInchide });
            pnlFooter.Resize += (s, e) =>
            {
                _btnGenereaza.Left = pnlFooter.Width - _btnGenereaza.Width - 18;
                _btnInchide.Left = _btnGenereaza.Left - _btnInchide.Width - 8;
            };
            _btnGenereaza.Left = 720;
            _btnInchide.Left = 592;

            // ── PDF Viewer ────────────────────────────────────
            _viewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(230, 230, 230),
                ShowToolbar = true,
                ShowBookmarks = false,
            };

            Controls.Add(_viewer);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            FormClosed += PdfPreviewForm_FormClosed;
        }

        private void LoadPdf()
        {
            try
            {
                if (!File.Exists(_pdfPath))
                {
                    MessageBox.Show("Fișierul PDF nu a fost găsit:\n" + _pdfPath,
                        "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // PdfiumViewer necesita stream deschis pe durata afisarii
                var doc = PdfDocument.Load(_pdfPath);
                _viewer.Document = doc;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la încărcarea PDF:\n" + ex.Message,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PdfPreviewForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Eliberam documentul
            try { _viewer.Document?.Dispose(); } catch { }

            // Stergem PDF-ul temporar daca e cazul
            if (_deletePdfOnClose && File.Exists(_pdfPath))
            {
                try { File.Delete(_pdfPath); } catch { }
            }
        }
    }
}