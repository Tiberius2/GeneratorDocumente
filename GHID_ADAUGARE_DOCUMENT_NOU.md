# Ghid: adăugarea unui document nou

Un document = **două fișiere** în folderul de șabloane (`TemplateDocsPath`), fără recompilare:

```
{TemplateDocsPath}\
  Sabloane Documente Interne\        ← categoria = numele folderului (coloana din selector)
    cerere_demisie_fara_preaviz.json ← formularul + regulile
    template_cerere_demisie.docx     ← documentul Word cu {{Placeholder}}
```

Documentul apare singur în selector la următoarea deschidere. O categorie nouă = un folder nou (ordinea coloanelor e în `DocumentRegistry.CategoryOrder`; folderele necunoscute apar la final).

---

## 1. Documentul Word (DOCX)

- Locurile completate se scriu `{{NumeCheie}}` — **fără diacritice, fără spații**, case-sensitive.
- Un placeholder rupt în mai multe bucăți de formatare se înlocuiește corect, **dar nu** dacă e într-un hyperlink, „track changes” sau control de conținut.
- Într-un paragraf cu placeholder se pierd: salturile de rând (`Shift+Enter`), tab-urile și desenele. Folosește paragrafe separate, indentare din paragraf și ține desenele în paragrafe fără placeholder.
- Majuscule: efectul de font „All caps” pe placeholder (nu „Change Case”, care strică cheia).
- Liste numerotate Word înaintea unui placeholder pot ascunde primul caracter — evită-le.
- **Liste (dynamic_list):** rândul de tabel (sau paragraful) care conține `{{cheie}}` a unui câmp din listă se repetă pentru fiecare element; dacă lista e goală, rândul dispare. Celulele unite vertical se repetă împreună. `{{NrCrt}}` = numărul elementului.
- Header / footer sunt procesate și ele.

### Placeholder-e automate (nu se pun în formular)
| Grup | Chei |
|---|---|
| Angajat | `NumeSalariat`, `CNP`, `Functie`, `NumeDepartament`, `NrCim`, `DataCim`, `SerieCI`, `NrCI`, `Domiciliu`, `NumarTelefonSalariat`, `EmailSalariat` |
| Companie | `NumeAngajator`, `CIFAngajator`, `ReprezentantLegal`, `FunctieReprezentant`, `AdresaCompanie`, `ZipCompanie`, `NrRegComertului`, `IbanCompanie`, `NrTelefonCompanie`, `EmailCompanie`, `WebsiteCompanie` |
| Document | `CodInregistrare`, `MentiuniDocument` (secțiunea „Mențiuni” a formularului) |
| Hook `InjectArticoleFinal` | `ArticolCompartiment`, `ArticolContestatie` |

`DataInreg` **nu** e automat: dacă documentul îl are, pune un câmp `date` cu cheia `DataInreg`.

---

## 2. Fișierul JSON

### 2.1 Minim
```json
{
  "title": "Cerere Demisie fara Preaviz",
  "template_file": "template_cerere_demisie.docx",
  "registratura_tip_doc_pk": 6,
  "registratura_date_field": "DataCerere",
  "order": 1,
  "sections": [
    {
      "title": "DATE CERERE",
      "fields": [
        { "key": "DataCerere", "label": "Data cererii", "type": "date", "label_width_percent": 50, "required": true },
        { "key": "DataIncetare", "label": "Data încetare contract", "type": "date", "label_width_percent": 50, "required": true }
      ]
    }
  ]
}
```

### 2.2 Rădăcina
| Cheie | Rol |
|---|---|
| `title` | numele documentului: cardul din selector, registratura, numele PDF-ului. **Fără diacritice** (excepție: „Acord Semnătură Electronică”). ⚠️ Semnăturile electronice găsesc PDF-ul după titlu. |
| `template_file` | numele DOCX-ului din același folder |
| `registratura_tip_doc_pk` | tipul de document în registratură; prezent (> 0) = documentul se înregistrează |
| `registratura_date_field` | cheia câmpului `date` care dă data înregistrării (și intră în codul din antet) |
| `order` | ordinea cardului în coloană (1, 2, 3…); fără = la final, alfabetic |
| `sections`, `hooks` | vezi mai jos |

### 2.3 Secțiune
| Cheie | Rol |
|---|---|
| `title` | titlul secțiunii; textul din paranteză apare ca indiciu în dreapta: `"PREDATOR 2 (persoana care semnează)"` |
| `fields` | câmpurile |
| `depends_on` + `depends_value` | secțiunea apare doar când câmpul are valoarea dată (`"A\|B"` + `"x\|y"` = ȘI). Dacă `depends_on` e un comutator Da/Nu, secțiunea apare **în interiorul** comutatorului. |
| `unlockable` | `true` = câmpurile `readonly` din secțiune au butonul „🔒 Deblochează” |

### 2.4 Câmp
| Cheie | Rol |
|---|---|
| `key` | cheia = `{{key}}` din DOCX |
| `label` | eticheta |
| `type` | `text`, `multiline`, `date`, `datetime`, `readonly`, `number`, `combo`, `person_picker`, `dynamic_list`, `clauze_editor` |
| `label_width_percent` | lățimea pe rând; câmpurile se așază pe același rând până la 100. Fără = rând întreg. |
| `required` | obligatoriu (verificat doar când câmpul e vizibil) |
| `placeholder`, `default` | text de ajutor / valoare inițială (`{year}`, `{today}`, tokenurile angajatului) |
| `depends_on` + `depends_value` | câmpul apare doar când condiția e îndeplinită |

Particularități pe tip:
- **date / datetime** — afișare `zz.ll.aaaa` (`hh:mm`), calendar la buton; `"default": ""` = pornește gol (altfel azi). `datetime` cu `time_depends_on` / `time_depends_value` = ora apare doar când condiția e adevărată (ex. comutator Zile / Ore).
- **combo** — `options`. Cu opțiunile `Da`/`Nu` → comutator; cu exact 2 opțiuni → comutator între variante; `"style": "segmented"` → butoane alăturate.
- **readonly** — precompletat automat dacă cheia e una din datele angajatului / companiei (ex. `NumeAngajator`, `CNP`), sau `AutoritateEmitenta` / `DataEliberareCI` (din ERP).
- **person_picker** — buton „Selectează persoana”; `maps` completează alte câmpuri: `{ "NumeSemnatar": "NumeComplet", "FunctieSemnatar": "Functie" }`. Valori: `NumeComplet`, `Nume`, `Prenume`, `CNP`, `Functie`, `CodCor`, `NrCim`, `DataCim`, `NumeDepartament`, `SerieCI`, `NrCI`, `Domiciliu`, `NumeComplet_Functie`; sufixul `:upper` = majuscule.
- **clauze_editor** — punctele Actului adițional (clauzele se configurează din formular, butonul „⚙ Editor clauze”).

### 2.5 Listă (`dynamic_list`)
| Cheie | Rol |
|---|---|
| `item_fields` | câmpurile unui element (vezi 2.6) |
| `edit_mode: "dialog"` | elementele se adaugă / editează într-o fereastră, în formular apar carduri; lista pornește goală |
| `dialog_title`, `summary_fields` | titlul ferestrei / cheile afișate pe card |
| `initial_rows` | câte rânduri goale la deschidere (implicit 1; nu la `dialog`) |
| `initial_items` | elemente precompletate: `[{ "TipMutatie": "Incheiere Contract", "DataMutatie": "{DataCim}" }]` |
| `hide_header`, `add_button_label` | fără titlul listei / textul butonului de adăugare |
| `required` | cel puțin un element completat |

Elementul e valid dacă **primul** câmp e completat.

### 2.6 Câmp de element (`item_fields`)
`key`, `label`, `type` (`text` implicit, `number`, `combo`, `date`, `date_range`, `check_text`, `person_picker`), `width_percent` (implicit 50), `placeholder`, `default`, `options`, `group` (titlu mic, rând nou), plus:
- **number** — `max`, `decimals`, `format` (ex. `"#,##0.##"` → `4.325,5`)
- **date** — `maps` = bucăți din dată: `{ "AnulMutatie": "yyyy", "LunaMutatie": "MM", "ZiMutatie": "dd" }`
- **date_range** — „zz.ll.aaaa - zz.ll.aaaa”
- **check_text** — bifă + text (`check_label`); în document doar textul, dacă e bifat
- **combo** cu `on_select` — completează alte câmpuri din același element: `{ "Modificare Contract": { "TipDoc": "Act aditional" } }`

Tokenuri în `default`, `initial_items`, `on_select`: `{NumeSalariat}`, `{CNP}`, `{Functie}`, `{NumeDepartament}`, `{NrCim}`, `{DataCim}`, `{SerieCI}`, `{NrCI}`, `{Domiciliu}`, `{today}`.

---

## 3. Hooks (`hooks[]`)
`{ "on": "on_open" | "on_change" | "on_generate", "handler": "...", "params": { ... } }` — la `on_generate` contează ordinea.

| Handler | Parametri | Efect |
|---|---|---|
| `SetDefault` | `field`, `value` (`{year}`, `{today}`) | valoare inițială dacă e gol |
| `SqlOnOpen` / `SqlOnGenerate` | `query` (`{PrsnId}`, `{CompanyId}`), `column`, `target_field` | rezultatul SQL în câmp |
| `CalcDataSfarsit` | `start_field`, `luni_field`, `target_field` | dată + luni |
| `CalcDataSfarsitDinCNP` | `cnp_field`, `ani_field`, `target_field` | data nașterii din CNP + ani |
| `CalcPerioadaSuspendare` | `ani_field`, `target_field` | „1 an” / „2 ani” |
| `CalcZileLucratoare` | `start_field`, `end_field`, `target_field` | zile lucrătoare |
| `FormatPerioada` | `start_field`, `end_field`, `target_field` (+ `tip_field`, `tip_ore_value`) | „zz.ll.aaaa - zz.ll.aaaa” (cu oră dacă tipul = ore) |
| `ConcatList` | `source_field` (lista), `item_key` (câmp din `item_fields`), `target_field`, `separator`, `distinct` | valorile listei într-un text |
| `SetIfEquals` | `source_field`, `equals`, `target_field`, `value_if_true`, `value_if_false` | valoare după o alegere (ex. bife `▣` / `□`) |
| `BlankIfNotEquals` | `source_field`, `equals`, `target_field`, `blank` | golește ținta dacă nu s-a ales varianta |
| `InjectArticoleFinal` | — | `{{ArticolCompartiment}}` / `{{ArticolContestatie}}` = ultimul „Art. N” + 1 / + 2 |

Convenții în document:
- **linie punctată albastră:** valoarea `_ _ _ _ _ _ _ _ _ _ _ _ _ _ _` (de obicei din `BlankIfNotEquals`) se colorează albastru — pentru variantele neselectate.
- **text tăiat:** o valoare `~~text~~` (de ex. din `SetIfEquals`) apare tăiată — varianta nealeasă (ex. „săptămâni / ~~luni~~”).
- **bife:** `▣` bifat, `□` nebifat (ca text).
- În **previzualizare**, câmpurile simple necompletate apar ca linie punctată: albastră (opțional) / roșiatică (obligatoriu). PDF-ul final nu le are.

---

## 4. Verificare înainte de a-l pune în folderul real
1. Fiecare `{{Cheie}}` din DOCX are o sursă: câmp, `maps`, element de listă, `target_field` de hook sau placeholder automat.
2. `template_file` = numele exact al DOCX-ului; JSON valid (UTF-8 fără BOM).
3. `registratura_tip_doc_pk` corect (întreabă dacă nu știi) și `registratura_date_field` = cheia unui câmp `date`.
4. `ConcatList.item_key` = cheie din `item_fields` (nu cheia listei).
5. Deschide documentul, verifică previzualizarea (liniile roșii = câmpuri obligatorii goale), apoi o generare de probă.
6. Dacă documentul se semnează electronic: șablonul de semnătură caută PDF-ul după **titlu** și pune semnăturile la coordonate fixe — titlul și paginarea trebuie să rămână cum se așteaptă.
