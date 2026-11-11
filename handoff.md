# HANDOFF — ActAditionalPlugin (generator documente HR)

Stack: C# .NET 4.7.2 WinForms, Softone ERP, Open XML SDK, PdfiumViewer.
Flux: JSON + DOCX în `TemplateDocsPath` → `DocumentRegistry` → `SelectorDialog` → `DynamicForm` → `DynamicTemplateEngine` → PDF în `RecruitmentDocsPath\{PrsnId} - {Nume}\` (folder găsit după prefixul `PrsnId`).
Mod de lucru cu utilizatorul (Tiberiu): el face DOCX-ul, eu fac JSON-ul, repar DOCX-ul și patch-uiesc codul doar când e nevoie. Răspunsuri scurte, RO, fără narare. Nu vrea modificări de cod pentru nume de fișier PDF (respins de 2 ori).

## Schema JSON (reală)
Root: `title`, `template_file`, `registratura`, `registratura_date_field`, `registratura_tip_doc_pk`, `order`, `sections`, `hooks`. Câmpuri cu `key` (nu `name`). Categoria = folderul din `TemplateDocsPath` (nu apare în JSON).
Tipuri câmp: `text`, `multiline`, `date`, `datetime`, `readonly`, `combo`, `number` (max 999), `person_picker` (`maps`), `dynamic_list` (`item_fields`), `clauze_editor`, `expand_table_row`.
`depends_on` / `depends_value` (AND cu `|`) → editabilitate dinamică (liste NU suportă).
Hooks: `SetDefault` (`{year}`/`{today}`), `SqlOnOpen`, `ConcatList` (`distinct`), `InjectArticoleFinal`, `CalcDataSfarsitDinCNP`, `SetIfEquals`, `BlankIfNotEquals`, `CalcZileLucratoare`, `FormatPerioada`. Evenimente: `on_open`, `on_change`, `on_generate`.
Linie punctată albastră: valoarea `_ _ _ _ _ _ _ _ _ _ _ _ _ _ _` (prin `BlankIfNotEquals`), colorată de `WordHelper`.
Bife albastre în Word: `□` / `▣` (text, fără `numPr`).
Placeholder-e comune: angajat (`NumeSalariat`, `CNP`, `Functie`, `NumeDepartament`, `NrCim`, `DataCim`, `SerieCI`, `NrCI`, `Domiciliu`, `NumarTelefonSalariat`, `EmailSalariat`), companie (`NumeAngajator`, `CIFAngajator`, `ReprezentantLegal`, `FunctieReprezentant`, `AdresaCompanie`, `ZipCompanie`, `NrRegComertului`, `IbanCompanie`, `NrTelefonCompanie`, `EmailCompanie`, `WebsiteCompanie`). `CodInregistrare` e automat. `DataInreg` NU e automat: se face un câmp `date` cu acea cheie și `registratura_date_field` pe el.
Nume PDF: `{Titlu}_{CodInregistrare}_{dd-MM-yyyy}.pdf` (global, neschimbat).
Capcane Word: placeholder rupt pe run-uri (repar cu `merge_runs.py`, rulat FĂRĂ `-I`), `numPr` care mănâncă bifa, acoladă ruptă.

## Documente existente
Acte Adiționale: act de bază (pk 2), Confidențialitate/Neconcurență (2), Acord Semnătură Electronică (2).
Documente Interne (order): 1 Demisie fără preaviz, 2 Suspendare CIM CC, 3 Înștiințare Sarcină, 4 CO/Zile Libere (toate patru cu pk `6` PRESUPUS, NECONFIRMAT), 5 Notificare Încetare CIM (pk 19), 6 Încetare CIM de la angajator (pk 11), **7 Adeverință de Vechime (pk 3, NOU)**.
Decizii = pk 11. Cercetare Disciplinară = pk 11.

## Adeverință de Vechime (livrat în această sesiune)
Fișiere: `adeverinta_vechime.json`, `Adeverinta_Vechime.docx` (reparat: `SerieNumarCI`→`{{SerieCI}} {{NrCI}}`, `AnulMutati`→`AnulMutatie`, `NumeIntocmitor`→`NumeIntocmitorHr`, `{{SalariulDeBaza}} Lei;`, cele 4 paragrafe finale rescrise cu placeholder-e interioare).
Cod patch-uit (3 fișiere, DOAR adăugiri, de aplicat de utilizator):
- `DocumentDefinition.cs`: `ItemFieldDefinition` + `options`, `default`, `max`, `decimals`, `format`.
- `DynamicForm.cs`: `DynamicListRow` suportă tipurile `combo`, `date` (cu `maps` = formate .NET → an/lună/zi), `date_range` ("dd.MM.yyyy - dd.MM.yyyy"), `check_text` (bifă + text), `number` cu `max`/`decimals`/`format`; clase noi `CheckTextBox`, `DateRangeBox`; `GetValues` adaugă `NrCrt` (numărul rândului).
- `DynamicTemplateEngine.cs`: `{{NrCrt}}` înlocuit în rândurile clonate (nu e marker de expandare).
Logică: 4 combo-uri Da/Nu (`ParagrafIncetareContractOptional`, `ParagrafPerioadaAbsenteNemotivateOptional`, `ParagrafPerioadaConcediuFaraPlataOptional`, `ParagrafPerioadaIncapacitateTemporaraOptional`); paragrafele apar mereu; la „Nu" câmpurile devin linii punctate albastre (`BlankIfNotEquals`); perioadele multiple se unesc cu `" si in perioada "` (`ConcatList`).
Defaulturi puse de mine (de confirmat): `TipNorma` = `intreaga`/`partiala`; `TipDurataContract` = `nedeterminata`/`determinata`; `NumeDirectorEconomic` = „Dunea Dorica"; `order` = 7.
NU testat în Softone/Visual Studio (nu am compilator aici): de compilat și încercat.

## De atenție
- `RegistraturaService.InregistreazaTabelaSpecifica`: orice categorie cu „interne" → `InsertDecizie` → rând în `CCCDCZCONTRACT` (CCCTIPDCZ dedus din titlu). Pentru Adeverință asta poate fi nedorit; de decis.
- pk `6` la cele 4 documente din Documente Interne e neconfirmat.
- Titlul din JSON apare în SelectorDialog, în registratură și în numele PDF-ului.

## Preferințe utilizator (rezumat)
Concis, RO/EN, loturi de cerințe într-un singur prompt, fără narare, edituri în loc de corecturi; avertizare la 15–20 mesaje + `handoff.md`.

## Actualizare 2026-11-06 (UI formular Adeverință)
Cod nou (peste patch-ul anterior; aceleași 3 fișiere: `DynamicForm.cs`, `DocumentDefinition.cs`, `DynamicTemplateEngine.cs`):
- Secțiune cu `depends_on` / `depends_value` (la nivel de secțiune) → apare în formular doar când condiția e adevărată; secțiunile de dedesubt urcă (`ReflowSections`, blocuri de secțiune în `BuildBody`). Documentul generat nu e afectat.
- Liste dinamice, opțiuni noi pe câmpul `dynamic_list`: `collapsible` (rând restrângibil, click pe antet, acordeon la adăugare), `summary_fields` (chei afișate în antetul rândului), `auto_height` + `max_height` (panou care urmează conținutul), `hide_header` (fără sub-antet duplicat), `add_button_label` (textul butonului; lățimea se potrivește pe text).
- Item field: `check_label` pentru `check_text`; combo-urile din rânduri sunt `NoScrollComboBox` standard (cu chenar, fără schimbare la scroll).
- JSON Adeverință: 1 mutație implicit; etichete „Tip Act Mutație”, „Nr. Înregistrare Act”; secțiunile paragrafelor (Încetare, Absențe, CFP, Incapacitate) apar doar la „Da”.
NU testat în Visual Studio / Softone (fără compilator în sandbox).

## Actualizare 2026-11-06 (bug tabel mutații)
- Cauză: celula de dată (An/Lună/Zi) e făcută din 3 rânduri unite vertical (vMerge); `ExpandParagraphOrRow` clona doar primul rând → luna/ziua apăreau separat și duplicate. Fix în `DynamicTemplateEngine.cs`: se clonează tot grupul de rânduri vMerge (`HasVMergeRestart` / `HasVMergeContinueOnly`). Generic, nu afectează tabelele cu rânduri simple.
- Capcană Word nou descoperită: `WordHelper.MergeAndReplace` reconstruiește paragraful din text → pierde `<w:br/>`, `<w:tab/>` și desene/textbox-uri din paragrafele care conțin `{{...}}`. În docx: br-urile din celule → paragrafe separate; tab-urile → indentare prima linie (`w:ind firstLine=720`); textbox-ul „paine sanatoasa" mutat din paragraful introductiv în titlu (paragraf fără placeholder).

## Actualizare 2026-11-06 (aranjare câmpuri mutații în formular)
- `item_fields[].group` (nou, `DocumentDefinition.cs` + `DynamicListRow.BuildLayout`): câmpul începe un rând nou și apare un mic titlu cu linie deasupra (până la următorul `group`).
- Mutații reorganizate pe grilă de treimi (coloane aliniate între rânduri): „Mutația” (tip / dată / funcție), „Salariu” (salariu / spor), „Actul care atestă mutația” (tip act / nr. înregistrare / dată act, apoi temei legal). Bifele: „Are spor”, „Are temei legal”.
