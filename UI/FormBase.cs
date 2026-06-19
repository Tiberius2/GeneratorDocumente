using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;

namespace ActAditionalPlugin.UI
{
    public abstract class FormBase : Form
    {
        // ── Tema ──────────────────────────────────────────────
        protected readonly DocumentTheme Theme;
        protected string _codGenerat = string.Empty;
        protected string _titluDocGenerat = string.Empty;

        protected static readonly Color TextPrincipal = Color.FromArgb(25, 35, 55);
        protected static readonly Color TextSecundar = Color.FromArgb(80, 100, 130);
        protected static readonly Color FundalForm = Color.FromArgb(242, 245, 250);
        protected static readonly Font FLabel = new Font("Segoe UI", 9f);
        protected static readonly Font FInput = new Font("Segoe UI", 10f, FontStyle.Bold);
        protected static readonly Font FSectiune = new Font("Segoe UI", 8f, FontStyle.Bold);

        protected Panel PnlBody { get; private set; }

        /// <summary>
        /// Campul de cod inregistrare din formular (setat de subclasa in BuildContent).
        /// FormBase il foloseste pentru a actualiza afisajul dupa recalculul codului.
        /// </summary>
        protected TextBox CodInregistrareField { get; set; }
        protected TextBox _txtMentiuni;
        protected System.Windows.Forms.Button BulkButton { get; private set; }

        protected FormBase(string titlu, DocumentTheme theme)
        {
            Theme = theme;
            Text = titlu;
            Size = new Size(860, 680);
            MinimumSize = new Size(720, 520);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10f);
            BackColor = FundalForm;
            BuildShell(titlu);
        }

        // ── Shell ─────────────────────────────────────────────
        private void BuildShell(string titlu)
        {
            var pnlTitlu = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Theme.Accent };
            pnlTitlu.Controls.Add(new Label
            {
                Text = titlu,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(18, 12)
            });

            PnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 10, 16, 10),
                BackColor = FundalForm
            };

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
            pnlFooter.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.AccentBorder))
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            // Buton Genereaza PDF
            var btnGen = new Button
            {
                Text = "▶ Previzualizeaza si Genereaza ◀",
                Size = new Size(252, 40),
                FlatStyle = FlatStyle.Popup,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 8,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            btnGen.MouseEnter += (s, e) => btnGen.BackColor = Theme.AccentDark;
            btnGen.MouseLeave += (s, e) => btnGen.BackColor = Theme.Accent;
            btnGen.Click += BtnPreview_Click;

            // Buton Previzualizare — tematizat, iconica diferita

            var btnInapoi = new Button
            {
                Text = "← Înapoi",
                Size = new Size(120, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Top = 10,
                Left = 18,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            btnInapoi.FlatAppearance.BorderSize = 1;
            btnInapoi.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnInapoi.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { btnGen, btnInapoi });

            // Buton generare in masa — activat de subclase (ActAditionalForm)
            var btnBulk = new Button
            {
                Text = "⊕ Generare în masă",
                Height = 38,
                Width = 180,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 173, 78),
                ForeColor = Color.FromArgb(60, 40, 10),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 9,
                Visible = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnBulk.FlatAppearance.BorderSize = 0;
            pnlFooter.Controls.Add(btnBulk);
            BulkButton = btnBulk;

            pnlFooter.Resize += (s, e) =>
            {
                btnGen.Left = pnlFooter.Width - btnGen.Width - 18;
                if (BulkButton != null && BulkButton.Visible)
                    BulkButton.Left = btnGen.Left - BulkButton.Width - 10;
            };
            btnGen.Left = 680;

            var pnlAngajat = BuildAngajatSection();
            Controls.Add(PnlBody);
            Controls.Add(pnlAngajat);
            Controls.Add(pnlTitlu);
            Controls.Add(pnlFooter);
            Shown += (s, e) => { ActiveControl = null; };
        }

        // ── PDF flow ──────────────────────────────────────────
        private void BtnGenPdf_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            PopulateModel();
            PopulateMentiuni();
            if (!OnBeforeGenerate()) return;            // confirmare + inregistrare registratura
            string tpl = GetTemplatePath();
            if (!File.Exists(tpl)) { ShowError("Template-ul nu a fost găsit:\n" + tpl); return; }
            try
            {
                string pdf = DoGenerateFinalPdf(tpl);
                OnAfterGenerate();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { ShowError("Eroare la generare PDF:\n" + ex.Message); }
        }

        private void ShowSuccessAndClose()
        {
            var data = ActAditionalPlugin.Services.RegistraturaService.Instance != null
                ? ActAditionalPlugin.Services.RegistraturaService.Instance.GetLoginDate()
                : System.DateTime.Today;
            using (var dlg = new SuccessDialog(_titluDocGenerat, _codGenerat, data, Theme))
                dlg.ShowDialog(this);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            PopulateModel();
            PopulateMentiuni();
            string tpl = GetTemplatePath();
            if (!File.Exists(tpl)) { ShowError("Template-ul nu a fost găsit:\n" + tpl); return; }
            try
            {
                string tempPdf = GenerateTempPdf(tpl);
                using (var dlg = new PdfPreviewForm(tempPdf, Theme, deletePdfOnClose: true))
                {
                    dlg.ShowDialog(this);
                    if (dlg.UserConfirmed)
                    {
                        if (!OnBeforeGenerate()) return;   // confirmare + inregistrare registratura
                        string pdf = DoGenerateFinalPdf(tpl);
                        OnAfterGenerate();
                        ShowSuccessAndClose();
                    }
                }
            }
            catch (Exception ex) { ShowError("Eroare la previzualizare:\n" + ex.Message); }
        }

        private string GenerateTempPdf(string templatePath)
        {
            string tempDocx = Path.Combine(Path.GetTempPath(),
                string.Format("preview_{0}.docx", GetType().Name));
            string tempPdf = Path.ChangeExtension(tempDocx, ".pdf");
            try
            {
                File.Copy(templatePath, tempDocx, true);
                FillDocxTemplate(tempDocx);
                WordHelper.ConvertToPdf(tempDocx, tempPdf);
            }
            finally { if (File.Exists(tempDocx)) File.Delete(tempDocx); }
            return tempPdf;
        }

        private static void ShowError(string msg)
            => MessageBox.Show(msg, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // ── Abstract / Virtual ─────────────────────────────────
        protected abstract Panel BuildAngajatSection();
        protected abstract bool ValidateForm();
        protected abstract void PopulateModel();
        protected abstract string GetTemplatePath();
        protected abstract void FillDocxTemplate(string docxPath);
        protected abstract string DoGenerateFinalPdf(string templatePath);
        protected virtual void OnAfterGenerate() { }

        /// <summary>
        /// Apelat dupa PopulateModel(). Subclasele (DocumentFormBase, PvFormBase) scriu
        /// continutul campului _txtMentiuni in proprietatea MentiuniDocument a modelului.
        /// </summary>
        protected virtual void PopulateMentiuni() { }

        /// <summary>
        /// Apelat inainte de generarea finala a PDF-ului.
        /// Subclasele (DocumentFormBase, PvFormBase) afiseaza promptul de confirmare
        /// si fac INSERT in Registratura. Returneaza false daca userul anuleaza.
        /// </summary>
        protected virtual bool OnBeforeGenerate() => true;

        protected virtual DateTime GetRegistraturaDate()
        {
            var svc = ActAditionalPlugin.Services.RegistraturaService.Instance;
            return svc != null ? svc.GetLoginDate() : DateTime.Today;
        }

        // ── Sectiune Mentiuni / Observatii ─────────────────────
        protected Panel AddMentiuniSection(ref int y)
        {
            var pnl = AddSectiune("MENȚIUNI / OBSERVAȚII", ref y, 102);
            _txtMentiuni = MakeMultiline(52);
            _txtMentiuni.Width = Math.Max(pnl.ClientSize.Width - pnl.Padding.Horizontal, 300);
            pnl.Controls.Add(_txtMentiuni);
            pnl.Resize += (s, e) => _txtMentiuni.Width = pnl.ClientSize.Width - pnl.Padding.Horizontal;
            return pnl;
        }

        // ── Layout helpers ─────────────────────────────────────
        protected Panel AddSectiune(string titlu, ref int yOffset, int height)
        {
            // Header-band sectiune
            var pnlHdr = new Panel
            {
                Left = 0,
                Top = yOffset,
                Height = 30,
                BackColor = Theme.AccentPal,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlHdr.Paint += (s, e) =>
                e.Graphics.FillRectangle(new SolidBrush(Theme.Accent), 0, 0, 4, pnlHdr.Height);
            pnlHdr.Controls.Add(new Label
            {
                Text = titlu,
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = Theme.AccentDark,
                AutoSize = true,
                Location = new Point(14, 6)
            });
            PnlBody.Controls.Add(pnlHdr);
            PnlBody.Resize += (s, e) =>
                pnlHdr.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
            pnlHdr.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);
            yOffset += 34;

            // Continut sectiune
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Left = 0,
                Top = yOffset,
                Height = height,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8),
                AutoSize = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            flow.Paint += PaintBorder;
            PnlBody.Controls.Add(flow);
            PnlBody.Resize += (s, e) =>
            {
                flow.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                foreach (Control c in flow.Controls)
                    c.Width = flow.ClientSize.Width - flow.Padding.Horizontal;
            };
            flow.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);
            yOffset += height + 14;
            return flow;
        }

        protected TableLayoutPanel AddRow(Panel parent, int[] colPercents)
        {
            var tbl = MakeTbl(colPercents.Length, colPercents);
            tbl.Margin = new Padding(0, 4, 0, 0);
            tbl.Width = Math.Max(parent.ClientSize.Width - parent.Padding.Horizontal, 400);
            parent.Controls.Add(tbl);
            parent.Resize += (s, e) =>
                tbl.Width = Math.Max(parent.ClientSize.Width - parent.Padding.Horizontal, 400);
            return tbl;
        }

        protected TableLayoutPanel AddRow(Panel parent, int top, int[] colPercents)
            => AddRow(parent, colPercents);

        protected void AddLabeledInput(TableLayoutPanel tbl, int col, string label, Control ctrl, bool required = false)
            => AddCell(tbl, col, label, ctrl, required);

        protected static TableLayoutPanel MakeRow(int[] percents)
        {
            var tbl = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = percents.Length,
                Height = 54,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < percents.Length; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percents[i]));
            return tbl;
        }

        // ── Factory controale ──────────────────────────────────
        protected static TextBox MakeReadonly()
            => new TextBox { ReadOnly = true, BackColor = Color.FromArgb(208, 213, 226), ForeColor = Color.FromArgb(60, 75, 100), Font = FInput, BorderStyle = BorderStyle.FixedSingle };

        protected static TextBox MakeInput(string placeholder = "")
        {
            var tb = new TextBox { BackColor = Color.White, ForeColor = TextPrincipal, Font = FInput, BorderStyle = BorderStyle.FixedSingle };
            if (!string.IsNullOrEmpty(placeholder)) SetPlaceholder(tb, placeholder);
            return tb;
        }

        protected static TextBox MakeMultiline(int height = 60)
            => new TextBox { Multiline = true, Height = height, BackColor = Color.White, ForeColor = TextPrincipal, Font = FInput, BorderStyle = BorderStyle.FixedSingle, ScrollBars = ScrollBars.Vertical };

        protected static DateTimePicker MakeDtp()
            => new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = FInput, Height = 26 };

        protected static ComboBox MakeCombo()
            => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = FInput, BackColor = Color.White, ForeColor = TextPrincipal };

        protected Label MakeSectionHeader(string text, int top)
            => new Label { Text = text, Font = FSectiune, ForeColor = Theme.Accent, AutoSize = true, Location = new Point(0, top) };

        protected static void SetPlaceholder(TextBox tb, string ph)
        {
            tb.Text = ph; tb.ForeColor = Color.Gray;
            tb.Font = new Font(FInput, FontStyle.Regular);
            tb.GotFocus += (s, e) => { if (tb.ForeColor == Color.Gray) { tb.Text = string.Empty; tb.ForeColor = TextPrincipal; tb.Font = FInput; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = ph; tb.ForeColor = Color.Gray; tb.Font = new Font(FInput, FontStyle.Regular); } };
        }

        protected static string GetText(TextBox tb) => tb.ForeColor == Color.Gray ? string.Empty : tb.Text.Trim();
        protected static DateTime GetDate(DateTimePicker dtp) => dtp.Value.Date;

        protected static bool RequireText(TextBox tb, string name)
        {
            if (string.IsNullOrWhiteSpace(GetText(tb)))
            {
                MessageBox.Show(string.Format("Câmpul \"{0}\" este obligatoriu.", name), "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus(); return false;
            }
            return true;
        }

        protected static bool ValidateCodInregistrare(TextBox tb)
        {
            string val = GetText(tb);
            if (string.IsNullOrWhiteSpace(val)) return true;
            var m = Regex.Match(val, @"^(\d{2})(\d{3})/(\d+)$");
            if (!m.Success)
            {
                MessageBox.Show("Codul de înregistrare trebuie să fie în formatul YYddd/#nr\nExemplu: 26001/1", "Format invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus(); return false;
            }
            int zi = int.Parse(m.Groups[2].Value);
            if (zi < 1 || zi > 366)
            {
                MessageBox.Show("Ziua din an (ddd) trebuie să fie între 001 și 366.", "Format invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus(); return false;
            }
            return true;
        }

        protected static void PaintBottomLine(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using (var pen = new Pen(Color.FromArgb(210, 220, 235)))
                e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        protected static void PaintBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using (var pen = new Pen(Color.FromArgb(200, 215, 235)))
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        private static TableLayoutPanel MakeTbl(int cols, int[] percents)
        {
            var tbl = new TableLayoutPanel { RowCount = 1, ColumnCount = cols, Height = 54, BackColor = Color.Transparent, Padding = new Padding(0), Margin = new Padding(0) };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < cols; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percents[i]));
            return tbl;
        }

        private static void AddCell(TableLayoutPanel tbl, int col, string labelText, Control ctrl, bool required = false)
        {
            var cell = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
            var pnlLbl = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = Color.FromArgb(55, 75, 105),
                AutoSize = true,
                Location = new Point(0, 2)
            };
            pnlLbl.Controls.Add(lbl);
            if (required)
            {
                var star = new Label { Text = " *", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.FromArgb(200, 50, 40), AutoSize = true, Location = new Point(lbl.PreferredWidth, 2) };
                lbl.SizeChanged += (s, e) => star.Left = lbl.Right;
                pnlLbl.Controls.Add(star);
            }
            ctrl.Dock = DockStyle.Top; ctrl.Height = 26;
            cell.Controls.Add(ctrl);
            cell.Controls.Add(pnlLbl);
            tbl.Controls.Add(cell, col, 0);
        }
    }
}