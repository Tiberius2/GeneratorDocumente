using System;
using System.Drawing;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;
using System.Diagnostics;

namespace ActAditionalPlugin.UI
{
    public abstract class DocumentFormBase : Form
    {
        public static Action<DocumentModelBase> OnDocumentGenerated;
        protected readonly DocumentModelBase _model;
        protected Panel PnlBody { get; private set; }

        // Tema
        protected static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        protected static readonly Color AlbastruPal = Color.FromArgb(235, 241, 251);
        protected static readonly Color AlbastruBorder = Color.FromArgb(180, 205, 235);
        protected static readonly Color TextPrincipal = Color.FromArgb(25, 35, 55);
        protected static readonly Color TextSecundar = Color.FromArgb(80, 100, 130);
        protected static readonly Color FundalForm = Color.FromArgb(242, 245, 250);
        protected static readonly Font FLabel = new Font("Segoe UI", 9f);
        protected static readonly Font FInput = new Font("Segoe UI", 10f, FontStyle.Bold);
        protected static readonly Font FSectiune = new Font("Segoe UI", 8f, FontStyle.Bold);

        protected DocumentFormBase(DocumentModelBase model, string titlu)
        {
            _model = model;
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
            // ── Titlu ──────────────────────────────────────────
            var pnlTitlu = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Albastru
            };
            pnlTitlu.Controls.Add(new Label
            {
                Text = titlu,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(18, 12)
            });

            // ── Sectiune angajat ───────────────────────────────
            var pnlAngajat = BuildAngajatSection();

            // ── Body scrollabil ────────────────────────────────
            PnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 10, 16, 10),
                BackColor = FundalForm
            };

            // ── Footer ─────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (var p = new Pen(AlbastruBorder))
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
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
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Left = 18
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

        // ── Sectiune angajat (TableLayoutPanel) ───────────────
        private TextBox _txtNume, _txtCnp, _txtNrCim, _txtDataCim, _txtFunctie;

        private Panel BuildAngajatSection()
        {
            var outer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.White,
                Padding = new Padding(16, 6, 16, 6)
            };
            outer.Paint += PaintBottomLine;

            // Header label
            var hdr = MakeSectionHeader("DATE ANGAJAT", 0);
            hdr.Dock = DockStyle.Top;
            hdr.Height = 20;
            outer.Controls.Add(hdr);

            // Rand 2: Nr CIM + Data CIM + Functie (adaugam primul = va fi jos)
            _txtNrCim = MakeReadonly();
            _txtDataCim = MakeReadonly();
            _txtFunctie = MakeReadonly();
            var tbl2 = MakeTableRow(3, new[] { 33, 33, 34 });
            tbl2.Dock = DockStyle.Top;
            AddLabeledCell(tbl2, 0, "Nr. CIM", _txtNrCim);
            AddLabeledCell(tbl2, 1, "Data CIM", _txtDataCim);
            AddLabeledCell(tbl2, 2, "Funcție", _txtFunctie);
            outer.Controls.Add(tbl2);

            // Rand 1: Angajat + CNP (adaugam al doilea = va fi sus)
            _txtNume = MakeReadonly();
            _txtCnp = MakeReadonly();
            var tbl1 = MakeTableRow(2, new[] { 60, 40 });
            tbl1.Dock = DockStyle.Top;
            AddLabeledCell(tbl1, 0, "Angajat", _txtNume);
            AddLabeledCell(tbl1, 1, "CNP", _txtCnp);
            outer.Controls.Add(tbl1);

            return outer;
        }

        private void PopulateAngajat()
        {
            _txtNume.Text = _model.NumeSalariat;
            _txtCnp.Text = _model.CNP;
            _txtNrCim.Text = _model.NrCim;
            _txtDataCim.Text = _model.DataCim != DateTime.MinValue
                ? _model.DataCim.ToString("dd.MM.yyyy") : string.Empty;
            _txtFunctie.Text = _model.Functie;
        }

        // ── Generare PDF ──────────────────────────────────────
        private void BtnGenPdf_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            PopulateModel();

            string tpl = GetTemplatePath();
            if (!System.IO.File.Exists(tpl))
            {
                MessageBox.Show("Template-ul nu a fost găsit:\n" + tpl,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                string pdf = TemplateEngine.GeneratePdf(_model, tpl);
                OnDocumentGenerated?.Invoke(_model);
                Process.Start(pdf);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la generare PDF:\n" + ex.Message,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            PopulateModel();

            string tpl = GetTemplatePath();
            if (!System.IO.File.Exists(tpl))
            {
                MessageBox.Show("Template-ul nu a fost găsit:\n" + tpl,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string tempPdf = GenerateTempPdf(tpl);

                using (var preview = new PdfPreviewForm(tempPdf, deletePdfOnClose: true))
                {
                    preview.ShowDialog(this);

                    if (preview.UserConfirmed)
                    {
                        string finalPdf = TemplateEngine.GeneratePdf(_model, tpl);
                        OnDocumentGenerated?.Invoke(_model);
                        Process.Start(finalPdf);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la previzualizare:\n" + ex.Message,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerateTempPdf(string templatePath)
        {
            string tempDocx = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                string.Format("preview_{0}_{1}.docx", _model.TipDocument, _model.PrsnId));
            string tempPdf = System.IO.Path.ChangeExtension(tempDocx, ".pdf");

            try
            {
                System.IO.File.Copy(templatePath, tempDocx, true);
                TemplateEngine.FillTemplatePublic(tempDocx, _model);
                TemplateEngine.ConvertToPdfPublic(tempDocx, tempPdf);
            }
            finally
            {
                if (System.IO.File.Exists(tempDocx))
                    System.IO.File.Delete(tempDocx);
            }

            return tempPdf;
        }

        protected abstract bool ValidateForm();
        protected abstract void PopulateModel();
        protected abstract string GetTemplatePath();

        // ══════════════════════════════════════════════════════
        //  HELPERS LAYOUT — reutilizate de subclase
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Creeaza o sectiune cu titlu + panel alb si o adauga in PnlBody la yOffset.
        /// Returneaza panoul interior.
        /// </summary>
        protected Panel AddSectiune(string titlu, ref int yOffset, int height)
        {
            // Label titlu sectiune
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

            // FlowLayoutPanel vertical — controalele se stivuiesc automat in ordine
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
            flow.Paint += PaintBorder;
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

        /// <summary>
        /// Agauga un rand de campuri (TableLayoutPanel) intr-un panou la pozitia top.
        /// colPercents: sumă = 100.
        /// </summary>
        protected TableLayoutPanel AddRow(Panel parent, int top, int[] colPercents)
        {
            var tbl = MakeTableRow(colPercents.Length, colPercents);
            tbl.Margin = new Padding(0, 4, 0, 0);
            tbl.Width = Math.Max(parent.ClientSize.Width - parent.Padding.Horizontal, 400);
            parent.Controls.Add(tbl);
            parent.Resize += (s, e) =>
                tbl.Width = Math.Max(parent.ClientSize.Width - parent.Padding.Horizontal, 400);
            return tbl;
        }

        protected void AddLabeledInput(TableLayoutPanel tbl, int col, string label, Control ctrl, bool required = false)
            => AddLabeledCell(tbl, col, label, ctrl, required);

        // ── Helpers privati ───────────────────────────────────
        private static TableLayoutPanel MakeTableRow(int cols, int[] percents)
        {
            var tbl = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = cols,
                Height = 54,
                Dock = DockStyle.None,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < cols; i++)
                tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percents[i]));
            return tbl;
        }

        private static void AddLabeledCell(TableLayoutPanel tbl, int col, string labelText, Control ctrl, bool required = false)
        {
            // Panel exterior cu padding dreapta pentru spatiu intre coloane
            var cell = new Panel
            {
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 8, 0)
            };

            // Panel pentru label + asterisk pe acelasi rand
            var pnlLbl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 18,
                BackColor = Color.Transparent
            };

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

            // Control jos — DockStyle.Top dupa panel label
            ctrl.Dock = DockStyle.Top;
            ctrl.Height = 26;

            // Ordinea adaugarii conteaza: ultimul adaugat e sus in DockStyle.Top
            // Deci adaugam intai ctrl, apoi pnlLbl — pnlLbl va fi deasupra
            cell.Controls.Add(ctrl);
            cell.Controls.Add(pnlLbl);

            tbl.Controls.Add(cell, col, 0);
        }

        private static void LayoutTableInPanel(TableLayoutPanel tbl, Panel parent)
        {
            tbl.Left = parent.Padding.Left;
            tbl.Width = parent.ClientSize.Width - parent.Padding.Horizontal;
        }

        protected static TextBox MakeReadonly()
            => new TextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(238, 242, 250),
                ForeColor = TextSecundar,
                Font = FInput,
                BorderStyle = BorderStyle.FixedSingle
            };

        protected static TextBox MakeInput(string placeholder = "")
        {
            var tb = new TextBox
            {
                BackColor = Color.White,
                ForeColor = TextPrincipal,
                Font = FInput,
                BorderStyle = BorderStyle.FixedSingle
            };
            if (!string.IsNullOrEmpty(placeholder)) SetPlaceholder(tb, placeholder);
            return tb;
        }

        protected static TextBox MakeMultiline(int height = 60)
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
            => new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Font = FInput,
                Height = 26
            };

        protected static ComboBox MakeCombo()
            => new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FInput,
                BackColor = Color.White,
                ForeColor = TextPrincipal
            };

        protected static void SetPlaceholder(TextBox tb, string ph)
        {
            tb.Text = ph;
            tb.ForeColor = Color.Gray;
            tb.Font = new Font(FInput, FontStyle.Regular);
            tb.GotFocus += (s, e) =>
            {
                if (tb.ForeColor == Color.Gray)
                {
                    tb.Text = string.Empty;
                    tb.ForeColor = TextPrincipal;
                    tb.Font = FInput;
                }
            };
            tb.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = ph;
                    tb.ForeColor = Color.Gray;
                    tb.Font = new Font(FInput, FontStyle.Regular);
                }
            };
        }

        protected static string GetText(TextBox tb)
            => tb.ForeColor == Color.Gray ? string.Empty : tb.Text.Trim();

        protected static DateTime GetDate(DateTimePicker dtp) => dtp.Value.Date;

        protected static Label MakeSectionHeader(string text, int top)
            => new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Albastru,
                AutoSize = true,
                Location = new Point(0, top)
            };

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

        /// <summary>
        /// Valideaza formatul codului de inregistrare: YYddd/#nr
        /// YY=2 cifre an, ddd=3 cifre zi (001-366), #nr=numar pozitiv
        /// Exemple: 26001/1, 26107/45, 25366/123
        /// </summary>
        protected static bool ValidateCodInregistrare(TextBox tb)
        {
            string val = GetText(tb);
            if (string.IsNullOrWhiteSpace(val)) return true;

            var match = System.Text.RegularExpressions.Regex.Match(
                val, @"^(\d{2})(\d{3})/(\d+)$");

            if (!match.Success)
            {
                MessageBox.Show(
                    "Codul de înregistrare trebuie să fie în formatul YYddd/#nr\n" +
                    "Exemplu: 26001/1, 26107/45\n\n" +
                    "YY  = ultimele 2 cifre ale anului\n" +
                    "ddd = ziua din an (001-366)\n" +
                    "#nr = numărul documentului",
                    "Format invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus();
                return false;
            }

            int zi = int.Parse(match.Groups[2].Value);
            if (zi < 1 || zi > 366)
            {
                MessageBox.Show(
                    "Ziua din an (ddd) trebuie să fie între 001 și 366.",
                    "Format invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus();
                return false;
            }

            return true;
        }

        protected void FillAngajator(DocumentModelBase m)
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

        private static void PaintBottomLine(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using (var pen = new Pen(Color.FromArgb(210, 220, 235)))
                e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using (var pen = new Pen(Color.FromArgb(200, 215, 235)))
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}