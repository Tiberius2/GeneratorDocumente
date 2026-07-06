using System;

namespace ActAditionalPlugin.Models
{
    /// <summary>
    /// Date complete despre o persoana selectata din PersonPickerDialog.
    /// Folosit ca DTO universal oriunde apare un camp de tip persoana.
    /// </summary>
    public class PersonInfo
    {
        public int PrsnId { get; set; }
        public string NumeComplet { get; set; }  // NAME + ' ' + NAME2
        public string Nume { get; set; }  // NAME
        public string Prenume { get; set; }  // NAME2
        public string CNP { get; set; }  // AFM
        public string Functie { get; set; }  // SOTITLENAME
        public string CodCor { get; set; }  // SPECIALTY.CODE
        public string NrCim { get; set; }  // PRSEXTRA.CCCVARCHAR05
        public DateTime DataCim { get; set; }  // PRSEXTRA.DATE03
        public string NumeDepartament { get; set; }  // DEPART.NAME
        public string SerieCI { get; set; }  // primele 2 caractere din PRSNIN.IDENTITYNUM
        public string NrCI { get; set; }    // cifrele dupa seria CI din PRSNIN.IDENTITYNUM
        public string Domiciliu { get; set; } // PRSNIN.ADDRESS

        public PersonInfo()
        {
            NumeComplet = string.Empty;
            Nume = string.Empty;
            Prenume = string.Empty;
            CNP = string.Empty;
            Functie = string.Empty;
            CodCor = string.Empty;
            NrCim = string.Empty;
            DataCim = DateTime.MinValue;
            NumeDepartament = string.Empty;
            SerieCI = string.Empty;
            NrCI = string.Empty;
            Domiciliu = string.Empty;
        }

        public string DataCimFormatata =>
            DataCim != DateTime.MinValue ? DataCim.ToString("dd.MM.yyyy") : string.Empty;

        /// <summary>
        /// Parseaza IDENTITYNUM (ex: "XZ123456") in SerieCI ("XZ") si NrCI ("123456").
        /// Seria = primele caractere non-cifra de la inceput (max 2).
        /// Numarul = toate cifrele ramase.
        /// </summary>
        public static void ParseIdentityNum(string identityNum, out string serie, out string nr)
        {
            serie = string.Empty;
            nr = string.Empty;
            if (string.IsNullOrWhiteSpace(identityNum)) return;

            string s = identityNum.Trim().ToUpper();
            int i = 0;
            // Consuma literele de la inceput (seria)
            while (i < s.Length && !char.IsDigit(s[i]) && i < 2)
                i++;
            serie = s.Substring(0, i);
            // Restul cifrelor = numarul
            string rest = s.Substring(i);
            foreach (char c in rest)
                if (char.IsDigit(c)) nr += c;
        }

        public override string ToString() => NumeComplet;
    }
}