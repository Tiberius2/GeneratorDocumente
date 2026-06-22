using ActAditionalPlugin.UI;
using Softone;
using System;
using System.Collections.Generic;

namespace ActAditionalPlugin.Services
{
    public static class BulkContext
    {
        public static List<AngajatPickerDialog.AngajatItem> Angajati { get; set; }
        public static Func<int, ErpCimData> GetCimData { get; set; }
        public static Func<int, string> GetAdresaPrimitor { get; set; }
        public static ErpCompanyData CompanyData { get; set; }
        public static XSupport XSupport { get; set; }

        public static bool IsAvailable =>
            Angajati != null && GetCimData != null && GetAdresaPrimitor != null;

        public static void Reset()
        {
            Angajati = null;
            GetCimData = null;
            GetAdresaPrimitor = null;
            CompanyData = null;
            XSupport = null;
        }
    }
}