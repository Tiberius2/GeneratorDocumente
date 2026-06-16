using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.Services
{
    public static class PvTemplateEngine
    {
        // ══════════════════════════════════════════════════════
        //  Entry point
        // ══════════════════════════════════════════════════════
        public static string GeneratePdf(PvModelBase model, string templatePath)
        {
            string basePath = Environment.GetEnvironmentVariable("RecruitmentDocsPath");
            if (string.IsNullOrWhiteSpace(basePath))
                throw new InvalidOperationException("Variabila de sistem RecruitmentDocsPath nu este setata.");

            string candidateFolder = string.Format("{0} - {1}", model.PrsnId, model.NumeSalariat);
            string outputDir = Path.Combine(basePath, candidateFolder);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string pdfPath = Path.Combine(outputDir, BuildPdfFileName(model));
            string tempDocx = Path.Combine(Path.GetTempPath(),
                string.Format("PV_{0}_{1}.docx", model.TipPV, model.PrsnId));

            try
            {
                File.Copy(templatePath, tempDocx, true);
                FillTemplate(tempDocx, model);
                WordHelper.ConvertToPdf(tempDocx, pdfPath);
            }
            finally
            {
                if (File.Exists(tempDocx)) File.Delete(tempDocx);
            }

            return pdfPath;
        }

        public static void FillTemplatePublic(string docxPath, PvModelBase model)
            => FillTemplate(docxPath, model);

        public static void ConvertToPdfPublic(string docxPath, string pdfPath)
            => WordHelper.ConvertToPdf(docxPath, pdfPath);

        // ── Naming PDF ────────────────────────────────────────
        private static string BuildPdfFileName(PvModelBase model)
        {
            return string.Format("PV_{0}_{1}_{2}.pdf",
                GetTipPVShort(model.TipPV),
                WordHelper.SanitizeFileName(model.CodInregistrare),
                model.DataPV.ToString("dd.MM.yyyy"));
        }

        private static string GetTipPVShort(TipPV tip)
        {
            switch (tip)
            {
                case TipPV.Echipamente: return "Echipamente";
                case TipPV.Electronice: return "Electronice";
                case TipPV.Autovehicul: return "Autovehicul";
                default: return tip.ToString();
            }
        }

        // ══════════════════════════════════════════════════════
        //  Fill template
        // ══════════════════════════════════════════════════════
        private static void FillTemplate(string docxPath, PvModelBase model)
        {
            using (var doc = WordprocessingDocument.Open(docxPath, true))
            {
                var body = doc.MainDocumentPart.Document.Body;

                ExpandBunuriTable(body, model);

                var map = BuildMap(model);
                ReplaceInBody(body, map);

                doc.MainDocumentPart.Document.Save();
            }
        }

        // ── Expand tabel bunuri ───────────────────────────────
        private static void ExpandBunuriTable(Body body, PvModelBase model)
        {
            List<PvBunItem> bunuri = null;
            var echip = model as PvEchipamenteModel;
            if (echip != null) bunuri = echip.Bunuri;
            var elec = model as PvElecroniceModel;
            if (elec != null) bunuri = elec.Bunuri;

            if (bunuri == null || bunuri.Count == 0) return;

            var table = body.Descendants<Table>()
                .FirstOrDefault(t => WordHelper.GetText(t).Contains("{{BunNume}}"));
            if (table == null) return;

            var templateRow = table.Descendants<TableRow>()
                .FirstOrDefault(r => WordHelper.GetText(r).Contains("{{BunNume}}"));
            if (templateRow == null) return;

            foreach (var bun in bunuri)
            {
                var newRow = (TableRow)templateRow.CloneNode(true);
                var rowMap = new Dictionary<string, string>
                {
                    { "{{BunNume}}",      bun.Nume ?? string.Empty },
                    { "{{BunCantitate}}", bun.Cantitate ?? "1" },
                    { "{{BunPret}}",      bun.Pret ?? string.Empty }
                };
                foreach (var para in newRow.Descendants<Paragraph>())
                    WordHelper.MergeAndReplace(para, rowMap);

                table.InsertBefore(newRow, templateRow);
            }

            templateRow.Remove();
        }

        // ══════════════════════════════════════════════════════
        //  Placeholder map
        // ══════════════════════════════════════════════════════
        private static Dictionary<string, string> BuildMap(PvModelBase m)
        {
            var map = new Dictionary<string, string>
            {
                { "{{CodInregistrare}}",     m.CodInregistrare ?? string.Empty },
                { "{{DataPV}}",              m.DataPV.ToString("dd.MM.yyyy") },
                { "{{TipPredare}}",          m.GetTipPredareText() },
                { "{{NumeSalariat}}",        m.NumeSalariat ?? string.Empty },
                { "{{CNP}}",                 m.CNP ?? string.Empty },
                { "{{Functie}}",             m.Functie ?? string.Empty },
                { "{{NumeAngajator}}",       m.NumeAngajator ?? string.Empty },
                { "{{CIFAngajator}}",        m.CIFAngajator ?? string.Empty },
                { "{{ReprezentantLegal}}",   m.ReprezentantLegal ?? string.Empty },
                { "{{FunctieReprezentant}}", m.FunctieReprezentant ?? string.Empty },
                { "{{AdresaCompanie}}",      m.AdresaCompanie ?? string.Empty },
                { "{{ZipCompanie}}",         m.ZipCompanie ?? string.Empty },
                { "{{NrRegComertului}}",     m.NrRegComertului ?? string.Empty },
                { "{{IbanCompanie}}",        m.IbanCompanie ?? string.Empty },
                { "{{NrTelefonCompanie}}",   m.NrTelefonCompanie ?? string.Empty },
                { "{{EmailCompanie}}",       m.EmailCompanie ?? string.Empty },
                { "{{WebsiteCompanie}}",     m.WebsiteCompanie ?? string.Empty },
            };

            var echip = m as PvEchipamenteModel;
            if (echip != null) map["{{MentiuniDocument}}"] = echip.MentiuniDocument ?? string.Empty;

            var elec = m as PvElecroniceModel;
            if (elec != null) map["{{MentiuniDocument}}"] = elec.MentiuniDocument ?? string.Empty;

            var auto = m as PvAutovehiculModel;
            if (auto != null) AddAutovehiculPlaceholders(map, auto);

            return map;
        }

        private static void AddAutovehiculPlaceholders(Dictionary<string, string> map, PvAutovehiculModel auto)
        {
            Func<string, string> U = s => (s ?? string.Empty).ToUpper();

            // Predator 2 — Art. 2
            map["{{NumePredator2}}"]     = U(auto.NumePredator2);
            map["{{FunctiePredator2}}"]  = U(auto.FunctiePredator2);
            map["{{CNPPredator2}}"]      = auto.CNPPredator2 ?? string.Empty;
            map["{{CISeriaPredator2}}"]  = U(auto.CISeriaPredator2);
            map["{{CINrPredator2}}"]     = auto.CINrPredator2 ?? string.Empty;
            map["{{DomiciliuPredator2}}"] = U(auto.DomiciliuPredator2);

            // Primitor — Art. 3
            map["{{CISeria}}"]    = U(auto.CISeria);
            map["{{CINr}}"]       = auto.CINr ?? string.Empty;
            map["{{Domiciliu}}"]  = U(auto.Domiciliu);

            // Date vehicul
            map["{{MarcaAuto}}"]        = U(auto.MarcaAuto);
            map["{{NrInmatriculare}}"]   = U(auto.NrInmatriculare);
            map["{{SerieSasiu}}"]        = U(auto.SerieSasiu);
            map["{{AnFabricatie}}"]      = auto.AnFabricatie ?? string.Empty;
            map["{{Kilometri}}"]         = auto.Kilometri ?? string.Empty;
            map["{{StareFunctionare}}"]  = auto.StareFunctionare ?? string.Empty;
            map["{{Avarii}}"]            = auto.Avarii ?? string.Empty;
            map["{{AnvelopeFata}}"]      = auto.AnvelopeFata ?? string.Empty;
            map["{{AnvelopeSpate}}"]     = auto.AnvelopeSpate ?? string.Empty;
            map["{{UzuraAnvelope}}"]     = auto.UzuraAnvelope ?? "0";

            // Dotari
            map["{{TrusaSanitara}}"]           = auto.TrusaSanitara ?? "DA";
            map["{{Extinctor}}"]               = auto.Extinctor ?? "DA";
            map["{{TriunghiReflectorizant}}"]  = auto.TriunghiReflectorizant ?? "DA";
            map["{{Cric}}"]                    = auto.Cric ?? "DA";
            map["{{CheieRoti}}"]               = auto.CheieRoti ?? "DA";
            map["{{VestaReflectorizanta}}"]    = auto.VestaReflectorizanta ?? "DA";
            map["{{RoataRezervа}}"]            = auto.RoataRezervа ?? "DA";
            map["{{UzuraRoataRezervа}}"]       = auto.UzuraRoataRezervа ?? "0";

            // Documente
            map["{{CertificatInmatriculare}}"] = auto.CertificatInmatriculare ?? "DA";
            map["{{AsigurareRCA}}"]            = auto.AsigurareRCA ?? "DA";
            map["{{Rovinieta}}"]               = auto.Rovinieta ?? "DA";

            map["{{MentiuniDocument}}"] = auto.MentiuniDocument ?? string.Empty;
        }

        // ── Replace in body ───────────────────────────────────
        private static void ReplaceInBody(Body body, Dictionary<string, string> map)
        {
            foreach (var para in body.Descendants<Paragraph>().ToList())
                WordHelper.MergeAndReplace(para, map);
        }
    }
}
