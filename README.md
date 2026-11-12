# ActAditionalPlugin — Generator Documente HR

Plugin pentru **Softone ERP** (C# .NET Framework 4.7.2) care generează documente HR — acte adiționale, decizii, cereri, procese verbale, adeverințe — ca PDF, cu înregistrare automată în registratură.

Interfața este web (**Microsoft Edge WebView2**) în ferestre ale pluginului: selectorul de documente, formularul fiecărui document (cu previzualizarea PDF alături) și editorul de clauze. Documentele sunt **data-driven**: un JSON (formularul) + un DOCX (șablonul Word cu `{{Placeholder}}`) — fără recompilare.

## Pornire din Softone
| Mod | Configurare |
|---|---|
| Comandă `4000502` | orice meniu (`ProgramGeneral`, `[WorksOn("GENERAL")]`) |
| Comandă `4000501` | ecranul PRSNIN, cu angajatul curent preselectat (`Program`) |
| Dll Form | `ActAditionalPlugin.dll;SelectorDialog` |

## Instalare
Se copiază în folderul Soft1 (`C:\Program Files (x86)\Soft1\Soft1 - …\`):
- `ActAditionalPlugin.dll` (dependențele .NET sunt incluse prin Costura)
- `WebView2Loader.dll` (x86, separat — se copiază la build în `bin\x86\Release`)

Pe PC trebuie instalat **Microsoft Edge WebView2 Runtime** (Evergreen) și Microsoft Word (conversia DOCX → PDF).

Variabile de sistem: `TemplateDocsPath` (folderul cu șabloane, câte un subfolder pe categorie) și `RecruitmentDocsPath` (dosarele PDF ale angajaților, `{PrsnId} - {Nume}`).

## Build
```
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" ActAditionalPlugin.csproj /p:Configuration=Release /p:Platform=x86 /p:FodyGenerateXsd=false /v:minimal
```

## Structura
```
PluginEntry.cs            comenzile Softone + pornirea aplicației (AppLauncher)
Models/                   DocumentDefinition (JSON), PersonInfo, ClauzeConfig, DocumentTheme, PluginConfig
Services/                 AppServices (inițializare), DocumentRegistry (șabloane), DynamicTemplateEngine (DOCX),
                          WordHelper (Word → PDF), DocumentGenerationService, RegistraturaService,
                          HookRegistry, ErpDataProvider, ClauzeService
UI/SelectorDialog.cs      selectorul (selector.html)
UI/Web/                   WebHostForm (baza ferestrelor web), WebDocumentForm (form.html), PersonList, WebViewSupport
UI/Web/Assets/            paginile: selector.*, form.*, picker.js (selectorul de angajați), app.css (componente comune)
```

## Documentație
- **[GHID_ADAUGARE_DOCUMENT_NOU.md](GHID_ADAUGARE_DOCUMENT_NOU.md)** — cum se adaugă un document (DOCX + JSON, schema completă, hooks).
- **[CLAUDE.md](CLAUDE.md)** — contextul tehnic complet al proiectului (decizii, capcane Softone / WebView2, istoric).
