using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;

namespace ActAditionalPlugin.UI.Web
{
    // ══════════════════════════════════════════════════════════
    //  WEB HOST FORM — baza ferestrelor aplicatiei (selector, formular).
    //  - WebView2 cu paginile servite din resursele DLL ("web/<fisier>")
    //    la https://app.generator/
    //  - mesaje JSON: pagina -> C# (HandleMessage), C# -> pagina (Post)
    //  - "ready" de la pagina (sau incarcarea paginii) -> SendInit
    //
    //  ATENTIE (Softone): fara async/await si fara struct-uri cu campuri de
    //  tip WebView2 in clasele care folosesc WebView2 — Softone citeste toate
    //  tipurile din DLL (GetTypes) INAINTE ca Costura sa poata despacheta
    //  Microsoft.Web.WebView2.Core ("Unable to load one or more of the
    //  requested types"). Se foloseste ContinueWith.
    //  ATENTIE: Softone are incarcat un Newtonsoft.Json vechi — doar API
    //  sigur (JObject.Parse, JToken.ToString(), conversii explicite).
    // ══════════════════════════════════════════════════════════
    public abstract class WebHostForm : Form
    {
        protected const string AppHost = "https://app.generator/";
        protected static readonly Color PageBack = Color.FromArgb(242, 245, 250);

        private readonly string _page;
        private Control _webParent;
        private bool _initSent;
        private bool _errorShown;

        protected WebView2 Web { get; private set; }

        protected WebHostForm(string page)
        {
            _page = page;
            BackColor = PageBack;
            Font = new Font("Segoe UI", 10f);
            TrySetIcon();
        }

        // Pagina a cerut datele (sau s-a incarcat): trimite mesajul "init"
        protected abstract void SendInit();

        // Mesajele paginii (in afara de ready / ping / jserror)
        protected abstract void HandleMessage(string type, JObject msg);

        // Pune controlul WebView2 in container; pornirea se face cu StartWebView()
        protected void AddWebView(Control parent)
        {
            _webParent = parent;
            Web = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = PageBack };
            parent.Controls.Add(Web);
        }

        protected void StartWebView()
        {
            string err;
            if (!WebViewSupport.IsRuntimeAvailable(out err))
            {
                InitFailed(new Exception(err + "\n\nInstalați „Microsoft Edge WebView2 Runtime” (Evergreen) " +
                    "și verificați că WebView2Loader.dll e lângă ActAditionalPlugin.dll."));
                return;
            }
            StartWebView(true);
        }

        // ro = true: limba romana (--lang=ro-RO), folder de date separat. Daca
        // pornirea esueaza (ex. 0x8007139F — proces WebView2 ramas cu alti
        // parametri), se reia automat FARA limba, pe folderul vechi.
        private void StartWebView(bool ro)
        {
            try
            {
                WebViewSupport.EnsureLoader();
                var ui = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
                WebViewSupport.CreateEnvironment(ro)
                    .ContinueWith(tEnv =>
                    {
                        if (tEnv.IsFaulted) { StartFailed(ro, tEnv.Exception.GetBaseException()); return; }
                        Web.EnsureCoreWebView2Async(tEnv.Result)
                            .ContinueWith(tCore =>
                            {
                                if (tCore.IsFaulted) { StartFailed(ro, tCore.Exception.GetBaseException()); return; }
                                OnWebViewReady();
                            }, ui);
                    }, ui);
            }
            catch (Exception ex)
            {
                StartFailed(ro, ex);
            }
        }

        private void StartFailed(bool ro, Exception ex)
        {
            if (!ro || IsDisposed) { InitFailed(ex); return; }
            // controlul WebView2 nu poate fi reinitializat cu alt mediu -> unul nou
            try
            {
                _webParent.Controls.Remove(Web);
                Web.Dispose();
            }
            catch { }
            Web = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = PageBack };
            _webParent.Controls.Add(Web);
            Web.BringToFront();
            StartWebView(false);
        }

        private void InitFailed(Exception ex)
        {
            MessageBox.Show(this, "Fereastra nu a putut fi inițializată (WebView2):\n" + ex.Message,
                "Generator Documente HR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnWebViewReady()
        {
            try
            {
                var core = Web.CoreWebView2;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsZoomControlEnabled = false;
                core.Settings.AreDevToolsEnabled = true;   // F12 pentru depanare

                core.AddWebResourceRequestedFilter(AppHost + "*", CoreWebView2WebResourceContext.All);
                core.WebResourceRequested += OnWebResourceRequested;
                core.WebMessageReceived += OnWebMessageReceived;
                core.NavigationCompleted += OnNavigationCompleted;

                core.Navigate(AppHost + _page);
            }
            catch (Exception ex)
            {
                InitFailed(ex);
            }
        }

        // Paginile (html / css / js) vin din resursele DLL-ului: "web/<nume fisier>"
        private void OnWebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var core = Web.CoreWebView2;
            string path = new Uri(e.Request.Uri).AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(path)) path = _page;

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("web/" + path);
            if (stream == null)
            {
                e.Response = core.Environment.CreateWebResourceResponse(null, 404, "Not Found", "");
                return;
            }

            var ms = new MemoryStream();
            using (stream) stream.CopyTo(ms);
            ms.Position = 0;
            e.Response = core.Environment.CreateWebResourceResponse(ms, 200, "OK",
                "Content-Type: " + ContentType(path) + "\r\nCache-Control: no-store");
        }

        private static string ContentType(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".html": return "text/html; charset=utf-8";
                case ".css": return "text/css; charset=utf-8";
                case ".js": return "application/javascript; charset=utf-8";
                case ".svg": return "image/svg+xml";
                case ".png": return "image/png";
                default: return "application/octet-stream";
            }
        }

        // Erorile din handler-ele WebView2 pot fi inghitite in tacere —
        // le aratam explicit (fereastra "diagnostic").
        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                JObject msg = JObject.Parse(e.WebMessageAsJson);
                string type = (string)msg["type"];
                switch (type)
                {
                    case "ready": DoSendInit(); break;
                    case "ping": Post(new JObject { ["type"] = "pong", ["at"] = DateTime.Now.ToString("HH:mm:ss") }); break;
                    case "jserror": ShowError("Eroare în pagină:\n" + (string)msg["message"]); break;
                    default: HandleMessage(type, msg); break;
                }
            }
            catch (Exception ex)
            {
                ShowError("Eroare la procesarea mesajului din pagină:\n" + ex);
            }
        }

        // Rezerva: dupa incarcarea paginii trimitem datele chiar daca "ready" nu a ajuns
        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (!e.IsSuccess) { ShowError("Pagina nu s-a încărcat: " + e.WebErrorStatus); return; }
                if (!_initSent) DoSendInit();
            }
            catch (Exception ex)
            {
                ShowError("Eroare după încărcarea paginii:\n" + ex);
            }
        }

        private void DoSendInit()
        {
            SendInit();
            _initSent = true;
        }

        protected void Post(JObject msg)
        {
            // JToken.ToString() exista in toate versiunile Newtonsoft (ToString(Formatting) nu)
            if (Web != null && Web.CoreWebView2 != null)
                Web.CoreWebView2.PostWebMessageAsJson(msg.ToString());
        }

        protected void ShowError(string text)
        {
            if (_errorShown) return;   // o singura fereastra de eroare
            _errorShown = true;
            MessageBox.Show(this, text, "Generator Documente HR — diagnostic",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _errorShown = false;
        }

        protected static string Hex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        protected static JObject ThemeJson(ActAditionalPlugin.Models.DocumentTheme t)
        {
            return new JObject
            {
                ["accent"] = Hex(t.Accent),
                ["accentDark"] = Hex(t.AccentDark),
                ["accentPal"] = Hex(t.AccentPal),
                ["accentBorder"] = Hex(t.AccentBorder)
            };
        }

        protected static string Str(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined) return string.Empty;
            var v = t as JValue;
            return v != null
                ? Convert.ToString(v.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
                : t.ToString();
        }

        // Iconita ferestrei: cea a aplicatiei gazda (Softone)
        private void TrySetIcon()
        {
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }
        }
    }
}
