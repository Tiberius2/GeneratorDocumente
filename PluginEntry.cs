using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;
using ActAditionalPlugin.UI;
using Softone;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ActAditionalPlugin
{
    // ══════════════════════════════════════════════════════════
    //  S1 — populeaza XSupport static la incarcarea pluginului.
    //  Necesar pentru apelul direct ca "Dll Form" din Softone
    //  (Tip operatie: Dll Form, Obiect/Fisier: ActAditionalPlugin.dll;SelectorDialog),
    //  unde Softone instantiaza formul direct prin reflection,
    //  fara sa treaca prin ExecCommand. SelectorDialog() foloseste
    //  S1.xSupp pentru a-si incarca singur datele.
    //  (acelasi pattern folosit in WacomSignaturePDF)
    // ══════════════════════════════════════════════════════════
    [WorksOn("GENERAL")]
    public class S1 : TXCode
    {
        public static XSupport xSupp;
        public override void Initialize() { base.Initialize(); xSupp = XSupport; }
    }

    // ══════════════════════════════════════════════════════════
    //  CMD 4000502 — apelabil din orice meniu Softone, fara a
    //  depinde de ecranul PRSNIN. Userul alege angajatul direct
    //  din SelectorDialog.
    //  [WorksOn("GENERAL")] = nu necesita context de ecran specific
    //  (acelasi pattern folosit in WacomSignaturePDF).
    // ══════════════════════════════════════════════════════════
    [WorksOn("GENERAL")]
    public class ProgramGeneral : TXCode
    {
        private static Form _activeFormGeneral;
        private static Mutex _mutexGeneral;
        private const string MutexNameGeneral = "ActAditionalPlugin_SingleInstance";

        private static string TemplatesRoot
        {
            get
            {
                string configured = PluginConfig.TemplatesRoot;
                if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
                    return configured;

                string dllDir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                return Path.Combine(dllDir, "Templates");
            }
        }

        public override object ExecCommand(int Cmd)
        {
            if (Cmd != 4000502)
                return null;

            try
            {
                // Single-instance guard (separat de cel din Program/PRSNIN,
                // ca sa nu interfereze cu fluxul existent)
                if (_activeFormGeneral != null && !_activeFormGeneral.IsDisposed)
                {
                    _activeFormGeneral.Invoke(new Action(() =>
                    {
                        if (_activeFormGeneral.WindowState == FormWindowState.Minimized)
                            _activeFormGeneral.WindowState = FormWindowState.Normal;
                        _activeFormGeneral.Activate();
                    }));
                    return base.ExecCommand(Cmd);
                }

                bool createdNew;
                _mutexGeneral = new Mutex(true, MutexNameGeneral, out createdNew);
                if (!createdNew)
                {
                    _mutexGeneral.Dispose(); _mutexGeneral = null;
                    return base.ExecCommand(Cmd);
                }

                // ── Initializare servicii (pe thread-ul TXCode) ───
               // PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                RegistraturaService.Initialize(XSupport);
                HookRegistry.RegisterAll();

                try
                {
                    DocumentRegistry.Initialize(TemplatesRoot);
                }
                catch (Exception ex)
                {
                    XSupport.Warning("Generator documente HR: Nu s-a putut incarca folderul Templates.\n" + ex.Message);
                    ReleaseMutexGeneral();
                    return base.ExecCommand(Cmd);
                }

                var persoane = PersonPickerDialog.LoadFromErp(XSupport);
                var companyData = ErpDataProvider.GetCompanyData(XSupport);

                BulkContext.XSupport = XSupport;
                BulkContext.CompanyData = companyData;

                var thread = new Thread(() =>
                {
                    try
                    {
                        RunLoopGeneral(companyData, persoane);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Eroare:\n" + ex.Message,
                            "Generator Documente HR",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _activeFormGeneral = null;
                        BulkContext.Reset();
                        ReleaseMutexGeneral();
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                XSupport.Warning("Generator documente HR error: " + ex.Message);
                ReleaseMutexGeneral();
            }

            return base.ExecCommand(Cmd);
        }

        private void RunLoopGeneral(ErpCompanyData companyData, List<PersonInfo> persoane)
        {
            ErpCimData cimData = null;
            int currentPrsnId = 0;

            while (true)
            {
                DocumentDefinition selectedDoc;
                PersonInfo selectedPerson;

                using (var selector = new SelectorDialog(persoane, currentPrsnId))
                {
                    _activeFormGeneral = selector;
                    if (selector.ShowDialog() != DialogResult.OK)
                    {
                        _activeFormGeneral = null;
                        return;
                    }

                    selectedDoc = selector.SelectedDocument;
                    selectedPerson = selector.SelectedPerson;
                    _activeFormGeneral = null;
                }

                if (selectedPerson == null)
                {
                    XSupport.Warning("Selectează un angajat pentru a continua.");
                    continue;
                }

                if (cimData == null || selectedPerson.PrsnId != currentPrsnId)
                {
                    cimData = ErpDataProvider.GetCimData(selectedPerson.PrsnId, BulkContext.XSupport);
                    currentPrsnId = selectedPerson.PrsnId;
                }

                var common = CommonDocumentValues.FromErp(
                    selectedPerson.PrsnId,
                    selectedPerson.NumeComplet,
                    selectedPerson.CNP,
                    selectedPerson.Functie,
                    cimData,
                    companyData);

                common.CodInregistrare = RegistraturaService.Instance.CalculateCod(
                    RegistraturaService.Instance.GetLoginDate());

                using (var form = new DynamicForm(selectedDoc, common, persoane))
                {
                    SetFormIconGeneral(form);
                    _activeFormGeneral = form;
                    form.ShowDialog();
                    _activeFormGeneral = null;
                }
            }
        }

        private static void SetFormIconGeneral(Form form)
        {
            try
            {
                string icoPath = Path.Combine(
                    Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "Resources", "softone.ico");
                if (File.Exists(icoPath))
                    form.Icon = new System.Drawing.Icon(icoPath);
            }
            catch { }
        }

        private void ReleaseMutexGeneral()
        {
            if (_mutexGeneral != null)
            {
                try { _mutexGeneral.ReleaseMutex(); } catch { }
                _mutexGeneral.Dispose();
                _mutexGeneral = null;
            }
        }
    }

    [WorksOn("PRSNIN")]
    public class Program : TXCode
    {
        private static Form _activeForm;
        private static Mutex _mutex;
        private const string MutexName = "ActAditionalPlugin_SingleInstance";

        // Calea catre folderul Templates (configurat in PluginConfig sau relativ la DLL)
        private static string TemplatesRoot
        {
            get
            {
                // Incearca din PluginConfig primul
                string configured = PluginConfig.TemplatesRoot;
                if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
                    return configured;

                // Fallback: langa DLL
                string dllDir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                return Path.Combine(dllDir, "Templates");
            }
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override object ExecCommand(int Cmd)
        {
            if (Cmd == 4000501)
                return ExecOpenWithCurrentPrsn(Cmd);

            return null;
        }

        // ══════════════════════════════════════════════════════
        //  CMD 4000501 — deschide cu angajatul curent din PRSNIN
        // ══════════════════════════════════════════════════════
        private object ExecOpenWithCurrentPrsn(int Cmd)
        {
            try
            {
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
                    _mutex.Dispose(); _mutex = null;
                    return base.ExecCommand(Cmd);
                }

                int companyId = XSupport.ConnectionInfo.CompanyId;

                // ── Citeste angajatul curent din ecranul PRSNIN ───
                var currentPrsn = TryReadCurrentPrsn(companyId);
                if (currentPrsn == null)
                {
                    ReleaseMutex();
                    return base.ExecCommand(Cmd);
                }

                // ── Initializare servicii (pe thread-ul TXCode) ───
                // PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                RegistraturaService.Initialize(XSupport);
                HookRegistry.RegisterAll();

                // ── Initializare DocumentRegistry ─────────────────
                try
                {
                    DocumentRegistry.Initialize(TemplatesRoot);
                }
                catch (Exception ex)
                {
                    XSupport.Warning("Generator documente HR: Nu s-a putut incarca folderul Templates.\n" + ex.Message);
                    ReleaseMutex();
                    return base.ExecCommand(Cmd);
                }

                // ── Incarca toti angajatii activi din ERP ──────────
                var persoane = PersonPickerDialog.LoadFromErp(XSupport);

                // ── Date angajat curent ────────────────────────────
                var cimData = ErpDataProvider.GetCimData(currentPrsn.PrsnId, XSupport);
                var companyData = ErpDataProvider.GetCompanyData(XSupport);

                // ── BulkContext pentru acces din forme ─────────────
                BulkContext.XSupport = XSupport;
                BulkContext.CompanyData = companyData;

                // ── Porneste thread STA ────────────────────────────
                var thread = new Thread(() =>
                {
                    try
                    {
                        RunLoop(currentPrsn, cimData, companyData, persoane);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Eroare:\n" + ex.Message,
                            "Generator Documente HR",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _activeForm = null;
                        BulkContext.Reset();
                        ReleaseMutex();
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                XSupport.Warning("Generator documente HR error: " + ex.Message);
                ReleaseMutex();
            }

            return base.ExecCommand(Cmd);
        }

        // ══════════════════════════════════════════════════════
        //  CMD 4000502 — vezi clasa ProgramGeneral [WorksOn("GENERAL")]
        //  mai jos in acest fisier. Acel cmd nu mai este gestionat
        //  aici, ca sa nu depinda de ecranul PRSNIN.
        // ══════════════════════════════════════════════════════

        private void ReleaseMutex()
        {
            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
                _mutex.Dispose();
                _mutex = null;
            }
        }

        // ══════════════════════════════════════════════════════
        //  LOOP PRINCIPAL
        //  SelectorDialog → DynamicForm → revenire la Selector
        // ══════════════════════════════════════════════════════
        private void RunLoop(
            PrsnSnapshot currentPrsn,
            ErpCimData cimData,
            ErpCompanyData companyData,
            List<PersonInfo> persoane)
        {
            int currentPrsnId = currentPrsn?.PrsnId ?? 0;

            while (true)
            {
                // ── Selector: alege angajat + tip document ─────────
                DocumentDefinition selectedDoc;
                PersonInfo selectedPerson;

                using (var selector = new SelectorDialog(persoane, currentPrsnId))
                {
                    // Pre-selecteaza angajatul curent (daca exista)
                    _activeForm = selector;
                    if (selector.ShowDialog() != DialogResult.OK)
                    {
                        _activeForm = null;
                        return;
                    }

                    selectedDoc = selector.SelectedDocument;
                    selectedPerson = selector.SelectedPerson;
                    _activeForm = null;
                }

                if (selectedPerson == null)
                {
                    XSupport.Warning("Selectează un angajat pentru a continua.");
                    continue;
                }

                // Reincarca datele CIM daca angajatul s-a schimbat fata de
                // ce aveam deja incarcat (sau daca nu aveam nimic incarcat)
                if (cimData == null || selectedPerson.PrsnId != currentPrsnId)
                {
                    cimData = ErpDataProvider.GetCimData(selectedPerson.PrsnId, BulkContext.XSupport);
                    currentPrsnId = selectedPerson.PrsnId;
                }

                PersonInfo personForDoc = selectedPerson;

                // ── Construieste CommonDocumentValues ──────────────
                var common = CommonDocumentValues.FromErp(
                    personForDoc.PrsnId,
                    personForDoc.NumeComplet,
                    personForDoc.CNP,
                    personForDoc.Functie,
                    cimData,
                    companyData);

                // Calculeaza cod inregistrare
                common.CodInregistrare = RegistraturaService.Instance.CalculateCod(
                    RegistraturaService.Instance.GetLoginDate());

                // ── Deschide DynamicForm ───────────────────────────
                using (var form = new DynamicForm(selectedDoc, common, persoane))
                {
                    SetFormIcon(form);
                    _activeForm = form;
                    form.ShowDialog();
                    _activeForm = null;
                }

                // Dupa inchiderea formularului, bucla reporneste → SelectorDialog
            }
        }

        // ══════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════
        private PrsnSnapshot TryReadCurrentPrsn(int companyId)
        {
            try
            {
                var prsnTbl = XModule.GetTable("PRSN");
                if (prsnTbl == null || prsnTbl.Current == null) return null;

                int prsnId = 0;
                int.TryParse(prsnTbl.Current["PRSN"]?.ToString() ?? string.Empty, out prsnId);
                if (prsnId == 0) return null;

                string name = prsnTbl.Current["NAME"]?.ToString() ?? string.Empty;
                string name2 = prsnTbl.Current["NAME2"]?.ToString() ?? string.Empty;
                string numeSalariat = string.Format("{0} {1}", name, name2).Trim().ToUpper();
                if (string.IsNullOrWhiteSpace(numeSalariat)) return null;

                string cnp = prsnTbl.Current["AFM"]?.ToString() ?? string.Empty;

                string functie = string.Empty;
                try
                {
                    var ds = XSupport.GetSQLDataSet(
                        "SELECT SOTITLENAME FROM PRSN WHERE PRSN = " + prsnId +
                        " AND COMPANY = " + companyId);
                    if (ds != null && ds.Count > 0)
                        functie = ds[0, "SOTITLENAME"]?.ToString()?.Trim() ?? string.Empty;
                }
                catch { }

                return new PrsnSnapshot
                {
                    PrsnId = prsnId,
                    NumeSalariat = numeSalariat,
                    CNP = cnp,
                    Functie = functie
                };
            }
            catch { return null; }
        }

        private static void SetFormIcon(Form form)
        {
            try
            {
                string icoPath = Path.Combine(
                    Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "Resources", "softone.ico");
                if (File.Exists(icoPath))
                    form.Icon = new System.Drawing.Icon(icoPath);
            }
            catch { }
        }

        // ── DTO minimal pentru angajatul curent din ecran ──────
        private class PrsnSnapshot
        {
            public int PrsnId { get; set; }
            public string NumeSalariat { get; set; }
            public string CNP { get; set; }
            public string Functie { get; set; }
        }
    }
}