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

        // Sectiunea apare in formular DOAR cand campul "depends_on" are valoarea
        // "depends_value" (suporta AND cu "|", ca la campuri). Altfel e ascunsa,
        // iar sectiunile de dedesubt urca. Documentul generat nu e afectat.
        [JsonProperty("depends_on")]
        public string DependsOn { get; set; }

        [JsonProperty("depends_value")]
        public string DependsValue { get; set; }

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
        //   date          — DateTimePicker (doar data)
        //   datetime      — DateTimePicker (data + ora, format dd.MM.yyyy HH:mm)
        //   readonly      — TextBox readonly (ex. CodInregistrare, NrCim)
        //   person_picker — buton + campuri autocomplete din PersonPickerDialog
        //   dynamic_list  — sectiune cu add/delete rows (membri, echipamente etc)
        //   combo         — ComboBox cu valori fixe (options)
        //   number        — NumericUpDown (0–999)
        //   clauze_editor — clauze Act Aditional (ClauzeEditorDialog)
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

        // ── Editabilitate conditionata ────────────────────────
        // Campul devine needitabil (readonly/disabled) daca valoarea
        // curenta a campului "depends_on" nu e exact "depends_value".
        // Ex: { "depends_on": "TipCerere", "depends_value": "Învoire" }
        [JsonProperty("depends_on")]
        public string DependsOn { get; set; }

        [JsonProperty("depends_value")]
        public string DependsValue { get; set; }

        // ── Pentru type: person_picker ────────────────────────
        // Ce campuri se autocompleteaza la selectia persoanei.
        // Cheia = key-ul campului din formular, valoarea = proprietatea din PersonInfo
        // Ex: { "NumeAutorReferat": "NumeComplet", "FunctieAutorReferat": "Functie" }
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }

        // ── Pentru type: combo ────────────────────────────────
        [JsonProperty("options")]
        public List<string> Options { get; set; }  // valori fixe

        // ── Pentru type: dynamic_list ─────────────────────────
        [JsonProperty("initial_rows")]
        public int InitialRows { get; set; }

        [JsonProperty("item_fields")]
        public List<ItemFieldDefinition> ItemFields { get; set; }

        // Randurile listei se pot restrange/extinde (click pe antet). Cand se adauga
        // un rand nou, celelalte se restrang automat.
        [JsonProperty("collapsible")]
        public bool Collapsible { get; set; }

        // Cheile (item_fields) afisate in antetul unui rand restrans
        [JsonProperty("summary_fields")]
        public List<string> SummaryFields { get; set; }

        // Panoul listei isi ajusteaza inaltimea dupa continut (pana la max_height)
        [JsonProperty("auto_height")]
        public bool AutoHeight { get; set; }

        [JsonProperty("max_height")]
        public int MaxHeight { get; set; }

        // Nu mai afiseaza antetul propriu al listei (titlul sectiunii e deja deasupra)
        [JsonProperty("hide_header")]
        public bool HideHeader { get; set; }

        // Textul butonului de adaugare (implicit "Adaugă " + label)
        [JsonProperty("add_button_label")]
        public string AddButtonLabel { get; set; }

        // "dialog": fiecare element se adauga / editeaza intr-o fereastra separata
        // (ListItemDialog); in formular apar doar carduri needitabile (restranse,
        // cu Editeaza / Sterge). Lista porneste goala. Implicit (absent): randurile
        // se editeaza direct in lista, ca inainte.
        [JsonProperty("edit_mode")]
        public string EditMode { get; set; }

        // Titlul ferestrei de editare (pentru edit_mode "dialog"); implicit = label
        [JsonProperty("dialog_title")]
        public string DialogTitle { get; set; }

        // Elemente precompletate la deschiderea formularului (inlocuiesc initial_rows).
        // Fiecare element: cheie item_field -> valoare; valorile pot contine
        // token-uri cu datele angajatului: {Functie}, {NrCim}, {DataCim}, {NumeSalariat},
        // {CNP}, {NumeDepartament}, {SerieCI}, {NrCI}, {Domiciliu}, {today}.
        [JsonProperty("initial_items")]
        public List<Dictionary<string, string>> InitialItems { get; set; }

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

        // Tipuri posibile: text, number, person_picker, combo, date, date_range, check_text
        // Daca lipseste din JSON ramane null — getter returneaza "text" ca fallback
        [JsonProperty("type")]
        private string _type;

        [JsonIgnore]
        public string Type
        {
            get { return string.IsNullOrWhiteSpace(_type) ? "text" : _type; }
            set { _type = value; }
        }

        [JsonProperty("width_percent")]
        public int WidthPercent { get; set; }

        [JsonProperty("placeholder")]
        public string Placeholder { get; set; }

        // Pentru person_picker in lista dinamica.
        // Pentru type "date": cheie placeholder -> format .NET de data
        //   ex. { "AnulMutatie": "yyyy", "LunaMutatie": "MM", "ZiMutatie": "dd" }
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }

        // Pentru type "combo": lista de optiuni (prima = implicita)
        [JsonProperty("options")]
        public List<string> Options { get; set; }

        // Valoare implicita (combo: optiunea selectata; text: textul initial)
        [JsonProperty("default")]
        public string Default { get; set; }

        // Pentru type "number": valoare maxima (implicit 9999) si zecimale (implicit 0)
        [JsonProperty("max")]
        public int Max { get; set; }

        [JsonProperty("decimals")]
        public int Decimals { get; set; }

        // Titlu de grup: daca e setat, campul incepe un rand nou, iar deasupra lui apare
        // un mic titlu cu linie separatoare (ex. "MUTAȚIE", "SALARIU"). Valabil pana la
        // urmatorul camp cu "group".
        [JsonProperty("group")]
        public string Group { get; set; }

        // Pentru type "check_text": textul bifei (implicit "Da")
        [JsonProperty("check_label")]
        public string CheckLabel { get; set; }

        // Pentru type "number": format de afisare in document (ex. "#,##0.##" -> 4.325)
        [JsonProperty("format")]
        public string Format { get; set; }

        // Pentru type "combo": la alegerea unei optiuni se completeaza automat alte
        // campuri din ACELASI rand. optiune -> { cheie item_field -> valoare }.
        // Valorile accepta token-urile de la initial_items ({Functie}, {DataCim}...).
        // Combo-urile si datele se seteaza mereu; text/numar/check_text doar daca sunt goale
        // (nu suprascriem ce a scris operatorul).
        [JsonProperty("on_select")]
        public Dictionary<string, Dictionary<string, string>> OnSelect { get; set; }

        public ItemFieldDefinition()
        {
            _type = "text";
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