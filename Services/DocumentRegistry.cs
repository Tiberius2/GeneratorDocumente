using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActAditionalPlugin.Models;
using Newtonsoft.Json;

namespace ActAditionalPlugin.Services
{
    // ══════════════════════════════════════════════════════════
    //  CATEGORY ENTRY — o categorie cu documentele ei
    // ══════════════════════════════════════════════════════════
    public class DocumentCategory
    {
        public string Name { get; set; }
        public List<DocumentDefinition> Documents { get; set; }

        public DocumentCategory()
        {
            Documents = new List<DocumentDefinition>();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  DOCUMENT REGISTRY
    //  Scanează /Templates la startup și construiește catalogul
    //  de documente disponibile, grupate pe categorii.
    //
    //  Structura așteptată pe disk:
    //    {TemplatesRoot}/
    //      {Categorie}/
    //        document.json
    //        document.docx
    // ══════════════════════════════════════════════════════════
    public static class DocumentRegistry
    {
        private static List<DocumentCategory> _categories;
        private static string _templatesRoot;

        // ── Ordinea categoriilor în SelectorDialog ─────────────
        // Ordinea categoriilor corespunde exact numelor de foldere de pe disk
        private static readonly List<string> CategoryOrder = new List<string>
        {
            "Sabloane Acte Aditionale",
            "Sabloane Decizii Suspendare",
            "Sabloane Decizii Incetare",
            "Sabloane Cercetare Disciplinara",
            "Sabloane Procese Verbale"
        };

        // ══════════════════════════════════════════════════════
        //  Initialize — apelat o singura data la startup
        // ══════════════════════════════════════════════════════
        public static void Initialize(string templatesRootPath)
        {
            _templatesRoot = templatesRootPath;
            _categories = new List<DocumentCategory>();
            Reload();
        }

        // ══════════════════════════════════════════════════════
        //  Reload — rescaneaza folderele (util pentru hot-reload)
        // ══════════════════════════════════════════════════════
        public static void Reload()
        {
            _categories.Clear();

            if (!Directory.Exists(_templatesRoot))
            {
                throw new DirectoryNotFoundException(
                    "Folderul Templates nu exista: " + _templatesRoot);
            }

            // Scaneaza fiecare subfolder = categorie
            var categoryFolders = Directory.GetDirectories(_templatesRoot);

            foreach (var folder in categoryFolders)
            {
                string categoryName = Path.GetFileName(folder);
                var category = new DocumentCategory { Name = categoryName };

                // Gaseste toate fisierele JSON din folder
                var jsonFiles = Directory.GetFiles(folder, "*.json");

                foreach (var jsonFile in jsonFiles)
                {
                    var def = LoadDefinition(jsonFile, categoryName, folder);
                    if (def != null)
                        category.Documents.Add(def);
                }

                // Sorteaza documentele alfabetic in categorie
                category.Documents = category.Documents
                    .OrderBy(d => d.Title)
                    .ToList();

                if (category.Documents.Count > 0)
                    _categories.Add(category);
            }

            // Sorteaza categoriile dupa ordinea predefinita
            _categories = _categories
                .OrderBy(c =>
                {
                    int idx = CategoryOrder.IndexOf(c.Name);
                    return idx >= 0 ? idx : 999;
                })
                .ToList();
        }

        // ══════════════════════════════════════════════════════
        //  Accessori
        // ══════════════════════════════════════════════════════
        public static List<DocumentCategory> GetCategories()
        {
            return _categories ?? new List<DocumentCategory>();
        }

        public static List<DocumentDefinition> GetAll()
        {
            return (_categories ?? new List<DocumentCategory>())
                .SelectMany(c => c.Documents)
                .ToList();
        }

        public static DocumentDefinition Find(string title)
        {
            return GetAll().FirstOrDefault(d =>
                string.Equals(d.Title, title, StringComparison.OrdinalIgnoreCase));
        }

        // ══════════════════════════════════════════════════════
        //  Load definition din JSON
        // ══════════════════════════════════════════════════════
        private static DocumentDefinition LoadDefinition(
            string jsonPath, string categoryName, string folder)
        {
            try
            {
                string json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                var def = JsonConvert.DeserializeObject<DocumentDefinition>(json);

                if (def == null)
                {
                    Log("JSON invalid (null dupa deserializare): " + jsonPath);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(def.Title))
                {
                    Log("JSON fara 'title': " + jsonPath);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(def.TemplateFile))
                {
                    Log("JSON fara 'template_file': " + jsonPath);
                    return null;
                }

                // Rezolva calea catre DOCX
                string docxPath = Path.Combine(folder, def.TemplateFile);
                if (!File.Exists(docxPath))
                {
                    Log(string.Format("Template DOCX lipsa '{0}' referit din '{1}'",
                        def.TemplateFile, jsonPath));
                    return null;
                }

                def.JsonPath = jsonPath;
                def.TemplatePath = docxPath;
                def.Category = categoryName;

                return def;
            }
            catch (Exception ex)
            {
                Log(string.Format("Eroare la citirea '{0}': {1}", jsonPath, ex.Message));
                return null;
            }
        }

        // ══════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════
        public static string GetTemplatesRoot() => _templatesRoot;

        private static void Log(string msg)
        {
            // In productie logam in event log sau fisier
            // Deocamdata doar in Debug Output
            System.Diagnostics.Debug.WriteLine("[DocumentRegistry] " + msg);
        }
    }
}