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

            var ref_ = model as ReferatDisciplinarModel;
            if (ref_ != null)
                return string.Format("Referat_Disciplinar_{0}_{1}.pdf",
                    WordHelper.SanitizeFileName(ref_.CodInregistrare),
                    ref_.DataReferat.ToString("dd-MM-yyyy"));

            var av = model as AvertismentDisciplinarModel;
            if (av != null)
                return string.Format("Avertisment_Disciplinar_{0}_{1}.pdf",
                    WordHelper.SanitizeFileName(av.CodInregistrare),
                    av.DataDecizie.ToString("dd-MM-yyyy"));

            var cc = model as DecizieConstituireComisieModel;
            if (cc != null)
                return string.Format("Decizie_Constituire_Comisie_{0}_{1}.pdf",
                    WordHelper.SanitizeFileName(cc.CodInregistrare),
                    cc.DataDecizie.ToString("dd-MM-yyyy"));

            var conv = model as ConvocareCercetareModel;
            if (conv != null)
                return string.Format("Convocare_Cercetare_{0}_{1}.pdf",
                    WordHelper.SanitizeFileName(conv.CodInregistrare),
                    conv.DataConvocare.ToString("dd-MM-yyyy"));

            var pvc = model as ProcesVerbalCercetareModel;
            if (pvc != null)
                return string.Format("PV_Cercetare_Disciplinara_{0}_{1}.pdf",
                    WordHelper.SanitizeFileName(pvc.CodInregistrare),
                    pvc.DataCercetare.ToString("dd-MM-yyyy"));

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

                var convocare = model as ConvocareCercetareModel;
                if (convocare != null)
                {
                    ExpandParagraphList(body, "{{NumeMembruComisie}}", convocare.Membri,
                        (m, i) => new Dictionary<string, string>
                        {
                            { "{{NumeMembruComisie}}", m.Nume },
                            { "{{FunctieMembruComisie}}", m.Functie }
                        });
                    // Referate concatenate intr-un singur placeholder
                }

                var comisie = model as DecizieConstituireComisieModel;
                if (comisie != null)
                {
                    ExpandParagraphList(body, "{{NumeMembru}}", comisie.Membri,
                        (m, i) => new Dictionary<string, string>
                        {
                            { "{{NumeMembru}}", m.Nume },
                            { "{{FunctieMembru}}", m.Functie }
                        });
                    ExpandParagraphList(body, "{{NumeMembruSemnatura}}", comisie.Membri,
                        (m, i) => new Dictionary<string, string>
                        {
                            { "{{NumeMembruSemnatura}}", m.Nume }
                        });
                }

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
                case TipDocument.IncetarePerioadaProba:
                    AddIncetarePerioadaProba(map, (IncetarePerioadaProbaModel)model); break;
                case TipDocument.ReferatDisciplinar:
                    AddReferatDisciplinar(map, (ReferatDisciplinarModel)model); break;
                case TipDocument.AvertismentDisciplinar:
                    AddAvertismentDisciplinar(map, (AvertismentDisciplinarModel)model); break;
                case TipDocument.DecizieConstituireComisie:
                    AddDecizieConstituireComisie(map, (DecizieConstituireComisieModel)model); break;
                case TipDocument.ConvocareCercetare:
                    AddConvocareCercetare(map, (ConvocareCercetareModel)model); break;
                case TipDocument.ProcesVerbalCercetare:
                    AddProcesVerbalCercetare(map, (ProcesVerbalCercetareModel)model); break;
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
                { "{{NumeDepartament}}",     m.NumeDepartament ?? string.Empty },
                { "{{CodInregistrare}}",     m.CodInregistrare ?? string.Empty },
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
                { "{{MentiuniDocument}}",    m.MentiuniDocument ?? string.Empty },
            };
        }

        private static void AddDecizie(Dictionary<string, string> map, DecizieModelBase m)
        {
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
            map["{{DataEmitereAct}}"] = m.DataEmitereAct.ToString("dd.MM.yyyy");
            map["{{DataVigoare}}"] = m.DataVigoare.ToString("dd.MM.yyyy");
        }

        private static void AddSuspendareCresterecopil(Dictionary<string, string> map, SuspendareCresterecopilModel m)
        {
            AddCerere(map, m);
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare != DateTime.MinValue
                ? m.DataEndSuspendare.ToString("dd.MM.yyyy") : string.Empty;
            map["{{PerioadaSuspendare}}"] = m.PerioadaSuspendare ?? string.Empty;
            map["{{NumeCopil}}"] = m.NumeCopil ?? string.Empty;
            map["{{CNPCopil}}"] = m.CNPCopil ?? string.Empty;
            map["{{SerieCertificat}}"] = m.SerieCertificat ?? string.Empty;
            map["{{NrCertificat}}"] = m.NrCertificat ?? string.Empty;
        }

        private static void AddSuspendareCresterecopilHandicap(Dictionary<string, string> map, SuspendareCresterecopilHandicapModel m)
        {
            AddCerere(map, m);
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare != DateTime.MinValue
                ? m.DataEndSuspendare.ToString("dd.MM.yyyy") : string.Empty;
            map["{{PerioadaSuspendare}}"] = m.PerioadaSuspendare ?? string.Empty;
            map["{{NumeCopil}}"] = m.NumeCopil ?? string.Empty;
            map["{{CNPCopil}}"] = m.CNPCopil ?? string.Empty;
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

        private static void AddIncetarePerioadaProba(Dictionary<string, string> map, IncetarePerioadaProbaModel m)
        {
            AddDecizie(map, m);
            map["{{DataIncetare}}"] = m.DataIncetare.ToString("dd.MM.yyyy");
            map["{{NrNotificare}}"] = m.NrNotificare ?? string.Empty;
            map["{{DataNotificare}}"] = m.DataNotificare != DateTime.MinValue
                ? m.DataNotificare.ToString("dd.MM.yyyy") : string.Empty;
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

        private static void AddReferatDisciplinar(Dictionary<string, string> map, ReferatDisciplinarModel m)
        {
            map["{{DataReferat}}"] = m.DataReferat != DateTime.MinValue
                ? m.DataReferat.ToString("dd.MM.yyyy") : string.Empty;
            map["{{NumeAutorReferat}}"] = m.NumeAutorReferat ?? string.Empty;
            map["{{FunctieAutorReferat}}"] = m.FunctieAutorReferat ?? string.Empty;
            map["{{LocMunca}}"] = m.LocMunca ?? string.Empty;
            map["{{DescriereFapta}}"] = m.DescriereFapta ?? string.Empty;
            map["{{ConsecinteAbateri}}"] = m.ConsecinteAbateri ?? string.Empty;
            map["{{TemeiLegal}}"] = m.TemeiLegal ?? string.Empty;
        }

        private static void AddAvertismentDisciplinar(Dictionary<string, string> map, AvertismentDisciplinarModel m)
        {
            map["{{DataDecizie}}"] = m.DataDecizie != DateTime.MinValue
                ? m.DataDecizie.ToString("dd.MM.yyyy") : string.Empty;
            map["{{NrReferat}}"] = m.NrReferat ?? string.Empty;
            map["{{DataReferat}}"] = m.DataReferat != DateTime.MinValue
                ? m.DataReferat.ToString("dd.MM.yyyy") : string.Empty;
            map["{{NumeAutorReferat}}"] = m.NumeAutorReferat ?? string.Empty;
            map["{{FunctieAutorReferat}}"] = m.FunctieAutorReferat ?? string.Empty;
            map["{{LocMunca}}"] = m.LocMunca ?? string.Empty;
            map["{{DescriereAbateri}}"] = m.DescriereAbateri ?? string.Empty;
            map["{{DescriereAbateriDetaliat}}"] = m.DescriereAbateriDetaliat ?? string.Empty;
            map["{{DataComunicare}}"] = m.DataComunicare != DateTime.MinValue
                ? m.DataComunicare.ToString("dd.MM.yyyy") : string.Empty;
        }

        // Gaseste primul paragraf care contine markerText, il duplica pentru fiecare item
        // si aplica substitutia individuala din mapBuilder pe fiecare copie.
        private static void ExpandParagraphList<T>(Body body, string markerText,
            List<T> items, Func<T, int, Dictionary<string, string>> mapBuilder)
        {
            if (items == null || items.Count == 0) return;

            var allParas = body.Descendants<Paragraph>().ToList();
            Paragraph templatePara = null;
            foreach (var para in allParas)
            {
                string text = string.Concat(para.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                    .Select(t => t.Text));
                if (text.Contains(markerText)) { templatePara = para; break; }
            }
            if (templatePara == null) return;

            // Inseram copii inainte de templatePara, apoi stergem templatePara
            var parent = templatePara.Parent;
            for (int i = 0; i < items.Count; i++)
            {
                var clone = (Paragraph)templatePara.CloneNode(true);
                var map = mapBuilder(items[i], i);
                foreach (var txtNode in clone.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().ToList())
                {
                    foreach (var kv in map)
                        txtNode.Text = txtNode.Text.Replace(kv.Key, kv.Value);
                }
                parent.InsertBefore(clone, templatePara);
            }
            parent.RemoveChild(templatePara);
        }

        private static void AddDecizieConstituireComisie(Dictionary<string, string> map, DecizieConstituireComisieModel m)
        {
            map["{{DataDecizie}}"] = m.DataDecizie != DateTime.MinValue
                ? m.DataDecizie.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DataNotaExplicativa}}"] = m.DataNotaExplicativa != DateTime.MinValue
                ? m.DataNotaExplicativa.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DescriereAbatere}}"] = m.DescriereAbatere ?? string.Empty;
            map["{{NumeIntocmitorHr}}"] = m.NumeIntocmitorHr ?? string.Empty;
            map["{{IntervalAniCCM}}"] = m.IntervalAniCCM ?? string.Empty;
            map["{{CodInregistrareITM}}"] = m.CodInregistrareITM ?? string.Empty;
            map["{{NumePresedinte}}"] = m.NumePresedinte ?? string.Empty;
            map["{{FunctiePresedinte}}"] = m.FunctiePresedinte ?? string.Empty;
            map["{{NumeObservator}}"] = m.NumeObservator ?? string.Empty;
            map["{{FunctieObservator}}"] = m.FunctieObservator ?? string.Empty;
            map["{{DataInceputCercetare}}"] = m.DataInceputCercetare != DateTime.MinValue
                ? m.DataInceputCercetare.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DataSfarsitCercetare}}"] = m.DataSfarsitCercetare != DateTime.MinValue
                ? m.DataSfarsitCercetare.ToString("dd.MM.yyyy") : string.Empty;

            // Referate concatenate (nu expandare paragrafe)
            if (m.Referate != null && m.Referate.Count > 0)
            {
                map["{{ReferateSursa}}"] = string.Join(", ",
                    m.Referate.Select(r => r.CodSiData ?? string.Empty));
                map["{{NumeSiFunctieIntocmitorReferat}}"] = string.Join(", ",
                    m.Referate.Select(r => r.Intocmitor ?? string.Empty).Distinct());
            }
            else
            {
                map["{{ReferateSursa}}"] = string.Empty;
                map["{{NumeSiFunctieIntocmitorReferat}}"] = string.Empty;
            }
            // {{NumeMembru}}/{{FunctieMembru}} si {{NumeMembruSemnatura}} expandate prin ExpandParagraphList
        }

        private static void AddConvocareCercetare(Dictionary<string, string> map, ConvocareCercetareModel m)
        {
            map["{{DataConvocare}}"] = m.DataConvocare != DateTime.MinValue
                ? m.DataConvocare.ToString("dd.MM.yyyy") : string.Empty;
            map["{{CodCor}}"] = m.CodCor ?? string.Empty;
            map["{{DataNotaExplicativa}}"] = m.DataNotaExplicativa != DateTime.MinValue
                ? m.DataNotaExplicativa.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DescriereAbatere}}"] = m.DescriereAbatere ?? string.Empty;
            map["{{IntervalAniCCM}}"] = m.IntervalAniCCM ?? string.Empty;
            map["{{CodInregistrareITM}}"] = m.CodInregistrareITM ?? string.Empty;
            map["{{LocCercetare}}"] = m.LocCercetare ?? string.Empty;
            map["{{DataCercetare}}"] = m.DataCercetare != DateTime.MinValue
                ? m.DataCercetare.ToString("dd.MM.yyyy") : string.Empty;
            map["{{OraConvocare}}"] = m.OraConvocare ?? string.Empty;
            map["{{NrDecizieComisie}}"] = m.NrDecizieComisie ?? string.Empty;
            map["{{DataDecizieComisie}}"] = m.DataDecizieComisie != DateTime.MinValue
                ? m.DataDecizieComisie.ToString("dd.MM.yyyy") : string.Empty;
            map["{{NumeIntocmitorHr}}"] = m.NumeIntocmitorHr ?? string.Empty;

            // Referate concatenate
            if (m.Referate != null && m.Referate.Count > 0)
            {
                map["{{ReferateSursa}}"] = string.Join(", ",
                    m.Referate.Select(r => r.CodSiData ?? string.Empty));
                map["{{NumeSiFunctieIntocmitorReferat}}"] = string.Join(", ",
                    m.Referate.Select(r => r.Intocmitor ?? string.Empty).Distinct());
            }
            else
            {
                map["{{ReferateSursa}}"] = string.Empty;
                map["{{NumeSiFunctieIntocmitorReferat}}"] = string.Empty;
            }
            // {{NumeMembruComisie}}/{{FunctieMembruComisie}} expandate prin ExpandParagraphList
        }

        private static void AddProcesVerbalCercetare(Dictionary<string, string> map, ProcesVerbalCercetareModel m)
        {
            map["{{DataCercetare}}"] = m.DataCercetare != DateTime.MinValue
                ? m.DataCercetare.ToString("dd.MM.yyyy") : string.Empty;
            map["{{LocCercetare}}"] = m.LocCercetare ?? string.Empty;
            map["{{ConcluziiComisie}}"] = m.ConcluziiComisie ?? string.Empty;
            map["{{SanctiuneaPropusa}}"] = m.SanctiuneaPropusa ?? string.Empty;
        }

        // ── Replace in body ───────────────────────────────────
        private static void ReplaceInBody(Body body, Dictionary<string, string> map)
        {
            foreach (var para in body.Descendants<Paragraph>().ToList())
                WordHelper.MergeAndReplace(para, map);
        }
    }
}