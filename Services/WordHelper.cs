using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ActAditionalPlugin.Services
{
    // Utilitare OpenXML + Word Interop partajate intre TemplateEngine si PvTemplateEngine
    internal static class WordHelper
    {
        // Marker folosit de hook-ul BlankIfNotEquals pentru variantele
        // neselectate (ex. Cerere CO/Vatra). Cand valoarea unui placeholder
        // e exact acest text, run-ul e colorat albastru la generare —
        // valorile reale raman in formatarea originala din template.
        internal const string BlankLineMarker = "_ _ _ _ _ _ _ _ _ _ _ _ _ _ _";
        private const string BlankLineColorHex = "0070C0";

        // Doar la previzualizarea din formularul web: camp OBLIGATORIU necompletat.
        // Acelasi aspect, dar rosiatic; caracterul invizibil (U+200B) il deosebeste
        // de BlankLineMarker.
        internal const string RequiredBlankMarker = BlankLineMarker + "​";
        private const string RequiredBlankColorHex = "C0504D";

        internal static string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "_";
            input = input.Replace('/', '-');
            foreach (char c in Path.GetInvalidFileNameChars())
                input = input.Replace(c, '_');
            return input.Trim();
        }

        internal static string GetText(OpenXmlElement el)
            => string.Concat(el.Descendants<Text>().Select(t => t.Text));

        // ── Replace cu pastrarea formatarii ──────────────────
        internal static void MergeAndReplace(Paragraph para, Dictionary<string, string> map)
        {
            var runs = para.Elements<Run>().ToList();
            if (runs.Count == 0) return;

            var segments = new List<Tuple<string, RunProperties>>();
            foreach (var run in runs)
            {
                string txt = string.Concat(run.Descendants<Text>().Select(t => t.Text));
                RunProperties rpr = run.RunProperties != null
                    ? (RunProperties)run.RunProperties.CloneNode(true) : null;
                segments.Add(Tuple.Create(txt, rpr));
            }

            string fullText = string.Concat(segments.Select(s => s.Item1));
            bool hasAny = map.Keys.Any(k => fullText.Contains(k));
            if (!hasAny && !fullText.Contains("{{")) return;

            foreach (var run in runs) run.Remove();

            string replaced = fullText;
            foreach (var kv in map)
                replaced = replaced.Replace(kv.Key, kv.Value);

            var posToRpr = BuildPosMap(segments);
            foreach (var r in BuildRuns(fullText, replaced, map, posToRpr))
                para.AppendChild(r);
        }

        internal static Run MakeRun(string text, RunProperties rpr)
        {
            var run = new Run();
            if (rpr != null) run.AppendChild((RunProperties)rpr.CloneNode(true));
            var t = new Text(text);
            if (text.StartsWith(" ") || text.EndsWith(" "))
                t.Space = SpaceProcessingModeValues.Preserve;
            run.AppendChild(t);
            return run;
        }

        internal static Run MakeRunWithBreaks(string text, RunProperties rpr)
        {
            var run = new Run();
            if (rpr != null) run.AppendChild((RunProperties)rpr.CloneNode(true));
            string[] lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) run.AppendChild(new Break());
                var t = new Text(lines[i]);
                if (lines[i].StartsWith(" ") || lines[i].EndsWith(" "))
                    t.Space = SpaceProcessingModeValues.Preserve;
                run.AppendChild(t);
            }
            return run;
        }

        private static Dictionary<int, RunProperties> BuildPosMap(List<Tuple<string, RunProperties>> segments)
        {
            var map = new Dictionary<int, RunProperties>();
            int pos = 0;
            foreach (var seg in segments)
            {
                for (int i = 0; i < seg.Item1.Length; i++)
                    map[pos + i] = seg.Item2;
                pos += seg.Item1.Length;
            }
            return map;
        }

        private static List<Run> BuildRuns(string orig, string replaced,
            Dictionary<string, string> map, Dictionary<int, RunProperties> posToRpr)
        {
            var result = new List<Run>();
            var ordered = map.Keys
                .Where(k => orig.Contains(k))
                .OrderBy(k => orig.IndexOf(k, StringComparison.Ordinal))
                .ToList();

            if (ordered.Count == 0)
            {
                result.Add(MakeRun(replaced, posToRpr.ContainsKey(0) ? posToRpr[0] : null));
                return result;
            }

            int origPos = 0, replPos = 0;
            foreach (var ph in ordered)
            {
                int phIdx = orig.IndexOf(ph, origPos, StringComparison.Ordinal);
                if (phIdx < 0) continue;
                string value = map[ph];

                if (phIdx > origPos)
                {
                    int vIdx = replaced.IndexOf(value, replPos, StringComparison.Ordinal);
                    int bLen = vIdx >= 0 ? vIdx - replPos : phIdx - origPos;
                    if (bLen > 0)
                    {
                        result.Add(MakeRun(replaced.Substring(replPos, bLen),
                            posToRpr.ContainsKey(origPos) ? posToRpr[origPos] : null));
                        replPos += bLen;
                    }
                }

                RunProperties phRpr = posToRpr.ContainsKey(phIdx) ? posToRpr[phIdx] : null;
                if (value == BlankLineMarker || value == RequiredBlankMarker)
                {
                    phRpr = phRpr != null ? (RunProperties)phRpr.CloneNode(true) : new RunProperties();
                    phRpr.RemoveAllChildren<Color>();
                    phRpr.PrependChild(new Color
                    {
                        Val = value == RequiredBlankMarker ? RequiredBlankColorHex : BlankLineColorHex
                    });
                }
                if (value.Length > 0)
                {
                    // "~~text~~" -> text taiat cu o linie (ex. varianta nealeasa:
                    // "saptamani / ~~luni~~"); markerii nu apar in document
                    string shown = value;
                    if (IsStrikeValue(value))
                    {
                        shown = value.Substring(2, value.Length - 4);
                        phRpr = phRpr != null ? (RunProperties)phRpr.CloneNode(true) : new RunProperties();
                        AddStrike(phRpr);
                    }
                    result.Add(shown.IndexOf('\n') >= 0 || shown.IndexOf('\r') >= 0
                        ? MakeRunWithBreaks(shown, phRpr)
                        : MakeRun(shown, phRpr));
                    replPos += value.Length;
                }
                origPos = phIdx + ph.Length;
            }

            if (replPos < replaced.Length)
            {
                string after = replaced.Substring(replPos);
                if (after.Length > 0)
                    result.Add(MakeRun(after, posToRpr.ContainsKey(origPos) ? posToRpr[origPos] : null));
            }

            return result.Count > 0 ? result : new List<Run> { MakeRun(replaced, null) };
        }

        // ── Text taiat (strikethrough) ───────────────────────
        // Valoare "~~text~~" (ex. setata de SetIfEquals) => "text" cu <w:strike/>.
        internal static bool IsStrikeValue(string v)
        {
            return v != null && v.Length > 4 && v.StartsWith("~~") && v.EndsWith("~~");
        }

        // <w:strike/> pus in ordinea din schema rPr (inainte de color, sz, u, lang ...)
        private static readonly HashSet<string> AfterStrike = new HashSet<string>
        {
            "dstrike", "outline", "shadow", "emboss", "imprint", "noProof", "snapToGrid",
            "vanish", "webHidden", "color", "spacing", "w", "kern", "position", "sz", "szCs",
            "highlight", "u", "effect", "bdr", "shd", "fitText", "vertAlign", "rtl", "cs",
            "em", "lang", "eastAsianLayout", "specVanish", "oMath"
        };

        private static void AddStrike(RunProperties rpr)
        {
            rpr.RemoveAllChildren<Strike>();
            var before = rpr.ChildElements.FirstOrDefault(c => AfterStrike.Contains(c.LocalName));
            if (before != null) rpr.InsertBefore(new Strike(), before);
            else rpr.AppendChild(new Strike());
        }

        // ── Word Interop → PDF (singleton instance) ───────────
        // Toate apelurile catre Word ruleaza pe un fir STA dedicat (WordThread),
        // nu pe firul ferestrei care le cere. Astfel formularul web poate face
        // previzualizarea in fundal fara sa blocheze interfata, iar instanta
        // Word e folosita mereu de pe acelasi fir (regula COM pentru STA).
        private static object _wordApp;
        private static Type _wordType;
        private static readonly object _wordLock = new object();
        private static readonly object _threadLock = new object();
        private static WordThread _thread;

        private static WordThread Worker
        {
            get
            {
                lock (_threadLock)
                {
                    if (_thread == null) _thread = new WordThread();
                    return _thread;
                }
            }
        }

        internal static void ConvertToPdf(string docxPath, string pdfPath)
        {
            Worker.Invoke(() => ConvertCore(docxPath, pdfPath));
        }

        internal static void ShutdownWordApp()
        {
            WordThread t;
            lock (_threadLock)
            {
                t = _thread;
                _thread = null;
            }
            if (t == null) return;
            try { t.Invoke(ShutdownCore); } catch { }
            t.Stop();
        }

        private static void ConvertCore(string docxPath, string pdfPath)
        {
            lock (_wordLock)
            {
                EnsureWordApp();
                object documents = _wordType.InvokeMember("Documents", BindingFlags.GetProperty, null, _wordApp, null);
                object doc = documents.GetType().InvokeMember("Open", BindingFlags.InvokeMethod, null, documents,
                    new object[] { docxPath, false, true });
                try
                {
                    doc.GetType().InvokeMember("ExportAsFixedFormat", BindingFlags.InvokeMethod, null, doc,
                        new object[] { pdfPath, 17 });
                }
                finally
                {
                    doc.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc, new object[] { false });
                }
            }
        }

        private static void ShutdownCore()
        {
            lock (_wordLock)
            {
                if (_wordApp == null) return;
                try
                {
                    // Inchidem Word doar daca nu are documente deschise (instanta e
                    // a noastra si invizibila, dar nu riscam sa inchidem ceva al userului)
                    object documents = _wordType.InvokeMember("Documents", BindingFlags.GetProperty, null, _wordApp, null);
                    int count = Convert.ToInt32(documents.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, documents, null));
                    if (count == 0)
                        _wordType.InvokeMember("Quit", BindingFlags.InvokeMethod, null, _wordApp, null);
                }
                catch { }
                try { System.Runtime.InteropServices.Marshal.FinalReleaseComObject(_wordApp); }
                catch { }
                _wordApp = null;
                _wordType = null;
            }
        }

        private static void EnsureWordApp()
        {
            if (_wordApp != null)
            {
                // Verifica daca instanta e inca vie
                try
                {
                    _wordType.InvokeMember("Visible", BindingFlags.GetProperty, null, _wordApp, null);
                    return; // e vie, o refolosim
                }
                catch
                {
                    _wordApp = null; // a murit, recreem
                }
            }

            _wordType = Type.GetTypeFromProgID("Word.Application");
            if (_wordType == null)
                throw new InvalidOperationException("Microsoft Word nu este instalat.");

            _wordApp = Activator.CreateInstance(_wordType);
            _wordType.InvokeMember("Visible", BindingFlags.SetProperty, null, _wordApp, new object[] { false });
        }

        // ══════════════════════════════════════════════════════
        //  Fir STA dedicat pentru Word, cu bucla de mesaje proprie.
        //  Invoke = executa sincron pe firul Word (apelantul asteapta);
        //  exceptiile se transmit apelantului.
        // ══════════════════════════════════════════════════════
        private sealed class WordThread
        {
            private readonly System.Threading.ManualResetEvent _ready =
                new System.Threading.ManualResetEvent(false);
            private System.Windows.Forms.Control _ctl;

            public WordThread()
            {
                var t = new System.Threading.Thread(Run)
                {
                    IsBackground = true,
                    Name = "GeneratorDocumente.Word"
                };
                t.SetApartmentState(System.Threading.ApartmentState.STA);
                t.Start();
                _ready.WaitOne();
            }

            private void Run()
            {
                _ctl = new System.Windows.Forms.Control();
                _ctl.CreateControl();
                var h = _ctl.Handle;   // forteaza crearea handle-ului pe acest fir
                _ready.Set();
                System.Windows.Forms.Application.Run();
            }

            public void Invoke(Action action)
            {
                Exception err = null;
                _ctl.Invoke(new Action(() =>
                {
                    try { action(); }
                    catch (Exception ex) { err = ex; }
                }));
                if (err != null) throw err;
            }

            public void Stop()
            {
                try
                {
                    _ctl.BeginInvoke(new Action(() =>
                    {
                        _ctl.Dispose();
                        System.Windows.Forms.Application.ExitThread();
                    }));
                }
                catch { }
            }
        }
    }
}