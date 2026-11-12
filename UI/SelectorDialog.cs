using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ActAditionalPlugin.Models;
using ActAditionalPlugin.Services;
using ActAditionalPlugin.UI.Web;
using Newtonsoft.Json.Linq;
using Softone;

namespace ActAditionalPlugin.UI
{
    // ══════════════════════════════════════════════════════════
    //  SELECTORUL DE DOCUMENTE (fereastra principala, pagina selector.html)
    //  - alegerea angajatului (selectorul web de persoane) + "Dosar Personal"
    //  - cardurile documentelor, pe categorii (DocumentRegistry)
    //  - documentul ales se deschide PESTE selector (WebDocumentForm, modal);
    //    dupa inchidere selectorul ramane deschis pentru documentul urmator.
    //
    //  Numele clasei si constructorul fara parametri raman: Softone il
    //  deschide si direct ca "Dll Form" (ActAditionalPlugin.dll;SelectorDialog).
    // ══════════════════════════════════════════════════════════
    public sealed class SelectorDialog : WebHostForm
    {
        private readonly XSupport _xs;
        private readonly ErpCompanyData _company;
        private readonly List<PersonInfo> _persoane;
        private readonly List<DocumentDefinition> _docs = new List<DocumentDefinition>();
        private readonly int _prsnId;

        private ErpCimData _cim;
        private int _cimPrsnId;

        // Softone "Dll Form": initializeaza singur serviciile (S1.xSupp)
        public SelectorDialog() : this(S1.xSupp, 0, null, null) { }

        internal SelectorDialog(XSupport xs, int prsnId, ErpCompanyData company, List<PersonInfo> persoane)
            : base("selector.html")
        {
            _xs = xs;

            if (company == null)
            {
                try { company = AppServices.Initialize(xs); }
                catch (Exception ex)
                {
                    MessageBox.Show("Eroare la inițializare:\n" + ex.Message,
                        "Generator Documente HR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    company = new ErpCompanyData();
                }
            }
            _company = company;

            if (persoane == null)
            {
                try { persoane = ErpDataProvider.LoadPersons(xs); }
                catch (Exception ex)
                {
                    MessageBox.Show("Eroare la încărcarea angajaților:\n" + ex.Message,
                        "Generator Documente HR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    persoane = new List<PersonInfo>();
                }
            }
            _persoane = persoane;
            _prsnId = _persoane.Any(p => p.PrsnId == prsnId) ? prsnId : 0;

            Text = "Generator Documente HR";
            Size = new Size(1400, 820);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;

            AddWebView(this);
            Shown += (s, e) => StartWebView();
            // selectorul e ultima fereastra: inchidem si instanta Word (conversia PDF)
            FormClosed += (s, e) => WordHelper.ShutdownWordApp();
        }

        protected override void SendInit()
        {
            _docs.Clear();
            var cats = new JArray();
            foreach (var cat in DocumentRegistry.GetCategories())
            {
                var docs = new JArray();
                foreach (var d in cat.Documents)
                {
                    docs.Add(new JObject { ["id"] = _docs.Count, ["title"] = d.Title ?? "" });
                    _docs.Add(d);
                }
                cats.Add(new JObject
                {
                    ["name"] = cat.Name ?? "",
                    ["theme"] = ThemeJson(DocumentTheme.ForCategory(cat.Name)),
                    ["docs"] = docs
                });
            }

            Post(new JObject
            {
                ["type"] = "init",
                ["categories"] = cats,
                ["persons"] = PersonList.ToJson(_persoane),
                ["prsnId"] = _prsnId
            });
        }

        protected override void HandleMessage(string type, JObject msg)
        {
            switch (type)
            {
                case "close":
                    DialogResult = DialogResult.Cancel;
                    Close();
                    break;
                case "openFolder":
                    OpenFolder(IntOf(msg["prsnId"]));
                    break;
                case "openDocument":
                {
                    int doc = IntOf(msg["doc"]), prsn = IntOf(msg["prsnId"]);
                    BeginInvoke(new Action(() => OpenDocument(doc, prsn)));
                    break;
                }
            }
        }

        private static int IntOf(JToken t)
        {
            int v;
            int.TryParse(Str(t), out v);
            return v;
        }

        // Folderul cu documentele angajatului (cautat dupa PrsnId, creat daca lipseste)
        private void OpenFolder(int prsnId)
        {
            var p = _persoane.FirstOrDefault(x => x.PrsnId == prsnId);
            if (p == null) return;
            try
            {
                string path = DynamicTemplateEngine.ResolvePersonFolder(p.PrsnId, p.NumeComplet);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Nu am putut deschide dosarul angajatului: " + ex.Message,
                    "Generator Documente HR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Deschide formularul documentului peste selector
        private void OpenDocument(int docIdx, int prsnId)
        {
            if (IsDisposed) return;
            bool generated = false;
            try
            {
                var def = docIdx >= 0 && docIdx < _docs.Count ? _docs[docIdx] : null;
                var p = _persoane.FirstOrDefault(x => x.PrsnId == prsnId);
                if (def == null || p == null) return;

                if (_cim == null || _cimPrsnId != p.PrsnId)
                {
                    _cim = ErpDataProvider.GetCimData(p.PrsnId, _xs ?? BulkContext.XSupport);
                    _cimPrsnId = p.PrsnId;
                }

                var common = CommonDocumentValues.FromErp(p.PrsnId, p.NumeComplet, p.CNP, p.Functie, _cim, _company);
                common.CodInregistrare = RegistraturaService.Instance.CalculateCod(
                    RegistraturaService.Instance.GetLoginDate());

                using (var form = new WebDocumentForm(def, common, _persoane))
                    generated = form.ShowDialog(this) == DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Eroare la deschiderea documentului:\n" + ex.Message,
                    "Generator Documente HR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Post(new JObject { ["type"] = "documentClosed", ["generated"] = generated });
            }
        }
    }
}
