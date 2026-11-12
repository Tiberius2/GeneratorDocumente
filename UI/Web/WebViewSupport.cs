using System;
using System.IO;
using System.Reflection;
using Microsoft.Web.WebView2.Core;

namespace ActAditionalPlugin.UI.Web
{
    // ══════════════════════════════════════════════════════════
    //  WEBVIEW2 SUPPORT
    //  - WebView2Loader.dll (nativ, x86) e un fisier SEPARAT, care
    //    trebuie sa stea langa ActAditionalPlugin.dll (folderul Soft1).
    //    Nu e inclus in DLL. La build se copiaza in bin\x86\Release.
    //  - Folderul de date WebView2 e in %LOCALAPPDATA% (folderul
    //    Soft1 din Program Files nu e scriibil).
    //  - Verifica daca WebView2 Runtime e instalat.
    // ══════════════════════════════════════════════════════════
    internal static class WebViewSupport
    {
        private const string LoaderFileName = "WebView2Loader.dll";
        private static readonly object _lock = new object();
        private static bool _loaderReady;

        public static string UserDataFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GeneratorDocumente", "WebView2", "UserData");
            }
        }

        // Folderul in care sta ActAditionalPlugin.dll (si WebView2Loader.dll)
        private static string PluginFolder
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        // Spune SDK-ului sa incarce WebView2Loader.dll de langa plugin.
        // Se face o singura data.
        public static void EnsureLoader()
        {
            lock (_lock)
            {
                if (_loaderReady) return;

                string folder = PluginFolder;
                if (!File.Exists(Path.Combine(folder, LoaderFileName)))
                    throw new FileNotFoundException(
                        LoaderFileName + " lipsește. Trebuie copiat lângă ActAditionalPlugin.dll, în:\n" + folder);

                CoreWebView2Environment.SetLoaderDllFolderPath(folder);
                _loaderReady = true;
            }
        }

        // Creeaza mediul WebView2 cu limba romana (--lang=ro-RO), fara
        // CoreWebView2EnvironmentOptions (da "Class not registered" cu loader-ul
        // incarcat din folderul pluginului). Argumentul se pune in variabila de
        // mediu WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS doar pe durata apelului,
        // apoi se reface valoarea veche (nu afectam alte componente din Softone).
        private const string ArgsVar = "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS";

        // ro = true  -> --lang=ro-RO, folder de date "UserDataRo" (separat, ca sa nu
        //              intre in conflict cu procese WebView2 pornite fara limba);
        // ro = false -> pornirea veche, fara argumente, folderul "UserData".
        public static System.Threading.Tasks.Task<CoreWebView2Environment> CreateEnvironment(bool ro)
        {
            EnsureLoader();
            if (!ro)
                return CoreWebView2Environment.CreateAsync(null, UserDataFolder, null);

            string prev = Environment.GetEnvironmentVariable(ArgsVar);
            try
            {
                string args = string.IsNullOrEmpty(prev) ? "--lang=ro-RO" : prev + " --lang=ro-RO";
                Environment.SetEnvironmentVariable(ArgsVar, args);
                return CoreWebView2Environment.CreateAsync(null, UserDataFolder + "Ro", null);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ArgsVar, prev);
            }
        }

        // true daca loader-ul exista si WebView2 Runtime e instalat; altfel error = motivul
        public static bool IsRuntimeAvailable(out string error)
        {
            error = null;
            try
            {
                EnsureLoader();
                string v = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (string.IsNullOrEmpty(v))
                {
                    error = "Microsoft Edge WebView2 Runtime nu este instalat.";
                    return false;
                }
                return true;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                error = "Microsoft Edge WebView2 Runtime nu este instalat.";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
