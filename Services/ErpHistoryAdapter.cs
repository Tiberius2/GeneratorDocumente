using ActAditionalPlugin.Models;
using System;

namespace ActAditionalPlugin.Services
{
    public interface IErpHistoryRepository
    {
        void InsertPv(PvModelBase model, dynamic xModule, dynamic xSupport);
        void InsertDocument(DocumentModelBase model, dynamic xModule, dynamic xSupport);
    }

    public class ErpHistoryAdapter : IErpHistoryRepository
    {
        public void InsertPv(PvModelBase model, dynamic xModule, dynamic xSupport)
        {
            if (model == null || xModule == null) return;
            try
            {
                var pvTable = xModule.GetTable("CCCPVEMISE");
                if (pvTable == null || pvTable.Current == null) return;

                pvTable.Current.Append();
                pvTable.Current["PRSN"] = model.PrsnId;
                pvTable.Current["CCCTRNDATE"] = DateTime.Now;
                pvTable.Current["CCCPVTYPE"] = GetPvTypeCode(model.TipPV);
                pvTable.Current["CCCPVNAME"] = GetPvObservatii(model);
                pvTable.Current["CCCPVNUMBER"] = GetPvNumber(model.CodInregistrare);
                pvTable.Current.Post();
                xModule.Exec("Button:Save");
            }
            catch (Exception ex)
            {
                try { xSupport.Warning("Nu s-a putut adauga PV in istoric (CCCPVEMISE): " + ex.Message); } catch { }
            }
        }

        public void InsertDocument(DocumentModelBase model, dynamic xModule, dynamic xSupport)
        {
            if (model == null || xModule == null) return;
            try
            {
                if (model is ActAditionalModel act)
                {
                    var tbl = xModule.GetTable("CCCACTEADITIONALE");
                    if (tbl == null || tbl.Current == null) return;

                    int companyId = 0;
                    try { companyId = xSupport.ConnectionInfo.CompanyId; } catch { }

                    tbl.Current.Append();
                    SafeSetField(tbl, "COMPANY", companyId);
                    SafeSetField(tbl, "PRSN", act.PrsnId);
                    SafeSetField(tbl, "CCCCODINREG", act.CodInregistrare);
                    SafeSetField(tbl, "CCCNRINREG", GetPvNumber(act.CodInregistrare));
                    SafeSetField(tbl, "CCCIDCONTRACT", ParseInt(act.NrCim));
                    SafeSetField(tbl, "CCCDATAINREG", GetSoftoneLoginDate(xSupport));
                    if (act.DataVigoare != DateTime.MinValue)
                        SafeSetField(tbl, "CCCDATAVIGOARE", act.DataVigoare);
                    SafeSetField(tbl, "CCCDOCUMENTSTATUS", 1);
                    tbl.Current.Post();
                    xModule.Exec("Button:Save");
                }
                else if (model is DecizieModelBase dec)
                {
                    var tbl = xModule.GetTable("CCCDCZCONTRACT");
                    if (tbl == null || tbl.Current == null) return;

                    int companyId = 0;
                    try { companyId = xSupport.ConnectionInfo.CompanyId; } catch { }

                    tbl.Current.Append();
                    SafeSetField(tbl, "COMPANY", companyId);
                    SafeSetField(tbl, "PRSN", dec.PrsnId);
                    SafeSetField(tbl, "CCCCODINREG", dec.CodInregistrare);
                    SafeSetField(tbl, "CCCNRINREG", GetPvNumber(dec.CodInregistrare));
                    SafeSetField(tbl, "CCCIDCONTRACT", ParseInt(dec.NrCim));
                    SafeSetField(tbl, "CCCDATAINREG", GetSoftoneLoginDate(xSupport));
                    if (dec.DataDecizie != DateTime.MinValue)
                        SafeSetField(tbl, "CCCDATAVIGOARE", dec.DataDecizie);
                    SafeSetField(tbl, "LINENUM", 1);
                    SafeSetField(tbl, "CCCSTATUS", 1);
                    SafeSetField(tbl, "CCCTIPDCZ", GetDecizieTipText(dec.TipDocument));
                    tbl.Current.Post();
                    xModule.Exec("Button:Save");
                }
            }
            catch (Exception ex)
            {
                try { xSupport.Warning("Nu s-a putut adauga documentul in istoric: " + ex.Message); } catch { }
            }
        }

        private static void SafeSetField(dynamic table, string fieldName, object value)
        {
            try
            {
                if (value == null) return;
                table.Current[fieldName] = value;
            }
            catch { }
        }

        private static int ParseInt(string value)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : 0;
        }

        private static int GetPvTypeCode(TipPV tip)
        {
            switch (tip)
            {
                case TipPV.Echipamente: return 1;
                case TipPV.Autovehicul: return 2;
                case TipPV.Electronice: return 6;
                default: return 0;
            }
        }

        private static int GetPvNumber(string codInregistrare)
        {
            if (string.IsNullOrWhiteSpace(codInregistrare)) return 0;
            int slashIndex = codInregistrare.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex + 1 < codInregistrare.Length)
            {
                int nr;
                if (int.TryParse(codInregistrare.Substring(slashIndex + 1), out nr))
                    return nr;
            }
            int fallback;
            return int.TryParse(codInregistrare, out fallback) ? fallback : 0;
        }

        private static string GetPvObservatii(PvModelBase model)
        {
            var echip = model as PvEchipamenteModel;
            if (echip != null && !string.IsNullOrWhiteSpace(echip.Mentiuni))
                return echip.Mentiuni.Trim();
            var elec = model as PvElecroniceModel;
            if (elec != null && !string.IsNullOrWhiteSpace(elec.Mentiuni))
                return elec.Mentiuni.Trim();
            return "-";
        }

        private static DateTime GetSoftoneLoginDate(dynamic xSupport)
        {
            try
            {
                var ci = xSupport.ConnectionInfo;
                var prop = ci.GetType().GetProperty("LoginDate");
                if (prop != null)
                {
                    object val = prop.GetValue(ci, null);
                    if (val is DateTime)
                        return ((DateTime)val).Date;
                }
            }
            catch { }
            return DateTime.Today;
        }

        private static string GetDecizieTipText(TipDocument tip)
        {
            switch (tip)
            {
                case TipDocument.SuspendareCresterecopil: return "Crestere copil";
                case TipDocument.SuspendareCresterecopilHandicap: return "Crestere copil handicap";
                case TipDocument.SuspendareAbsenteNemotivate: return "Absente nemotivate";
                case TipDocument.SuspendareAcordParti: return "Acordul partilor";
                case TipDocument.SuspendareSiIncetareSuspendare: return "Suspendare + Incetare";
                case TipDocument.IncetareSuspendare: return "Incetare suspendare";
                case TipDocument.IncetareDemisie: return "Incetare demisie";
                case TipDocument.IncetareExpirare: return "Expirare termen";
                case TipDocument.IncetareDisciplinar: return "Concediere disciplinara";
                default: return tip.ToString();
            }
        }
    }
}
