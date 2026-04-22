using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.Services
{
    public static class TemplateEngine
    {
        // ══════════════════════════════════════════════════════
        //  Entry point
        // ══════════════════════════════════════════════════════
        public static string GeneratePdf(DocumentModelBase model, string templatePath)
        {
            string basePath = Environment.GetEnvironmentVariable("RecruitmentDocsPath");
            if (string.IsNullOrWhiteSpace(basePath))
                throw new InvalidOperationException(
                    "Variabila de sistem RecruitmentDocsPath nu este setata.");

            string candidateFolder = string.Format("{0} - {1}", model.PrsnId, model.NumeSalariat);
            string outputDir = Path.Combine(basePath, candidateFolder);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string pdfFileName = BuildPdfFileName(model);
            string pdfPath = Path.Combine(outputDir, pdfFileName);
            string tempDocx = Path.Combine(Path.GetTempPath(),
                string.Format("{0}_{1}.docx", model.TipDocument, model.PrsnId));

            try
            {
                File.Copy(templatePath, tempDocx, true);
                FillTemplate(tempDocx, model);
                WordHelper.ConvertToPdf(tempDocx, pdfPath);
            }
            finally
            {
                if (File.Exists(tempDocx))
                    File.Delete(tempDocx);
            }

            return pdfPath;
        }

        // ── Helpers publici pentru preview ────────────────────
        public static void FillTemplatePublic(string docxPath, DocumentModelBase model)
            => FillTemplate(docxPath, model);

        public static void ConvertToPdfPublic(string docxPath, string pdfPath)
            => WordHelper.ConvertToPdf(docxPath, pdfPath);

        // ── Naming PDF ────────────────────────────────────────
        private static string BuildPdfFileName(DocumentModelBase model)
        {
            var aa = model as ActAditionalModel;
            if (aa != null)
                return string.Format("Act_Aditional_{0}_{1}.pdf",
                    WordHelper.SanitizeFileName(aa.CodInregistrare),
                    aa.DataEmitereAct.ToString("dd.MM.yyyy"));

            var dec = model as DecizieModelBase;
            if (dec != null)
                return string.Format("{0}_{1}_{2}.pdf",
                    GetTipDecizieShort(model.TipDocument),
                    WordHelper.SanitizeFileName(dec.CodInregistrare),
                    dec.DataDecizie.ToString("dd.MM.yyyy"));

            return string.Format("{0}_{1}.pdf", model.TipDocument, DateTime.Today.ToString("dd.MM.yyyy"));
        }

        private static string GetTipDecizieShort(TipDocument tip)
        {
            switch (tip)
            {
                case TipDocument.SuspendareCresterecopil: return "Decizie_Suspendare_Cresterecopil";
                case TipDocument.SuspendareCresterecopilHandicap: return "Decizie_Suspendare_CresterecopilHandicap";
                case TipDocument.SuspendareAbsenteNemotivate: return "Decizie_Suspendare_AbsenteNemotivate";
                case TipDocument.SuspendareAcordParti: return "Decizie_Suspendare_AcordParti";
                case TipDocument.SuspendareSiIncetareSuspendare: return "Decizie_Suspendare_SiIncetare";
                case TipDocument.IncetareSuspendare: return "Decizie_Incetare_Suspendare";
                case TipDocument.IncetareDemisie: return "Decizie_Incetare_Demisie";
                case TipDocument.IncetareExpirare: return "Decizie_Incetare_Expirare";
                case TipDocument.IncetareDisciplinar: return "Decizie_Incetare_Disciplinar";
                default: return "Decizie_" + tip.ToString();
            }
        }

        // ══════════════════════════════════════════════════════
        //  Fill template
        // ══════════════════════════════════════════════════════
        private static void FillTemplate(string docxPath, DocumentModelBase model)
        {
            using (var doc = WordprocessingDocument.Open(docxPath, true))
            {
                var body = doc.MainDocumentPart.Document.Body;

                var aa = model as ActAditionalModel;
                if (aa != null)
                    ExpandModificariTable(body, aa.Modificari);

                var decBase = model as DecizieModelBase;
                if (decBase != null)
                    InjectArticoleFinal(body, decBase);

                var placeholders = BuildPlaceholders(model);
                ReplaceInBody(body, placeholders);

                doc.MainDocumentPart.Document.Save();
            }
        }

        // ══════════════════════════════════════════════════════
        //  Expandare tabel modificari (Act Aditional)
        // ══════════════════════════════════════════════════════
        private static void ExpandModificariTable(Body body, List<PunctModificare> modificari)
        {
            if (modificari == null || modificari.Count == 0) return;

            var table = body.Descendants<Table>()
                .FirstOrDefault(t => WordHelper.GetText(t).Contains("{{ModificareNr}}"));
            if (table == null) return;

            var templateRow = table.Descendants<TableRow>()
                .FirstOrDefault(r => WordHelper.GetText(r).Contains("{{ModificareNr}}"));
            if (templateRow == null) return;

            for (int i = 0; i < modificari.Count; i++)
            {
                var newRow = (TableRow)templateRow.CloneNode(true);
                var rowMap = new Dictionary<string, string>
                {
                    { "{{ModificareNr}}",        (i + 1).ToString() },
                    { "{{ModificareReferinta}}", modificari[i].Referinta ?? string.Empty },
                    { "{{ModificareText}}",       modificari[i].TextModificare ?? string.Empty }
                };
                foreach (var para in newRow.Descendants<Paragraph>())
                    WordHelper.MergeAndReplace(para, rowMap);

                table.InsertBefore(newRow, templateRow);
            }

            templateRow.Remove();
        }

        // ══════════════════════════════════════════════════════
        //  Articole fixe finale (Compartiment + Contestatie)
        // ══════════════════════════════════════════════════════
        private static void InjectArticoleFinal(Body body, DecizieModelBase model)
        {
            string fullText = WordHelper.GetText(body);

            int nrArt = 0;
            string textFaraPlaceholders = fullText
                .Replace("{{ArticolCompartiment}}", "")
                .Replace("{{ArticolContestatie}}", "");

            for (int i = 1; i <= 20; i++)
            {
                if (textFaraPlaceholders.Contains("Art." + i) ||
                    textFaraPlaceholders.Contains("Art. " + i))
                    nrArt = i;
            }

            _artCompartiment = string.Format(
                "Art.{0} Compartimentul juridic, personal şi financiar contabil vor duce la îndeplinire prezenta.",
                nrArt + 1);
            _artContestatie = string.Format(
                "Art.{0} Prezenta decizie poate fi contestata in termen de 30 de zile de la comunicare la Tribunalul Botosani.",
                nrArt + 2);
        }

        [ThreadStatic]
        private static string _artCompartiment;
        [ThreadStatic]
        private static string _artContestatie;

        // ══════════════════════════════════════════════════════
        //  Placeholder maps per tip document
        // ══════════════════════════════════════════════════════
        private static Dictionary<string, string> BuildPlaceholders(DocumentModelBase model)
        {
            var map = BuildCommonPlaceholders(model);

            switch (model.TipDocument)
            {
                case TipDocument.ActAditional:
                    AddActAditional(map, (ActAditionalModel)model); break;
                case TipDocument.SuspendareCresterecopil:
                    AddSuspendareCresterecopil(map, (SuspendareCresterecopilModel)model); break;
                case TipDocument.SuspendareCresterecopilHandicap:
                    AddSuspendareCresterecopilHandicap(map, (SuspendareCresterecopilHandicapModel)model); break;
                case TipDocument.SuspendareAbsenteNemotivate:
                    AddSuspendareAbsente(map, (SuspendareAbsenteNemotivateModel)model); break;
                case TipDocument.SuspendareAcordParti:
                    AddSuspendareAcordParti(map, (SuspendareAcordPartiModel)model); break;
                case TipDocument.SuspendareSiIncetareSuspendare:
                    AddSuspendareSiIncetare(map, (SuspendareSiIncetareSuspendareModel)model); break;
                case TipDocument.IncetareSuspendare:
                    AddIncetareSuspendare(map, (IncetareSuspendareModel)model); break;
                case TipDocument.IncetareDemisie:
                    AddIncetareDemisie(map, (IncetareDemisieModel)model); break;
                case TipDocument.IncetareExpirare:
                    AddIncetareExpirare(map, (IncetareExpirareModel)model); break;
                case TipDocument.IncetareDisciplinar:
                    AddIncetareDisciplinar(map, (IncetareDisciplinarModel)model); break;
            }

            return map;
        }

        private static Dictionary<string, string> BuildCommonPlaceholders(DocumentModelBase m)
        {
            return new Dictionary<string, string>
            {
                { "{{NumeSalariat}}",        m.NumeSalariat ?? string.Empty },
                { "{{CNP}}",                 m.CNP ?? string.Empty },
                { "{{Functie}}",             m.Functie ?? string.Empty },
                { "{{NrCim}}",               m.NrCim ?? string.Empty },
                { "{{DataCim}}",             m.DataCim != DateTime.MinValue ? m.DataCim.ToString("dd.MM.yyyy") : string.Empty },
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
                { "{{ArticolCompartiment}}", _artCompartiment ?? string.Empty },
                { "{{ArticolContestatie}}",  _artContestatie ?? string.Empty },
            };
        }

        private static void AddDecizie(Dictionary<string, string> map, DecizieModelBase m)
        {
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{DataDecizie}}"] = m.DataDecizie != DateTime.MinValue
                ? m.DataDecizie.ToString("dd.MM.yyyy") : string.Empty;
        }

        private static void AddCerere(Dictionary<string, string> map, DecizieModelCuCerere m)
        {
            AddDecizie(map, m);
            map["{{NrCerere}}"] = m.NrCerere ?? string.Empty;
            map["{{DataCerere}}"] = m.DataCerere != DateTime.MinValue
                ? m.DataCerere.ToString("dd.MM.yyyy") : string.Empty;
        }

        // ── Per tip ───────────────────────────────────────────
        private static void AddActAditional(Dictionary<string, string> map, ActAditionalModel m)
        {
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{DataEmitereAct}}"] = m.DataEmitereAct.ToString("dd.MM.yyyy");
            map["{{DataVigoare}}"] = m.DataVigoare.ToString("dd.MM.yyyy");
        }

        private static void AddSuspendareCresterecopil(Dictionary<string, string> map, SuspendareCresterecopilModel m)
        {
            AddCerere(map, m);
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{PerioadaSuspendare}}"] = m.PerioadaSuspendare ?? string.Empty;
            map["{{NumeCopil}}"] = m.NumeCopil ?? string.Empty;
            map["{{SerieCertificat}}"] = m.SerieCertificat ?? string.Empty;
            map["{{NrCertificat}}"] = m.NrCertificat ?? string.Empty;
        }

        private static void AddSuspendareCresterecopilHandicap(Dictionary<string, string> map, SuspendareCresterecopilHandicapModel m)
        {
            AddCerere(map, m);
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare.ToString("dd.MM.yyyy");
            map["{{PerioadaSuspendare}}"] = m.PerioadaSuspendare ?? string.Empty;
            map["{{NumeCopil}}"] = m.NumeCopil ?? string.Empty;
            map["{{SerieCertificat}}"] = m.SerieCertificat ?? string.Empty;
            map["{{NrCertificat}}"] = m.NrCertificat ?? string.Empty;
            map["{{GradHandicap}}"] = m.GradHandicap ?? string.Empty;
            map["{{NrCertificatHandicap}}"] = m.NrCertificatHandicap ?? string.Empty;
            map["{{DataCertificatHandicap}}"] = m.DataCertificatHandicap != DateTime.MinValue
                ? m.DataCertificatHandicap.ToString("dd.MM.yyyy") : string.Empty;
        }

        private static void AddSuspendareAbsente(Dictionary<string, string> map, SuspendareAbsenteNemotivateModel m)
        {
            AddDecizie(map, m);
            map["{{NrReferat}}"] = m.NrReferat ?? string.Empty;
            map["{{DataReferat}}"] = m.DataReferat != DateTime.MinValue
                ? m.DataReferat.ToString("dd.MM.yyyy") : string.Empty;
            map["{{IntocmitDe}}"] = m.IntocmitDe ?? string.Empty;
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare.ToString("dd.MM.yyyy");
            map["{{DataIncetareSuspendare}}"] = m.DataIncetareSuspendare.ToString("dd.MM.yyyy");
        }

        private static void AddSuspendareAcordParti(Dictionary<string, string> map, SuspendareAcordPartiModel m)
        {
            AddCerere(map, m);
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare.ToString("dd.MM.yyyy");
        }

        private static void AddSuspendareSiIncetare(Dictionary<string, string> map, SuspendareSiIncetareSuspendareModel m)
        {
            AddCerere(map, m);
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare.ToString("dd.MM.yyyy");
            map["{{DataIncetareSuspendare}}"] = m.DataIncetareSuspendare.ToString("dd.MM.yyyy");
        }

        private static void AddIncetareSuspendare(Dictionary<string, string> map, IncetareSuspendareModel m)
        {
            AddCerere(map, m);
            map["{{DataIncetareSuspendare}}"] = m.DataIncetareSuspendare.ToString("dd.MM.yyyy");
        }

        private static void AddIncetareDemisie(Dictionary<string, string> map, IncetareDemisieModel m)
        {
            AddCerere(map, m);
            map["{{DataIncetare}}"] = m.DataIncetare.ToString("dd.MM.yyyy");
            map["{{ArticolDemisie}}"] = m.ArticolDemisie ?? string.Empty;
        }

        private static void AddIncetareExpirare(Dictionary<string, string> map, IncetareExpirareModel m)
        {
            AddDecizie(map, m);
            map["{{DataIncetare}}"] = m.DataIncetare.ToString("dd.MM.yyyy");
        }

        private static void AddIncetareDisciplinar(Dictionary<string, string> map, IncetareDisciplinarModel m)
        {
            AddDecizie(map, m);
            map["{{DataIncetare}}"] = m.DataIncetare.ToString("dd.MM.yyyy");
            map["{{PerioadaCercetare}}"] = m.PerioadaCercetare ?? string.Empty;
            map["{{NrProcesVerbal}}"] = m.NrProcesVerbal ?? string.Empty;
            map["{{DataProcesVerbal}}"] = m.DataProcesVerbal != DateTime.MinValue
                ? m.DataProcesVerbal.ToString("dd.MM.yyyy") : string.Empty;
            map["{{LocCercetare}}"] = m.LocCercetare ?? string.Empty;
            map["{{MotiveleSanctionarii}}"] = m.MotiveleSanctionarii ?? string.Empty;
            map["{{ImprejurariFapte}}"] = m.ImprejurariFapte ?? string.Empty;
            map["{{GradVinovatie}}"] = m.GradVinovatie ?? string.Empty;
            map["{{ConsecinteAbateri}}"] = m.ConsecinteAbateri ?? string.Empty;
            map["{{NumeIntocmit}}"] = m.NumeIntocmit ?? string.Empty;
            map["{{FunctieIntocmit}}"] = m.FunctieIntocmit ?? string.Empty;
        }

        // ── Replace in body ───────────────────────────────────
        private static void ReplaceInBody(Body body, Dictionary<string, string> map)
        {
            foreach (var para in body.Descendants<Paragraph>().ToList())
                WordHelper.MergeAndReplace(para, map);
        }
    }
}
