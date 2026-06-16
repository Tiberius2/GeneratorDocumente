using System;
using Softone;

namespace ActAditionalPlugin.Services
{
    public class ErpCimData
    {
        public string NrCim { get; set; }
        public DateTime DataCim { get; set; }

        public ErpCimData()
        {
            NrCim = string.Empty;
            DataCim = DateTime.MinValue;
        }
    }

    public class ErpCompanyData
    {
        public string NumeAngajator { get; set; }
        public string CIFAngajator { get; set; }
        public string ReprezentantLegal { get; set; }
        public string FunctieReprezentant { get; set; }
        public string AdresaCompanie { get; set; }
        public string ZipCompanie { get; set; }
        public string NrRegComertului { get; set; }
        public string IbanCompanie { get; set; }
        public string NrTelefonCompanie { get; set; }
        public string EmailCompanie { get; set; }
        public string WebsiteCompanie { get; set; }

        public ErpCompanyData()
        {
            NumeAngajator = string.Empty;
            CIFAngajator = string.Empty;
            ReprezentantLegal = string.Empty;
            FunctieReprezentant = string.Empty;
            AdresaCompanie = string.Empty;
            ZipCompanie = string.Empty;
            NrRegComertului = string.Empty;
            IbanCompanie = string.Empty;
            NrTelefonCompanie = string.Empty;
            EmailCompanie = string.Empty;
            WebsiteCompanie = string.Empty;
        }
    }

    public static class ErpDataProvider
    {
        /// <summary>
        /// Preia toate datele companiei + reprezentantul legal + functia lui
        /// intr-un singur SQL cu JOIN.
        /// </summary>
        public static ErpCompanyData GetCompanyData(XSupport xSupport)
        {
            var result = new ErpCompanyData();

            try
            {
                int companyId = xSupport.ConnectionInfo.CompanyId;

                // SQL combinat: date firma + reprezentant legal + functia lui
                string sql =
                    "SELECT " +
                    "    C.NAME       AS NumeAngajator, " +
                    "    C.AFM        AS CIFAngajator, " +
                    "    CEXT.NAME + ' ' + CEXT.NAME2 AS ReprezentantLegalNume, " +
                    "    CEXT.NAME    AS RepNume, " +
                    "    CEXT.NAME2   AS RepNume2, " +
                    "    'Jud.' + C.DISTRICT + ', Comuna Mihai Eminescu, Sat ' + C.CITY + ' ' + C.ADDRESS AS AdresaCompanie, " +
                    "    C.ZIP        AS ZipCompanie, " +
                    "    C.BGBULSTAT  AS NrRegComertului, " +
                    "    C.IBAN       AS IbanCompanie, " +
                    "    C.PHONE1     AS NrTelefonCompanie, " +
                    "    C.EMAIL      AS EmailCompanie, " +
                    "    C.WEBPAGE    AS WebsiteCompanie, " +
                    "    S.NAME       AS FunctieReprezentant " +
                    "FROM COMPANY C " +
                    "JOIN COMPANYEXT CEXT ON C.COMPANY = CEXT.COMPANY " +
                    "LEFT JOIN PRSN P ON P.COMPANY = C.COMPANY " +
                    "    AND P.NAME  LIKE '%' + CEXT.NAME  + '%' " +
                    "    AND P.NAME2 LIKE '%' + CEXT.NAME2 + '%' " +
                    "LEFT JOIN SPECIALTY S ON P.SPECIALTY = S.SPECIALTY " +
                    "WHERE CEXT.ACCTOFFICE = 2 AND C.COMPANY = " + companyId;

                var ds = xSupport.GetSQLDataSet(sql);

                if (ds != null && ds.Count > 0)
                {
                    result.NumeAngajator = ds[0, "NumeAngajator"]?.ToString()?.Trim() ?? string.Empty;
                    result.CIFAngajator = ds[0, "CIFAngajator"]?.ToString()?.Trim() ?? string.Empty;
                    result.ReprezentantLegal = ds[0, "ReprezentantLegalNume"]?.ToString()?.Trim() ?? string.Empty;
                    result.FunctieReprezentant = ds[0, "FunctieReprezentant"]?.ToString()?.Trim() ?? string.Empty;
                    result.AdresaCompanie = ds[0, "AdresaCompanie"]?.ToString()?.Trim() ?? string.Empty;
                    result.ZipCompanie = ds[0, "ZipCompanie"]?.ToString()?.Trim() ?? string.Empty;
                    result.NrRegComertului = ds[0, "NrRegComertului"]?.ToString()?.Trim() ?? string.Empty;
                    result.IbanCompanie = ds[0, "IbanCompanie"]?.ToString()?.Trim() ?? string.Empty;
                    result.NrTelefonCompanie = ds[0, "NrTelefonCompanie"]?.ToString()?.Trim() ?? string.Empty;
                    result.EmailCompanie = ds[0, "EmailCompanie"]?.ToString()?.Trim() ?? string.Empty;
                    result.WebsiteCompanie = ds[0, "WebsiteCompanie"]?.ToString()?.Trim() ?? string.Empty;
                }
                else
                {
                    xSupport.Warning("ActAditional: nu s-au gasit date companie pentru COMPANY " + companyId);
                }
            }
            catch (Exception ex)
            {
                xSupport.Warning("ActAditional GetCompanyData error: " + ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Citeste numarul si data contractului din PRSEXTRA.
        /// </summary>
        public static ErpCimData GetCimData(int prsnId, XSupport xSupport)
        {
            var result = new ErpCimData();

            try
            {
                int companyId = xSupport.ConnectionInfo.CompanyId;
                var ds = xSupport.GetSQLDataSet(
                    "SELECT PEX.NUM03 AS NrCim, PEX.DATE03 AS DataCim " +
                    "FROM PRSEXTRA PEX " +
                    "JOIN PRSN P ON PEX.PRSN = P.PRSN " +
                    "WHERE PEX.PRSN = " + prsnId + " AND PEX.COMPANY = " + companyId);

                if (ds != null && ds.Count > 0)
                {
                    var nrCimObj = ds[0, "NrCim"];

                    if (nrCimObj != null && nrCimObj != DBNull.Value)
                        result.NrCim = Convert.ToInt32(nrCimObj).ToString();
                    else
                        result.NrCim = string.Empty;

                    DateTime parsedDate;
                    string rawDate = ds[0, "DataCim"]?.ToString() ?? string.Empty;
                    if (DateTime.TryParse(rawDate, out parsedDate))
                        result.DataCim = parsedDate;
                }
                else
                {
                    xSupport.Warning("ActAditional: nu s-a gasit contract pentru PRSN " + prsnId);
                }
            }
            catch (Exception ex)
            {
                xSupport.Warning("ActAditional ErpDataProvider error: " + ex.Message);
            }

            return result;
        }
    }
}