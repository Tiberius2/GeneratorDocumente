# Ghid: Adăugarea unui document nou în Generator HR

> **Fără recompilare.** Adăugarea unui document nou necesită doar două fișiere pe disk:
> un fișier **JSON** (configurația formularului) și un fișier **DOCX** (template-ul Word).
> Nu se modifică niciun fișier `.cs`.

---

## Cuprins

1. [Structura folderelor pe disk](#1-structura-folderelor-pe-disk)
2. [Pasul 1 — Creează template-ul DOCX](#2-pasul-1--creeaz%C4%83-template-ul-docx)
3. [Pasul 2 — Creează fișierul JSON](#3-pasul-2--creeaz%C4%83-fi%C8%99ierul-json)
4. [Referință completă schema JSON](#4-referin%C8%9B%C4%83-complet%C4%83-schema-json)
5. [Tipuri de câmpuri disponibile](#5-tipuri-de-c%C3%A2mpuri-disponibile)
6. [Hookuri disponibile](#6-hookuri-disponibile)
7. [Placeholder-e comune automate](#7-placeholder-e-comune-automate)
8. [Calcul înălțime secțiuni](#8-calcul-%C3%AEn%C4%83l%C8%9Bime-sec%C8%9Biuni)
9. [Adăugarea unei categorii noi](#9-ad%C4%83ugarea-unei-categorii-noi)
10. [Exemple complete](#10-exemple-complete)
11. [Checklist final](#11-checklist-final)
12. [Greșeli frecvente](#12-gre%C8%99eli-frecvente)

---

## 1. Structura folderelor pe disk

Folderul rădăcină este cel indicat de variabila de sistem `TemplateDocsPath`.

```
{TemplateDocsPath}/
  Sabloane Acte Aditionale/
    act_aditional.json
    ActAditional_template.docx
  Sabloane Decizii Suspendare/
    suspendare_acord_parti.json
    template_suspendare_acord_parti.docx
    ...
  Sabloane Decizii Incetare/
    ...
  Sabloane Cercetare Disciplinara/
    ...
  Sabloane Procese Verbale/
    ...
```

**Reguli:**
- Fiecare categorie = un subfolder. Numele folderului e afișat ca titlu de coloană în selector.
- Fiecare document = exact 2 fișiere în același folder: `*.json` + `*.docx`.
- Numele fișierelor nu contează (pot fi orice), atât timp cât JSON-ul referă corect DOCX-ul prin câmpul `"template_file"`.

---

## 2. Pasul 1 — Creează template-ul DOCX

### 2.1 Placeholder-e în template

În documentul Word, scrie placeholder-ele între acolade duble: `{{NumeCamp}}`.

**Exemplu de paragraf în Word:**
```
Subsemnatul {{NumeSalariat}}, CNP {{CNP}}, angajat în funcția de {{Functie}}
la {{NumeAngajator}}, prin prezenta declar...
```

**Reguli obligatorii:**
- Placeholder-ul trebuie să fie **scris continuu**, fără întreruperi de formatare la mijloc. Dacă Word a rupt `{{Nume` și `Salariat}}` în run-uri separate (se întâmplă uneori la copy-paste), șterge și retastează manual.
- Cheia din placeholder (`NumeSalariat`) trebuie să fie **identică** cu `"key"` din JSON sau cu un placeholder comun automat (lista la secțiunea 7).
- Cheile sunt **case-sensitive**: `{{numeangajat}}` ≠ `{{NumeAngajat}}`.

### 2.2 Placeholder-e în tabele

Dacă un placeholder e în interiorul unui tabel și vrei să se expandeze câte un rând per item dintr-o listă dinamică, placeholder-ul **trebuie să fie singurul conținut al rândului template** — nu îl combina cu text fix în aceeași celulă.

### 2.3 Fișiere multi-pagină

Nu există restricții — template-ul poate fi oricâte pagini, poate conține anteturi, subsoluri, tabele complexe, stiluri. Engine-ul înlocuiește placeholder-ele în tot documentul.

---

## 3. Pasul 2 — Creează fișierul JSON

Copiază JSON-ul unui document similar din același folder și adaptează-l.

Salvează fișierul cu encoding **UTF-8** (nu UTF-8 BOM).

### Structura minimă

```json
{
  "title": "Titlul documentului (apare în selector și în header formular)",
  "template_file": "template_nume_fisier.docx",
  "registratura": true,
  "registratura_date_field": "DataDecizie",
  "registratura_tip_doc_pk": 11,
  "sections": [
    {
      "title": "TITLU SECȚIUNE",
      "height": 82,
      "fields": [
        {
          "key": "DataDecizie",
          "label": "Data deciziei",
          "type": "date",
          "label_width_percent": 100,
          "required": true
        }
      ]
    }
  ]
}
```

---

## 4. Referință completă schema JSON

### Nivel rădăcină

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `title` | string | ✅ | Titlul documentului — apare în selector și în header-ul formularului |
| `template_file` | string | ✅ | Numele fișierului DOCX din același folder |
| `registratura` | bool | — | `true` = documentul se înregistrează automat în `CCCVREGISTRATURA` la generare |
| `registratura_date_field` | string | — | Key-ul câmpului de tip `date` din formular folosit ca dată de înregistrare |
| `registratura_tip_doc_pk` | int | — | PK-ul tipului de document din tabela de registratură |
| `order` | int | — | Ordinea cardului în coloana din selector (1, 2, 3...). Documentele fără `order` apar la final, alfabetic |
| `sections` | array | ✅ | Lista secțiunilor formularului |
| `hooks` | array | — | Lista hookurilor (operații automate la deschidere/generare) |

### Secțiune (`sections[]`)

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `title` | string | ✅ | Titlul secțiunii (afișat cu bara colorată în formular) |
| `height` | int | — | Înălțimea în pixeli a panelului secțiunii. Dacă lipsește, se calculează automat. **Vezi secțiunea 8.** |
| `fields` | array | ✅ | Lista câmpurilor din secțiune |

### Câmp (`fields[]`)

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `key` | string | ✅ | Identificatorul unic al câmpului. Folosit ca placeholder în DOCX: `{{key}}` |
| `label` | string | ✅ | Eticheta afișată deasupra câmpului în formular |
| `type` | string | ✅ | Tipul câmpului. **Vezi secțiunea 5.** |
| `label_width_percent` | int | — | Procentul din lățimea rândului ocupat de acest câmp. Câmpurile de pe același rând trebuie să sumeze 100. Dacă lipsește sau e 0 = 100% (câmpul singur pe rând) |
| `required` | bool | — | `true` = câmpul e obligatoriu; validarea blochează generarea dacă e gol |
| `default` | string | — | Valoarea pre-completată la deschiderea formularului |
| `placeholder` | string | — | Text hint gri afișat în câmp când e gol |
| `options` | array | — | Lista de opțiuni pentru tip `combo` |
| `options_sql` | string | — | SQL pentru popularea unui `combo` din baza de date (neimplementat momentan) |
| `maps` | object | — | Pentru `person_picker`: mapare câmpuri de autocompletat după selectarea persoanei |
| `initial_rows` | int | — | Pentru `dynamic_list`: numărul de rânduri inițiale (default 1) |
| `item_fields` | array | — | Pentru `dynamic_list`: definițiile câmpurilor din fiecare rând |
| `table_marker` | string | — | Pentru `expand_table_row`: numele markerului din tabelul DOCX |

### Câmp item din dynamic_list (`item_fields[]`)

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `key` | string | ✅ | Identificatorul câmpului în cadrul rândului |
| `label` | string | ✅ | Eticheta afișată |
| `type` | string | — | `text` (default), `number`, `person_picker` |
| `width_percent` | int | — | Procentul din lățimea rândului (câmpurile dintr-un rând sumează 100) |
| `placeholder` | string | — | Text hint |
| `maps` | object | — | Pentru `person_picker` în liste: autocompletare alte câmpuri din același rând |

---

## 5. Tipuri de câmpuri disponibile

### `text`
TextBox simplu, o linie.
```json
{ "key": "LocMunca", "label": "Locul de muncă", "type": "text", "placeholder": "ex. Depozit" }
```

### `multiline`
TextBox multi-linie, 140px înălțime (~7 rânduri). Nu necesita `height` explicit.
```json
{ "key": "DescriereFapta", "label": "Descrierea faptei", "type": "multiline" }
```

### `date`
DateTimePicker, format `dd.MM.yyyy`. Valoarea în placeholder: `{{DataDecizie}}` → `"14.04.2025"`.
```json
{ "key": "DataDecizie", "label": "Data deciziei", "type": "date", "label_width_percent": 50, "required": true }
```

### `readonly`
TextBox gri, needitabil. Util pentru câmpuri calculate sau informative.
```json
{ "key": "CodInregistrare", "label": "Cod înregistrare", "type": "readonly" }
```
> ⚠️ `CodInregistrare` e afișat automat în header-ul formularului — nu îl pune în secțiuni.

### `number`
NumericUpDown (0–999). Triggerează hookuri `on_change` la modificare.
```json
{ "key": "LuniSuspendare", "label": "Luni suspendare", "type": "number", "label_width_percent": 30 }
```

### `combo`
Dropdown cu valori fixe.
```json
{
  "key": "TipSanctiune",
  "label": "Tipul sancțiunii",
  "type": "combo",
  "options": ["Avertisment scris", "Reducere salariu 10%", "Desfacere CIM"]
}
```

### `person_picker`
Buton care deschide dialogul de selectare angajat. După selectare, completează automat câmpurile definite în `maps`.

```json
{
  "key": "AutorReferat",
  "label": "Autor referat",
  "type": "person_picker",
  "label_width_percent": 100,
  "maps": {
    "NumeAutorReferat": "NumeComplet",
    "FunctieAutorReferat": "Functie"
  }
}
```

**Proprietăți disponibile pentru `maps` (dreapta):**

| Valoare | Ce returnează |
|---------|---------------|
| `NumeComplet` | Nume + Prenume (majuscule) |
| `Nume` | Doar numele de familie |
| `Prenume` | Doar prenumele |
| `CNP` | CNP-ul persoanei |
| `Functie` | Funcția/titlul |
| `CodCor` | Codul COR |
| `NrCim` | Numărul contractului individual de muncă |
| `DataCim` | Data CIM formatată `dd.MM.yyyy` |
| `NumeDepartament` | Numele departamentului |
| `NumeComplet_Functie` | `"IONESCU ION — Brutar"` (concatenare cu em-dash) |

> ⚠️ **Regulă esențială pentru `person_picker`:** câmpul picker în sine (cu `key: "AutorReferat"`) NU generează un placeholder în DOCX. Placeholder-ele vin din câmpurile definite în `maps` (`NumeAutorReferat`, `FunctieAutorReferat`). Acele câmpuri trebuie să existe și în `fields[]` (ca `type: "text"`) pentru a fi vizibile și editabile în formular.

### `dynamic_list`
Secțiune cu rânduri add/delete. Fiecare rând are câmpurile definite în `item_fields`.

```json
{
  "key": "Membri",
  "label": "Membri comisie",
  "type": "dynamic_list",
  "initial_rows": 2,
  "item_fields": [
    {
      "key": "NumeMembru",
      "label": "Nume și prenume",
      "type": "person_picker",
      "width_percent": 50,
      "maps": { "NumeMembru": "NumeComplet", "FunctieMembru": "Functie" }
    },
    { "key": "FunctieMembru", "label": "Funcția", "width_percent": 50 }
  ]
}
```

**Cum apare în DOCX:** pune în template un paragraf (sau rând de tabel) cu placeholder-ele item-urilor:
```
{{NumeMembru}} — {{FunctieMembru}}
```
Engine-ul clonează acel paragraf/rând pentru fiecare item din listă.

> ⚠️ Fiecare placeholder expandabil dintr-o `dynamic_list` trebuie să fie pe **propriul paragraf** în DOCX. Nu combina `{{NumeMembru}} și {{FunctieMembru}}` pe același rând de tabel dacă vrei expandare per rând — pune ambele în celule separate ale aceluiași rând template.

### `clauze_editor`
Specific Actelor Adiționale. Nu se folosește în documente noi din alte categorii.

### `expand_table_row`
Expandare rând dintr-un tabel existent, identificat prin `table_marker`. Folosit în Acte Adiționale.

---

## 6. Hookuri disponibile

Hookurile sunt operații automate definite în `"hooks": []` la nivel de document.

### `SqlOnOpen`
Execută un SQL la deschiderea formularului și pune rezultatul într-un câmp.

```json
{
  "on": "on_open",
  "handler": "SqlOnOpen",
  "params": {
    "query": "SELECT TOP 1 S.CODE AS CodCor FROM PRSJOBPOS PJ JOIN JOBPOSITION J ON PJ.JOBPOSITION=J.JOBPOSITION JOIN SPECIALTY S ON J.SPECIALTY=S.SPECIALTY WHERE PJ.PRSN={PrsnId} AND PJ.COMPANY={CompanyId}",
    "column": "CodCor",
    "target_field": "CodCor"
  }
}
```

Placeholder-e disponibile în `query`: `{PrsnId}`, `{CompanyId}`.

### `SqlOnGenerate`
Identic cu `SqlOnOpen`, dar rulează la momentul generării PDF-ului (nu la deschidere).

### `ConcatList`
Concatenează valorile unui câmp dintr-o `dynamic_list` într-un singur string și îl pune într-un placeholder.

```json
{
  "on": "on_generate",
  "handler": "ConcatList",
  "params": {
    "source_field": "Membri",
    "item_key": "NumeMembru",
    "target_field": "ListaMembri",
    "separator": ", "
  }
}
```

Rezultat: `{{ListaMembri}}` → `"IONESCU ION, POPESCU MARIA, GHEORGHE VASILE"`.

> ⚠️ `item_key` trebuie să fie **exact** cheia din `item_fields`, nu cheia câmpului picker.

### `CalcDataSfarsit`
Calculează o dată de sfârșit = dată start + N luni. Se declanșează la modificarea câmpurilor de tip `date` sau `number`.

```json
{
  "on": "on_change",
  "handler": "CalcDataSfarsit",
  "params": {
    "start_field": "DataStartSuspendare",
    "luni_field": "LuniSuspendare",
    "target_field": "DataSfarsitSuspendare"
  }
}
```

### `InjectArticoleFinal`
Numără ultimul `Art.N` din document și populează automat `{{ArticolCompartiment}}` și `{{ArticolContestatie}}` cu articolele N+1 și N+2. Folosit în decizii cu structură pe articole.

```json
{
  "on": "on_generate",
  "handler": "InjectArticoleFinal",
  "params": {}
}
```

---

## 7. Placeholder-e comune automate

Aceste placeholder-e sunt populate automat din datele ERP — **nu le pune în `fields[]`**, le scrii direct în DOCX.

### Date angajat (persoana selectată)
| Placeholder | Conținut |
|-------------|----------|
| `{{NumeSalariat}}` | Numele complet al angajatului selectat |
| `{{CNP}}` | CNP-ul angajatului |
| `{{Functie}}` | Funcția angajatului |
| `{{NumeDepartament}}` | Departamentul angajatului |
| `{{NrCim}}` | Numărul contractului individual de muncă |
| `{{DataCim}}` | Data CIM (`dd.MM.yyyy`) |
| `{{CodInregistrare}}` | Codul de înregistrare calculat automat (`YYddd/NR`) |

### Date companie
| Placeholder | Conținut |
|-------------|----------|
| `{{NumeAngajator}}` | Denumirea firmei |
| `{{CIFAngajator}}` | CIF-ul firmei |
| `{{ReprezentantLegal}}` | Numele reprezentantului legal |
| `{{FunctieReprezentant}}` | Funcția reprezentantului legal |
| `{{AdresaCompanie}}` | Adresa sediului |
| `{{ZipCompanie}}` | Codul poștal |
| `{{NrRegComertului}}` | Nr. registrul comerțului |
| `{{IbanCompanie}}` | IBAN-ul firmei |
| `{{NrTelefonCompanie}}` | Telefonul firmei |
| `{{EmailCompanie}}` | Email-ul firmei |
| `{{WebsiteCompanie}}` | Website-ul firmei |

### Generate de hookuri
| Placeholder | Populat de |
|-------------|-----------|
| `{{MentiuniDocument}}` | Câmpul de mențiuni din formular (toggle checkbox) |
| `{{ArticolCompartiment}}` | Hook `InjectArticoleFinal` |
| `{{ArticolContestatie}}` | Hook `InjectArticoleFinal` |

---

## 8. Calcul înălțime secțiuni

Câmpul `"height"` din secțiune controlează înălțimea panelului în pixeli.

**Formula:**
```
height = 16 (padding)
       + număr_câmpuri_normale × 58
       + număr_câmpuri_multiline × 172
```

**Exemple rapide:**

| Conținut secțiune | Height |
|-------------------|--------|
| 1 câmp simplu (`date`, `text`, `combo`) | 74 → rotunjit la **82** |
| 2 câmpuri simple pe același rând | 82 |
| 2 câmpuri simple pe rânduri separate | 132 |
| 1 câmp `date` + 1 câmp `multiline` | `16 + 58 + 172` = **246** |
| 2 câmpuri `multiline` | `16 + 172 + 172` = **360** |
| 1 câmp `person_picker` + 2 câmpuri `text` sub el | `16 + 58 + 58 + 58` = **190** → **198** |

> Dacă omiți `"height"`, sistemul îl calculează automat — dar rezultatul poate fi ușor diferit față de formula de mai sus. Recomandat să specifici explicit pentru control precis.

> Pentru secțiuni cu `dynamic_list`, NU specifici `height` pe secțiune — înălțimea listei e controlată de `height` din secțiunea JSON (care devine înălțimea panelului de iteme: `sectionHeight - 42`).

---

## 9. Adăugarea unei categorii noi

Dacă documentul nu se încadrează în nicio categorie existentă:

1. **Creează un subfolder nou** în `{TemplateDocsPath}/` cu numele categoriei, ex: `Sabloane Contracte Speciale`.
2. **Adaugă folderul în `CategoryOrder`** din `DocumentRegistry.cs` (singura modificare de cod necesară), altfel categoria apare la final în selector:
   ```csharp
   private static readonly List<string> CategoryOrder = new List<string>
   {
       "Sabloane Acte Aditionale",
       "Sabloane Decizii Suspendare",
       "Sabloane Decizii Incetare",
       "Sabloane Cercetare Disciplinara",
       "Sabloane Procese Verbale",
       "Sabloane Contracte Speciale"   // ← adaugă aici
   };
   ```
3. **Adaugă tema vizuală** în `DocumentTheme.cs`, metoda `ForCategory()`:
   ```csharp
   if (lower.Contains("contracte speciale"))
       return Acte; // sau o paletă nouă
   ```

Fără pasul 2, categoria tot apare în selector (la final), dar ordinea e impredictibilă.

---

## 10. Exemple complete

### Exemplu A — Document simplu (2 date, un text)

**`notificare_simpla.json`:**
```json
{
  "title": "Notificare Internă",
  "template_file": "template_notificare.docx",
  "registratura": true,
  "registratura_date_field": "DataNotificare",
  "registratura_tip_doc_pk": 11,
  "order": 5,
  "sections": [
    {
      "title": "DATE NOTIFICARE",
      "height": 82,
      "fields": [
        {
          "key": "DataNotificare",
          "label": "Data notificării",
          "type": "date",
          "label_width_percent": 50,
          "required": true
        },
        {
          "key": "TermenRaspuns",
          "label": "Termen răspuns",
          "type": "date",
          "label_width_percent": 50
        }
      ]
    },
    {
      "title": "CONȚINUT",
      "height": 188,
      "fields": [
        {
          "key": "TextNotificare",
          "label": "Textul notificării",
          "type": "multiline"
        }
      ]
    }
  ]
}
```

**Placeholder-e în DOCX:**
```
Către: {{NumeSalariat}}
Data: {{DataNotificare}}

{{TextNotificare}}

Termen răspuns: {{TermenRaspuns}}
```

---

### Exemplu B — Document cu persoană suplimentară și dată calculată

**`suspendare_noua.json`** (fragment):
```json
{
  "title": "Decizie Suspendare Nouă",
  "template_file": "template_suspendare_noua.docx",
  "registratura": true,
  "registratura_date_field": "DataDecizie",
  "registratura_tip_doc_pk": 11,
  "sections": [
    {
      "title": "DATE DECIZIE",
      "height": 82,
      "fields": [
        { "key": "DataDecizie", "label": "Data deciziei", "type": "date",
          "label_width_percent": 50, "required": true },
        { "key": "LuniSuspendare", "label": "Luni suspendare", "type": "number",
          "label_width_percent": 50 }
      ]
    },
    {
      "title": "PERIOADĂ",
      "height": 82,
      "fields": [
        { "key": "DataStartSuspendare", "label": "Data start", "type": "date",
          "label_width_percent": 50, "required": true },
        { "key": "DataSfarsitSuspendare", "label": "Data sfârșit (calculat)",
          "type": "readonly", "label_width_percent": 50 }
      ]
    },
    {
      "title": "ÎNLOCUITOR",
      "height": 140,
      "fields": [
        { "key": "InlocuitorPicker", "label": "Înlocuitor", "type": "person_picker",
          "label_width_percent": 100,
          "maps": { "NumeInlocuitor": "NumeComplet", "FunctieInlocuitor": "Functie" } },
        { "key": "NumeInlocuitor", "label": "Nume înlocuitor", "type": "text",
          "label_width_percent": 50 },
        { "key": "FunctieInlocuitor", "label": "Funcție înlocuitor", "type": "text",
          "label_width_percent": 50 }
      ]
    }
  ],
  "hooks": [
    {
      "on": "on_change",
      "handler": "CalcDataSfarsit",
      "params": {
        "start_field": "DataStartSuspendare",
        "luni_field": "LuniSuspendare",
        "target_field": "DataSfarsitSuspendare"
      }
    }
  ]
}
```

---

### Exemplu C — Document cu listă dinamică și concatenare

```json
{
  "title": "Proces Verbal Predare-Primire",
  "template_file": "template_pv_predare.docx",
  "registratura": false,
  "sections": [
    {
      "title": "BUNURI PREDATE",
      "fields": [
        {
          "key": "Bunuri",
          "label": "Bunuri",
          "type": "dynamic_list",
          "initial_rows": 1,
          "item_fields": [
            { "key": "NumeBun", "label": "Denumire bun", "type": "text", "width_percent": 60 },
            { "key": "Cantitate", "label": "Cantitate", "type": "number", "width_percent": 20 },
            { "key": "SerieNr", "label": "Serie/Nr.", "type": "text", "width_percent": 20 }
          ]
        }
      ]
    }
  ],
  "hooks": [
    {
      "on": "on_generate",
      "handler": "ConcatList",
      "params": {
        "source_field": "Bunuri",
        "item_key": "NumeBun",
        "target_field": "ListaBunuri",
        "separator": "; "
      }
    }
  ]
}
```

**În DOCX**, pentru expandare per rând de tabel:
```
| Nr. | Denumire       | Cantitate   | Serie/Nr.   |
|-----|----------------|-------------|-------------|
|     | {{NumeBun}}    | {{Cantitate}}| {{SerieNr}} |
```
Rândul cu placeholder-e se clonează automat per item.

---

## 11. Checklist final

Înainte să testezi documentul nou, verifică:

- [ ] Fișierul DOCX e salvat și accesibil în folderul categoriei
- [ ] Fișierul JSON e salvat cu encoding UTF-8 (fără BOM)
- [ ] `"template_file"` din JSON corespunde **exact** numelui fișierului DOCX (inclusiv extensia)
- [ ] Toate cheile din `"fields"` sunt unice în document
- [ ] Suma `label_width_percent` pe fiecare rând e exact 100 (sau un singur câmp fără percent = 100% automat)
- [ ] Fiecare placeholder `{{Cheie}}` din DOCX există fie în `fields[]`, fie în lista de placeholder-e comune (secțiunea 7)
- [ ] Placeholder-ele din DOCX sunt scrise continuu, fără întreruperi de formatare la mijloc
- [ ] `item_key` din hookul `ConcatList` e identic cu `key`-ul din `item_fields` (nu cu cel al picker-ului)
- [ ] Dacă ai `dynamic_list`, placeholder-ul expandabil din DOCX e pe propriul paragraf/rând de tabel
- [ ] `"height"` e calculat corect pentru secțiunile cu `multiline` (vezi secțiunea 8)
- [ ] Dacă documentul e într-o categorie nouă, ai adăugat categoria în `CategoryOrder` din `DocumentRegistry.cs`

---

## 12. Greșeli frecvente

**Placeholder rămâne neînlocuit în PDF**
→ Verifică ortografia exactă a cheii în JSON și în DOCX (case-sensitive).
→ Verifică că placeholder-ul nu e rupt în run-uri separate în Word (șterge și retastează).

**`ConcatList` produce string gol**
→ `item_key` nu corespunde cu `key` din `item_fields`. Copierea dintr-un JSON similar poate introduce această eroare.

**Câmpul `multiline` apare turtit (26px)**
→ Ai uitat să specifici `"type": "multiline"` — dacă scrii `"type": "text"` câmpul e single-line.

**Lista dinamică nu expandează în DOCX**
→ Placeholder-ul din DOCX (`{{NumeMembru}}`) nu e pe propriul paragraf — e în același paragraf cu alt text.

**Formularul nu apare în selector**
→ Fie JSON-ul are eroare de sintaxă (validează cu un JSON validator online), fie `"template_file"` pointează la un DOCX care nu există, fie folderul e gol.

**Înălțimea secțiunii e prea mică / câmpurile sunt clipuite**
→ Recalculează `"height"` folosind formula din secțiunea 8, ținând cont de câmpurile `multiline` (172px fiecare, nu 58).

**Data calculată (`CalcDataSfarsit`) nu se actualizează**
→ Hookul e `"on": "on_change"` — se declanșează doar la modificarea unui câmp `date` sau `number`. Verifică că `start_field` și `luni_field` sunt chei exacte din `fields[]`.
