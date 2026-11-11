# CONTEXT — ActAditionalPlugin (Generator Documente HR)

Document pentru un agent nou care preia proiectul în VS Code. Citește-l integral înainte de a modifica ceva. Ultima actualizare: 2026-11-11.

---

## 0. TL;DR

- Plugin Softone ERP (C# .NET Framework 4.7.2, WinForms) care generează documente HR (acte adiționale, decizii, cereri, PV-uri, adeverințe) în PDF.
- **Data-driven:** un document nou = un JSON (formular + hooks) + un DOCX (template Word cu `{{Placeholder}}`). Codul se atinge doar când lipsește o capabilitate (tip de câmp, hook, comportament de listă).
- Utilizatorul (Tiberiu, dezvoltator, vorbește română) face DOCX-ul. Tu faci JSON-ul, repari DOCX-ul (placeholder-e rupte etc.) și patch-uiești codul minimal când e nevoie.
- ⛔ **REGULĂ IMPORTANTĂ (2026-11-11): lucrezi DOAR în folderul proiectului** (`D:\Visual Studio Proiecte Vatra\GeneratorDocumente`). Sub nicio formă nu citești, nu modifici, nu muți și nu ștergi nimic în folderul real al șabloanelor (`TemplateDocsPath`, `D:\VATRA DOMNEASCA\...`), în `RecruitmentDocsPath`, în `C:\Program Files (x86)\Soft1\` sau în orice alt loc din afara proiectului — nici pentru verificări. Dacă ai nevoie de un JSON/DOCX, cere-i utilizatorului să-l pună într-un folder din proiect.
- Se testează funcțional DOAR de utilizator, în Softone (UI-ul nu rulează fără Softone). **Compilarea se poate face local** (VS 2026 / MSBuild), cu ieșirea în `bin/`/`obj/` din proiect (ignorate de git):
  `"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" ActAditionalPlugin.csproj /p:Configuration=Release /p:Platform=x86 /p:FodyGenerateXsd=false /v:minimal`
  Compilează după fiecare patch și spune explicit ce n-a fost testat în Softone.
- Preferințe de comunicare: direct, concis, fără narare a gândirii, răspunsuri în română (uneori amestec RO/EN), cerințele vin în loturi într-un singur mesaj. Nu vrea modificări de cod pentru „nume fișier PDF” (respinse de 2 ori — nu le mai propune). Dacă greșești, el editează promptul și regenerează; nu se așteaptă la corecturi în lanț.

---

## 1. Stack și mediu

| Element | Detaliu |
|---|---|
| Limbaj / runtime | C#, .NET Framework 4.7.2, WinForms |
| ERP | Softone (Soft1), plugin încărcat ca DLL. DLL-ul instalat stă în `C:\Program Files (x86)\Soft1\Soft1 - Vatra\` și `Soft1 - Ice\`; `Softone.Lib` se referă din `Soft1 - Vatra`. Post-build-ul de copiere e gol în `.csproj` (scos de utilizator) — copierea o face el. |
| Word | Open XML SDK (`DocumentFormat.OpenXml`) pentru completare; Word Interop (late-bound, `WordHelper.ConvertToPdf`) pentru DOCX → PDF |
| Preview PDF | PdfiumViewer (în formular, split 40/60) |
| JSON | Newtonsoft.Json |
| Variabile de sistem | `TemplateDocsPath` (rădăcina șabloanelor; fallback `Templates/` lângă DLL), `RecruitmentDocsPath` (rădăcina dosarelor PDF ale angajaților) |
| Companie | VATRA DOMNEASCĂ SRL. Datele companiei vin din ERP (`ErpDataProvider.GetCompanyData`: COMPANY + COMPANYEXT); doar `FunctieReprezentant` vine din `PluginConfig.cs`. Celelalte proprietăți din `PluginConfig` (nume, CIF, IBAN…) nu sunt folosite. |
| Șabloane reale | `TemplateDocsPath` = `D:\VATRA DOMNEASCA\SemnaturiElectronice - Documents\Sabloane Acte si Decizii` (folder sincronizat, folosit în producție — ⛔ INTERZIS: nu-l accesa deloc, vezi §0) |
| Git | ramura de lucru `redefinire-proiect`, ramura principală `master` |

Constrângeri .NET/Softone importante:
- **Fără metode generice proprii cu constrângeri de tip** (ex. `GetAncestor<T>`) — provoacă erori de încărcare a DLL-ului în Softone. Folosește `as`-cast și metode negenerice (ex. `GetParentTableRow()`). Apelurile LINQ/OpenXML obișnuite (`Descendants<Text>()`) sunt OK.
- Sintaxă C# moderată (pattern matching `is X x` e deja folosit), dar rămâi conservator.
- Fișierele `.cs` din proiect sunt în general **UTF-8 cu BOM și CRLF**. Excepții: `PluginEntry.cs` și `WordHelper.cs` sunt fără BOM, iar `WordHelper.cs` e LF pe disc. Păstrează formatul existent al fiecărui fișier (altfel git vede tot fișierul modificat).
- JSON-urile se salvează UTF-8 fără BOM. Diacriticele sunt permise în `title`, `label`, `options` etc. În **placeholder-ele din DOCX NU se folosesc diacritice**.
- Acces la dataset Softone: `ds[i, "COLOANA"]`. SQL se rulează prin `BulkContext.XSupport` (`GetSQLDataSet()` pentru citire, `ExecuteSQL()` pentru scriere).

---

## 2. Structura repository-ului

```
ActAditionalPlugin.csproj / GeneratorDocumente.slnx / packages.config
PluginEntry.cs                 // puncte de intrare Softone
Models/
  DocumentDefinition.cs        // DocumentDefinition, SectionDefinition, FieldDefinition, ItemFieldDefinition (schema JSON)
  PluginConfig.cs              // TemplatesRoot (env TemplateDocsPath) + date companie hardcodate
  PersonInfo.cs                // persoana selectabilă (person_picker)
  ClauzeConfig.cs              // doar pentru Act Adițional (clauze_editor)
Services/
  DocumentRegistry.cs          // scanează TemplateDocsPath: categorie = subfolder, document = *.json + *.docx; CategoryOrder
  DynamicTemplateEngine.cs     // FillTemplate: placeholder-e, liste dinamice, hooks on_generate, PDF, folder angajat
  HookRegistry.cs              // hooks (vezi §6)
  WordHelper.cs                // MergeAndReplace, colorare linii punctate, conversie PDF
  RegistraturaService.cs       // cod înregistrare YYddd/NR + inserturi în tabele specifice
  ErpDataProvider.cs           // SQL: date angajat/CIM/companie (PRSN, PRSEXTRA, DEPART, COMPANY, COMPANYEXT)
  ClauzeService.cs, BulkContext.cs
UI/
  SelectorDialog.cs            // alege angajat + document (carduri pe categorii), buton „Dosar Personal”
  DynamicForm.cs               // formularul generat din JSON + DynamicListRow, CheckTextBox, DateRangeBox, NoScrollComboBox
  DocumentTheme.cs             // teme de culoare pe categorie
  PersonPickerDialog.cs, ClauzeEditorDialog.cs, PunctModificareControl.cs, ConfirmareDialog.cs, SuccessDialog.cs
Resources/                     // png-uri (inclusiv dosar_personal.png), logo, .docx legacy
GHID_ADAUGARE_DOCUMENT_NOU.md  // ghid de bază (secțiunile de mai jos îl actualizează)
```

Puncte de intrare Softone: `S1` (static, `xSupp`, `[WorksOn("GENERAL")]`), `ProgramGeneral` cmd 4000502 `[GENERAL]`, `Program` cmd 4000501 `[PRSNIN]`; Form DLL: `ActAditionalPlugin.dll;SelectorDialog` (constructor fără parametri).

Șabloanele (JSON + DOCX) NU stau în repo-ul de cod, ci în folderul indicat de `TemplateDocsPath` (structura din §3). Tu lucrezi doar pe copii pe care utilizatorul le pune într-un folder din proiect — întreabă-l unde; nu accesa niciodată folderul real (§0).

---

## 3. Fluxul aplicației

1. `DocumentRegistry.Initialize(TemplatesRoot)` scanează subfolderele. **Categoria = numele folderului.** Fiecare `*.json` valid (cu `title`, `template_file` care există) devine un `DocumentDefinition`. Documentele se sortează după `order` (>0 primele, crescător), restul alfabetic. Categoriile se sortează după `CategoryOrder` (hardcodat în `DocumentRegistry.cs`); cele necunoscute merg la final.
2. `SelectorDialog`: utilizatorul alege angajatul (buton custom-painted: badge roșu pulsând când nu e ales nimeni; albastru cu inițiale după selecție), apoi cardul documentului. Buton „Dosar Personal” (lângă angajat, apare doar cu angajat selectat) deschide folderul PDF al angajatului.
3. `DynamicForm` construiește formularul din `sections[]`, aplică `depends_on`, rulează hooks `on_open` / `on_change`.
4. La „Generează PDF” (`DynamicForm.OnGenerate`): validare (`required`), hooks `on_generate`, recalculare cod, confirmare, apoi **întâi PDF-ul, abia apoi registratura**:
   - codul se recalculează chiar înainte de generare; după generare se verifică `RegistraturaService.CodExista` — dacă între timp codul a fost luat de altcineva, PDF-ul se șterge și se regenerează cu codul următor (max 3 încercări);
   - `Inregistreaza` (CCCVREGISTRATURA + CCCVDOCAUDIT) + `InregistreazaTabelaSpecifica`; dacă înregistrarea eșuează, PDF-ul generat se șterge;
   - la orice eroare formularul rămâne deschis, codul din antet se reactualizează, iar o nouă încercare nu dublează înregistrarea.
   După succes aplicația revine la `SelectorDialog`. Rămâne o fereastră de câteva milisecunde între verificare și INSERT (fără index unic în DB nu se poate elimina complet).
5. **Nume PDF:** `{Titlu}_{CodInregistrare}_{dd-MM-yyyy}.pdf`, cu `SanitizeFileName` (`/` → `-`). Titlul din JSON apare identic în selector, în registratură și în numele fișierului. Titlurile au diacritice și asta e intenționat.
   ⚠️ **Titlul + paginarea sunt folosite de semnăturile electronice:** `{TemplateDocsPath}\Sabloane Semnaturi Electronice\*.json` (JSON-uri ale altui sistem, ignorate de `DocumentRegistry` fiindcă n-au `title`) găsesc PDF-urile după tipar de nume (`FileSystemBlock.FilePattern`, ex. `"Cerere Demisie fără Preaviz_*.pdf"`) și pun semnăturile la coordonate fixe (`Page`, `X/Y/W/H`). Redenumirea unui titlu sau schimbarea paginării DOCX poate rupe semnarea — întreabă utilizatorul de șablonul de semnătură corespunzător (nu-l căuta tu în folderul real). Fără șablon de semnătură (2026-11-11): Adeverință de Vechime, Convocare Cercetare, Decizie Constituire Comisie, PV Cercetare.
6. **Folder angajat:** `RecruitmentDocsPath\{PrsnId} - {Nume}\`. Folderul existent se caută după prefixul `"{PrsnId} - "` (`DynamicTemplateEngine.ResolvePersonFolder(prsnId, numeSalariat, createIfMissing)`), NU după nume (numele se poate schimba în Softone după parsarea buletinului, cu diacritice). Dacă nu există, se creează cu numele curent. Metoda e folosită și de butonul „Dosar Personal”.

---

## 4. Schema JSON (stare curentă, inclusiv extensiile din sesiunile recente)

### 4.1 Root
`title`, `template_file`, `registratura` (bool), `registratura_date_field` (cheia unui câmp `date` → devine data de înregistrare și intră în numele PDF), `registratura_tip_doc_pk` (int), `order` (int), `sections[]`, `hooks[]`.
Nu există `category`, `id` sau `tip_doc_pk` la root. Câmpurile folosesc `key` (nu `name`).

### 4.2 Secțiune
`title`, `height` (px; pentru liste cu `auto_height` e ignorat practic), `fields[]`, **`depends_on` / `depends_value`** (NOU): secțiunea apare în formular doar când condiția e adevărată; cele de dedesubt urcă (`ReflowSections` în `DynamicForm`). Nu afectează documentul generat.

### 4.3 Câmp (`fields[]`)
`key`, `label`, `type`, `label_width_percent` (suma pe rând = 100), `required`, `placeholder`, `default`, `options` (combo), `maps` (person_picker), `initial_rows` + `item_fields` (dynamic_list), `depends_on` / `depends_value`.

Tipuri: `text`, `multiline` (140px), `date` (dd.MM.yyyy), `datetime` (cu oră, `dd.MM.yyyy HH:mm`), `readonly`, `number` (0–999), `combo`, `person_picker`, `dynamic_list`, `clauze_editor`.
`options_sql` (combo din SQL) și tipul `expand_table_row` (+ `table_marker`, `ExpandTableRows`) au fost **eliminate** pe 2026-11-11: nu erau implementate în formular și niciun JSON nu le folosea. Un JSON care le-ar conține tot se încarcă (Newtonsoft ignoră proprietățile necunoscute), dar fără efect.

`person_picker`: la selecție, `{{CheiaPickerului}}` = `NumeComplet`; un `maps` pe aceeași cheie (ex. `"NumePrenumeInlocuitor": "NumeComplet:upper"`) are prioritate. Valoarea pickerului stă în `_formValues` (controlul e doar un buton) și nu mai e ștearsă la `on_change`; `required` pe picker verifică selecția.

`depends_on` / `depends_value`: editabilitate dinamică (câmpul devine disabled/readonly când condiția nu e îndeplinită; AND cu `|`, ex. `"depends_on":"A|B","depends_value":"x|y"`). Câmpurile `required` inactive sunt ignorate la validare. Se (re)evaluează după `on_open` și `on_change` (`ApplyFieldDependencies`).
Combo-urile declanșează `on_change` (`SelectedIndexChanged → RunHooks("on_change")`).
`SetDefault` acceptă valorile speciale `{year}` și `{today}`.

### 4.4 Opțiuni pe `dynamic_list` (NOU)
| Opțiune | Efect |
|---|---|
| `collapsible` | rândurile se restrâng/extind (click pe antet); antet cu număr + rezumat; la adăugare, celelalte se restrâng (acordeon) |
| `summary_fields` | lista cheilor din `item_fields` afișate în antet când rândul e restrâns (separate prin ` · `) |
| `auto_height` + `max_height` | panoul listei își ajustează înălțimea după conținut (max implicit 560); secțiunile de dedesubt se reașază |
| `hide_header` | nu desenează sub-antetul propriu al listei (titlul secțiunii e deja deasupra; evită titlul dublu) |
| `add_button_label` | textul butonului „+ …” (implicit „Adaugă ” + label); lățimea butonului se potrivește pe text |
| `edit_mode: "dialog"` (2026-11-11) | elementele se adaugă/editează într-o fereastră separată (`ListItemDialog`, câmpurile din `item_fields` cu `group`-urile lor, mai aerisite); în formular apar doar carduri needitabile (`ListItemCard`): restrânse implicit (număr + `summary_fields`), extensibile pentru a vedea toate valorile, cu „✎ Editează” și „✕” (cu confirmare). Lista pornește GOALĂ (`initial_rows`/`collapsible` ignorate). „Anulează” în dialog nu modifică elementul (se editează o copie — `DynamicListRow.CopyStateFrom`). Funcționează cu `auto_height`/`max_height`, `hide_header`, `required`. |
| `dialog_title` | titlul ferestrei pentru `edit_mode: "dialog"` (implicit = `label`); prefixat automat cu „Adăugare — ” / „Editare — ” |
| `initial_items` | listă de elemente precompletate la deschidere (`[{ "cheie_item": "valoare", ... }]`); înlocuiește `initial_rows`. Merge în ambele moduri. Valorile pot conține token-uri cu datele angajatului: `{NumeSalariat}`, `{CNP}`, `{Functie}`, `{NumeDepartament}`, `{NrCim}`, `{DataCim}`, `{SerieCI}`, `{NrCI}`, `{Domiciliu}`, `{today}` (`DynamicForm.ResolveCommonTokens`). Datele se dau `dd.MM.yyyy`; valorile care nu se potrivesc (opțiune inexistentă, dată goală) sunt ignorate. |

Cardurile listei `dialog`: iconiță de extindere = săgeată simplă „V” (jos = restrâns, sus = extins; modelul ales de utilizator, fără chenar), titlu (rezumatul) bold + UPPERCASE. Sub o listă cu `auto_height` se lasă 40px spațiu până la secțiunea următoare (`ReflowSections`). Panoul listei `dialog` e o casetă albă cu chenar subțire (ca secțiunile normale, `PaintBorder`), iar cardurile stau în interior cu margini egale (`DialogListPadX`=12, `DialogListPadY`=10, `DialogListGap`=8) — fără chenarul mov dublu al listelor clasice.

Fără aceste opțiuni listele se comportă exact ca înainte (documentele vechi nu sunt afectate). O listă cu `label` gol nu mai desenează antet.

### 4.5 `item_fields[]` (câmpurile dintr-un rând de listă)
`key`, `label`, `type`, `width_percent` (rândurile se umplu până la 100, apoi trec pe rândul următor), `placeholder`, `maps`, `options`, `default`, `max`, `decimals`, `format`, `check_label`, `group`.

Tipuri suportate în rând:
- `text` (cu `placeholder` gri sau `default`), `person_picker` (cu `maps`)
- `number`: implicit 0–9999, zecimale 0. Cu `max`, `decimals`, `format` (ex. `"#,##0.##"`, formatat cu cultura `ro-RO` → `4.325`).
- `combo`: `options`, `default`. Folosește `NoScrollComboBox` (stil standard cu chenar, fără schimbare de valoare la scroll).
- `date`: DateTimePicker `dd.MM.yyyy`. **`maps`** pe `date` = „cheie placeholder → format .NET” și împarte data în bucăți per rând, ex. `{"AnulMutatie":"yyyy","LunaMutatie":"MM","ZiMutatie":"dd"}` (cultura `ro-RO`).
- `date_range`: două date, valoare `"dd.MM.yyyy - dd.MM.yyyy"` (la schimbarea începutului, sfârșitul se aliniază dacă era mai mic).
- `check_text`: bifă + câmp text activ doar când e bifat; valoare = textul dacă e bifat, altfel `""`. `check_label` = textul bifei (implicit „Da”).
- `group`: câmpul începe un rând nou și deasupra apare un mic titlu cu linie separatoare (valabil până la următorul `group`).

- `on_select` (pe `combo`): `{ "Opțiune": { "cheie_item": "valoare", ... } }` — la alegerea opțiunii se completează alte câmpuri din ACELAȘI rând (token-uri ca la `initial_items`). Combo-urile și datele se setează mereu; text / number / check_text doar dacă sunt goale (nu suprascrie ce a scris operatorul). Se aplică și la crearea unui element nou, pentru opțiunea implicită (`ApplyOnSelectAll`); NU se aplică la `initial_items` și la copierea pentru editare.

Automat în orice rând: `{{NrCrt}}` = numărul rândului (disponibil în clone, dar nu e marker de expandare).

### 4.6 Valori pentru `maps` la `person_picker`
`NumeComplet`, `Nume`, `Prenume`, `CNP`, `Functie`, `CodCor`, `NrCim`, `DataCim`, `NumeDepartament`, `SerieCI`, `NrCI`, `Domiciliu`, `NumeComplet_Functie`; sufix `:upper` pentru majuscule (ex. `"NumeComplet:upper"`) — **doar la nivel de formular**; în `item_fields` (`DynamicListRow`) sufixul nu e suportat și dă valoare goală. În `dynamic_list`, valorile mapate fără control propriu se păstrează în `_extraValues` și sunt disponibile ca placeholder per rând.

---

## 5. Placeholder-e automate (nu se pun în `fields[]`)

Angajat (persoana selectată): `NumeSalariat`, `CNP` (PRSN.AFM), `Functie` (PRSN.SOTITLENAME), `NumeDepartament` (JOIN DEPART), `NrCim` (string, din `PRSEXTRA.CCCVARCHAR05` — mapare ulterioară; vechiul `NUM03` nu mai e valabil), `DataCim` (dd.MM.yyyy), `SerieCI` / `NrCI` (parsate din `PRSN.IDENTITYNUM` prin `ParseIdentityNum`), `Domiciliu` (`PRSN.ADDRESS`), `NumarTelefonSalariat` (`PRSN.MOBILEPHONE`), `EmailSalariat` (`PRSN.EMAIL`), `CodInregistrare`.
Companie: `NumeAngajator`, `CIFAngajator`, `ReprezentantLegal` (COMPANYEXT ACCTOFFICE=2), `FunctieReprezentant` (**din `PluginConfig`, hardcodat**), `AdresaCompanie`, `ZipCompanie`, `NrRegComertului`, `IbanCompanie`, `NrTelefonCompanie`, `EmailCompanie`, `WebsiteCompanie`. (Telefon/email de angajat există; cele de companie sunt separate.)
Din formular: orice `key` din `fields[]`, plus cheile din `maps` și din hooks (`target_field`).
**`DataInreg` NU e automat.** Dacă header-ul DOCX are `{{DataInreg}}`, în JSON trebuie un câmp `date` cu `key: "DataInreg"`, iar `registratura_date_field` pointează la el (cum e la Adeverință).
`CodInregistrare` apare automat în header-ul formularului — nu se pune în `sections`.
`PrePopulateReadonlyCommonFields` (DynamicForm) precompletează câmpuri **readonly** ale căror `key` coincide cu o valoare comună (NumeAngajator, AdresaCompanie, NrRegComertului, CIFAngajator, ReprezentantLegal, NumeSalariat, CNP, Functie, NumeDepartament, NrCim, SerieCI, NrCI, Domiciliu, TelefonSalariat/EmailSalariat). Folosește-l pentru formulare „doar confirmare”.

---

## 6. Hooks (`hooks[]`: `{ "on": "...", "handler": "...", "params": {...} }`)

Evenimente: `on_open`, `on_change`, `on_generate`. Ordinea din JSON contează la `on_generate` (ex. `ConcatList` înainte de `BlankIfNotEquals` care golește rezultatul).

| Handler | Params | Efect |
|---|---|---|
| `SetDefault` | `field`, `value` (`{year}`, `{today}`) | setează câmpul la deschidere dacă e gol |
| `SqlOnOpen` / `SqlOnGenerate` | `query` (`{PrsnId}`, `{CompanyId}`), `column`, `target_field` | rulează SQL, pune rezultatul în câmp |
| `CalcDataSfarsit` | `start_field`, `luni_field`, `target_field` | start + luni |
| `CalcDataSfarsitDinCNP` | `cnp_field`, `ani_field`, `target_field` | data nașterii din CNP + ani |
| `CalcPerioadaSuspendare` | `ani_field`, `target_field` | 1 → „1 an”, 2 → „2 ani” |
| `ConcatList` | `source_field` (cheia listei), `item_key` (cheia din `item_fields`, NU cheia pickerului), `target_field`, `separator`, `distinct` ("true") | concatenează valori dintr-o listă |
| `InjectArticoleFinal` | — | calculează `{{ArticolCompartiment}}` / `{{ArticolContestatie}}` (ultimul Art.N + 1/+2); rulează în `FillTemplate` prin `RunSingleHook` |
| `SetIfEquals` | `source_field`, `equals`, `target_field`, `value_if_true`, `value_if_false` | setează o valoare după egalitate (ex. bifă `▣`/`□`) |
| `BlankIfNotEquals` | `source_field`, `equals`, `target_field`, `blank` | dacă sursa ≠ `equals`, suprascrie ținta cu `blank` (linie punctată albastră) |
| `CalcZileLucratoare` | `start_field`, `end_field`, `target_field` | zile lucrătoare (exclude weekend) |
| `FormatPerioada` | `start_field`, `end_field`, `tip_field`, `tip_ore_value`, `target_field` | combină două câmpuri `datetime` într-un interval text, cu/fără oră |

Convenții vizuale în documentul generat:
- **Linie punctată albastră:** valoarea exactă `_ _ _ _ _ _ _ _ _ _ _ _ _ _ _` (`WordHelper.BlankLineMarker`) se colorează `0070C0`. Se obține cu `BlankIfNotEquals`. Regula utilizatorului: textul variantelor/paragrafelor neselectate apare ÎNTOTDEAUNA în document; doar câmpurile necompletate devin linii punctate.
- **Bife:** `□` (nebifat) și `▣` (bifat) ca TEXT (utilizatorul le vrea unicode, cu „căsuță încărcată”, nu pătrat plin `■`).

---

## 7. Reguli pentru template-urile DOCX (capcane reale întâlnite)

1. **Placeholder rupt.** `MergeAndReplace` concatenează textul tuturor run-urilor directe ale paragrafului, deci un `{{...}}` rupt în mai multe run-uri SE ÎNLOCUIEȘTE corect. Rămâne neînlocuit doar dacă e în run-uri imbricate (`w:hyperlink`, `w:ins`, `w:sdt`, `w:smartTag`), dacă are acolade rupte (`{{X}`) sau spații în interior (`{{ DataPV }}` — a existat la PV Autovehicul, corectat).
2. **`WordHelper.MergeAndReplace` reconstruiește paragraful din text.** Într-un paragraf care conține `{{...}}` se PIERD: `<w:br/>` (salturi de rând), `<w:tab/>`, desene/textbox-uri, orice run non-text. (Un `br`/`tab` la începutul sau sfârșitul paragrafului se pierde fără efect vizibil major — cazul cererilor din Documente Interne; la Concediere Disciplinară `br`-ul din mijloc face ca motivele să urce pe rândul cu „…reprezentate de:” — acceptat de utilizator.) Soluții aplicate: salt de rând → paragrafe separate în celulă; tab → `w:ind firstLine` în `pPr`; desenele/textbox-urile se țin în paragrafe FĂRĂ placeholder (ex. textbox-ul „paine sanatoasa” a fost mutat în paragraful titlului). Formatarea run-ului (bold, culoare, caps) se păstrează pe valoarea înlocuită (per caracter, prin `posToRpr`).
3. **Liste numerotate native (`<w:numPr>`)** înainte de un placeholder mănâncă vizual bifa/primul run în MS Word. Folosește placeholder-ul ca singur „bullet”.
4. **Majuscule în PDF:** aplică efectul de font „All caps” (`<w:caps/>`) pe run-ul placeholder-ului. NU folosi Shift+F3 / Change Case — schimbă textul în `{{TIPMUTATIE}}` și placeholder-ul nu se mai înlocuiește.
5. **Liste dinamice în tabele:** rândul-șablon se clonează per item. `ExpandParagraphOrRow` caută primul paragraf cu marker `{{cheie}}` pentru fiecare `item_field` și cheie mapată din `maps`. Marker-ul trebuie să fie singur pe paragraf/rând (nu amestecat cu alt text în același paragraf pentru expandare de paragrafe). Dacă e în tabel, se clonează `TableRow`.
   - **Grupuri vMerge (NOU):** dacă rândul-șablon deschide celule unite vertical (`vMerge restart`), se clonează tot grupul (rândul + rândurile următoare care doar continuă unirea). Exemplu: celula de dată împărțită în 3 rânduri An/Lună/Zi la Adeverință; placeholder-ele `{{AnulMutatie}}`, `{{LunaMutatie}}`, `{{ZiMutatie}}` stau în cele 3 rânduri ale grupului.
   - **Listă goală** (niciun rând valid): paragraful / rândul-șablon (sau grupul vMerge) se ELIMINĂ, ca să nu rămână `{{Cheie}}` ca text. Excepții, ca DOCX-ul să rămână valid: dacă rândul e singurul din tabel sau paragraful e singurul din celulă, se păstrează cu valorile golite.
   - Act Adițional folosește `clauze_editor` (cheile fixe `ModificareNr`/`ModificareReferinta`/`ModificareText`), expandat tot prin `ExpandDynamicLists`.
6. Tabelele „floating” (`tblpPr`) din template-ul Adeverinței se randează diferit în LibreOffice față de Word; contează Word/PDF-ul real.
7. Celule goale: un placeholder gol într-un paragraf separat lasă un rând gol (ex. spor/temei neselectat la Adeverință). Acceptat.
8. Header/footer: `FillTemplate` procesează și `HeaderParts`/`FooterParts` (necesar la PV Cercetare).
9. Fără diacritice în numele placeholder-elor; cheile sunt case-sensitive.

---

## 8. Registratura

`RegistraturaService` (singleton) calculează codul `YYddd/NR` (an + zi din an + număr) cu `XSupport.GetSQLDataSet()` (citire) și `XSupport.ExecuteSQL()` (scriere). Tabele: `CCCVREGISTRATURA` (registru), plus inserări specifice în `InregistreazaTabelaSpecifica`, rutate după categorie:
- categorie cu „aditional” → `CCCACTEADITIONALE`
- categorie cu „suspendare”/„incetare”/„interne” (match `lower.Contains(...)`) → `InsertDecizie` → `CCCDCZCONTRACT` (CCCTIPDCZ dedus din titlu; chei recunoscute ex. `DataIncetare` → `CCCDATAVIGOARE`)
- PV-uri → `CCCPVEMISE`

**`registratura_tip_doc_pk` NU e per categorie.** Valori confirmate de utilizator: Act adițional și Confidențialitate = 2; **Acord Semnătură Electronică = 36**; Decizii (inclusiv Încetare CIM de la angajator și toate din Cercetare Disciplinară) = 11; Notificare Încetare CIM = 19; Adeverință de Vechime = 3; **Procese Verbale = 22**; cele 4 cereri din Documente Interne (Demisie fără preaviz, Suspendare CIM CC, Înștiințare Sarcină, CO/Zile Libere) = 6. Întreabă utilizatorul înainte de a presupune pk pentru un document nou.

`RegistraturaService.CodExista(cod)` verifică dacă un cod e deja în `CCCVREGISTRATURA` (folosit în `OnGenerate`, vezi §3.4; erorile SQL nu sunt înghițite).

Notă: pentru „Documente Interne” rutarea prin `InsertDecizie` inserează un rând în `CCCDCZCONTRACT` și pentru cereri/adeverință. Utilizatorul a decis că e OK (tabela e doar pentru a vedea ce documente s-au generat per angajat) — nu schimba.

---

## 9. Teme / categorii

`DocumentRegistry.CategoryOrder` (hardcodat): `Sabloane Acte Aditionale`, `Sabloane Decizii Suspendare`, `Sabloane Decizii Incetare`, `Sabloane Cercetare Disciplinara`, `Sabloane Procese Verbale`, `Sabloane Documente Interne` (corectat 2026-11-11; înainte era `Documente Interne` și nu se potrivea cu folderul). Numele din listă trebuie să fie identic cu numele folderului. Pe disc mai există `Sabloane Semnaturi Electronice` (JSON-uri fără `title`, ignorate — vezi §3.5). Categoriile necunoscute (999) rămân în ordinea în care le întoarce `Directory.GetDirectories`.
`PriorityScrollPanel` (body-ul formularului) derulează la rotița mouse-ului doar când fereastra de sub cursor e panelul sau un copil al lui (`WindowFromPoint`) — un dialog modal deschis peste (ex. `PersonPickerDialog`) își primește singur rotița.
Word: o singură instanță invizibilă refolosită (`WordHelper`); se închide (`ShutdownWordApp`, doar dacă nu are documente deschise) la ieșirea din bucla thread-ului STA din `PluginEntry` și la închiderea `SelectorDialog` în modul Dll Form. `DocumentTheme.ForCategory` mapează după substring (acte/aditional → albastru `RGB(63,129,198)`, suspendare → teal, incetare → roz, cercetare/disciplinar → violet `RGB(120,60,170)`, verbale/procese → chihlimbar); categoriile necunoscute iau tema implicită (Acte).
UI: sidebar/preview 40/60 (`FormBase.BuildShellSplit`), body scrollabil, footer izolat: Anulare (roșu deschis), Previzualizează/Actualizează (galben `RGB(255,243,176)`), Generează PDF (accent). Antet formular: Angajat/CNP/Funcție/Nr.CIM/Data CIM/Departament în câmpuri readonly bordate. `NoScrollComboBox` peste tot. Secțiuni cu bară colorată de 4px.
Butonul „Dosar Personal”: desenat custom (`DrawDosarButton`), icon din `Properties.Resources.dosar_personal` (resursă strong-typed, NU `LoadIcon`/manifest), fundal teal-oliv `(58,74,70)`, bordură aurie `(191,155,89)`, efect de apăsare (fundal mai închis + conținut mutat 1px).

---

## 10. Catalog documente existente

**Sabloane Acte Aditionale** — `registratura_date_field` = `DataEmitereAct`, pk 2 (Acord Semnătură Electronică: pk 36)
1. Act Adițional (de bază, cu `clauze_editor`/`ClauzeEditorDialog`)
2. Act Adițional — Confidențialitate și Neconcurență (`DataEmitereAct`, `DataModificareClauze`, `CuantumConfidentialitate`, `CuantumNeconcurenta`; text legal static)
3. Act Adițional — Acord Semnătură Electronică (formular doar de confirmare, secțiuni readonly auto-precompletate, `NumarTelefonSalariat`/`EmailSalariat` din PRSN)

**Sabloane Decizii Suspendare / Incetare / Cercetare Disciplinara / Procese Verbale** — pk 11 la decizii, pk 22 la PV-uri; document-uri: suspendări (abs. nemotivate, creștere copil, acord părți etc.), încetări (demisie, expirare termen, perioadă probă, încetare suspendare), Referat/Avertisment/Decizie Constituire Comisie/Convocare/PV Cercetare/Decizie Concediere Disciplinară, PV Autovehicul/Echipamente/Electronice. Convenții: `registratura_date_field` = `DataDecizie`/`DataCercetare` etc. Avertismentul are câmpul `LocMunca` (secțiunea „LOC DE MUNCĂ”, adăugată 2026-11-11).

**Sabloane Documente Interne** (folderul real; order)
1. Cerere Demisie fără Preaviz (pk 6; `DataIncetare` standard)
2. Cerere Suspendare CIM — Creșterea Copilului (pk 6; chei normalizate: `Domiciliu`, `CNP`, `DataStartSuspendare`, `DataEndSuspendare`)
3. Cerere Înștiințare Sarcină (pk 6; `ListaDocumente` = multiline, text liber)
4. Cerere Concediu / Zile Libere „CO/Vatra” (pk 6): 3 variante (CO legal / fără plată / învoire) dintr-un combo, câmpuri cu `depends_on`, bifă `▣`/`□` + linii punctate albastre pe variantele neselectate, `CalcZileLucratoare`, perioade cu `datetime` + `FormatPerioada`, selector Zile/Ore, înlocuitor principal + secundar opțional
5. Notificare Încetare CIM (pk 19, `registratura_date_field` = `DataNotificare`; secțiune „Cine a luat decizia”: `person_picker` + nume + funcție readonly)
6. Decizie Încetare CIM — de la Angajator (pk 11, `InjectArticoleFinal`, `PERSOANA CARE A FORMULAT NOTIFICAREA` cu person_picker). Mutată din `Sabloane Decizii Incetare` în `Sabloane Documente Interne` pe 2026-11-11 (rutarea în registratură rămâne `InsertDecizie`; tema devine cea implicită).
7. **Adeverință de Vechime** (pk 3, `registratura_date_field` = `DataInreg`) — vezi §11

Convenția comună pentru „întocmit de HR”: câmp `NumeIntocmitorHr` (text, default „Marin Iulia Alina”) și placeholder `{{NumeIntocmitorHr}}`.

---

## 11. Adeverință de Vechime (cea mai recentă implementare)

Fișiere: `adeverinta_vechime.json`, `Adeverinta_Vechime.docx`.

Formular:
- **DATE ADEVERINȚĂ:** `DataInreg` (date, required), `NumeIntocmitorHr`, `NumeDirectorEconomic` (default „Dunea Dorica” — luat din modelul vechi, de confirmat).
- **CARTE DE IDENTITATE:** `SerieCI`, `NrCI` (se precompletează din `PRSN.IDENTITYNUM`, editabile), `AutoritateEmitenta` (default „SPCLEP Botosani”), `DataEliberareCI`. În DOCX: `{{SerieCI}} {{NrCI}}`.
- **CONTRACT DE MUNCĂ:** `TipNorma` (combo `intreaga`/`partiala`), `TipDurataContract` (combo `nedeterminata`/`determinata` — nu fusese cerut explicit, ales de agent).
- **MUTAȚII INTERVENITE:** `dynamic_list` `Mutatii` cu `edit_mode: "dialog"` (din 2026-11-11; `dialog_title` „Mutație intervenită”, buton „Adaugă mutație”, `required`, auto_height, hide_header, `summary_fields` = `TipMutatie`, `DataMutatie`, `FunctieCurenta`).
  - **Element implicit** (`initial_items`): „Incheiere Contract”, data mutației = `{DataCim}`, funcția = `{Functie}`, act = „Contract”, nr. = `{NrCim}`, data actului = `{DataCim}`. Operatorul doar editează sporul / temeiul legal dacă e cazul.
  - **Reguli** (`on_select` pe `TipMutatie`, `default` = „Modificare Contract” pentru mutațiile noi): Incheiere → act „Contract” + nr./date din CIM; Modificare → „Act aditional”; Suspendare / Incetare → „Decizie”; la toate, funcția = `{Functie}` dacă e goală. Operatorul poate modifica orice.
  Câmpuri în dialog, pe grupuri:
  - „Mutația”: `TipMutatie` („Tip mutație”, combo: Incheiere / Modificare / Suspendare / Incetare Contract) 50% | `DataMutatie` (date, `maps` → `AnulMutatie`/`LunaMutatie`/`ZiMutatie`) 50%; `FunctieCurenta` 100%
  - „Salariu”: `SalariulDeBaza` (number, max 99999, 2 zecimale, format `#,##0.##`) 35% | `DetaliuSporOptional` (check_text, „Are spor”) 65%
  - „Actul care atestă mutația”: `TipDoc` („Tip act”, combo: Contract, Act aditional, Decizie, Notificare, Cerere, Referat) 40% | `NrInregistrareDoc` („Nr. înregistrare act”) 30% | `DataInregDoc` („Data actului”) 30%; `DetaliiTemeiLegalOptional` (check_text, „Are temei legal”) 100%
  - Versiunea nouă a JSON-ului e în proiect: `Sabloane_lucru\Sabloane Documente Interne\adeverinta_vechime.json` (pornită din arhiva din 06.11); utilizatorul o copiază în folderul real.
- **PARAGRAFE FINALE:** 4 combo-uri Da/Nu (`ParagrafIncetareContractOptional`, `ParagrafPerioadaAbsenteNemotivateOptional`, `ParagrafPerioadaConcediuFaraPlataOptional`, `ParagrafPerioadaIncapacitateTemporaraOptional`). Fiecare secțiune de conținut (Încetare contract: `DataIncetareContract` + `PrevederiLegale` — default editabil „Art. 81 alin. 7 din Legea nr. 53/2003”; trei liste de perioade `date_range` — absențe nemotivate, concediu fără plată, incapacitate temporară) are `depends_on` pe combo-ul ei, deci apare în formular doar la „Da”.

Document:
- Cele 4 paragrafe au textul fix cu placeholder-e interioare (`{{DataIncetareContract}}`, `{{PrevederiLegale}}`, `{{PerioadaAbsenteNemotivateOptional}}`, `{{PerioadaConediuFaraPlataOptional}}` [sic, „Conediu” — numele cerut de utilizator], `{{PerioadaIncapacitateTemporaraOptional}}`). Apar ÎNTOTDEAUNA; la „Nu” valorile sunt golite cu `BlankIfNotEquals` → linii punctate albastre. Perioadele multiple din aceeași listă se unesc cu `ConcatList` și separatorul `" si in perioada "`.
- Tabelul de mutații folosește grupul vMerge de 3 rânduri (An/Lună/Zi). `{{TipMutatie}}` și `{{FunctieCurenta}}` au „All caps” aplicat ca formatare.
- Salariul se afișează `{{SalariulDeBaza}} Lei;` și, pe paragraf separat, `{{DetaliuSporOptional}}`; actul: `{{TipDoc}}` pe un paragraf, `{{NrInregistrareDoc}} / {{DataInregDoc}}` pe altul; temeiul legal pe al treilea.

---

## 12. Istoric al extensiilor de cod (față de arhiva de cod trimisă în iulie 2026)

Fișierele modificate în sesiunile recente: `DynamicForm.cs`, `DynamicTemplateEngine.cs`, `DocumentDefinition.cs`, `HookRegistry.cs`, `WordHelper.cs`, `RegistraturaService.cs`, `ErpDataProvider.cs`, `DocumentRegistry.cs`, `SelectorDialog.cs`. Dacă repo-ul din VS Code nu conține aceste modificări, aplică-le din versiunile livrate (cele mai recente) — nu rescrie de la zero.

Extensii (toate aditive, opt-in prin JSON):
- `depends_on`/`depends_value` pe câmpuri și pe secțiuni; `ReflowSections`/`UpdateSectionVisibility`/`UpdateListHeight` în `DynamicForm`.
- Tip `datetime`; combo declanșează `on_change`.
- Hooks `SetIfEquals`, `BlankIfNotEquals`, `CalcZileLucratoare`, `FormatPerioada`; `SetDefault` cu `{year}`/`{today}`; `person_picker` maps cu sufix `:upper`.
- `WordHelper`: colorare albastră pentru `BlankLineMarker`.
- Căutare folder angajat după `PrsnId` (`ResolvePersonFolder`); buton „Dosar Personal”.
- `PrePopulateReadonlyCommonFields`; `TelefonSalariat`/`EmailSalariat` în lanțul `ErpDataProvider` → `CommonDocumentValues` → `DynamicTemplateEngine` → `DynamicForm`.
- Liste dinamice: tipuri `combo`, `date` (+`maps`), `date_range`, `check_text`, `number` (`max`/`decimals`/`format`), `group`; opțiuni `collapsible`, `summary_fields`, `auto_height`, `max_height`, `hide_header`, `add_button_label`; `{{NrCrt}}`; clonare grupuri vMerge în `ExpandParagraphOrRow`; `HasVMergeRestart`/`HasVMergeContinueOnly`.

Corecturi 2026-11-11 (compilate local, netestate în Softone):
- `DynamicForm`: valoarea `person_picker` nu mai e ștearsă la `on_change`; `maps` pe cheia pickerului (cu `:upper`) nu mai e suprascris; `required` pe picker validează corect.
- `DynamicForm.OnGenerate` + `RegistraturaService.CodExista`: PDF înainte de registratură, cod proaspăt + verificare cod liber, ștergere PDF dacă înregistrarea eșuează (§3.4).
- `DynamicTemplateEngine.ExpandDynamicLists` / `ExpandParagraphOrRow`: liste goale → paragraf/rând eliminat (§7.5).
- `PriorityScrollPanel`: rotița ajunge la dialogurile modale (§9).
- `WordHelper.ShutdownWordApp` apelat la final (`PluginEntry`, `SelectorDialog` standalone); timerul de puls din `SelectorDialog` oprit la `FormClosed`.

- Ștergerea oricărui element dintr-o listă (rând `dynamic_list`, card `edit_mode: "dialog"`, clauză Act Adițional) cere confirmare: `ConfirmareStergereDialog.Intreaba(owner, mesaj, theme)` — butoane „Da” / „Nu” în română, „Nu” implicit (Enter/Esc nu șterg). Folosește-l pentru orice ștergere nouă.

Revertate / respinse de utilizator (nu le reintroduce):
- Hook `PrefixIfNotEmpty` („și/sau”) — respins.
- Format de nume PDF MAJUSCULE_UNDERSCORE și câmp `filename_title` — respinse, tot codul revertat.
- Bifă `■` plină — înlocuită cu `□`/`▣`.

---

## 13. Probleme deschise / de verificat

1. Corecturile din 2026-11-11 (§12) compilează, dar n-au fost testate în Softone — rămâi atent la regresii.
2. Bug latent cunoscut: `SetControlValue` nu tratează `ComboBox` (un `SetDefault`/`SqlOnOpen` pe un combo nu se vede în UI). Niciun JSON actual nu e afectat.
3. `NumeDirectorEconomic` = „Dunea Dorica” și valorile `TipNorma`/`TipDurataContract` sunt alegeri ale agentului — de confirmat cu utilizatorul.
4. Câmpul `number` de nivel formular are max 999 (în liste: configurabil).
5. `FunctieReprezentant` e hardcodat în `PluginConfig` (se schimbă prin recompilare).
6. Secțiunile din liste nu suportă `depends_on` pe câmpurile din rând (doar pe secțiune întreagă sau pe câmpuri de nivel formular).

---

## 14. Cum lucrezi cu utilizatorul (flux recomandat)

Document nou, în ordine:
1. Primești DOCX-ul (și uneori un model completat). Extrage placeholder-ele; verifică run-uri rupte, `numPr`, `br`/`tab`/desene în paragrafe cu placeholder, acolade rupte, diacritice în nume.
2. Mapează placeholder-ele pe: placeholder-e automate (§5), câmpuri din formular, chei de listă/`maps`, ținte de hooks. Normalizează numele nestandard (`NumeIntocmitor` → `NumeIntocmitorHr`, `DomiciliuSalariat` → `Domiciliu`, `CNPSalariat` → `CNP`).
3. Întreabă DOAR ce nu se poate deduce (pk registratură, valori implicite, categorie/folder, `order`). Pentru restul alege o valoare rezonabilă și spune ce ai ales.
4. Scrie JSON-ul (schema din §4), apoi verifică încrucișat: fiecare `{{Cheie}}` din DOCX are sursă; fiecare `item_key` de `ConcatList` e cheie din `item_fields`; suma `label_width_percent`/`width_percent` pe rând = 100; `template_file` coincide cu numele DOCX-ului.
5. Patch-uri de cod doar dacă mecanismul lipsește; păstrează opt-in, nu schimba comportamentul documentelor existente, păstrează BOM + CRLF.
6. Livrează fișierele complete (nu diff-uri), cu o notă scurtă: ce s-a schimbat, ce e de confirmat, ce n-a fost testat.

Reguli de stil: răspunsuri scurte, fără explicații lungi, liste doar când ajută. Numele de fișiere/chei în backticks. Nu repeta ce a făcut utilizatorul.

---

## 15. Placeholder-e și chei — verificare rapidă la un document nou

- Cheile din JSON = placeholder-e în DOCX (case-sensitive).
- Valorile unei `dynamic_list` nu se expandează decât dacă markerul `{{cheie}}` al unui `item_field` (sau cheie mapată) apare în template.
- `ConcatList.item_key` trebuie să fie cheia din `item_fields` (nu cheia listei, nu cea a pickerului).
- Câmp `multiline` scris greșit ca `text` apare turtit (26px).
- Formularul nu apare în selector: JSON invalid, `template_file` inexistent, folder gol sau nu direct sub `TemplateDocsPath`.
- Secțiune clipuită: recalculează `height` (câmp simplu ≈ 58px, multiline ≈ 172px, padding 16px) — la liste cu `auto_height` nu e nevoie.
