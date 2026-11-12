using System.Collections.Generic;
using Newtonsoft.Json;

namespace ActAditionalPlugin.Models
{
    // ══════════════════════════════════════════════════════════
    //  DOCUMENT DEFINITION — un fisier .json din folderul de sabloane.
    //
    //  Aici sunt DOAR proprietatile folosite de C# (registratura, generarea
    //  documentului, hooks). Formularul (pagina web) primeste JSON-ul brut si
    //  citeste singur restul: label, required, options, depends_on, style,
    //  edit_mode, initial_items, on_select etc. — vezi GHID_ADAUGARE_DOCUMENT_NOU.md.
    // ══════════════════════════════════════════════════════════
    public class DocumentDefinition
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonIgnore]
        public string Category { get; set; }      // = numele folderului

        [JsonProperty("template_file")]
        public string TemplateFile { get; set; }

        // Ordinea cardului in categorie (>0 primele, crescator; restul alfabetic)
        [JsonProperty("order")]
        public int Order { get; set; }

        // Registratura: documentul se inregistreaza daca are tip de document
        // (registratura_tip_doc_pk > 0). "registratura": true e acceptat in
        // continuare, dar nu mai e necesar.
        [JsonProperty("registratura")]
        public bool Registratura { get; set; }

        [JsonProperty("registratura_date_field")]
        public string RegistraturaDateField { get; set; }

        [JsonProperty("registratura_tip_doc_pk")]
        public int RegistraturaTipDocPk { get; set; }

        [JsonProperty("sections")]
        public List<SectionDefinition> Sections { get; set; }

        [JsonProperty("hooks")]
        public List<HookDefinition> Hooks { get; set; }

        [JsonIgnore]
        public string JsonPath { get; set; }      // calea fisierului JSON

        [JsonIgnore]
        public string TemplatePath { get; set; }  // calea fisierului DOCX

        public bool UsesRegistratura
        {
            get { return Registratura || RegistraturaTipDocPk > 0; }
        }

        public DocumentDefinition()
        {
            Sections = new List<SectionDefinition>();
            Hooks = new List<HookDefinition>();
        }
    }

    public class SectionDefinition
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("fields")]
        public List<FieldDefinition> Fields { get; set; }

        public SectionDefinition()
        {
            Fields = new List<FieldDefinition>();
        }
    }

    public class FieldDefinition
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        // text, multiline, date, datetime, readonly, number, combo,
        // person_picker, dynamic_list, clauze_editor
        [JsonProperty("type")]
        public string Type { get; set; }

        // person_picker: cheie tinta -> proprietatea persoanei (ex. "NumeComplet:upper")
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }

        // dynamic_list: campurile unui element
        [JsonProperty("item_fields")]
        public List<ItemFieldDefinition> ItemFields { get; set; }

        public FieldDefinition()
        {
            Maps = new Dictionary<string, string>();
            ItemFields = new List<ItemFieldDefinition>();
        }
    }

    // Camp dintr-un element de lista (dynamic_list)
    public class ItemFieldDefinition
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        // text (implicit), number, person_picker, combo, date, date_range, check_text
        [JsonProperty("type")]
        private string _type;

        [JsonIgnore]
        public string Type
        {
            get { return string.IsNullOrWhiteSpace(_type) ? "text" : _type; }
            set { _type = value; }
        }

        // person_picker: ca la FieldDefinition.Maps
        // date: cheie placeholder -> format .NET (ex. { "AnulMutatie": "yyyy" })
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }

        // number: zecimale si format in document (ex. "#,##0.##", cultura ro-RO)
        [JsonProperty("decimals")]
        public int Decimals { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        public ItemFieldDefinition()
        {
            _type = "text";
            Maps = new Dictionary<string, string>();
        }
    }

    public class HookDefinition
    {
        // on_open, on_change, on_generate
        [JsonProperty("on")]
        public string On { get; set; }

        // numele handler-ului din HookRegistry
        [JsonProperty("handler")]
        public string Handler { get; set; }

        [JsonProperty("params")]
        public Dictionary<string, string> Params { get; set; }

        public HookDefinition()
        {
            Params = new Dictionary<string, string>();
        }
    }
}
