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

        // ── INSERT tabele specifice per categorie ─────────────
        // Apelat dupa Inregistreaza(), pentru categoriile care
        // au si o tabela proprie (Acte Aditionale, Decizii, PV).
        public void InregistreazaTabelaSpecifica(
            string category,
            string codInreg,
            DateTime dataInreg,
            int prsnId,
            System.Collections.Generic.Dictionary<string, object> formValues,
            int cccIdContract = 0)
        {
            if (string.IsNullOrEmpty(category)) return;
            string lower = category.ToLower();

            try
            {
                if (lower.Contains("acte") || lower.Contains("aditional"))
                    InsertActAditional(codInreg, dataInreg, prsnId, formValues, cccIdContract);
                else if (lower.Contains("suspendare") || lower.Contains("incetare") || lower.Contains("interne"))
                    InsertDecizie(codInreg, dataInreg, prsnId, formValues, cccIdContract);
                else if (lower.Contains("verbale") || lower.Contains("procese"))
                    InsertProcesVerbal(codInreg, dataInreg, prsnId, formValues);
                // Cercetare Disciplinara — fara tabela specifica, doar registratura
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[RegistraturaService] InregistreazaTabelaSpecifica error: " + ex.Message);
            }
        }

        // ── CCCACTEADITIONALE ─────────────────────────────────
        // Coloane: COMPANY, PRSN, CCCCODINREG, CCCNRINREG,
        //          CCCDATAINREG, LINENUM(auto), CCCDATAVIGOARE,
        //          CCCDOCUMENTSTATUS=1, CCCMOTIVEACT, CCCACTOBS, CCCIDCONTRACT
        private void InsertActAditional(
            string codInreg, DateTime dataInreg, int prsnId,
            System.Collections.Generic.Dictionary<string, object> formValues,
            int cccIdContract)
        {
            int nrInreg = NrDinCod(codInreg);
            string dataStr = dataInreg.ToString("yyyy-MM-dd");

            // CCCDATAVIGOARE — din campul DataVigoare daca exista
            string dataVigoare = dataStr;
            object dvObj;
            if (formValues.TryGetValue("DataVigoare", out dvObj) && dvObj != null)
            {
                DateTime dv;
                if (DateTime.TryParse(dvObj.ToString(), out dv))
                    dataVigoare = dv.ToString("yyyy-MM-dd");
            }

            // LINENUM — urmatorul nr pentru acest angajat
            int lineNum = NextLineNum("CCCACTEADITIONALE", "PRSN", prsnId);

            string idContractSql = cccIdContract > 0 ? cccIdContract.ToString() : "NULL";

            string sql = string.Format(
                "INSERT INTO CCCACTEADITIONALE " +
                "(COMPANY,PRSN,CCCCODINREG,CCCNRINREG,CCCDATAINREG,LINENUM," +
                " CCCDATAVIGOARE,CCCDOCUMENTSTATUS,CCCMOTIVEACT,CCCACTOBS,CCCIDCONTRACT) " +
                "VALUES ({0},{1},'{2}',{3},'{4}',{5},'{6}',1,NULL,NULL,{7})",
                _company, prsnId, codInreg, nrInreg, dataStr, lineNum,
                dataVigoare, idContractSql);

            _xs.ExecuteSQL(sql);
        }

        // ── CCCDCZCONTRACT ────────────────────────────────────
        // Coloane: COMPANY, PRSN, CCCCODINREG, CCCNRINREG,
        //          CCCDATAINREG, CCCDATAVIGOARE, LINENUM(auto),
        //          CCCSTATUS=1, CCCTIPDCZ(titlu doc), CCCREMARKS, CCCIDCONTRACT
        private void InsertDecizie(
            string codInreg, DateTime dataInreg, int prsnId,
            System.Collections.Generic.Dictionary<string, object> formValues,
            int cccIdContract)
        {
            int nrInreg = NrDinCod(codInreg);
            string dataStr = dataInreg.ToString("yyyy-MM-dd");

            // CCCDATAVIGOARE — data start suspendare / data incetare / data decizie
            string dataVigoare = dataStr;
            foreach (var key in new[] { "DataStartSuspendare", "DataIncetare",
                                        "DataIncetareSuspendare", "DataDecizie" })
            {
                object obj;
                if (formValues.TryGetValue(key, out obj) && obj != null)
                {
                    DateTime d;
                    if (DateTime.TryParse(obj.ToString(), out d))
                    { dataVigoare = d.ToString("yyyy-MM-dd"); break; }
                }
            }

            // CCCTIPDCZ — prioritate: camp explicit "TipDecizie" din formular,
            // altfel dedus din titlul documentului
            string tipDcz = string.Empty;
            object tipObj;
            if (formValues.TryGetValue("TipDecizie", out tipObj) && tipObj != null
                && !string.IsNullOrWhiteSpace(tipObj.ToString()))
            {
                tipDcz = tipObj.ToString().Trim();
            }
            else
            {
                object titleObj;
                if (formValues.TryGetValue("_DocTitle", out titleObj) && titleObj != null)
                    tipDcz = tipDczDinTitlu(titleObj.ToString());
            }

            int lineNum = NextLineNum("CCCDCZCONTRACT", "PRSN", prsnId);
            string idContractSql = cccIdContract > 0 ? cccIdContract.ToString() : "NULL";
            string tipSafe = tipDcz.Replace("'", "''");

            string sql = string.Format(
                "INSERT INTO CCCDCZCONTRACT " +
                "(COMPANY,PRSN,CCCCODINREG,CCCNRINREG,CCCDATAINREG,CCCDATAVIGOARE," +
                " LINENUM,CCCSTATUS,CCCTIPDCZ,CCCREMARKS,CCCIDCONTRACT) " +
                "VALUES ({0},{1},'{2}',{3},'{4}','{5}',{6},1,'{7}',NULL,{8})",
                _company, prsnId, codInreg, nrInreg, dataStr, dataVigoare,
                lineNum, tipSafe, idContractSql);

            _xs.ExecuteSQL(sql);
        }

        // ── CCCPVEMISE ────────────────────────────────────────
        // Coloane: PRSN, COMPANY, CCCPVTYPE, CCCPVNAME,
        //          ISACTIVE=1, CCCTRNDATE, LINENUM(auto),
        //          CCCPVNUMBER, CODINREGISTRARE
        // CCCPVTYPE: 1=Echipamente/Uniforme/Electronice, 2=Autovehicul
        private void InsertProcesVerbal(
            string codInreg, DateTime dataInreg, int prsnId,
            System.Collections.Generic.Dictionary<string, object> formValues)
        {
            int nrInreg = NrDinCod(codInreg);

            // CCCPVTYPE din titlul documentului
            int pvType = 1; // default echipamente
            object titleObj;
            if (formValues.TryGetValue("_DocTitle", out titleObj) && titleObj != null)
            {
                string title = titleObj.ToString().ToLower();
                if (title.Contains("autovehicul")) pvType = 2;
            }

            // CCCPVNAME — titlul documentului
            string pvName = string.Empty;
            if (titleObj != null) pvName = titleObj.ToString().Replace("'", "''");

            int lineNum = NextLineNum("CCCPVEMISE", "PRSN", prsnId);
            string dataStr = dataInreg.ToString("yyyy-MM-dd HH:mm:ss");

            string sql = string.Format(
                "INSERT INTO CCCPVEMISE " +
                "(PRSN,COMPANY,CCCPVTYPE,CCCPVNAME,ISACTIVE,CCCTRNDATE," +
                " LINENUM,CCCPVNUMBER,CODINREGISTRARE) " +
                "VALUES ({0},{1},{2},'{3}',1,GETDATE(),{4},{5},'{6}')",
                prsnId, _company, pvType, pvName, lineNum, nrInreg, codInreg);

            _xs.ExecuteSQL(sql);
        }

        // ── Helpers ───────────────────────────────────────────
        private static int NrDinCod(string codInreg)
        {
            int nr = 1;
            var parts = codInreg.Split('/');
            if (parts.Length == 2) int.TryParse(parts[1], out nr);
            return nr;
        }

        private int NextLineNum(string table, string prsnCol, int prsnId)
        {
            string sql = string.Format(
                "SELECT ISNULL(MAX(LINENUM),0)+1 AS NR FROM {0} WHERE {1}={2} AND COMPANY={3}",
                table, prsnCol, prsnId, _company);
            try
            {
                var ds = _xs.GetSQLDataSet(sql);
                if (ds != null && ds.Count > 0)
                {
                    int nr = 1;
                    int.TryParse(ds[0, "NR"]?.ToString() ?? "1", out nr);
                    return nr;
                }
            }
            catch { }
            return 1;
        }

        private static string tipDczDinTitlu(string title)
        {
            // Folosim direct titlul documentului din JSON, fara diacritice
            return RemoveDiacritics(title);
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (char c in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

    }
}