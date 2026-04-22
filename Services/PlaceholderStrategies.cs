using System;
using System.Collections.Generic;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.Services
{
    internal static class PlaceholderStrategies
    {
        private static readonly Dictionary<TipDocument, Func<DocumentModelBase, Dictionary<string,string>>> _registry;

        static PlaceholderStrategies()
        {
            _registry = new Dictionary<TipDocument, Func<DocumentModelBase, Dictionary<string,string>>>
            {
                { TipDocument.ActAditional, m => BuildActAditional((ActAditionalModel)m) },
                { TipDocument.SuspendareCresterecopil, m => BuildSuspendareCresterecopil((SuspendareCresterecopilModel)m) },
                { TipDocument.SuspendareCresterecopilHandicap, m => BuildSuspendareCresterecopilHandicap((SuspendareCresterecopilHandicapModel)m) },
                { TipDocument.SuspendareAbsenteNemotivate, m => BuildSuspendareAbsente((SuspendareAbsenteNemotivateModel)m) },
                { TipDocument.SuspendareAcordParti, m => BuildSuspendareAcordParti((SuspendareAcordPartiModel)m) },
                { TipDocument.SuspendareSiIncetareSuspendare, m => BuildSuspendareSiIncetare((SuspendareSiIncetareSuspendareModel)m) },
                { TipDocument.IncetareSuspendare, m => BuildIncetareSuspendare((IncetareSuspendareModel)m) },
                { TipDocument.IncetareDemisie, m => BuildIncetareDemisie((IncetareDemisieModel)m) },
                { TipDocument.IncetareExpirare, m => BuildIncetareExpirare((IncetareExpirareModel)m) },
                { TipDocument.IncetareDisciplinar, m => BuildIncetareDisciplinar((IncetareDisciplinarModel)m) }
            };
        }

        public static Dictionary<string,string> BuildPlaceholders(DocumentModelBase model)
        {
            var map = DocumentTemplateEngine.BuildCommonPlaceholders(model);
            if (_registry.TryGetValue(model.TipDocument, out var func))
            {
                var extra = func(model);
                foreach (var kv in extra) map[kv.Key] = kv.Value;
            }
            return map;
        }

        // Per-type builders (copied from previous TemplateEngine Add* methods)
        private static Dictionary<string,string> BuildActAditional(ActAditionalModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{DataEmitereAct}}"] = m.DataEmitereAct.ToString("dd.MM.yyyy");
            map["{{DataVigoare}}"] = m.DataVigoare.ToString("dd.MM.yyyy");
            return map;
        }

        private static Dictionary<string,string> BuildSuspendareCresterecopil(SuspendareCresterecopilModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{DataDecizie}}"] = m.DataDecizie != DateTime.MinValue ? m.DataDecizie.ToString("dd.MM.yyyy") : string.Empty;
            map["{{NrCerere}}"] = m.NrCerere ?? string.Empty;
            map["{{DataCerere}}"] = m.DataCerere != DateTime.MinValue ? m.DataCerere.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{PerioadaSuspendare}}"] = m.PerioadaSuspendare ?? string.Empty;
            map["{{NumeCopil}}"] = m.NumeCopil ?? string.Empty;
            map["{{SerieCertificat}}"] = m.SerieCertificat ?? string.Empty;
            map["{{NrCertificat}}"] = m.NrCertificat ?? string.Empty;
            return map;
        }

        private static Dictionary<string,string> BuildSuspendareCresterecopilHandicap(SuspendareCresterecopilHandicapModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{NrCerere}}"] = m.NrCerere ?? string.Empty;
            map["{{DataCerere}}"] = m.DataCerere != DateTime.MinValue ? m.DataCerere.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare.ToString("dd.MM.yyyy");
            map["{{PerioadaSuspendare}}"] = m.PerioadaSuspendare ?? string.Empty;
            map["{{NumeCopil}}"] = m.NumeCopil ?? string.Empty;
            map["{{SerieCertificat}}"] = m.SerieCertificat ?? string.Empty;
            map["{{NrCertificat}}"] = m.NrCertificat ?? string.Empty;
            map["{{GradHandicap}}"] = m.GradHandicap ?? string.Empty;
            map["{{NrCertificatHandicap}}"] = m.NrCertificatHandicap ?? string.Empty;
            map["{{DataCertificatHandicap}}"] = m.DataCertificatHandicap != DateTime.MinValue ? m.DataCertificatHandicap.ToString("dd.MM.yyyy") : string.Empty;
            return map;
        }

        private static Dictionary<string,string> BuildSuspendareAbsente(SuspendareAbsenteNemotivateModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{DataDecizie}}"] = m.DataDecizie != DateTime.MinValue ? m.DataDecizie.ToString("dd.MM.yyyy") : string.Empty;
            map["{{NrReferat}}"] = m.NrReferat ?? string.Empty;
            map["{{DataReferat}}"] = m.DataReferat != DateTime.MinValue ? m.DataReferat.ToString("dd.MM.yyyy") : string.Empty;
            map["{{IntocmitDe}}"] = m.IntocmitDe ?? string.Empty;
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare.ToString("dd.MM.yyyy");
            map["{{DataIncetareSuspendare}}"] = m.DataIncetareSuspendare.ToString("dd.MM.yyyy");
            return map;
        }

        private static Dictionary<string,string> BuildSuspendareAcordParti(SuspendareAcordPartiModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{NrCerere}}"] = m.NrCerere ?? string.Empty;
            map["{{DataCerere}}"] = m.DataCerere != DateTime.MinValue ? m.DataCerere.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare.ToString("dd.MM.yyyy");
            return map;
        }

        private static Dictionary<string,string> BuildSuspendareSiIncetare(SuspendareSiIncetareSuspendareModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{NrCerere}}"] = m.NrCerere ?? string.Empty;
            map["{{DataCerere}}"] = m.DataCerere != DateTime.MinValue ? m.DataCerere.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DataStartSuspendare}}"] = m.DataStartSuspendare.ToString("dd.MM.yyyy");
            map["{{DataEndSuspendare}}"] = m.DataEndSuspendare.ToString("dd.MM.yyyy");
            map["{{DataIncetareSuspendare}}"] = m.DataIncetareSuspendare.ToString("dd.MM.yyyy");
            return map;
        }

        private static Dictionary<string,string> BuildIncetareSuspendare(IncetareSuspendareModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{NrCerere}}"] = m.NrCerere ?? string.Empty;
            map["{{DataCerere}}"] = m.DataCerere != DateTime.MinValue ? m.DataCerere.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DataIncetareSuspendare}}"] = m.DataIncetareSuspendare.ToString("dd.MM.yyyy");
            return map;
        }

        private static Dictionary<string,string> BuildIncetareDemisie(IncetareDemisieModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{NrCerere}}"] = m.NrCerere ?? string.Empty;
            map["{{DataCerere}}"] = m.DataCerere != DateTime.MinValue ? m.DataCerere.ToString("dd.MM.yyyy") : string.Empty;
            map["{{DataIncetare}}"] = m.DataIncetare.ToString("dd.MM.yyyy");
            map["{{ArticolDemisie}}"] = m.ArticolDemisie ?? string.Empty;
            return map;
        }

        private static Dictionary<string,string> BuildIncetareExpirare(IncetareExpirareModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{DataIncetare}}"] = m.DataIncetare.ToString("dd.MM.yyyy");
            return map;
        }

        private static Dictionary<string,string> BuildIncetareDisciplinar(IncetareDisciplinarModel m)
        {
            var map = new Dictionary<string,string>();
            map["{{CodInregistrare}}"] = m.CodInregistrare ?? string.Empty;
            map["{{DataIncetare}}"] = m.DataIncetare.ToString("dd.MM.yyyy");
            map["{{PerioadaCercetare}}"] = m.PerioadaCercetare ?? string.Empty;
            map["{{NrProcesVerbal}}"] = m.NrProcesVerbal ?? string.Empty;
            map["{{DataProcesVerbal}}"] = m.DataProcesVerbal != DateTime.MinValue ? m.DataProcesVerbal.ToString("dd.MM.yyyy") : string.Empty;
            map["{{LocCercetare}}"] = m.LocCercetare ?? string.Empty;
            map["{{MotiveleSanctionarii}}"] = m.MotiveleSanctionarii ?? string.Empty;
            map["{{ImprejurariFapte}}"] = m.ImprejurariFapte ?? string.Empty;
            map["{{GradVinovatie}}"] = m.GradVinovatie ?? string.Empty;
            map["{{ConsecinteAbateri}}"] = m.ConsecinteAbateri ?? string.Empty;
            map["{{NumeIntocmit}}"] = m.NumeIntocmit ?? string.Empty;
            map["{{FunctieIntocmit}}"] = m.FunctieIntocmit ?? string.Empty;
            return map;
        }
    }
}
