using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.UI
{
    // ══════════════════════════════════════════════════════════
    //  LIST ITEM DIALOG
    //  Fereastra de adaugare / editare a unui element dintr-o
    //  dynamic_list cu "edit_mode": "dialog". Gazduieste un
    //  DynamicListRow in modul editor (doar campurile).
    //  La inchidere editorul e scos din fereastra, ca sa nu fie
    //  distrus odata cu ea (ramane purtatorul datelor elementului).
    // ══════════════════════════════════════════════════════════
    public sealed class ListItemDialog : Form
    {
        private const int HeaderH = 52;
        private const int FooterH = 60;
        private const int PadX = 24;
        private const int PadY = 16;

        public ListItemDialog(string titlu, DynamicListRow editor, DocumentTheme theme)
        {
            Text = titlu;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);

            // ── Antet ─────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = HeaderH, BackColor = theme.Accent };
            pnlHeader.Controls.Add(new Label
            {
                Text = titlu,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(PadX - 4, 14)
            });

            // ── Subsol ────────────────────────────────────────
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = FooterH, BackColor = Color.FromArgb(248, 249, 252) };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(theme.AccentBorder))
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var btnSalveaza = new Button
            {
                Text = "✓  Salvează",
                Size = new Size(140, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Top = 12
            };
            btnSalveaza.FlatAppearance.BorderColor = theme.AccentDark;
            btnSalveaza.FlatAppearance.BorderSize = 1;
            btnSalveaza.MouseEnter += (s, e) => btnSalveaza.BackColor = theme.AccentDark;
            btnSalveaza.MouseLeave += (s, e) => btnSalveaza.BackColor = theme.Accent;
            btnSalveaza.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            var btnAnuleaza = new Button
            {
                Text = "Anulează",
                Size = new Size(110, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 80, 110),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Top = 12
            };
            btnAnuleaza.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnAnuleaza.FlatAppearance.BorderSize = 1;
            btnAnuleaza.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.Add(btnSalveaza);
            pnlFooter.Controls.Add(btnAnuleaza);
            pnlFooter.Resize += (s, e) =>
            {
                btnSalveaza.Left = pnlFooter.Width - btnSalveaza.Width - PadX;
                btnAnuleaza.Left = btnSalveaza.Left - btnAnuleaza.Width - 10;
            };

            // ── Corp (campurile) ──────────────────────────────
            var pnlBody = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            editor.Left = PadX;
            editor.Top = PadY;
            pnlBody.Controls.Add(editor);
            pnlBody.Resize += (s, e) =>
                editor.Width = Math.Max(pnlBody.ClientSize.Width - 2 * PadX, 300);

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            // Dimensiune: latime fixa, inaltime dupa continut (cu scroll daca nu incape)
            int bodyH = editor.Height + 2 * PadY;
            int maxBody = Math.Max(Screen.PrimaryScreen.WorkingArea.Height - HeaderH - FooterH - 120, 200);
            ClientSize = new Size(880, HeaderH + Math.Min(bodyH, maxBody) + FooterH);
            editor.Width = ClientSize.Width - 2 * PadX - (bodyH > maxBody ? SystemInformation.VerticalScrollBarWidth : 0);

            CancelButton = btnAnuleaza;

            // Editorul nu trebuie distrus odata cu fereastra
            FormClosed += (s, e) => pnlBody.Controls.Remove(editor);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  LIST ITEM CARD
    //  Elementul afisat in formular pentru o lista "dialog":
    //  needitabil, restrans implicit (antet cu numar + rezumat),
    //  se poate extinde pentru a vedea toate valorile.
    //  Butoane: Editeaza (deschide ListItemDialog), Sterge.
    // ══════════════════════════════════════════════════════════
    public class ListItemCard : Panel
    {
        private const int HdrH = 38;
        private const int CapH = 24;
        private const int CellH = 42;

        private static readonly Font FNum = new Font("Segoe UI Semibold", 10f);
        private static readonly Font FSum = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font FCap = new Font("Segoe UI", 8f, FontStyle.Bold);
        private static readonly Font FLbl = new Font("Segoe UI", 8.5f);
        private static readonly Font FVal = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Color CBorder = Color.FromArgb(195, 165, 225);
        private static readonly Color CLbl = Color.FromArgb(90, 105, 130);
        private static readonly Color CVal = Color.FromArgb(25, 35, 55);

        private readonly DocumentTheme _theme;
        private readonly Panel _hdr;
        private readonly Button _btnEdit;
        private readonly Button _btnDelete;
        private DynamicListRow _row;
        private bool _expanded;
        private int _numar;

        public Action OnEdit { get; set; }
        public Action OnDelete { get; set; }
        public Action OnLayoutChanged { get; set; }

        public DynamicListRow Row
        {
            get { return _row; }
            set { _row = value; UpdateHeight(); Invalidate(true); }
        }

        public int Numar
        {
            get { return _numar; }
            set { _numar = value; _hdr.Invalidate(); }
        }

        public ListItemCard(DynamicListRow row, DocumentTheme theme)
        {
            _row = row;
            _theme = theme;
            _numar = row.Numar;
            BackColor = Color.White;
            DoubleBuffered = true;

            _hdr = new Panel
            {
                Left = 4,
                Top = 1,
                Height = HdrH - 2,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(243, 238, 251)
            };
            _hdr.Paint += PaintHeader;
            _hdr.Click += (s, e) => Toggle();

            _btnEdit = new Button
            {
                Text = "✎  Editează",
                Size = new Size(108, 28),
                Top = 4,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = theme.AccentDark,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnEdit.FlatAppearance.BorderColor = theme.AccentBorder;
            _btnEdit.FlatAppearance.BorderSize = 1;
            _btnEdit.Click += (s, e) => { if (OnEdit != null) OnEdit(); };

            _btnDelete = new Button
            {
                Text = "✕",
                Size = new Size(30, 28),
                Top = 4,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(180, 50, 40),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += (s, e) => { if (OnDelete != null) OnDelete(); };

            _hdr.Controls.Add(_btnEdit);
            _hdr.Controls.Add(_btnDelete);
            Controls.Add(_hdr);

            Resize += (s, e) =>
            {
                _hdr.Width = Math.Max(Width - 5, 100);
                _btnDelete.Left = _hdr.Width - _btnDelete.Width - 6;
                _btnEdit.Left = _btnDelete.Left - _btnEdit.Width - 6;
                Invalidate();
            };

            UpdateHeight();
        }

        private void Toggle()
        {
            _expanded = !_expanded;
            UpdateHeight();
            Invalidate(true);
            if (OnLayoutChanged != null) OnLayoutChanged();
        }

        private List<List<ItemFieldDefinition>> FieldRows()
        {
            return DynamicListRow.GroupRows(_row.FieldDef.ItemFields);
        }

        private void UpdateHeight()
        {
            if (!_expanded) { Height = HdrH; return; }
            int h = HdrH + 8;
            foreach (var r in FieldRows())
            {
                if (!string.IsNullOrEmpty(r[0].Group)) h += CapH;
                h += CellH;
            }
            Height = h + 8;
        }

        private string BuildSummary(Dictionary<string, string> vals)
        {
            var keys = _row.FieldDef.SummaryFields != null && _row.FieldDef.SummaryFields.Count > 0
                ? _row.FieldDef.SummaryFields
                : _row.FieldDef.ItemFields.Take(3).Select(f => f.Key).ToList();
            var parts = new List<string>();
            foreach (var k in keys)
            {
                string v;
                if (vals.TryGetValue(k, out v) && !string.IsNullOrWhiteSpace(v)) parts.Add(v);
            }
            return parts.Count > 0 ? string.Join("  ·  ", parts) : "(necompletat)";
        }

        private void PaintHeader(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Iconita de extindere: sageata simpla (jos = restrans, sus = extins)
            DrawExpandIcon(g, new Rectangle(9, 8, 20, 20), _expanded);

            using (var br = new SolidBrush(_theme.AccentDark))
                g.DrawString(_numar + ".", FNum, br, 36, 9);

            // Titlul elementului: bold, uppercase
            var vals = _row.GetValues();
            string titlu = BuildSummary(vals).ToUpper(new System.Globalization.CultureInfo("ro-RO"));
            using (var br = new SolidBrush(CVal))
            using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                float w = Math.Max(_btnEdit.Left - 8 - 64, 40);
                g.DrawString(titlu, FSum, br, new RectangleF(64, 10, w, 20), sf);
            }
        }

        // Sageata "V" simpla, cu capete drepte (ca modelul primit de la utilizator)
        private void DrawExpandIcon(Graphics g, Rectangle r, bool expanded)
        {
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            const float half = 7f;   // jumatate din latimea sagetii
            const float h = 3.8f;    // jumatate din inaltimea sagetii
            var pts = expanded
                ? new[] { new PointF(cx - half, cy + h), new PointF(cx, cy - h), new PointF(cx + half, cy + h) }
                : new[] { new PointF(cx - half, cy - h), new PointF(cx, cy + h), new PointF(cx + half, cy - h) };
            using (var pen = new Pen(_theme.AccentDark, 2.4f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Flat,
                EndCap = System.Drawing.Drawing2D.LineCap.Flat,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Miter
            })
                g.DrawLines(pen, pts);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;

            using (var pen = new Pen(CBorder))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            using (var br = new SolidBrush(_theme.Accent))
                g.FillRectangle(br, 0, 0, 4, Height);

            if (!_expanded) return;

            var vals = _row.GetValues();
            int left = 18, right = Width - 14;
            int width = Math.Max(right - left, 100);
            int y = HdrH + 8;

            using (var brCap = new SolidBrush(_theme.AccentDark))
            using (var penCap = new Pen(Color.FromArgb(220, 205, 240)))
            using (var brLbl = new SolidBrush(CLbl))
            using (var brVal = new SolidBrush(CVal))
            using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                foreach (var row in FieldRows())
                {
                    if (!string.IsNullOrEmpty(row[0].Group))
                    {
                        g.DrawString(row[0].Group.ToUpper(), FCap, brCap, left, y + 4);
                        g.DrawLine(penCap, left, y + CapH - 3, right, y + CapH - 3);
                        y += CapH;
                    }

                    int totalPct = row.Sum(f => f.WidthPercent > 0 ? f.WidthPercent : 0);
                    int rem = row.Count(f => f.WidthPercent == 0);
                    int remPct = rem > 0 ? (100 - totalPct) / rem : 0;

                    float x = left;
                    foreach (var f in row)
                    {
                        int pct = f.WidthPercent > 0 ? f.WidthPercent : remPct;
                        float cw = width * pct / 100f;
                        string v;
                        if (!vals.TryGetValue(f.Key, out v) || string.IsNullOrWhiteSpace(v)) v = "—";
                        g.DrawString(f.Label ?? f.Key, FLbl, brLbl, new RectangleF(x, y + 2, cw - 12, 16), sf);
                        g.DrawString(v, FVal, brVal, new RectangleF(x, y + 19, cw - 12, 20), sf);
                        x += cw;
                    }
                    y += CellH;
                }
            }
        }
    }
}
