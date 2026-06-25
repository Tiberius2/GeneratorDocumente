using System.Collections.Generic;
using Newtonsoft.Json;

namespace ActAditionalPlugin.Models
{
    // ══════════════════════════════════════════════════════════
    //  DOCUMENT DEFINITION
    //  Reprezinta continutul unui fisier .json din /Templates
    // ══════════════════════════════════════════════════════════
    public class DocumentDefinition
    {
        // ── Identitate ────────────────────────────────────────
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }  // preluat din numele folderului

        [JsonProperty("template_file")]
        public string TemplateFile { get; set; }  // ex. "template_pv_cercetare.docx"

        // Ordinea cardului in cadrul categoriei (in SelectorDialog).
        // Optional: 0/absent = fara preferinta, cade la final, sortat alfabetic.
        // Valorile explicite (1, 2, 3...) sunt afisate primele, in ordine crescatoare.
        [JsonProperty("order")]
        public int Order { get; set; }

        // ── Registratura ──────────────────────────────────────
        [JsonProperty("registratura")]
        public bool Registratura { get; set; }

        [JsonProperty("registratura_date_field")]
        public string RegistraturaDateField { get; set; }  // key-ul campului data

        [JsonProperty("registratura_tip_doc_pk")]
        public int RegistraturaTipDocPk { get; set; }

        // ── Sectiuni formular ─────────────────────────────────
        [JsonProperty("sections")]
        public List<SectionDefinition> Sections { get; set; }

        // ── Hooks ─────────────────────────────────────────────
        [JsonProperty("hooks")]
        public List<HookDefinition> Hooks { get; set; }

        // ── Calculat la runtime (nu din JSON) ─────────────────
        [JsonIgnore]
        public string JsonPath { get; set; }     // calea completa a fisierului JSON

        [JsonIgnore]
        public string TemplatePath { get; set; } // calea completa a fisierului DOCX

        public DocumentDefinition()
        {
            Sections = new List<SectionDefinition>();
            Hooks = new List<HookDefinition>();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SECTION DEFINITION
    // ══════════════════════════════════════════════════════════
    public class SectionDefinition
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        // Inaltime explicita in pixeli (optional).
        // Daca 0 sau absent, se calculeaza automat din campuri.
        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("fields")]
        public List<FieldDefinition> Fields { get; set; }

        public SectionDefinition()
        {
            Fields = new List<FieldDefinition>();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FIELD DEFINITION
    // ══════════════════════════════════════════════════════════
    public class FieldDefinition
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        // Tipuri posibile:
        //   text          — TextBox simplu
        //   multiline     — TextBox multiline
        //   date          — DateTimePicker
        //   readonly      — TextBox readonly (ex. CodInregistrare, NrCim)
        //   person_picker — buton + campuri autocomplete din PersonPickerDialog
        //   dynamic_list  — sectiune cu add/delete rows (membri, echipamente etc)
        //   combo         — ComboBox cu valori fixe sau din SQL
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("label_width_percent")]
        public int LabelWidthPercent { get; set; }  // pentru rows cu mai multe campuri

        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("default")]
        public string Default { get; set; }

        [JsonProperty("placeholder")]
        public string Placeholder { get; set; }

        // ── Pentru type: person_picker ────────────────────────
        // Ce campuri se autocompleteaza la selectia persoanei.
        // Cheia = key-ul campului din formular, valoarea = proprietatea din PersonInfo
        // Ex: { "NumeAutorReferat": "NumeComplet", "FunctieAutorReferat": "Functie" }
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }

        // ── Pentru type: combo ────────────────────────────────
        [JsonProperty("options")]
        public List<string> Options { get; set; }  // valori fixe

        [JsonProperty("options_sql")]
        public string OptionsSql { get; set; }     // sau din SQL

        // ── Pentru type: dynamic_list ─────────────────────────
        [JsonProperty("initial_rows")]
        public int InitialRows { get; set; }

        [JsonProperty("item_fields")]
        public List<ItemFieldDefinition> ItemFields { get; set; }

        // ── Pentru expand_table_row (Act Aditional) ───────────
        // Marker-ul din template care identifica randul de tabel de expandat
        [JsonProperty("table_marker")]
        public string TableMarker { get; set; }

        public FieldDefinition()
        {
            Maps = new Dictionary<string, string>();
            Options = new List<string>();
            ItemFields = new List<ItemFieldDefinition>();
            InitialRows = 1;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  ITEM FIELD DEFINITION (campuri dintr-un rand de lista dinamica)
    // ══════════════════════════════════════════════════════════
    public class ItemFieldDefinition
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        // Tipuri posibile: text, number, person_picker
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("width_percent")]
        public int WidthPercent { get; set; }

        [JsonProperty("placeholder")]
        public string Placeholder { get; set; }

        // Pentru person_picker in lista dinamica
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }

        public ItemFieldDefinition()
        {
            Type = "text";
            Maps = new Dictionary<string, string>();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  HOOK DEFINITION
    // ══════════════════════════════════════════════════════════
    public class HookDefinition
    {
        // Cand ruleaza hook-ul: "on_open", "on_generate"
        [JsonProperty("on")]
        public string On { get; set; }

        // Numele handler-ului inregistrat in HookRegistry
        // Ex: "InjectArticoleFinal", "SqlQuery"
        [JsonProperty("handler")]
        public string Handler { get; set; }

        // Parametri optionali specifici handler-ului
        // Ex pentru SqlQuery: { "query": "SELECT...", "target_field": "CodCor" }
        [JsonProperty("params")]
        public Dictionary<string, string> Params { get; set; }

        public HookDefinition()
        {
            Params = new Dictionary<string, string>();
        }
    }
}