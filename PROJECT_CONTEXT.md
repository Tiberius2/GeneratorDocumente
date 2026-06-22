# Generator Documente HR — Project Context
**Data:** Iunie 2026 | **Stack:** .NET 4.7.2, WinForms, C# 7.3, Softone TXCode

---

## Arhitectură Generală

**Entry point:** `PluginEntry.cs` — CMD 4000501, WorksOn PRSNIN  
**Namespace root:** `ActAditionalPlugin` → foldere `Models/`, `Services/`, `UI/`  
**Packages:** PdfSharp 6.2.4, Open XML SDK, Word Interop, Newtonsoft.Json, PdfiumViewer

### Flow principal
```
Softone (PRSNIN) → PluginEntry (TXCode thread)
  → LoadHiredEmployees() — SQL: PRSN JOIN PRSJOBPOS WHERE JOBPOSITION != 0
  → BulkContext.Set(angajati, delegates, XSupport)
  → RegistraturaService.Initialize(XSupport)
  → STA Thread:
      └─ LOOP INFINIT:
          ├─ SelectorDialog(angajati, prsn.PrsnId)
          │     └─ Anulează → EXIT
          ├─ CreateForm(selection, prsn, cimData, ...)
          └─ form.ShowDialog() → revenire la SelectorDialog (OK sau Cancel)
```

---

## Layout Split 40/60 (toate formularele)

Toate formularele folosesc layout split `SplitContainer`:
- **Stânga 40%:** sidebar cu formular scrollabil + footer static
- **Dreapta 60%:** `PdfViewer` embedded (PdfiumViewer), gol la deschidere

**Footer sidebar (3 butoane):**
- **Anulare** (stânga, roșu deschis `RGB(255,220,220)`, icon `back_arrow`) → `DialogResult.Cancel` + `Close()`
- **Previzualizează** (dreapta-2, galben `RGB(255,243,176)`, icon `refreshPreview`) → generează PDF temp, îl afișează în viewer. Text se schimbă în "Actualizează" după prima previzualizare
- **Generează PDF** (dreapta-1, `Theme.Accent`, icon `documentOK`) → validare completă + generare finală + registratură

**Iconițe:** `Properties.Resources.refreshPreview`, `Properties.Resources.documentOK`, `Properties.Resources.back_arrow` (toate adăugate ca Image în Resources)

**`IsSplitterFixed = true`** — splitter-ul nu poate fi mutat  
**`ValidateFormForPreview()`** — virtual, validare minimă (doar cod înregistrare), override în forme cu câmpuri blocante

**`MakeMultiline(height=88)`** — TextBox multiline; mouse wheel redirecționat către PnlBody  
`FlatAppearance.BorderSize = 3` pe toate butoanele

---

## SelectorDialog

- Categorii: ACTE ADIȚIONALE, DECIZII — SUSPENDARE, DECIZII — ÎNCETARE, REFERATE, PROCESE VERBALE
- **Constructor:** `SelectorDialog(List<AngajatItem> angajati, int currentPrsnId)`
- **Proprietăți output:** `SelectedPrsnId`, `SelectedName`, `SelectedCNP`, `SelectedFunctie`, `Selection`

---

## Tipuri Documente & Formulare

### ACTE ADIȚIONALE
| Tip | Form | Template |
|---|---|---|
| Act Adițional | `ActAditionalForm` | `ActAditional_template.docx` |

### DECIZII — SUSPENDARE
| Tip | Form | TipDocument enum |
|---|---|---|
| Creștere copil | `SuspendareCresterecopilForm` | `SuspendareCresterecopil = 1` |
| Creștere copil handicap | `SuspendareCresterecopilHandicapForm` | `SuspendareCresterecopilHandicap = 2` |
| Absențe nemotivate | `SuspendareAbsenteForm` | `SuspendareAbsenteNemotivate = 3` |
| Acordul părților | `SuspendareAcordPartiForm` | `SuspendareAcordParti = 4` |
| Suspendare + Încetare | `SuspendareSiIncetareSuspendareForm` | `SuspendareSiIncetareSuspendare = 5` |

### DECIZII — ÎNCETARE
| Tip | Form | TipDocument enum |
|---|---|---|
| Încetare suspendare | `IncetareSuspendareForm` | `IncetareSuspendare = 6` |
| Demisie | `IncetareDemisieForm` | `IncetareDemisie = 7` |
| Expirare termen | `IncetareExpirareForm` | `IncetareExpirare = 8` |
| Disciplinar | `IncetareDisciplinarForm` | `IncetareDisciplinar = 9` |
| Perioadă de probă | `IncetarePerioadaProbaForm` | `IncetarePerioadaProba = 10` |

### REFERATE
| Tip | Form | TipDocument enum |
|---|---|---|
| Referat disciplinar | `ReferatDisciplinarForm` | `ReferatDisciplinar = 11` |
| Avertisment disciplinar | `AvertismentDisciplinarForm` | `AvertismentDisciplinar = 12` |

### PROCESE VERBALE
| Tip | Form |
|---|---|
| Echipamente | `PvBunuriForm` |
| Electronice | `PvBunuriForm` (alt template) |
| Autovehicul | `PvAutovehiculForm` |

---

## Modele

```
DocumentModelBase (abstract)
├── PrsnId, NumeSalariat, CNP, Functie
├── NumeDepartament  ← NOU (din ErpCimData via JOIN DEPART)
├── NrCim, DataCim
├── CodInregistrare  ← în BuildCommonPlaceholders (disponibil pentru TOATE tipurile)
├── NumeAngajator, CIFAngajator, ReprezentantLegal, FunctieReprezentant
├── AdresaCompanie, ZipCompanie, NrRegComertului, IbanCompanie
├── NrTelefonCompanie, EmailCompanie, WebsiteCompanie
├── MentiuniDocument
└── TipDocument (abstract)

ReferatDisciplinarModel : DocumentModelBase
└── DataReferat, NumeAutorReferat, FunctieAutorReferat, LocMunca
    DescriereFapta, ConsecinteAbateri, TemeiLegal

AvertismentDisciplinarModel : DocumentModelBase
└── DataDecizie, NrReferat, DataReferat
    NumeAutorReferat, FunctieAutorReferat, LocMunca
    DescriereAbateri, DescriereAbateriDetaliat, DataComunicare
```

---

## Placeholder-e Template (BuildCommonPlaceholders)

Disponibile pentru TOATE documentele:
`{{NumeSalariat}}`, `{{CNP}}`, `{{Functie}}`, `{{NumeDepartament}}`, `{{CodInregistrare}}`,
`{{NrCim}}`, `{{DataCim}}`, `{{NumeAngajator}}`, `{{CIFAngajator}}`, `{{ReprezentantLegal}}`,
`{{FunctieReprezentant}}`, `{{AdresaCompanie}}`, `{{ZipCompanie}}`, `{{NrRegComertului}}`,
`{{IbanCompanie}}`, `{{NrTelefonCompanie}}`, `{{EmailCompanie}}`, `{{WebsiteCompanie}}`,
`{{ArticolCompartiment}}`, `{{ArticolContestatie}}`, `{{MentiuniDocument}}`

Placeholder-e specifice Referat: `{{DataReferat}}`, `{{NumeAutorReferat}}`, `{{FunctieAutorReferat}}`, `{{LocMunca}}`, `{{DescriereFapta}}`, `{{ConsecinteAbateri}}`, `{{TemeiLegal}}`

Placeholder-e specifice Avertisment: `{{DataDecizie}}`, `{{NrReferat}}`, `{{DataReferat}}`, `{{NumeAutorReferat}}`, `{{FunctieAutorReferat}}`, `{{LocMunca}}`, `{{DescriereAbateri}}`, `{{DescriereAbateriDetaliat}}`, `{{DataComunicare}}`

---

## Servicii

### RegistraturaService (singleton)
```csharp
RegistraturaService.Initialize(XSupport)
RegistraturaService.Instance.CalculateCod(date)     // → "26168/3"
RegistraturaService.Instance.Inregistreaza(cod, date, tipDocPK, titlu, prsnId)
RegistraturaService.Instance.GetLoginDate()
```

### ErpDataProvider
```csharp
ErpDataProvider.GetCimData(prsnId, XSupport)
// SQL extins: JOIN DEPART → aduce și NumeDepartament în ErpCimData
ErpDataProvider.GetCompanyData(XSupport)
```

### BulkContext (static bridge TXCode↔STA)
```csharp
BulkContext.Angajati          // List<AngajatItem>
BulkContext.CompanyData       // ErpCompanyData
BulkContext.GetCimData        // Func<int, ErpCimData>
BulkContext.GetAdresaPrimitor // Func<int, string>
BulkContext.XSupport          // XSupport — NOU, pentru SQL din forme (WORKAREA etc.)
BulkContext.IsAvailable       // → true dacă toate setate
BulkContext.Reset()
```

### TemplateEngine
```csharp
TemplateEngine.GeneratePdf(model, templatePath)
// CodInregistrare acum în BuildCommonPlaceholders (nu mai e doar în AddDecizie/AddActAditional)
// Filename: tip_CodInreg_DataDoc.pdf (ex: Referat_Disciplinar_26171-56_22-06-2026.pdf)
```

### ClauzeService / CnpHelper
Neschimbate față de versiunea anterioară.

---

## FormBase — Structură UI

```
FormBase (abstract)
├── Theme: DocumentTheme
├── PnlBody: Panel (scrollabil, în split Panel1)
├── SplitContainer: 40% sidebar / 60% PdfViewer
│
├── Footer sidebar (static):
│   ├── btnInapoi (stânga, roșu deschis, icon back_arrow)
│   ├── btnActualizeaza (dreapta-2, galben, icon refreshPreview)
│   └── btnGenereaza (dreapta-1, Theme.Accent, icon documentOK)
│
├── AddSectiune(titlu, ref y, height) → FlowLayoutPanel în PnlBody
├── AddMentiuniSection(ref y) → _txtMentiuni (Width explicit, fără Dock.Top!)
├── AddRow(panel, int[] percents) → TableLayoutPanel
├── AddLabeledInput(tbl, col, label, control, required=false)
│
├── MakeInput(placeholder) → TextBox cu placeholder gri
├── MakeDtp() → DateTimePicker
├── MakeReadonly() → TextBox readonly
├── MakeMultiline(height=88) → TextBox multiline, MouseWheel → PnlBody scroll
│
├── ValidateFormForPreview() virtual → validare minimă (cod înregistrare)
├── GetRegistraturaDate() virtual → override în subclase
├── PopulateMentiuni() virtual
│
└── ResizeImage(Image, w, h) → scalează icoane la 20×20

DocumentFormBase : FormBase
├── CodInregistrareField → TextBox readonly cu cod live
└── FillAngajator(model) → populează câmpurile companiei

DecizieFormBase : DocumentFormBase
├── ValidateFormForPreview() override → ValidateDecizie() (doar cod)
└── GetRegistraturaDate() override → DtpDataDecizie.Value.Date
```

---

## ReferatDisciplinarForm

**Template:** `template_referat_disciplinar.docx`  
**Secțiuni:** Date document | Autor referat | Locul de muncă (ComboBox) | Descriere abatere (2 textarea) | Temei legal | Mențiuni  
**LocMunca:** ComboBox populat din `SELECT DISTINCT NAME, ADDRESS FROM WORKAREA WHERE COMPANY={id} AND ISACTIVE=1`  
Format item: `"NAME - ADDRESS"`  
**Filename PDF:** `Referat_Disciplinar_{CodInreg}_{DataReferat}.pdf`  
**DB:** fără scriere în DB după generare (nu e Decizie)

## AvertismentDisciplinarForm

**Template:** `template_avertisment.docx`  
**Secțiuni:** Date decizie | Referat sursă | Autor referat | Locul de muncă (ComboBox) | Descrierea abaterii (2 textarea: detaliat + scurt) | Data comunicării | Mențiuni  
**LocMunca:** același ComboBox WORKAREA ca la Referat  
**Filename PDF:** `Avertisment_Disciplinar_{CodInreg}_{DataDecizie}.pdf`  
**Câmpuri template:** `{{DescriereAbateriDetaliat}}` (intro) + `{{DescriereAbateri}}` (Art. 2)

---

## PluginEntry — Structuri Cheie

```csharp
// După generare (OK sau Cancel) → SelectorDialog se redeschide mereu
// BulkContext.XSupport = XSupport setat la inițializare

// CreateForm switch include:
case TipDocument.ReferatDisciplinar: return new ReferatDisciplinarForm(...)
case TipDocument.AvertismentDisciplinar: return new AvertismentDisciplinarForm(...)
```

---

## Convenții & Reguli

| Regulă | Detaliu |
|---|---|
| Stringuri UI | Română cu diacritice în labels vizibile, fără diacritice în cod/placeholder |
| Placeholdere template | `{{NumePlaceholder}}` fără spații |
| `CodInregistrare` | În `BuildCommonPlaceholders` — disponibil pentru toate tipurile |
| `MentiuniDocument` | NU în PDF, DA în DB (Acte/Decizii); PV: nici în PDF nici în DB |
| `ErpCimData`/`ErpCompanyData` | Namespace `ActAditionalPlugin.Services` |
| Dataset Softone | `ds[i, "COLOANA"]` — nu `ds[i]["COLOANA"]` sau `ds.Current` |
| `GetRegistraturaDate()` | Virtual în FormBase, override obligatoriu în forme cu date proprii |
| `AddMentiuniSection` | FĂRĂ `Dock = DockStyle.Top` (width explicit pe resize) |
| `BulkContext.XSupport` | Disponibil pentru SQL din forme (WORKAREA, etc.) |
| `ValidateFormForPreview()` | Override când ValidateForm() blochează preview (câmpuri non-obligatorii pentru preview) |
| `MakeMultiline` | Nu folosi `Dock.Top` în FlowLayoutPanel; setează Width explicit + Resize handler |

---

## Probleme Cunoscute Rezolvate

1. `GetRegistraturaDate` lipsea din FormBase ca virtual → adăugat
2. `ErpCimData`/`ErpCompanyData` referite ca `Models.*` → corectate la `Services.*`
3. `ClauzeConfig.Clauze` (API veche) → `GetTipSelectat().Clauze`
4. `_txtMentiuni.Dock = DockStyle.Top` în FlowLayoutPanel → width explicit
5. `pnlIncetareWrapper` suprapunea mențiunile → `y += pnlIncInner.Height + 8`
6. Label "DATA ÎNCETARE SUSPENDARE" invizibil → `Theme.Accent`
7. App se închidea după generare → loop redeschide SelectorDialog pentru OK și Cancel
8. `{{CodInregistrare}}` nu se înlocuia în Referat/Avertisment → mutat în `BuildCommonPlaceholders`
9. PdfViewer ascuns de placeholder label (Z-order) → `BringToFront()` + `lblPlaceholder.Visible=false`
10. MouseWheel pe multiline TextBox nu scrola formularul → redirecționat în `MakeMultiline`
11. Dataset Softone iterat greșit cu `ds[i]["COL"]` → corectat la `ds[i, "COL"]`
