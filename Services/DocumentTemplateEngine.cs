using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ActAditionalPlugin.Services
{
    internal static class DocumentTemplateEngine
    {
        public static string GeneratePdf<TModel>(TModel model, string templatePath,
            Func<TModel, Dictionary<string, string>> buildMap,
            Action<Body, TModel> expandTables = null,
            Func<TModel, string> buildFileName = null,
            string tempPrefix = null)
        {
            string basePath = Environment.GetEnvironmentVariable("RecruitmentDocsPath");
            if (string.IsNullOrWhiteSpace(basePath))
                throw new InvalidOperationException("Variabila de sistem RecruitmentDocsPath nu este setata.");

            var prsnIdProp = typeof(TModel).GetProperty("PrsnId");
            var numeProp = typeof(TModel).GetProperty("NumeSalariat");
            string candidateFolder = string.Empty;
            try
            {
                var prsnId = prsnIdProp != null ? prsnIdProp.GetValue(model)?.ToString() : string.Empty;
                var nume = numeProp != null ? numeProp.GetValue(model)?.ToString() : string.Empty;
                candidateFolder = string.Format("{0} - {1}", prsnId, nume);
            }
            catch { candidateFolder = string.Empty; }

            string outputDir = string.IsNullOrWhiteSpace(candidateFolder) ? basePath : Path.Combine(basePath, candidateFolder);
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            string pdfFileName = buildFileName != null ? buildFileName(model) : typeof(TModel).Name + "_" + DateTime.Today.ToString("dd.MM.yyyy") + ".pdf";
            string pdfPath = Path.Combine(outputDir, pdfFileName);

            string tempDocx = Path.Combine(Path.GetTempPath(), string.Format("{0}_{1}.docx", tempPrefix ?? typeof(TModel).Name, Guid.NewGuid().ToString("N")));

            try
            {
                File.Copy(templatePath, tempDocx, true);
                FillTemplate(tempDocx, model, expandTables, buildMap);
                WordHelper.ConvertToPdf(tempDocx, pdfPath);
            }
            finally
            {
                try { if (File.Exists(tempDocx)) File.Delete(tempDocx); } catch { }
            }

            return pdfPath;
        }

        private static void FillTemplate<TModel>(string docxPath, TModel model, Action<Body, TModel> expandTables, Func<TModel, Dictionary<string, string>> buildMap)
        {
            using (var doc = WordprocessingDocument.Open(docxPath, true))
            {
                var body = doc.MainDocumentPart.Document.Body;
                expandTables?.Invoke(body, model);
                var map = buildMap(model) ?? new Dictionary<string, string>();
                ReplaceInBody(body, map);
                doc.MainDocumentPart.Document.Save();
            }
        }

        public static void FillDocx<TModel>(string docxPath, TModel model, Func<TModel, Dictionary<string, string>> buildMap, Action<Body, TModel> expandTables = null)
        {
            // Fill placeholders directly in an existing docx file
            FillTemplate(docxPath, model, expandTables, buildMap);
        }

        public static void ReplaceInBody(Body body, Dictionary<string, string> map)
        {
            foreach (var para in body.Descendants<Paragraph>().ToList())
                WordHelper.MergeAndReplace(para, map);
        }

        public static Dictionary<string, string> BuildCommonPlaceholders(object m)
        {
            var map = new Dictionary<string, string>();
            try
            {
                string GetString(string name)
                {
                    var p = m.GetType().GetProperty(name);
                    if (p == null) return string.Empty;
                    var v = p.GetValue(m);
                    return v?.ToString() ?? string.Empty;
                }

                DateTime GetDate(string name)
                {
                    var p = m.GetType().GetProperty(name);
                    if (p == null) return DateTime.MinValue;
                    var v = p.GetValue(m);
                    return v is DateTime ? (DateTime)v : DateTime.MinValue;
                }

                map["{{NumeSalariat}}"] = GetString("NumeSalariat");
                map["{{CNP}}"] = GetString("CNP");
                map["{{Functie}}"] = GetString("Functie");
                map["{{NrCim}}"] = GetString("NrCim");
                var dataCim = GetDate("DataCim");
                map["{{DataCim}}"] = dataCim != DateTime.MinValue ? dataCim.ToString("dd.MM.yyyy") : string.Empty;
                map["{{NumeAngajator}}"] = GetString("NumeAngajator");
                map["{{CIFAngajator}}"] = GetString("CIFAngajator");
                map["{{ReprezentantLegal}}"] = GetString("ReprezentantLegal");
                map["{{FunctieReprezentant}}"] = GetString("FunctieReprezentant");
                map["{{AdresaCompanie}}"] = GetString("AdresaCompanie");
                map["{{ZipCompanie}}"] = GetString("ZipCompanie");
                map["{{NrRegComertului}}"] = GetString("NrRegComertului");
                map["{{IbanCompanie}}"] = GetString("IbanCompanie");
                map["{{NrTelefonCompanie}}"] = GetString("NrTelefonCompanie");
                map["{{EmailCompanie}}"] = GetString("EmailCompanie");
                map["{{WebsiteCompanie}}"] = GetString("WebsiteCompanie");
            }
            catch { }
            return map;
        }
    }
}
