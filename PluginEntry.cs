using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;
using ActAditionalPlugin.UI;
using Softone;

namespace ActAditionalPlugin
{
    // ══════════════════════════════════════════════════════════
    //  S1 — populeaza XSupport static la incarcarea pluginului.
    //  Necesar pentru deschiderea directa ca "Dll Form"
    //  (ActAditionalPlugin.dll;SelectorDialog), unde Softone creeaza
    //  fereastra prin reflection, fara ExecCommand.
    // ══════════════════════════════════════════════════════════
    [WorksOn("GENERAL")]
    public class S1 : TXCode
    {
        public static XSupport xSupp;
        public override void Initialize() { base.Initialize(); xSupp = XSupport; }
    }

    // CMD 4000502 — din orice meniu Softone; angajatul se alege in selector
    [WorksOn("GENERAL")]
    public class ProgramGeneral : TXCode
    {
        public override object ExecCommand(int Cmd)
        {
            if (Cmd != 4000502) return null;
            AppLauncher.Launch(XSupport, 0);
            return base.ExecCommand(Cmd);
        }
    }

    // CMD 4000501 — din ecranul PRSNIN, cu angajatul curent preselectat
    [WorksOn("PRSNIN")]
    public class Program : TXCode
    {
        public override object ExecCommand(int Cmd)
        {
            if (Cmd != 4000501) return null;
            AppLauncher.Launch(XSupport, ReadCurrentPrsnId());
            return base.ExecCommand(Cmd);
        }

        private int ReadCurrentPrsnId()
        {
            try
            {
                var prsnTbl = XModule.GetTable("PRSN");
                if (prsnTbl == null || prsnTbl.Current == null) return 0;
                int prsnId;
                int.TryParse(prsnTbl.Current["PRSN"]?.ToString() ?? string.Empty, out prsnId);
                return prsnId;
            }
            catch { return 0; }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  Pornirea aplicatiei din comenzile Softone: o singura instanta,
    //  serviciile se initializeaza pe firul Softone, apoi selectorul
    //  ruleaza pe un fir STA propriu (documentele se deschid peste el).
    // ══════════════════════════════════════════════════════════
    internal static class AppLauncher
    {
        private const string MutexName = "ActAditionalPlugin_SingleInstance";
        private static Form _active;
        private static Mutex _mutex;

        public static void Launch(XSupport xs, int prsnId)
        {
            try
            {
                // deja deschis -> il aducem in fata
                var active = _active;
                if (active != null && !active.IsDisposed)
                {
                    active.Invoke(new Action(() =>
                    {
                        if (active.WindowState == FormWindowState.Minimized)
                            active.WindowState = FormWindowState.Normal;
                        active.Activate();
                    }));
                    return;
                }

                bool createdNew;
                _mutex = new Mutex(true, MutexName, out createdNew);
                if (!createdNew)
                {
                    _mutex.Dispose(); _mutex = null;
                    return;
                }

                ErpCompanyData company;
                List<PersonInfo> persoane;
                try
                {
                    company = AppServices.Initialize(xs);
                    persoane = ErpDataProvider.LoadPersons(xs);
                }
                catch (Exception ex)
                {
                    xs.Warning("Generator documente HR: inițializarea a eșuat.\n" + ex.Message);
                    ReleaseMutex();
                    return;
                }

                var thread = new Thread(() =>
                {
                    try
                    {
                        using (var selector = new SelectorDialog(xs, prsnId, company, persoane))
                        {
                            _active = selector;
                            selector.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Eroare:\n" + ex.Message, "Generator Documente HR",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _active = null;
                        WordHelper.ShutdownWordApp();
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
                xs.Warning("Generator documente HR error: " + ex.Message);
                ReleaseMutex();
            }
        }

        private static void ReleaseMutex()
        {
            var m = _mutex;
            _mutex = null;
            if (m == null) return;
            try { m.ReleaseMutex(); } catch { }
            m.Dispose();
        }
    }
}
