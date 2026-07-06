using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using Softone;

namespace ActAditionalPlugin.UI
{
    /// <summary>
    /// Dialog universal de selectare persoana.
    /// Aduce toti angajatii activi din companie cu toate datele relevante.
    /// Refolosibil oriunde apare un camp de tip persoana in formular.
    /// </summary>
    public sealed class PersonPickerDialog : Form
    {
        // ── Rezultat selectie ──────────────────────────────────
        public PersonInfo SelectedPerson { get; private set; }

        // ── SQL pentru toti angajatii activi ──────────────────
        public static readonly string SQL_ANGAJATI =
            "SELECT " +
            "    P.PRSN, " +
            "    P.NAME AS Nume, " +
            "    ISNULL(P.NAME2, '') AS Prenume, " +
            "    P.NAME + ' ' + ISNULL(P.NAME2, '') AS NumeComplet, " +
            "    ISNULL(P.AFM, '') AS CNP, " +
            "    ISNULL(P.SOTITLENAME, '') AS Functie, " +
            "    ISNULL(S.CODE, '') AS CodCor, " +
            "    ISNULL(PEX.CCCVARCHAR05, '') AS NrCim, " +
            "    PEX.DATE03 AS DataCim, " +
            "    ISNULL(D.NAME, '') AS NumeDepartament, " +
            "    ISNULL(P.IDENTITYNUM, '') AS IdentityNum, " +
            "    ISNULL(P.ADDRESS, '') AS Domiciliu " +
            "FROM PRSN P " +
            "JOIN PRSEXTRA PEX ON PEX.PRSN = P.PRSN AND PEX.COMPANY = P.COMPANY " +
            "LEFT JOIN PRSJOBPOS PJ ON PJ.PRSN = P.PRSN AND PJ.COMPANY = P.COMPANY " +
            "LEFT JOIN JOBPOSITION J ON PJ.JOBPOSITION = J.JOBPOSITION " +
            "LEFT JOIN SPECIALTY S ON J.SPECIALTY = S.SPECIALTY " +
            "LEFT JOIN DEPART D ON P.DEPART = D.DEPART AND D.COMPANY = P.COMPANY " +
            "WHERE P.COMPANY = {0} AND P.ISACTIVE = 1 " +
            "ORDER BY P.NAME, P.NAME2";

        // ── Model intern ───────────────────────────────────────
        private readonly List<PersonInfo> _all;
        private List<PersonInfo> _filtered;
        private readonly string _title;
        private readonly string _subtitle;

        // ── Controale ─────────────────────────────────────────
        private TextBox _txtSearch;
        private ListView _lst;
        private Button _btnOk;

        // ── Tema ──────────────────────────────────────────────
        private static readonly Color DarkBg = Color.FromArgb(220, 225, 238);
        private static readonly Color DarkHeader = Color.FromArgb(195, 205, 225);
        private static readonly Color DarkRow1 = Color.FromArgb(232, 236, 248);
        private static readonly Color DarkRow2 = Color.FromArgb(218, 224, 240);
        private static readonly Color Albastru = Color.FromArgb(63, 129, 198);
        private static readonly Color TextDes = Color.FromArgb(15, 25, 50);

        // ══════════════════════════════════════════════════════
        //  Factory — incarca din ERP
        // ══════════════════════════════════════════════════════
        public static List<PersonInfo> LoadFromErp(XSupport xs)
        {
            var result = new List<PersonInfo>();
            try
            {
                int companyId = xs.ConnectionInfo.CompanyId;
                string sql = string.Format(SQL_ANGAJATI, companyId);
                var ds = xs.GetSQLDataSet(sql);
                if (ds == null) return result;

                for (int i = 0; i < ds.Count; i++)
                {
                    var p = new PersonInfo
                    {
                        PrsnId = Convert.ToInt32(ds[i, "PRSN"]),
                        Nume = ds[i, "Nume"]?.ToString()?.Trim() ?? string.Empty,
                        Prenume = ds[i, "Prenume"]?.ToString()?.Trim() ?? string.Empty,
                        NumeComplet = ds[i, "NumeComplet"]?.ToString()?.Trim() ?? string.Empty,
                        CNP = ds[i, "CNP"]?.ToString()?.Trim() ?? string.Empty,
                        Functie = ds[i, "Functie"]?.ToString()?.Trim() ?? string.Empty,
                        CodCor = ds[i, "CodCor"]?.ToString()?.Trim() ?? string.Empty,
                        NrCim = ds[i, "NrCim"]?.ToString()?.Trim() ?? string.Empty,
                        NumeDepartament = ds[i, "NumeDepartament"]?.ToString()?.Trim() ?? string.Empty,
                    };

                    DateTime dataCim;
                    string rawDate = ds[i, "DataCim"]?.ToString() ?? string.Empty;
                    if (DateTime.TryParse(rawDate, out dataCim))
                        p.DataCim = dataCim;

                    // SerieCI + NrCI din IDENTITYNUM
                    string identityNum = ds[i, "IdentityNum"]?.ToString()?.Trim() ?? string.Empty;
                    string serie, nrci;
                    PersonInfo.ParseIdentityNum(identityNum, out serie, out nrci);
                    p.SerieCI = serie;
                    p.NrCI = nrci;

                    p.Domiciliu = ds[i, "Domiciliu"]?.ToString()?.Trim() ?? string.Empty;

                    result.Add(p);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la incarcarea angajatilor: " + ex.Message,
                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
        }

        // ══════════════════════════════════════════════════════
        //  Constructor
        // ══════════════════════════════════════════════════════
        public PersonPickerDialog(List<PersonInfo> persoane,
            string title = "Selectare persoana",
            string subtitle = "Selecteaza persoana din lista")
        {
            _all = persoane ?? new List<PersonInfo>();
            _filtered = new List<PersonInfo>(_all);
            _title = title;
            _subtitle = subtitle;

            Text = title;
            Size = new Size(820, 560);
            MinimumSize = new Size(700, 400);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = DarkBg;
            Font = new Font("Segoe UI", 10f);

            BuildUI();
            PopulateList(_all);
        }

        // ══════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = DarkHeader };
            var lblTitle = new Label
            {
                Text = _title,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 8)
            };
            var lblSub = new Label
            {
                Text = _subtitle,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextDes,
                AutoSize = true,
                Location = new Point(16, 34)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // ── Search ────────────────────────────────────────
            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(10, 6, 10, 4),
                BackColor = Color.White
            };
            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(25, 35, 55)
            };
            SetPlaceholder(_txtSearch, "Cauta dupa nume, functie, CNP...");
            _txtSearch.TextChanged += OnSearchChanged;
            _txtSearch.KeyDown += OnSearchKeyDown;
            pnlSearch.Controls.Add(_txtSearch);

            // ── ListView ──────────────────────────────────────
            _lst = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = DarkRow1,
                ForeColor = TextDes,
                Font = new Font("Segoe UI", 9.5f),
                OwnerDraw = true,
                MultiSelect = false
            };
            _lst.Columns.Add("Nume complet", 220);
            _lst.Columns.Add("Functie", 180);
            _lst.Columns.Add("COR", 70);
            _lst.Columns.Add("CNP", 140);
            _lst.Columns.Add("Nr. CIM", 80);
            _lst.Columns.Add("Data CIM", -2); // -2 = fill remaining width

            _lst.DrawColumnHeader += OnDrawColumnHeader;
            _lst.DrawItem += OnDrawItem;
            _lst.DrawSubItem += OnDrawSubItem;
            _lst.DoubleClick += (s, e) => Confirma();
            _lst.SelectedIndexChanged += OnSelectionChanged;

            // ── Footer ────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(230, 235, 245)
            };
            _btnOk = new Button
            {
                Text = "Selecteaza  →",
                Size = new Size(150, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Albastru,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 8,
                Enabled = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.FlatAppearance.BorderColor = Albastru;
            _btnOk.Click += (s, e) => Confirma();

            var btnCancel = new Button
            {
                Text = "Anuleaza",
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 225, 235),
                ForeColor = Color.FromArgb(60, 75, 100),
                Font = new Font("Segoe UI", 10f),
                Top = 8,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnOk, btnCancel });
            pnlFooter.Resize += (s, e) =>
            {
                _btnOk.Left = pnlFooter.Width - _btnOk.Width - 14;
                btnCancel.Left = _btnOk.Left - btnCancel.Width - 8;
            };

            Controls.Add(_lst);
            Controls.Add(pnlSearch);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            AcceptButton = _btnOk;
            CancelButton = btnCancel;
        }

        // ══════════════════════════════════════════════════════
        //  POPULATE / FILTER
        // ══════════════════════════════════════════════════════
        private void PopulateList(List<PersonInfo> sursa)
        {
            _lst.Items.Clear();
            foreach (var p in sursa)
            {
                var item = new ListViewItem(p.NumeComplet) { Tag = p };
                item.SubItems.Add(p.Functie);
                item.SubItems.Add(p.CodCor);
                item.SubItems.Add(p.CNP);
                item.SubItems.Add(p.NrCim);
                item.SubItems.Add(p.DataCimFormatata);
                _lst.Items.Add(item);
            }
            _btnOk.Enabled = false;
        }

        private bool _suppressSearch = false;

        private void OnSearchChanged(object sender, EventArgs e)
        {
            if (_suppressSearch) return;

            string q = _txtSearch.ForeColor == Color.Gray
                ? string.Empty
                : _txtSearch.Text.Trim().ToLower();

            _filtered = string.IsNullOrEmpty(q)
                ? new List<PersonInfo>(_all)
                : _all.Where(p =>
                    p.NumeComplet.ToLower().Contains(q) ||
                    p.Functie.ToLower().Contains(q) ||
                    p.CNP.Contains(q)).ToList();

            PopulateList(_filtered);

            if (_filtered.Count > 0)
            {
                _lst.Items[0].Selected = true;
                _lst.Items[0].Focused = true;
            }
        }

        private void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _lst.Items.Count > 0)
            {
                _lst.Focus();
                _lst.Items[0].Selected = true;
                e.Handled = true;
            }
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (_lst.SelectedItems.Count == 0) return;
            SelectedPerson = _lst.SelectedItems[0].Tag as PersonInfo;
            _btnOk.Enabled = SelectedPerson != null;
        }

        private void Confirma()
        {
            if (SelectedPerson == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        // ══════════════════════════════════════════════════════
        //  OWNER DRAW — ListView cu tema dark
        // ══════════════════════════════════════════════════════
        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(215, 222, 235)), e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text,
                new Font("Segoe UI", 9f, FontStyle.Bold),
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height),
                Color.FromArgb(55, 75, 105),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private void OnDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // desenul e facut in OnDrawSubItem
            e.DrawDefault = false;
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool sel = e.Item.Selected;
            var bg = sel
                ? Albastru
                : (e.ItemIndex % 2 == 0 ? DarkRow1 : DarkRow2);

            e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);

            var fg = sel ? Color.White : Color.FromArgb(25, 35, 55);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text,
                new Font("Segoe UI", 9.5f),
                new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height),
                fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        // ══════════════════════════════════════════════════════
        //  HELPER
        // ══════════════════════════════════════════════════════
        private void SetPlaceholder(TextBox tb, string ph)
        {
            tb.Text = ph;
            tb.ForeColor = Color.Gray;
            tb.GotFocus += (s, e) =>
            {
                if (tb.ForeColor != Color.Gray) return;
                _suppressSearch = true;
                tb.Text = string.Empty;
                tb.ForeColor = Color.FromArgb(25, 35, 55);
                _suppressSearch = false;
            };
            tb.LostFocus += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(tb.Text)) return;
                _suppressSearch = true;
                tb.Text = ph;
                tb.ForeColor = Color.Gray;
                _suppressSearch = false;
            };
        }
    }
}