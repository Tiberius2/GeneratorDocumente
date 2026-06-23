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
        public string NumeDepartament { get; set; }
        public string NrCim { get; set; }
        public DateTime DataCim { get; set; }

        // ── Angajator (din PluginConfig / ERP) ───────────────
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

        // ── Tip document ──────────────────────────────────────
        public abstract TipDocument TipDocument { get; }

        // ── Registratura ──────────────────────────────────────
        public string CodInregistrare { get; set; }

        // ── Mentiuni / Observatii ─────────────────────────────
        public string MentiuniDocument { get; set; }

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
            CodInregistrare = string.Empty;
            MentiuniDocument = string.Empty;
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
        IncetareDisciplinar = 9,
        IncetarePerioadaProba = 10,
        ReferatDisciplinar = 11,
        AvertismentDisciplinar = 12,
        DecizieConstituireComisie = 13,
        ConvocareCercetare = 14,
        ProcesVerbalCercetare = 15
    }

    // ══════════════════════════════════════════════════════════
    //  1. ACT ADITIONAL
    // ══════════════════════════════════════════════════════════
    public class ActAditionalModel : DocumentModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.ActAditional; } }

        public DateTime DataEmitereAct { get; set; }
        public DateTime DataVigoare { get; set; }
        public List<PunctModificare> Modificari { get; set; }

        public ActAditionalModel()
        {
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
    //  MODEL BAZA DECIZIE
    // ══════════════════════════════════════════════════════════
    public abstract class DecizieModelBase : DocumentModelBase
    {
        public DateTime DataDecizie { get; set; }
        // CodInregistrare e pe DocumentModelBase
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
        public DateTime DataEndSuspendare { get; set; }
        public string PerioadaSuspendare { get; set; }
        public string NumeCopil { get; set; }
        public string CNPCopil { get; set; }
        public string SerieCertificat { get; set; }
        public string NrCertificat { get; set; }

        public SuspendareCresterecopilModel()
        {
            PerioadaSuspendare = string.Empty;
            NumeCopil = string.Empty;
            CNPCopil = string.Empty;
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
        public string CNPCopil { get; set; }
        public string SerieCertificat { get; set; }
        public string NrCertificat { get; set; }
        public string GradHandicap { get; set; }
        public string NrCertificatHandicap { get; set; }
        public DateTime DataCertificatHandicap { get; set; }

        public SuspendareCresterecopilHandicapModel()
        {
            PerioadaSuspendare = string.Empty;
            NumeCopil = string.Empty;
            CNPCopil = string.Empty;
            SerieCertificat = string.Empty;
            NrCertificat = string.Empty;
            GradHandicap = string.Empty;
            NrCertificatHandicap = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  4. SUSPENDARE — absente nemotivate
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
        public bool IncludeIncetare { get; set; }

        public SuspendareAbsenteNemotivateModel()
        {
            NrReferat = string.Empty;
            IntocmitDe = string.Empty;
            IncludeIncetare = true;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  5. SUSPENDARE — acordul partilor
    // ══════════════════════════════════════════════════════════
    public class SuspendareAcordPartiModel : DecizieModelCuCerere
    {
        public override TipDocument TipDocument { get { return TipDocument.SuspendareAcordParti; } }

        public DateTime DataStartSuspendare { get; set; }
        public DateTime DataEndSuspendare { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    //  6. SUSPENDARE + INCETARE SUSPENDARE
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
        public string ArticolDemisie { get; set; }

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
        public string PerioadaCercetare { get; set; }
        public string NrProcesVerbal { get; set; }
        public DateTime DataProcesVerbal { get; set; }
        public string LocCercetare { get; set; }
        public string MotiveleSanctionarii { get; set; }
        public string ImprejurariFapte { get; set; }
        public string GradVinovatie { get; set; }
        public string ConsecinteAbateri { get; set; }
        public string NumeIntocmit { get; set; }
        public string FunctieIntocmit { get; set; }

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

    // ══════════════════════════════════════════════════════════
    //  11. INCETARE CIM — perioada de proba
    // ══════════════════════════════════════════════════════════
    public class IncetarePerioadaProbaModel : DecizieModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.IncetarePerioadaProba; } }

        public DateTime DataIncetare { get; set; }
        public string NrNotificare { get; set; }
        public DateTime DataNotificare { get; set; }

        public IncetarePerioadaProbaModel()
        {
            NrNotificare = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  REFERAT DISCIPLINAR
    // ══════════════════════════════════════════════════════════
    public class ReferatDisciplinarModel : DocumentModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.ReferatDisciplinar; } }

        public DateTime DataReferat { get; set; }
        public string NumeAutorReferat { get; set; }
        public string FunctieAutorReferat { get; set; }
        public string LocMunca { get; set; }
        public string DescriereFapta { get; set; }
        public string ConsecinteAbateri { get; set; }
        public string TemeiLegal { get; set; }

        public ReferatDisciplinarModel()
        {
            NumeAutorReferat = string.Empty;
            FunctieAutorReferat = string.Empty;
            LocMunca = string.Empty;
            DescriereFapta = string.Empty;
            ConsecinteAbateri = string.Empty;
            TemeiLegal = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  AVERTISMENT DISCIPLINAR
    // ══════════════════════════════════════════════════════════
    public class AvertismentDisciplinarModel : DocumentModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.AvertismentDisciplinar; } }

        public DateTime DataDecizie { get; set; }
        public string NrReferat { get; set; }
        public DateTime DataReferat { get; set; }
        public string NumeAutorReferat { get; set; }
        public string FunctieAutorReferat { get; set; }
        public string LocMunca { get; set; }
        public string DescriereAbateri { get; set; }
        public string DescriereAbateriDetaliat { get; set; }
        public DateTime DataComunicare { get; set; }

        public AvertismentDisciplinarModel()
        {
            NrReferat = string.Empty;
            NumeAutorReferat = string.Empty;
            FunctieAutorReferat = string.Empty;
            LocMunca = string.Empty;
            DescriereAbateri = string.Empty;
            DescriereAbateriDetaliat = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  DECIZIE CONSTITUIRE COMISIE CERCETARE DISCIPLINARA
    // ══════════════════════════════════════════════════════════
    // ── DTO-uri pentru liste dinamice ─────────────────────────
    public class ReferatSursaItem
    {
        public string CodSiData { get; set; }   // ex. "168/14.04.2025"
        public string Intocmitor { get; set; }  // ex. "Aniculaesei Anisoara — Contabil"
    }

    public class MembruComisieItem
    {
        public string Nume { get; set; }
        public string Functie { get; set; }
    }

    public class DecizieConstituireComisieModel : DocumentModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.DecizieConstituireComisie; } }

        public DateTime DataDecizie { get; set; }
        public string NumeIntocmitorHr { get; set; }
        public DateTime DataNotaExplicativa { get; set; }
        public string DescriereAbatere { get; set; }
        public string IntervalAniCCM { get; set; }
        public string CodInregistrareITM { get; set; }

        // Lista dinamica referate
        public List<ReferatSursaItem> Referate { get; set; }

        // Comisia
        public string NumePresedinte { get; set; }
        public string FunctiePresedinte { get; set; }
        public List<MembruComisieItem> Membri { get; set; }
        public string NumeObservator { get; set; }
        public string FunctieObservator { get; set; }

        // Perioada cercetare
        public DateTime DataInceputCercetare { get; set; }
        public DateTime DataSfarsitCercetare { get; set; }

        public DecizieConstituireComisieModel()
        {
            DescriereAbatere = string.Empty;
            NumeIntocmitorHr = "Marin Iulia Alina";
            IntervalAniCCM = "2024-2026";
            CodInregistrareITM = "6123/CCMMRM/19.07.2024";
            Referate = new List<ReferatSursaItem>();
            NumePresedinte = string.Empty;
            FunctiePresedinte = string.Empty;
            Membri = new List<MembruComisieItem>();
            NumeObservator = string.Empty;
            FunctieObservator = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  CONVOCARE CERCETARE DISCIPLINARA
    // ══════════════════════════════════════════════════════════
    public class ConvocareCercetareModel : DocumentModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.ConvocareCercetare; } }

        // Date convocare
        public DateTime DataConvocare { get; set; }
        public string CodCor { get; set; }

        // Referatele sursa (refolosim acelasi DTO)
        public List<ReferatSursaItem> Referate { get; set; }
        public DateTime DataNotaExplicativa { get; set; }

        // Descriere abatere
        public string DescriereAbatere { get; set; }

        // Precompletate, editabile
        public string IntervalAniCCM { get; set; }
        public string CodInregistrareITM { get; set; }

        // Detalii cercetare
        public string LocCercetare { get; set; }
        public DateTime DataCercetare { get; set; }
        public string OraConvocare { get; set; }

        // Decizia comisiei
        public string NrDecizieComisie { get; set; }
        public DateTime DataDecizieComisie { get; set; }

        // Membri comisie (lista dinamica)
        public List<MembruComisieItem> Membri { get; set; }

        // Intocmitor HR
        public string NumeIntocmitorHr { get; set; }

        public ConvocareCercetareModel()
        {
            CodCor = string.Empty;
            Referate = new List<ReferatSursaItem>();
            DescriereAbatere = string.Empty;
            IntervalAniCCM = "2024-2026";
            CodInregistrareITM = "6123/CCMMRM/19.07.2024";
            LocCercetare = "Localitatea Cătămărăști Deal, Județul Botoșani, la Biroul Administrativ";
            OraConvocare = string.Empty;
            NrDecizieComisie = string.Empty;
            Membri = new List<MembruComisieItem>();
            NumeIntocmitorHr = "Marin Iulia Alina";
        }
    }

    // ══════════════════════════════════════════════════════════
    //  PROCES VERBAL CERCETARE DISCIPLINARA
    // ══════════════════════════════════════════════════════════
    public class ProcesVerbalCercetareModel : DocumentModelBase
    {
        public override TipDocument TipDocument { get { return TipDocument.ProcesVerbalCercetare; } }

        public DateTime DataCercetare { get; set; }
        public string LocCercetare { get; set; }
        public string NrDecizieComisie { get; set; }
        public string DataDecizieComisie { get; set; }
        public string DataNotaExplicativa { get; set; }
        public string DescriereAbatere { get; set; }
        public string ConcluziiComisie { get; set; }
        public string SanctiuneaPropusa { get; set; }
        public List<MembruComisieItem> Membri { get; set; }

        public ProcesVerbalCercetareModel()
        {
            LocCercetare = "Biroul Administrativ al Societatii din Sat Catamarasti-Deal, Com. Mihai Eminescu, Jud. Botosani";
            NrDecizieComisie = string.Empty;
            DataDecizieComisie = string.Empty;
            DataNotaExplicativa = string.Empty;
            DescriereAbatere = string.Empty;
            ConcluziiComisie = string.Empty;
            SanctiuneaPropusa = string.Empty;
            Membri = new List<MembruComisieItem>();
        }
    }
}