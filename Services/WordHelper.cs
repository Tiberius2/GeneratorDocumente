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
                if (value.Length > 0)
                {
                    result.Add(value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0
                        ? MakeRunWithBreaks(value, phRpr)
                        : MakeRun(value, phRpr));
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

        // ── Word Interop → PDF ────────────────────────────────
        internal static void ConvertToPdf(string docxPath, string pdfPath)
        {
            Type wordType = Type.GetTypeFromProgID("Word.Application");
            if (wordType == null)
                throw new InvalidOperationException("Microsoft Word nu este instalat.");

            object wordApp = Activator.CreateInstance(wordType);
            try
            {
                wordType.InvokeMember("Visible", BindingFlags.SetProperty, null, wordApp, new object[] { false });
                object documents = wordType.InvokeMember("Documents", BindingFlags.GetProperty, null, wordApp, null);
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
            finally
            {
                wordType.InvokeMember("Quit", BindingFlags.InvokeMethod, null, wordApp, null);
            }
        }
    }
}
