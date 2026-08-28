using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ActAditionalPlugin.Models;
using Softone;

namespace ActAditionalPlugin.Services
{
    // ══════════════════════════════════════════════════════════
    //  HOOK CONTEXT
    //  Informatii disponibile unui handler la momentul rularii.
    // ══════════════════════════════════════════════════════════
    public class HookContext
    {
        // Definitia documentului curent
        public DocumentDefinition Definition { get; set; }

        // Valorile din formular (modificabile de hook)
        public Dictionary<string, object> FormValues { get; set; }

        // Valorile comune angajat + companie (modificabile de hook)
        public CommonDocumentValues Common { get; set; }

        // Parametrii din JSON (ex: { "query": "SELECT...", "target_field": "CodCor" })
        public Dictionary<string, string> Params { get; set; }

        // Acces la ERP (pentru SQL hooks)
        public XSupport XSupport { get; set; }

        // Body-ul documentului Word (disponibil doar pentru on_generate)
        public Body DocumentBody { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    //  HOOK REGISTRY
    //  Mapare nume → handler.
    //  Adaugarea unui nou hook = adaugarea unei metode statice
    //  si inregistrarea ei in RegisterAll().
    // ══════════════════════════════════════════════════════════
    public static class HookRegistry
    {
        private static readonly Dictionary<string, Action<HookContext>> _handlers
            = new Dictionary<string, Action<HookContext>>(StringComparer.OrdinalIgnoreCase);

        // ══════════════════════════════════════════════════════
        //  Initializare — apelat o singura data la startup
        // ══════════════════════════════════════════════════════
        public static void RegisterAll()
        {
            _handlers.Clear();

            // ── Handlere built-in ─────────────────────────────
            _handlers["InjectArticoleFinal"] = InjectArticoleFinal;
            _handlers["SqlOnOpen"] = SqlOnOpen;
            _handlers["SqlOnGenerate"] = SqlOnGenerate;
            _handlers["ConcatList"] = ConcatList;
            _handlers["CalcDataSfarsit"] = CalcDataSfarsit;
            _handlers["CalcDataSfarsitDinCNP"] = CalcDataSfarsitDinCNP;
            _handlers["CalcPerioadaSuspendare"] = CalcPerioadaSuspendare;
            _handlers["SetDefault"] = SetDefault;
            _handlers["SetIfEquals"] = SetIfEquals;
            _handlers["BlankIfNotEquals"] = BlankIfNotEquals;
            _handlers["CalcZileLucratoare"] = CalcZileLucratoare;
            _handlers["FormatPerioada"] = FormatPerioada;
        }

        // ══════════════════════════════════════════════════════
        //  Run hooks pentru un eveniment specific
        // ══════════════════════════════════════════════════════
        public static void RunHooks(string onEvent, HookContext ctx)
        {
            if (ctx.Definition.Hooks == null) return;

            foreach (var hook in ctx.Definition.Hooks
                .Where(h => string.Equals(h.On, onEvent, StringComparison.OrdinalIgnoreCase)))
            {
                Action<HookContext> handler;
                if (_handlers.TryGetValue(hook.Handler, out handler))
                {
                    ctx.Params = hook.Params ?? new Dictionary<string, string>();
                    try { handler(ctx); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            string.Format("[HookRegistry] Eroare handler '{0}': {1}",
                                hook.Handler, ex.Message));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        string.Format("[HookRegistry] Handler necunoscut: '{0}'", hook.Handler));
                }
            }
        }

        /// <summary>
        /// Ruleaza un singur handler dupa nume, cu contextul dat (inclusiv DocumentBody).
        /// Folosit din DynamicTemplateEngine pentru hookuri care necesita acces la body-ul Word.
        /// </summary>
        public static void RunSingleHook(string handlerName, HookContext ctx)
        {
            Action<HookContext> handler;
            if (_handlers.TryGetValue(handlerName, out handler))
            {
                try { handler(ctx); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        string.Format("[HookRegistry] Eroare handler '{0}': {1}",
                            handlerName, ex.Message));
                }
            }
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: InjectArticoleFinal
        //  Calculeaza numarul urmator dupa ultimul Art.N din doc
        //  si populeaza ArticolCompartiment + ArticolContestatie.
        //
        //  JSON: { "on": "on_generate", "handler": "InjectArticoleFinal" }
        // ══════════════════════════════════════════════════════
        private static void InjectArticoleFinal(HookContext ctx)
        {
            if (ctx.DocumentBody == null) return;

            string fullText = WordHelper.GetText(ctx.DocumentBody);
            string textFara = fullText
                .Replace("{{ArticolCompartiment}}", "")
                .Replace("{{ArticolContestatie}}", "");

            int nrArt = 0;
            for (int i = 1; i <= 20; i++)
            {
                if (textFara.Contains("Art." + i) || textFara.Contains("Art. " + i))
                    nrArt = i;
            }

            ctx.Common.ArticolCompartiment = string.Format(
                "Art.{0} Compartimentul juridic, personal şi financiar contabil " +
                "vor duce la îndeplinire prezenta.",
                nrArt + 1);

            ctx.Common.ArticolContestatie = string.Format(
                "Art.{0} Prezenta decizie poate fi contestata in termen de 30 de zile " +
                "de la comunicare la Tribunalul Botosani.",
                nrArt + 2);
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: SqlOnOpen
        //  Executa un SQL la deschiderea formularului si pune
        //  rezultatul intr-un camp specificat.
        //
        //  JSON: {
        //    "on": "on_open",
        //    "handler": "SqlOnOpen",
        //    "params": {
        //      "query": "SELECT TOP 1 S.CODE FROM ... WHERE PJ.PRSN={PrsnId}",
        //      "column": "CodCor",
        //      "target_field": "CodCor"
        //    }
        //  }
        //
        //  Placeholder-e disponibile in query: {PrsnId}, {CompanyId}
        // ══════════════════════════════════════════════════════
        private static void SqlOnOpen(HookContext ctx)
        {
            if (ctx.XSupport == null) return;

            string query, column, targetField;
            if (!ctx.Params.TryGetValue("query", out query)) return;
            if (!ctx.Params.TryGetValue("column", out column)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;

            string sql = ReplaceSqlParams(query, ctx);

            try
            {
                var ds = ctx.XSupport.GetSQLDataSet(sql);
                if (ds != null && ds.Count > 0)
                {
                    string val = ds[0, column]?.ToString()?.Trim() ?? string.Empty;
                    ctx.FormValues[targetField] = val;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[HookRegistry] SqlOnOpen error: " + ex.Message);
            }
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: SqlOnGenerate
        //  Executa un SQL la generarea documentului si pune
        //  rezultatul intr-un camp/placeholder specificat.
        //
        //  JSON: {
        //    "on": "on_generate",
        //    "handler": "SqlOnGenerate",
        //    "params": {
        //      "query": "SELECT TOP 1 NR FROM ... WHERE PRSN={PrsnId}",
        //      "column": "NR",
        //      "target_field": "NumarInregistrareITM"
        //    }
        //  }
        // ══════════════════════════════════════════════════════
        private static void SqlOnGenerate(HookContext ctx)
        {
            // Aceeasi logica ca SqlOnOpen — ruleaza la generate
            SqlOnOpen(ctx);
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: ConcatList
        //  Concateneaza o lista dinamica intr-un singur placeholder.
        //  Util pentru {{ReferateSursa}} (lista de referate concatenate).
        //
        //  JSON: {
        //    "on": "on_generate",
        //    "handler": "ConcatList",
        //    "params": {
        //      "source_field": "Referate",
        //      "item_key": "CodSiData",
        //      "target_field": "ReferateSursa",
        //      "separator": ", "
        //    }
        //  }
        // ══════════════════════════════════════════════════════
        private static void ConcatList(HookContext ctx)
        {
            string sourceField, itemKey, targetField, separator, distinctStr;
            if (!ctx.Params.TryGetValue("source_field", out sourceField)) return;
            if (!ctx.Params.TryGetValue("item_key", out itemKey)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;
            ctx.Params.TryGetValue("separator", out separator);
            if (separator == null) separator = ", ";
            ctx.Params.TryGetValue("distinct", out distinctStr);
            bool distinct = distinctStr != null &&
                (distinctStr.Equals("true", StringComparison.OrdinalIgnoreCase) || distinctStr == "1");

            if (!ctx.FormValues.ContainsKey(sourceField)) return;

            var rows = ctx.FormValues[sourceField] as List<Dictionary<string, string>>;
            if (rows == null) return;

            var values = rows
                .Where(r => r.ContainsKey(itemKey) && !string.IsNullOrWhiteSpace(r[itemKey]))
                .Select(r => r[itemKey].Trim());

            if (distinct)
                values = values.Distinct();

            ctx.FormValues[targetField] = string.Join(separator, values);
        }

        // ══════════════════════════════════════════════════════
        //  HELPER — inlocuieste parametrii in query SQL
        //  Suporta: {PrsnId}, {CompanyId}
        // ══════════════════════════════════════════════════════
        private static string ReplaceSqlParams(string query, HookContext ctx)
        {
            int companyId = 0;
            try { companyId = ctx.XSupport.ConnectionInfo.CompanyId; } catch { }

            return query
                .Replace("{PrsnId}", ctx.Common.PrsnId.ToString())
                .Replace("{CompanyId}", companyId.ToString());
        }

        // ══════════════════════════════════════════════════════
        //  Inregistrare handler custom din exterior (extensibil)
        // ══════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════
        //  HANDLER: CalcDataSfarsit
        //  Calculeaza DataSfarsitSuspendare = DataStart + LuniSuspendare luni
        //  JSON: { "on": "on_open", "handler": "CalcDataSfarsit",
        //          "params": { "start_field": "DataStartSuspendare",
        //                      "luni_field": "LuniSuspendare",
        //                      "target_field": "DataEndSuspendare" } }
        // ══════════════════════════════════════════════════════════
        private static void CalcDataSfarsit(HookContext ctx)
        {
            // Ruleaza on_change — hookul e apelat din form la schimbarea campurilor
            string startField, luniField, targetField;
            if (!ctx.Params.TryGetValue("start_field", out startField)) return;
            if (!ctx.Params.TryGetValue("luni_field", out luniField)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;

            object startVal, luniVal;
            if (!ctx.FormValues.TryGetValue(startField, out startVal)) return;
            if (!ctx.FormValues.TryGetValue(luniField, out luniVal)) return;

            DateTime dataStart;
            int luni;
            if (!DateTime.TryParse(startVal?.ToString(), out dataStart)) return;
            if (!int.TryParse(luniVal?.ToString(), out luni) || luni <= 0) return;

            ctx.FormValues[targetField] = dataStart.AddMonths(luni).ToString("dd.MM.yyyy");
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: CalcDataSfarsitDinCNP
        //  Calculeaza DataEndSuspendare = DataNastere(din CNP) + AniSuspendare ani.
        //  Se declanseaza on_change la modificarea CNPCopil sau AniSuspendare.
        //
        //  JSON: { "on": "on_change", "handler": "CalcDataSfarsitDinCNP",
        //          "params": { "cnp_field": "CNPCopil",
        //                      "ani_field": "AniSuspendare",
        //                      "target_field": "DataEndSuspendare" } }
        //
        //  Format CNP: SAALLZZXXXXC
        //    S  = sex/secol (1=M 1900, 2=F 1900, 5=M 2000, 6=F 2000 etc.)
        //    AA = an (2 cifre), LL = luna, ZZ = zi
        // ══════════════════════════════════════════════════════
        private static void CalcDataSfarsitDinCNP(HookContext ctx)
        {
            string cnpField, aniField, targetField;
            if (!ctx.Params.TryGetValue("cnp_field", out cnpField)) return;
            if (!ctx.Params.TryGetValue("ani_field", out aniField)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;

            object cnpObj, aniObj;
            if (!ctx.FormValues.TryGetValue(cnpField, out cnpObj)) return;
            if (!ctx.FormValues.TryGetValue(aniField, out aniObj)) return;

            string cnp = cnpObj?.ToString()?.Trim() ?? string.Empty;
            if (cnp.Length != 13) return;

            int ani;
            if (!int.TryParse(aniObj?.ToString(), out ani) || ani <= 0) return;

            // Extrage data nasterii din CNP
            DateTime dataNastere;
            try
            {
                int s = int.Parse(cnp.Substring(0, 1));
                int aa = int.Parse(cnp.Substring(1, 2));
                int ll = int.Parse(cnp.Substring(3, 2));
                int zz = int.Parse(cnp.Substring(5, 2));

                int an;
                if (s == 1 || s == 2) an = 1900 + aa;
                else if (s == 3 || s == 4) an = 1800 + aa;
                else if (s == 5 || s == 6) an = 2000 + aa;
                else if (s == 7 || s == 8) an = 2000 + aa; // rezidenti
                else return;

                dataNastere = new DateTime(an, ll, zz);
            }
            catch { return; }

            // DataEnd = DataNastere + AniSuspendare ani
            DateTime dataEnd = dataNastere.AddYears(ani);
            ctx.FormValues[targetField] = dataEnd.ToString("dd.MM.yyyy");
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: CalcPerioadaSuspendare
        //  Converteste AniSuspendare (numar) in text pentru {{PerioadaSuspendare}}
        //  ex. 2 → "2 ani", 1 → "1 an"
        //
        //  JSON: { "on": "on_generate", "handler": "CalcPerioadaSuspendare",
        //          "params": { "ani_field": "AniSuspendare",
        //                      "target_field": "PerioadaSuspendare" } }
        // ══════════════════════════════════════════════════════
        private static void CalcPerioadaSuspendare(HookContext ctx)
        {
            string aniField, targetField;
            if (!ctx.Params.TryGetValue("ani_field", out aniField)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;

            object aniObj;
            if (!ctx.FormValues.TryGetValue(aniField, out aniObj)) return;

            int ani;
            if (!int.TryParse(aniObj?.ToString(), out ani) || ani <= 0) return;

            ctx.FormValues[targetField] = ani == 1 ? "1 an" : ani + " ani";
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: SetDefault
        //  Seteaza valoarea unui camp la deschidere daca e gol / zero.
        //  Util pentru NumericUpDown cu valoare default specifica per document.
        //
        //  JSON: { "on": "on_open", "handler": "SetDefault",
        //          "params": { "field": "AniSuspendare", "value": "2" } }
        // ══════════════════════════════════════════════════════
        private static void SetDefault(HookContext ctx)
        {
            string field, value;
            if (!ctx.Params.TryGetValue("field", out field)) return;
            if (!ctx.Params.TryGetValue("value", out value)) return;

            // Valori speciale rezolvate la runtime (nu pot fi hardcodate in JSON)
            if (value == "{year}") value = DateTime.Today.Year.ToString();
            else if (value == "{today}") value = DateTime.Today.ToString("dd.MM.yyyy");

            object existing;
            bool isEmpty = !ctx.FormValues.TryGetValue(field, out existing)
                        || existing == null
                        || existing.ToString() == "0"
                        || string.IsNullOrWhiteSpace(existing.ToString());

            if (isEmpty)
                ctx.FormValues[field] = value;
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: SetIfEquals
        //  Seteaza target_field la value_if_true / value_if_false,
        //  in functie de valoarea curenta a source_field.
        //  Util pentru bife (■/gol) si texte derivate dintr-o
        //  selectie (ex. combo cu variante).
        //
        //  JSON: { "on": "on_generate", "handler": "SetIfEquals",
        //          "params": { "source_field": "TipCerere",
        //                      "equals": "Învoire",
        //                      "target_field": "BifaInvoire",
        //                      "value_if_true": "■",
        //                      "value_if_false": "" } }
        // ══════════════════════════════════════════════════════
        private static void SetIfEquals(HookContext ctx)
        {
            string sourceField, equals, targetField;
            if (!ctx.Params.TryGetValue("source_field", out sourceField)) return;
            if (!ctx.Params.TryGetValue("equals", out equals)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;

            string valTrue, valFalse;
            ctx.Params.TryGetValue("value_if_true", out valTrue);
            ctx.Params.TryGetValue("value_if_false", out valFalse);

            object srcObj;
            string src = ctx.FormValues.TryGetValue(sourceField, out srcObj)
                ? (srcObj != null ? srcObj.ToString() : string.Empty)
                : string.Empty;

            ctx.FormValues[targetField] = string.Equals(src, equals, StringComparison.Ordinal)
                ? (valTrue ?? string.Empty)
                : (valFalse ?? string.Empty);
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: BlankIfNotEquals
        //  Daca source_field NU e egal cu "equals", suprascrie
        //  target_field cu "blank" (ex. linie punctata albastra).
        //  Daca E egal, lasa target_field neatins — valoarea reala
        //  introdusa de utilizator ramane vizibila.
        //  Poate fi inlantuit: mai multe hook-uri pe acelasi
        //  target_field (verificand campuri diferite) — o data
        //  blancat, ramane blancat (hook-urile ulterioare care
        //  gasesc egalitate nu il ating).
        //
        //  JSON: { "on": "on_generate", "handler": "BlankIfNotEquals",
        //          "params": { "source_field": "TipCerere",
        //                      "equals": "Învoire",
        //                      "target_field": "MotivInvoire",
        //                      "blank": "_ _ _ _ _ _ _ _ _ _ _ _ _ _ _" } }
        // ══════════════════════════════════════════════════════
        private static void BlankIfNotEquals(HookContext ctx)
        {
            string sourceField, equals, targetField, blank;
            if (!ctx.Params.TryGetValue("source_field", out sourceField)) return;
            if (!ctx.Params.TryGetValue("equals", out equals)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;
            if (!ctx.Params.TryGetValue("blank", out blank)) return;

            object srcObj;
            string src = ctx.FormValues.TryGetValue(sourceField, out srcObj)
                ? (srcObj != null ? srcObj.ToString() : string.Empty)
                : string.Empty;

            if (!string.Equals(src, equals, StringComparison.Ordinal))
                ctx.FormValues[targetField] = blank;
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: CalcZileLucratoare
        //  Numara zilele lucratoare (luni-vineri) intre doua date,
        //  capete incluse. Scrie in target_field; ramane editabil
        //  manual dupa — se recalculeaza doar cand se schimba
        //  din nou una din date (acelasi tipar ca CalcDataSfarsitDinCNP).
        //
        //  JSON: { "on": "on_change", "handler": "CalcZileLucratoare",
        //          "params": { "start_field": "DataInceputInterval",
        //                      "end_field": "DataSfarsitInterval",
        //                      "target_field": "NrZile" } }
        // ══════════════════════════════════════════════════════
        private static void CalcZileLucratoare(HookContext ctx)
        {
            string startField, endField, targetField;
            if (!ctx.Params.TryGetValue("start_field", out startField)) return;
            if (!ctx.Params.TryGetValue("end_field", out endField)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;

            object startObj, endObj;
            if (!ctx.FormValues.TryGetValue(startField, out startObj)) return;
            if (!ctx.FormValues.TryGetValue(endField, out endObj)) return;

            DateTime start, end;
            if (!DateTime.TryParse(startObj != null ? startObj.ToString() : null, out start)) return;
            if (!DateTime.TryParse(endObj != null ? endObj.ToString() : null, out end)) return;
            if (end < start) return;

            int zile = 0;
            for (DateTime d = start.Date; d <= end.Date; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    zile++;

            ctx.FormValues[targetField] = zile.ToString();
        }

        // ══════════════════════════════════════════════════════
        //  HANDLER: FormatPerioada
        //  Combina doua campuri "datetime" (start/sfarsit) intr-un
        //  interval text, cu sau fara ora — in functie de un camp
        //  "tip" (ex. combo "Zile"/"Ore"). Daca tip_field lipseste
        //  sau nu se potriveste cu tip_ore_value, formateaza doar data.
        //
        //  JSON: { "on": "on_generate", "handler": "FormatPerioada",
        //          "params": { "start_field": "DataInceputRecuperare",
        //                      "end_field": "DataSfarsitRecuperare",
        //                      "tip_field": "TipIntervalRecuperare",
        //                      "tip_ore_value": "Ore",
        //                      "target_field": "PerioadaRecuperare" } }
        // ══════════════════════════════════════════════════════
        private static void FormatPerioada(HookContext ctx)
        {
            string startField, endField, targetField, tipField, tipOreValue;
            if (!ctx.Params.TryGetValue("start_field", out startField)) return;
            if (!ctx.Params.TryGetValue("end_field", out endField)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;
            ctx.Params.TryGetValue("tip_field", out tipField);
            ctx.Params.TryGetValue("tip_ore_value", out tipOreValue);

            object startObj, endObj;
            if (!ctx.FormValues.TryGetValue(startField, out startObj)) return;
            if (!ctx.FormValues.TryGetValue(endField, out endObj)) return;

            DateTime start, end;
            if (!DateTime.TryParse(startObj != null ? startObj.ToString() : null, out start)) return;
            if (!DateTime.TryParse(endObj != null ? endObj.ToString() : null, out end)) return;

            bool cuOra = false;
            if (!string.IsNullOrEmpty(tipField))
            {
                object tipObj;
                string tipVal = ctx.FormValues.TryGetValue(tipField, out tipObj)
                    ? (tipObj != null ? tipObj.ToString() : string.Empty)
                    : string.Empty;
                cuOra = string.Equals(tipVal, tipOreValue, StringComparison.Ordinal);
            }

            string fmt = cuOra ? "dd.MM.yyyy HH:mm" : "dd.MM.yyyy";
            ctx.FormValues[targetField] = start.ToString(fmt) + " - " + end.ToString(fmt);
        }

        public static void Register(string name, Action<HookContext> handler)
        {
            _handlers[name] = handler;
        }

        public static bool HasHandler(string name)
        {
            return _handlers.ContainsKey(name);
        }
    }
}