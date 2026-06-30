# ActAditionalPlugin — Ghid Arhitectural
*Actualizat: 25 Iunie 2026*

---

## 1. Structura proiectului

```
ActAditionalPlugin/
├── Models/
│   ├── DocumentDefinition.cs   — modele JSON (def doc, sectiuni, campuri, hookuri)
│   ├── PersonInfo.cs           — DTO persoana selectata din picker
│   ├── PluginConfig.cs         — constante hardcodate fallback (companie)
│   └── ClauzeConfig.cs         — model JSON pentru clauze Act Aditional
├── Services/
│   ├── DocumentRegistry.cs     — incarca toate JSON-urile din /Templates
│   ├── DynamicTemplateEngine.cs — umple DOCX + converteste la PDF
│   ├── HookRegistry.cs         — handlere hook-uri
│   ├── ErpDataProvider.cs      — SQL angajat + companie din ERP
│   ├── RegistraturaService.cs  — INSERT CCCVREGISTRATURA + tabele specifice
│   ├── ClauzeService.cs        — citire/scriere clauze din JSON
│   ├── WordHelper.cs           — utilitare OpenXML
│   └── BulkContext.cs          — bridge XSupport static
├── UI/
│   ├── DynamicForm.cs          — formularul principal (2280+ linii)
│   ├── SelectorDialog.cs       — selectie document + angajat
│   ├── PersonPickerDialog.cs   — picker angajati cu search
│   ├── ClauzeEditorDialog.cs   — editor doua-nivel clauze
│   ├── PunctModificareControl.cs — control clauza individuala (resize)
│   ├── ConfirmareDialog.cs     — dialog confirmare (returneaza DialogResult.OK)
│   ├── SuccessDialog.cs        — dialog succes
│   ├── DocumentTheme.cs        — culori per categorie
│   └── PriorityScrollPanel.cs  — panel cu WM_MOUSEWHEEL interceptat
└── Templates/                  — (extern, langa DLL)
    ├── Acte Aditionale/
    │   ├── act_aditional.json
    │   └── template_act_aditional.docx
    ├── Suspendare/
    ├── Incetare/
    ├── Procese Verbale/
    └── Cercetare Disciplinara/
```

---

## 2. Modele JSON (DocumentDefinition)

### 2.1 Structura unui fisier `.json`

```json
{
  "title": "Titlul documentului",
  "category": "Acte Aditionale",
  "template_file": "template_xxx.docx",
  "order": 1,
  "registratura": true,
  "registratura_date_field": "DataActului",
  "registratura_tip_doc_pk": 42,
  "sections": [ ... ],
  "hooks": [ ... ]
}
```

**Campuri cheie:**
- `order` — pozitia cardului in SelectorDialog (0 = fara preferinta, sortat alfabetic la final)
- `registratura` — daca se face INSERT in CCCVREGISTRATURA la generare
- `registratura_date_field` — key-ul campului data folosit pentru data inregistrarii
- `registratura_tip_doc_pk` — PK din CCCTIPDOCREG

### 2.2 Tipuri de campuri (`type`)

| Tip | Control | Note |
|-----|---------|------|
| `text` | TextBox | triggheruieste `on_change` daca e referit in params hook |
| `multiline` | TextBox multiline | inaltime `MultilineHeight = 140px` |
| `date` | DateTimePicker | triggheruieste `on_change` automat |
| `readonly` | TextBox readonly | ex. `CodInregistrare`, `NrCim` |
| `person_picker` | Button + autocomplete | foloseste `maps` pentru populare campuri |
| `dynamic_list` | Randuri add/delete | foloseste `item_fields` + `initial_rows` |
| `combo` | ComboBox | `options` (fix) sau `options_sql` |
| `expand_table_row` | — | expandeaza rand in tabel DOCX, necesita `table_marker` |
| `clauze_editor` | Buton deschide ClauzeEditorDialog | format dat identic cu `dynamic_list` |

### 2.3 `person_picker` — campul `maps`

```json
{
  "key": "NumeAutor",
  "type": "person_picker",
  "maps": {
    "NumeAutor": "NumeComplet",
    "FunctieAutor": "Functie",
    "CNPAutor": "CNP"
  }
}
```

Cheile din `maps` = key-uri de campuri din formular; valorile = proprietati din `PersonInfo`.

Proprietati disponibile in `PersonInfo`: `NumeComplet`, `Nume`, `Prenume`, `CNP`, `Functie`, `CodCor`, `NrCim`, `DataCim`, `NumeDepartament`.

### 2.4 `dynamic_list` — `item_fields`

```json
{
  "key": "Membri",
  "type": "dynamic_list",
  "initial_rows": 3,
  "item_fields": [
    {
      "key": "NumeMembruComisie",
      "label": "Nume si prenume",
      "type": "person_picker",
      "width_percent": 50,
      "maps": {
        "NumeMembruComisie": "NumeComplet",
        "FunctieMembruComisie": "Functie"
      }
    },
    {
      "key": "FunctieMembruComisie",
      "label": "Functia",
      "width_percent": 50
    }
  ]
}
```

**IMPORTANT:** `item_key` in hookul `ConcatList` trebuie sa fie identic cu `key`-ul din `item_fields`, nu cu `key`-ul campului `dynamic_list`.

---

## 3. HookRegistry — handlere disponibile

### 3.1 Wiring hooks in DynamicForm

| Eveniment `on` | Cand ruleaza |
|----------------|-------------|
| `on_open` | La deschiderea formularului (dupa build UI) |
| `on_change` | La schimbarea oricarui `date`, `number`, sau `text` referit in params |
| `on_generate` | La Preview si la Generare PDF finala |

**Guard re-intrare:** `_hooksRunning` boolean previne `Collection was modified` la hook-uri care modifica form values.

### 3.2 Handlere built-in

#### `SetDefault`
Seteaza valoarea unui camp la deschidere daca e gol/zero.
```json
{ "on": "on_open", "handler": "SetDefault",
  "params": { "field": "AniSuspendare", "value": "2" } }
```

#### `CalcDataSfarsit`
`DataEnd = DataStart + LuniSuspendare luni`
```json
{ "on": "on_change", "handler": "CalcDataSfarsit",
  "params": { "start_field": "DataStartSuspendare",
              "luni_field": "LuniSuspendare",
              "target_field": "DataEndSuspendare" } }
```

#### `CalcDataSfarsitDinCNP`
`DataEnd = DataNastereDinCNP + AniSuspendare ani`
```json
{ "on": "on_change", "handler": "CalcDataSfarsitDinCNP",
  "params": { "cnp_field": "CNPCopil",
              "ani_field": "AniSuspendare",
              "target_field": "DataEndSuspendare" } }
```
Triggheruieste automat la modificarea campului `text` `CNPCopil` (DynamicForm detecteaza referinta in params).

#### `CalcPerioadaSuspendare`
Converteste numar ani in text: `2 → "2 ani"`, `1 → "1 an"`
```json
{ "on": "on_generate", "handler": "CalcPerioadaSuspendare",
  "params": { "ani_field": "AniSuspendare",
              "target_field": "PerioadaSuspendare" } }
```

#### `ConcatList`
Concateneaza o lista dinamica intr-un singur placeholder.
```json
{ "on": "on_generate", "handler": "ConcatList",
  "params": { "source_field": "Referate",
              "item_key": "CodSiData",
              "target_field": "ReferateSursa",
              "separator": ", " } }
```
**ATENTIE:** `item_key` = key-ul din `item_fields`, nu din campul `dynamic_list`.

#### `InjectArticoleFinal`
Calculeaza ultimul `Art.N` din document si seteaza `ArticolCompartiment` + `ArticolContestatie`.
```json
{ "on": "on_generate", "handler": "InjectArticoleFinal" }
```

#### `SqlOnOpen` / `SqlOnGenerate`
Executa SQL si pune rezultatul intr-un camp.
```json
{ "on": "on_open", "handler": "SqlOnOpen",
  "params": { "query": "SELECT TOP 1 S.CODE FROM ... WHERE PJ.PRSN={PrsnId}",
              "column": "CodCor",
              "target_field": "CodCor" } }
```
Placeholdere SQL disponibile: `{PrsnId}`, `{CompanyId}`.

### 3.3 Adaugarea unui handler nou

```csharp
// In HookRegistry.cs — RegisterAll():
_handlers["NumeHandler"] = NumeHandler;

// Metoda:
private static void NumeHandler(HookContext ctx)
{
    string param;
    if (!ctx.Params.TryGetValue("cheie_param", out param)) return;
    ctx.FormValues["CampTarget"] = valoare_calculata;
}
```

---

## 4. DynamicTemplateEngine — flux generare

```
GeneratePdf() / GeneratePreviewDocx()
    └── FillTemplate(tempDocx)
            ├── ExpandDynamicLists()   — cloneaza paragrafe/randuri per item
            ├── ExpandTableRows()      — expand_table_row cu table_marker
            ├── BuildPlaceholderMap()  — combina Common + FormValues
            └── MergeAndReplace()      — replace {{placeholder}} in fiecare paragraf
```

### 4.1 Reguli template DOCX

- Fiecare placeholder expandabil (`dynamic_list`, `expand_table_row`) trebuie sa fie **pe propriul paragraf/rand** in DOCX
- Placeholderele simple pot fi oriunde: `{{NumeSalariat}}`, `{{DataActului}}` etc.
- Listele dinamice se expandeaza **inainte** de replace-ul simplu

### 4.2 Placeholder-e comune disponibile automat

```
{{NumeSalariat}}, {{CNP}}, {{Functie}}, {{NumeDepartament}}
{{NrCim}}, {{DataCim}}, {{CodInregistrare}}
{{NumeAngajator}}, {{CIFAngajator}}, {{ReprezentantLegal}}, {{FunctieReprezentant}}
{{AdresaCompanie}}, {{ZipCompanie}}, {{NrRegComertului}}
{{IbanCompanie}}, {{NrTelefonCompanie}}, {{EmailCompanie}}, {{WebsiteCompanie}}
{{MentiuniDocument}}
{{ArticolCompartiment}}, {{ArticolContestatie}}  ← populate de InjectArticoleFinal
```

---

## 5. RegistraturaService — inregistrare documente

### 5.1 Flux

```
Genereaza PDF
    → Inregistreaza(codInreg, data, tipDocPK, titlu, prsnId)
          → INSERT CCCVREGISTRATURA + INSERT CCCVDOCAUDIT
    → InregistreazaTabelaSpecifica(category, ...)
          → acte/aditional → INSERT CCCACTEADITIONALE
          → suspendare/incetare → INSERT CCCDCZCONTRACT
          → verbale/procese → INSERT CCCPVEMISE
          → cercetare disciplinara → doar registratura (fara tabela specifica)
```

### 5.2 Format cod: `YYddd/NR`

- `YY` = ultimele 2 cifre ale anului
- `ddd` = ziua din an (001-366)
- `NR` = urmatorul numar secvential per prefix + company

### 5.3 Tabele specifice per categorie

| Categorie (lower contains) | Tabela |
|---------------------------|--------|
| `acte`, `aditional` | `CCCACTEADITIONALE` |
| `suspendare`, `incetare` | `CCCDCZCONTRACT` |
| `verbale`, `procese` | `CCCPVEMISE` |
| `disciplinar`, `cercetare` | — (doar registratura) |

---

## 6. ErpDataProvider

### 6.1 `GetCimData(prsnId, xSupport)`
- Sursa: `PRSEXTRA.NUM03` (NrCim), `PRSEXTRA.DATE03` (DataCim)
- JOIN `DEPART` pentru `NumeDepartament`
- JOIN `PRSJOBPOS → JOBPOSITION → SPECIALTY` pentru `CodCor`

### 6.2 `GetCompanyData(xSupport)`
- Sursa: `COMPANY` JOIN `COMPANYEXT` (ACCTOFFICE=2) + `SPECIALTY` pentru functie reprezentant
- **Fallback la `PluginConfig`** pentru orice camp gol returnat de SQL

---

## 7. DynamicForm — arhitectura UI

### 7.1 Layout principal

```
DynamicForm (Form)
└── SplitContainer (Horizontal, Dock=Fill)
    ├── Panel1 (sus) — SplitContainer Vertical
    │   ├── Panel1 (stanga) — SplitContainer Horizontal intern
    │   │   ├── Panel1 (sus) — _pnlBody (PriorityScrollPanel, AutoScroll)
    │   │   │     ├── BuildTitluSection()     — titlu doc + CodInregistrare
    │   │   │     ├── BuildAngajatInBody()    — date angajat readonly
    │   │   │     └── BuildSection() × N      — sectiunile din JSON
    │   │   └── Panel2 (jos) — _pnlMentiuniWrapper
    │   │         └── _pnlMentiuni (CheckBox + TextBox, Dock=Fill)
    │   └── Panel2 (dreapta) — PDF viewer (PdfiumViewer)
    └── Panel2 (jos) — Footer (Anulare | Previzualizeaza | Genereaza PDF)
```

**Scroll blocat la footer:** `innerSplit` (SplitContainer Horizontal) izoleaza `_pnlBody` de footer — scrollbar-ul nu depaseste footer-ul.

### 7.2 PriorityScrollPanel
Custom `Panel` care intercepteaza `WM_MOUSEWHEEL` si prioritizeaza scroll-ul pe el in loc de Form.

### 7.3 Sectiuni si campuri

```csharp
BuildSection(section, ref y)
    → AddSectiuneHeader()           // titlu sectiune violet
    → BuildFieldsInPanel(fields)
         → BuildFieldControl(field) // returneaza Control per tip
         → BuildDynamicListField()  // pentru dynamic_list
         → BuildClauzeEditorSection() // pentru clauze_editor
```

### 7.4 Dynamic list rows (`DynamicListRow`)

Clasa nested in `DynamicForm`. Fiecare rand:
- `TableLayoutPanel` cu `ColumnCount = item_fields.Count + 1` (ultima coloana = buton X)
- Per celula: `Panel` cu `Dock=Fill`, `Padding=(0,4,6,4)`
  - `Label` cu `Dock=Top`, `Height=16` (adaugat al doilea → apare sus)
  - `ctrl` cu `Dock=Top`, `Height=26` (adaugat primul → apare jos)
- `rowH = 50` (label 16 + ctrl 26 + padding 8)

### 7.5 Teme colori (DocumentTheme)

| Categorie | Culoare accent |
|-----------|----------------|
| Acte Aditionale | Albastru `RGB(63,129,198)` |
| Suspendare | Teal |
| Incetare | Rose |
| Procese Verbale | Amber |
| Cercetare Disciplinara | Violet `RGB(120,60,170)` |

---

## 8. PersonInfo — proprietati disponibile in `maps`

```
PrsnId          — ID ERP
NumeComplet     — NAME + ' ' + NAME2
Nume            — NAME
Prenume         — NAME2
CNP             — AFM
Functie         — SOTITLENAME
CodCor          — SPECIALTY.CODE
NrCim           — PRSEXTRA.CCCVARCHAR05
DataCim         — PRSEXTRA.DATE03 (DataCimFormatata = dd.MM.yyyy)
NumeDepartament — DEPART.NAME
```

---

## 9. Adaugarea unui document nou (zero cod)

1. **Creeaza** `Templates/NumeCategorie/titlu_doc.json`
2. **Creeaza** `Templates/NumeCategorie/template_titlu_doc.docx` cu placeholdere `{{CheieField}}`
3. In JSON, defineste `sections` cu campurile necesare
4. Adauga `hooks` daca e nevoie de calcule automate
5. Seteaza `registratura: true` + `registratura_tip_doc_pk` + `registratura_date_field`
6. **Recompilare NU e necesara** — `DocumentRegistry` incarca automat la startup

---

## 10. Patterns ERP (Softone)

```csharp
// SQL
var ds = xSupport.GetSQLDataSet("SELECT ...");
string val = ds[0, "COLOANA"]?.ToString()?.Trim() ?? string.Empty;

// Execute (INSERT/UPDATE)
xSupport.ExecuteSQL("INSERT INTO ...");

// Company/User
int companyId = xSupport.ConnectionInfo.CompanyId;
int userId    = xSupport.ConnectionInfo.UserId;

// Warning dialog
xSupport.Warning("Mesaj eroare");
```

---

## 11. Probleme cunoscute si fix-uri aplicate

| Problema | Fix |
|----------|-----|
| `ConfirmareDialog` bloca generarea | `DialogResult.OK` (nu `Yes`) |
| `ConcatList` item_key mismatch | Verificat ca `item_key` = key din `item_fields` |
| `CalcDataSfarsit` nu triggherua pe campuri `date` | `dtp.ValueChanged += RunHooks("on_change")` |
| Scroll sidebar blocat sub header | Eliminat `pnlHeader` separat; totul in `_pnlBody` scrollabil cu `innerSplit` |
| Hook re-entrance `Collection was modified` | Guard `_hooksRunning` boolean |
| Titlu sectiuni `dynamic_list` nu aparea | `AddSectiuneHeader` apelat in `BuildSection` |
| `ItemFieldDefinition.Type` null | Backing field `_type` cu getter fallback `"text"` |
| `multiline` turtit la 26px | `AddLabeledInput` + `AddRow(height)` + `FieldControlHeight()` |
| Label-uri campuri `dynamic_list` nu apareau | `Dock=Top` pe label+ctrl, adaugate in ordine inversa |
| Label taie campul | `rowH=50`, label `Height=16`, ctrl `Height=26`, padding `(0,4,6,4)` |
| `GetAncestor<T>` pattern matching | Revert — nu e suportat in .NET 4.7.2 target al Softone |
