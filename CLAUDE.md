# CONTEXT — ActAditionalPlugin (Generator Documente HR)

Document pentru un agent nou care preia proiectul în VS Code. Citește-l integral înainte de a modifica ceva. Ultima actualizare: 2026-11-12 (după „purge”: tot UI-ul e web, codul WinForms vechi a fost șters).

---

## 0. TL;DR

- Plugin Softone ERP (C# .NET Framework 4.7.2) care generează documente HR (acte adiționale, decizii, cereri, PV-uri, adeverințe) în PDF, cu înregistrare în registratură.
- **Tot UI-ul e web (WebView2)**: selectorul de documente, selectorul de angajați, formularul documentului (cu PDF-ul real alături), editorul de clauze, confirmările. Formularul clasic WinForms (`DynamicForm` & co.) NU mai există.
- **Data-driven:** un document nou = un JSON (formular + hooks) + un DOCX (template Word cu `{{Placeholder}}`). Codul se atinge doar când lipsește o capabilitate. Schema completă: **`GHID_ADAUGARE_DOCUMENT_NOU.md`** (ține-l la zi când adaugi ceva).
- Utilizatorul (Tiberiu, dezvoltator, vorbește română) face DOCX-ul. Tu faci JSON-ul, repari DOCX-ul (placeholder-e rupte etc.) și patch-uiești codul când e nevoie.
- ⛔ **REGULĂ IMPORTANTĂ: lucrezi DOAR în folderul proiectului** (`D:\Visual Studio Proiecte Vatra\GeneratorDocumente`). Sub nicio formă nu citești, nu modifici, nu muți și nu ștergi nimic în folderul real al șabloanelor (`TemplateDocsPath`, `D:\VATRA DOMNEASCA\...`), în `RecruitmentDocsPath`, în `C:\Program Files (x86)\Soft1\` sau în orice alt loc din afara proiectului — nici pentru verificări (nici `ls` în Program Files). Copiile de lucru ale șabloanelor: `Sabloane Pentru redefinirea proiectului\<categorie>\`; utilizatorul le copiază în folderul real.
- Se testează funcțional DOAR de utilizator, în Softone. **Compilarea se face local**, ieșire în `bin/`/`obj/` (ignorate de git):
  `"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" ActAditionalPlugin.csproj /p:Configuration=Release /p:Platform=x86 /p:FodyGenerateXsd=false /v:minimal`
  După fiecare patch: compilează, `node --check` pe JS-urile modificate, verificarea `GetTypes` (§2.1) și spune explicit ce n-a fost testat în Softone.
- Preferințe: direct, concis, răspunsuri în română, cerințele vin în loturi. Nu vrea modificări de cod pentru „nume fișier PDF” (respinse de 2 ori). Dacă greșești, el editează promptul și regenerează.

---

## 1. Stack și mediu

| Element | Detaliu |
|---|---|
| Limbaj / runtime | C#, .NET Framework 4.7.2, WinForms doar ca gazdă pentru WebView2 + PdfiumViewer |
| ERP | Softone (Soft1), plugin încărcat ca DLL. Instalat în `C:\Program Files (x86)\Soft1\Soft1 - Vatra\` și `Soft1 - Ice\` (copierea o face utilizatorul) |
| UI | Microsoft.Web.WebView2 1.0.4314.35 (Core + WinForms, împachetate de Costura); paginile în `UI\Web\Assets` (EmbeddedResource) |
| Word | Open XML SDK pentru completare; Word Interop late-bound pe fir STA dedicat (`WordHelper`) pentru DOCX → PDF |
| Preview PDF | PdfiumViewer (dreapta formularului, `FitBest`) |
| JSON | Newtonsoft.Json (⚠️ versiunea veche din Softone, §2.1) |
| Variabile de sistem | `TemplateDocsPath` (șabloanele; fallback `Templates\` lângă DLL), `RecruitmentDocsPath` (dosarele PDF ale angajaților) |
| Companie | VATRA DOMNEASCĂ SRL; datele din ERP (`ErpDataProvider.GetCompanyData`: COMPANY + COMPANYEXT); doar `FunctieReprezentant` e în `PluginConfig.cs` |
| Git | ramura de lucru `redefinire-proiect`, principală `master` |

Instalare: `ActAditionalPlugin.dll` + **`WebView2Loader.dll` (x86, fișier separat — utilizatorul NU vrea să fie inclus în DLL)**; target-ul `CopyWebView2Loader` îl copiază la build în `bin\x86\Release`. Pe PC: WebView2 Runtime (Evergreen) + Word.

### 2.1 Capcane Softone / WebView2 (obligatoriu de respectat)
- ⚠️ **Fără `async`/`await` și fără `struct`-uri cu câmpuri de tip WebView2.** Softone face `GetTypes()` pe tot DLL-ul ÎNAINTE ca resolver-ul Costura să existe; un state machine async cu `TaskAwaiter<CoreWebView2Environment>` → „Unable to load one or more of the requested types” la pornirea Softone. Folosește `ContinueWith(..., TaskScheduler.FromCurrentSynchronizationContext())` (vezi `WebHostForm.StartWebView`). Tipurile WebView2 sunt OK ca parametri, variabile locale, câmpuri / proprietăți în clase.
  **Verificare după build:** în PowerShell pe 32 de biți, `[Reflection.Assembly]::LoadFrom("...\bin\x86\Release\ActAditionalPlugin.dll").GetTypes()` — singura `LoaderException` acceptată e `Softone.Lib`.
- ⚠️ **Newtonsoft.Json vechi în procesul Softone** → `MissingMethodException` pentru API nou (ex. `JToken.ToString(Formatting)`). Doar: `JObject.Parse`, `JToken.ToString()`, indexere, conversii explicite `(string)tok`, `JsonConvert.SerializeObject/DeserializeObject`, `[JsonProperty]`.
- ⚠️ **NU folosi `CoreWebView2EnvironmentOptions`** — cu loader-ul din folderul pluginului dă „Class not registered”. Argumentele de pornire se dau prin variabila de mediu `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS`, setată doar pe durata `CreateAsync` (`WebViewSupport.CreateEnvironment(true)` → `--lang=ro-RO`, folder `%LOCALAPPDATA%\GeneratorDocumente\WebView2\UserDataRo`). Dacă pornirea eșuează (ex. 0x8007139F = proces WebView2 rămas cu alți parametri), `WebHostForm.StartFailed` recreează controlul și reia fără argumente pe `UserData`.
- `--lang=ro-RO` NU schimbă formatul input-urilor native de dată → pagina folosește elementul propriu `<ro-date>` (§4).
- Erorile din handler-ele WebView2 sunt înghițite în tăcere → `WebHostForm` le afișează (fereastra „diagnostic”); erorile JS vin prin mesajul `jserror`. DevTools (F12) e activ.
- Dialogurile modale / operațiile lungi pornite din mesajele paginii merg prin `BeginInvoke` (fără bucle de mesaje imbricate în evenimentele WebView2).
- **Fără metode generice proprii cu constrângeri de tip** (ex. `GetAncestor<T>`) — erori de încărcare în Softone. LINQ / OpenXML obișnuit e OK.
- Encoding: `.cs` UTF-8 **cu BOM** + CRLF (excepții: `PluginEntry.cs` fără BOM; `WordHelper.cs` fără BOM, LF). Paginile web și JSON-urile: UTF-8 **fără BOM** (web: CRLF; JSON-urile de lucru: LF, indentare 2). Păstrează formatul fiecărui fișier.
- SQL: `XSupport.GetSQLDataSet()` (citire, `ds[i, "COLOANA"]`), `ExecuteSQL()` (scriere), prin `BulkContext.XSupport`.

---

## 2. Arhitectura

```
PluginEntry.cs        S1 (xSupp static), ProgramGeneral (cmd 4000502, [GENERAL]), Program (cmd 4000501, [PRSNIN],
                      citește PRSN-ul curent) → AppLauncher.Launch (o instanță: mutex + aducere în față;
                      AppServices.Initialize + LoadPersons pe firul Softone; SelectorDialog pe fir STA propriu;
                      la final ShutdownWordApp + BulkContext.Reset)
Models/               DocumentDefinition (DOAR proprietățile folosite de C#), PersonInfo, ClauzeConfig,
                      DocumentTheme (culori pe categorie), PluginConfig (TemplatesRoot, FunctieReprezentant)
Services/             AppServices (inițializare comună), DocumentRegistry, DynamicTemplateEngine (DOCX),
                      WordHelper, DocumentGenerationService, RegistraturaService, HookRegistry,
                      ErpDataProvider (companie, CIM, LoadPersons), ClauzeService, BulkContext
UI/SelectorDialog.cs  fereastra principală (selector.html) — numele clasei + ctor fără parametri rămân pentru
                      Softone „Dll Form” (ActAditionalPlugin.dll;SelectorDialog), care inițializează singur serviciile
UI/Web/WebHostForm.cs baza ferestrelor web: WebView2, pagini din resurse la https://app.generator/,
                      mesaje JSON (ready→SendInit, ping, jserror; restul → HandleMessage), Post, ShowError
UI/Web/WebDocumentForm.cs  formularul unui document: stânga form.html (40%), dreapta PDF (60%)
UI/Web/PersonList.cs  persoanele → JSON pentru picker.js + valorile `maps` (inclusiv `:upper`)
UI/Web/Assets/        form.html|css|js, selector.html|css|js, picker.js (selectorul de angajați, comun),
                      app.css (picker + ferestrele de mesaj, comun; se încarcă după form.css)
```

### 2.2 Fluxul
1. `AppServices.Initialize`: `RegistraturaService`, `HookRegistry.RegisterAll`, `DocumentRegistry.Initialize(PluginConfig.TemplatesRoot)`, date companie, `BulkContext`. `ErpDataProvider.LoadPersons` = toți angajații activi (PRSN + PRSEXTRA + DEPART + SPECIALTY).
2. **Selectorul** (`SelectorDialog` / `selector.html`): butonul angajatului (roșu pulsând = neales; albastru cu inițiale = ales) deschide selectorul web de angajați; „Dosar personal” deschide folderul angajatului; documentele sunt coloane pe categorii (culorile din `DocumentTheme`), cu căutare. Clic = alege, dublu-clic / Enter / „Continuă” = deschide (fără angajat → se deschide întâi selectorul de angajați).
3. `openDocument` → C# construiește `CommonDocumentValues` (CIM-ul angajatului e ținut în cache) + codul de înregistrare și deschide **`WebDocumentForm` modal PESTE selector**; la închidere, `documentClosed` resetează documentul ales (angajatul rămâne).
4. **Formularul** (`form.html`): `init` = JSON-ul brut al documentului + valori inițiale (precompletare ERP + hooks `on_open`) + angajat + persoane + clauze + temă + cod. Modificările → `change` (hooks `on_change`); data de înregistrare → `regDate` → `cod`; previzualizarea automată (§4).
5. **Generare:** `generate` → valori + hooks `on_generate` + cod proaspăt → `confirmGenerate` (fereastra web de confirmare cu cod + dată) → `generateConfirmed` → `DocumentGenerationService.GenerateAndRegister`: **întâi PDF-ul, apoi registratura**; după PDF se verifică `RegistraturaService.CodExista` (cod luat între timp → PDF șters și regenerat cu codul următor, max 3); dacă înregistrarea eșuează, PDF-ul se șterge → `generated` (fereastra de succes) → `close`. Erorile → `generateDone {error}` (fereastră de eroare în pagină), formularul rămâne deschis, o nouă încercare nu dublează înregistrarea.
6. **Nume PDF:** `{Titlu}_{CodInregistrare}_{dd-MM-yyyy}.pdf` (`SanitizeFileName`, `/` → `-`). **Titlurile JSON sunt fără diacritice** (excepție: „Acord Semnătură Electronică”).
   ⚠️ **Titlul + paginarea sunt folosite de semnăturile electronice** (`{TemplateDocsPath}\Sabloane Semnaturi Electronice\*.json`, ignorate de `DocumentRegistry` fiindcă n-au `title`): găsesc PDF-ul după tipar de nume (`FilePattern`, ex. `"Cerere Demisie fara Preaviz_*.pdf"`) și pun semnăturile la coordonate fixe. Redenumirea unui titlu sau schimbarea paginării poate rupe semnarea — avertizează și întreabă de șablonul de semnătură. Fără șablon de semnătură (2026-11-11): Adeverință de Vechime, Convocare Cercetare, Decizie Constituire Comisie, PV Cercetare. Pe 2026-11-12 18 titluri au pierdut diacriticele; tiparele de nume ale semnăturilor au fost actualizate de utilizator.
7. **Folder angajat:** `RecruitmentDocsPath\{PrsnId} - {Nume}\`, găsit după prefixul `"{PrsnId} - "` (`DynamicTemplateEngine.ResolvePersonFolder`), nu după nume; creat dacă lipsește.
8. Word: o instanță invizibilă refolosită, pe firul `WordThread`; `ShutdownWordApp` la închiderea selectorului (închide Word doar dacă nu are documente deschise).

---

## 3. Mesaje pagină ↔ C#

| Fereastră | Pagina → C# | C# → pagina |
|---|---|---|
| Comun (`WebHostForm`) | `ready`, `ping`, `jserror` | `pong` |
| Selector | `openDocument {doc, prsnId}`, `openFolder {prsnId}`, `close` | `init {categories[{name, theme, docs[{id,title}]}], persons, prsnId}`, `documentClosed {generated}` |
| Formular | `cancel`, `close`, `change`, `regDate`, `preview {values, lists, clauze, required, auto}`, `generate`, `generateConfirmed`, `generateCancelled`, `pickPerson {requestId, prsnId, maps}`, `loadClauzeConfig`, `saveClauzeConfig {config}` | `init`, `changed`, `cod`, `previewDone {ok, auto, message}`, `confirmGenerate`, `generated`, `generateDone {error}`, `personPicked`, `clauze`, `clauzeConfig`, `clauzeSaved` |

`BuildFormValues` convertește valorile paginii în formatul motorului: date `dd.MM.yyyy`, `maps` pe date (bucăți An/Lună/Zi, cultura ro-RO), `date_range` „a - b”, `check_text` doar dacă e bifat, `number` cu `format` ro-RO, `NrCrt`, elemente invalide (primul câmp gol) sărite, clauze → `ModificareNr/ModificareReferinta/ModificareText`, `MentiuniDocument`.

---

## 4. Pagina formularului (`form.js`) — reguli de randare și comportament

- Combo `Da`/`Nu` → comutator; secțiunile cu `depends_on` = acel combo și `depends_value` = „Da” apar **în interiorul** comutatorului și nu în navigare. Combo cu 2 opțiuni → comutator între variante; `"style": "segmented"` → butoane alăturate; altfel listă.
- `depends_on` pe câmp / secțiune: inactivul se **ascunde** (rândurile complet inactive dispar); câmpurile `required` ascunse nu se validează.
- Titluri „X (Y)” → titlu X + indiciu Y în dreapta. `NrCim`/`DataCim`/`NumeDepartament` nu se afișează (sunt în cardul angajatului).
- `dynamic_list`: `edit_mode: "dialog"` → carduri + fereastră de editare; altfel rânduri editabile (o coloană → rând compact). Ștergerile cer confirmare (`confirmDel`, Da/Nu, „Nu” implicit).
- **Date:** elementul propriu `<ro-date type="date|datetime">` — text `zz.ll.aaaa[ hh:mm]`, separatorii se pun singuri la tastare (`roDateMask`, doar la inserare), validare reală (`roDateParse`), text invalid → revine la ultima valoare la ieșire, calendar nativ ascuns (`showPicker`). `.value` e ISO; focusul e pe input-ul interior → `el.contains(document.activeElement)`. `"default": ""` = pornește gol. `time_depends_on`/`time_depends_value` pe `datetime` = ora doar când condiția e adevărată.
- **Persoane:** `person_picker` deschide selectorul web (`PersonPicker.open`), apoi `pickPerson {prsnId, maps}` → C# calculează valorile. Butonul ales arată numele + „↻ Schimbă”.
- `unlockable: true` pe secțiune → buton „🔒 Deblochează” pentru câmpurile `readonly` (Acord Semnătură Electronică).
- **Mențiuni:** secțiunea „MENȚIUNI” (comutator, ascunsă implicit) → `{{MentiuniDocument}}`. ⚠️ Niciun DOCX actual nu conține placeholder-ul.
- **Previzualizare automată:** la ~1,2 s după ultima modificare și la deschidere (`schedulePreview`/`autoPreview`); butonul „Previzualizează” rămâne. C#: hooks `on_generate` pe firul ferestrei, DOCX + conversie în fundal (`ThreadPool` + firul Word); o cerere venită în timpul generării se păstrează (doar ultima); pagina PDF curentă se păstrează. Eroarea automată = doar starea „eroare”; cea cerută cu butonul = fereastră de eroare.
- **Linii punctate în previzualizare:** câmpurile simple goale după hooks (`number` 0 = gol) → `BlankLineMarker` albastru (opțional) sau roșiatic `C0504D` (`RequiredBlankMarker` = marker + U+200B, obligatoriu ACUM — pagina trimite `required`). Doar previzualizarea (`emptyMarkers` → `FillTemplate` pas 3b); PDF-ul final nu e afectat.
- **Clauze (Act Adițional, `clauze_editor`):** puncte = clauză din setul activ (sau „Text liber”), câmpurile ei, textul rezultat (`{0}`, `{1}`…), „✎ Editează textul” manual / „↺ Revino”. `required` = cel puțin un punct completat.
- **Editorul de clauze** (fereastra `#ovCl`, butonul „⚙ Editor clauze”): un ecran — setul de clauze (= `TipContract`; „★ Folosește în formular” = `tipSelectatId`, „⧉ Copie a setului”, „＋ Set nou gol”, șterge) + clauzele ca acordeon (▲▼, ⧉, ✕; titlu, „Ce se modifică” = `TextClauza`, „Textul nou” = `TextDinamic` scris cu **[Nume câmp]**, „＋ Câmp de completat”, câmpurile deduse din text cu tip ghicit din nume, „Afișează în formular” = `Activ`, exemplu live). Conversie `clFromStore` / `clToStore` ({n} ↔ [nume]); fișierul `clauze_act_aditional.json` (lângă șabloane, `ClauzeService`) are același format ca înainte.

---

## 5. Schema JSON — rezumat (detalii în GHID)

Rădăcina: `title`, `template_file`, `registratura_tip_doc_pk` (> 0 ⇒ registratură; `"registratura": true` nu mai e necesar), `registratura_date_field`, `order`, `sections[]`, `hooks[]`. Secțiune: `title`, `fields`, `depends_on`/`depends_value`, `unlockable`. Câmp: `key`, `label`, `type`, `label_width_percent` (fără = rând întreg), `required`, `placeholder`, `default` (tokenuri `{today}`, `{year}`, datele angajatului), `depends_on`/`depends_value`, `options`, `style`, `maps`, `time_depends_on`/`time_depends_value`. Listă: `item_fields`, `edit_mode`, `dialog_title`, `summary_fields`, `initial_rows` (implicit 1), `initial_items`, `hide_header`, `add_button_label`, `required`. Element: `key`, `label`, `type`, `width_percent` (implicit 50), `placeholder`, `default`, `options`, `on_select`, `maps`, `max`, `decimals`, `format`, `check_label`, `group`.

Curățenie 2026-11-12 (Node, fără schimbare de comportament): scoase `ui`, `registratura`, `height`, `auto_height`, `max_height`, `label_width_percent: 100`, `initial_rows: 1`; JSON-urile re-formatate cu indentare 2. `DocumentDefinition` (C#) conține doar ce folosește C# (titlu, șablon, ordine, registratură, secțiuni → câmpuri `key/label/type/maps/item_fields`, elemente `key/type/maps/decimals/format`, hooks); pagina citește JSON-ul brut.

Placeholder-e automate: angajat (`NumeSalariat`, `CNP`, `Functie`, `NumeDepartament`, `NrCim` din `PRSEXTRA.CCCVARCHAR05`, `DataCim`, `SerieCI`/`NrCI` din `PRSN.IDENTITYNUM`, `Domiciliu`, `NumarTelefonSalariat`, `EmailSalariat`), companie (`NumeAngajator`, `CIFAngajator`, `ReprezentantLegal`, `FunctieReprezentant`, `AdresaCompanie`, `ZipCompanie`, `NrRegComertului`, `IbanCompanie`, `NrTelefonCompanie`, `EmailCompanie`, `WebsiteCompanie`), `CodInregistrare`, `MentiuniDocument`, `ArticolCompartiment`/`ArticolContestatie` (hook). `DataInreg` NU e automat. Precompletare la deschidere (`BuildInitialValues`) pentru cheile `CISeria`/`SerieCI`, `CINr`/`NrCI`, `Domiciliu`, `NumeSalariat`, `CNP`, `Functie`, `NumeDepartament`, `NrCim`, telefon/email, datele companiei și `AutoritateEmitenta` / `DataEliberareCI` (tabela `EMPLOYEE`: `ISSAUTHORITY`, `DATEISS`).

Hooks: `SetDefault`, `SqlOnOpen`, `SqlOnGenerate`, `CalcDataSfarsit`, `CalcDataSfarsitDinCNP`, `CalcPerioadaSuspendare`, `CalcZileLucratoare`, `FormatPerioada`, `ConcatList`, `SetIfEquals`, `BlankIfNotEquals`, `InjectArticoleFinal` (rulează și în `FillTemplate`). La `on_generate` contează ordinea.
Convenții în document: linie punctată albastră = valoarea `_ _ _ … _` (`BlankLineMarker`, variante neselectate); `~~text~~` = text tăiat (varianta nealeasă); bife `▣`/`□` ca text (nu `■`).

---

## 6. Reguli pentru DOCX (capcane reale)

1. Placeholder rupt în mai multe run-uri = OK; NU în run-uri imbricate (`w:hyperlink`, `w:ins`, `w:sdt`, `w:smartTag`), acolade rupte sau spații interioare.
2. `MergeAndReplace` reconstruiește paragraful: într-un paragraf cu `{{...}}` se pierd `<w:br/>`, `<w:tab/>`, desenele. Soluții: paragrafe separate, `w:ind firstLine`, desene în paragrafe fără placeholder. Formatarea run-ului se păstrează pe valoare.
3. `<w:numPr>` înaintea unui placeholder ascunde vizual primul run în Word.
4. Majuscule: „All caps” (`<w:caps/>`), nu Change Case.
5. Liste: rândul / paragraful cu `{{cheie}}` al unui `item_field` (sau cheie mapată) se clonează; grupurile `vMerge` se clonează împreună; listă goală → rândul dispare (păstrat golit dacă e singurul din tabel / celulă). `clauze_editor` = cheile fixe `ModificareNr/ModificareReferinta/ModificareText`.
6. Header / footer procesate. Tabelele „floating” se văd diferit în LibreOffice — contează Word.
7. Fără diacritice în numele placeholder-elor; case-sensitive.

---

## 7. Registratura

`RegistraturaService`: cod `YYddd/NR`, `CCCVREGISTRATURA` + `CCCVDOCAUDIT`, plus `InregistreazaTabelaSpecifica` după categorie: „aditional” → `CCCACTEADITIONALE`; „suspendare”/„incetare”/„interne” → `InsertDecizie` → `CCCDCZCONTRACT` (CCCTIPDCZ = titlul fără diacritice; e OK și pentru cereri — decizia utilizatorului); PV-uri → `CCCPVEMISE` (CCCPVTYPE 2 dacă titlul conține „autovehicul”).
`registratura_tip_doc_pk` (confirmate): Act adițional + Confidențialitate = 2; Acord Semnătură Electronică = 36; decizii (inclusiv Încetare CIM de la angajator și toată Cercetarea Disciplinară) = 11; Notificare Încetare CIM = 19; Adeverință de Vechime = 3; Procese Verbale = 22; cele 4 cereri din Documente Interne = 6. Întreabă pentru un document nou.
`CodExista(cod)` — verificarea codului liber după generarea PDF-ului.

---

## 8. Categorii / teme

`DocumentRegistry.CategoryOrder` (identic cu numele folderelor): `Sabloane Acte Aditionale`, `Sabloane Decizii Suspendare`, `Sabloane Decizii Incetare`, `Sabloane Cercetare Disciplinara`, `Sabloane Procese Verbale`, `Sabloane Documente Interne`; necunoscutele la final. `DocumentTheme.ForCategory` (substring): acte/aditional albastru `(63,129,198)`, suspendare teal, incetare roz, cercetare/disciplinar violet `(120,60,170)`, verbale/procese chihlimbar, interne verde `(59,138,74)`; implicit = Acte.

---

## 9. Catalog documente (29) — copii în `Sabloane Pentru redefinirea proiectului\`

**Acte Adiționale** (`DataEmitereAct`): Act Aditional (clauze), Act Aditional — Confidentialitate si Neconcurenta, Acord Semnătură Electronică (secțiuni `unlockable`, date precompletate).
**Decizii Suspendare** (6) / **Decizii Încetare** (4) — pk 11, `DataDecizie`, `InjectArticoleFinal`.
**Cercetare Disciplinară** (6, pk 11): Referat, Avertisment, Decizie Constituire Comisie, Convocare, PV Cercetare, Decizie Concediere. Reorganizate 2026-11-12 (secțiuni în ordinea din document, câmpuri din document obligatorii). `CodInregistrareITM` implicit „4711/CCMMRM/04.08.2026”, `IntervalAniCCM` „2026-2028” (Convocare + Decizie Constituire). Nr./data documentului anterior se completează MANUAL (datele pornesc goale, obligatorii) — precompletarea din registratură a fost respinsă ca nefezabilă momentan. Concediere: `PerioadaCercetare` = 2 date + `FormatPerioada`. Convocare: `CodCor` din `SqlOnOpen`.
**Procese Verbale** (pk 22, `DataPV`): Echipamente de Lucru, Echipamente Electronice (`TipPredare` segmented, cantitate/preț `number`, preț `#,##0.00`), Predare-Primire Autovehicul (ordinea din document, dotări Da/Nu, RCA/rovinietă pornesc goale).
**Documente Interne:** Cerere Demisie fara Preaviz (6), Cerere Suspendare CIM — Cresterea Copilului (6), Cerere Instiintare Sarcina (6; „Vârsta sarcinii” + comutator `UnitateSarcina` săptămâni/luni → `{{TextSaptamani}} / {{TextLuni}}` cu varianta nealeasă tăiată — DOCX modificat în copia din proiect), Cerere Concediu / Zile Libere „CO/Vatra” (6; ordine Interval → Tip cerere (segmented) → secțiunea variantei → Înlocuitori; bife `▣`/`□`, linii albastre pe variantele neselectate, `CalcZileLucratoare`, `FormatPerioada`, Zile/Ore cu `time_depends_on`), Notificare Incetare CIM (19, `DataNotificare`), Decizie Incetare CIM — de la Angajator (11), **Adeverinta de Vechime** (3, `DataInreg`).

Convenția „întocmit de HR”: câmp `NumeIntocmitorHr` (default „Marin Iulia Alina”).

### Adeverința de Vechime
- DATE ADEVERINȚĂ: `DataInreg`, `NumeIntocmitorHr`, `NumeDirectorEconomic` („Dunea Dorica Liliana”). CARTE DE IDENTITATE: `SerieCI`, `NrCI`, `AutoritateEmitenta` (default „SPCLEP Botosani”, ERP dacă există), `DataEliberareCI`. CONTRACT: `TipNorma` (intreaga/partiala), `TipDurataContract` (nedeterminata/determinata).
- MUTAȚII: listă `Mutatii`, `edit_mode: "dialog"`, `required`; element implicit „Incheiere Contract” din datele CIM (`initial_items`); `on_select` pe `TipMutatie` (Incheiere → Contract + nr./date CIM; Modificare → Act aditional; Suspendare/Incetare → Decizie; funcția = `{Functie}` dacă e goală). Câmpuri pe grupuri: Mutația (`TipMutatie`, `DataMutatie` cu `maps` An/Lună/Zi, `FunctieCurenta`), Salariu (`SalariulDeBaza` number `#,##0.##`, `DetaliuSporOptional` check_text), Actul (`TipDoc`, `NrInregistrareDoc`, `DataInregDoc`, `DetaliiTemeiLegalOptional`).
- PARAGRAFE FINALE: 4 comutatoare Da/Nu cu secțiunile lor în interior (încetare: `DataIncetareContract` + `PrevederiLegale` default „Art. 81 alin. 7 din Legea nr. 53/2003”; 3 liste de perioade `date_range`). În document paragrafele apar mereu; la „Nu” valorile devin linii albastre (`BlankIfNotEquals`); perioadele se unesc cu `ConcatList` (`" si in perioada "`). `{{PerioadaConediuFaraPlataOptional}}` [sic] e numele cerut de utilizator.
- DOCX: tabelul de mutații folosește grupul vMerge An/Lună/Zi; `{{TipMutatie}}`/`{{FunctieCurenta}}` au „All caps”.

---

## 10. Istoric important

- **2026-11-12 — „purge”:** tot UI-ul trecut pe web; șterse: `DynamicForm` (+ `DynamicListRow` etc.), `ListItemDialog`, `PunctModificareControl`, `ClauzeEditorDialog`, `ConfirmareDialog`, `ConfirmareStergereDialog`, `SuccessDialog`, `PersonPickerDialog`, `DocumentFormFactory`, resursele PNG / `Resources.resx` / `Settings`, `PROJECT_CONTEXT_old.md`, `handoff.md`. Noi: `WebHostForm`, `PersonList`, `AppServices`, `AppLauncher`, `selector.*`, `picker.js`, `app.css`. Selectorul rămâne deschis sub formular (înainte se închidea și se redeschidea). PRSNIN fără angajat curent → selectorul se deschide fără preselecție (înainte nu se deschidea nimic).
- Corecturi anterioare încă valabile: PDF înainte de registratură + cod liber verificat; liste goale → rândul șablonului eliminat; `ResolvePersonFolder` după PrsnId; Word pe fir dedicat.

Respinse de utilizator (nu le reintroduce): hook `PrefixIfNotEmpty`; nume PDF MAJUSCULE_UNDERSCORE / `filename_title`; bifa plină `■`; precompletarea nr./datei din registratură la Cercetare Disciplinară (momentan).

---

## 11. Probleme deschise / de verificat

1. Purge-ul din 2026-11-12 a fost testat de utilizator în Softone (2026-11-12) și funcționează. Dacă apare ceva, verifică întâi zonele noi: selectorul web, selectorul de angajați, confirmarea / succesul web, deschiderea ca Dll Form.
2. Semnăturile electronice: tiparele de nume au fost actualizate de utilizator după titlurile fără diacritice (2026-11-12). Pe viitor utilizatorul vrea să dea acces la proiectul semnăturilor, ca să fie mai puțin hardcodat (tipar de nume + coordonate fixe) — momentan rămâne așa.
3. Câmpul `number` de nivel formular are max 999 (în liste: `max`).
4. `FunctieReprezentant` e hardcodat în `PluginConfig`.
5. Câmpurile din elementele unei liste nu suportă `depends_on`.
6. `{{MentiuniDocument}}` nu există în niciun DOCX.

---

## 12. Cum lucrezi cu utilizatorul

Document nou: (1) extrage placeholder-ele din DOCX și verifică capcanele §6; (2) mapează-le pe placeholder-e automate, câmpuri, liste/`maps`, ținte de hooks (normalizează: `NumeIntocmitor` → `NumeIntocmitorHr`, `DomiciliuSalariat` → `Domiciliu`, `CNPSalariat` → `CNP`); (3) întreabă doar ce nu se deduce (pk registratură, valori implicite, categorie, `order`); (4) scrie JSON-ul și verifică încrucișat (fiecare `{{Cheie}}` are sursă, `ConcatList.item_key` din `item_fields`, `template_file` corect); (5) cod doar dacă lipsește mecanismul — opt-in, fără să schimbi documentele existente; (6) livrează fișierele complete cu o notă scurtă: ce s-a schimbat, ce e de confirmat, ce n-a fost testat.

Stil: răspunsuri scurte, liste doar când ajută, numele de fișiere/chei în backticks.
