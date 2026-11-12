using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;
using Newtonsoft.Json.Linq;
using PdfiumViewer;

namespace ActAditionalPlugin.UI.Web
{
    // ══════════════════════════════════════════════════════════
    //  FORMULARUL UNUI DOCUMENT
    //  stanga (40%) = pagina web (form.html), generata din JSON-ul
    //  documentului; dreapta (60%) = PDF-ul real (PdfiumViewer).
    //
    //  Mesaje pagina -> C#: cancel, change, regDate, preview, generate,
    //    generateConfirmed / generateCancelled, close, pickPerson,
    //    loadClauzeConfig, saveClauzeConfig
    //  C# -> pagina: init, changed, cod, previewDone, confirmGenerate,
    //    generated, generateDone, personPicked, clauze, clauzeConfig, clauzeSaved
    // ══════════════════════════════════════════════════════════
    public sealed class WebDocumentForm : WebHostForm
    {
        private readonly DocumentDefinition _def;
        private readonly CommonDocumentValues _common;
        private readonly List<PersonInfo> _persoane;
        private readonly DocumentTheme _theme;

        private PdfViewer _pdfViewer;
        private Label _lblPlaceholder;
        private SplitContainer _split;
        private string _previewPdfPath;

        public WebDocumentForm(DocumentDefinition def, CommonDocumentValues common,
            List<PersonInfo> persoane) : base("form.html")
        {
            _def = def;
            _common = common;
            _persoane = persoane ?? new List<PersonInfo>();
            _theme = DocumentTheme.ForCategory(def.Category);

            Text = def.Title;
            Size = new Size(1280, 800);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            BuildShell();

            Shown += (s, e) =>
            {
                _split.SplitterDistance = (int)(_split.Width * 0.40);
                StartWebView();
            };
            FormClosed += (s, e) =>
            {
                try { if (_pdfViewer.Document != null) _pdfViewer.Document.Dispose(); } catch { }
                DocumentGenerationService.TryDeleteFile(_previewPdfPath);
            };
        }

        // ── split 40 / 60 ─────────────────────────────────────
        private void BuildShell()
        {
            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = true,
                SplitterWidth = 3,
                BackColor = Color.FromArgb(185, 193, 207)
            };
            AddWebView(_split.Panel1);

            _lblPlaceholder = new Label
            {
                Text = "↺  Previzualizarea PDF apare aici",
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(150, 160, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(228, 231, 237)
            };
            _pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                ShowToolbar = true,
                ShowBookmarks = false,
                ZoomMode = PdfViewerZoomMode.FitBest,
                Visible = false
            };
            _split.Panel2.Controls.Add(_pdfViewer);
            _split.Panel2.Controls.Add(_lblPlaceholder);
            Controls.Add(_split);

            _split.Resize += (s, e) =>
            {
                if (_split.Width > 0) _split.SplitterDistance = (int)(_split.Width * 0.40);
            };
        }

        // ══════════════════════════════════════════════════════
        //  MESAJE DE LA PAGINA
        //  Dialogurile modale si operatiile lungi pornesc prin BeginInvoke
        //  (dupa ce handler-ul WebView2 se termina).
        // ══════════════════════════════════════════════════════
        protected override void HandleMessage(string type, JObject msg)
        {
            switch (type)
            {
                case "cancel":
                    DialogResult = DialogResult.Cancel;
                    Close();
                    break;
                case "close":       // dupa fereastra de succes
                    DialogResult = DialogResult.OK;
                    Close();
                    break;
                case "change": HandleChange(msg); break;
                case "pickPerson": PickPerson(msg); break;
                case "regDate": RecalcCodFromPage((string)msg["value"]); break;
                case "preview": BeginInvoke(new Action(() => Preview(msg))); break;
                case "generate": BeginInvoke(new Action(() => Generate(msg))); break;
                case "generateConfirmed": BeginInvoke(new Action(GenerateConfirmed)); break;
                case "generateCancelled": _pendingGenerate = null; break;
                case "loadClauzeConfig": BeginInvoke(new Action(LoadClauzeConfig)); break;
                case "saveClauzeConfig": BeginInvoke(new Action(() => SaveClauzeConfig(msg))); break;
            }
        }

        // ── "init": definitia documentului + angajatul + tema + persoanele ──
        protected override void SendInit()
        {
            JObject definition;
            try
            {
                definition = JObject.Parse(File.ReadAllText(_def.JsonPath, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "JSON-ul documentului nu a putut fi citit:\n" + ex.Message,
                    "Generator Documente HR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var c = _common;
            Post(new JObject
            {
                ["type"] = "init",
                ["initialValues"] = BuildInitialValues(),
                ["title"] = _def.Title,
                ["category"] = _def.Category ?? "",
                ["cod"] = c.CodInregistrare ?? "",
                ["definition"] = definition,
                ["employee"] = new JObject
                {
                    ["NumeSalariat"] = c.NumeSalariat ?? "",
                    ["CNP"] = c.CNP ?? "",
                    ["Functie"] = c.Functie ?? "",
                    ["NumeDepartament"] = c.NumeDepartament ?? "",
                    ["NrCim"] = c.NrCim ?? "",
                    ["DataCim"] = c.DataCim != DateTime.MinValue ? c.DataCim.ToString("dd.MM.yyyy") : "",
                    ["SerieCI"] = c.SerieCI ?? "",
                    ["NrCI"] = c.NrCI ?? "",
                    ["Domiciliu"] = c.Domiciliu ?? "",
                    ["NumarTelefonSalariat"] = c.TelefonSalariat ?? "",
                    ["EmailSalariat"] = c.EmailSalariat ?? ""
                },
                ["persons"] = PersonList.ToJson(_persoane),
                ["clauze"] = HasClauzeEditor ? BuildClauze() : new JArray(),
                ["theme"] = ThemeJson(_theme)
            });
        }

        // ══════════════════════════════════════════════════════
        //  VALORI INITIALE: precompletare din ERP + hooks "on_open".
        //  Datele sunt in format dd.MM.yyyy (pagina le converteste).
        // ══════════════════════════════════════════════════════
        private JObject BuildInitialValues()
        {
            var vals = new Dictionary<string, object>();
            var keys = new HashSet<string>(_def.Sections
                .SelectMany(s => s.Fields).Select(f => f.Key).Where(k => k != null));

            var c = _common;
            var common = new Dictionary<string, string>
            {
                { "CISeria", c.SerieCI }, { "SerieCI", c.SerieCI },
                { "CINr", c.NrCI }, { "NrCI", c.NrCI },
                { "Domiciliu", c.Domiciliu }, { "NumeSalariat", c.NumeSalariat },
                { "CNP", c.CNP }, { "Functie", c.Functie },
                { "NumeDepartament", c.NumeDepartament }, { "NrCim", c.NrCim },
                { "NumarTelefonSalariat", c.TelefonSalariat }, { "EmailSalariat", c.EmailSalariat },
                { "NumeAngajator", c.NumeAngajator }, { "AdresaCompanie", c.AdresaCompanie },
                { "NrRegComertului", c.NrRegComertului }, { "CIFAngajator", c.CIFAngajator },
                { "ReprezentantLegal", c.ReprezentantLegal },
                // Carte de identitate — din EMPLOYEE
                { "AutoritateEmitenta", c.AutoritateEmitentaCI },
                { "DataEliberareCI", c.DataEliberareCI != DateTime.MinValue
                                         ? c.DataEliberareCI.ToString("dd.MM.yyyy") : string.Empty },
            };
            foreach (var kv in common)
                if (keys.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                    vals[kv.Key] = kv.Value;

            RunHooksSafe("on_open", vals);

            var o = new JObject();
            foreach (var kv in vals)
                o[kv.Key] = kv.Value != null ? kv.Value.ToString() : "";
            return o;
        }

        // Pagina trimite valorile campurilor simple; rulam hooks "on_change"
        // si trimitem inapoi toate valorile (pagina aplica doar ce s-a schimbat).
        private void HandleChange(JObject msg)
        {
            var vals = new Dictionary<string, object>();
            var jv = msg["values"] as JObject;
            if (jv != null)
                foreach (var p in jv.Properties())
                    vals[p.Name] = (string)p.Value;

            RunHooksSafe("on_change", vals);

            var o = new JObject();
            foreach (var kv in vals)
                o[kv.Key] = kv.Value != null ? kv.Value.ToString() : "";
            Post(new JObject { ["type"] = "changed", ["values"] = o });
        }

        private void RunHooksSafe(string ev, Dictionary<string, object> vals)
        {
            try
            {
                HookRegistry.RunHooks(ev, new HookContext
                {
                    Definition = _def,
                    FormValues = vals,
                    Common = _common,
                    XSupport = BulkContext.XSupport
                });
            }
            catch (Exception ex)
            {
                ShowError("Eroare la rularea hooks \"" + ev + "\":\n" + ex.Message);
            }
        }

        // ── cod de inregistrare (antetul paginii) ─────────────
        private void PostCod()
        {
            Post(new JObject { ["type"] = "cod", ["cod"] = _common.CodInregistrare ?? "" });
        }

        // Pagina trimite data de inregistrare (dd.MM.yyyy) cand se schimba
        private void RecalcCodFromPage(string data)
        {
            DateTime d;
            if (!DateTime.TryParse(data, out d)) d = DateTime.Today;
            DocumentGenerationService.RecalcCod(_common, d);
            PostCod();
        }

        // ══════════════════════════════════════════════════════
        //  ALEGERE PERSOANA: pagina alege persoana (selectorul web) si
        //  trimite { requestId, prsnId, maps: { cheieTinta: "Proprietate[:upper]" } };
        //  C# calculeaza valorile (inclusiv :upper) -> "personPicked".
        // ══════════════════════════════════════════════════════
        private void PickPerson(JObject msg)
        {
            var res = new JObject { ["type"] = "personPicked", ["requestId"] = msg["requestId"] };
            int id;
            int.TryParse(Str(msg["prsnId"]), out id);
            var p = _persoane.FirstOrDefault(x => x.PrsnId == id);
            if (p == null)
            {
                res["cancelled"] = true;
                Post(res);
                return;
            }

            res["prsnId"] = p.PrsnId;
            res["numeComplet"] = p.NumeComplet ?? "";
            var values = new JObject();
            var maps = msg["maps"] as JObject;
            if (maps != null)
                foreach (var m in maps.Properties())
                    values[m.Name] = PersonList.Value(p, (string)m.Value);
            res["values"] = values;
            Post(res);
        }

        // ══════════════════════════════════════════════════════
        //  VALORI PAGINA -> formValues (formatul asteptat de
        //  DynamicTemplateEngine si de hooks)
        //  msg.values = campuri simple, deja in formatul documentului (dd.MM.yyyy ...)
        //  msg.lists  = { cheieLista: [ { values: {...}, extra: {...} } ] }
        //               cu valori brute: date ISO, date_range [a,b],
        //               check_text {on,text}, numere ca text invariant
        //  msg.clauze = { cheie: [ {referinta, text} ] } (Act Aditional)
        // ══════════════════════════════════════════════════════
        private static readonly System.Globalization.CultureInfo Ro =
            new System.Globalization.CultureInfo("ro-RO");

        private Dictionary<string, object> BuildFormValues(JObject msg)
        {
            var values = new Dictionary<string, object>();

            var jv = msg["values"] as JObject;
            if (jv != null)
                foreach (var p in jv.Properties())
                    values[p.Name] = Str(p.Value);

            var jl = msg["lists"] as JObject;
            foreach (var f in _def.Sections.SelectMany(s => s.Fields)
                .Where(f => f.Type == "dynamic_list"))
            {
                var rows = new List<Dictionary<string, string>>();
                var arr = jl != null ? jl[f.Key] as JArray : null;
                if (arr != null)
                {
                    int nr = 0;
                    foreach (var it in arr)
                    {
                        nr++;
                        var row = ConvertItem(f, it as JObject, nr);
                        if (row != null) rows.Add(row);
                    }
                }
                values[f.Key] = rows;
            }

            var jc = msg["clauze"] as JObject;
            foreach (var f in _def.Sections.SelectMany(s => s.Fields)
                .Where(f => f.Type == "clauze_editor"))
            {
                var rows = new List<Dictionary<string, string>>();
                var arr = jc != null ? jc[f.Key] as JArray : null;
                if (arr != null)
                {
                    int nr = 0;
                    foreach (var it in arr)
                    {
                        var o = it as JObject;
                        if (o == null) continue;
                        string text = Str(o["text"]).Trim();
                        if (text.Length == 0) continue;
                        nr++;
                        rows.Add(new Dictionary<string, string>
                        {
                            { "ModificareNr", nr.ToString() },
                            { "ModificareReferinta", Str(o["referinta"]).Trim() },
                            { "ModificareText", text }
                        });
                    }
                }
                values[f.Key] = rows;
            }

            // Mentiunile vin din pagina (comutatorul "Mentiuni"); "" daca e oprit
            if (!values.ContainsKey("MentiuniDocument"))
                values["MentiuniDocument"] = string.Empty;
            return values;
        }

        private static Dictionary<string, string> ConvertItem(FieldDefinition f, JObject item, int nr)
        {
            if (item == null || f.ItemFields == null || f.ItemFields.Count == 0) return null;
            var vals = item["values"] as JObject ?? new JObject();
            var extra = item["extra"] as JObject;

            // Rand valid = primul camp completat
            var first = f.ItemFields[0];
            if ((first.Type == "text" || first.Type == "person_picker")
                && string.IsNullOrWhiteSpace(Str(vals[first.Key])))
                return null;

            var row = new Dictionary<string, string>();
            foreach (var itf in f.ItemFields)
            {
                var tok = vals[itf.Key];
                row[itf.Key] = ItemValue(itf, tok);

                // "date" cu maps: bucati din data (an / luna / zi ...)
                if (itf.Type == "date" && itf.Maps != null && itf.Maps.Count > 0)
                {
                    DateTime d;
                    if (TryIso(Str(tok), out d))
                        foreach (var map in itf.Maps)
                            row[map.Key] = d.ToString(map.Value, Ro);
                }
            }

            if (extra != null)
                foreach (var p in extra.Properties())
                    if (!row.ContainsKey(p.Name)) row[p.Name] = Str(p.Value);

            if (!row.ContainsKey("NrCrt")) row["NrCrt"] = nr.ToString();
            return row;
        }

        private static string ItemValue(ItemFieldDefinition itf, JToken tok)
        {
            switch (itf.Type)
            {
                case "date":
                {
                    DateTime d;
                    return TryIso(Str(tok), out d) ? d.ToString("dd.MM.yyyy") : string.Empty;
                }
                case "date_range":
                {
                    var arr = tok as JArray;
                    if (arr == null || arr.Count == 0) return string.Empty;
                    DateTime a, b;
                    if (!TryIso(Str(arr[0]), out a)) return string.Empty;
                    if (arr.Count < 2 || !TryIso(Str(arr[1]), out b)) b = a;
                    return a.ToString("dd.MM.yyyy") + " - " + b.ToString("dd.MM.yyyy");
                }
                case "check_text":
                {
                    var o = tok as JObject;
                    if (o == null) return string.Empty;
                    var on = o["on"];
                    bool bifat = on != null && on.Type == JTokenType.Boolean && (bool)on;
                    return bifat ? Str(o["text"]).Trim() : string.Empty;
                }
                case "number":
                {
                    decimal m;
                    if (!decimal.TryParse(Str(tok), System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.InvariantCulture, out m))
                        m = 0m;
                    if (itf.Decimals > 0) m = Math.Round(m, itf.Decimals);
                    return !string.IsNullOrEmpty(itf.Format) ? m.ToString(itf.Format, Ro) : m.ToString();
                }
                default:
                    return Str(tok).Trim();
            }
        }

        private static bool TryIso(string s, out DateTime d)
        {
            return DateTime.TryParseExact(s ?? string.Empty, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out d);
        }

        // ══════════════════════════════════════════════════════
        //  PREVIZUALIZARE PDF (dreapta)
        //  Ruleaza IN FUNDAL (completare DOCX + conversie Word pe firul Word),
        //  ca formularul sa ramana utilizabil. Pagina o cere automat dupa o
        //  pauza ("auto": true) sau la butonul Previzualizeaza. O cerere venita
        //  in timpul generarii se pastreaza (doar ultima) si ruleaza dupa.
        // ══════════════════════════════════════════════════════
        private bool _previewRunning;
        private JObject _pendingPreview;

        // Campurile simple (nu liste) necompletate dupa hooks -> linie punctata
        // in previzualizare: true = obligatoriu acum (pagina trimite "required"),
        // false = optional.
        private Dictionary<string, bool> BuildEmptyMarkers(JObject msg, Dictionary<string, object> formValues)
        {
            var required = new HashSet<string>();
            var jr = msg["required"] as JArray;
            if (jr != null)
                foreach (var t in jr) required.Add(Str(t));

            var result = new Dictionary<string, bool>();
            foreach (var f in _def.Sections.SelectMany(s => s.Fields))
            {
                if (f == null || string.IsNullOrEmpty(f.Key)) continue;
                if (f.Type == "dynamic_list" || f.Type == "clauze_editor") continue;
                object v;
                string s = formValues.TryGetValue(f.Key, out v) && v != null ? v.ToString() : "";
                bool empty = string.IsNullOrWhiteSpace(s) || (f.Type == "number" && s.Trim() == "0");
                if (empty) result[f.Key] = required.Contains(f.Key);
            }
            return result;
        }

        private void Preview(JObject msg)
        {
            if (IsDisposed) return;
            if (_previewRunning) { _pendingPreview = msg; return; }

            bool auto = msg["auto"] != null && msg["auto"].Type == JTokenType.Boolean && (bool)msg["auto"];
            Dictionary<string, object> formValues;
            Dictionary<string, bool> emptyMarkers;
            try
            {
                formValues = BuildFormValues(msg);
                RunHooksSafe("on_generate", formValues);   // pe firul ferestrei (pot folosi XSupport)
                emptyMarkers = BuildEmptyMarkers(msg, formValues);
            }
            catch (Exception ex)
            {
                PreviewFinished(null, ex, auto);
                return;
            }

            _previewRunning = true;
            var def = _def;
            var common = _common;
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                string pdf = null;
                Exception err = null;
                try { pdf = DocumentGenerationService.GeneratePreviewPdf(def, formValues, common, emptyMarkers); }
                catch (Exception ex) { err = ex; }

                try
                {
                    if (!IsDisposed && IsHandleCreated)
                        BeginInvoke(new Action(() => PreviewFinished(pdf, err, auto)));
                    else
                        DocumentGenerationService.TryDeleteFile(pdf);
                }
                catch
                {
                    DocumentGenerationService.TryDeleteFile(pdf);
                }
            });
        }

        private void PreviewFinished(string pdf, Exception err, bool auto)
        {
            _previewRunning = false;
            if (IsDisposed) { DocumentGenerationService.TryDeleteFile(pdf); return; }

            if (err == null && pdf != null)
            {
                try
                {
                    // pastram pagina pe care se uita utilizatorul
                    int page = 0;
                    try { if (_pdfViewer.Document != null) page = _pdfViewer.Renderer.Page; } catch { }

                    if (_pdfViewer.Document != null)
                    {
                        _pdfViewer.Document.Dispose();
                        _pdfViewer.Document = null;
                    }
                    DocumentGenerationService.TryDeleteFile(_previewPdfPath);

                    _previewPdfPath = pdf;
                    _pdfViewer.Document = PdfDocument.Load(pdf);
                    _pdfViewer.ZoomMode = PdfViewerZoomMode.FitBest;
                    _pdfViewer.Visible = true;
                    _pdfViewer.BringToFront();
                    _lblPlaceholder.Visible = false;
                    try
                    {
                        if (page > 0 && page < _pdfViewer.Document.PageCount)
                            _pdfViewer.Renderer.Page = page;
                    }
                    catch { }
                }
                catch (Exception ex) { err = ex; }
            }

            // eroarea la previzualizarea ceruta cu butonul o arata pagina (fereastra de eroare)
            Post(err != null
                ? new JObject { ["type"] = "previewDone", ["ok"] = false, ["auto"] = auto, ["message"] = err.Message }
                : new JObject { ["type"] = "previewDone", ["ok"] = true });

            if (_pendingPreview != null)
            {
                var next = _pendingPreview;
                _pendingPreview = null;
                Preview(next);
            }
        }

        // ══════════════════════════════════════════════════════
        //  GENERARE PDF FINAL + REGISTRATURA (in doi pasi)
        //  1. "generate": valori + hooks + cod proaspat -> "confirmGenerate"
        //     (pagina arata fereastra de confirmare)
        //  2. "generateConfirmed": PDF -> verificare cod -> registratura
        //     -> "generated" (fereastra de succes; "close" o inchide)
        // ══════════════════════════════════════════════════════
        private sealed class PendingGenerate
        {
            public Dictionary<string, object> Values;
            public DateTime DataReg;
        }
        private PendingGenerate _pendingGenerate;

        private void Generate(JObject msg)
        {
            Dictionary<string, object> formValues;
            try
            {
                formValues = BuildFormValues(msg);
                RunHooksSafe("on_generate", formValues);
            }
            catch (Exception ex)
            {
                GenerateFailed("Valorile din formular nu au putut fi pregătite:\n" + ex.Message);
                return;
            }

            DateTime dataReg = DocumentGenerationService.GetRegistraturaDate(_def, formValues);
            bool cuRegistratura = DocumentGenerationService.UsesRegistratura(_def);

            // Cod proaspat (altcineva poate fi inregistrat un document intre timp)
            if (cuRegistratura)
            {
                DocumentGenerationService.RecalcCod(_common, dataReg);
                PostCod();
            }

            _pendingGenerate = new PendingGenerate { Values = formValues, DataReg = dataReg };
            Post(new JObject
            {
                ["type"] = "confirmGenerate",
                ["title"] = _def.Title,
                ["cod"] = cuRegistratura ? (_common.CodInregistrare ?? "") : "",
                ["data"] = dataReg.ToString("dd.MM.yyyy")
            });
        }

        private void GenerateConfirmed()
        {
            var pg = _pendingGenerate;
            _pendingGenerate = null;
            if (pg == null || IsDisposed) return;

            bool cuRegistratura = DocumentGenerationService.UsesRegistratura(_def);
            Cursor = Cursors.WaitCursor;
            try
            {
                DocumentGenerationService.GenerateAndRegister(_def, pg.Values, _common, pg.DataReg);
                Cursor = Cursors.Default;
                PostCod();
                Post(new JObject
                {
                    ["type"] = "generated",
                    ["title"] = _def.Title,
                    ["cod"] = cuRegistratura ? (_common.CodInregistrare ?? "") : "",
                    ["data"] = pg.DataReg.ToString("dd.MM.yyyy")
                });
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                if (cuRegistratura)
                {
                    try { DocumentGenerationService.RecalcCod(_common, pg.DataReg); PostCod(); } catch { }
                }
                GenerateFailed("Eroare la generarea PDF:\n" + ex.Message);
            }
        }

        private void GenerateFailed(string text)
        {
            Post(new JObject { ["type"] = "generateDone", ["ok"] = false, ["error"] = text });
        }

        // ══════════════════════════════════════════════════════
        //  CLAUZE ACT ADITIONAL (clauze_editor)
        //  Lista clauzelor active ale setului selectat (ClauzeService).
        //  Campurile sunt in ordinea "ordine" — {0}, {1} ... din textul dinamic.
        // ══════════════════════════════════════════════════════
        private bool HasClauzeEditor
        {
            get { return _def.Sections.SelectMany(s => s.Fields).Any(f => f.Type == "clauze_editor"); }
        }

        private static JArray BuildClauze()
        {
            var arr = new JArray();
            ClauzeConfig cfg;
            try { cfg = ClauzeService.Load(); }
            catch { return arr; }
            var tip = cfg != null ? cfg.GetTipSelectat() : null;
            if (tip == null || tip.Clauze == null) return arr;

            foreach (var cl in tip.Clauze.Where(x => x != null && x.Activ))
            {
                var campuri = new JArray();
                foreach (var cp in (cl.Campuri ?? new List<CampClauza>()).OrderBy(x => x.Ordine))
                    campuri.Add(new JObject
                    {
                        ["label"] = cp.Label ?? "",
                        ["placeholder"] = cp.Placeholder ?? "",
                        ["tip"] = cp.TipCamp ?? "text"
                    });
                arr.Add(new JObject
                {
                    ["id"] = cl.Id ?? "",
                    ["titlu"] = cl.Titlu ?? "(fără titlu)",
                    ["referinta"] = cl.TextClauza ?? "",
                    ["text"] = cl.TextDinamic ?? "",
                    ["campuri"] = campuri
                });
            }
            return arr;
        }

        // Editorul de clauze din pagina: configuratia completa -> pagina;
        // la "Salveaza" pagina o trimite inapoi si o scriem cu ClauzeService.
        private void LoadClauzeConfig()
        {
            if (IsDisposed) return;
            try
            {
                var cfg = ClauzeService.Load();
                Post(new JObject
                {
                    ["type"] = "clauzeConfig",
                    ["config"] = JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(cfg))
                });
            }
            catch (Exception ex)
            {
                Post(new JObject { ["type"] = "clauzeConfig", ["error"] = ex.Message });
            }
        }

        private void SaveClauzeConfig(JObject msg)
        {
            if (IsDisposed) return;
            try
            {
                var jcfg = msg["config"] as JObject;
                if (jcfg == null) throw new InvalidOperationException("Configurația lipsește.");
                var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<ClauzeConfig>(jcfg.ToString());
                if (cfg == null || cfg.TipuriContract == null || cfg.TipuriContract.Count == 0)
                    throw new InvalidOperationException("Trebuie să existe cel puțin un set de clauze.");
                ClauzeService.Save(cfg);
                Post(new JObject { ["type"] = "clauzeSaved", ["ok"] = true });
                Post(new JObject { ["type"] = "clauze", ["clauze"] = BuildClauze() });
            }
            catch (Exception ex)
            {
                Post(new JObject { ["type"] = "clauzeSaved", ["ok"] = false, ["error"] = ex.Message });
            }
        }
    }
}
