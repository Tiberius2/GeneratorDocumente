using System;
using System.Collections.Generic;
using ActAditionalPlugin.Models;
using Newtonsoft.Json.Linq;

namespace ActAditionalPlugin.UI.Web
{
    // Angajatii pentru selectorul de persoane din pagini (picker.js) si
    // valorile pentru "maps" ale campurilor person_picker.
    internal static class PersonList
    {
        public static JArray ToJson(List<PersonInfo> persoane)
        {
            var arr = new JArray();
            if (persoane == null) return arr;
            foreach (var p in persoane)
                arr.Add(new JObject
                {
                    ["id"] = p.PrsnId,
                    ["nume"] = p.NumeComplet ?? "",
                    ["functie"] = p.Functie ?? "",
                    ["cor"] = p.CodCor ?? "",
                    ["cnp"] = p.CNP ?? "",
                    ["nrCim"] = p.NrCim ?? "",
                    ["dataCim"] = p.DataCimFormatata ?? "",
                    ["dep"] = p.NumeDepartament ?? ""
                });
            return arr;
        }

        // Valoarea unei proprietati a persoanei pentru "maps" (sufix ":upper" = majuscule)
        public static string Value(PersonInfo p, string prop)
        {
            if (p == null || string.IsNullOrEmpty(prop)) return string.Empty;
            bool upper = prop.EndsWith(":upper", StringComparison.OrdinalIgnoreCase);
            if (upper) prop = prop.Substring(0, prop.Length - ":upper".Length);

            string v;
            switch (prop)
            {
                case "NumeComplet": v = p.NumeComplet; break;
                case "Nume": v = p.Nume; break;
                case "Prenume": v = p.Prenume; break;
                case "CNP": v = p.CNP; break;
                case "Functie": v = p.Functie; break;
                case "CodCor": v = p.CodCor; break;
                case "NrCim": v = p.NrCim; break;
                case "NumeDepartament": v = p.NumeDepartament; break;
                case "DataCim": v = p.DataCimFormatata; break;
                case "SerieCI": v = p.SerieCI; break;
                case "NrCI": v = p.NrCI; break;
                case "Domiciliu": v = p.Domiciliu; break;
                case "NumeComplet_Functie":
                    v = string.IsNullOrEmpty(p.Functie) ? p.NumeComplet : p.NumeComplet + " — " + p.Functie;
                    break;
                default: v = string.Empty; break;
            }
            v = v ?? string.Empty;
            return upper ? v.ToUpper(new System.Globalization.CultureInfo("ro-RO")) : v;
        }
    }
}
