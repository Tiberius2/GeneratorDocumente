using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;
using ActAditionalPlugin.UI;
using Softone;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace ActAditionalPlugin
{
    [WorksOn("PRSNIN")]
    public class Program : TXCode
    {
        private static Form _activeForm;
        private static Mutex _mutex;
        private const string MutexName = "ActAditionalPlugin_SingleInstance";

        // Date angajat citite la intrarea in comanda
        private class PrsnInfo
        {
            public int PrsnId;
            public string NumeSalariat;
            public string CNP;
            public string Functie;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override object ExecCommand(int Cmd)
        {
            if (Cmd != 4000501)
                return null;

            try
            {
                DocumentFormBase.OnDocumentGenerated = AddDocumentToDatagrid;

                // Single-instance guard
                if (_activeForm != null && !_activeForm.IsDisposed)
                {
                    _activeForm.Invoke(new Action(() =>
                    {
                        if (_activeForm.WindowState == FormWindowState.Minimized)
                            _activeForm.WindowState = FormWindowState.Normal;
                        _activeForm.Activate();
                    }));
                    return base.ExecCommand(Cmd);
                }

                bool createdNew;
                _mutex = new Mutex(true, MutexName, out createdNew);
                if (!createdNew)
                {
                    _mutex.Dispose();
                    _mutex = null;
                    return base.ExecCommand(Cmd);
                }

                int companyId = XSupport.ConnectionInfo.CompanyId;
                var prsn = TryReadPrsn(companyId);
                if (prsn == null) return base.ExecCommand(Cmd);

                // Pre-incarca lista angajatilor angajati (pe thread-ul TXCode)
                var angajati = LoadHiredEmployees(companyId);

                ErpCimData cimData = ErpDataProvider.GetCimData(prsn.PrsnId, XSupport);
                ErpCompanyData companyData = ErpDataProvider.GetCompanyData(XSupport);
                string officialName = ReadOfficialName();
                string adresaPrimitor = ReadAdresaPrimitor(prsn.PrsnId, companyId);

                PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                RegistraturaService.Initialize(XSupport);

                // Seteaza contextul pentru generare in masa (captureaza XSupport pe thread-ul TXCode)
                BulkContext.Angajati = angajati;
                BulkContext.CompanyData = companyData;
                BulkContext.GetCimData = prsnId => ErpDataProvider.GetCimData(prsnId, XSupport);
                BulkContext.GetAdresaPrimitor = prsnId => ReadAdresaPrimitor(prsnId, companyId);
                BulkContext.XSupport = XSupport;
                var thread = new Thread(() =>
                {
                    try
                    {
                        // ── Selector tip document (picker angajat integrat in header) ──
                        DocumentSelection selection;
                        using (var selector = new SelectorDialog(angajati, prsn.PrsnId))
                        {
                            _activeForm = selector;
                            if (selector.ShowDialog() != DialogResult.OK) return;
                            selection = selector.Selection;

                            // Daca s-a schimbat angajatul in picker din header
                            if (selector.SelectedPrsnId > 0 && selector.SelectedPrsnId != prsn.PrsnId)
                            {
                                prsn = new PrsnInfo
                                {
                                    PrsnId = selector.SelectedPrsnId,
                                    NumeSalariat = selector.SelectedName,
                                    CNP = selector.SelectedCNP,
                                    Functie = selector.SelectedFunctie
                                };
                                cimData = ErpDataProvider.GetCimData(prsn.PrsnId, XSupport);
                                adresaPrimitor = ReadAdresaPrimitor(prsn.PrsnId, companyId);
                            }
                            _activeForm = null;
                        }

                        bool reopen = true;
                        while (reopen)
                        {
                            reopen = false;
                            using (var form = CreateForm(selection, prsn, cimData, officialName, companyData, adresaPrimitor))
                            {
                                if (form == null) break;
                                SetFormIcon(form);
                                _activeForm = form;
                                var result = form.ShowDialog();
                                _activeForm = null;

                                if (result == DialogResult.Cancel || result == DialogResult.OK)
                                {
                                    using (var selector2 = new SelectorDialog(angajati, prsn.PrsnId))
                                    {
                                        _activeForm = selector2;
                                        if (selector2.ShowDialog() != DialogResult.OK)
                                        {
                                            _activeForm = null;
                                            break;
                                        }
                                        selection = selector2.Selection;
                                        reopen = true;
                                        _activeForm = null;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Eroare:\n" + ex.Message, "Generator documente HR",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        DocumentFormBase.OnDocumentGenerated = null;
                        _activeForm = null;
                        BulkContext.Reset();
                        if (_mutex != null)
                        {
                            try { _mutex.ReleaseMutex(); } catch { }
                            _mutex.Dispose();
                            _mutex = null;
                        }
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                XSupport.Warning("Generator documente HR error: " + ex.Message);
            }

            return base.ExecCommand(Cmd);
        }

        // ── Factory unificat ──────────────────────────────────
        // Creeaza form-ul corect in functie de tipul selectiei (Doc sau PV).
        private Form CreateForm(DocumentSelection selection, PrsnInfo prsn,
            ErpCimData cimData, string officialName, ErpCompanyData companyData, string adresaPrimitor)
        {
            var docSel = selection as DocSelection;
            if (docSel != null)
            {
                var model = CreateDocModel(docSel.Tip, prsn, cimData, officialName, companyData);
                return CreateDocForm(docSel.Tip, model);
            }

            var pvSel = selection as PvSelection;
            if (pvSel != null)
            {
                var model = CreatePvModel(pvSel.Tip, prsn, companyData, adresaPrimitor);
                return CreatePvForm(pvSel.Tip, model, AddPvToDatagrid);
            }

            return null;
        }

        // ── Fabrica modele Doc ────────────────────────────────
        private static DocumentModelBase CreateDocModel(TipDocument tip, PrsnInfo prsn,
            ErpCimData cimData, string officialName, ErpCompanyData companyData)
        {
            DocumentModelBase m;
            switch (tip)
            {
                case TipDocument.ActAditional: m = new ActAditionalModel(); break;
                case TipDocument.SuspendareCresterecopil: m = new SuspendareCresterecopilModel(); break;
                case TipDocument.SuspendareCresterecopilHandicap: m = new SuspendareCresterecopilHandicapModel(); break;
                case TipDocument.SuspendareAbsenteNemotivate: m = new SuspendareAbsenteNemotivateModel(); break;
                case TipDocument.SuspendareAcordParti: m = new SuspendareAcordPartiModel(); break;
                case TipDocument.SuspendareSiIncetareSuspendare: m = new SuspendareSiIncetareSuspendareModel(); break;
                case TipDocument.IncetareSuspendare: m = new IncetareSuspendareModel(); break;
                case TipDocument.IncetareDemisie: m = new IncetareDemisieModel(); break;
                case TipDocument.IncetareExpirare: m = new IncetareExpirareModel(); break;
                case TipDocument.IncetareDisciplinar: m = new IncetareDisciplinarModel(); break;
                case TipDocument.IncetarePerioadaProba: m = new IncetarePerioadaProbaModel(); break;
                case TipDocument.ReferatDisciplinar: m = new ReferatDisciplinarModel(); break;
                case TipDocument.AvertismentDisciplinar: m = new AvertismentDisciplinarModel(); break;
                default: m = new ActAditionalModel(); break;
            }

            m.PrsnId = prsn.PrsnId;
            m.NumeSalariat = prsn.NumeSalariat;
            m.CNP = prsn.CNP;
            m.Functie = prsn.Functie;
            m.NrCim = cimData.NrCim;
            m.DataCim = cimData.DataCim;
            m.NumeDepartament = cimData.NumeDepartament;
            ApplyCompanyData(m, companyData);

            var absente = m as SuspendareAbsenteNemotivateModel;
            if (absente != null && string.IsNullOrWhiteSpace(absente.IntocmitDe))
                absente.IntocmitDe = officialName;

            var disciplinar = m as IncetareDisciplinarModel;
            if (disciplinar != null && string.IsNullOrWhiteSpace(disciplinar.NumeIntocmit))
                disciplinar.NumeIntocmit = officialName;
            m.CodInregistrare = RegistraturaService.Instance.CalculateCod(
            RegistraturaService.Instance.GetLoginDate());
            return m;
        }

        // ── Fabrica formulare Doc ─────────────────────────────
        private static Form CreateDocForm(TipDocument tip, DocumentModelBase model)
        {
            switch (tip)
            {
                case TipDocument.ActAditional: return new ActAditionalForm((ActAditionalModel)model);
                case TipDocument.SuspendareCresterecopil: return new SuspendareCresterecopilForm((SuspendareCresterecopilModel)model);
                case TipDocument.SuspendareCresterecopilHandicap: return new SuspendareCresterecopilHandicapForm((SuspendareCresterecopilHandicapModel)model);
                case TipDocument.SuspendareAbsenteNemotivate: return new SuspendareAbsenteForm((SuspendareAbsenteNemotivateModel)model);
                case TipDocument.SuspendareAcordParti: return new SuspendareAcordPartiForm((SuspendareAcordPartiModel)model);
                case TipDocument.SuspendareSiIncetareSuspendare: return new SuspendareSiIncetareSuspendareForm((SuspendareSiIncetareSuspendareModel)model);
                case TipDocument.IncetareSuspendare: return new IncetareSuspendareForm((IncetareSuspendareModel)model);
                case TipDocument.IncetareDemisie: return new IncetareDemisieForm((IncetareDemisieModel)model);
                case TipDocument.IncetareExpirare: return new IncetareExpirareForm((IncetareExpirareModel)model);
                case TipDocument.IncetareDisciplinar: return new IncetareDisciplinarForm((IncetareDisciplinarModel)model);
                case TipDocument.IncetarePerioadaProba: return new IncetarePerioadaProbaForm((IncetarePerioadaProbaModel)model);
                case TipDocument.ReferatDisciplinar: return new ReferatDisciplinarForm((ReferatDisciplinarModel)model);
                case TipDocument.AvertismentDisciplinar: return new AvertismentDisciplinarForm((AvertismentDisciplinarModel)model);
                default: return null;
            }
        }

        // ── Fabrica modele PV ─────────────────────────────────
        private static PvModelBase CreatePvModel(TipPV tip, PrsnInfo prsn,
            ErpCompanyData companyData, string adresaPrimitor)
        {
            PvModelBase m;
            switch (tip)
            {
                case TipPV.Electronice: m = new PvElecroniceModel(); break;
                case TipPV.Autovehicul: m = new PvAutovehiculModel(); break;
                default: m = new PvEchipamenteModel(); break;
            }

            m.PrsnId = prsn.PrsnId;
            m.NumeSalariat = prsn.NumeSalariat;
            m.CNP = prsn.CNP;
            m.Functie = prsn.Functie;
            ApplyCompanyData(m, companyData);

            var auto = m as PvAutovehiculModel;
            if (auto != null && !string.IsNullOrWhiteSpace(adresaPrimitor))
                auto.Domiciliu = adresaPrimitor;
            m.CodInregistrare = RegistraturaService.Instance.CalculateCod(
            RegistraturaService.Instance.GetLoginDate());
            return m;
        }

        // ── Fabrica formulare PV ──────────────────────────────
        private static Form CreatePvForm(TipPV tip, PvModelBase model, Action<PvModelBase> onPdfGenerated)
        {
            switch (tip)
            {
                case TipPV.Echipamente:
                    return new PvBunuriForm((PvEchipamenteModel)model,
                        "Proces Verbal — Echipamente de lucru / Uniformă", "BUNURI PREDATE", onPdfGenerated);
                case TipPV.Electronice:
                    return new PvBunuriForm((PvElecroniceModel)model,
                        "Proces Verbal — Echipamente Electronice", "ECHIPAMENTE PREDATE", onPdfGenerated);
                case TipPV.Autovehicul:
                    return new PvAutovehiculForm((PvAutovehiculModel)model, onPdfGenerated);
                default:
                    return null;
            }
        }

        // ── Helpers citire date ERP ───────────────────────────
        private List<UI.AngajatPickerDialog.AngajatItem> LoadHiredEmployees(int companyId)
        {
            var result = new List<UI.AngajatPickerDialog.AngajatItem>();
            try
            {
                // Angajati cu pozitie activa (JOBPOSITION != 0) — logica IsPrsnHired
                string sql = string.Format(
                    "SELECT DISTINCT P.PRSN, RTRIM(P.NAME) + ' ' + ISNULL(RTRIM(P.NAME2), '') AS NUMECOMPLET," +
                    " ISNULL(P.AFM, '') AS CNP," +
                    " ISNULL(P.SOTITLENAME, '') AS FUNCTIE" +
                    " FROM PRSN P" +
                    " INNER JOIN PRSJOBPOS J ON J.PRSN = P.PRSN AND J.COMPANY = {0}" +
                    " WHERE J.JOBPOSITION IS NOT NULL AND J.JOBPOSITION <> 0" +
                    " ORDER BY NUMECOMPLET", companyId);

                var ds = XSupport.GetSQLDataSet(sql);
                if (ds != null)
                {
                    for (int i = 0; i < ds.Count; i++)
                    {
                        int id = 0;
                        int.TryParse(ds[i, "PRSN"]?.ToString() ?? string.Empty, out id);
                        if (id == 0) continue;
                        result.Add(new UI.AngajatPickerDialog.AngajatItem
                        {
                            PrsnId = id,
                            Name = (ds[i, "NUMECOMPLET"]?.ToString() ?? string.Empty).Trim().ToUpper(),
                            CNP = ds[i, "CNP"]?.ToString() ?? string.Empty,
                            Functie = ds[i, "FUNCTIE"]?.ToString() ?? string.Empty
                        });
                    }
                }
            }
            catch { }
            return result;
        }

        private PrsnInfo TryReadPrsn(int companyId)
        {
            var prsnTbl = XModule.GetTable("PRSN");
            if (prsnTbl == null || prsnTbl.Current == null) return null;

            int prsnId = 0;
            int.TryParse(prsnTbl.Current["PRSN"]?.ToString() ?? string.Empty, out prsnId);
            if (prsnId == 0) return null;

            string namePart = prsnTbl.Current["NAME"]?.ToString() ?? string.Empty;
            string name2Part = prsnTbl.Current["NAME2"]?.ToString() ?? string.Empty;
            string numeSalariat = string.Format("{0} {1}", namePart, name2Part).Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(numeSalariat)) return null;

            string cnp = prsnTbl.Current["AFM"]?.ToString() ?? string.Empty;

            string functie = string.Empty;
            try
            {
                var ds = XSupport.GetSQLDataSet(
                    "SELECT P.SOTITLENAME FROM PRSN P WHERE P.PRSN = " + prsnId + " AND P.COMPANY = " + companyId);
                if (ds != null && ds.Count > 0)
                    functie = ds[0, "SOTITLENAME"]?.ToString()?.Trim() ?? string.Empty;
            }
            catch { }

            return new PrsnInfo { PrsnId = prsnId, NumeSalariat = numeSalariat, CNP = cnp, Functie = functie };
        }

        private string ReadOfficialName()
        {
            try
            {
                int userId = XSupport.ConnectionInfo.UserId;
                var ds = XSupport.GetSQLDataSet("SELECT NAME FROM USERS WHERE USERS.USERS = " + userId);
                if (ds != null && ds.Count > 0)
                    return ds[0, "NAME"]?.ToString() ?? string.Empty;
            }
            catch { }
            return string.Empty;
        }

        private string ReadAdresaPrimitor(int prsnId, int companyId)
        {
            try
            {
                var ds = XSupport.GetSQLDataSet(
                    "SELECT P.ADDRESS FROM PRSN P WHERE P.PRSN = " + prsnId + " AND P.COMPANY = " + companyId);
                if (ds != null && ds.Count > 0)
                    return ds[0, "ADDRESS"]?.ToString()?.Trim() ?? string.Empty;
            }
            catch { }
            return string.Empty;
        }

        private static void SetFormIcon(Form form)
        {
            try
            {
                string icoPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "Resources", "softone.ico");
                if (System.IO.File.Exists(icoPath))
                    form.Icon = new System.Drawing.Icon(icoPath);
            }
            catch { }
        }

        // ── ApplyCompanyData (overload Doc + PV) ─────────────
        private static void ApplyCompanyData(DocumentModelBase m, ErpCompanyData data)
        {
            bool useErp = data != null && !string.IsNullOrWhiteSpace(data.NumeAngajator);
            m.NumeAngajator = useErp ? data.NumeAngajator : PluginConfig.NumeAngajator;
            m.CIFAngajator = useErp ? data.CIFAngajator : PluginConfig.CIFAngajator;
            m.ReprezentantLegal = useErp ? data.ReprezentantLegal : PluginConfig.ReprezentantLegal;
            m.FunctieReprezentant = useErp ? data.FunctieReprezentant : PluginConfig.FunctieReprezentant;
            m.AdresaCompanie = useErp ? data.AdresaCompanie : PluginConfig.AdresaCompanie;
            m.ZipCompanie = useErp ? data.ZipCompanie : PluginConfig.ZipCompanie;
            m.NrRegComertului = useErp ? data.NrRegComertului : PluginConfig.NrRegComertului;
            m.IbanCompanie = useErp ? data.IbanCompanie : PluginConfig.IbanCompanie;
            m.NrTelefonCompanie = useErp ? data.NrTelefonCompanie : PluginConfig.NrTelefonCompanie;
            m.EmailCompanie = useErp ? data.EmailCompanie : PluginConfig.EmailCompanie;
            m.WebsiteCompanie = useErp ? data.WebsiteCompanie : PluginConfig.WebsiteCompanie;
        }

        private static void ApplyCompanyData(PvModelBase m, ErpCompanyData data)
        {
            bool useErp = data != null && !string.IsNullOrWhiteSpace(data.NumeAngajator);
            m.NumeAngajator = useErp ? data.NumeAngajator : PluginConfig.NumeAngajator;
            m.CIFAngajator = useErp ? data.CIFAngajator : PluginConfig.CIFAngajator;
            m.ReprezentantLegal = useErp ? data.ReprezentantLegal : PluginConfig.ReprezentantLegal;
            m.FunctieReprezentant = useErp ? data.FunctieReprezentant : PluginConfig.FunctieReprezentant;
            m.AdresaCompanie = useErp ? data.AdresaCompanie : PluginConfig.AdresaCompanie;
            m.ZipCompanie = useErp ? data.ZipCompanie : PluginConfig.ZipCompanie;
            m.NrRegComertului = useErp ? data.NrRegComertului : PluginConfig.NrRegComertului;
            m.IbanCompanie = useErp ? data.IbanCompanie : PluginConfig.IbanCompanie;
            m.NrTelefonCompanie = useErp ? data.NrTelefonCompanie : PluginConfig.NrTelefonCompanie;
            m.EmailCompanie = useErp ? data.EmailCompanie : PluginConfig.EmailCompanie;
            m.WebsiteCompanie = useErp ? data.WebsiteCompanie : PluginConfig.WebsiteCompanie;
        }

        // ── Inserare datagrid ─────────────────────────────────
        private void AddPvToDatagrid(PvModelBase model)
        {
            if (model == null) return;
            try
            {
                var pvTable = XModule.GetTable("CCCPVEMISE");
                int companyId = XSupport.ConnectionInfo.CompanyId;
                if (pvTable == null || pvTable.Current == null) return;
                pvTable.Current.Append();
                pvTable.Current["PRSN"] = model.PrsnId;
                pvTable.Current["CCCTRNDATE"] = DateTime.Now;
                pvTable.Current["CODINREGISTRARE"] = model.CodInregistrare;
                pvTable.Current["CCCPVTYPE"] = GetPvTypeCode(model.TipPV);
                pvTable.Current["CCCPVNAME"] = GetPvObservatii(model);
                pvTable.Current["CCCPVNUMBER"] = GetPvNumber(model.CodInregistrare);
                pvTable.Current.Post();
                this.XModule.Exec("Button:Save");
            }
            catch (Exception ex)
            {
                XSupport.Warning("Nu s-a putut adauga PV in istoric (CCCPVEMISE): " + ex.Message);
            }
        }

        private void AddDocumentToDatagrid(DocumentModelBase model)
        {
            if (model == null) return;
            try
            {
                if (model is ActAditionalModel)
                    AddActAditionalToDatagrid((ActAditionalModel)model);
                else if (model is DecizieModelBase)
                    AddDecizieToDatagrid((DecizieModelBase)model);
            }
            catch (Exception ex)
            {
                XSupport.Warning("Nu s-a putut adauga documentul in istoric: " + ex.Message);
            }
        }

        private void AddActAditionalToDatagrid(ActAditionalModel model)
        {
            try
            {
                var tbl = XModule.GetTable("CCCACTEADITIONALE");
                if (tbl == null || tbl.Current == null) return;

                int companyId = XSupport.ConnectionInfo.CompanyId;
                tbl.Current.Append();
                SafeSetField(tbl, "COMPANY", companyId);
                SafeSetField(tbl, "PRSN", model.PrsnId);
                SafeSetField(tbl, "CCCCODINREG", model.CodInregistrare);
                SafeSetField(tbl, "CCCNRINREG", GetPvNumber(model.CodInregistrare));
                SafeSetField(tbl, "CCCIDCONTRACT", ParseInt(model.NrCim));
                SafeSetField(tbl, "CCCDATAINREG", GetSoftoneLoginDate());
                if (model.DataVigoare != DateTime.MinValue)
                    SafeSetField(tbl, "CCCDATAVIGOARE", model.DataVigoare);
                SafeSetField(tbl, "CCCDOCUMENTSTATUS", 1);
                if (!string.IsNullOrWhiteSpace(model.MentiuniDocument))
                    SafeSetField(tbl, "CCCACTOBS", model.MentiuniDocument);
                tbl.Current.Post();
                this.XModule.Exec("Button:Save");
            }
            catch (Exception ex)
            {
                XSupport.Warning("Istoric act aditional indisponibil (CCCACTEADITIONALE): " + ex.Message);
            }
        }

        private void AddDecizieToDatagrid(DecizieModelBase model)
        {
            try
            {
                var tbl = XModule.GetTable("CCCDCZCONTRACT");
                if (tbl == null || tbl.Current == null) return;

                int companyId = XSupport.ConnectionInfo.CompanyId;
                tbl.Current.Append();
                SafeSetField(tbl, "COMPANY", companyId);
                SafeSetField(tbl, "PRSN", model.PrsnId);
                SafeSetField(tbl, "CCCCODINREG", model.CodInregistrare);
                SafeSetField(tbl, "CCCNRINREG", GetPvNumber(model.CodInregistrare));
                SafeSetField(tbl, "CCCIDCONTRACT", ParseInt(model.NrCim));
                SafeSetField(tbl, "CCCDATAINREG", GetSoftoneLoginDate());
                if (model.DataDecizie != DateTime.MinValue)
                    SafeSetField(tbl, "CCCDATAVIGOARE", model.DataDecizie);
                SafeSetField(tbl, "LINENUM", 1);
                SafeSetField(tbl, "CCCSTATUS", 1);
                SafeSetField(tbl, "CCCTIPDCZ", GetDecizieTipText(model.TipDocument));
                if (!string.IsNullOrWhiteSpace(model.MentiuniDocument))
                    SafeSetField(tbl, "CCCREMARKS", model.MentiuniDocument);
                tbl.Current.Post();
                this.XModule.Exec("Button:Save");
            }
            catch (Exception ex)
            {
                XSupport.Warning("Istoric decizie indisponibil (CCCDCZCONTRACT): " + ex.Message);
            }
        }

        // ── Utilitare ─────────────────────────────────────────
        private static void SafeSetField(dynamic table, string fieldName, object value)
        {
            try { if (value != null) table.Current[fieldName] = value; }
            catch { }
        }

        private DateTime GetSoftoneLoginDate()
        {
            try
            {
                var ci = XSupport.ConnectionInfo;
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
            return !string.IsNullOrWhiteSpace(model.MentiuniDocument)
                ? model.MentiuniDocument.Trim()
                : "-";
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
                case TipDocument.IncetarePerioadaProba: return "Incetare perioada proba";
                default: return tip.ToString();
            }
        }
    }
}