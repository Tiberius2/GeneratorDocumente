# Changelog — ActAditionalPlugin

Rezumat al modificărilor făcute față de starea descrisă în `PROJECT_CONTEXT.md` (Iunie 2026).

---

## 1. Bug-uri corectate

| # | Problemă | Fix | Fișier(e) |
|---|---|---|---|
| 6 | `ConfirmareDialog` seta `DialogResult.Yes` la click pe "Da", dar `DynamicForm.OnGenerate()` verifica `DialogResult.OK` → generarea PDF nu se declanșa niciodată. | `DialogResult.Yes` → `DialogResult.OK` | `ConfirmareDialog.cs` |
| 1 | Hook-ul `ConcatList` avea `"item_key": "Intocmitor"`, dar cheia reală din `item_fields` era `NumeIntocmitor` → `{{NumeSiFunctieIntocmitorReferat}}` rămânea gol în PDF. | `item_key` corectat în ambele JSON-uri. | `convocare_cercetare.json`, `decizie_constituire_comisie.json` |
| 5 | Hook-ul `CalcDataSfarsit` (event `on_change`) nu se declanșa când se modifica un câmp de tip `date` (`start_field`) — doar `NumericUpDown` triggeruia `on_change`. | Adăugat `dtp.ValueChanged += (s, e) => RunHooks("on_change", null);` în `case "date"` din `BuildFieldControl()`. | `DynamicForm.cs` |
| 2 | `PriorityScrollPanel` era menționată în context, dar nu exista în cod — scroll pe sidebar fără interceptare a `WM_MOUSEWHEEL`. | Clasă nouă (`Panel` + `IMessageFilter`) care interceptează global `WM_MOUSEWHEEL`, verifică dacă mouse-ul e peste panel și scrollează manual, consumând mesajul (oprește un `NumericUpDown`/`ComboBox` cu focus să fure scroll-ul). `_pnlBody` retipizat din `Panel` în `PriorityScrollPanel`. | `DynamicForm.cs` |
| 3 | `PersonInfo` nu avea `NumeDepartament` → `maps: {"X": "NumeDepartament"}` din JSON returna mereu `string.Empty`. | Proprietate nouă `NumeDepartament` în `PersonInfo` + `LEFT JOIN DEPART D ON P.DEPART = D.DEPART AND D.COMPANY = P.COMPANY` și `ISNULL(D.NAME,'') AS NumeDepartament` în `SQL_ANGAJATI` + mapping în `LoadFromErp()` + `case "NumeDepartament"` adăugat în **ambele** metode `GetPersonProperty()` (DynamicForm și DynamicListRow). | `PersonInfo.cs`, `PersonPickerDialog.cs`, `DynamicForm.cs` |
| 4 | Câmpurile autocompletate dintr-un `person_picker` în `dynamic_list` rămâneau `ReadOnly = true` permanent — utilizatorul nu putea corecta manual. | Scos `tb2.ReadOnly = true;` din `BuildPersonPickerCell()`. | `DynamicForm.cs` |

---

## 2. Bug suplimentar găsit: înălțimea câmpurilor `multiline`

Nu era doar lipsa unui standard — erau **două bug-uri reale de layout**, descoperite în două runde succesive:

1. **`AddLabeledInput()`** forța **orice** control la `Height = 26`, indiferent de tip — inclusiv textbox-urile multiline create cu 160px, turtindu-le vizual.
2. **`AddRow()`** crea rândul (`TableLayoutPanel`) cu `Height = 54` **fix** — chiar și după fixul #1, un textbox de 140px era plasat într-un container de 54px și clipuit la marginea acestuia.

### Fix
- Const nou: `MultilineHeight = 140`.
- Helper nou: `FieldControlHeight(FieldDefinition f)` → `140` pentru `multiline`, `26` pentru restul tipurilor.
- `AddRow(Panel, int[])` → `AddRow(Panel, int[], int height = 54)`, apelat cu `rowFields.Max(FieldControlHeight) + 28`.
- `AddLabeledInput()` nu mai suprascrie `Height` pentru `TextBox` cu `Multiline = true`.
- `CalcSectionHeight()` (fallback când JSON nu specifică `"height"`) actualizat: `110` → `MultilineHeight + 32`.
- `BuildRegularFieldInline()` scalat la fel pentru câmpuri multiline mixate în secțiuni cu `dynamic_list`.

### Recalculare `"height"` în JSON
Secțiunile cu câmpuri multiline aveau `"height"` dimensionat pentru bug-ul vechi — au fost recalculate (`16 + nr_multiline × 172 + nr_alte_câmpuri × 58`):

| Fișier | Secțiune | height vechi → nou |
|---|---|---|
| `avertisment_disciplinar.json` | DESCRIERE ABATERI (2 multiline) | 212 → 360 |
| `pv_cercetare_disciplinara.json` | NOTA EXPLICATIVĂ ȘI DESCRIEREA ABATERII | 212 → 246 |
| `pv_cercetare_disciplinara.json` | CONCLUZIILE COMISIEI | 154 → 188 |
| `pv_cercetare_disciplinara.json` | SANCȚIUNEA PROPUSĂ | 154 → 188 |
| `convocare_cercetare.json` | NOTA EXPLICATIVĂ ȘI DESCRIERE ABATERE | 212 → 246 |
| `decizie_constituire_comisie.json` | NOTA EXPLICATIVĂ ȘI DESCRIERE ABATERE | 212 → 246 |
| `incetare_disciplinar.json` | MOTIVELE SANCȚIONĂRII (3 multiline) | 472 → 590 |
| `referat_disciplinar.json` | DESCRIERE FAPTĂ | 154 → 188 |
| `referat_disciplinar.json` | CONSECINȚE ȘI TEMEI LEGAL | 212 → 246 |

---

## 3. Feature nou: `order` pentru carduri în `SelectorDialog`

Înainte: ordinea categoriilor venea din `CategoryOrder` (`DocumentRegistry.cs`), dar documentele *din interiorul* unei categorii erau mereu sortate alfabetic, fără posibilitate de control.

### Implementare
- **`DocumentDefinition.cs`**: proprietate nouă
  ```csharp
  [JsonProperty("order")]
  public int Order { get; set; }
  ```
- **`DocumentRegistry.cs`**: sortare schimbată din
  ```csharp
  category.Documents = category.Documents.OrderBy(d => d.Title).ToList();
  ```
  în
  ```csharp
  category.Documents = category.Documents
      .OrderBy(d => d.Order > 0 ? d.Order : int.MaxValue)
      .ThenBy(d => d.Title)
      .ToList();
  ```

### Utilizare
În JSON-ul documentului, la nivel rădăcină (lângă `"title"`):
```json
"order": 1
```
- Documentele cu `order` explicit (>0) apar primele, în ordine crescătoare.
- Documentele fără `order` (sau `0`) cad la final, sortate alfabetic — comportamentul vechi e păstrat by default; nu e nevoie să adaugi `order` la toate documentele, doar la cele pe care vrei să le repoziționezi.

---

## 4. Fișiere finale modificate

**Cod (`Commited_Code.zip`):**
- `ConfirmareDialog.cs`
- `DynamicForm.cs`
- `PersonInfo.cs`
- `PersonPickerDialog.cs`
- `DocumentDefinition.cs`
- `DocumentRegistry.cs`

**Șabloane / JSON:**
- `convocare_cercetare.json`
- `decizie_constituire_comisie.json`
- `avertisment_disciplinar.json`
- `pv_cercetare_disciplinara.json`
- `incetare_disciplinar.json`
- `referat_disciplinar.json`

---

## 5. Ce NU s-a schimbat

Arhitectura, structura de foldere, schema JSON a documentelor, lista de hook-uri disponibile și placeholder-ele comune din `PROJECT_CONTEXT.md` rămân exact așa cum erau descrise — toate modificările de mai sus sunt bug-fix-uri și un feature nou, construite pe infrastructura existentă, fără breaking changes la schema JSON sau la API-ul intern al engine-ului.
