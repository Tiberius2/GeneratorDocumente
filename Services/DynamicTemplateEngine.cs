using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.Services
{
    // ══════════════════════════════════════════════════════════
    //  DYNAMIC TEMPLATE ENGINE
    //
    //  Primeste:
    //    - DocumentDefinition (structura JSON)
    //    - FormValues (Dictionary<string, object>) — valorile din formular
    //    - CommonValues (date angajat + companie)
    //
    //  Produce:
    //    - PDF final sau DOCX temp pentru preview
    // ══════════════════════════════════════════════════════════
    public static class DynamicTemplateEngine
    {
        // ══════════════════════════════════════════════════════
        //  Entry points
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Genereaza PDF final in folderul angajatului.
        /// </summary>
        public static string GeneratePdf(
            DocumentDefinition def,
            Dictionary<string, object> formValues,
            CommonDocumentValues common)
        {
            string basePath = Environment.GetEnvironmentVariable("RecruitmentDocsPath");
            if (string.IsNullOrWhiteSpace(basePath))
                throw new InvalidOperationException(
                    "Variabila de sistem RecruitmentDocsPath nu este setata.");

            string candidateFolder = string.Format("{0} - {1}",
                common.PrsnId, common.NumeSalariat);
            string outputDir = Path.Combine(basePath, candidateFolder);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string pdfFileName = BuildPdfFileName(def, formValues, common);
            string pdfPath = Path.Combine(outputDir, pdfFileName);
            string tempDocx = Path.Combine(Path.GetTempPath(),
                string.Format("doc_{0}_{1}.docx", common.PrsnId,
                    Guid.NewGuid().ToString("N").Substring(0, 8)));

            try
            {
                File.Copy(def.TemplatePath, tempDocx, true);
                FillTemplate(tempDocx, def, formValues, common);
                WordHelper.ConvertToPdf(tempDocx, pdfPath);
            }
            finally
            {
                if (File.Exists(tempDocx)) File.Delete(tempDocx);
            }

            return pdfPath;
        }

        /// <summary>
        /// Genereaza DOCX temporar pentru preview (nu salveaza PDF final).
        /// </summary>
        public static string GeneratePreviewDocx(
            DocumentDefinition def,
            Dictionary<string, object> formValues,
            CommonDocumentValues common)
        {
            string tempDocx = Path.Combine(Path.GetTempPath(),
                string.Format("preview_{0}_{1}.docx", common.PrsnId,
                    Guid.NewGuid().ToString("N").Substring(0, 8)));

            File.Copy(def.TemplatePath, tempDocx, true);
            FillTemplate(tempDocx, def, formValues, common);
            return tempDocx;
        }

        // ══════════════════════════════════════════════════════
        //  FILL TEMPLATE
        // ══════════════════════════════════════════════════════
        private static void FillTemplate(
            string docxPath,
            DocumentDefinition def,
            Dictionary<string, object> formValues,
            CommonDocumentValues common)
        {
            using (var doc = WordprocessingDocument.Open(docxPath, true))
            {
                var body = doc.MainDocumentPart.Document.Body;

                // 1. Expandare liste dinamice (inainte de replace simplu)
                ExpandDynamicLists(body, def, formValues);

                // 2. Expandare tabele (ex. ActAditional — ModificareNr)
                ExpandTableRows(body, def, formValues);

                // 2b. Hookuri on_generate cu acces la DocumentBody
                //     (ex: InjectArticoleFinal — numara Art.N si calculeaza articolele finale)
                if (def.Hooks != null)
                {
                    foreach (var hook in def.Hooks.Where(h => h.On == "on_generate"))
                    {
                        var ctx = new HookContext
                        {
                            Definition = def,
                            FormValues = formValues,
                            Common = common,
                            Params = hook.Params,
                            DocumentBody = body,
                            XSupport = BulkContext.XSupport
                        };
                        HookRegistry.RunSingleHook(hook.Handler, ctx);
                    }
                }

                // 3. Build map placeholdere → valori
                var map = BuildPlaceholderMap(def, formValues, common);

                // 4. Replace in tot documentul (body)
                foreach (var para in body.Descendants<Paragraph>().ToList())
                    WordHelper.MergeAndReplace(para, map);

                // 4b. Replace si in header/footer — placeholderele simple (nu liste)
                //     pot ajunge acolo daca autorul templateului a folosit
                //     subsol/antet Word in loc de text in body.
                foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                {
                    foreach (var para in footerPart.Footer.Descendants<Paragraph>().ToList())
                        WordHelper.MergeAndReplace(para, map);
                    footerPart.Footer.Save();
                }
                foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                {
                    foreach (var para in headerPart.Header.Descendants<Paragraph>().ToList())
                        WordHelper.MergeAndReplace(para, map);
                    headerPart.Header.Save();
                }

                doc.MainDocumentPart.Document.Save();
            }
        }

        // ══════════════════════════════════════════════════════
        //  EXPANDARE LISTE DINAMICE
        //  Pentru fiecare camp de tip dynamic_list, gaseste
        //  paragraful marker si il cloneaza per item.
        // ══════════════════════════════════════════════════════
        private static void ExpandDynamicLists(
            Body body,
            DocumentDefinition def,
            Dictionary<string, object> formValues)
        {
            // Procesam atat dynamic_list cat si clauze_editor (acelasi format de date)
            var listFields = def.Sections
                .SelectMany(s => s.Fields)
                .Where(f => f.Type == "dynamic_list" || f.Type == "clauze_editor")
                .ToList();

            foreach (var field in listFields)
            {
                if (!formValues.ContainsKey(field.Key)) continue;

                var rows = formValues[field.Key] as List<Dictionary<string, string>>;
                if (rows == null || rows.Count == 0) continue;

                if (field.Type == "clauze_editor")
                {
                    // Cheile fixe din CollectFormValues pentru clauze
                    var clauzeKeys = new List<string> { "ModificareNr", "ModificareReferinta", "ModificareText" };
                    // Expandam dupa primul placeholder gasit in template
                    foreach (var key in clauzeKeys)
                    {
                        string marker = "{{" + key + "}}";
                        bool found = body.Descendants<Paragraph>()
                            .Any(p => string.Concat(p.Descendants<Text>().Select(t => t.Text)).Contains(marker));
                        if (found)
                        {
                            ExpandParagraphOrRow(body, marker, rows, key, clauzeKeys);
                            break; // un singur marker e suficient — expandeaza tot randul/paragraful
                        }
                    }
                }
                else
                {
                    // Cheile "oficiale" (au propriul item_field) plus cheile mapate
                    // din person_picker (maps{}) care nu au control propriu —
                    // ex. NumeMembruSemnatura / NumeMembruTabel, disponibile in
                    // randul din GetValues() dar fara sa fie declarate ca item_field.
                    var ownKeys = field.ItemFields.Select(f2 => f2.Key).ToList();
                    var mappedKeys = field.ItemFields
                        .Where(f2 => f2.Maps != null)
                        .SelectMany(f2 => f2.Maps.Keys)
                        .Distinct()
                        .ToList();
                    var allKeys = ownKeys.Union(mappedKeys).ToList();

                    // Fiecare cheie (oficiala sau mapata) poate fi un placeholder
                    // expandabil, oriunde apare prima data in document.
                    foreach (var key in allKeys)
                    {
                        string marker = "{{" + key + "}}";
                        ExpandParagraphOrRow(body, marker, rows, key, allKeys);
                    }
                }
            }
        }

        /// <summary>
        /// Gaseste paragraful/randul de tabel cu markerText si il expandeaza per item.
        /// Daca markerul e in tabel → expandeaza TableRow.
        /// Altfel → expandeaza Paragraph.
        /// </summary>
        private static void ExpandParagraphOrRow(
            Body body,
            string markerText,
            List<Dictionary<string, string>> rows,
            string primaryKey,
            List<string> allKeys)
        {
            // Gaseste paragraful cu marker
            Paragraph templatePara = null;
            foreach (var para in body.Descendants<Paragraph>().ToList())
            {
                string text = string.Concat(
                    para.Descendants<Text>().Select(t => t.Text));
                if (text.Contains(markerText)) { templatePara = para; break; }
            }
            if (templatePara == null) return;

            // Verifica daca e in TableRow
            var parentRow = GetParentTableRow(templatePara);

            if (parentRow != null)
            {
                // Expandare la nivel de rand de tabel
                var tableParent = parentRow.Parent;
                foreach (var row in rows)
                {
                    var cloneRow = (TableRow)parentRow.CloneNode(true);
                    ApplyRowMap(cloneRow, row, allKeys);
                    tableParent.InsertBefore(cloneRow, parentRow);
                }
                tableParent.RemoveChild(parentRow);
            }
            else
            {
                // Expandare la nivel de paragraf
                var parent = templatePara.Parent;
                foreach (var row in rows)
                {
                    var clone = (Paragraph)templatePara.CloneNode(true);
                    ApplyParaMap(clone, row, allKeys);
                    parent.InsertBefore(clone, templatePara);
                }
                parent.RemoveChild(templatePara);
            }
        }

        private static void ApplyRowMap(
            TableRow row,
            Dictionary<string, string> values,
            List<string> keys)
        {
            var map = keys.ToDictionary(
                k => "{{" + k + "}}",
                k => values.ContainsKey(k) ? values[k] : string.Empty);

            foreach (var para in row.Descendants<Paragraph>().ToList())
                WordHelper.MergeAndReplace(para, map);
        }

        private static void ApplyParaMap(
            Paragraph para,
            Dictionary<string, string> values,
            List<string> keys)
        {
            var map = keys.ToDictionary(
                k => "{{" + k + "}}",
                k => values.ContainsKey(k) ? values[k] : string.Empty);

            WordHelper.MergeAndReplace(para, map);
        }

        // ══════════════════════════════════════════════════════
        //  EXPANDARE TABELE (ex. ActAditional: {{ModificareNr}})
        //  Tip special: expand_table_row cu table_marker in JSON
        // ══════════════════════════════════════════════════════
        private static void ExpandTableRows(
            Body body,
            DocumentDefinition def,
            Dictionary<string, object> formValues)
        {
            var tableFields = def.Sections
                .SelectMany(s => s.Fields)
                .Where(f => f.Type == "expand_table_row"
                         && !string.IsNullOrEmpty(f.TableMarker))
                .ToList();

            foreach (var field in tableFields)
            {
                if (!formValues.ContainsKey(field.Key)) continue;

                var rows = formValues[field.Key] as List<Dictionary<string, string>>;
                if (rows == null || rows.Count == 0) continue;

                string marker = "{{" + field.TableMarker + "}}";

                // Gaseste tabelul si randul template
                var table = body.Descendants<Table>()
                    .FirstOrDefault(t => WordHelper.GetText(t).Contains(marker));
                if (table == null) continue;

                var templateRow = table.Descendants<TableRow>()
                    .FirstOrDefault(r => WordHelper.GetText(r).Contains(marker));
                if (templateRow == null) continue;

                var allKeys = field.ItemFields.Select(f2 => f2.Key).ToList();

                for (int i = 0; i < rows.Count; i++)
                {
                    var cloneRow = (TableRow)templateRow.CloneNode(true);
                    // Adaugam si indexul (Nr.) automat
                    var rowData = new Dictionary<string, string>(rows[i]);
                    rowData[field.TableMarker] = (i + 1).ToString();
                    ApplyRowMap(cloneRow, rowData, new List<string> { field.TableMarker }.Concat(allKeys).ToList());
                    table.InsertBefore(cloneRow, templateRow);
                }
                table.RemoveChild(templateRow);
            }
        }

        // ══════════════════════════════════════════════════════
        //  BUILD PLACEHOLDER MAP
        //  Combina valorile comune (angajat + companie) cu
        //  valorile din formular intr-un singur dictionar.
        // ══════════════════════════════════════════════════════
        private static Dictionary<string, string> BuildPlaceholderMap(
            DocumentDefinition def,
            Dictionary<string, object> formValues,
            CommonDocumentValues common)
        {
            var map = new Dictionary<string, string>();

            // ── Valori comune angajat ──────────────────────────
            map["{{NumeSalariat}}"] = common.NumeSalariat ?? string.Empty;
            map["{{CNP}}"] = common.CNP ?? string.Empty;
            map["{{Functie}}"] = common.Functie ?? string.Empty;
            map["{{NumeDepartament}}"] = common.NumeDepartament ?? string.Empty;
            map["{{NrCim}}"] = common.NrCim ?? string.Empty;
            map["{{DataCim}}"] = common.DataCim != DateTime.MinValue
                ? common.DataCim.ToString("dd.MM.yyyy") : string.Empty;
            map["{{SerieCI}}"] = common.SerieCI ?? string.Empty;
            map["{{NrCI}}"] = common.NrCI ?? string.Empty;
            map["{{Domiciliu}}"] = common.Domiciliu ?? string.Empty;
            map["{{CodInregistrare}}"] = common.CodInregistrare ?? string.Empty;

            // ── Valori comune companie ─────────────────────────
            map["{{NumeAngajator}}"] = common.NumeAngajator ?? string.Empty;
            map["{{CIFAngajator}}"] = common.CIFAngajator ?? string.Empty;
            map["{{ReprezentantLegal}}"] = common.ReprezentantLegal ?? string.Empty;
            map["{{FunctieReprezentant}}"] = common.FunctieReprezentant ?? string.Empty;
            map["{{AdresaCompanie}}"] = common.AdresaCompanie ?? string.Empty;
            map["{{ZipCompanie}}"] = common.ZipCompanie ?? string.Empty;
            map["{{NrRegComertului}}"] = common.NrRegComertului ?? string.Empty;
            map["{{IbanCompanie}}"] = common.IbanCompanie ?? string.Empty;
            map["{{NrTelefonCompanie}}"] = common.NrTelefonCompanie ?? string.Empty;
            map["{{EmailCompanie}}"] = common.EmailCompanie ?? string.Empty;
            map["{{WebsiteCompanie}}"] = common.WebsiteCompanie ?? string.Empty;
            map["{{MentiuniDocument}}"] = common.MentiuniDocument ?? string.Empty;

            // ── Placeholder-e generate de hooks ───────────────
            map["{{ArticolCompartiment}}"] = common.ArticolCompartiment ?? string.Empty;
            map["{{ArticolContestatie}}"] = common.ArticolContestatie ?? string.Empty;

            // ── Valori din formular ────────────────────────────
            foreach (var kv in formValues)
            {
                // Listele dinamice si tabelele sunt deja expandate — sarim
                if (kv.Value is List<Dictionary<string, string>>) continue;

                string placeholder = "{{" + kv.Key + "}}";
                map[placeholder] = kv.Value != null
                    ? kv.Value.ToString()
                    : string.Empty;
            }

            return map;
        }

        // ══════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════
        private static TableRow GetParentTableRow(OpenXmlElement element)
        {
            var current = element.Parent;
            while (current != null)
            {
                var row = current as TableRow;
                if (row != null) return row;
                current = current.Parent;
            }
            return null;
        }

        private static string BuildPdfFileName(
            DocumentDefinition def,
            Dictionary<string, object> formValues,
            CommonDocumentValues common)
        {
            string titleSafe = WordHelper.SanitizeFileName(def.Title);
            string codInreg = WordHelper.SanitizeFileName(common.CodInregistrare ?? string.Empty);
            string data = DateTime.Today.ToString("dd-MM-yyyy");

            // Incearca sa foloseasca data din campul de registratura
            if (!string.IsNullOrEmpty(def.RegistraturaDateField)
                && formValues.ContainsKey(def.RegistraturaDateField))
            {
                DateTime d;
                if (DateTime.TryParse(formValues[def.RegistraturaDateField]?.ToString(), out d))
                    data = d.ToString("dd-MM-yyyy");
            }

            return string.Format("{0}_{1}_{2}.pdf", titleSafe, codInreg, data);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  COMMON DOCUMENT VALUES
    //  Date comune angajat + companie, populate din ERP
    //  inainte de deschiderea formularului.
    // ══════════════════════════════════════════════════════════
    public class CommonDocumentValues
    {
        // ── Angajat ───────────────────────────────────────────
        public int PrsnId { get; set; }
        public string NumeSalariat { get; set; }
        public string CNP { get; set; }
        public string Functie { get; set; }
        public string NumeDepartament { get; set; }
        public string NrCim { get; set; }
        public DateTime DataCim { get; set; }
        public string SerieCI { get; set; }
        public string NrCI { get; set; }
        public string Domiciliu { get; set; }
        public string CodInregistrare { get; set; }

        // ── Companie ──────────────────────────────────────────
        public string NumeAngajator { get; set; }
        public string CIFAngajator { get; set; }
        public string ReprezentantLegal { get; set; }
        public string FunctieReprezentant { get; set; }
        public string AdresaCompanie { get; set; }
        public string ZipCompanie { get; set; }
        public string NrRegComertului { get; set; }
        public string IbanCompanie { get; set; }
        public string NrTelefonCompanie { get; set; }
        public string EmailCompanie { get; set; }
        public string WebsiteCompanie { get; set; }

        // ── Mentiuni + Articole (populate de hooks) ───────────
        public string MentiuniDocument { get; set; }
        public string ArticolCompartiment { get; set; }
        public string ArticolContestatie { get; set; }

        public CommonDocumentValues()
        {
            NumeSalariat = string.Empty;
            CNP = string.Empty;
            Functie = string.Empty;
            NumeDepartament = string.Empty;
            NrCim = string.Empty;
            DataCim = DateTime.MinValue;
            SerieCI = string.Empty;
            NrCI = string.Empty;
            Domiciliu = string.Empty;
            CodInregistrare = string.Empty;
            NumeAngajator = string.Empty;
            CIFAngajator = string.Empty;
            ReprezentantLegal = string.Empty;
            FunctieReprezentant = string.Empty;
            AdresaCompanie = string.Empty;
            ZipCompanie = string.Empty;
            NrRegComertului = string.Empty;
            IbanCompanie = string.Empty;
            NrTelefonCompanie = string.Empty;
            EmailCompanie = string.Empty;
            WebsiteCompanie = string.Empty;
            MentiuniDocument = string.Empty;
            ArticolCompartiment = string.Empty;
            ArticolContestatie = string.Empty;
        }

        /// <summary>
        /// Populeaza din ErpCimData + ErpCompanyData (bridge catre vechiul sistem).
        /// </summary>
        public static CommonDocumentValues FromErp(
            int prsnId,
            string numeSalariat,
            string cnp,
            string functie,
            ErpCimData cimData,
            ErpCompanyData companyData)
        {
            return new CommonDocumentValues
            {
                PrsnId = prsnId,
                NumeSalariat = numeSalariat,
                CNP = cnp,
                Functie = functie,
                NumeDepartament = cimData.NumeDepartament,
                NrCim = cimData.NrCim,
                DataCim = cimData.DataCim,
                SerieCI = cimData.SerieCI,
                NrCI = cimData.NrCI,
                Domiciliu = cimData.Domiciliu,
                NumeAngajator = companyData.NumeAngajator,
                CIFAngajator = companyData.CIFAngajator,
                ReprezentantLegal = companyData.ReprezentantLegal,
                FunctieReprezentant = companyData.FunctieReprezentant,
                AdresaCompanie = companyData.AdresaCompanie,
                ZipCompanie = companyData.ZipCompanie,
                NrRegComertului = companyData.NrRegComertului,
                IbanCompanie = companyData.IbanCompanie,
                NrTelefonCompanie = companyData.NrTelefonCompanie,
                EmailCompanie = companyData.EmailCompanie,
                WebsiteCompanie = companyData.WebsiteCompanie,
            };
        }
    }
}