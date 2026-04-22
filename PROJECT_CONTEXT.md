# ActAditionalPlugin + PV Plugin — Rezumat proiect (actualizat 22.04.2026)

## Tehnologii
- C# .NET Framework 4.7.2, WinForms
- Softone ERP plugin (TXCode architecture), `[WorksOn("PRSNIN")]`
- DocumentFormat.OpenXml (Open XML SDK) — fill template .docx
- Word Interop (late binding) — conversie .docx → .pdf
- PdfiumViewer (NuGet) — preview PDF
- Cmd 4000501 = Decizii + Acte Adiționale
- Cmd 4000502 = Procese Verbale

## Culoare temă
`RGB(63, 129, 198)` — consistent în toată aplicația

## Variabile de sistem
- `RecruitmentDocsPath` — folderul candidaților (unde se salvează PDF-urile)
- `TemplateDocsPath` — folderul cu template-urile .docx

## Structura fișierelor

```
Models/
  DocumentModels.cs   ← modele decizii + acte
  PvModels.cs         ← modele PV (PvBunItem, PvEchipamenteModel, PvElecroniceModel, PvAutovehiculModel)
  PluginConfig.cs     ← GetTemplatePath(TipDocument/TipPV), date angajator hardcodate (fallback)

Services/
  WordHelper.cs           ← utilitare OpenXML+Word Interop partajate (MergeAndReplace, MakeRun,
                             MakeRunWithBreaks, BuildPosMap, BuildRuns, GetText,
                             ConvertToPdf, SanitizeFileName)
  TemplateEngine.cs       ← fill + PDF decizii/acte (foloseste WordHelper)
  PvTemplateEngine.cs     ← fill + PDF procese verbale (foloseste WordHelper)
  ErpDataProvider.cs      ← GetCimData + GetCompanyData
  ClauzeTextBuilder.cs    ← builder text clauze act adițional

UI/
  DocumentFormBase.cs      ← baza formulare decizii/acte
  DecizieFormBase.cs       ← baza decizii (TxtCodInregistrare, DtpDataDecizie)
  ActAditionalForm.cs      ← formular act adițional
  DeciziiSuspendare.cs     ← 5 clase suspendare
  DeciziiIncetare.cs       ← 3 clase încetare
  PunctModificareControl.cs← control clauze act adițional
  PdfPreviewForm.cs        ← preview PDF (PdfiumViewer), buton "Salvează PDF"
  DocumentSelectorDialog.cs← selector tip document (decizii/acte)
  PvSelectorDialog.cs      ← selector tip PV
  PvFormBase.cs            ← baza formulare PV
  PvForms.cs               ← PvEchipamenteForm, PvElectroniceForm, PvAutovehiculForm
  EchipamentItemControl.cs ← control item echipament (Denumire + Cantitate NumericUpDown + Preț)

PluginEntry.cs  ← ExecCommand 4000501+4000502, Mutex single instance, factories, datagrid INSERT
```

## Modele — ierarhie

### Decizii/Acte
```
DocumentModelBase                    ← PrsnId, NumeSalariat, CNP, Functie, NrCim, DataCim + angajator
├── ActAditionalModel                ← CodInregistrare, DataEmitereAct, DataVigoare, List<PunctModificare>
└── DecizieModelBase                 ← CodInregistrare, DataDecizie
    ├── DecizieModelCuCerere         ← NrCerere, DataCerere
    │   ├── SuspendareCresterecopilModel
    │   ├── SuspendareCresterecopilHandicapModel
    │   ├── SuspendareAcordPartiModel
    │   ├── SuspendareSiIncetareSuspendareModel
    │   ├── IncetareSuspendareModel
    │   └── IncetareDemisieModel
    └── direct din DecizieModelBase
        ├── SuspendareAbsenteNemotivateModel ← NrReferat, DataReferat, IntocmitDe, IncludeIncetare
        ├── IncetareExpirareModel
        └── IncetareDisciplinarModel
```

### PV
```
PvModelBase                ← CodInregistrare, DataPV, TipPredare, NumeSalariat, CNP, Functie + angajator
├── PvEchipamenteModel     ← List<PvBunItem> Bunuri, Mentiuni
├── PvElecroniceModel      ← List<PvBunItem> Bunuri, Mentiuni
└── PvAutovehiculModel     ← (detalii mai jos)
```

`PvBunItem`: `Nume`, `Cantitate`, `Pret`

### PvAutovehiculModel — câmpuri complete
```
// Al doilea predator (Art. 2)
NumePredator2, FunctiePredator2, CNPPredator2, CISeriaPredator2, CINrPredator2, DomiciliuPredator2

// Primitor (Art. 3) — Domiciliu preumplut din PRSN.ADDRESS
CISeria, CINr, Domiciliu

// Vehicul
MarcaAuto, NrInmatriculare, SerieSasiu, AnFabricatie, Kilometri
StareFunctionare, Avarii, AnvelopeFata, AnvelopeSpate, UzuraAnvelope

// Dotări (DA/NU)
TrusaSanitara, Extinctor, TriunghiReflectorizant, Cric, CheieRoti, VestaReflectorizanta
RoataRezervа, UzuraRoataRezervа    ← fără diacritice în proprietate și placeholder

// Documente
CertificatInmatriculare, AsigurareRCA, Rovinieta
```

**Important**: Placeholder-ele în template-uri NU au diacritice:
`{{RoataRezervа}}`, `{{UzuraRoataRezervа}}` (litera `a` normală, nu `ă`)

### TipPredare (enum)
`Simplu=0, Exploatare=1, Mentenanta=2, Custodie=3, Administrare=4, Receptie=5, Relocare=6, CasareScoatere=7`

## Cod înregistrare
- Format: `YYddd/#nr` (ex: `26001/1`)
- Validat cu regex `^(\d{2})(\d{3})/(\d+)$`
- În numele fișierului: `/` → `-` via `WordHelper.SanitizeFileName`

## Naming PDF
```
Act_Aditional_{CodInregistrare}_{Data}.pdf
Decizie_{Tip}_{CodInregistrare}_{Data}.pdf    ex: Decizie_Incetare_Demisie_26063-2_04.03.2026.pdf
PV_{Tip}_{CodInregistrare}_{Data}.pdf         ex: PV_Autovehicul_26001-1_20.04.2026.pdf
```

## Template-uri (.docx în %TemplateDocsPath%)
### Decizii/Acte
- `ActAditional_template.docx`
- `template_suspendare_crestere_copil.docx`
- `template_suspendare_crestere_copil_handicap.docx`
- `template_suspendare_absente_nemotivate.docx`
- `template_suspendare_absente_nemotivate_fara_incetare.docx`
- `template_suspendare_acord_parti.docx`
- `template_suspendare_si_incetare_suspendare.docx`
- `template_incetare_suspendare.docx`
- `template_incetare_demisie.docx`
- `template_incetare_expirare_termen.docx`
- `template_incetare_disciplinar.docx`

### PV
- `template_pv_echipamente.docx` ← folosit și pentru Electronice
- `template_pv_autovehicul.docx`

## Placeholder-e comune (toate documentele)
```
{{NumeAngajator}}, {{CIFAngajator}}, {{AdresaCompanie}}, {{ZipCompanie}},
{{NrRegComertului}}, {{IbanCompanie}}, {{NrTelefonCompanie}},
{{EmailCompanie}}, {{WebsiteCompanie}}, {{ReprezentantLegal}},
{{FunctieReprezentant}}, {{CodInregistrare}}, {{NumeSalariat}},
{{CNP}}, {{Functie}}, {{NrCim}}, {{DataCim}}
```

### Placeholder-e PV Autovehicul
```
{{TipPredare}}, {{DataPV}},
{{NumePredator2}}, {{FunctiePredator2}}, {{CNPPredator2}},
{{CISeriaPredator2}}, {{CINrPredator2}}, {{DomiciliuPredator2}},
{{CISeria}}, {{CINr}}, {{Domiciliu}},
{{MarcaAuto}}, {{NrInmatriculare}}, {{SerieSasiu}},
{{AnFabricatie}}, {{Kilometri}}, {{StareFunctionare}}, {{Avarii}},
{{AnvelopeFata}}, {{AnvelopeSpate}}, {{UzuraAnvelope}},
{{TrusaSanitara}}, {{Extinctor}}, {{TriunghiReflectorizant}}, {{Cric}},
{{CheieRoti}}, {{VestaReflectorizanta}}, {{RoataRezervа}}, {{UzuraRoataRezervа}},
{{CertificatInmatriculare}}, {{AsigurareRCA}}, {{Rovinieta}}
```
**Uppercase** aplicat în `PvTemplateEngine.AddAutovehiculPlaceholders` pentru:
NumePredator2, FunctiePredator2, CISeriaPredator2, DomiciliuPredator2, CISeria, Domiciliu,
MarcaAuto, NrInmatriculare, SerieSasiu. CNP-urile și câmpurile descriptive NU sunt uppercase.

Art. 5, 6, 7 sunt hardcodate în template — nu sunt placeholder. Art. 7 conține `{{NumeSalariat}}`.

## ERP Data
- `GetCimData(prsnId, xSupport)`:
  - câmp: `PRSEXTRA.CCCNUM06` (NrCim — tip int în ERP custom) / `DATE05` (DataCim)
  - conversie: `Convert.ToInt32(nrCimObj).ToString()` — evita valori float reziduale
  - fallback la `string.Empty` dacă câmpul e null/DBNull
- `GetCompanyData(xSupport)` → SQL JOIN COMPANY+COMPANYEXT+PRSN+SPECIALTY
- `officialName` → `USERS.NAME` via `XSupport.ConnectionInfo.UserId`
- `adresaPrimitor` → `SELECT P.ADDRESS FROM PRSN P WHERE P.PRSN={id} AND P.COMPANY={companyId}`
  preumple `Domiciliu` în `PvAutovehiculModel`
- Toate SQL-urile folosesc `int companyId = xSupport.ConnectionInfo.CompanyId` — niciun ID hardcodat
- Fallback la `PluginConfig` hardcodat dacă SQL eșuează

## PluginEntry — arhitectură internă

### Clasa PrsnInfo (nested private)
```csharp
private class PrsnInfo { int PrsnId; string NumeSalariat; string CNP; string Functie; }
```

### Metode helper extrase
- `TryReadPrsn(int companyId) → PrsnInfo?` — citire PRSN table + SQL SOTITLENAME; returnează null dacă datele lipsesc
- `ReadOfficialName() → string` — USERS.NAME pentru userul logat
- `ReadAdresaPrimitor(prsnId, companyId) → string` — PRSN.ADDRESS pentru Domiciliu PV Autovehicul
- `SetFormIcon(Form)` — static, aplică softone.ico pe orice formular
- `ApplyCompanyData(DocumentModelBase, ErpCompanyData)` — populează 11 câmpuri angajator sau fallback PluginConfig
- `ApplyCompanyData(PvModelBase, ErpCompanyData)` — idem pentru modele PV

### Semnături factories
```csharp
private static DocumentModelBase CreateModel(TipDocument tip, PrsnInfo prsn,
    ErpCimData cimData, string officialName, ErpCompanyData companyData)

private static PvModelBase CreatePvModel(TipPV tip, PrsnInfo prsn,
    ErpCompanyData companyData, string adresaPrimitor)

private static Form CreatePvForm(TipPV tip, PvModelBase model, Action<PvModelBase> onPdfGenerated)
```

## Single Instance
- `Mutex` named `"ActAditionalPlugin_SingleInstance"`
- `_activeForm` setat pe selector + formular + re-selector

## WordHelper — utilitare partajate (Services/WordHelper.cs)
Clasă `internal static` folosită de ambele engines:
- `SanitizeFileName(string)` — `/` → `-`, caractere invalide → `_`
- `GetText(OpenXmlElement)` — concatenează toate Text descendants
- `MergeAndReplace(Paragraph, Dictionary)` — merge runs + replace cu păstrare RunProperties;
  skip dacă niciun placeholder găsit și textul nu conține `{{`
- `MakeRun(string, RunProperties)` — Run cu Space=Preserve dacă text începe/termină cu spațiu
- `MakeRunWithBreaks(string, RunProperties)` — `\r\n` / `\n` → `<w:br/>` în același Run
- `BuildPosMap(segments)` — hartă poziție → RunProperties pentru rebuild formatting
- `BuildRuns(orig, replaced, map, posToRpr)` — reconstruiește runs cu formatting corect după replace
- `ConvertToPdf(docxPath, pdfPath)` — Word Interop late binding, ExportAsFixedFormat=17

## TemplateEngine — funcționalități cheie
- `GeneratePdf`, `FillTemplatePublic`, `ConvertToPdfPublic` — entry points
- `FillTemplate` → `ExpandModificariTable` + `InjectArticoleFinal` + `BuildPlaceholders` + `ReplaceInBody`
- `ExpandModificariTable`: clonat rând template, replace per rând via `WordHelper.MergeAndReplace`
- `InjectArticoleFinal`: numără Art.N în text, calculează `_artCompartiment` / `_artContestatie`
- `[ThreadStatic]` pe `_artCompartiment` / `_artContestatie`
- `ReplaceInBody` → `WordHelper.MergeAndReplace` pe fiecare paragraf
- Toate metodele de helpers OpenXML delegate la `WordHelper`

## PvTemplateEngine — funcționalități cheie
- `ExpandBunuriTable`: găsește tabelul cu `{{BunNume}}`, clonează rândul template per bun
- `AddAutovehiculPlaceholders`: injectare câmpuri autovehicul cu uppercase selectiv
- `ReplaceInBody` → `WordHelper.MergeAndReplace` pe fiecare paragraf
- Toate metodele de helpers OpenXML delegate la `WordHelper`

## DocumentFormBase — mecanisme cheie
- `public static Action<DocumentModelBase> OnDocumentGenerated` — callback setat de `PluginEntry`
  înainte de deschiderea thread-ului STA; invocat după fiecare PDF generat cu succes
- `OnDocumentGenerated?.Invoke(_model)` în `BtnGenPdf_Click` și `BtnPreview_Click` (după confirmare)
- Șters în `finally` din thread-ul STA
- `FillAngajator(DocumentModelBase)` — completează câmpurile angajator din `PluginConfig` (fallback UI)

## PvFormBase — mecanisme cheie
- Constructor: `PvFormBase(PvModelBase model, string titlu, Action<PvModelBase> onPdfGenerated = null)`
- `_onPdfGenerated?.Invoke(_model)` după fiecare PDF generat cu succes (gen + preview confirmed)
- `MakeTipPredareCombo()` + `GetTipPredare(ComboBox)` — dropdown cu 8 tipuri
- `AddSectiune(ref y, height)` → FlowLayoutPanel alb cu border
- `AddRow(parent, colPercents[])` → TableLayoutPanel fără param `top` (diferit față de DocumentFormBase)

## ActAditionalForm — scroll fix
- `_pnlModificari.AutoScroll = true` — scroll intern când punctele depășesc înălțimea panoului
- Anchors: `Left | Right | Top | Bottom` — se extinde cu fereastra

## Inserare în datagrid-uri ERP după generare PDF

### PV → CCCPVEMISE (via `AddPvToDatagrid` în PluginEntry)
```
PRSN, CCCTRNDATE, CCCPVTYPE (int: 1=Echipamente, 6=Electronice, 2=Autovehicul),
CCCPVNAME (mentiuni sau "-"), CCCPVNUMBER (nr după "/" din CodInregistrare)
```
Mecanism: `pvTable.Current.Append()` → setare câmpuri → `pvTable.Current.Post()` → `XModule.Exec("Button:Save")`

### Decizii/Acte → tabele custom ERP (via `AddDocumentToDatagrid` în PluginEntry)
- **Act Adițional** → `CCCACTEADITIONALE`:
  `COMPANY, PRSN, CCCCODINREG, CCCNRINREG, CCCIDCONTRACT (NrCim), CCCDATAINREG, CCCDATAVIGOARE, CCCDOCUMENTSTATUS=1`
- **Decizii** → `CCCDCZCONTRACT`:
  `COMPANY, PRSN, CCCCODINREG, CCCNRINREG, CCCIDCONTRACT (NrCim parsed int), CCCDATAINREG, CCCDATAVIGOARE (DataDecizie), LINENUM=1, CCCSTATUS=1, CCCTIPDCZ (text tip)`
- `int companyId = XSupport.ConnectionInfo.CompanyId` declarat local în fiecare metodă de insert
- `SafeSetField(dynamic table, string field, object value)` — try/catch pentru câmpuri opționale
- `GetSoftoneLoginDate()` — reflection pe `ConnectionInfo.LoginDate`
- `GetPvNumber(codInregistrare)` — extrage nr după `/`

## Preferințe cod
- Minimal diffs când e posibil, fișiere complete pentru schimbări structurale
- Comentarii concise în română
- UI strings în română
- Placeholder-e fără diacritice
- Fără `PdfGenerator.cs` — înlocuit complet de `TemplateEngine`
- Niciun ID hardcodat în SQL — totul prin `companyId = xSupport.ConnectionInfo.CompanyId`
