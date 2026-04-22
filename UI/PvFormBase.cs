using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;
using System.Diagnostics;

namespace ActAditionalPlugin.UI
{
    public abstract class PvFormBase : Form
    {
        protected readonly PvModelBase _model;
        private readonly Action<PvModelBase> _onPdfGenerated;
        protected Panel PnlBody { get; private set; }

        // Tema — aceeasi ca DocumentFormBase
        protected static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        protected static readonly Color AlbastruBorder = Color.FromArgb(180, 205, 235);
        protected static readonly Color TextPrincipal = Color.FromArgb(25, 35, 55);
        protected static readonly Color TextSecundar = Color.FromArgb(80, 100, 130);
        protected static readonly Color FundalForm = Color.FromArgb(242, 245, 250);
        protected static readonly Font FInput = new Font("Segoe UI", 10f, FontStyle.Bold);
        protected static readonly Font FSectiune = new Font("Segoe UI", 8f, FontStyle.Bold);

        private TextBox _txtNume, _txtCnp, _txtFunctie;

        protected PvFormBase(PvModelBase model, string titlu, Action<PvModelBase> onPdfGenerated = null)
        {
            _model = model;
            _onPdfGenerated = onPdfGenerated;
            Text = titlu;
            Size = new Size(860, 680);
            MinimumSize = new Size(720, 520);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10f);
            BackColor = FundalForm;

            BuildShell(titlu);
            PopulateAngajat();
        }

        private void BuildShell(string titlu)
        {
            // Titlu
            var pnlTitlu = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Albastru };
            pnlTitlu.Controls.Add(new Label
            {
                Text = titlu,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(18, 12)
            });

            // Sectiune angajat
            var pnlAngajat = BuildAngajatSection();

            // Body
            PnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 10, 16, 10),
                BackColor = FundalForm
            };

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(AlbastruBorder))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var btnGen = new Button
            {
                Text = "Generează PDF",
                Size = new Size(160, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Albastru,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnGen.FlatAppearance.BorderSize = 0;
            btnGen.Click += BtnGenPdf_Click;

            var btnPreview = new Button
            {
                Text = "👁 Previzualizează",
                Size = new Size(170, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnPreview.FlatAppearance.BorderSize = 1;
            btnPreview.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnPreview.Click += BtnPreview_Click;

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

            pnlFooter.Controls.AddRange(new Control[] { btnGen, btnPreview, btnInapoi });
            pnlFooter.Resize += (s, e) =>
            {
                btnGen.Left = pnlFooter.Width - btnGen.Width - 18;
                btnPreview.Left = btnGen.Left - btnPreview.Width - 8;
            };
            btnGen.Left = 680;
            btnPreview.Left = 502;

            Controls.Add(PnlBody);
            Controls.Add(pnlAngajat);
            Controls.Add(pnlTitlu);
            Controls.Add(pnlFooter);
        }

        // ── Sectiune angajat ──────────────────────────────────
        private Panel BuildAngajatSection()
        {
            var outer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.White,
                Padding = new Padding(16, 6, 16, 6)
            };
            outer.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 220, 235)))
                    e.Graphics.DrawLine(pen, 0, outer.Height - 1, outer.Width, outer.Height - 1);
            };

            var hdr = new Label
            {
                Text = "DATE ANGAJAT (PRIMITOR)",
                Font = FSectiune,
                ForeColor = Albastru,
                Dock = DockStyle.Top,
                Height = 20,
                AutoSize = false
            };
            outer.Controls.Add(hdr);

            _txtFunctie = MakeReadonly();
            var tbl2 = MakeTbl(3, new[] { 33, 33, 34 });
            tbl2.Dock = DockStyle.Top;
            AddCell(tbl2, 0, "Funcție", _txtFunctie);
            outer.Controls.Add(tbl2);

            _txtNume = MakeReadonly();
            _txtCnp = MakeReadonly();
            var tbl1 = MakeTbl(2, new[] { 60, 40 });
            tbl1.Dock = DockStyle.Top;
            AddCell(tbl1, 0, "Angajat", _txtNume);
            AddCell(tbl1, 1, "CNP", _txtCnp);
            outer.Controls.Add(tbl1);

            return outer;
        }

        private void PopulateAngajat()
        {
            _txtNume.Text = _model.NumeSalariat;
            _txtCnp.Text = _model.CNP;
            _txtFunctie.Text = _model.Functie;
        }

        // ── PDF handlers ──────────────────────────────────────
        private void BtnGenPdf_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            PopulateModel();
            string tpl = GetTemplatePath();
            if (!System.IO.File.Exists(tpl))
            { MessageBox.Show("Template-ul nu a fost găsit:\n" + tpl, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            try
            {
                string pdf = PvTemplateEngine.GeneratePdf(_model, tpl);
                _onPdfGenerated?.Invoke(_model);
                Process.Start(pdf);
            }
            catch (Exception ex)
            { MessageBox.Show("Eroare la generare PDF:\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            PopulateModel();
            string tpl = GetTemplatePath();
            if (!System.IO.File.Exists(tpl))
            { MessageBox.Show("Template-ul nu a fost găsit:\n" + tpl, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            try
            {
                string tempDocx = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    string.Format("pv_preview_{0}.docx", _model.PrsnId));
                string tempPdf = System.IO.Path.ChangeExtension(tempDocx, ".pdf");
                try
                {
                    System.IO.File.Copy(tpl, tempDocx, true);
                    PvTemplateEngine.FillTemplatePublic(tempDocx, _model);
                    PvTemplateEngine.ConvertToPdfPublic(tempDocx, tempPdf);
                }
                finally
                {
                    if (System.IO.File.Exists(tempDocx)) System.IO.File.Delete(tempDocx);
                }

                using (var preview = new PdfPreviewForm(tempPdf, deletePdfOnClose: true))
                {
                    preview.ShowDialog(this);
                    if (preview.UserConfirmed)
                    {
                        string finalPdf = PvTemplateEngine.GeneratePdf(_model, tpl);
                        _onPdfGenerated?.Invoke(_model);
                        Process.Start(finalPdf);
                    }
                }
            }
            catch (Exception ex)
            { MessageBox.Show("Eroare la previzualizare:\n" + ex.Message, "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        protected abstract bool ValidateForm();
        protected abstract void PopulateModel();
        protected abstract string GetTemplatePath();

        // ══════════════════════════════════════════════════════
        //  HELPERS LAYOUT
        // ══════════════════════════════════════════════════════
        protected Panel AddSectiune(string titlu, ref int yOffset, int height)
        {
            var lbl = new Label
            {
                Text = titlu,
                Font = FSectiune,
                ForeColor = Albastru,
                AutoSize = true,
                Left = 0,
                Top = yOffset
            };
            PnlBody.Controls.Add(lbl);
            yOffset += 22;

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Left = 0,
                Top = yOffset,
                Height = height,
                BackColor = Color.White,
                Padding = new Padding(12, 6, 12, 6),
                AutoSize = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            flow.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(200, 215, 235)))
                    e.Graphics.DrawRectangle(pen, 0, 0, flow.Width - 1, flow.Height - 1);
            };
            PnlBody.Controls.Add(flow);
            PnlBody.Resize += (s, e) =>
            {
                flow.Width = PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal;
                foreach (Control c in flow.Controls)
                    c.Width = flow.ClientSize.Width - flow.Padding.Horizontal;
            };
            flow.Width = Math.Max(PnlBody.ClientSize.Width - PnlBody.Padding.Horizontal, 400);

            yOffset += height + 10;
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

        protected void AddLabeledInput(TableLayoutPanel tbl, int col, string label, Control ctrl, bool required = false)
            => AddCell(tbl, col, label, ctrl, required);

        private static TableLayoutPanel MakeTbl(int cols, int[] percents)
        {
            var tbl = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = cols,
                Height = 54,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < cols; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percents[i]));
            return tbl;
        }

        private static void AddCell(TableLayoutPanel tbl, int col, string labelText, Control ctrl, bool required = false)
        {
            var cell = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
            var pnlLbl = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 100, 130),
                AutoSize = true,
                Location = new Point(0, 2)
            };
            pnlLbl.Controls.Add(lbl);
            if (required)
            {
                var star = new Label
                {
                    Text = " *",
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(200, 50, 40),
                    AutoSize = true,
                    Location = new Point(lbl.PreferredWidth, 2)
                };
                lbl.SizeChanged += (s, e) => star.Left = lbl.Right;
                pnlLbl.Controls.Add(star);
            }
            ctrl.Dock = DockStyle.Top; ctrl.Height = 26;
            cell.Controls.Add(ctrl);
            cell.Controls.Add(pnlLbl);
            tbl.Controls.Add(cell, col, 0);
        }

        protected static TextBox MakeReadonly()
            => new TextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(238, 242, 250),
                ForeColor = Color.FromArgb(80, 100, 130),
                Font = FInput,
                BorderStyle = BorderStyle.FixedSingle
            };

        protected static TextBox MakeInput(string placeholder = "")
        {
            var tb = new TextBox { BackColor = Color.White, ForeColor = TextPrincipal, Font = FInput, BorderStyle = BorderStyle.FixedSingle };
            if (!string.IsNullOrEmpty(placeholder)) SetPlaceholder(tb, placeholder);
            return tb;
        }

        protected static TextBox MakeMultiline(int height = 80)
            => new TextBox
            {
                Multiline = true,
                Height = height,
                BackColor = Color.White,
                ForeColor = TextPrincipal,
                Font = FInput,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };

        protected static DateTimePicker MakeDtp()
            => new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = FInput, Height = 26 };

        protected static ComboBox MakeCombo()
            => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = FInput, BackColor = Color.White, ForeColor = TextPrincipal };

        protected static void SetPlaceholder(TextBox tb, string ph)
        {
            tb.Text = ph; tb.ForeColor = Color.Gray;
            tb.Font = new Font(FInput, FontStyle.Regular);
            tb.GotFocus += (s, e) => { if (tb.ForeColor == Color.Gray) { tb.Text = ""; tb.ForeColor = TextPrincipal; tb.Font = FInput; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = ph; tb.ForeColor = Color.Gray; tb.Font = new Font(FInput, FontStyle.Regular); } };
        }

        protected static string GetText(TextBox tb) => tb.ForeColor == Color.Gray ? string.Empty : tb.Text.Trim();
        protected static DateTime GetDate(DateTimePicker dtp) => dtp.Value.Date;

        protected static bool RequireText(TextBox tb, string name)
        {
            if (string.IsNullOrWhiteSpace(GetText(tb)))
            {
                MessageBox.Show(string.Format("Câmpul \"{0}\" este obligatoriu.", name),
                    "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus(); return false;
            }
            return true;
        }

        protected static bool ValidateCodInregistrare(TextBox tb)
        {
            string val = GetText(tb);
            if (string.IsNullOrWhiteSpace(val)) return true;
            var match = System.Text.RegularExpressions.Regex.Match(val, @"^(\d{2})(\d{3})/(\d+)$");
            if (!match.Success)
            {
                MessageBox.Show("Codul de înregistrare trebuie să fie în formatul YYddd/#nr\nExemplu: 26001/1",
                    "Format invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus(); return false;
            }
            int zi = int.Parse(match.Groups[2].Value);
            if (zi < 1 || zi > 366)
            {
                MessageBox.Show("Ziua din an (ddd) trebuie să fie între 001 și 366.",
                    "Format invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus(); return false;
            }
            return true;
        }

        protected void FillAngajatorFields(PvModelBase m)
        {
            m.NumeAngajator = PluginConfig.NumeAngajator;
            m.CIFAngajator = PluginConfig.CIFAngajator;
            m.ReprezentantLegal = PluginConfig.ReprezentantLegal;
            m.FunctieReprezentant = PluginConfig.FunctieReprezentant;
            m.AdresaCompanie = PluginConfig.AdresaCompanie;
            m.ZipCompanie = PluginConfig.ZipCompanie;
            m.NrRegComertului = PluginConfig.NrRegComertului;
            m.IbanCompanie = PluginConfig.IbanCompanie;
            m.NrTelefonCompanie = PluginConfig.NrTelefonCompanie;
            m.EmailCompanie = PluginConfig.EmailCompanie;
            m.WebsiteCompanie = PluginConfig.WebsiteCompanie;
        }

        /// <summary>Dropdown cu toate tipurile de predare-primire.</summary>
        protected ComboBox MakeTipPredareCombo()
        {
            var cmb = MakeCombo();
            cmb.Items.AddRange(new[]
            {
                "Predare-primire",
                "Predare-primire în exploatare",
                "Predare-primire mentenanță",
                "Predare-primire în custodie",
                "Predare-primire administrare",
                "Predare-primire recepție",
                "Predare-primire relocare",
                "Predare-primire casare / scoatere din uz"
            });
            cmb.SelectedIndex = 0;
            return cmb;
        }

        protected static TipPredare GetTipPredare(ComboBox cmb)
        {
            return (TipPredare)cmb.SelectedIndex;
        }
    }
}