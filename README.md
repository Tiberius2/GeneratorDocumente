# ActAditionalPlugin — Generator Documente HR

Plugin .NET 4.7.2 WinForms integrat în **Softone ERP** pentru generarea documentelor HR (acte adiționale, decizii, procese verbale, cercetare disciplinară) din șabloane **JSON + DOCX**, fără recompilare.

---

## Cuprins

1. [Arhitectură generală](#arhitectură-generală)
2. [Structura proiectului](#structura-proiectului)
3. [Configurare și deployment](#configurare-și-deployment)
4. [Cum funcționează fluxul](#cum-funcționează-fluxul)
5. [Modele de date](#modele-de-date)
6. [Servicii](#servicii)
7. [UI](#ui)
8. [Entry point Softone](#entry-point-softone)
9. [Schema JSON — referință completă](#schema-json--referință-completă)
10. [Hookuri disponibile](#hookuri-disponibile)
11. [Placeholder-e comune automate](#placeholder-e-comune-automate)
12. [Adăugarea unui document nou](#adăugarea-unui-document-nou)
13. [Patterns Softone](#patterns-softone)
14. [Greșeli frecvente](#greșeli-frecvente)

---

## Arhitectură generală

```
Softone ERP
    │
    ├─ TXCode [WorksOn("GENERAL")]  ← S1 (populează XSupport static)
    ├─ TXCode [WorksOn("GENERAL")]  ← ProgramGeneral (cmd 4000502, independent de ecran)
    └─ TXCode [WorksOn("PRSNIN")]   ← Program (cmd 4000501, flux cu angajat din ecran)
                │
                ▼
        SelectorDialog
        ┌─────────────────────────────┐
        │  Selectează angajat         │
        │  Selectează tip document    │
        └─────────────┬───────────────┘
                      │
                      ▼
              DynamicForm
        ┌─────────────────────────────┐
        │  Câmpuri generate din JSON  │
        │  Hookuri on_open            │
        │  Preview PDF (PdfiumViewer) │
        │  Hookuri on_generate        │
        └─────────────┬───────────────┘
                      │
					  ▼
             DynamicTemplateEngine
        ┌─────────────────────────────┐
        │  Completează DOCX template  │
        │  Expandează liste dinamice  │
        │  Convertește la PDF         │
        └─────────────┬───────────────┘
                      │
					  ▼
              RegistraturaService
        ┌─────────────────────────────┐
        │  INSERT CCCVREGISTRATURA    │
        │  INSERT tabele specifice    │
        └─────────────────────────────┘
```

**Principiu cheie:** Adăugarea unui document nou nu necesită modificarea codului C# — doar un fișier `.json` + un fișier `.docx` în folderul corect.
** ADAUGAREA UNUI SABLON NOU : Este disponibil un ghid tehnic care explica in mare ce trebuie sa contina jsonul. 
** Indicat ar fi ca prima data sa se construiasca docxul generic si jsonul sa fie construit in functie de docx. (mai multe in ghidul tehnic de lucru)

---

## Structura proiectului

```
ActAditionalPlugin/
├── PluginEntry.cs              # Entry point Softone (S1, ProgramGeneral, Program)
├── Models/
│   ├── DocumentDefinition.cs  # Schema JSON deserializată
│   ├── PersonInfo.cs          # DTO persoană (din picker sau ERP)
│   ├── PluginConfig.cs        # Configurare statică (env vars, date companie)
│   └── ClauzeConfig.cs        # Config specific Act Adițional (clauze)
├── Services/
│   ├── DocumentRegistry.cs    # Scanează /Templates și construiește catalogul
│   ├── DynamicTemplateEngine.cs # Completează DOCX + convertește PDF
│   ├── ErpDataProvider.cs     # SQL Softone: angajat, companie, CIM
│   ├── HookRegistry.cs        # Mapare nume hook → handler
│   ├── RegistraturaService.cs # Înregistrare în CCCVREGISTRATURA
│   ├── BulkContext.cs         # Bridge static XSupport/CompanyData între thread-uri // asta este pentru o implementare in viitor (nedecisa)
│   ├── ClauzeService.cs       # Logic specific clauze Act Adițional
│   └── WordHelper.cs          # Utilitare Open XML // ugly stuff, pentru o convertire corecta din template in document copletat -- m-am luat dupa un alt proiect + AI
└── UI/
    ├── SelectorDialog.cs      # Fereastra principală selector document + angajat
    ├── DynamicForm.cs         # Formularul generat din JSON dinamic
    ├── PersonPickerDialog.cs  # Dialog căutare/selectare angajat
    ├── ClauzeEditorDialog.cs  # Editor avansat clauze Act Adițional
    ├── DocumentTheme.cs       # Paleta de culori per categorie
    ├── ConfirmareDialog.cs    # Dialog confirmare generare
    ├── SuccessDialog.cs       # Dialog succes cu cod înregistrare
    └── PunctModificareControl.cs # Control UI pentru un punct de clauză
```

---

## Configurare și deployment

### Variabile de sistem Windows (obligatorii)

| Variabilă | Descriere | Exemplu |
|-----------|-----------|---------|
| `TemplateDocsPath` | Folderul cu template-uri JSON + DOCX | `C:\HR\Templates` | Configurata din system env variables 
| `RecruitmentDocsPath` | Folderul unde se salvează PDF-urile generate | `C:\HR\Documente` |

Dacă `TemplateDocsPath` nu e setat, fallback la `{DLL_DIR}\Templates\`. (care nu exista in mod normal, deci eroare)

### Post-build -- se copiaza dllul direct intr-un path unde e softone (for convenience)

```
copy /Y "$(TargetPath)" "C:\Program Files (x86)\Soft1\Soft1 - Ice\ActAditionalPlugin.dll"
```

### Dependențe NuGet

- `Newtonsoft.Json` — deserializare JSON
- `DocumentFormat.OpenXml` (Open XML SDK) — manipulare DOCX
- `DocumentFormat.OpenXml.Framework`  (Open XML SDK) — manipulare DOCX
- `PdfiumViewer` — preview PDF în DynamicForm 0
- `PdfiumViewer` - PdfiumViewer.Native.x86_64.v8-xfa (OBLIGATORIU)
- `CosturaFody` + `Fody` - pentru a avea un dll ce contine toate dllurile necesare interop
- `Softone SDK` — TXCode, XSupport (furnished by Softone)

### Înregistrare / Deschidere în Softone

Două moduri de apelare:

**1. Din ecranul PRSNIN (angajat deja selectat):**
- Tip operație: `Command`
- CMD: `4000501`

**2. Din orice meniu, independent de ecran:**
- Tip operație: `Command`
- CMD: `4000502`

**2. Ca Dll Form (deschide SelectorDialog direct):**
- Tip operație: `Dll Form`
- Obiect/Fișier: `ActAditionalPlugin.dll;SelectorDialog`
- Comanda operatiune Softone in process: ".ActAditionalPlugin.dll;SelectorDialog" -> DllForm

---

## Cum funcționează fluxul

### 1. Startup
```
PluginEntry.S1.Initialize()
    └─► S1.xSupp = XSupport   (populat static, disponibil din orice thread)
```

### 2. Deschidere formular
```
ExecCommand(4000502)
    ├─► RegistraturaService.Initialize(XSupport)
    ├─► HookRegistry.RegisterAll()
    ├─► DocumentRegistry.Initialize(TemplatesRoot)
    │       └─► Scanează foldere, deserializează JSON-uri
    ├─► ErpDataProvider.GetCompanyData()
    ├─► PersonPickerDialog.LoadFromErp()  ← toți angajații activi
    └─► new SelectorDialog(persoane, currentPrsnId)
```

### 3. Selectare document + angajat
```
SelectorDialog
    ├─► User selectează angajat → PersonPickerDialog
    └─► User selectează document + apasă Continuă
            ├─► ErpDataProvider.GetCimData(prsnId)
            ├─► CommonDocumentValues.FromErp(...)
            └─► new DynamicForm(selectedDoc, common, persoane)
```

### 4. Completare formular
```
DynamicForm
    ├─► BuildBody() ← generează UI din JSON
    ├─► PrePopulateAngajatCIFields() ← CI/Domiciliu din common
    ├─► RunHooks("on_open")
    └─► [User completează câmpurile]
```

### 5. Generare PDF
```
DynamicForm.BtnGenereaza_Click()
    ├─► ValidateRequired()
    ├─► CollectFormValues()
    ├─► RunHooks("on_generate")
    ├─► DynamicTemplateEngine.GeneratePdf()
    │       ├─► Copiază DOCX template în temp
    │       ├─► RunHooks("on_generate") pe body
    │       ├─► ExpandDynamicLists()
    │       ├─► BuildPlaceholderMap() → înlocuiește {{cheie}} → valoare
    │       └─► Convertește DOCX → PDF (via Word COM sau LibreOffice)
    └─► RegistraturaService.Inregistreaza()
```

---

## Modele de date

### `DocumentDefinition`
Reprezintă un fișier `.json` deserializat.

```csharp
public class DocumentDefinition
{
    public string Title { get; set; }           // titlul documentului
    public string Category { get; set; }         // din numele folderului
    public string TemplateFile { get; set; }     // ex: "template_decizie.docx"
    public int Order { get; set; }               // ordinea în selector (0 = la final)
    public bool Registratura { get; set; }
    public string RegistraturaDateField { get; set; }
    public int RegistraturaTipDocPk { get; set; }
    public List<SectionDefinition> Sections { get; set; }
    public List<HookDefinition> Hooks { get; set; }
    // Runtime (nu din JSON):
    public string JsonPath { get; set; }
    public string TemplatePath { get; set; }
}
```

### `PersonInfo`
DTO universal pentru orice persoană (angajat principal sau picker secundar).

```csharp
public class PersonInfo
{
    public int PrsnId { get; set; }
    public string NumeComplet { get; set; }   // NAME + NAME2
    public string Nume { get; set; }           // NAME
    public string Prenume { get; set; }        // NAME2
    public string CNP { get; set; }            // PRSN.AFM
    public string Functie { get; set; }        // PRSN.SOTITLENAME
    public string CodCor { get; set; }         // SPECIALTY.CODE
    public string NrCim { get; set; }          // PRSEXTRA.NUM03
    public DateTime DataCim { get; set; }      // PRSEXTRA.DATE03
    public string NumeDepartament { get; set; }// DEPART.NAME
    public string SerieCI { get; set; }        // primele 2 litere din PRSN.IDENTITYNUM
    public string NrCI { get; set; }           // cifrele din PRSN.IDENTITYNUM
    public string Domiciliu { get; set; }      // PRSN.ADDRESS

    // Parsează "XZ123456" → serie="XZ", nr="123456"
    public static void ParseIdentityNum(string id, out string serie, out string nr)
}
```

### `CommonDocumentValues`
Date comune angajat + companie, populat automat din ERP la deschiderea formularului.

```csharp
public class CommonDocumentValues
{
    // Angajat
    public int PrsnId { get; set; }
    public string NumeSalariat { get; set; }
    public string CNP { get; set; }
    public string Functie { get; set; }
    public string NumeDepartament { get; set; }
    public string NrCim { get; set; }
    public DateTime DataCim { get; set; }
    public string SerieCI { get; set; }
    public string NrCI { get; set; }
    public string Domiciliu { get; set; }
    public string CodInregistrare { get; set; }

    // Companie
    public string NumeAngajator { get; set; }
    public string CIFAngajator { get; set; }
    public string ReprezentantLegal { get; set; }
    public string FunctieReprezentant { get; set; }  // din PluginConfig
    // ... adresă, IBAN, telefon, email, website
}
```

---

## Servicii

### `DocumentRegistry`
Singleton static. Scanează `TemplatesRoot` la startup și menține catalogul.

```csharp
DocumentRegistry.Initialize(string templatesRootPath);
List<DocumentCategory> DocumentRegistry.GetCategories();
DocumentDefinition DocumentRegistry.FindByTitle(string title);
```

**Ordinea categoriilor** este hardcodată în `CategoryOrder` (list în interiorul clasei). Folderele inexistente în `CategoryOrder` apar la final, sortate alfabetic.

### `ErpDataProvider`
Toate query-urile SQL către Softone.

```csharp
// Date angajat: NrCim, DataCim, Departament, SerieCI, NrCI, Domiciliu
ErpCimData GetCimData(int prsnId, XSupport xSupport)

// Date companie: din COMPANY + COMPANYEXT (ACCTOFFICE=2)
// FunctieReprezentant vine din PluginConfig (nu din SQL)
ErpCompanyData GetCompanyData(XSupport xSupport)
```

**Pattern Softone pentru SQL:**
```csharp
var ds = xSupport.GetSQLDataSet("SELECT ...");
string val = ds[0, "COLOANA"]?.ToString()?.Trim() ?? string.Empty;
```

### `DynamicTemplateEngine`
Completează template-ul DOCX cu valorile din formular.

**Fluxul intern `GeneratePdf`:**
1. Copiază template în `%TEMP%`
2. Rulează hookuri `on_generate` pe body OpenXML
3. `ExpandDynamicLists()` — clonează rânduri de tabel pentru fiecare item din liste dinamice
4. `BuildPlaceholderMap()` — construiește dicționarul `{{cheie}} → valoare`
5. Înlocuiește toate placeholder-ele din document
6. Convertește DOCX → PDF

**Placeholder-ele din `BuildPlaceholderMap`** includ automat toate câmpurile din `formValues` + toate câmpurile din `CommonDocumentValues` (vezi secțiunea [Placeholder-e comune](#placeholder-e-comune-automate)).

### `HookRegistry`
Mapare `string → Action<HookContext>`.

```csharp
HookRegistry.RegisterAll();  // apelat o singură dată la startup
HookRegistry.Register("NumeHook", handler);  // pentru hookuri custom viitoare
```

### `RegistraturaService`
Singleton inițializat cu `XSupport`.

```csharp
RegistraturaService.Initialize(xSupport);
RegistraturaService.Instance.Inregistreaza(def, formValues, common);
// Format cod: YYddd/NR (ex: 26182/5)
string cod = RegistraturaService.Instance.CalculateCod(loginDate);
```

### `BulkContext`
Bridge static pentru a pasa `XSupport` și `CompanyData` către thread-uri STA secundare (WinForms rulează pe STA).

```csharp
BulkContext.XSupport = XSupport;   // setat în ExecCommand, înainte de new Thread()
BulkContext.CompanyData = data;
BulkContext.Reset();               // apelat în finally după închiderea formularului
```

---

## UI

### `SelectorDialog`
Fereastra principală. Doi constructori:

```csharp
// Flux normal (din ExecCommand cu persoane deja încărcate)
new SelectorDialog(List<PersonInfo> persoane, int currentPrsnId = 0)

// Dll Form — încarcă singur totul via S1.xSupp
new SelectorDialog()
```

**Stări buton angajat din selector dialog:**
- Fără angajat: fundal roșu discret, badge pulsant animat, label "OBLIGATORIU"
- Cu angajat: fundal albastru, cerc cu inițiale, label "ANGAJAT SELECTAT" + iconița ✎

### `DynamicForm`
Generează UI-ul complet din `DocumentDefinition`. Suportă toate tipurile de câmpuri. Layout: split 40/60 (formular | preview PDF).

**Câmpuri generate din JSON:**

|	 `type`       | Control WinForms 		  | 			Note 		  |
|-------------    |---------------------------|---------------------------|
| `text` 	      | 	`TextBox`  	 		  | Triggerează `on_change`   |
| `multiline`     | `TextBox` multiline 	  | 			   			  |
| `date` 	      |  	`DateTimePicker` 	  | Triggerează `on_change`   |
| `readonly`      | `TextBox` disabled 		  | 						  |
| `number` 	      | `NumericUpDown` 		  | Triggerează `on_change`   |
| `combo` 	  	  | `ComboBox` 				  | `options: ["DA","NU"]` pentru boolean |
| `person_picker` | `Button` + câmpuri `maps` | Deschide `PersonPickerDialog` |
| `dynamic_list`  | `FlowLayoutPanel` cu rânduri add/delete |             |

**Precompletare automată CI/Domiciliu:** La deschidere, `PrePopulateAngajatCIFields()` populează câmpurile cu key `CISeria`/`SerieCI`, `CINr`/`NrCI`, `Domiciliu` din datele angajatului principal dacă sunt goale.

---

## Entry point Softone

```csharp
// Populează XSupport static la încărcarea pluginului
[WorksOn("GENERAL")]
public class S1 : TXCode
{
    public static XSupport xSupp;
    public override void Initialize() { base.Initialize(); xSupp = XSupport; }
}

// CMD 4000502 — independent de ecran, angajatul se alege din SelectorDialog
[WorksOn("GENERAL")]
public class ProgramGeneral : TXCode
{
    public override object ExecCommand(int Cmd)  // Cmd == 4000502
}

// CMD 4000501 — dependent de ecranul PRSNIN, angajatul vine din ecran
[WorksOn("PRSNIN")]
public class Program : TXCode
{
    public override object ExecCommand(int Cmd)  // Cmd == 4000501
}
```

**Single-instance guard:** Fiecare clasă are un `Mutex` separat (`ActAditionalPlugin_SingleInstance`). Dacă formularul e deja deschis, îl aduce în față în loc să deschidă altul.

---

## Schema JSON — referință completă

### Rădăcină

```jsonc
{
  "title": "Titlul documentului",         // afișat în selector și header formular
  "template_file": "template.docx",       // EXACT numele fișierului DOCX din același folder
  "registratura": true,                   // înregistrare automată în CCCVREGISTRATURA
  "registratura_date_field": "DataDoc",   // key-ul câmpului dată din formular
  "registratura_tip_doc_pk": 11,          // PK tip document din tabela registratură
  "order": 1,                             // ordinea în selector (opțional; 0 = la final)
  "sections": [],                         // lista secțiunilor
  "hooks": []                             // lista hookurilor
}
```

### Secțiune

```jsonc
{
  "title": "DATE DECIZIE",
  "height": 82,    // pixeli; omite pentru calcul automat
  "fields": []
}
```

**Calcul `height`:** `16 (padding) + n_campuri_simple × 58 + n_campuri_multiline × 172`

### Câmp

```jsonc
{
  "key": "DataDecizie",           // ID unic → placeholder {{DataDecizie}} în DOCX
  "label": "Data deciziei",
  "type": "date",                 // text | multiline | date | readonly | number | combo | person_picker | dynamic_list
  "label_width_percent": 50,      // suma pe rând = 100
  "required": true,
  "placeholder": "ex. 01.01.2026",
  "options": ["DA", "NU"],        // pentru combo
  "maps": {                       // pentru person_picker
    "NumeAutor": "NumeComplet",
    "CISeriaAutor": "SerieCI",
    "NrCIAutor": "NrCI",
    "DomiciliuAutor": "Domiciliu"
  },
  "initial_rows": 1,              // pentru dynamic_list
  "item_fields": []               // pentru dynamic_list
}
```

### Item field (în `dynamic_list`)

```jsonc
{
  "key": "NumeMembru",
  "label": "Nume și prenume",
  "type": "person_picker",   // text | number | person_picker
  "width_percent": 60,
  "maps": {
    "NumeMembru": "NumeComplet",
    "FunctieMembru": "Functie"
  }
}
```

### Hook

```jsonc
{
  "on": "on_open",           // on_open | on_generate | on_change
  "handler": "SetDefault",
  "params": {
    "field": "AniSuspendare",
    "value": "2"
  }
}
```

---

## Hookuri disponibile

| Handler | `on` | Descriere |
|---------|------|-----------|
| `SetDefault` | `on_open` | Setează valoarea unui câmp dacă e gol |
| `CalcDataSfarsit` | `on_change` | DataEnd = DataStart + LuniSuspendare |
| `CalcDataSfarsitDinCNP` | `on_change` | DataEnd = DataNaștere(CNP) + AniSuspendare |
| `CalcPerioadaSuspendare` | `on_generate` | Convertește nr. ani → text ("2 ani") |
| `ConcatList` | `on_generate` | Concatenează valorile unui câmp din `dynamic_list` |
| `InjectArticoleFinal` | `on_generate` | Calculează nr. articole finale din document |
| `SqlOnOpen` | `on_open` | Execută SQL și pune rezultatul într-un câmp |
| `SqlOnGenerate` | `on_generate` | Execută SQL la generare |

### Exemplu `ConcatList` cu `distinct`

```jsonc
{
  "on": "on_generate",
  "handler": "ConcatList",
  "params": {
    "source_field": "Referate",
    "item_key": "NumeIntocmitor",
    "target_field": "ListaIntocmitori",
    "separator": ", ",
    "distinct": "true"
  }
}
```

> ⚠️ `item_key` trebuie să fie cheia din `item_fields`, **nu** cheia câmpului `dynamic_list`.

---

## Placeholder-e comune automate

Aceste placeholder-e sunt populate automat — **nu le pune în `fields[]`**, le folosești direct în DOCX.

### Date angajat

| Placeholder | Sursă |
|-------------|-------|
| `{{NumeSalariat}}` | `PRSN.NAME + NAME2` |
| `{{CNP}}` | `PRSN.AFM` |
| `{{Functie}}` | `PRSN.SOTITLENAME` |
| `{{NumeDepartament}}` | `DEPART.NAME` |
| `{{NrCim}}` | `PRSEXTRA.NUM03` |
| `{{DataCim}}` | `PRSEXTRA.DATE03` (dd.MM.yyyy) |
| `{{SerieCI}}` | Primele 2 litere non-cifră din `PRSN.IDENTITYNUM` |
| `{{NrCI}}` | Cifrele din `PRSN.IDENTITYNUM` după serie |
| `{{Domiciliu}}` | `PRSN.ADDRESS` |
| `{{CodInregistrare}}` | Calculat automat (format `YYddd/NR`) |

### Date companie

| Placeholder | Sursă |
|-------------|-------|
| `{{NumeAngajator}}` | `COMPANY.NAME` |
| `{{CIFAngajator}}` | `COMPANY.AFM` |
| `{{ReprezentantLegal}}` | `COMPANYEXT.NAME + NAME2` (ACCTOFFICE=2) |
| `{{FunctieReprezentant}}` | `PluginConfig.FunctieReprezentant` (**hardcodat**, nu din SQL) |
| `{{AdresaCompanie}}` | Construită din `COMPANY` (județ, comună, stradă) |
| `{{NrRegComertului}}` | `COMPANY.BGBULSTAT` |
| `{{IbanCompanie}}` | `COMPANY.IBAN` |
| `{{NrTelefonCompanie}}` | `COMPANY.PHONE1` |
| `{{EmailCompanie}}` | `COMPANY.EMAIL` |
| `{{WebsiteCompanie}}` | `COMPANY.WEBPAGE` |

### Proprietăți disponibile în `maps` (person_picker)

| Valoare în maps | Ce returnează |
|-----------------|---------------|
| `NumeComplet` | Prenume + Nume |
| `Functie` | `PRSN.SOTITLENAME` |
| `CNP` | `PRSN.AFM` |
| `SerieCI` | Primele 2 litere din `PRSN.IDENTITYNUM` |
| `NrCI` | Cifrele din `PRSN.IDENTITYNUM` |
| `Domiciliu` | `PRSN.ADDRESS` |
| `NrCim` | `PRSEXTRA.NUM03` |
| `DataCim` | `PRSEXTRA.DATE03` (dd.MM.yyyy) |
| `NumeDepartament` | `DEPART.NAME` |
| `NumeComplet_Functie` | `Prenume Nume — Funcție` |

---

## Adăugarea unui document nou

### Pași

1. **Creează subfolder** în `TemplateDocsPath/{Categorie}/` // daca este cazul pentru o categorie noua
2. **Creează `document.json`** — copiază unul similar și adaptează
3. **Creează `document.docx`** — șablon Word cu placeholder-e `{{cheie}}`
4. Repornește pluginul (sau Softone) → documentul apare automat în selector

### Categorie nouă

Adaugă folderul pe disk. Adaugă numele în `CategoryOrder` din `DocumentRegistry.cs` pentru a controla ordinea în selector (opțional — fără asta apare la final in dreapta).

### Exemplu JSON minimal

```json
{
  "title": "Notificare Internă",
  "template_file": "template_notificare.docx",
  "registratura": true,
  "registratura_date_field": "DataNotificare",
  "registratura_tip_doc_pk": 11,
  "order": 1,
  "sections": [
    {
      "title": "DATE NOTIFICARE",
      "height": 82,
      "fields": [
        {
          "key": "DataNotificare",
          "label": "Data notificării",
          "type": "date",
          "label_width_percent": 50,
          "required": true
        },
        {
          "key": "TextNotificare",
          "label": "Conținut",
          "type": "multiline",
          "label_width_percent": 100
        }
      ]
    }
  ]
}
```

---

## Patterns Softone

```csharp
// SQL Dataset
var ds = xSupport.GetSQLDataSet("SELECT COL FROM TABLE WHERE ID = " + id);
string val = ds[0, "COL"]?.ToString()?.Trim() ?? string.Empty;
int count = ds.Count;

// Execute SQL (INSERT/UPDATE)
xSupport.ExecuteSQL("INSERT INTO ...");

// CompanyId / UserId
int companyId = xSupport.ConnectionInfo.CompanyId;
int userId    = xSupport.ConnectionInfo.UserId;

// WorksOn GENERAL — pentru acces independent de ecran
[WorksOn("GENERAL")]
public class MyClass : TXCode
{
    public static XSupport xSupp;
    public override void Initialize() { base.Initialize(); xSupp = XSupport; }
}

// Thread STA pentru WinForms
var thread = new Thread(() => { form.ShowDialog(); });
thread.SetApartmentState(ApartmentState.STA);
thread.IsBackground = true;
thread.Start();
```

> ⚠️ **Generic type constraints** (`where T : class`) cauzează runtime failures în mediul Softone. Folosește întotdeauna `as`-cast fără generic constraints.

---

## Greșeli frecvente

| Simptom | Cauză | Fix |
|---------|-------|-----|
| `{{Cheie}}` rămâne neînlocuit în PDF | Cheie greșită sau placeholder rupt în run-uri separate în Word | Verifică ortografia (case-sensitive); șterge și retastează placeholder-ul |
| `ConcatList` produce string gol | `item_key` e cheia `dynamic_list`, nu cheia din `item_fields` | Schimbă `item_key` cu cheia exactă din `item_fields` |
| `ConcatList` produce duplicate | Lipsește `"distinct": "true"` | Adaugă parametrul în `params` |
| Câmp `multiline` apare turtit | `"type": "text"` în loc de `"type": "multiline"` | Corectează tipul |
| Lista dinamică nu se expandează în DOCX | Placeholder-ul nu e pe propriul rând de tabel | Pune placeholder-ul singur în rândul de tabel |
| Documentul nu apare în selector | JSON invalid / `template_file` greșit / folder gol | Validează JSON; verifică că DOCX-ul există cu exact acel nume |
| `FunctieReprezentant` gol | Câmpul nu există în SQL, vine din `PluginConfig` | Editează `PluginConfig.FunctieReprezentant` și recompilează |
| Eroare la start: `TemplateDocsPath` | Variabila de sistem nu e setată | Setează variabila sau pune template-urile în `{DLL_DIR}\Templates\` |
| Formularul nu apare (single-instance) | O instanță e deja deschisă | Mutex-ul previne a doua instanță; aduce fereastra existentă în față |
