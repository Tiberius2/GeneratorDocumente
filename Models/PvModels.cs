using System;

namespace ActAditionalPlugin.Models
{
    public enum TipPV
    {
        Echipamente = 0,
        Electronice = 1,
        Autovehicul = 2
    }

    public enum TipPredare
    {
        Simplu = 0,
        Exploatare = 1,
        Mentenanta = 2,
        Custodie = 3,
        Administrare = 4,
        Receptie = 5,
        Relocare = 6,
        CasareScoatere = 7
    }

    public abstract class PvModelBase
    {
        public abstract TipPV TipPV { get; }

        public string CodInregistrare { get; set; }
        public DateTime DataPV { get; set; }
        public TipPredare TipPredare { get; set; }

        // Angajat (Primitor)
        public int PrsnId { get; set; }
        public string NumeSalariat { get; set; }
        public string CNP { get; set; }
        public string Functie { get; set; }

        // Angajator (Predator)
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

        protected PvModelBase()
        {
            CodInregistrare = string.Empty;
            DataPV = DateTime.Today;
            TipPredare = TipPredare.Simplu;
            NumeSalariat = string.Empty;
            CNP = string.Empty;
            Functie = string.Empty;
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

        public string GetTipPredareText()
        {
            switch (TipPredare)
            {
                case TipPredare.Exploatare: return "PREDARE-PRIMIRE IN EXPLOATARE";
                case TipPredare.Mentenanta: return "PREDARE-PRIMIRE MENTENANTA";
                case TipPredare.Custodie: return "PREDARE-PRIMIRE IN CUSTODIE";
                case TipPredare.Administrare: return "PREDARE-PRIMIRE ADMINISTRARE";
                case TipPredare.Receptie: return "PREDARE-PRIMIRE RECEPTIE";
                case TipPredare.Relocare: return "PREDARE-PRIMIRE RELOCARE";
                case TipPredare.CasareScoatere: return "PREDARE-PRIMIRE CASARE / SCOATERE DIN UZ";
                default: return "PREDARE-PRIMIRE";
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  PV ECHIPAMENTE / ELECTRONICE
    // ══════════════════════════════════════════════════════════
    public class PvBunItem
    {
        public string Nume { get; set; }
        public string Cantitate { get; set; }
        public string Pret { get; set; }

        public PvBunItem() { Nume = string.Empty; Cantitate = "1"; Pret = string.Empty; }
    }

    public class PvEchipamenteModel : PvModelBase
    {
        public override TipPV TipPV { get { return TipPV.Echipamente; } }
        public System.Collections.Generic.List<PvBunItem> Bunuri { get; set; }
        public string Mentiuni { get; set; }

        public PvEchipamenteModel()
        {
            Bunuri = new System.Collections.Generic.List<PvBunItem>();
            Mentiuni = string.Empty;
        }
    }

    public class PvElecroniceModel : PvModelBase
    {
        public override TipPV TipPV { get { return TipPV.Electronice; } }
        public System.Collections.Generic.List<PvBunItem> Bunuri { get; set; }
        public string Mentiuni { get; set; }

        public PvElecroniceModel()
        {
            Bunuri = new System.Collections.Generic.List<PvBunItem>();
            Mentiuni = string.Empty;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  PV AUTOVEHICUL
    // ══════════════════════════════════════════════════════════
    public class PvAutovehiculModel : PvModelBase
    {
        public override TipPV TipPV { get { return TipPV.Autovehicul; } }

        // ── Al doilea predator (Art. 2) — detalii complete ───
        public string NumePredator2 { get; set; }
        public string FunctiePredator2 { get; set; }
        public string CNPPredator2 { get; set; }
        public string CISeriaPredator2 { get; set; }
        public string CINrPredator2 { get; set; }
        public string DomiciliuPredator2 { get; set; }

        // ── Date primitor (Art. 3) ────────────────────────────
        public string CISeria { get; set; }
        public string CINr { get; set; }
        public string Domiciliu { get; set; }  // preumplut din ERP (PRSN.ADDRESS)

        // ── Date vehicul ──────────────────────────────────────
        public string MarcaAuto { get; set; }
        public string NrInmatriculare { get; set; }
        public string SerieSasiu { get; set; }
        public string AnFabricatie { get; set; }
        public string Kilometri { get; set; }
        public string StareFunctionare { get; set; }
        public string Avarii { get; set; }
        public string AnvelopeFata { get; set; }
        public string AnvelopeSpate { get; set; }
        public string UzuraAnvelope { get; set; }

        // ── Dotari (DA/NU) ────────────────────────────────────
        public string TrusaSanitara { get; set; }
        public string Extinctor { get; set; }
        public string TriunghiReflectorizant { get; set; }
        public string Cric { get; set; }
        public string CheieRoti { get; set; }
        public string VestaReflectorizanta { get; set; }
        public string RoataRezervа { get; set; }
        public string UzuraRoataRezervа { get; set; }  // % uzura roata rezerva

        // ── Documente vehicul ────────────────────────────────
        public string CertificatInmatriculare { get; set; }
        public string AsigurareRCA { get; set; }
        public string Rovinieta { get; set; }

        public PvAutovehiculModel()
        {
            NumePredator2 = string.Empty;
            FunctiePredator2 = string.Empty;
            CNPPredator2 = string.Empty;
            CISeriaPredator2 = string.Empty;
            CINrPredator2 = string.Empty;
            DomiciliuPredator2 = string.Empty;
            CISeria = string.Empty;
            CINr = string.Empty;
            Domiciliu = string.Empty;
            MarcaAuto = string.Empty;
            NrInmatriculare = string.Empty;
            SerieSasiu = string.Empty;
            AnFabricatie = string.Empty;
            Kilometri = string.Empty;
            StareFunctionare = "buna";
            Avarii = "-";
            AnvelopeFata = string.Empty;
            AnvelopeSpate = string.Empty;
            UzuraAnvelope = "0";
            TrusaSanitara = "DA";
            Extinctor = "DA";
            TriunghiReflectorizant = "DA";
            Cric = "DA";
            CheieRoti = "DA";
            VestaReflectorizanta = "DA";
            RoataRezervа = "DA";
            UzuraRoataRezervа = "0";
            CertificatInmatriculare = "DA";
            AsigurareRCA = "DA";
            Rovinieta = "DA";
        }
    }
}