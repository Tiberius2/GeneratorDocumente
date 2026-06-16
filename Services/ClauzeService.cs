using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ActAditionalPlugin.Models;
using Newtonsoft.Json;

namespace ActAditionalPlugin.Services
{
    public static class ClauzeService
    {
        private const string FileName = "clauze_act_aditional.json";

        public static string GetFilePath()
        {
            string dir = Environment.GetEnvironmentVariable("TemplateDocsPath");
            if (string.IsNullOrWhiteSpace(dir))
                dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(dir, FileName);
        }

        public static ClauzeConfig Load()
        {
            string path = GetFilePath();
            if (!File.Exists(path)) { var def = CreateDefault(); Save(def); return def; }
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var cfg = JsonConvert.DeserializeObject<ClauzeConfig>(json);
                if (cfg == null || cfg.TipuriContract == null || cfg.TipuriContract.Count == 0)
                    return CreateDefault();
                return cfg;
            }
            catch { return CreateDefault(); }
        }

        public static void Save(ClauzeConfig config)
        {
            string path = GetFilePath();
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented), Encoding.UTF8);
        }

        private static ClauzeConfig CreateDefault()
        {
            var tip = new TipContract { Id = "tip001", Nume = "CIM Standard", Activ = true };
            tip.Clauze.AddRange(new[]
            {
                Cl("sal001","Salariu de baza (Lit. I, pct. 1)",
                   "La litera I, punctul 1, \"Salariul de baza\" se modifica si va avea urmatorul cuprins:",
                   "Salariul de baza brut lunar: {0} RON.",
                   new[]{F("Salariu nou (RON brut)","ex. 4500","numar zecimal",0)}),

                Cl("fct001","Functie / COR (Lit. F)",
                   "La litera F, \"Functia/meseria\" se modifica si va avea urmatorul cuprins:",
                   "Functia/meseria: {0} conform Clasificarii Ocupatiilor din Romania, cod COR {1}.",
                   new[]{F("Functie noua","ex. BRUTAR","text",0),F("Cod COR","ex. 751201","text",1)}),

                Cl("prg001","Program de lucru (Lit. G, pct. 1)",
                   "La litera G, punctul 1, \"Durata muncii\" se modifica si va avea urmatorul cuprins:",
                   "{0}", new[]{F("Descriere program","ex. 8 ore/zi, 40 ore/saptamana, de luni pana vineri","text",0)}),

                Cl("loc001","Loc de munca (Lit. E, pct. 1)",
                   "La litera E, punctul 1, \"Locul de munca\" se modifica si va avea urmatorul cuprins:",
                   "{0}", new[]{F("Locul de munca","ex. Activitatea se desfasoara la...","text",0)}),

                Cl("dur001","Durata contract (Lit. C)",
                   "La litera C, \"Durata contractului\" se modifica si va avea urmatorul cuprins:",
                   "Contractul individual de munca este incheiat pe durata {0}.",
                   new[]{F("Durata","ex. nedeterminata / determinata pana la 31.12.2025","text",0)}),

                Cl("con001","Concediu de baza (Lit. H)",
                   "La litera H, \"Concediul\" se modifica si va avea urmatorul cuprins:",
                   "Concediul de odihna anual platit este de {0} zile lucratoare.",
                   new[]{F("Zile concediu","ex. 21","numar intreg",0)}),

                Cl("prc001","Preaviz concediere (Lit. J, lit. a)",
                   "La litera J, litera a), \"Preavizul la concediere\" se modifica si va avea urmatorul cuprins:",
                   "In cazul concedierii, salariatul beneficiaza de un preaviz de {0} zile lucratoare.",
                   new[]{F("Zile preaviz","ex. 20","numar intreg",0)}),

                Cl("prd001","Preaviz demisie (Lit. J, lit. b)",
                   "La litera J, litera b), \"Preavizul la demisie\" se modifica si va avea urmatorul cuprins:",
                   "In cazul demisiei, salariatul are obligatia de a respecta un preaviz de {0} zile lucratoare.",
                   new[]{F("Zile preaviz","ex. 20","numar intreg",0)}),
            });

            return new ClauzeConfig { TipuriContract = new List<TipContract> { tip }, TipSelectatId = tip.Id };
        }

        private static ClauzeActAditional Cl(string id, string titlu, string tc, string td, CampClauza[] campuri)
        {
            var c = new ClauzeActAditional { Id = id, Titlu = titlu, TextClauza = tc, TextDinamic = td, Activ = true };
            c.Campuri.AddRange(campuri);
            return c;
        }

        private static CampClauza F(string label, string ph, string tip, int ord)
            => new CampClauza { Label = label, Placeholder = ph, TipCamp = tip, Ordine = ord };
    }
}