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
        /// Folderul cu template-urile .docx — din variabila de sistem TemplateDocsPath.
        /// Fallback la Resources/ langa DLL daca variabila nu e setata.
        /// </summary>
        private static string TemplatesDir
        {
            get
            {
                string envPath = System.Environment.GetEnvironmentVariable("TemplateDocsPath");
                return !string.IsNullOrWhiteSpace(envPath) ? envPath : ResourcesDir;
            }
        }

        public static string GetTemplatePath(TipPV tip)
        {
            string fileName;
            switch (tip)
            {
                case TipPV.Echipamente: fileName = "template_pv_echipamente.docx"; break;
                case TipPV.Electronice: fileName = "template_pv_echipamente.docx"; break;
                case TipPV.Autovehicul: fileName = "template_pv_autovehicul.docx"; break;
                default: fileName = string.Empty; break;
            }
            return Path.Combine(TemplatesDir, fileName);
        }

        public static string GetTemplatePath(TipDocument tip)
        {
            string fileName;
            switch (tip)
            {
                case TipDocument.ActAditional:
                    fileName = "ActAditional_template.docx"; break;
                case TipDocument.SuspendareCresterecopil:
                    fileName = "template_suspendare_crestere_copil.docx"; break;
                case TipDocument.SuspendareCresterecopilHandicap:
                    fileName = "template_suspendare_crestere_copil_handicap.docx"; break;
                case TipDocument.SuspendareAbsenteNemotivate:
                    fileName = "template_suspendare_absente_nemotivate.docx"; break;
                case TipDocument.SuspendareAcordParti:
                    fileName = "template_suspendare_acord_parti.docx"; break;
                case TipDocument.SuspendareSiIncetareSuspendare:
                    fileName = "template_suspendare_si_incetare_suspendare.docx"; break;
                case TipDocument.IncetareSuspendare:
                    fileName = "template_incetare_suspendare.docx"; break;
                case TipDocument.IncetareDemisie:
                    fileName = "template_incetare_demisie.docx"; break;
                case TipDocument.IncetareExpirare:
                    fileName = "template_incetare_expirare_termen.docx"; break;
                case TipDocument.IncetareDisciplinar:
                    fileName = "template_incetare_disciplinar.docx"; break;
                case TipDocument.IncetarePerioadaProba:
                    fileName = "template_incetare_perioada_proba.docx"; break;
                case TipDocument.ReferatDisciplinar:
                    fileName = "template_referat_disciplinar.docx"; break;
                case TipDocument.AvertismentDisciplinar:
                    fileName = "template_avertisment.docx"; break;
                case TipDocument.DecizieConstituireComisie:
                    fileName = "template_decizie_constituire_comisie.docx"; break;
                case TipDocument.ConvocareCercetare:
                    fileName = "template_convocare_cercetare.docx"; break;
                case TipDocument.ProcesVerbalCercetare:
                    fileName = "template_pv_cercetare_disciplinara.docx"; break;
                default:
                    fileName = string.Empty; break;
            }
            return Path.Combine(TemplatesDir, fileName);
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