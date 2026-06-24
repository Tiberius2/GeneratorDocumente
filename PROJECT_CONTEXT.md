# ActAditionalPlugin — Context Proiect

## Stare curentă (Iunie 2026)

### Arhitectura nouă (Data-Driven)
Proiectul a fost migrat complet la un sistem data-driven bazat pe JSON + DOCX template.
**Zero recompilare** pentru adăugarea de documente noi.

### Structura fișierelor noi (de adăugat în proiect)

**Models/**
- `DocumentDefinition.cs` — structura JSON deserializată (SectionDefinition, FieldDefinition, ItemFieldDefinition, HookDefinition). `SectionDefinition` are proprietatea `Height` (int, opțional).
- `PersonInfo.cs` — DTO angajat: PrsnId, NumeComplet, Nume, Prenume, CNP, Functie, CodCor, NrCim, DataCim
- `ClauzeConfig.cs` — conține și clasa `PunctModificare` (mutată din DocumentModels.cs șters)

**Services/**
- `DocumentRegistry.cs` — scanează folderele din `TemplateDocsPath` la startup. `CategoryOrder` = ["Sabloane Acte Aditionale", "Sabloane Decizii Suspendare", "Sabloane Decizii Incetare", "Sabloane Cercetare Disciplinara", "Sabloane Procese Verbale"]
- `DynamicTemplateEngine.cs` — engine nou: `GeneratePdf()`, `GeneratePreviewDocx()`, `ExpandDynamicLists()`, `ExpandTableRows()`. `GetParentTableRow()` în loc de `GetAncestor<T>` (compatibilitate .NET 4.7.2). Include clasa `CommonDocumentValues`.
- `HookRegistry.cs` — handlere: `InjectArticoleFinal`, `SqlOnOpen`, `SqlOnGenerate`, `ConcatList`, `CalcDataSfarsit`. Evenimente: `on_open`, `on_generate`, `on_change`.
- `BulkContext.cs` — rescris: `Persoane`, `GetCimData`, `CompanyData`, `XSupport`

**UI/**
- `DynamicForm.cs` — forma universală. Conține și `PriorityScrollPanel`, `NoScrollComboBox`, `DynamicListRow`.
  - Header: titlu stânga + cod înregistrare dreapta (centrat vertical, 13pt Bold, fundal albastru închis)
  - Section angajat: 2 rânduri (Angajat/CNP/Functie + NrCim/DataCim/Departament), câmpuri cu border
  - `BuildBody()`: iterează secțiunile JSON direct, fără secțiune hardcodată de identificare
  - `BuildDynamicListField()`: primește `sectionHeight` → `itemsPanelH = sectionHeight - 42`
  - `BuildClauzeEditorSection()`: primește `sectionHeight`, pornește cu 2 clauze inițiale
  - `DynamicListRow.BuildLayout()`: suport multi-rând (câmpurile se grupează automat per 100%)
  - Mențiuni: checkbox toggle "Adaugă mențiuni (nu apar în PDF)" în body, la final
  - Footer: 56px, doar butoane (Anulare / Previzualizează / Generează PDF)
  - `RecalcCodInregistrare(DateTime? data)`: recalculează la schimbarea primei date
  - `PriorityScrollPanel`: interceptează `WM_MOUSEWHEEL` pe sidebar
  - `NoScrollComboBox`: ignoră scroll când dropdown e închis
  - `GetPersonProperty`: suportă "NumeComplet_Functie" (concatenare cu " — ")
  - `HeaderFields`: {"NrCim", "DataCim", "NumeDepartament"} — sărite din body (afișate în header)
- `PersonPickerDialog.cs` — dialog light-themed (fundal albăstrui mediu). `_suppressSearch` flag pentru fix flicker. Ultima coloană `width=-2` (fill). SQL cu LEFT JOIN.
- `SelectorDialog.cs` — dinamic din `DocumentRegistry`. Selector angajat prin `PersonPickerDialog`.
- `PunctModificareControl.cs` — `NoScrollComboBox` pentru dropdown clauze. Fix poziție buton Editează (sub text).

**Fișiere de șters din proiect:**
- `DocumentModels.cs`, `SelectionTypes.cs`, `PvModels.cs`
- `ActAditionalForm.cs`, `AngajatPickerDialog.cs`, `AngajatMultiPickerDialog.cs`
- `AvertismentDisciplinarForm.cs`, `CercetareDisciplinaraForm.cs`
- `DecizieFormBase.cs`, `DeciziiIncetare.cs`, `DeciziiSuspendare.cs`
- `DocumentFormBase.cs`, `EchipamentItemControl.cs`, `FormBase.cs`
- `PvFormBase.cs`, `Pvforms.cs`, `ReferatDisciplinarForm.cs`
- `BulkConfirmareDialog.cs`, `BulkRezultateDialog.cs`, `ButtonTheme.cs`
- `TemplateEngine.cs`, `PvTemplateEngine.cs`, `ClauzeService.cs` (de restaurat din backup)

**Fișiere modificate:**
- `PluginConfig.cs` — `TemplatesRoot` public citește `TemplateDocsPath`. Metodele `GetTemplatePath(TipDocument/TipPV)` șterse.
- `DocumentTheme.cs` — `For(TipDocument)` și `For(TipPV)` șterse. Adăugat `ForCategory(string)`.
- `RegistraturaService.cs` — `GetTipDocPK` și `GetTitluDoc` șterse.
- `BulkContext.cs` — rescris fără referințe la `AngajatPickerDialog`.
- `ClauzeConfig.cs` — `PunctModificare` adăugat aici.

**Fișiere de restaurat (existau, au fost șterse greșit):**
- `ClauzeService.cs`, `PunctModificareControl.cs`, `ClauzeEditorDialog.cs`

### Structura Templates pe disk
```
{TemplateDocsPath}/
  Sabloane Acte Aditionale/
    ActAditional_template.docx
    act_aditional.json
  Sabloane Decizii Suspendare/
    template_suspendare_*.docx + *.json (6 documente)
  Sabloane Decizii Incetare/
    template_incetare_*.docx + *.json (4 documente)
  Sabloane Cercetare Disciplinara/
    template_*.docx + *.json (6 documente)
  Sabloane Procese Verbale/
    template_pv_*.docx + *.json (3 documente)
```

### Schema JSON
```json
{
  "title": "Titlu Document",
  "template_file": "template.docx",
  "registratura": true,
  "registratura_date_field": "DataDecizie",
  "registratura_tip_doc_pk": 11,
  "sections": [
    {
      "title": "TITLU SECTIUNE",
      "height": 82,
      "fields": [
        { "key": "CheieField", "label": "Label", "type": "date|text|multiline|readonly|combo|person_picker|dynamic_list|clauze_editor|expand_table_row|number", "label_width_percent": 50, "required": true, "default": "...", "placeholder": "...", "maps": {"TargetKey": "PersonInfoProp"} },
        { "key": "Lista", "type": "dynamic_list", "initial_rows": 1,
          "item_fields": [
            { "key": "Cheie", "label": "Label", "type": "text|person_picker|number", "width_percent": 50, "maps": {"AltCamp": "NumeComplet"} }
          ]
        }
      ]
    }
  ],
  "hooks": [
    { "on": "on_open|on_generate|on_change", "handler": "NumeHandler", "params": {} }
  ]
}
```

### Tipuri de câmpuri
- `text` — TextBox simplu
- `multiline` — TextBox multiline, 160px (~8 rânduri)
- `date` — DateTimePicker
- `readonly` — TextBox readonly gri
- `number` — NumericUpDown (triggheruiește `on_change`)
- `combo` — ComboBox cu `options: []` sau `options_sql`
- `person_picker` — buton → PersonPickerDialog → autocomplete via `maps{}`
- `dynamic_list` — listă dinamică cu add/delete, suport multi-rând per item
- `clauze_editor` — specific ActAditional, cu buton Editor Clauze
- `expand_table_row` — expandare rând tabel DOCX (ActAditional modificări)

### PersonInfo properties pentru `maps{}`
`NumeComplet`, `Nume`, `Prenume`, `CNP`, `Functie`, `CodCor`, `NrCim`, `DataCim`, `NumeComplet_Functie`

### Hookuri disponibile
- `InjectArticoleFinal` — (`on_generate`) numără Art.N și injectează ArticolCompartiment/ArticolContestatie
- `SqlOnOpen` — (`on_open`) params: `query`, `column`, `target_field`. Placeholders: `{PrsnId}`, `{CompanyId}`
- `SqlOnGenerate` — (`on_generate`) același mecanism
- `ConcatList` — (`on_generate`) params: `source_field`, `item_key`, `target_field`, `separator`
- `CalcDataSfarsit` — (`on_change`) params: `start_field`, `luni_field`, `target_field`. Calculează DataEnd = DataStart + N luni

### Placeholdere comune (BuildCommonPlaceholders)
`{{NumeSalariat}}`, `{{CNP}}`, `{{Functie}}`, `{{NumeDepartament}}`, `{{NrCim}}`, `{{DataCim}}`,
`{{CodInregistrare}}`, `{{NumeAngajator}}`, `{{CIFAngajator}}`, `{{ReprezentantLegal}}`,
`{{FunctieReprezentant}}`, `{{AdresaCompanie}}`, `{{ZipCompanie}}`, `{{NrRegComertului}}`,
`{{IbanCompanie}}`, `{{NrTelefonCompanie}}`, `{{EmailCompanie}}`, `{{WebsiteCompanie}}`,
`{{MentiuniDocument}}`, `{{ArticolCompartiment}}`, `{{ArticolContestatie}}`

### Build Event (Post-build)
```
copy /Y "$(TargetPath)" "C:\Program Files (x86)\Soft1\Soft1 - Ice\$(TargetFileName)"
copy /Y "$(TargetPath).config" "C:\Program Files (x86)\Soft1\Soft1 - Ice\$(TargetFileName).config"
exit 0
```

### Probleme rezolvate în sesiunea curentă
- `GetAncestor<T>` → `GetParentTableRow()` (compatibilitate DLL Softone)
- Flicker la PersonPickerDialog search → `_suppressSearch` flag
- Scroll reset la delete din dynamic list → salvare/restaurare `_pnlBody.AutoScrollPosition`
- `NoScrollComboBox` pentru dropdown clauze și combouri
- `PriorityScrollPanel` pentru scroll prioritar pe sidebar
- Cod înregistrare în header dreapta, centrat vertical față de label
- Multiline height 160px standard (8 rânduri)
- Mențiuni cu toggle checkbox în body
- `DynamicListRow` suport multi-rând (grupare automată per 100%)
- `NumeComplet_Functie` concatenare în `GetPersonProperty`
- Referate sursă: 4 câmpuri pe 2 rânduri cu person picker pentru Întocmitor
