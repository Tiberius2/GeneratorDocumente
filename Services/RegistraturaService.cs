using System;

namespace ActAditionalPlugin.Services
{
    public sealed class RegistraturaService
    {
        public static RegistraturaService Instance { get; private set; }

        public static void Initialize(dynamic xSupport)
        {
            Instance = new RegistraturaService(xSupport);
        }

        private readonly dynamic _xs;
        private readonly int _company;
        private readonly int _userId;

        private RegistraturaService(dynamic xSupport)
        {
            _xs = xSupport;
            _company = (int)xSupport.ConnectionInfo.CompanyId;
            _userId = (int)xSupport.ConnectionInfo.UserId;
        }

        // ── LoginDate via reflectie ───────────────────────────
        public DateTime GetLoginDate()
        {
            try
            {
                var ci = _xs.ConnectionInfo;
                var prop = ci.GetType().GetProperty("LoginDate");
                if (prop != null)
                {
                    var val = prop.GetValue(ci, null);
                    if (val is DateTime) return ((DateTime)val).Date;
                }
            }
            catch { }
            return DateTime.Today;
        }

        // ── Calcul cod YYddd/NR ───────────────────────────────
        public string CalculateCod(DateTime data)
        {
            string prefix = data.ToString("yy") + data.DayOfYear.ToString("D3");
            string sql = string.Format(
                "SELECT ISNULL(MAX(NRINREG),0)+1 AS NR FROM CCCVREGISTRATURA " +
                "WHERE COMPANY={0} AND CODINREG LIKE '{1}/%'",
                _company, prefix);

            int nr = 1;
            try
            {
                var ds = _xs.GetSQLDataSet(sql);
                if (ds != null && ds.Count > 0)
                    int.TryParse(ds[0, "NR"]?.ToString() ?? "1", out nr);
            }
            catch { }

            return string.Format("{0}/{1}", prefix, nr);
        }

        // ── INSERT via ExecuteSQL ─────────────────────────
        public void Inregistreaza(string codInreg, DateTime dataInreg,
            int tipDocPK, string titluDoc, int prsnId)
        {
            int nrInreg = 1;
            var parts = codInreg.Split('/');
            if (parts.Length == 2) int.TryParse(parts[1], out nrInreg);

            string dataStr = dataInreg.ToString("yyyy-MM-dd");
            string titluSafe = (titluDoc ?? string.Empty).Replace("'", "''");

            string sqlMain = string.Format(
                "INSERT INTO CCCVREGISTRATURA " +
                "(CODINREG,DATAINREG,NRINREG,STATUS,DIRECTIE,TIPDOC,TITLUDOC,DETALIIDOC," +
                " DATASERVER,USERID,TIPTERT,TRDRTERT,PRSNTERT,CCCVARCHAR01,COMPANY) " +
                "VALUES ('{0}','{1}',{2},1,3,{3},'{4}',NULL,GETDATE(),{5},5,NULL,{6},NULL,{7})",
                codInreg, dataStr, nrInreg, tipDocPK, titluSafe, _userId, prsnId, _company);

            string sqlAudit = string.Format(
                "INSERT INTO CCCVDOCAUDIT " +
                "(CODINREG,DATAINREG,STATUS,DIRECTIE,TIPDOC,TITLUDOC,DETALIIDOC,TIPTERT," +
                " TRDRTERT,PRSNTERT,CCCVARCHAR01,TIPMODIFICARE,CAMPMODIFICAT," +
                " VALOAREVECHE,VALOARENOUA,DATAMODIFICARE,USERID,COMPANY) " +
                "VALUES ('{0}','{1}',1,3,{2},'{3}',NULL,5,NULL,{4},NULL,1,NULL,NULL,NULL,GETDATE(),{5},{6})",
                codInreg, dataStr, tipDocPK, titluSafe, prsnId, _userId, _company);

            _xs.ExecuteSQL(sqlMain);
            _xs.ExecuteSQL(sqlAudit);
        }

        // ── Mapari statice ─────────────────────────────────────
        public static int GetTipDocPK(Models.TipDocument tip)
        {
            switch (tip)
            {
                case Models.TipDocument.ActAditional: return 2;
                default: return 11;
            }
        }

        public static int GetTipDocPK(Models.TipPV tip) => 22;

        public static string GetTitluDoc(Models.TipDocument tip)
        {
            switch (tip)
            {
                case Models.TipDocument.ActAditional: return "Act Aditional";
                case Models.TipDocument.SuspendareCresterecopil: return "Decizie Suspendare Crestere Copil";
                case Models.TipDocument.SuspendareCresterecopilHandicap: return "Decizie Suspendare Crestere Copil Handicap";
                case Models.TipDocument.SuspendareAbsenteNemotivate: return "Decizie Suspendare Absente Nemotivate";
                case Models.TipDocument.SuspendareAcordParti: return "Decizie Suspendare Acordul Partilor";
                case Models.TipDocument.SuspendareSiIncetareSuspendare: return "Decizie Suspendare si Incetare";
                case Models.TipDocument.IncetareSuspendare: return "Decizie Incetare Suspendare";
                case Models.TipDocument.IncetareDemisie: return "Decizie Incetare prin Demisie";
                case Models.TipDocument.IncetareExpirare: return "Decizie Incetare prin Expirare Termen";
                case Models.TipDocument.IncetareDisciplinar: return "Decizie Concediere Disciplinara";
                case Models.TipDocument.IncetarePerioadaProba: return "Decizie Incetare Perioada Proba";
                default: return tip.ToString();
            }
        }

        public static string GetTitluDoc(Models.TipPV tip)
        {
            switch (tip)
            {
                case Models.TipPV.Echipamente: return "Proces Verbal Echipamente de Lucru";
                case Models.TipPV.Electronice: return "Proces Verbal Echipamente Electronice";
                case Models.TipPV.Autovehicul: return "Proces Verbal Autovehicul";
                default: return "Proces Verbal";
            }
        }
    }
}