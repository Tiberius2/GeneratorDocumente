using System.IO;
using System.Reflection;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin
{
    public static class PluginConfig
    {
        private static readonly string _baseDir =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // ── Resurse ───────────────────────────────────────────
        private static string ResourcesDir
        {
            get { return Path.Combine(_baseDir, "Resources"); }
        }

        /// <summary>
        /// Folderul radacina cu template-urile — din variabila de sistem TemplateDocsPath.
        /// Structura asteptata: {TemplatesRoot}/{Categorie}/{document.json + document.docx}
        /// Fallback la Resources/Templates/ langa DLL daca variabila nu e setata.
        /// </summary>
        public static string TemplatesRoot
        {
            get
            {
                string envPath = System.Environment.GetEnvironmentVariable("TemplateDocsPath");
                if (!string.IsNullOrWhiteSpace(envPath) && System.IO.Directory.Exists(envPath))
                    return envPath;
                return Path.Combine(_baseDir, "Templates");
            }
        }

        // Pastrat pentru compatibilitate — nu mai e folosit in noul sistem
        private static string TemplatesDir
        {
            get { return TemplatesRoot; }
        }


        public static string LogoPath
        {
            get { return Path.Combine(ResourcesDir, "logo.png"); }
        }

        // ── Date angajator ────────────────────────────────────
        public static string NumeAngajator { get { return "VATRA DOMNEASCA SRL"; } }
        public static string CIFAngajator { get { return "29038003"; } }
        public static string ReprezentantLegal { get { return "TIMOFTE MIRCEA GABRIEL"; } }
        public static string FunctieReprezentant { get { return "MANAGER GENERAL"; } }
        public static string AdresaCompanie { get { return "Judetul Botosani, Comuna Mihai Eminescu, Sat Catamarati-Deal, Str. Freziilor Nr. 7"; } }
        public static string ZipCompanie { get { return "717248"; } }
        public static string NrRegComertului { get { return "J07/314/2011"; } }
        public static string IbanCompanie { get { return "RO22 BACX 0000 0011 9564 2000"; } }
        public static string NrTelefonCompanie { get { return "0745.999.888"; } }
        public static string EmailCompanie { get { return "office@vatradomneasca.ro"; } }
        public static string WebsiteCompanie { get { return "www.vatradomneasca.ro"; } }
    }
}