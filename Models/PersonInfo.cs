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
        }

        public string DataCimFormatata =>
            DataCim != DateTime.MinValue ? DataCim.ToString("dd.MM.yyyy") : string.Empty;

        public override string ToString() => NumeComplet;
    }
}