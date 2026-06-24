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
            string sourceField, itemKey, targetField, separator;
            if (!ctx.Params.TryGetValue("source_field", out sourceField)) return;
            if (!ctx.Params.TryGetValue("item_key", out itemKey)) return;
            if (!ctx.Params.TryGetValue("target_field", out targetField)) return;
            ctx.Params.TryGetValue("separator", out separator);
            if (separator == null) separator = ", ";

            if (!ctx.FormValues.ContainsKey(sourceField)) return;

            var rows = ctx.FormValues[sourceField] as List<Dictionary<string, string>>;
            if (rows == null) return;

            string result = string.Join(separator,
                rows.Where(r => r.ContainsKey(itemKey) && !string.IsNullOrWhiteSpace(r[itemKey]))
                    .Select(r => r[itemKey]));

            ctx.FormValues[targetField] = result;
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