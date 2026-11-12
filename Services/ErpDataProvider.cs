using System;
using Softone;
using ActAditionalPlugin.Models;

namespace ActAditionalPlugin.Services
{
    public class ErpCimData
    {
        public string NrCim { get; set; }
        public DateTime DataCim { get; set; }
        public string NumeDepartament { get; set; }
        public string CodCor { get; set; }
        public string SerieCI { get; set; }
        public string NrCI { get; set; }
        public string Domiciliu { get; set; }
        public string Telefon { get; set; }
        public string Email { get; set; }
        // Din EMPLOYEE (legat prin PRSN + COMPANY): autoritatea emitenta si data eliberarii CI
        public string AutoritateEmitentaCI { get; set; }
        public DateTime DataEliberareCI { get; set; }

        public ErpCimData()
        {
            AutoritateEmitentaCI = string.Empty;
            DataEliberareCI = DateTime.MinValue;
            NrCim = string.Empty;
            DataCim = DateTime.MinValue;
            NumeDepartament = string.Empty;
            CodCor = string.Empty;
            Telefon = string.Empty;
            Email = string.Empty;
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
        // ── Toti angajatii activi ai companiei (selectorul de angajati) ──
        private const string SqlAngajati =
            "SELECT " +
            "    P.PRSN, " +
            "    P.NAME AS Nume, " +
            "    ISNULL(P.NAME2, '') AS Prenume, " +
            "    P.NAME + ' ' + ISNULL(P.NAME2, '') AS NumeComplet, " +
            "    ISNULL(P.AFM, '') AS CNP, " +
            "    ISNULL(P.SOTITLENAME, '') AS Functie, " +
            "    ISNULL(S.CODE, '') AS CodCor, " +
            "    ISNULL(PEX.CCCVARCHAR05, '') AS NrCim, " +
            "    PEX.DATE03 AS DataCim, " +
            "    ISNULL(D.NAME, '') AS NumeDepartament, " +
            "    ISNULL(P.IDENTITYNUM, '') AS IdentityNum, " +
            "    ISNULL(P.ADDRESS, '') AS Domiciliu " +
            "FROM PRSN P " +
            "JOIN PRSEXTRA PEX ON PEX.PRSN = P.PRSN AND PEX.COMPANY = P.COMPANY " +
            "LEFT JOIN PRSJOBPOS PJ ON PJ.PRSN = P.PRSN AND PJ.COMPANY = P.COMPANY " +
            "LEFT JOIN JOBPOSITION J ON PJ.JOBPOSITION = J.JOBPOSITION " +
            "LEFT JOIN SPECIALTY S ON J.SPECIALTY = S.SPECIALTY " +
            "LEFT JOIN DEPART D ON P.DEPART = D.DEPART AND D.COMPANY = P.COMPANY " +
            "WHERE P.COMPANY = {0} AND P.ISACTIVE = 1 " +
            "ORDER BY P.NAME, P.NAME2";

        public static System.Collections.Generic.List<PersonInfo> LoadPersons(XSupport xs)
        {
            var result = new System.Collections.Generic.List<PersonInfo>();
            var ds = xs.GetSQLDataSet(string.Format(SqlAngajati, xs.ConnectionInfo.CompanyId));
            if (ds == null) return result;

            for (int i = 0; i < ds.Count; i++)
            {
                var p = new PersonInfo
                {
                    PrsnId = Convert.ToInt32(ds[i, "PRSN"]),
                    Nume = ds[i, "Nume"]?.ToString()?.Trim() ?? string.Empty,
                    Prenume = ds[i, "Prenume"]?.ToString()?.Trim() ?? string.Empty,
                    NumeComplet = ds[i, "NumeComplet"]?.ToString()?.Trim() ?? string.Empty,
                    CNP = ds[i, "CNP"]?.ToString()?.Trim() ?? string.Empty,
                    Functie = ds[i, "Functie"]?.ToString()?.Trim() ?? string.Empty,
                    CodCor = ds[i, "CodCor"]?.ToString()?.Trim() ?? string.Empty,
                    NrCim = ds[i, "NrCim"]?.ToString()?.Trim() ?? string.Empty,
                    NumeDepartament = ds[i, "NumeDepartament"]?.ToString()?.Trim() ?? string.Empty,
                    Domiciliu = ds[i, "Domiciliu"]?.ToString()?.Trim() ?? string.Empty
                };

                DateTime dataCim;
                if (DateTime.TryParse(ds[i, "DataCim"]?.ToString() ?? string.Empty, out dataCim))
                    p.DataCim = dataCim;

                string serie, nrci;
                PersonInfo.ParseIdentityNum(ds[i, "IdentityNum"]?.ToString()?.Trim() ?? string.Empty,
                    out serie, out nrci);
                p.SerieCI = serie;
                p.NrCI = nrci;

                result.Add(p);
            }
            return result;
        }

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
                    "    C.WEBPAGE    AS WebsiteCompanie " +
                    "FROM COMPANY C " +
                    "JOIN COMPANYEXT CEXT ON C.COMPANY = CEXT.COMPANY AND CEXT.ACCTOFFICE = 2 " +
                    "WHERE C.COMPANY = " + companyId;

                var ds = xSupport.GetSQLDataSet(sql);

                if (ds != null && ds.Count > 0)
                {
                    result.NumeAngajator = ds[0, "NumeAngajator"]?.ToString()?.Trim() ?? string.Empty;
                    result.CIFAngajator = ds[0, "CIFAngajator"]?.ToString()?.Trim() ?? string.Empty;
                    result.ReprezentantLegal = ds[0, "ReprezentantLegalNume"]?.ToString()?.Trim() ?? string.Empty;
                    // FunctieReprezentant — din PluginConfig (configurata manual)
                    result.FunctieReprezentant = PluginConfig.FunctieReprezentant;

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
                    "SELECT PEX.CCCVARCHAR05 AS NrCim, PEX.DATE03 AS DataCim, " +
                    "ISNULL(D.NAME, '') AS NumeDepartament, " +
                    "ISNULL(P.IDENTITYNUM, '') AS IdentityNum, " +
                    "ISNULL(P.ADDRESS, '') AS Domiciliu, " +
                    "ISNULL(P.MOBILEPHONE, '') AS Telefon, " +
                    "ISNULL(P.EMAIL, '') AS Email " +
                    "FROM PRSEXTRA PEX " +
                    "JOIN PRSN P ON PEX.PRSN = P.PRSN " +
                    "LEFT JOIN DEPART D ON P.DEPART = D.DEPART AND D.COMPANY = " + companyId + " " +
                    "WHERE PEX.PRSN = " + prsnId + " AND PEX.COMPANY = " + companyId);

                if (ds != null && ds.Count > 0)
                {
                    result.NrCim = ds[0, "NrCim"]?.ToString()?.Trim() ?? string.Empty;

                    DateTime parsedDate;
                    string rawDate = ds[0, "DataCim"]?.ToString() ?? string.Empty;
                    if (DateTime.TryParse(rawDate, out parsedDate))
                        result.DataCim = parsedDate;

                    result.NumeDepartament = ds[0, "NumeDepartament"]?.ToString()?.Trim() ?? string.Empty;

                    // SerieCI + NrCI din IDENTITYNUM
                    string identityNum = ds[0, "IdentityNum"]?.ToString()?.Trim() ?? string.Empty;
                    string serie, nrci;
                    PersonInfo.ParseIdentityNum(identityNum, out serie, out nrci);
                    result.SerieCI = serie;
                    result.NrCI = nrci;
                    result.Domiciliu = ds[0, "Domiciliu"]?.ToString()?.Trim() ?? string.Empty;
                    result.Telefon = ds[0, "Telefon"]?.ToString()?.Trim() ?? string.Empty;
                    result.Email = ds[0, "Email"]?.ToString()?.Trim() ?? string.Empty;
                }
                else
                {
                    xSupport.Warning("ActAditional: nu s-a gasit contract pentru PRSN " + prsnId);
                }

                // Carte de identitate — din EMPLOYEE (EMPLOYEE.ISSAUTHORITY, EMPLOYEE.DATEISS).
                // Interogare separata, cu try propriu: daca ceva nu merge aici,
                // restul datelor angajatului raman incarcate.
                try
                {
                    var dsCi = xSupport.GetSQLDataSet(
                        "SELECT ISNULL(E.ISSAUTHORITY, '') AS AutoritateCI, E.DATEISS AS DataEliberareCI " +
                        "FROM EMPLOYEE E " +
                        "WHERE E.PRSN = " + prsnId + " AND E.COMPANY = " + companyId);
                    if (dsCi != null && dsCi.Count > 0)
                    {
                        result.AutoritateEmitentaCI = dsCi[0, "AutoritateCI"]?.ToString()?.Trim() ?? string.Empty;
                        DateTime dataIss;
                        if (DateTime.TryParse(dsCi[0, "DataEliberareCI"]?.ToString() ?? string.Empty, out dataIss))
                            result.DataEliberareCI = dataIss;
                    }
                }
                catch (Exception exCi)
                {
                    System.Diagnostics.Debug.WriteLine("[ErpDataProvider] EMPLOYEE (CI): " + exCi.Message);
                }

                // CodCor — din PRSJOBPOS -> JOBPOSITION -> SPECIALTY
                var dsCor = xSupport.GetSQLDataSet(
                    "SELECT TOP 1 S.CODE AS CodCor " +
                    "FROM PRSJOBPOS PJ " +
                    "JOIN JOBPOSITION J ON PJ.JOBPOSITION = J.JOBPOSITION " +
                    "JOIN SPECIALTY S ON J.SPECIALTY = S.SPECIALTY " +
                    "WHERE PJ.PRSN = " + prsnId + " AND PJ.COMPANY = " + companyId);

                if (dsCor != null && dsCor.Count > 0)
                    result.CodCor = dsCor[0, "CodCor"]?.ToString()?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                xSupport.Warning("ActAditional ErpDataProvider error: " + ex.Message);
            }

            return result;
        }
    }
}