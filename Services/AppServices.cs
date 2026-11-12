using Softone;

namespace ActAditionalPlugin.Services
{
    // ══════════════════════════════════════════════════════════
    //  Initializarea comuna a pluginului: registratura, hooks,
    //  sabloanele (DocumentRegistry), datele companiei, BulkContext.
    //  Folosita de comenzile Softone (PluginEntry) si de
    //  SelectorDialog deschis direct ca "Dll Form".
    //  Arunca exceptie daca ceva esential nu poate fi incarcat.
    // ══════════════════════════════════════════════════════════
    internal static class AppServices
    {
        public static ErpCompanyData Initialize(XSupport xs)
        {
            RegistraturaService.Initialize(xs);
            HookRegistry.RegisterAll();
            DocumentRegistry.Initialize(PluginConfig.TemplatesRoot);

            var company = ErpDataProvider.GetCompanyData(xs);
            BulkContext.XSupport = xs;
            BulkContext.CompanyData = company;
            return company;
        }
    }
}
