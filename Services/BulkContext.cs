using ActAditionalPlugin.Models;
using Softone;
using System;
using System.Collections.Generic;

namespace ActAditionalPlugin.Services
{
    public static class BulkContext
    {
        // Pastrat pentru compatibilitate cu HookRegistry si DynamicForm
        public static List<PersonInfo> Persoane { get; set; }
        public static Func<int, ErpCimData> GetCimData { get; set; }
        public static ErpCompanyData CompanyData { get; set; }
        public static XSupport XSupport { get; set; }

        public static void Reset()
        {
            Persoane = null;
            GetCimData = null;
            CompanyData = null;
            XSupport = null;
        }
    }
}