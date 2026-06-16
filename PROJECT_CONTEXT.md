# Generator Documente HR — Context Proiect

## Descriere generala
Plugin Softone ERP (.NET 4.7.2, WinForms, TXCode architecture) pentru generarea documentelor HR ca PDF din template-uri Word.  
Entry point: **CMD 4000501** (singurul — 4000502 eliminat).  
Lucreaza pe ecranul `PRSNIN` (`[WorksOn("PRSNIN")]`).

---

## Stack tehnic
- .NET 4.7.2 WinForms
- PdfSharp, PdfiumViewer (preview PDF)
- Word Interop + OpenXML (generare DOCX → PDF)
- Newtonsoft.Json (clauze act aditional + alte modele)
- Costura.Fody (embed DLL-uri)
- Softone TXCode API: `XSupport`, `XModule`, `XTable`, `XRow`

---

## Structura fisiere

```
Models/
  DocumentModels.cs       ← DocumentModelBase + toate modelele decizii/acte
  PvModels.cs             ← PvModelBase + modele PV + IPvBunuriModel
  DocumentTheme.cs        ← 4 palete de culori per categorie
  SelectionTypes.cs       ← DocSelection / PvSelection (discriminated union)
  PluginConfig.cs         ← template paths, date companie fallback
  ClauzeConfig.cs         ← ClauzeActAditional, CampClauza, ClauzeConfig (JSON model)

Services/
  RegistraturaService.cs  ← calcul cod YYddd/NR, INSERT CCCVREGISTRATURA + CCCVDOCAUDIT
  TemplateEngine.cs       ← fill + generate PDF pentru documente
  PvTemplateEngine.cs     ← fill + generate PDF pentru PV
  ErpDataProvider.cs      ← GetCimData (NUM03, DATE03), GetCompanyData
  WordHelper.cs           ← OpenXML + Word Interop utilities
  ClauzeService.cs        ← Load/Save clauze_act_aditional.json din %TemplateDocsPath%

UI/
  FormBase.cs             ← baza UI comuna: tema, shell, layout helpers, PDF flow
  DocumentFormBase.cs     ← extinde FormBase: sectiune angajat cu CIM, TemplateEngine
  PvFormBase.cs           ← extinde FormBase: sectiune angajat fara CIM, PvTemplateEngine
  DecizieFormBase.cs      ← baza decizii (TxtCodInregistrare, DtpDataDecizie)
  SelectorDialog.cs       ← selector unificat 4 categorii cu carduri
  ConfirmareDialog.cs     ← prompt confirmare inregistrare (stilizat tema)
  SuccessDialog.cs        ← dialog succes dupa generare
  PdfPreviewForm.cs       ← previzualizare PDF cu tema categoriei
  ActAditionalForm.cs     ← formular Act Aditional
  DeciziiSuspendare.cs    ← 6 forme suspendare
  DeciziiIncetare.cs      ← 5 forme incetare (inclusiv IncetarePerioadaProba)
  PvForms.cs              ← PvBunuriForm (Echipamente+Electronice) + PvAutovehiculForm
  PunctModificareControl.cs ← control punct modificare, JSON-driven din ClauzeConfig
  ClauzeEditorDialog.cs   ← dialog editor clauze act aditional (add/edit/delete)
  EchipamentItemControl.cs

PluginEntry.cs            ← single entry 4000501, factory unificat, RegistraturaService.Initialize
```

---

## Modele documente

### TipDocument enum → PK CCCTIPDOCREG
| TipDocument | Descriere | PK |
|---|---|---|
| ActAditional | Act Adițional | 2 |
| SuspendareCresterecopil | Decizie Suspendare Creștere Copil | 11 |
| SuspendareCresterecopilHandicap | Decizie Suspendare Creștere Copil Handicap | 11 |
| SuspendareAbsenteNemotivate | Decizie Suspendare Absențe Nemotivate | 11 |
| SuspendareAcordParti | Decizie Suspendare Acordul Părților | 11 |
| SuspendareSiIncetareSuspendare | Decizie Suspendare și Încetare | 11 |
| IncetareSuspendare | Decizie Încetare Suspendare | 11 |
| IncetareDemisie | Decizie Încetare prin Demisie | 11 |
| IncetareExpirare | Decizie Încetare prin Expirare Termen | 11 |
| IncetareDisciplinar | Decizie Concediere Disciplinară | 11 |
| IncetarePerioadaProba | Decizie Încetare Perioadă de Probă | 11 |

### TipPV enum → PK CCCTIPDOCREG
| TipPV | PK |
|---|---|
| Echipamente | 22 |
| Electronice | 22 |
| Autovehicul | 22 |

---

## Teme culori (DocumentTheme)

| Categorie | Culoare | Accent RGB |
|---|---|---|
| Acte Adiționale | Albastru | (63, 129, 198) |
| Decizii Suspendare | Teal | (32, 158, 145) |
| Decizii Încetare | Rose | (192, 72, 68) |
| Procese Verbale | Amber | (192, 120, 30) |

Fiecare temă are: `Accent`, `AccentPal`, `AccentBorder`, `AccentDark`.

---

## Registratura (CCCVREGISTRATURA)

### Cod înregistrare format
`YYddd/NR` — ex: `26133/5`  
- `YY` = ultimele 2 cifre an  
- `ddd` = ziua din an (001-366)  
- `NR` = MAX(NRINREG)+1 per zi

### Data înregistrare
Codul se calculează pe baza datei din câmpul de dată al formularului (nu loginDate fix).  
`FormBase` expune `protected virtual DateTime GetRegistraturaDate()` — fallback la `loginDate`.  
Override-uri:
- `DecizieFormBase` → `DtpDataDecizie.Value.Date`
- `ActAditionalForm` → `_dtpDataAct.Value.Date`
- `PvBunuriForm` / `PvAutovehiculForm` → `_dtpData.Value.Date`

La schimbarea datei din DTP, `CodInregistrareField` se actualizează live via `ValueChanged`.

### Parametri INSERT
- `DATAINREG` = data selectată de user în formular
- `DIRECTIE` = 3 (Intern)
- `STATUS` = 1 (Înregistrat)
- `TIPTERT` = 5 (Angajat)
- `PRSNTERT` = model.PrsnId

### Tabele
- `CCCVREGISTRATURA` — înregistrări principale
- `CCCVDOCAUDIT` — audit trail modificări
- `CCCTIPDOCREG` — tipuri documente configurabile

### SQL execute
`XSupport.ExecuteSQL(sql)` — pentru INSERT

---

## Mențiuni / Observații

Toate tipurile de documente (decizii, acte, PV) au secțiunea **MENȚIUNI / OBSERVAȚII** la finalul formularului.

- Câmpul `protected TextBox _txtMentiuni` definit în `FormBase`
- Metoda `protected Panel AddMentiuniSection(ref int y)` în `FormBase` — adaugă secțiunea
- `protected virtual void PopulateMentiuni()` în `FormBase` — apelat după `PopulateModel()`
- Override în `DocumentFormBase` și `PvFormBase` → scrie în `model.MentiuniDocument`
- Proprietatea `MentiuniDocument` există pe `DocumentModelBase` și `PvModelBase`
- Placeholder în template: `{{MentiuniDocument}}`

**Notă ActAditional:** secțiunea Mențiuni e plasată ÎNAINTE de secțiunea MODIFICĂRI (din cauza panelului cu scroll dinamic al modificărilor).

---

## Clauze Act Adițional (JSON-driven)

### Fișier configurare
`%TemplateDocsPath%\clauze_act_aditional.json`  
Creat automat cu valori default la prima rulare dacă nu există.

### Structura JSON
```json
{
  "clauze": [
    {
      "id": "fct001",
      "titlu": "Functie / COR (Lit. F)",
      "textClauza": "La litera F, ... se modifica si va avea urmatorul cuprins:",
      "textDinamic": "Functia/meseria: {0} conform COR, cod {1}.",
      "campuri": [
        { "label": "Functie noua", "placeholder": "ex. BRUTAR", "ordine": 0 },
        { "label": "Cod COR", "placeholder": "ex. 751201", "ordine": 1 }
      ],
      "activ": true
    }
  ]
}
```

- `textClauza` = text static (referința articolului)
- `textDinamic` = text cu `{0}`, `{1}` înlocuite cu valorile introduse de user
- `campuri` = câmpurile de input generate dinamic în `PunctModificareControl`
- `activ` = false ascunde clauza din dropdown fără să o șteargă

### Editare clauze
Din `ActAditionalForm`, butonul **"⚙ Editează clauze"** deschide `ClauzeEditorDialog`.  
Editorul permite add/edit/delete clauze și câmpuri. La salvare, scrie JSON-ul pe disk.  
La revenire din editor, toate `PunctModificareControl`-urile existente sunt actualizate via `SetClauze()`.

### PunctModificareControl
- Dropdown populat din JSON (doar clauzele cu `activ=true`) + "Text liber" built-in
- La selecție: generează câmpuri de input din `campuri[]`
- Preview calculat via `string.Format(textDinamic, values)`
- Buton "✎ Editează" pentru override manual al textului generat
- `GetPunct()` returnează `PunctModificare { Referinta, TextModificare }`

---

## Date ERP

### GetCimData (ErpDataProvider)
```sql
SELECT PEX.NUM03 AS NrCim, PEX.DATE03 AS DataCim
FROM PRSINEXTRA PEX WHERE PEX.PRSN = :prsnId
```

### GetCompanyData
Citit din Softone via XSupport, fallback din `PluginConfig`.

### OfficialName
```sql
SELECT NAME FROM USERS WHERE USERS = :userId
```

---

## Flow principal

```
ExecCommand(4000501)
  → RegistraturaService.Initialize(XSupport)
  → TryReadPrsn() — citeste PRSN curent
  → GetCimData(), GetCompanyData(), ReadOfficialName()
  → STA Thread:
      → SelectorDialog (4 categorii, carduri)
      → CreateForm(DocSelection/PvSelection)
          → model.CodInregistrare = RegistraturaService.CalculateCod(loginDate)
      → Form.ShowDialog()
          → "Previzualizează & Generează" → PdfPreviewForm
          → "Salvează PDF" → OnBeforeGenerate()
              → ConfirmareDialog (titlu, cod, data, tema)
              → RegistraturaService.Inregistreaza()
              → DoGenerateFinalPdf()
              → SuccessDialog (titlu, cod, data, tema)
              → Close()
```

---

## Arhitectura UI

### FormBase (abstract)
- Tema per categorie (`DocumentTheme`)
- Shell: header colorat + body scrollabil + footer cu butoane
- Buton unic: **"▶ Previzualizează & Generează"** (FlatStyle.Popup)
- Abstract: `BuildAngajatSection()`, `ValidateForm()`, `PopulateModel()`, `GetTemplatePath()`, `FillDocxTemplate()`, `DoGenerateFinalPdf()`
- Virtual: `OnBeforeGenerate()`, `PopulateMentiuni()`, `GetRegistraturaDate()`
- `CodInregistrareField` — TextBox readonly setat de subclase, actualizat după recalcul cod
- `_txtMentiuni` — TextBox protected, populat via `PopulateMentiuni()` după `PopulateModel()`
- `AddMentiuniSection(ref y)` — helper care adaugă secțiunea MENȚIUNI / OBSERVAȚII
- `ShowSuccessAndClose()` → afișează SuccessDialog, apoi Close()

### Sectiuni (`AddSectiune`)
Header-band cu bara laterala 4px (Accent), fundal AccentPal, text 10pt Segoe UI Semibold.

### Campuri readonly
`BackColor = Color.FromArgb(208, 213, 226)` — vizibil diferit față de fundal.

---

## Template placeholders

### Comune (toate documentele)
`{{NumeSalariat}}`, `{{CNP}}`, `{{NrCim}}`, `{{DataCim}}`, `{{Functie}}`,  
`{{NumeAngajator}}`, `{{CIFAngajator}}`, `{{ReprezentantLegal}}`, `{{AdresaCompanie}}`,  
`{{ZipCompanie}}`, `{{NrRegComertului}}`, `{{IbanCompanie}}`, `{{NrTelefonCompanie}}`,  
`{{EmailCompanie}}`, `{{WebsiteCompanie}}`, `{{CodInregistrare}}`, `{{ArticolCompartiment}}`,  
`{{ArticolContestatie}}`, **`{{MentiuniDocument}}`**

### IncetarePerioadaProba (specifice)
`{{DataDecizie}}`, `{{DataIncetare}}`, `{{NrNotificare}}`, `{{DataNotificare}}`

---

## Conventii importante

- Toate stringurile UI în **română**
- Nu se deschide PDF în Adobe după generare
- Cod înregistrare **readonly** în formulare (recalculat la schimbarea datei)
- `FillAngajator()` și `FillAngajatorFields()` = **no-op**
- `DocumentModelBase.CodInregistrare` și `MentiuniDocument` pe baza (nu pe subclase)
- `PvModelBase.MentiuniDocument` pe baza (nu pe subclase PV)
- `IPvBunuriModel` expune `MentiuniDocument` (nu `Mentiuni`)
- `ClauzeTextBuilder.cs` — păstrat dar neutilizat pentru clauze noi (înlocuit de JSON)

---

## Tabele Softone folosite

| Tabel | Scop |
|---|---|
| PRSN | Date angajat curent |
| PRSINEXTRA | NrCim (NUM03), DataCim (DATE03) |
| USERS | Nume official (oficiant) |
| CCCVREGISTRATURA | Registratura documente |
| CCCVDOCAUDIT | Audit modificari |
| CCCTIPDOCREG | Tipuri documente |
| CCCDCZCONTRACT | Istoric decizii |
| CCCACTEADITIONALE | Istoric acte aditionale |

---

## Redist necesar pe client

1. **.NET Framework 4.7.2** (inclus în Windows 10 1803+)
2. **VC++ Redistributable 2015-2022 x86** (pentru PdfiumViewer)
3. **pdfium.dll** — copiat lângă plugin
4. **Microsoft Word** (2013+) — pentru generarea PDF prin Word Interop

---

## Fisiere de configurare runtime

| Fisier | Locatie | Scop |
|---|---|---|
| `clauze_act_aditional.json` | `%TemplateDocsPath%` | Lista clauze act aditional, editabila din UI |
| Template-uri `.docx` | `%TemplateDocsPath%` | Template-uri Word per tip document |
