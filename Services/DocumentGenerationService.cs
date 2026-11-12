using System;
using System.Collections.Generic;
using System.IO;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.Services
{
    // ══════════════════════════════════════════════════════════
    //  DOCUMENT GENERATION SERVICE
    //  Logica de previzualizare / generare PDF + registratura,
    //  (folosita de WebDocumentForm).
    //  Fara UI: confirmarea si mesajul de succes le arata pagina.
    // ══════════════════════════════════════════════════════════
    public static class DocumentGenerationService
    {
        public static bool UsesRegistratura(DocumentDefinition def)
        {
            return def.UsesRegistratura && RegistraturaService.Instance != null;
        }

        // Data de inregistrare = campul registratura_date_field (dd.MM.yyyy), altfel azi
        public static DateTime GetRegistraturaDate(DocumentDefinition def, Dictionary<string, object> formValues)
        {
            if (!string.IsNullOrEmpty(def.RegistraturaDateField)
                && formValues.ContainsKey(def.RegistraturaDateField))
            {
                DateTime d;
                if (DateTime.TryParse(formValues[def.RegistraturaDateField]?.ToString(), out d))
                    return d;
            }
            return DateTime.Today;
        }

        // Cod de inregistrare proaspat pentru data data (actualizeaza common)
        public static void RecalcCod(CommonDocumentValues common, DateTime data)
        {
            if (RegistraturaService.Instance == null) return;
            common.CodInregistrare = RegistraturaService.Instance.CalculateCod(data);
        }

        // PDF temporar pentru previzualizare (se sterge de apelant)
        // emptyMarkers (optional, formularul web): cheie camp necompletat -> obligatoriu?
        public static string GeneratePreviewPdf(DocumentDefinition def,
            Dictionary<string, object> formValues, CommonDocumentValues common,
            Dictionary<string, bool> emptyMarkers = null)
        {
            string tempDocx = DynamicTemplateEngine.GeneratePreviewDocx(def, formValues, common, emptyMarkers);
            string tempPdf = Path.ChangeExtension(tempDocx, ".pdf");
            try
            {
                WordHelper.ConvertToPdf(tempDocx, tempPdf);
            }
            finally
            {
                TryDeleteFile(tempDocx);
            }
            return tempPdf;
        }

        // ══════════════════════════════════════════════════════
        //  Generare PDF final + inregistrare. Arunca exceptie la eroare.
        //  1. PDF-ul se genereaza INAINTE de inregistrare: daca generarea
        //     esueaza, nu ramane niciun rand in registratura.
        //     Codul se recalculeaza chiar inainte de generare si se verifica
        //     ca e inca liber; daca intre timp a fost luat, se regenereaza
        //     PDF-ul cu codul urmator (max 3 incercari).
        //  2. Inregistrare. Daca esueaza, PDF-ul generat se sterge, ca sa nu
        //     ramana un document cu un cod neinregistrat.
        //  common.CodInregistrare = codul folosit. Returneaza calea PDF-ului.
        // ══════════════════════════════════════════════════════
        public static string GenerateAndRegister(DocumentDefinition def,
            Dictionary<string, object> formValues, CommonDocumentValues common, DateTime dataReg)
        {
            bool cuRegistratura = UsesRegistratura(def);
            string pdfPath = null;

            const int MaxIncercari = 3;
            for (int incercare = 1; ; incercare++)
            {
                if (cuRegistratura) RecalcCod(common, dataReg);
                pdfPath = DynamicTemplateEngine.GeneratePdf(def, formValues, common);

                if (!cuRegistratura
                    || !RegistraturaService.Instance.CodExista(common.CodInregistrare))
                    break;

                TryDeleteFile(pdfPath);
                pdfPath = null;
                if (incercare >= MaxIncercari)
                    throw new InvalidOperationException(
                        "Nu s-a putut obține un cod de înregistrare liber. Încearcă din nou.");
            }

            if (cuRegistratura)
            {
                try
                {
                    RegistraturaService.Instance.Inregistreaza(
                        common.CodInregistrare, dataReg,
                        def.RegistraturaTipDocPk,
                        def.Title,
                        common.PrsnId);
                }
                catch
                {
                    TryDeleteFile(pdfPath);
                    throw;
                }

                // Insert in tabela specifica categoriei (Acte Aditionale / Decizii / PV)
                // Transmitem titlul documentului prin formValues pentru tipDcz/pvType
                formValues["_DocTitle"] = def.Title;
                RegistraturaService.Instance.InregistreazaTabelaSpecifica(
                    def.Category,
                    common.CodInregistrare,
                    dataReg,
                    common.PrsnId,
                    formValues);
            }

            return pdfPath;
        }

        public static void TryDeleteFile(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                try { File.Delete(path); } catch { }
        }
    }
}
