using System;
using System.Collections.Generic;
using ActAditionalPlugin.UI;

namespace ActAditionalPlugin.Services
{
    /// <summary>
    /// Context static pentru generarea in masa a actelor aditionale.
    /// Setat pe thread-ul TXCode inainte de startul thread-ului STA.
    /// </summary>
    public static class BulkContext
    {
        public static List<AngajatPickerDialog.AngajatItem> Angajati { get; set; }
        public static Func<int, ErpCimData> GetCimData { get; set; }
        public static Func<int, string> GetAdresaPrimitor { get; set; }
        public static ErpCompanyData CompanyData { get; set; }

        public static bool IsAvailable =>
            Angajati != null && GetCimData != null && GetAdresaPrimitor != null;

        public static void Reset()
        {
            Angajati = null;
            GetCimData = null;
            GetAdresaPrimitor = null;
            CompanyData = null;
        }
    }
}