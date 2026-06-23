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

**Iconițe:** `Properties.Resources.refreshPreview`, `Properties.Resources.documentOK`, `Properties.Resources.back_arrow`

**`IsSplitterFixed = true`** — splitter-ul nu poate fi mutat  
**`ValidateFormForPreview()`** — virtual, validare minimă (doar cod înregistrare)  
**`MakeMultiline(height=88)`** — TextBox multiline; mouse wheel redirecționat către PnlBody  
`FlatAppearance.BorderSize = 3` pe toate butoanele

---

## SelectorDialog

- Categorii: **ACTE ADIȚIONALE, DECIZII — SUSPENDARE, DECIZII — ÎNCETARE, CERCETARE DISCIPLINARĂ, PROCESE VERBALE**
- Constructor: `SelectorDialog(List<AngajatItem> angajati, int currentPrsnId)`
- Proprietăți output: `SelectedPrsnId`, `SelectedName`, `SelectedCNP`, `SelectedFunctie`, `Selection`

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
| Perioadă de probă | `IncetarePerioadaProbaForm` | `IncetarePerioadaProba = 10` |

### CERCETARE DISCIPLINARĂ (fostă "REFERATE" — redenumită și extinsă)
| Tip | Form | TipDocument enum | Template |
|---|---|---|---|
| Referat disciplinar | `ReferatDisciplinarForm` | `ReferatDisciplinar = 11` | `template_referat_disciplinar.docx` |
| Avertisment disciplinar | `AvertismentDisciplinarForm` | `AvertismentDisciplinar = 12` | `template_avertisment.docx` |
| Constituire comisie | `DecizieConstituireComisieForm` | `DecizieConstituireComisie = 13` | `template_decizie_constituire_comisie.docx` |
| Convocare cercetare | `ConvocareCercetareForm` | `ConvocareCercetare = 14` | `template_convocare_cercetare.docx` |
| Proces verbal cercetare | `ProcesVerbalCercetareForm` | `ProcesVerbalCercetare = 15` | `template_pv_cercetare_disciplinara.docx` |
| Decizie disciplinară | `IncetareDisciplinarForm` | `IncetareDisciplinar = 9` | (existent) |

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
├── NumeDepartament
├── NrCim, DataCim
├── CodInregistrare
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

// DTO-uri liste dinamice (folosite de formele de cercetare disciplinara)
ReferatSursaItem { CodSiData, Intocmitor }
MembruComisieItem { Nume, Functie }

DecizieConstituireComisieModel : DocumentModelBase
└── DataDecizie, NumeIntocmitorHr (default "Marin Iulia Alina")
    DataNotaExplicativa, DescriereAbatere
    IntervalAniCCM (default "2024-2026")
    CodInregistrareITM (default "6123/CCMMRM/19.07.2024")
    List<ReferatSursaItem> Referate
    NumePresedinte, FunctiePresedinte
    List<MembruComisieItem> Membri
    NumeObservator, FunctieObservator
    DataInceputCercetare, DataSfarsitCercetare

ConvocareCercetareModel : DocumentModelBase
└── DataConvocare, NumeIntocmitorHr (default "Marin Iulia Alina")
    CodCor (din ERP: PRSJOBPOS→JOBPOSITION→SPECIALTY.CODE)
    List<ReferatSursaItem> Referate, DataNotaExplicativa, DescriereAbatere
    IntervalAniCCM (default "2024-2026")
    CodInregistrareITM (default "6123/CCMMRM/19.07.2024")
    LocCercetare (default "Localitatea Cătămărăști Deal..."), DataCercetare, OraConvocare
    NrDecizieComisie, DataDecizieComisie
    List<MembruComisieItem> Membri

ProcesVerbalCercetareModel : DocumentModelBase
└── DataCercetare, LocCercetare, ConcluziiComisie, SanctiuneaPropusa
```

---

## Placeholder-e Template (BuildCommonPlaceholders)

Disponibile pentru TOATE documentele:
`{{NumeSalariat}}`, `{{CNP}}`, `{{Functie}}`, `{{NumeDepartament}}`, `{{CodInregistrare}}`,
`{{NrCim}}`, `{{DataCim}}`, `{{NumeAngajator}}`, `{{CIFAngajator}}`, `{{ReprezentantLegal}}`,
`{{FunctieReprezentant}}`, `{{AdresaCompanie}}`, `{{ZipCompanie}}`, `{{NrRegComertului}}`,
`{{IbanCompanie}}`, `{{NrTelefonCompanie}}`, `{{EmailCompanie}}`, `{{WebsiteCompanie}}`,
`{{ArticolCompartiment}}`, `{{ArticolContestatie}}`, `{{MentiuniDocument}}`

**Cercetare disciplinară — comune:**
`{{DataDecizie}}`, `{{NumeIntocmitorHr}}`, `{{IntervalAniCCM}}`, `{{CodInregistrareITM}}`,
`{{DataNotaExplicativa}}`, `{{DescriereAbatere}}`,
`{{ReferateSursa}}` (concatenat ", "), `{{NumeSiFunctieIntocmitorReferat}}` (distinct, concatenat ", ")

**Decizie constituire comisie — specifice:**
`{{NumePresedinte}}`, `{{FunctiePresedinte}}`,
`{{NumeMembru}}` + `{{FunctieMembru}}` (linie expandabilă per membru — `ExpandParagraphList`),
`{{NumeMembruSemnatura}}` (expandabil la semnături),
`{{NumeObservator}}`, `{{FunctieObservator}}`,
`{{DataInceputCercetare}}`, `{{DataSfarsitCercetare}}`

**Convocare — specifice:**
`{{DataConvocare}}`, `{{CodCor}}`, `{{LocCercetare}}`, `{{DataCercetare}}`, `{{OraConvocare}}`,
`{{NrDecizieComisie}}`, `{{DataDecizieComisie}}`,
`{{NumeMembruComisie}}` + `{{FunctieMembruComisie}}` (expandabil per membru)

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
// ErpCimData: NrCim, DataCim, NumeDepartament, CodCor
// CodCor: SELECT TOP 1 S.CODE FROM PRSJOBPOS PJ
//   JOIN JOBPOSITION J ON PJ.JOBPOSITION = J.JOBPOSITION
//   JOIN SPECIALTY S ON J.SPECIALTY = S.SPECIALTY
//   WHERE PJ.PRSN = {id} AND PJ.COMPANY = {companyId}
ErpDataProvider.GetCompanyData(XSupport)
```

### BulkContext (static bridge TXCode↔STA)
```csharp
BulkContext.Angajati, BulkContext.CompanyData, BulkContext.GetCimData
BulkContext.GetAdresaPrimitor, BulkContext.XSupport, BulkContext.IsAvailable, BulkContext.Reset()
```

### TemplateEngine
```csharp
TemplateEngine.GeneratePdf(model, templatePath)
// ExpandParagraphList<T>(body, markerText, items, mapBuilder)
//   — găsește paragraful cu marker, îl clonează per item, șterge originalul
//   — folosit pentru: {{NumeMembru}}/{{FunctieMembru}}, {{NumeMembruSemnatura}},
//                     {{NumeMembruComisie}}/{{FunctieMembruComisie}}
// ReferateSursa și NumeSiFunctieIntocmitorReferat: concatenate cu ", " (Distinct pentru intocmitori)
```

---

## FormBase — Structură UI

```
FormBase (abstract)
├── Theme: DocumentTheme
├── PnlBody: Panel (AutoScroll, în split Panel1)
├── SplitContainer: 40% sidebar / 60% PdfViewer
│
├── AddSectiune(titlu, ref y, height) → FlowLayoutPanel în PnlBody (AutoSize, MinimumSize)
├── AddDynamicListSection(titlu, btnText, onAdd, ref y) → Panel cu AutoScroll=true, height=200
│     — header cu buton "+ Adaugă..." (Theme.Accent, alb), panel items scrollabil
│     — folosit pentru liste de RefератSursa și MembruComisie
├── AddMentiuniSection(ref y) → _txtMentiuni
├── AddRow(panel, int[] percents) → TableLayoutPanel
├── AddLabeledInput(tbl, col, label, control, required=false)
│
├── MakeInput(placeholder) → TextBox cu placeholder gri (SetPlaceholder)
├── MakeDtp() → DateTimePicker
├── MakeReadonly() → TextBox readonly
├── MakeMultiline(height=88) → TextBox multiline, MouseWheel → PnlBody scroll
├── MakeCombo() → ComboBox, MouseWheel blocat (redirect PnlBody)
│
├── ReflowPnlBody() — restivuiește controalele din PnlBody în ordinea adăugării (nu după Top)
├── RecalcFlowHeight(flow) — recalculează height FlowLayoutPanel după conținut
├── RedirectWheelToPnlBody — handler shared pentru ComboBox MouseWheel
│
├── ValidateFormForPreview() virtual
├── GetRegistraturaDate() virtual
├── PopulateMentiuni() virtual
└── ResizeImage(Image, w, h)

DocumentFormBase : FormBase
├── CodInregistrareField → TextBox readonly cu cod live
└── FillAngajator(model)

DecizieFormBase : DocumentFormBase
├── ValidateFormForPreview() override
└── GetRegistraturaDate() override → DtpDataDecizie.Value.Date
```

### Controale dinamice (CercetareDisciplinaraForms.cs)
```
PhHelper (static)
├── SetPh(tb, ph) — placeholder gri + GotFocus/LostFocus
└── RelayoutPanel(pnl, items) — stivuiește items în panel cu AutoScroll

ReferatSursaControl : Panel
├── Numar (label), OnDelete (Action)
├── _txtCodSiData (placeholder "ex. 168/14.04.2025")
├── _txtIntocmitor (placeholder "Nume, Prenume — Funcție")
├── IsValid() — verifică ForeColor != Gray
└── GetItem() → ReferatSursaItem

MembruComisieControl : Panel
├── Numar, OnDelete
├── _txtNume, _txtFunctie
├── IsValid(), GetItem() → MembruComisieItem
```

---

## DocumentTheme

| Temă | Culoare | Folosită pentru |
|---|---|---|
| `Acte` | Albastru RGB(63,129,198) | Act Adițional |
| `Suspendare` | Verde teal | Decizii Suspendare |
| `Incetare` | Roșu | Decizii Încetare |
| `CercetareDisciplinara` | Violet RGB(120,60,170) | Toate tipurile 9,11-15 |
| `Pv` | Amber | Procese Verbale |

`DocumentTheme.For(TipDocument)` → returnează tema corectă.

---

## PluginEntry — Structuri Cheie

```csharp
// CreateDocModel populează CodCor doar pentru ConvocareCercetareModel:
var convocare = m as ConvocareCercetareModel;
if (convocare != null) convocare.CodCor = cimData.CodCor;

// CreateForm switch include toate cele 6 tipuri din Cercetare Disciplinară
```

---

## Convenții & Reguli

| Regulă | Detaliu |
|---|---|
| Stringuri UI | Română cu diacritice în labels, fără diacritice în cod/placeholder |
| Placeholdere template | `{{NumePlaceholder}}` fără spații |
| `CodInregistrare` | În `BuildCommonPlaceholders` — disponibil pentru toate |
| `MentiuniDocument` | NU în PDF, DA în DB (Acte/Decizii); PV: nici PDF nici DB |
| `ErpCimData`/`ErpCompanyData` | Namespace `ActAditionalPlugin.Services` |
| Dataset Softone | `ds[i, "COLOANA"]` — nu `ds[i]["COLOANA"]` |
| `GetRegistraturaDate()` | Virtual în FormBase, override obligatoriu |
| `AddMentiuniSection` | FĂRĂ `Dock = DockStyle.Top` |
| `BulkContext.XSupport` | Disponibil pentru SQL din forme |
| `MakeCombo()` | Metodă de instanță (nu static) — include MouseWheel blocat |
| `AddDynamicListSection` | Protected în FormBase — folosit pt liste Referate/Membri |
| `ReflowPnlBody` | Sortare după ordinea din Controls, NU după Top |
| `ExpandParagraphList` | Fiecare placeholder pe paragraf separat în template! |
| `ReferateSursa` concatenat | `string.Join(", ", ...)` — nu expand paragraf |
| `NumeSiFunctieIntocmitorReferat` | `.Distinct()` înainte de Join |
| MouseWheel | Blocat pe ComboBox (MakeCombo) și NumericUpDown (EchipamentItemControl) |
| CCM/ITM precompletate | IntervalAniCCM="2024-2026", CodInregistrareITM="6123/CCMMRM/19.07.2024" |
| NumeIntocmitorHr default | "Marin Iulia Alina" — editabil manual |

---

## Probleme Cunoscute Rezolvate

1. `GetRegistraturaDate` lipsea din FormBase → adăugat virtual
2. `ErpCimData`/`ErpCompanyData` referite ca `Models.*` → `Services.*`
3. `ClauzeConfig.Clauze` (API veche) → `GetTipSelectat().Clauze`
4. `_txtMentiuni.Dock = DockStyle.Top` → width explicit
5. `pnlIncetareWrapper` suprapunea mențiunile → `y += height + 8`
6. Label "DATA ÎNCETARE SUSPENDARE" invizibil → `Theme.Accent`
7. App se închidea după generare → loop redeschide SelectorDialog
8. `{{CodInregistrare}}` nu se înlocuia în Referat/Avertisment → `BuildCommonPlaceholders`
9. PdfViewer ascuns de placeholder label → `BringToFront()` + `lblPlaceholder.Visible=false`
10. MouseWheel pe multiline TextBox → redirecționat în `MakeMultiline`
11. Dataset Softone `ds[i]["COL"]` → `ds[i, "COL"]`
12. FlowLayoutPanel AutoSize=true → expansiune infinită pe orizontală → AutoSize=false + RecalcFlowHeight
13. BeginInvoke înainte de handle creat → eliminat ControlAdded+BeginInvoke
14. ReflowPnlBody sort după Top → amestecare la resize → sort după ordinea din Controls
15. HandleCreated apelat cu PnlBody fără dimensiuni → eliminat, mutat în Shown+ResizeEnd
16. `{{NumeMembruSemnatura}}` + `{{NumeObservator}}` în același paragraf → split în paragrafe separate
17. PlaceholderText (C# 11) → SetPh() + GotFocus/LostFocus pattern
18. RelayoutPanel private în DecizieConstituireComisieForm → mutat în PhHelper static
19. AddDynamicListSection private → mutat în FormBase protected
20. ComboBox/NumericUpDown consumau scroll wheel → MouseWheel blocat și redirecționat
