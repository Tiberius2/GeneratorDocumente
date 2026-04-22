using System;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.Services
{
    /// <summary>
    /// Construieste textul juridic al fiecarei clauze.
    /// Referintele sunt mapate exact dupa contractul individual de munca Vatra Domneasca.
    /// Zero dependinte de WinForms.
    /// </summary>
    public static class ClauzeTextBuilder
    {
        // ── Referinte exacte din CIM ──────────────────────────
        public static readonly string RefLocMunca = "La litera E, pct. 1, \"Activitatea se desfasoara la...\"";
        public static readonly string RefFunctie = "La litera F, \"Functia/meseria\"";
        public static readonly string RefProgram = "La litera G, pct. 1, \"durata normala a timpului de lucru\"";
        public static readonly string RefConcediu = "La litera H, \"Durata concediului anual de odihna\"";
        public static readonly string RefConcediuSup = "La litera H, \"concediul suplimentar\"";
        public static readonly string RefSalariu = "La litera I, pct. 1, \"Salariul de baza lunar brut\"";
        public static readonly string RefSporuri = "La litera I, pct. 2, lit. a), \"sporuri\"";
        public static readonly string RefIndemnizatii = "La litera I, pct. 2, lit. b), \"indemnizatii\"";
        public static readonly string RefDataPlata = "La litera I, pct. 5, \"Data la care se plateste salariul\"";
        public static readonly string RefPreavizConc = "La litera J, lit. a), \"perioada de preaviz in cazul concedierii\"";
        public static readonly string RefPreavizDem = "La litera J, lit. b), \"perioada de preaviz in cazul demisiei\"";
        public static readonly string RefDurata = "La litera C, \"durata contractului\"";
        public static readonly string RefFormare = "La litera O, \"Formarea profesionala\"";
        public static readonly string RefDrepturi = "La litera R, \"Drepturi si obligatii generale ale partilor\"";

        // ── Constructori ──────────────────────────────────────

        public static PunctModificare BuildLocMunca(string descriere)
        {
            return new PunctModificare
            {
                Referinta = RefLocMunca,
                TextModificare = descriere.Trim()
            };
        }

        public static PunctModificare BuildFunctie(string functieNoua, string codCor)
        {
            string cor = string.IsNullOrWhiteSpace(codCor) ? "" : " - " + codCor.Trim();
            return new PunctModificare
            {
                Referinta = RefFunctie,
                TextModificare = string.Format(
                    "Functia/meseria: {0}{1} conform Clasificarii Ocupatiilor din Romania.",
                    functieNoua.Trim().ToUpper(), cor)
            };
        }

        public static PunctModificare BuildProgram(string descriereLibera)
        {
            return new PunctModificare
            {
                Referinta = RefProgram,
                TextModificare = descriereLibera.Trim()
            };
        }

        public static PunctModificare BuildConcediu(int zileBaza)
        {
            return new PunctModificare
            {
                Referinta = RefConcediu,
                TextModificare = string.Format(
                    "Durata concediului anual de odihna este de {0} zile lucratoare.", zileBaza)
            };
        }

        public static PunctModificare BuildConcediuSuplimentar(int zileSuplimentar)
        {
            return new PunctModificare
            {
                Referinta = RefConcediuSup,
                TextModificare = string.Format(
                    "Salariatul beneficiaza de un concediu suplimentar cu o durata de {0} zile lucratoare.", zileSuplimentar)
            };
        }

        public static PunctModificare BuildSalariu(decimal salariuNou)
        {
            return new PunctModificare
            {
                Referinta = RefSalariu,
                TextModificare = string.Format("Salariul de baza lunar brut: {0:N0} Lei.", salariuNou)
            };
        }

        public static PunctModificare BuildSporuri(string descriereLibera)
        {
            return new PunctModificare
            {
                Referinta = RefSporuri,
                TextModificare = descriereLibera.Trim()
            };
        }

        public static PunctModificare BuildIndemnizatii(string descriereLibera)
        {
            return new PunctModificare
            {
                Referinta = RefIndemnizatii,
                TextModificare = descriereLibera.Trim()
            };
        }

        public static PunctModificare BuildDataPlata(int ziPlata)
        {
            return new PunctModificare
            {
                Referinta = RefDataPlata,
                TextModificare = string.Format(
                    "Data la care se plateste salariul este data de {0} ale lunii urmatoare. " +
                    "Daca data de {0} va fi intr-o zi nelucratoare, plata salariului se va efectua in urmatoarea zi lucratoare.",
                    ziPlata)
            };
        }

        public static PunctModificare BuildPreavizConcediere(int zile)
        {
            return new PunctModificare
            {
                Referinta = RefPreavizConc,
                TextModificare = string.Format(
                    "Perioada de preaviz in cazul concedierii este de {0} zile lucratoare, conform Legii nr. 53/2003 - Codul muncii.", zile)
            };
        }

        public static PunctModificare BuildPreavizDemisie(int zile)
        {
            return new PunctModificare
            {
                Referinta = RefPreavizDem,
                TextModificare = string.Format(
                    "Perioada de preaviz in cazul demisiei este de {0} zile lucratoare, conform Legii nr. 53/2003 - Codul muncii.", zile)
            };
        }

        public static PunctModificare BuildDurata(bool determinat, DateTime? dataExpirare)
        {
            string text;
            if (determinat && dataExpirare.HasValue)
                text = string.Format(
                    "Contractul individual de munca se modifica in contract pe perioada determinata, pana la data de {0}.",
                    dataExpirare.Value.ToString("dd.MM.yyyy"));
            else
                text = "Contractul individual de munca se modifica in contract pe perioada nedeterminata.";

            return new PunctModificare
            {
                Referinta = RefDurata,
                TextModificare = text
            };
        }

        public static PunctModificare BuildFormare(string descriereLibera)
        {
            return new PunctModificare
            {
                Referinta = RefFormare,
                TextModificare = descriereLibera.Trim()
            };
        }

        public static PunctModificare BuildDrepturi(string descriereLibera)
        {
            return new PunctModificare
            {
                Referinta = RefDrepturi,
                TextModificare = descriereLibera.Trim()
            };
        }

        public static PunctModificare BuildTextLiber(string text)
        {
            return new PunctModificare
            {
                Referinta = "",
                TextModificare = text.Trim()
            };
        }
    }
}