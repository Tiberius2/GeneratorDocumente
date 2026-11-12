using System.IO;
using System.Reflection;

namespace ActAditionalPlugin
{
    public static class PluginConfig
    {
        private static readonly string _baseDir =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Folderul radacina cu sabloanele — din variabila de sistem TemplateDocsPath.
        /// Structura: {TemplatesRoot}/{Categorie}/{document.json + document.docx}.
        /// Fallback: folderul "Templates" de langa DLL.
        /// </summary>
        public static string TemplatesRoot
        {
            get
            {
                string envPath = System.Environment.GetEnvironmentVariable("TemplateDocsPath");
                if (!string.IsNullOrWhiteSpace(envPath) && Directory.Exists(envPath))
                    return envPath;
                return Path.Combine(_baseDir, "Templates");
            }
        }

        // Singura data de companie care nu vine din ERP ({{FunctieReprezentant}})
        public static string FunctieReprezentant { get { return "MANAGER GENERAL"; } }
    }
}
