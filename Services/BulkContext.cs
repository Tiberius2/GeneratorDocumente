using Softone;

namespace ActAditionalPlugin.Services
{
    // Contextul Softone al sesiunii curente (setat la pornire, folosit de hooks / formulare)
    public static class BulkContext
    {
        public static ErpCompanyData CompanyData { get; set; }
        public static XSupport XSupport { get; set; }

        public static void Reset()
        {
            CompanyData = null;
            XSupport = null;
        }
    }
}
