using System;
using System.Collections.Generic;

namespace ActAditionalPlugin.Models
{
    // ══════════════════════════════════════════════════════════
    //  MODEL DE BAZA — campuri comune tuturor documentelor
    // ══════════════════════════════════════════════════════════
    public abstract class DocumentModelBase
    {
        // ── Angajat (din ERP) ─────────────────────────────────
        public int PrsnId { get; set; }
        public string NumeSalariat { get; set; }
        public string CNP { get; set; }
        public string Functie { get; set; }
        public string NrCim { get; set; }
        public DateTime DataCim { get; set; }

        // ── Angajator (din PluginConfig) ──────────────────────
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

        // ── Tip document (pentru routing in TemplateEngine) ───
        public abstract TipDocument TipDocument { get; }

        public DocumentModelBase()
        {
            NumeSalariat = string.Empty;
            CNP = string.Empty;
            Functie = string.Empty;
            NrCim = string.Empty;
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
        }
    }

    // ══════════════════════════════════════════════════════════
    //  ENUM tip document
    // ══════════════════════════════════════════════════════════
    public enum TipDocument
    {
        ActAditional = 0,
        SuspendareCresterecopil = 1,
        SuspendareCresterecopilHandicap = 2,
        SuspendareAbsenteNemotivate = 3,
        SuspendareAcordParti = 4,
        SuspendareSiIncetareSuspendare = 5,
        IncetareSuspendare = 6,
        IncetareDemisie = 7,
        IncetareExpirare = 8,
        IncetareDisciplinar = 9
    }

    // ══════════════════════════════════════════════════════════
    //  1. ACT ADITIONAL
    // ══════════════════════════════════════════════════════════
    public class ActAditionalModel : DocumentModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.ActAditional; } }

        public string CodInregistrare { get; set; }
        public DateTime DataEmitereAct { get; set; }
        public DateTime DataVigoare { get; set; }

        // Modificari Art. I
        public List<PunctModificare> Modificari { get; set; }

        public ActAditionalModel()
        {
            CodInregistrare = string.Empty;
            Modificari = new List<PunctModificare>();
        }
    }

    public class PunctModificare
    {
        public string Referinta { get; set; }
        public string TextModificare { get; set; }

        public PunctModificare()
        {
            Referinta = string.Empty;
            TextModificare = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MODEL BAZA DECIZIE — campuri comune tuturor deciziilor
    // ══════════════════════════════════════════════════════════
    public abstract class DecizieModelBase : DocumentModelBase
    {
        public string CodInregistrare { get; set; }
        public DateTime DataDecizie { get; set; }

        public DecizieModelBase()
        {
            CodInregistrare = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MODEL BAZA DECIZIE CU CERERE
    // ══════════════════════════════════════════════════════════
    public abstract class DecizieModelCuCerere : DecizieModelBase
    {
        public string NrCerere { get; set; }
        public DateTime DataCerere { get; set; }

        public DecizieModelCuCerere()
        {
            NrCerere = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  2. SUSPENDARE — crestere copil
    // ══════════════════════════════════════════════════════════
    public class SuspendareCresterecopilModel : DecizieModelCuCerere
    {
        public override TipDocument TipDocument { get { return TipDocument.SuspendareCresterecopil; } }

        public DateTime DataStartSuspendare { get; set; }
        public string PerioadaSuspendare { get; set; }  // ex: "2 ani"
        public string NumeCopil { get; set; }
        public string SerieCertificat { get; set; }
        public string NrCertificat { get; set; }

        public SuspendareCresterecopilModel()
        {
            PerioadaSuspendare = string.Empty;
            NumeCopil = string.Empty;
            SerieCertificat = string.Empty;
            NrCertificat = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  3. SUSPENDARE — crestere copil cu handicap
    // ══════════════════════════════════════════════════════════
    public class SuspendareCresterecopilHandicapModel : DecizieModelCuCerere
    {
        public override TipDocument TipDocument { get { return TipDocument.SuspendareCresterecopilHandicap; } }

        public DateTime DataStartSuspendare { get; set; }
        public DateTime DataEndSuspendare { get; set; }
        public string PerioadaSuspendare { get; set; }
        public string NumeCopil { get; set; }
        public string SerieCertificat { get; set; }
        public string NrCertificat { get; set; }
        public string GradHandicap { get; set; }  // ex: "mediu", "grav"
        public string NrCertificatHandicap { get; set; }
        public DateTime DataCertificatHandicap { get; set; }

        public SuspendareCresterecopilHandicapModel()
        {
            PerioadaSuspendare = string.Empty;
            NumeCopil = string.Empty;
            SerieCertificat = string.Empty;
            NrCertificat = string.Empty;
            GradHandicap = string.Empty;
            NrCertificatHandicap = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  4. SUSPENDARE — absente nemotivate (art. 51 alin. 2)
    //  Structura bazata pe referat, nu cerere (ca la disciplinar)
    // ══════════════════════════════════════════════════════════
    public class SuspendareAbsenteNemotivateModel : DecizieModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.SuspendareAbsenteNemotivate; } }

        public string NrReferat { get; set; }
        public DateTime DataReferat { get; set; }
        public string IntocmitDe { get; set; }
        public DateTime DataStartSuspendare { get; set; }
        public DateTime DataEndSuspendare { get; set; }
        public DateTime DataIncetareSuspendare { get; set; }
        public bool IncludeIncetare { get; set; }  // alege template-ul

        public SuspendareAbsenteNemotivateModel()
        {
            NrReferat = string.Empty;
            IntocmitDe = string.Empty;
            IncludeIncetare = true;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  5. SUSPENDARE — acordul partilor (art. 54)
    // ══════════════════════════════════════════════════════════
    public class SuspendareAcordPartiModel : DecizieModelCuCerere
    {
        public override TipDocument TipDocument { get { return TipDocument.SuspendareAcordParti; } }

        public DateTime DataStartSuspendare { get; set; }
        public DateTime DataEndSuspendare { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    //  6. SUSPENDARE + INCETARE SUSPENDARE (combinat)
    // ══════════════════════════════════════════════════════════
    public class SuspendareSiIncetareSuspendareModel : DecizieModelCuCerere
    {
        public override TipDocument TipDocument { get { return TipDocument.SuspendareSiIncetareSuspendare; } }

        public DateTime DataStartSuspendare { get; set; }
        public DateTime DataEndSuspendare { get; set; }
        public DateTime DataIncetareSuspendare { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    //  7. INCETARE SUSPENDARE
    // ══════════════════════════════════════════════════════════
    public class IncetareSuspendareModel : DecizieModelCuCerere
    {
        public override TipDocument TipDocument { get { return TipDocument.IncetareSuspendare; } }

        public DateTime DataIncetareSuspendare { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    //  8. INCETARE CIM — demisie
    // ══════════════════════════════════════════════════════════
    public class IncetareDemisieModel : DecizieModelCuCerere
    {
        public override TipDocument TipDocument { get { return TipDocument.IncetareDemisie; } }

        public DateTime DataIncetare { get; set; }
        public string ArticolDemisie { get; set; }  // ex: "81 alin. 1" sau "81 alin. 7"

        public IncetareDemisieModel()
        {
            ArticolDemisie = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  9. INCETARE CIM — expirare termen
    // ══════════════════════════════════════════════════════════
    public class IncetareExpirareModel : DecizieModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.IncetareExpirare; } }

        public DateTime DataIncetare { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    //  10. INCETARE CIM — disciplinar
    // ══════════════════════════════════════════════════════════
    public class IncetareDisciplinarModel : DecizieModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.IncetareDisciplinar; } }

        public DateTime DataIncetare { get; set; }
        public string PerioadaCercetare { get; set; }  // ex: "12.06.2025 – 27.06.2025"
        public string NrProcesVerbal { get; set; }
        public DateTime DataProcesVerbal { get; set; }
        public string LocCercetare { get; set; }
        public string MotiveleSanctionarii { get; set; }
        public string ImprejurariFapte { get; set; }
        public string GradVinovatie { get; set; }
        public string ConsecinteAbateri { get; set; }
        public string NumeIntocmit { get; set; }  // ex: "Marin Iulia-Alina"
        public string FunctieIntocmit { get; set; }  // ex: "Specialist Resurse Umane"

        public IncetareDisciplinarModel()
        {
            PerioadaCercetare = string.Empty;
            NrProcesVerbal = string.Empty;
            LocCercetare = string.Empty;
            MotiveleSanctionarii = string.Empty;
            ImprejurariFapte = string.Empty;
            GradVinovatie = string.Empty;
            ConsecinteAbateri = string.Empty;
            NumeIntocmit = string.Empty;
            FunctieIntocmit = string.Empty;
        }
    }
}