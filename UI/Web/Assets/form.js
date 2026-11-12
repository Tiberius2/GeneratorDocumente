// ═══════════════════════════════════════════════════════════
//  Generator Documente HR — formular web (WebView2)
//  ETAPA 2: campurile generate din JSON-ul documentului.
//
//  Reguli de afisare:
//   - combo cu optiunile Nu / Da          -> comutator Da / Nu
//     (sectiunile cu depends_on pe el = "Da" apar in interiorul lui)
//   - combo cu exact doua optiuni         -> comutator intre variante
//   - dynamic_list cu edit_mode "dialog"  -> carduri + fereastra de editare
//   - dynamic_list (implicit)              -> randuri editabile in formular
//
//  Valori interne: date = "yyyy-mm-dd", datetime = "yyyy-mm-ddThh:mm".
//  Spre C# se trimit in formatul documentului (dd.MM.yyyy, dd.MM.yyyy HH:mm).
// ═══════════════════════════════════════════════════════════
"use strict";

// ── utilitare ────────────────────────────────────────────────
const $ = (s, r) => (r || document).querySelector(s);
const $$ = (s, r) => [...(r || document).querySelectorAll(s)];
const esc = s => String(s ?? "").replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
const pad2 = n => String(n).padStart(2, "0");
const clone = o => JSON.parse(JSON.stringify(o));
const norm = s => String(s ?? "").trim().toLowerCase();

function todayIso() {
  const d = new Date();
  return d.getFullYear() + "-" + pad2(d.getMonth() + 1) + "-" + pad2(d.getDate());
}
function roToIso(s) {
  const m = /^(\d{1,2})\.(\d{1,2})\.(\d{4})/.exec(String(s ?? "").trim());
  return m ? `${m[3]}-${pad2(m[2])}-${pad2(m[1])}` : "";
}
function isoToRo(s) {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(s || "");
  return m ? `${m[3]}.${m[2]}.${m[1]}` : "";
}
function roToIsoDT(s) {
  const m = /^(\d{1,2})\.(\d{1,2})\.(\d{4})(?:\s+(\d{1,2}):(\d{2}))?/.exec(String(s ?? "").trim());
  return m ? `${m[3]}-${pad2(m[2])}-${pad2(m[1])}T${pad2(m[4] || 0)}:${m[5] || "00"}` : "";
}
function isoDTToRo(s) {
  const m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(s || "");
  return m ? `${m[3]}.${m[2]}.${m[1]} ${m[4]}:${m[5]}` : "";
}
function parseNum(v) {
  const s = String(v ?? "").trim();
  if (!s) return NaN;
  return s.indexOf(",") >= 0 ? Number(s.replace(/\./g, "").replace(",", ".")) : Number(s);
}

// ── camp de data propriu: <ro-date type="date|datetime"> ─────
// Afiseaza mereu zz.ll.aaaa (si hh:mm), indiferent de limba sistemului.
// Punctele / spatiul / ":" se pun automat cand se tasteaza cifrele.
// Butonul deschide calendarul nativ (input ascuns). Proprietatea .value
// e in format ISO ("yyyy-mm-dd" / "yyyy-mm-ddThh:mm"), ca la input-urile
// native, deci restul codului il foloseste la fel.
const CAL_ICON = `<svg viewBox="0 0 24 24" width="17" height="17" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round"><rect x="3.5" y="5" width="17" height="15.5" rx="2.5"/><path d="M3.5 10h17M8 3v4M16 3v4"/></svg>`;

function roDateMask(txt, withTime) {
  const lens = withTime ? [2, 2, 4, 2, 2] : [2, 2, 4];
  const seps = [".", ".", " ", ":"];
  const groups = txt.split(/\D+/);
  const endsSep = /\D$/.test(txt);
  const parts = [];
  let s = 0, partial = false;
  for (let gi = 0; gi < groups.length && s < lens.length && !partial; gi++) {
    let g = groups[gi];
    const closed = gi < groups.length - 1 || endsSep;   // grup urmat de separator tastat
    while (g.length && s < lens.length) {
      const take = g.slice(0, lens[s]);
      g = g.slice(lens[s]);
      if (take.length === lens[s]) { parts.push(take); s++; continue; }
      if (closed && !g.length) {
        parts.push(s === 2 ? (take.length === 2 ? "20" + take : take) : take.padStart(2, "0"));
        s++;
      } else { parts.push(take); partial = true; }
      break;
    }
  }
  let out = "";
  parts.forEach((p, i) => { out += (i ? seps[i - 1] : "") + p; });
  // dupa un grup complet se pune automat separatorul urmator
  if (!partial && parts.length && parts.length < lens.length &&
      parts[parts.length - 1].length === lens[parts.length - 1]) out += seps[parts.length - 1];
  return out;
}

function roDateParse(txt, withTime) {
  const m = /^(\d{1,2})\.(\d{1,2})\.(\d{4})(?:\s+(\d{1,2}):(\d{2}))?$/.exec(String(txt || "").trim());
  if (!m) return null;
  const d = +m[1], mo = +m[2], y = +m[3];
  const dt = new Date(y, mo - 1, d);
  if (dt.getFullYear() !== y || dt.getMonth() !== mo - 1 || dt.getDate() !== d) return null;
  const iso = `${y}-${pad2(mo)}-${pad2(d)}`;
  if (!withTime) return iso;
  const h = m[4] != null ? +m[4] : 0, mi = m[5] != null ? +m[5] : 0;
  if (h > 23 || mi > 59) return null;
  return `${iso}T${pad2(h)}:${pad2(mi)}`;
}

function roDateShow(v, withTime) {
  const m = /^(\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2}))?/.exec(v || "");
  if (!m) return "";
  return `${m[3]}.${m[2]}.${m[1]}` + (withTime ? ` ${m[4] || "00"}:${m[5] || "00"}` : "");
}

if (typeof customElements !== "undefined" && typeof HTMLElement !== "undefined") {
  customElements.define("ro-date", class extends HTMLElement {
    get type() { return this.getAttribute("type") === "datetime" ? "datetime" : "date"; }
    get withTime() { return this.type === "datetime"; }
    get value() { return this._v != null ? this._v : (this.getAttribute("value") || ""); }
    set value(v) {
      v = String(v ?? "");
      if (!this.withTime && v.length > 10) v = v.slice(0, 10);
      this._v = v;
      if (this._t) { this._t.value = roDateShow(v, this.withTime); this.classList.remove("bad"); }
    }
    connectedCallback() {
      if (this._t) return;
      const wt = this.withTime;
      this._v = this.value;
      this.innerHTML =
        `<input class="inp" type="text" inputmode="numeric" autocomplete="off" spellcheck="false" placeholder="${wt ? "zz.ll.aaaa hh:mm" : "zz.ll.aaaa"}" maxlength="${wt ? 16 : 10}">` +
        `<button type="button" class="dt-btn" tabindex="-1" title="Alege din calendar">${CAL_ICON}</button>` +
        `<input class="dt-native" type="${wt ? "datetime-local" : "date"}" tabindex="-1" aria-hidden="true">`;
      this._t = this.children[0];
      this._b = this.children[1];
      this._n = this.children[2];
      this._t.value = roDateShow(this._v, wt);

      this._t.addEventListener("input", e => {
        // la tastare (nu la stergere) refacem formatul: 12112026 -> 12.11.2026
        if (!e.inputType || e.inputType.indexOf("insert") === 0) {
          const atEnd = this._t.selectionStart === this._t.value.length;
          if (atEnd) this._t.value = roDateMask(this._t.value, wt);
        }
        const txt = this._t.value.trim();
        if (!txt) { this._v = ""; this.classList.remove("bad"); return; }
        let iso = roDateParse(txt, wt);
        if (!iso && wt) {           // data completa fara ora -> ora 00:00 (sau cea existenta)
          const d = roDateParse(txt, false);
          if (d) iso = d + "T" + (/T(\d{2}:\d{2})/.exec(this._v || "") || [0, "00:00"])[1];
        }
        if (iso) { this._v = iso; this.classList.remove("bad"); }
        // altfel: valoare incompleta — evenimentul trece mai departe, dar .value ramane vechea data
      });
      this._t.addEventListener("blur", () => {
        const txt = this._t.value.trim();
        if (!txt) return;
        // text invalid / incomplet -> revenim la ultima data valida
        this._t.value = roDateShow(this._v, wt);
      });
      this._b.addEventListener("click", () => {
        if (this._t.disabled) return;
        this._n.value = this._v || "";
        try { this._n.showPicker(); } catch (e) { this._n.focus(); this._n.click(); }
      });
      this._n.addEventListener("change", e => {
        e.stopPropagation();
        let v = this._n.value;
        if (!v) return;
        this.value = v;
        this.dispatchEvent(new Event("input", { bubbles: true }));
        this._t.focus();
      });
      this._n.addEventListener("input", e => e.stopPropagation());
    }
  });
}

// ── legatura cu C# (WebDocumentForm) ─────────────────────────
const host = window.chrome && window.chrome.webview ? window.chrome.webview : null;
function send(msg) {
  if (host) host.postMessage(msg);
  else console.log("[fara host]", msg);
}
const pending = {};
let reqSeq = 0;
function request(msg) {
  return new Promise(resolve => {
    const id = "r" + (++reqSeq);
    pending[id] = resolve;
    send(Object.assign({ requestId: id }, msg));
  });
}
function onMessage(msg) {
  switch (msg.type) {
    case "init": init(msg); break;
    case "changed": applyChanged(msg.values || {}); break;
    case "clauze": clauzeUpdated(msg.clauze || []); break;
    case "clauzeConfig": clConfigLoaded(msg); break;
    case "clauzeSaved": clSaved(msg); break;
    case "cod": $("#codInreg").textContent = msg.cod || "—"; break;
    case "previewDone":
      previewDone(msg);
      break;
    case "confirmGenerate": confirmGenerate(msg); break;
    case "generated": generated(msg); break;
    case "generateDone":       // generarea a esuat
      setBusy(false);
      if (msg.error) showMsg({ kind: "err", title: "Documentul nu a fost generat", text: msg.error, buttons: [["OK", "acc", true]] });
      break;
    case "personPicked": {
      const r = pending[msg.requestId];
      if (r) { delete pending[msg.requestId]; r(msg); }
      break;
    }
  }
}
if (host) host.addEventListener("message", e => onMessage(e.data));

// ── stare ────────────────────────────────────────────────────
const HEADER_FIELDS = new Set(["NrCim", "DataCim", "NumeDepartament"]);   // apar deja in cardul angajatului
const app = {
  def: null, employee: {},
  fields: {},        // key -> definitie camp (nivel formular)
  fieldSec: {},      // key -> indexul sectiunii
  vals: {},          // valori simple (si tinte maps / hooks)
  lists: {},         // key lista -> [{ values: {}, extra: {} }]
  open: {},          // key lista -> Set(indexuri carduri extinse)
  nested: {},        // key comutator Da/Nu -> [sectiuni afisate in interiorul lui]
  nestedSecs: new Set(),
  hasOnChange: false,
  ment: { on: false, text: "" },   // mentiuni (MentiuniDocument)
  clauze: [],        // clauzele configurate (Act Aditional), din C#
  puncte: {},        // key clauze_editor -> [punct]
  edit: null        // { key, idx, item } — elementul din fereastra de editare
};

// ── tipuri de combo ──────────────────────────────────────────
function comboKind(f) {
  const o = (f.options || []).map(String);
  if (o.length === 2) return o.map(norm).sort().join("|") === "da|nu" ? "nuda" : "duo";
  return "select";
}
const isDa = v => norm(v) === "da";
const daOpt = f => (f.options || []).map(String).find(o => norm(o) === "da") || "Da";
const nuOpt = f => (f.options || []).map(String).find(o => norm(o) === "nu") || "Nu";

// token-uri cu datele angajatului (initial_items, on_select, default)
function resolveTokens(v) {
  if (typeof v !== "string" || v.indexOf("{") < 0) return v;
  const e = app.employee;
  const map = {
    NumeSalariat: e.NumeSalariat, CNP: e.CNP, Functie: e.Functie, NumeDepartament: e.NumeDepartament,
    NrCim: e.NrCim, DataCim: e.DataCim, SerieCI: e.SerieCI, NrCI: e.NrCI, Domiciliu: e.Domiciliu,
    today: isoToRo(todayIso()),
    year: String(new Date().getFullYear())
  };
  return v.replace(/\{(\w+)\}/g, (m, k) => (k in map ? (map[k] || "") : m));
}

// ═══════════════════════════════════════════════════════════
//  INITIALIZARE
// ═══════════════════════════════════════════════════════════
function init(msg) {
  if (app.def) return;                 // "init" poate veni de doua ori (rezerva din C#)
  app.def = msg.definition || {};
  app.employee = msg.employee || {};
  app.clauze = msg.clauze || [];
  if (window.PersonPicker) PersonPicker.setPersons(msg.persons || []);
  applyTheme(msg.theme);

  $("#docTitle").textContent = msg.title || app.def.title || "";
  $("#docCat").textContent = msg.category || "";
  $("#codInreg").textContent = msg.cod || "—";
  document.title = msg.title || "Generator Documente HR";

  renderEmployee();
  buildModel(msg.initialValues || {});
  render();
  sendRegDate();    // codul de inregistrare pentru data initiala
  schedulePreview(400);   // prima previzualizare, imediat dupa deschidere
}

// Data de inregistrare (registratura_date_field) -> C# recalculeaza codul din antet
function sendRegDate() {
  const k = app.def && app.def.registratura_date_field;
  if (!k || !app.fields[k]) return;
  send({ type: "regDate", value: toRo(app.fields[k], app.vals[k]) });
}

function applyTheme(t) {
  if (!t) return;
  const r = document.documentElement.style;
  r.setProperty("--accent", t.accent);
  r.setProperty("--accent-dark", t.accentDark);
  r.setProperty("--accent-pal", t.accentPal);
  r.setProperty("--accent-border", t.accentBorder);
}

function renderEmployee() {
  const e = app.employee;
  const rows = [["Angajat", e.NumeSalariat], ["CNP", e.CNP], ["Funcție", e.Functie],
                ["Nr. CIM", e.NrCim], ["Data CIM", e.DataCim], ["Departament", e.NumeDepartament]];
  $("#empGrid").innerHTML = rows.map(([l, v]) =>
    `<div><small>${l}</small><b title="${esc(v)}">${esc(v) || "—"}</b></div>`).join("");
  $("#avatar").textContent = String(e.NumeSalariat || "?").split(/\s+/).filter(Boolean)
    .map(p => p[0]).slice(0, 2).join("").toUpperCase();
}

function buildModel(iv) {
  const secs = app.def.sections || [];
  secs.forEach((s, i) => (s.fields || []).forEach(f => {
    if (!f || !f.key) return;
    app.fields[f.key] = f;
    app.fieldSec[f.key] = i;
  }));

  // sectiuni afisate in interiorul unui comutator Da/Nu
  secs.forEach((s, i) => {
    const on = s.depends_on;
    if (!on || on.indexOf("|") >= 0) return;
    const f = app.fields[on];
    if (f && f.type === "combo" && comboKind(f) === "nuda" && isDa(s.depends_value) && app.fieldSec[on] !== i) {
      (app.nested[on] = app.nested[on] || []).push(s);
      app.nestedSecs.add(i);
    }
  });

  Object.values(app.fields).forEach(f => {
    if (f.type === "dynamic_list") app.lists[f.key] = initialItems(f);
    else if (f.type === "clauze_editor") app.puncte[f.key] = [newPunct()];
    else app.vals[f.key] = initialValue(f, iv);
  });
  // valori din hooks pentru chei care nu sunt campuri (raman disponibile in document)
  Object.keys(iv).forEach(k => { if (!(k in app.vals) && !app.fields[k]) app.vals[k] = String(iv[k]); });

  app.hasOnChange = (app.def.hooks || []).some(h => h && h.on === "on_change");
}

function initialValue(f, iv) {
  const has = Object.prototype.hasOwnProperty.call(iv, f.key);
  const v = has ? String(iv[f.key]) : (f.default != null ? resolveTokens(String(f.default)) : null);
  // "default": "" pe date / datetime = campul porneste gol (ex. date de valabilitate)
  const emptyOk = f.default === "" && !has;
  switch (f.type) {
    case "date": return roToIso(v) || (emptyOk ? "" : todayIso());
    case "datetime": return roToIsoDT(v) || (emptyOk ? "" : todayIso() + "T00:00");
    case "number": return v != null && v !== "" ? String(parseInt(v, 10) || 0) : "0";
    case "combo": {
      const o = (f.options || []).map(String);
      return v != null && o.indexOf(v) >= 0 ? v : (o[0] || "");
    }
    case "person_picker": return "";
    default: return v != null ? v : "";
  }
}

// ── elemente de lista ────────────────────────────────────────
const itemField = (lf, k) => (lf.item_fields || []).find(x => x.key === k);
const isDialogList = f => norm(f.edit_mode) === "dialog";
const addLabel = f => f.add_button_label || ("Adaugă " + String(f.label || "").toLowerCase());

function itemBlank(itf) {
  switch (itf.type || "text") {
    case "combo": {
      const o = (itf.options || []).map(String);
      const d = itf.default != null ? String(itf.default) : null;
      return d && o.indexOf(d) >= 0 ? d : (o[0] || "");
    }
    case "date": return todayIso();
    case "date_range": return [todayIso(), todayIso()];
    case "check_text": return { on: false, text: "" };
    case "number": return "";
    default: return itf.default != null && !itf.placeholder ? String(itf.default) : "";
  }
}

// Seteaza o valoare venita ca text (initial_items / on_select / persoana)
function setItemValue(item, itf, raw) {
  const v = resolveTokens(String(raw ?? ""));
  switch (itf.type || "text") {
    case "combo":
      if ((itf.options || []).map(String).indexOf(v) >= 0) item.values[itf.key] = v;
      break;
    case "date": {
      const iso = roToIso(v);
      if (iso) item.values[itf.key] = iso;
      break;
    }
    case "date_range": {
      const p = v.split(/\s+-\s+/);
      const a = roToIso(p[0]), b = roToIso(p[1]);
      if (a) item.values[itf.key] = [a, b || a];
      break;
    }
    case "check_text":
      item.values[itf.key] = { on: v !== "", text: v };
      break;
    case "number": {
      const n = parseNum(v);
      if (!isNaN(n)) item.values[itf.key] = String(n);
      break;
    }
    default:
      if (v !== "") item.values[itf.key] = v;
  }
}

function newItem(lf, initValues) {
  const item = { values: {}, extra: {} };
  (lf.item_fields || []).forEach(itf => { item.values[itf.key] = itemBlank(itf); });
  if (initValues) Object.keys(initValues).forEach(k => {
    const itf = itemField(lf, k);
    if (itf) setItemValue(item, itf, initValues[k]);
    else item.extra[k] = resolveTokens(String(initValues[k] ?? ""));
  });
  return item;
}

function initialItems(f) {
  if (Array.isArray(f.initial_items) && f.initial_items.length)
    return f.initial_items.map(v => newItem(f, v));
  if (isDialogList(f)) return [];                       // lista "dialog" porneste goala
  const n = f.initial_rows > 0 ? f.initial_rows : 1;    // implicit un rand
  return Array.from({ length: n }, () => { const it = newItem(f); applyAllOnSelect(f, it); return it; });
}

function isItemEmpty(v, type) {
  if (type === "check_text") return !(v && v.on && String(v.text || "").trim());
  if (type === "number") return v == null || String(v).trim() === "" || Number(v) === 0;
  return v == null || String(v).trim() === "";
}

// on_select: combo-uri si date se seteaza mereu; text / numar / check_text doar daca sunt goale
function applyOnSelect(lf, item, itf) {
  const rules = itf.on_select && itf.on_select[item.values[itf.key]];
  if (!rules) return [];
  const changed = [];
  Object.keys(rules).forEach(k => {
    const t = itemField(lf, k);
    if (!t) return;
    const tt = t.type || "text";
    const onlyIfEmpty = tt === "text" || tt === "number" || tt === "check_text";
    if (onlyIfEmpty && !isItemEmpty(item.values[k], tt)) return;
    const before = JSON.stringify(item.values[k]);
    setItemValue(item, t, rules[k]);
    if (JSON.stringify(item.values[k]) !== before) changed.push(k);
  });
  return changed;
}
function applyAllOnSelect(lf, item) {
  (lf.item_fields || []).forEach(itf => {
    if ((itf.type || "text") === "combo" && itf.on_select) applyOnSelect(lf, item, itf);
  });
}

// Randul e "valid" daca primul camp are valoare (ca DynamicListRow.IsValid)
function itemValid(lf, it) {
  const first = (lf.item_fields || [])[0];
  if (!first) return false;
  const t = first.type || "text";
  if (t === "text" || t === "person_picker") return String(it.values[first.key] || "").trim() !== "";
  return true;
}

function display(itf, v) {
  switch (itf.type || "text") {
    case "date": return isoToRo(v);
    case "date_range": return v && v[0] ? isoToRo(v[0]) + " - " + isoToRo(v[1] || v[0]) : "";
    case "check_text": return v && v.on ? String(v.text || "") : "";
    case "number": {
      if (v === "" || v == null) return "";
      const n = Number(v);
      if (isNaN(n)) return String(v);
      return n.toLocaleString("ro-RO", {
        minimumFractionDigits: 0, maximumFractionDigits: itf.decimals > 0 ? itf.decimals : 0,
        useGrouping: !!(itf.format && itf.format.indexOf(",") >= 0)
      });
    }
    default: return v == null ? "" : String(v);
  }
}

// ═══════════════════════════════════════════════════════════
//  RANDARE
// ═══════════════════════════════════════════════════════════
const CHEV = `<svg class="chev" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="square"><polyline points="5,9 12,16 19,9"/></svg>`;

// "PARAGRAFE FINALE (Da = ...; Nu = ...)" -> titlu + indiciu
function splitTitle(t) {
  const m = /^(.*?)\s*\((.+)\)\s*$/.exec(String(t || ""));
  return m ? [m[1], m[2]] : [String(t || ""), ""];
}

// randuri: campurile se aduna pana la 100% (fara procent = rand intreg)
function groupTop(fields) {
  const rows = [];
  let cur = [], sum = 0;
  fields.forEach(f => {
    const w = f.label_width_percent > 0 ? f.label_width_percent : 100;
    if (sum + w > 100 && cur.length) { rows.push(cur); cur = []; sum = 0; }
    cur.push(f); sum += w;
    if (sum >= 100) { rows.push(cur); cur = []; sum = 0; }
  });
  if (cur.length) rows.push(cur);
  return rows;
}
// ca DynamicListRow.GroupRows: implicit 50%, "group" incepe rand nou
function groupRows(fields) {
  const rows = [];
  let cur = [], sum = 0;
  fields.forEach(f => {
    const pct = f.width_percent > 0 ? f.width_percent : 50;
    if ((sum + pct > 100 || f.group) && cur.length) { rows.push(cur); cur = []; sum = 0; }
    cur.push(f); sum += pct;
    if (sum >= 100) { rows.push(cur); cur = []; sum = 0; }
  });
  if (cur.length) rows.push(cur);
  return rows;
}
function cols(row, key, filler) {
  const ws = row.map(f => (f[key] > 0 ? f[key] : 0));
  const tot = ws.reduce((a, b) => a + b, 0);
  const zero = ws.filter(w => !w).length;
  const rem = zero ? Math.max((100 - tot) / zero, 10) : 0;
  const parts = ws.map(w => (w || rem) + "fr");
  if (filler && !zero && tot < 100) parts.push((100 - tot) + "fr");
  return parts.join(" ");
}

function render() {
  const secs = app.def.sections || [];
  let html = "", n = 0;
  secs.forEach((s, i) => {
    if (app.nestedSecs.has(i)) return;
    n++;
    const [title, hint] = splitTitle(s.title);
    html += `<section class="sec" id="sec-${i}" data-sec="${i}" data-title="${esc(title)}">
      <div class="sec-h"><span class="num">${n}</span><span>${esc(title)}</span>${hint ? `<span class="hint">${esc(hint)}</span>` : ""}${s.unlockable ? `<button type="button" class="btn btn-ghost btn-sm sec-lock" data-unlock="${i}" title="Permite modificarea datelor preluate din ERP">🔒 Deblochează</button>` : ""}</div>
      <div class="sec-b">${fieldsHtml(s.fields || [])}</div>
    </section>`;
  });
  html += mentiuniHtml(n + 1);
  $("#sections").innerHTML = html;
  Object.keys(app.lists).forEach(renderList);
  Object.keys(app.puncte).forEach(renderPuncte);

  $("#nav").innerHTML = $$(".sec:not(.sec-ment)").map(s =>
    `<button type="button" class="chip" data-go="${s.id}"><i></i>${esc(s.dataset.title)}</button>`).join("");

  updateDeps();
  updateNav();
}

function fieldsHtml(fields) {
  const fs = fields.filter(f => f && f.key && !HEADER_FIELDS.has(f.key));
  const blocks = [];
  let buf = [];
  const flush = () => {
    if (!buf.length) return;
    blocks.push(groupTop(buf).map(r =>
      `<div class="row" style="grid-template-columns:${cols(r, "label_width_percent", true)}">${r.map(fieldHtml).join("")}</div>`).join(""));
    buf = [];
  };
  fs.forEach(f => {
    if (app.nested[f.key]) { flush(); blocks.push(paraHtml(f)); }
    else if (f.type === "dynamic_list") { flush(); blocks.push(listHtml(f)); }
    else if (f.type === "clauze_editor") { flush(); blocks.push(clauzeHtml(f)); }
    else buf.push(f);
  });
  flush();
  return blocks.map(b => `<div class="blk">${b}</div>`).join("");
}

// continutul butonului de alegere persoana: gol -> iconita + indemn;
// ales -> numele + eticheta "Schimba" (sa fie clar ca se poate alege alta)
const PERSON_ICON = `<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 3.6-7 8-7s8 3 8 7"/></svg>`;
function pickerInner(v, emptyText) {
  return v
    ? `<span class="pk-ico">${PERSON_ICON}</span><span class="pk-name">${esc(v)}</span><span class="pk-act">↻ Schimbă</span>`
    : `<span class="pk-ico">${PERSON_ICON}</span><span class="pk-name">${esc(emptyText)}</span>`;
}

// datetime cu "time_depends_on" / "time_depends_value": ora se afiseaza doar
// cand conditia e adevarata (ex. comutator Zile / Ore); altfel doar data.
function showsTime(f) {
  return !f.time_depends_on || isActive(f.time_depends_on, f.time_depends_value);
}
function rerenderTimeFields(k) {
  Object.values(app.fields).forEach(f => {
    if (f.type !== "datetime" || !f.time_depends_on) return;
    if (String(f.time_depends_on).split("|").indexOf(k) < 0) return;
    const w = $(`[data-fw="${CSS.escape(f.key)}"]`);
    if (w) w.outerHTML = fieldHtml(f);
  });
}

function labelHtml(f) {
  return `<span class="l">${esc(f.label || f.key)}${f.required ? '<i class="req">*</i>' : ""}</span>`;
}

function comboHtml(f, v, attr) {
  const opts = (f.options || []).map(String);
  if (f.style === "segmented" && opts.length)
    return `<div class="seg" ${attr} data-kind="seg">${opts.map(o =>
      `<button type="button" class="opt${o === v ? " on" : ""}" data-v="${esc(o)}">${esc(o)}</button>`).join("")}</div>`;
  const kind = comboKind(f);
  if (kind === "nuda") {
    const on = isDa(v);
    return `<div class="swf${on ? " on" : ""}"><label class="switch"><input type="checkbox" ${attr} data-kind="nuda"${on ? " checked" : ""}><span></span></label><span class="sw-lbl">${on ? "Da" : "Nu"}</span></div>`;
  }
  if (kind === "duo") {
    const isB = v === opts[1];
    return `<div class="duo" ${attr} data-kind="duo"><button type="button" class="opt${!isB ? " on" : ""}" data-v="${esc(opts[0])}">${esc(opts[0])}</button><label class="switch"><input type="checkbox"${isB ? " checked" : ""}><span></span></label><button type="button" class="opt${isB ? " on" : ""}" data-v="${esc(opts[1])}">${esc(opts[1])}</button></div>`;
  }
  return `<select class="inp" ${attr}>${opts.map(o => `<option${o === v ? " selected" : ""}>${esc(o)}</option>`).join("")}</select>`;
}

function fieldHtml(f) {
  const k = f.key, v = app.vals[k], a = `data-f="${esc(k)}"`;
  let ctl;
  switch (f.type) {
    case "multiline":
      ctl = `<textarea class="inp" ${a} placeholder="${esc(f.placeholder || "")}">${esc(v)}</textarea>`; break;
    case "readonly":
      ctl = `<input class="inp" ${a} readonly value="${esc(v)}">`; break;
    case "date":
      ctl = `<ro-date class="dt" type="date" ${a} value="${esc(v)}"></ro-date>`; break;
    case "datetime":
      ctl = showsTime(f)
        ? `<ro-date class="dt" type="datetime" ${a} value="${esc(v)}"></ro-date>`
        : `<ro-date class="dt" type="date" ${a} value="${esc(String(v || "").slice(0, 10))}"></ro-date>`;
      break;
    case "number":
      ctl = `<input class="inp" type="number" min="0" max="999" step="1" ${a} value="${esc(v)}">`; break;
    case "combo":
      ctl = comboHtml(f, v, a); break;
    case "person_picker":
      ctl = `<button type="button" class="picker${v ? " set" : ""}" data-pick="${esc(k)}" title="${v ? "Apasă pentru a alege altă persoană" : "Apasă pentru a alege persoana"}">${pickerInner(v, "Selectează persoana…")}</button>`; break;
    case "clauze_editor":
      ctl = `<div class="notice">Editorul de clauze nu este disponibil încă în formularul nou.</div>`; break;
    default:
      ctl = `<input class="inp" ${a} value="${esc(v)}" placeholder="${esc(f.placeholder || "")}">`;
  }
  return `<div class="f" data-fw="${esc(k)}">${labelHtml(f)}${ctl}<small class="err">Câmp obligatoriu</small></div>`;
}

// Mentiuni ({{MentiuniDocument}}) — ascunse implicit,
// vizibile la bifare; la debifare textul se goleste. Nu apar in navigare.
function mentiuniHtml(n) {
  const m = app.ment;
  return `<section class="sec sec-ment" id="sec-ment">
    <div class="sec-h"><span class="num">${n}</span><span>MENȚIUNI</span><span class="hint">opțional</span></div>
    <div class="sec-b"><div class="para${m.on ? " on" : ""}" id="mentBox">
      <div class="para-h"><div class="t">Adaugă mențiuni în document</div>
        <span class="sw-lbl">${m.on ? "Da" : "Nu"}</span>
        <label class="switch"><input type="checkbox" data-ment="on"${m.on ? " checked" : ""}><span></span></label>
      </div>
      <div class="para-b"><textarea class="inp" data-ment="text" placeholder="Textul mențiunilor…">${esc(m.text)}</textarea></div>
    </div></div>
  </section>`;
}

// comutator Da/Nu cu sectiunile dependente in interior
function paraHtml(f) {
  const on = isDa(app.vals[f.key]);
  const subsDefs = app.nested[f.key];
  const subs = subsDefs.map(s => {
    const [t] = splitTitle(s.title);
    const cap = subsDefs.length > 1 || norm(t) !== norm(f.label) ? `<div class="sub-cap">${esc(t)}</div>` : "";
    return `<div class="sub">${cap}${fieldsHtml(s.fields || [])}</div>`;
  }).join("");
  return `<div class="para${on ? " on" : ""}" data-fw="${esc(f.key)}">
    <div class="para-h"><div class="t">${esc(f.label || f.key)}</div>
      <span class="sw-lbl">${on ? "Da" : "Nu"}</span>
      <label class="switch"><input type="checkbox" data-f="${esc(f.key)}" data-kind="nuda"${on ? " checked" : ""}><span></span></label>
    </div>
    <div class="para-b">${subs}</div>
  </div>`;
}

// ── clauze Act Aditional (clauze_editor) ─────────────────────
// Fiecare punct: clauza aleasa din lista configurata (sau "Text liber"),
// campurile ei, apoi textul rezultat (referinta + text) — ca in
// PunctModificareControl. Textul se poate edita manual.
const LIBER = "__liber";
function newPunct() { return { sel: "", vals: [], liber: "", manual: false, mref: "", mtext: "" }; }
const clauzaById = id => app.clauze.find(c => c.id === id);
const fmtClauza = (t, vals) => String(t || "").replace(/\{(\d+)\}/g, (m, i) => (vals[+i] != null ? vals[+i] : ""));

function campText(cp, v) {
  if (norm(cp.tip) === "data") return isoToRo(v);
  return String(v ?? "").trim();
}

// textul final al punctului: { ref, text } sau null daca e necompletat
function punctResult(p) {
  if (p.manual) return p.mtext.trim() ? { ref: p.mref.trim(), text: p.mtext.trim() } : null;
  if (!p.sel) return null;
  if (p.sel === LIBER) return p.liber.trim() ? { ref: "", text: p.liber.trim() } : null;
  const c = clauzaById(p.sel);
  if (!c) return null;
  const cs = c.campuri || [];
  if (!cs.length) return { ref: c.referinta || "", text: c.text || "" };
  const vals = cs.map((cp, j) => campText(cp, p.vals[j]));
  if (vals.every(v => !v)) return null;
  return { ref: c.referinta || "", text: fmtClauza(c.text, vals) };
}

function clauzeHtml(f) {
  const k = esc(f.key);
  return `<div class="f dl" data-fw="${k}">
    <div class="list-tools"><span class="l">Fiecare punct modifică o clauză din contract</span>
      <button type="button" class="btn btn-ghost btn-sm" data-cledit title="Configurează lista de clauze (pentru toate actele adiționale)">⚙ Editor clauze</button></div>
    <div data-puncte="${k}"></div>
    <div class="add-row"><button type="button" class="btn btn-ghost btn-sm" data-addp="${k}">＋ Adaugă clauză</button></div>
    <small class="err">Completează cel puțin o clauză</small></div>`;
}

function renderPuncte(key) {
  const box = $(`[data-puncte="${CSS.escape(key)}"]`);
  if (!box) return;
  const ps = app.puncte[key] || [];
  box.innerHTML = ps.length ? ps.map((p, i) => punctHtml(key, p, i)).join("")
                            : `<div class="empty">Nicio clauză. Apasă „＋ Adaugă clauză”.</div>`;
}

function punctHtml(key, p, i) {
  const opts = `<option value=""${p.sel ? "" : " selected"} disabled>Alege clauza modificată…</option>` +
    app.clauze.map(c => `<option value="${esc(c.id)}"${c.id === p.sel ? " selected" : ""}>${esc(c.titlu)}</option>`).join("") +
    `<option value="${LIBER}"${p.sel === LIBER ? " selected" : ""}>Text liber</option>`;
  return `<div class="irow pct" data-pct="${esc(key)}" data-idx="${i}">
    <div class="irow-h pct-h"><span>${i + 1}.</span>
      <select class="inp pct-sel" data-pf="sel">${opts}</select>
      <button type="button" class="btn btn-x" data-delp title="Șterge">✕</button></div>
    <div class="pct-b">${punctBodyHtml(p)}</div>
  </div>`;
}

function punctBodyHtml(p) {
  if (p.manual) {
    return `<div class="pct-edit">
      <div class="f"><span class="l">Referința clauzei</span><input class="inp" data-pf="mref" value="${esc(p.mref)}"></div>
      <div class="f"><span class="l">Textul modificării</span><textarea class="inp" data-pf="mtext">${esc(p.mtext)}</textarea></div>
      <div class="pct-acts"><span class="hint-s">Text editat manual</span><button type="button" class="btn btn-ghost btn-sm" data-preset>↺ Revino la textul generat</button></div>
    </div>`;
  }
  if (!p.sel) return "";
  let inputs = "";
  if (p.sel === LIBER) {
    inputs = `<div class="f"><span class="l">Textul clauzei</span><textarea class="inp" data-pf="liber" placeholder="Introduceți textul clauzei…">${esc(p.liber)}</textarea></div>`;
  } else {
    const c = clauzaById(p.sel);
    const cs = (c && c.campuri) || [];
    if (cs.length) {
      inputs = `<div class="row" style="grid-template-columns:repeat(${Math.min(cs.length, 3)}, 1fr)">` + cs.map((cp, j) => {
        const v = p.vals[j] ?? "";
        const a = `data-pf="v" data-ci="${j}"`;
        const ctl = norm(cp.tip) === "data"
          ? `<ro-date class="dt" type="date" ${a} value="${esc(v)}"></ro-date>`
          : `<input class="inp" ${a} value="${esc(v)}" placeholder="${esc(cp.placeholder || "")}"${norm(cp.tip).indexOf("numar") === 0 ? ' inputmode="decimal"' : ""}>`;
        return `<div class="f"><span class="l">${esc(cp.label || "Valoare")}</span>${ctl}</div>`;
      }).join("") + `</div>`;
    }
  }
  return inputs + `<div class="pct-prev-wrap">${punctPreviewHtml(p)}</div>`;
}

function punctPreviewHtml(p) {
  const r = punctResult(p);
  if (!r) return "";
  return `<div class="pct-prev">
    ${r.ref ? `<small>${esc(r.ref)}</small>` : ""}<b>${esc(r.text)}</b>
    <div class="pct-acts"><span class="hint-s">Așa apare în document</span><button type="button" class="btn btn-ghost btn-sm" data-pedit>✎ Editează textul</button></div>
  </div>`;
}

function punctCtx(el) {
  const row = el.closest("[data-pct]");
  if (!row) return null;
  const key = row.dataset.pct, idx = +row.dataset.idx;
  const p = (app.puncte[key] || [])[idx];
  return p ? { key, idx, p, row } : null;
}

function rerenderPunct(ctx) {
  $(".pct-b", ctx.row).innerHTML = punctBodyHtml(ctx.p);
}

// filtrare la tastare pentru campurile numerice ale clauzelor
function filterNumar(input, tip) {
  const t = norm(tip);
  let v = input.value;
  if (t === "numar intreg") v = v.replace(/\D/g, "");
  else if (t === "numar zecimal") {
    v = v.replace(/[^\d.,]/g, "");
    const m = /^(\d*)([.,]?)(\d*)/.exec(v);
    v = m[1] + m[2] + m[3].replace(/[.,]/g, "");
  }
  if (v !== input.value) input.value = v;
  return v;
}

// returneaza true daca evenimentul apartine unui punct de clauza
function onPunctInput(t) {
  const pf = t.closest("[data-pf]");
  const ctx = pf && punctCtx(pf);
  if (!ctx) return false;
  const p = ctx.p, kind = pf.dataset.pf;
  if (kind === "sel") {
    if (p.sel === pf.value) return true;
    p.sel = pf.value; p.vals = []; p.liber = ""; p.manual = false;
    rerenderPunct(ctx);
    const first = $(".pct-b input, .pct-b textarea", ctx.row);
    if (first) first.focus();
  } else if (kind === "v") {
    const c = clauzaById(p.sel);
    const cp = c && (c.campuri || [])[+pf.dataset.ci];
    const v = pf.tagName === "RO-DATE" ? pf.value : filterNumar(pf, cp ? cp.tip : "");
    if (p.vals[+pf.dataset.ci] === v) return true;
    p.vals[+pf.dataset.ci] = v;
    $(".pct-prev-wrap", ctx.row).innerHTML = punctPreviewHtml(p);
  } else if (kind === "liber") {
    if (p.liber === pf.value) return true;
    p.liber = pf.value;
    $(".pct-prev-wrap", ctx.row).innerHTML = punctPreviewHtml(p);
  } else if (kind === "mref") { if (p.mref === pf.value) return true; p.mref = pf.value; }
  else if (kind === "mtext") { if (p.mtext === pf.value) return true; p.mtext = pf.value; }
  afterListChange(ctx.key);
  return true;
}

// returneaza true daca click-ul a fost tratat
function onPunctClick(t) {
  if (t.closest("[data-cledit]")) { openClEditor(); return true; }
  const add = t.closest("[data-addp]");
  if (add) {
    const key = add.dataset.addp;
    (app.puncte[key] = app.puncte[key] || []).push(newPunct());
    renderPuncte(key);
    afterListChange(key);
    const rows = $$(`[data-pct="${CSS.escape(key)}"]`);
    const sel = rows.length && $(".pct-sel", rows[rows.length - 1]);
    if (sel) sel.focus();
    return true;
  }
  const ctx = punctCtx(t);
  if (!ctx) return false;
  if (t.closest("[data-delp]")) {
    confirmDel(`Ștergi clauza nr. ${ctx.idx + 1}?`, () => {
      app.puncte[ctx.key].splice(ctx.idx, 1);
      renderPuncte(ctx.key);
      afterListChange(ctx.key);
    });
    return true;
  }
  if (t.closest("[data-pedit]")) {
    const r = punctResult(ctx.p) || { ref: "", text: "" };
    ctx.p.manual = true; ctx.p.mref = r.ref; ctx.p.mtext = r.text;
    rerenderPunct(ctx);
    const ta = $('[data-pf="mtext"]', ctx.row);
    if (ta) ta.focus();
    return true;
  }
  if (t.closest("[data-preset]")) {
    ctx.p.manual = false;
    rerenderPunct(ctx);
    afterListChange(ctx.key);
    return true;
  }
  return false;
}

// lista de clauze s-a schimbat (Editor clauze) — pastram ce se mai potriveste
function clauzeUpdated(list) {
  app.clauze = list;
  Object.keys(app.puncte).forEach(key => {
    app.puncte[key].forEach(p => {
      if (p.sel && p.sel !== LIBER && !clauzaById(p.sel) && !p.manual) { p.sel = ""; p.vals = []; }
    });
    renderPuncte(key);
    afterListChange(key);
  });
  toast("Clauzele au fost salvate — lista din formular e actualizată");
}

function collectClauze() {
  const o = {};
  Object.keys(app.puncte).forEach(k => {
    o[k] = (app.puncte[k] || []).map(punctResult).filter(Boolean)
      .map(r => ({ referinta: r.ref, text: r.text }));
  });
  return o;
}

// ═══════════════════════════════════════════════════════════
//  EDITORUL DE CLAUZE (configuratia pentru toate actele aditionale)
//  Gandit pentru tehnician, nu pentru programator:
//   - textul nou se scrie ca o fraza; ce completeaza operatorul se pune
//     intre paranteze drepte: "Salariul de baza brut lunar: [Salariu nou] RON."
//   - campurile se deduc din text (tipul se ghiceste din nume, se poate schimba)
//   - un singur ecran: seturi de clauze (= tipuri de contract) + clauzele
//     setului ca lista; clauza aleasa se editeaza direct in lista
//  La salvare se converteste in formatul existent ({0}, {1} + campuri), deci
//  ClauzeService si documentul raman neschimbate.
// ═══════════════════════════════════════════════════════════
const CAMP_TIPURI = [["text", "Text"], ["numar intreg", "Număr întreg"], ["numar zecimal", "Sumă / număr cu zecimale"], ["data", "Dată"]];
const cl = { cfg: null, set: 0, open: -1, dirty: false, busy: false };
const rid = () => Math.random().toString(16).slice(2, 10).padEnd(8, "0");
const BR_RE = /\[([^\[\]\n]+)\]/g;

function normTipCamp(t) {
  const n = norm(t || "text");
  return ({ string: "text", integer: "numar intreg", decimal: "numar zecimal", date: "data" })[n] || n;
}

// tipul campului ghicit din nume (se poate schimba din lista)
function guessTip(name) {
  const n = " " + norm(name) + " ";
  if (/\sdat[aă]\s/.test(n) || n.indexOf("data ") >= 0) return "data";
  if (/(salariu|sum[aă]|lei|ron|spor|cuantum|valoare|pre[tț])/.test(n)) return "numar zecimal";
  if (/(zile|ore|num[aă]r|\snr\.?\s|luni|ani|procent)/.test(n)) return "numar intreg";
  return "text";
}

// ── conversie format salvat <-> format de lucru ─────────────
// lucru: { id, titlu, textClauza, text: "...[Camp]...", activ, meta: { nume: {tipCamp, placeholder} } }
function clFromStore(k) {
  const camps = (k.campuri || []).slice().sort((a, b) => (a.ordine || 0) - (b.ordine || 0));
  const used = new Set();
  const labels = camps.map((f, j) => {
    let l = String(f.label || "").replace(/[\[\]\n]/g, " ").trim() || ("Câmp " + (j + 1));
    const base = l; let n = 2;
    while (used.has(norm(l))) l = base + " " + (n++);
    used.add(norm(l));
    return l;
  });
  const meta = {};
  camps.forEach((f, j) => { meta[labels[j]] = { tipCamp: normTipCamp(f.tipCamp), placeholder: f.placeholder || "" }; });
  const text = String(k.textDinamic || "").replace(/\{(\d+)\}/g, (m, n) =>
    "[" + (labels[+n] || ("Câmp " + (+n + 1))) + "]");
  return { id: k.id || rid(), titlu: k.titlu || "", textClauza: k.textClauza || "", text, activ: k.activ !== false, meta };
}

function clFields(k) {
  const out = [], seen = new Set();
  let m;
  BR_RE.lastIndex = 0;
  while ((m = BR_RE.exec(k.text || ""))) {
    const n = m[1].trim();
    if (n && !seen.has(norm(n))) { seen.add(norm(n)); out.push(n); }
  }
  return out;
}

function clMeta(k, name, create) {
  const key = Object.keys(k.meta).find(x => norm(x) === norm(name));
  if (key) return k.meta[key];
  if (!create) return null;
  return (k.meta[name] = { tipCamp: guessTip(name), placeholder: "" });
}

function clToStore(k) {
  const names = clFields(k), idx = {};
  names.forEach((n, i) => { idx[norm(n)] = i; });
  const textDinamic = String(k.text || "").replace(BR_RE, (m, n) => {
    const i = idx[norm(n.trim())];
    return i == null ? m : "{" + i + "}";
  });
  const campuri = names.map((n, i) => {
    const me = clMeta(k, n) || {};
    return { label: n, placeholder: me.placeholder || "", ordine: i, tipCamp: me.tipCamp || guessTip(n) };
  });
  return { id: k.id, titlu: String(k.titlu).trim(), textClauza: k.textClauza, textDinamic, activ: !!k.activ, campuri };
}

// ── deschidere / incarcare ──────────────────────────────────
function openClEditor() {
  cl.cfg = null; cl.open = -1; cl.dirty = false; cl.busy = false;
  $("#clBody").innerHTML = `<div class="empty">Se încarcă clauzele…</div>`;
  $("#clSt").textContent = "";
  $("#btnClSave").disabled = true;
  $("#ovCl").classList.add("show");
  send({ type: "loadClauzeConfig" });
}

function clConfigLoaded(msg) {
  if (!$("#ovCl").classList.contains("show")) return;
  if (msg.error || !msg.config) {
    $("#clBody").innerHTML = `<div class="notice">Clauzele nu au putut fi încărcate: ${esc(msg.error || "răspuns gol")}</div>`;
    return;
  }
  const c = msg.config;
  cl.cfg = {
    tipSelectatId: c.tipSelectatId || "",
    seturi: (c.tipuriContract || []).map(t => ({
      id: t.id || rid(), nume: t.nume || "", activ: t.activ !== false,
      clauze: (t.clauze || []).map(clFromStore)
    }))
  };
  if (!cl.cfg.seturi.length) cl.cfg.seturi.push({ id: rid(), nume: "CIM Standard", activ: true, clauze: [] });
  const sel = cl.cfg.seturi.findIndex(t => t.id === cl.cfg.tipSelectatId);
  cl.set = sel >= 0 ? sel : 0;
  $("#btnClSave").disabled = false;
  renderCl();
}

function clMark() {
  cl.dirty = true;
  $("#clSt").textContent = "Modificări nesalvate";
}

const clSet = () => cl.cfg && cl.cfg.seturi[cl.set];

// ── randare ─────────────────────────────────────────────────
function renderCl(focusSel) {
  if (!cl.cfg) return;
  const st = $("#clBody").scrollTop;
  $("#clBody").innerHTML = clHelpHtml() + clSetHtml() + clListHtml();
  $("#clBody").scrollTop = st;
  if (focusSel) setTimeout(() => {
    const el = $(focusSel, $("#clBody"));
    if (el) { el.focus(); if (el.select) el.select(); }
  }, 30);
}

function clHelpHtml() {
  return `<div class="cl-help">
    <b>Cum scrii o clauză:</b> un <b>titlu</b> (ce alege operatorul din listă), textul <b>fix</b> și <b>textul nou</b>.
    În textul nou, ce completează operatorul pui între paranteze drepte, de ex.
    <code>Salariul de bază brut lunar: [Salariu nou] RON.</code> — „Salariu nou” devine automat un câmp în formular.
  </div>`;
}

function clSetHtml() {
  const c = cl.cfg, s = clSet();
  const implicit = s.id === c.tipSelectatId;
  const opts = c.seturi.map((x, i) =>
    `<option value="${i}"${i === cl.set ? " selected" : ""}>${x.id === c.tipSelectatId ? "★ " : ""}${esc(x.nume || "(fără nume)")} — ${x.clauze.length} clauze</option>`).join("");
  return `<div class="cl-sec">
    <div class="row" style="grid-template-columns:1fr 1fr">
      <div class="f"><span class="l">Setul de clauze afișat</span><select class="inp" data-clf="set">${opts}</select></div>
      <div class="f"><span class="l">Numele setului</span><input class="inp" data-clf="setNume" value="${esc(s.nume)}" placeholder="ex. CIM Standard"></div>
    </div>
    <div class="cl-acts">
      ${implicit ? `<span class="cl-badge">★ Acesta e setul folosit în formular</span>`
                 : `<button type="button" class="btn btn-ghost btn-sm" data-cla="implicit">★ Folosește acest set în formular</button>`}
      <span class="grow"></span>
      <button type="button" class="btn btn-ghost btn-sm" data-cla="copySet" title="Set nou pornind de la clauzele acestuia">⧉ Copie a setului</button>
      <button type="button" class="btn btn-ghost btn-sm" data-cla="newSet">＋ Set nou gol</button>
      <button type="button" class="btn btn-ghost btn-sm cl-danger" data-cla="delSet">✕ Șterge setul</button>
    </div>
  </div>`;
}

function clListHtml() {
  const s = clSet();
  const cards = s.clauze.map((k, i) => clCardHtml(k, i, s.clauze.length)).join("");
  return `<div class="cl-sec">
    <div class="cl-cap">Clauzele setului „${esc(s.nume || "(fără nume)")}” <span class="hint-s">${s.clauze.length} · ordinea de aici e ordinea din formular · clic pe o clauză ca s-o modifici</span></div>
    ${cards || `<div class="empty">Setul nu are clauze. Apasă „＋ Clauză nouă”.</div>`}
    <div class="add-row"><button type="button" class="btn btn-acc btn-sm" data-cla="addCl">＋ Clauză nouă</button></div>
  </div>`;
}

function clSummary(k) {
  const t = String(k.text || "").replace(/\s+/g, " ").trim();
  return t || "fără text nou";
}

function clCardHtml(k, i, n) {
  const open = cl.open === i;
  return `<div class="cl-card${open ? " open" : ""}${k.activ ? "" : " off"}" data-ci="${i}">
    <div class="cl-ch" data-cla="toggle" data-i="${i}">
      <div class="cl-ord"><button type="button" data-cla="up" data-i="${i}" title="Mută mai sus"${i ? "" : " disabled"}>▲</button><button type="button" data-cla="down" data-i="${i}" title="Mută mai jos"${i < n - 1 ? "" : " disabled"}>▼</button></div>
      <div class="cl-txt"><b>${esc(k.titlu || "(fără titlu)")}</b>${k.activ ? "" : '<span class="cl-off">ascunsă în formular</span>'}
        <small data-sum="${i}">${esc(clSummary(k))}</small></div>
      <button type="button" class="btn btn-ghost btn-sm" data-cla="dup" data-i="${i}" title="Duplică clauza">⧉</button>
      <button type="button" class="btn btn-x" data-cla="delCl" data-i="${i}" title="Șterge clauza">✕</button>
      ${CHEV}
    </div>
    ${open ? clBodyHtml(k, i) : ""}
  </div>`;
}

function clBodyHtml(k, i) {
  return `<div class="cl-cb">
    <div class="f"><span class="l">Titlu — cum apare în lista operatorului</span>
      <input class="inp" data-clf="titlu" data-i="${i}" value="${esc(k.titlu)}" placeholder="ex. Salariu de bază (Lit. I, pct. 1)"></div>
    <div class="f"><span class="l">Ce se modifică — textul fix</span>
      <textarea class="inp cl-ta sm" data-clf="textClauza" data-i="${i}" placeholder="ex. La litera I, punctul 1, „Salariul de bază” se modifică și va avea următorul cuprins:">${esc(k.textClauza)}</textarea></div>
    <div class="f"><span class="l">Textul nou — pune între [paranteze drepte] ce completează operatorul</span>
      <textarea class="inp cl-ta" data-clf="text" data-i="${i}" placeholder="ex. Salariul de bază brut lunar: [Salariu nou] RON.">${esc(k.text)}</textarea>
      <div class="cl-acts"><button type="button" class="btn btn-ghost btn-sm" data-cla="addField" data-i="${i}">＋ Câmp de completat</button>
        <span class="hint-s">adaugă [Câmp nou] unde e cursorul — apoi scrie-i numele</span></div></div>
    <div data-fields="${i}">${clFieldsHtml(k, i)}</div>
    <div class="cl-bottom">
      <div class="swf${k.activ ? " on" : ""}"><label class="switch"><input type="checkbox" data-clf="activ" data-i="${i}"${k.activ ? " checked" : ""}><span></span></label>
        <span class="cl-sw-t">Afișează clauza în lista din formular</span></div>
    </div>
    <div class="f"><span class="l">Exemplu — așa apare în document</span><div data-prev="${i}">${clPrevHtml(k)}</div></div>
  </div>`;
}

function clFieldsHtml(k, i) {
  const names = clFields(k);
  if (!names.length)
    return `<div class="cl-nof">Nu sunt câmpuri de completat — textul nou intră în document exact cum e scris.</div>`;
  return `<div class="cl-fields"><span class="l">Câmpurile pe care le completează operatorul</span>` +
    names.map(n => {
      const me = clMeta(k, n, true);
      return `<div class="cl-frow"><span class="cl-tok">${esc(n)}</span>
        <select class="inp" data-clf="ftip" data-i="${i}" data-name="${esc(n)}" title="Ce fel de valoare se scrie">${CAMP_TIPURI.map(([v, l]) => `<option value="${v}"${v === me.tipCamp ? " selected" : ""}>${l}</option>`).join("")}</select>
        <input class="inp" data-clf="fex" data-i="${i}" data-name="${esc(n)}" value="${esc(me.placeholder)}" placeholder="exemplu arătat operatorului (opțional)">
      </div>`;
    }).join("") + `</div>`;
}

function clPrevHtml(k) {
  const text = String(k.text || "").replace(BR_RE, (m, n) => {
    const me = clMeta(k, n.trim());
    return "\u0001" + ((me && me.placeholder) || n.trim()) + "\u0002";
  });
  const html = esc(text).replace(/\u0001/g, '<span class="cl-ph">').replace(/\u0002/g, "</span>");
  return `<div class="pct-prev">${k.textClauza ? `<small>${esc(k.textClauza)}</small>` : ""}<b>${html || "—"}</b></div>`;
}

// ── evenimente ──────────────────────────────────────────────
function clInput(e) {
  const el = e.target.closest("[data-clf]");
  if (!el || !cl.cfg) return;
  const s = clSet(), f = el.dataset.clf, i = +el.dataset.i;
  const k = s && s.clauze[i];
  switch (f) {
    case "set":
      if (e.type !== "change") return;
      cl.set = +el.value; cl.open = -1; renderCl(); return;
    case "setNume": {
      s.nume = el.value;
      const o = $(`[data-clf="set"] option[value="${cl.set}"]`);
      if (o) o.textContent = (s.id === cl.cfg.tipSelectatId ? "★ " : "") + (el.value || "(fără nume)") + " — " + s.clauze.length + " clauze";
      break;
    }
    case "titlu": {
      k.titlu = el.value;
      const b = $(`.cl-card[data-ci="${i}"] .cl-txt b`);
      if (b) b.textContent = el.value || "(fără titlu)";
      break;
    }
    case "textClauza": k.textClauza = el.value; break;
    case "text": {
      k.text = el.value;
      const box = $(`[data-fields="${i}"]`);
      if (box && !box.contains(document.activeElement)) box.innerHTML = clFieldsHtml(k, i);
      const sm = $(`[data-sum="${i}"]`);
      if (sm) sm.textContent = clSummary(k);
      break;
    }
    case "activ": {
      k.activ = el.checked; syncSwitch(el, el.checked);
      const card = $(`.cl-card[data-ci="${i}"]`);
      if (card) card.classList.toggle("off", !el.checked);
      break;
    }
    case "ftip": clMeta(k, el.dataset.name, true).tipCamp = el.value; break;
    case "fex": clMeta(k, el.dataset.name, true).placeholder = el.value; break;
    default: return;
  }
  clMark();
  if (k) { const p = $(`[data-prev="${i}"]`); if (p) p.innerHTML = clPrevHtml(k); }
}

function clClick(e) {
  if (e.target.closest("[data-cl-close]")) { closeClEditor(); return; }
  const b = e.target.closest("[data-cla]");
  if (!b || !cl.cfg || b.disabled) return;
  const c = cl.cfg, s = clSet(), i = +b.dataset.i;
  const swap = (arr, a, z) => { const x = arr[a]; arr[a] = arr[z]; arr[z] = x; };
  const copyClauza = k => ({ id: rid(), titlu: k.titlu, textClauza: k.textClauza, text: k.text, activ: k.activ, meta: clone(k.meta) });
  switch (b.dataset.cla) {
    case "toggle": cl.open = cl.open === i ? -1 : i; renderCl(cl.open === i ? `[data-clf="titlu"][data-i="${i}"]` : null); return;
    case "up": if (i > 0) { swap(s.clauze, i, i - 1); if (cl.open === i) cl.open = i - 1; else if (cl.open === i - 1) cl.open = i; clMark(); renderCl(); } return;
    case "down": if (i < s.clauze.length - 1) { swap(s.clauze, i, i + 1); if (cl.open === i) cl.open = i + 1; else if (cl.open === i + 1) cl.open = i; clMark(); renderCl(); } return;
    case "addCl":
      s.clauze.push({ id: rid(), titlu: "Clauză nouă", textClauza: "", text: "", activ: true, meta: {} });
      cl.open = s.clauze.length - 1; clMark();
      renderCl(`[data-clf="titlu"][data-i="${cl.open}"]`);
      setTimeout(() => { const card = $(`.cl-card[data-ci="${cl.open}"]`); if (card) card.scrollIntoView({ block: "nearest", behavior: "smooth" }); }, 60);
      return;
    case "dup": {
      const k = copyClauza(s.clauze[i]);
      k.titlu = (k.titlu || "Clauză") + " (copie)";
      s.clauze.splice(i + 1, 0, k);
      cl.open = i + 1; clMark();
      renderCl(`[data-clf="titlu"][data-i="${i + 1}"]`);
      return;
    }
    case "delCl":
      confirmDel(`Ștergi clauza „${s.clauze[i].titlu || "(fără titlu)"}”?`, () => {
        s.clauze.splice(i, 1);
        if (cl.open === i) cl.open = -1; else if (cl.open > i) cl.open--;
        clMark(); renderCl();
      });
      return;
    case "addField": {
      const ta = $(`[data-clf="text"][data-i="${i}"]`);
      if (!ta) return;
      const k = s.clauze[i];
      let name = "Câmp nou", n = 2;
      while (clFields(k).some(x => norm(x) === norm(name))) name = "Câmp nou " + (n++);
      const st = ta.selectionStart ?? ta.value.length, en = ta.selectionEnd ?? st;
      const before = ta.value.slice(0, st), sep = before && !/\s$/.test(before) ? " " : "";
      ta.value = before + sep + "[" + name + "]" + ta.value.slice(en);
      ta.focus();
      ta.selectionStart = st + sep.length + 1;            // numele selectat: se scrie direct peste el
      ta.selectionEnd = ta.selectionStart + name.length;
      ta.dispatchEvent(new Event("input", { bubbles: true }));
      return;
    }
    case "implicit": c.tipSelectatId = s.id; clMark(); renderCl(); return;
    case "newSet":
      c.seturi.push({ id: rid(), nume: "Set nou", activ: true, clauze: [] });
      cl.set = c.seturi.length - 1; cl.open = -1; clMark();
      renderCl('[data-clf="setNume"]');
      return;
    case "copySet":
      c.seturi.push({ id: rid(), nume: (s.nume || "Set") + " (copie)", activ: true, clauze: s.clauze.map(copyClauza) });
      cl.set = c.seturi.length - 1; cl.open = -1; clMark();
      renderCl('[data-clf="setNume"]');
      toast("Am făcut o copie a setului — schimbă-i numele și modifică clauzele");
      return;
    case "delSet":
      if (c.seturi.length <= 1) { toast("Trebuie să rămână cel puțin un set de clauze"); return; }
      confirmDel(`Ștergi setul „${s.nume}” cu toate cele ${s.clauze.length} clauze?`, () => {
        if (c.tipSelectatId === s.id) c.tipSelectatId = (c.seturi.find(x => x !== s) || {}).id || "";
        c.seturi.splice(cl.set, 1);
        cl.set = 0; cl.open = -1; clMark(); renderCl();
      });
      return;
  }
}

function closeClEditor(force) {
  if (!force && cl.dirty) {
    confirmDel("Ai modificări nesalvate în clauze. Închizi fără să le salvezi?", () => closeClEditor(true), "Modificări nesalvate");
    return;
  }
  $("#ovCl").classList.remove("show");
  cl.cfg = null; cl.dirty = false;
}

function clSave() {
  if (!cl.cfg || cl.busy) return;
  const c = cl.cfg;
  for (let si = 0; si < c.seturi.length; si++) {
    const s = c.seturi[si];
    if (!String(s.nume).trim()) { cl.set = si; cl.open = -1; renderCl('[data-clf="setNume"]'); toast("Completează numele setului de clauze"); return; }
    const bad = s.clauze.findIndex(k => !String(k.titlu).trim());
    if (bad >= 0) { cl.set = si; cl.open = bad; renderCl(`[data-clf="titlu"][data-i="${bad}"]`); toast("Completează titlul clauzei"); return; }
  }
  if (!c.seturi.some(t => t.id === c.tipSelectatId)) c.tipSelectatId = c.seturi[0].id;
  const out = {
    tipSelectatId: c.tipSelectatId,
    tipuriContract: c.seturi.map(s => ({ id: s.id, nume: String(s.nume).trim(), activ: !!s.activ, clauze: s.clauze.map(clToStore) }))
  };
  cl.busy = true;
  $("#btnClSave").disabled = true;
  $("#clSt").textContent = "Se salvează…";
  send({ type: "saveClauzeConfig", config: out });
}

function clSaved(msg) {
  cl.busy = false;
  $("#btnClSave").disabled = false;
  if (!msg.ok) { $("#clSt").textContent = "Salvarea a eșuat"; toast("Clauzele nu au fost salvate: " + (msg.error || "")); return; }
  cl.dirty = false;
  closeClEditor(true);     // mesajul "clauze" (lista noua) vine imediat dupa, cu toast
}

$("#ovCl").addEventListener("click", clClick);
$("#ovCl").addEventListener("input", clInput);
$("#ovCl").addEventListener("change", clInput);
$("#btnClSave").onclick = clSave;

// ── liste ────────────────────────────────────────────────────
function listHtml(f) {
  const k = esc(f.key);
  const title = !f.hide_header && f.label ? labelHtml(f) : "<span></span>";
  if (isDialogList(f)) {
    return `<div class="f dl" data-fw="${k}">
      <div class="list-tools">${title}<button type="button" class="btn btn-acc btn-sm" data-add="${k}">＋ ${esc(addLabel(f))}</button></div>
      <div data-items="${k}"></div>
      <small class="err">Adaugă cel puțin un element</small></div>`;
  }
  return `<div class="f dl" data-fw="${k}">
    ${!f.hide_header && f.label ? `<div class="list-tools">${title}</div>` : ""}
    <div data-items="${k}"></div>
    <div class="add-row"><button type="button" class="btn btn-ghost btn-sm" data-add="${k}">＋ ${esc(addLabel(f))}</button></div>
    <small class="err">Adaugă cel puțin un element</small></div>`;
}

function renderList(key) {
  const f = app.fields[key], items = app.lists[key] || [];
  const box = $(`[data-items="${CSS.escape(key)}"]`);
  if (!f || !box) return;
  if (isDialogList(f)) {
    box.innerHTML = items.length
      ? items.map((it, i) => cardHtml(f, it, i)).join("")
      : `<div class="empty">Nu a fost adăugat niciun element. Apasă „＋ ${esc(addLabel(f))}”.</div>`;
  } else {
    const single = (f.item_fields || []).length === 1;
    box.innerHTML = items.length
      ? items.map((it, i) => single ? compactRowHtml(f, it, i) : inlineRowHtml(f, it, i)).join("")
      : `<div class="empty">Niciun element.</div>`;
  }
}

function summaryHtml(f, it) {
  const keys = Array.isArray(f.summary_fields) && f.summary_fields.length
    ? f.summary_fields : (f.item_fields || []).slice(0, 3).map(x => x.key);
  const parts = keys.map(k => {
    const itf = itemField(f, k);
    return itf ? display(itf, it.values[k]) : (it.extra[k] || "");
  }).filter(x => String(x).trim());
  return parts.length ? parts.map(esc).join("<em>·</em>") : "(necompletat)";
}

function detailsHtml(f, it) {
  return groupRows(f.item_fields || []).map(r =>
    (r[0].group ? `<div class="cap">${esc(r[0].group)}</div>` : "") +
    `<div class="drow" style="grid-template-columns:${cols(r, "width_percent", false)}">${r.map(itf =>
      `<div><small>${esc(itf.label || itf.key)}</small><b>${esc(display(itf, it.values[itf.key])) || "—"}</b></div>`).join("")}</div>`
  ).join("");
}

function cardHtml(f, it, i) {
  const open = (app.open[f.key] || new Set()).has(i);
  return `<div class="mcard${open ? " open" : ""}" data-card="${esc(f.key)}" data-idx="${i}">
    <div class="mhead" data-tog>${CHEV}<span class="nr">${i + 1}.</span>
      <span class="ttl">${summaryHtml(f, it)}</span>
      <button type="button" class="btn btn-ghost btn-sm" data-edit>✎ Editează</button>
      <button type="button" class="btn btn-x" data-del title="Șterge">✕</button>
    </div>
    <div class="mbody"><div><div class="mdet">${detailsHtml(f, it)}</div></div></div>
  </div>`;
}

function inlineRowHtml(f, it, i) {
  return `<div class="irow" data-row="${esc(f.key)}" data-idx="${i}">
    <div class="irow-h"><span>${i + 1}.</span><span class="grow"></span>
      <button type="button" class="btn btn-x" data-delrow title="Șterge">✕</button></div>
    ${itemFormHtml(f, it)}
  </div>`;
}

function compactRowHtml(f, it, i) {
  const itf = f.item_fields[0];
  return `<div class="irow compact" data-row="${esc(f.key)}" data-idx="${i}">
    <div class="grow">${itemInputHtml(itf, it.values[itf.key])}</div>
    <button type="button" class="btn btn-x" data-delrow title="Șterge">✕</button>
  </div>`;
}

function itemCellHtml(itf, v) {
  return `<span class="l">${esc(itf.label || itf.key)}<span class="auto" data-auto="${esc(itf.key)}"></span></span>${itemInputHtml(itf, v)}`;
}

function itemFormHtml(f, it) {
  return groupRows(f.item_fields || []).map(r =>
    (r[0].group ? `<div class="grp-cap">${esc(r[0].group)}</div>` : "") +
    `<div class="row" style="grid-template-columns:${cols(r, "width_percent", false)}">${r.map(itf =>
      `<div class="f" data-iw="${esc(itf.key)}">${itemCellHtml(itf, it.values[itf.key])}</div>`).join("")}</div>`
  ).join("");
}

function itemInputHtml(itf, v) {
  const a = `data-i="${esc(itf.key)}"`;
  switch (itf.type || "text") {
    case "combo":
      return comboHtml(itf, v, a);
    case "date":
      return `<ro-date class="dt" type="date" ${a} value="${esc(v)}"></ro-date>`;
    case "date_range": {
      const r = v || ["", ""];
      return `<div class="per" ${a} data-kind="range"><ro-date class="dt" type="date" data-part="0" value="${esc(r[0])}"></ro-date><span class="sep">–</span><ro-date class="dt" type="date" data-part="1" value="${esc(r[1])}"></ro-date></div>`;
    }
    case "check_text": {
      const c = v || { on: false, text: "" };
      return `<div class="chk" ${a} data-kind="check"><label><input type="checkbox" data-part="on"${c.on ? " checked" : ""}> ${esc(itf.check_label || "Da")}</label><input class="inp" data-part="text" value="${esc(c.text)}"${c.on ? "" : " disabled"}></div>`;
    }
    case "number": {
      const step = itf.decimals > 0 ? Math.pow(10, -itf.decimals) : 1;
      return `<input class="inp" type="number" min="0" max="${itf.max > 0 ? itf.max : 9999}" step="${step}" ${a} value="${esc(v)}" placeholder="0">`;
    }
    case "person_picker":
      return `<button type="button" class="picker${v ? " set" : ""}" ${a} data-kind="pick" title="${v ? "Apasă pentru a alege altă persoană" : "Apasă pentru a alege persoana"}">${pickerInner(v, "Selectează…")}</button>`;
    default:
      return `<input class="inp" ${a} value="${esc(v)}" placeholder="${esc(itf.placeholder || "")}">`;
  }
}

// ═══════════════════════════════════════════════════════════
//  VALORI SI SINCRONIZARE
// ═══════════════════════════════════════════════════════════
function setVal(k, v) {
  if (app.vals[k] === v) return;
  app.vals[k] = v;
  syncTop(k);
  afterChange(k);
}

function afterChange(k) {
  rerenderTimeFields(k);
  updateDeps();
  updateNav();
  const w = $(`[data-fw="${CSS.escape(k)}"]`);
  if (w) w.classList.remove("invalid");
  scheduleChange();
  if (app.def && k === app.def.registratura_date_field) sendRegDate();
  schedulePreview();
}

function syncDuo(wrap, v) {
  if (wrap.dataset.kind === "seg") {
    $$(".opt", wrap).forEach(o => o.classList.toggle("on", o.dataset.v === v));
    return;
  }
  const opts = $$(".opt", wrap);
  const isB = !!opts[1] && opts[1].dataset.v === v;
  if (opts[0]) opts[0].classList.toggle("on", !isB);
  if (opts[1]) opts[1].classList.toggle("on", isB);
  const cb = $("input", wrap);
  if (cb) cb.checked = isB;
}

function syncSwitch(cb, on) {
  cb.checked = on;
  const box = cb.closest(".para") || cb.closest(".swf");
  if (!box) return;
  box.classList.toggle("on", on);
  const l = $(".sw-lbl", box);
  if (l) l.textContent = on ? "Da" : "Nu";
}

function syncTop(k) {
  const v = app.vals[k];
  $$(`[data-f="${CSS.escape(k)}"]`).forEach(el => {
    if (el.dataset.kind === "nuda") syncSwitch(el, isDa(v));
    else if (el.dataset.kind === "duo" || el.dataset.kind === "seg") syncDuo(el, v);
    else {
      let dv = v ?? "";
      if (el.type === "date" && String(dv).length > 10) dv = String(dv).slice(0, 10);   // datetime afisat doar ca data
      if (!el.contains(document.activeElement) && el.value !== dv) el.value = dv;
    }
  });
  const pk = $(`[data-pick="${CSS.escape(k)}"]`);
  if (pk) {
    pk.classList.toggle("set", !!v);
    pk.innerHTML = pickerInner(v, "Selectează persoana…");
    pk.title = v ? "Apasă pentru a alege altă persoană" : "Apasă pentru a alege persoana";
  }
}

// ── depends_on ───────────────────────────────────────────────
function isActive(on, val) {
  if (!on) return true;
  const ks = String(on).split("|"), vs = String(val ?? "").split("|");
  if (ks.length !== vs.length) return false;
  return ks.every((k, i) => String(app.vals[k] ?? "") === vs[i]);
}

function sectionVisible(si) {
  const s = (app.def.sections || [])[si];
  if (!s) return true;
  if (app.nestedSecs.has(si)) return isDa(app.vals[s.depends_on]);
  return isActive(s.depends_on, s.depends_value);
}

function fieldVisible(f) {
  return sectionVisible(app.fieldSec[f.key]) && isActive(f.depends_on, f.depends_value);
}

function updateDeps() {
  Object.values(app.fields).forEach(f => {
    if (!f.depends_on) return;
    const act = isActive(f.depends_on, f.depends_value);
    const w = $(`[data-fw="${CSS.escape(f.key)}"]`);
    if (!w) return;
    w.classList.toggle("off", !act);
    $$("input,select,textarea,button", w).forEach(x => {
      if (x.hasAttribute("readonly")) return;
      x.disabled = !act;
    });
    $$(".duo,.seg", w).forEach(d => d.classList.toggle("dis", !act));
  });
  // randurile in care toate campurile sunt inactive dispar de tot
  $$("#sections .row").forEach(r => {
    const fs = [...r.children].filter(c => c.classList.contains("f"));
    r.classList.toggle("all-off", fs.length > 0 && fs.every(c => c.classList.contains("off")));
  });
  (app.def.sections || []).forEach((s, i) => {
    if (app.nestedSecs.has(i)) return;
    const el = document.getElementById("sec-" + i);
    if (el) el.hidden = !sectionVisible(i);
  });
}

// ── navigare + stare sectiuni ────────────────────────────────
function fieldOk(f) {
  if (!f.required || !fieldVisible(f)) return true;
  if (f.type === "dynamic_list") return (app.lists[f.key] || []).some(it => itemValid(f, it));
  if (f.type === "clauze_editor") return (app.puncte[f.key] || []).some(p => punctResult(p));
  return String(app.vals[f.key] ?? "").trim() !== "";
}

function updateNav() {
  $$(".sec").forEach(s => {
    const chip = $(`#nav [data-go="${s.id}"]`);
    if (!chip) return;
    chip.hidden = s.hidden;
    const keys = $$("[data-fw]", s).map(w => w.dataset.fw);
    const fs = keys.map(k => app.fields[k]).filter(Boolean);
    const anyReq = fs.some(f => f.required && fieldVisible(f));
    const ok = fs.every(fieldOk);
    chip.classList.toggle("done", ok && (anyReq || fs.length > 0));
    chip.classList.toggle("err", !ok && s.classList.contains("tried"));
  });
}

function validate() {
  let first = null;
  $$(".f.invalid, .para.invalid").forEach(x => x.classList.remove("invalid"));
  Object.values(app.fields).forEach(f => {
    if (fieldOk(f)) return;
    const w = $(`[data-fw="${CSS.escape(f.key)}"]`);
    if (w) { w.classList.add("invalid"); if (!first) first = w; }
  });
  $$(".sec").forEach(s => s.classList.add("tried"));
  updateNav();
  if (first) {
    first.scrollIntoView({ behavior: "smooth", block: "center" });
    const i = $("input:not([disabled]),select,textarea,button", first);
    if (i) i.focus({ preventScroll: true });
    toast("Completează câmpurile marcate cu roșu");
    return false;
  }
  return true;
}

// ── on_change (hooks din C#) ─────────────────────────────────
function toRo(f, v) {
  if (!f) return v == null ? "" : String(v);
  if (f.type === "date") return isoToRo(v);
  if (f.type === "datetime") return isoDTToRo(v);
  if (f.type === "number") return String(parseInt(v, 10) || 0);
  return v == null ? "" : String(v);
}
function collectSimple() {
  const o = {};
  Object.keys(app.vals).forEach(k => { o[k] = toRo(app.fields[k], app.vals[k]); });
  if (!app.fields.MentiuniDocument)
    o.MentiuniDocument = app.ment.on ? String(app.ment.text || "").trim() : "";
  return o;
}
let changeTimer = null;
function scheduleChange() {
  if (!app.hasOnChange) return;
  clearTimeout(changeTimer);
  changeTimer = setTimeout(() => send({ type: "change", values: collectSimple() }), 250);
}
function applyChanged(vals) {
  Object.keys(vals).forEach(k => {
    const f = app.fields[k];
    if (f && (f.type === "person_picker" || f.type === "dynamic_list" || f.type === "clauze_editor")) return;
    let v = vals[k];
    if (f && f.type === "date") v = roToIso(v) || app.vals[k];
    else if (f && f.type === "datetime") v = roToIsoDT(v) || app.vals[k];
    else if (f && f.type === "number") v = String(parseInt(v, 10) || 0);
    if (app.vals[k] === v) return;
    const el = $(`[data-f="${CSS.escape(k)}"]`);
    if (el && el.contains(document.activeElement)) return;    // nu suprascriem ce scrie utilizatorul
    app.vals[k] = v;
    syncTop(k);
  });
  updateDeps();
  updateNav();
}

// ═══════════════════════════════════════════════════════════
//  EVENIMENTE
// ═══════════════════════════════════════════════════════════
function itemCtx(el) {
  if (el.closest("#itemBody") && app.edit)
    return { f: app.fields[app.edit.key], item: app.edit.item, root: $("#itemBody"), inline: false };
  const row = el.closest("[data-row]");
  if (!row) return null;
  const key = row.dataset.row;
  const item = (app.lists[key] || [])[+row.dataset.idx];
  return item ? { f: app.fields[key], item, root: row, inline: true } : null;
}

function refreshItemCell(ctx, k) {
  const itf = itemField(ctx.f, k);
  const w = $(`[data-iw="${CSS.escape(k)}"]`, ctx.root);
  if (itf && w) w.innerHTML = itemCellHtml(itf, ctx.item.values[k]);
}

function markAuto(ctx, keys) {
  keys.forEach(k => {
    const a = $(`[data-auto="${CSS.escape(k)}"]`, ctx.root);
    if (a) a.textContent = "• completat automat";
  });
}

function readItemInput(iEl, target, itf) {
  switch (itf.type || "text") {
    case "combo": {
      const kind = comboKind(itf);
      if (kind === "nuda") return iEl.checked ? daOpt(itf) : nuOpt(itf);
      if (kind === "duo") return target.type === "checkbox" ? (target.checked ? itf.options[1] : itf.options[0]) : undefined;
      return iEl.value;
    }
    case "date_range": {
      const ins = $$("ro-date", iEl);
      let a = ins[0].value, b = ins[1].value;
      if (a && (!b || b < a)) { b = a; ins[1].value = a; }
      return [a, b];
    }
    case "check_text": {
      const on = $('[data-part="on"]', iEl).checked;
      const tx = $('[data-part="text"]', iEl);
      tx.disabled = !on;
      if (on && target.dataset.part === "on") tx.focus();
      return { on, text: tx.value };
    }
    case "person_picker":
      return undefined;
    default:
      return iEl.value;
  }
}

function setItemFromUser(ctx, itf, v) {
  ctx.item.values[itf.key] = v;
  const a = $(`[data-auto="${CSS.escape(itf.key)}"]`, ctx.root);
  if (a) a.textContent = "";
  if ((itf.type || "text") === "combo" && itf.on_select) {
    const changed = applyOnSelect(ctx.f, ctx.item, itf);
    changed.forEach(k => refreshItemCell(ctx, k));
    markAuto(ctx, changed);
  }
  if (ctx.inline) afterListChange(ctx.f.key);
}

function onInput(e) {
  const t = e.target;
  if (t.dataset && t.dataset.ment) {
    if (t.dataset.ment === "on") {
      if (app.ment.on === t.checked) return;
      app.ment.on = t.checked;
      syncSwitch(t, t.checked);
      const tx = $('[data-ment="text"]');
      if (!t.checked) { app.ment.text = ""; tx.value = ""; }
      else setTimeout(() => tx.focus(), 30);
    } else {
      if (app.ment.text === t.value) return;
      app.ment.text = t.value;
    }
    schedulePreview();
    return;
  }
  if (onPunctInput(t)) return;
  const iEl = t.closest("[data-i]");
  if (iEl) {
    const ctx = itemCtx(iEl);
    if (!ctx) return;
    const itf = itemField(ctx.f, iEl.dataset.i);
    if (!itf) return;
    const v = readItemInput(iEl, t, itf);
    if (v === undefined) return;
    if (JSON.stringify(ctx.item.values[itf.key]) === JSON.stringify(v)) return;
    if (iEl.dataset.kind === "nuda") syncSwitch(iEl, isDa(v));
    if (iEl.dataset.kind === "duo") syncDuo(iEl, v);
    setItemFromUser(ctx, itf, v);
    return;
  }
  const fEl = t.closest("[data-f]");
  if (!fEl) return;
  const k = fEl.dataset.f, f = app.fields[k];
  if (!f) return;
  let v;
  if (fEl.dataset.kind === "nuda") v = fEl.checked ? daOpt(f) : nuOpt(f);
  else if (fEl.dataset.kind === "duo") {
    if (t.type !== "checkbox") return;
    v = t.checked ? f.options[1] : f.options[0];
  }
  else if (f.type === "datetime" && fEl.type === "date") {
    // doar data: pastram ora existenta (sau 00:00)
    const m = /T(\d{2}:\d{2})/.exec(app.vals[k] || "");
    v = fEl.value ? fEl.value + "T" + (m ? m[1] : "00:00") : "";
  }
  else v = fEl.value;
  setVal(k, v);
}

// secțiune cu "unlockable": true — campurile "readonly" devin editabile la cerere
function toggleUnlock(btn) {
  const si = +btn.dataset.unlock;
  const sec = document.getElementById("sec-" + si);
  const s = (app.def.sections || [])[si];
  if (!sec || !s) return;
  const on = !sec.classList.contains("unlocked");
  sec.classList.toggle("unlocked", on);
  btn.textContent = on ? "🔓 Blochează la loc" : "🔒 Deblochează";
  btn.title = on ? "Datele rămân cum le-ai modificat; câmpurile nu mai pot fi editate"
                 : "Permite modificarea datelor preluate din ERP";
  let first = null;
  (s.fields || []).filter(f => f && f.type === "readonly").forEach(f => {
    const inp = $(`[data-f="${CSS.escape(f.key)}"]`, sec);
    if (!inp) return;
    if (on) { inp.removeAttribute("readonly"); if (!first) first = inp; }
    else inp.setAttribute("readonly", "");
  });
  if (first) first.focus();
}

function onClick(e) {
  const t = e.target;
  const unl = t.closest("[data-unlock]");
  if (unl) { toggleUnlock(unl); return; }
  if (onPunctClick(t)) return;

  const opt = t.closest(".duo .opt, .seg .opt");
  if (opt) {
    const wrap = opt.closest(".duo, .seg");
    if (wrap.classList.contains("dis")) return;
    const v = opt.dataset.v;
    if (wrap.dataset.f) { setVal(wrap.dataset.f, v); return; }
    if (wrap.dataset.i) {
      const ctx = itemCtx(wrap);
      const itf = ctx && itemField(ctx.f, wrap.dataset.i);
      if (!itf || ctx.item.values[itf.key] === v) return;
      syncDuo(wrap, v);
      setItemFromUser(ctx, itf, v);
    }
    return;
  }

  const go = t.closest("[data-go]");
  if (go) { document.getElementById(go.dataset.go).scrollIntoView({ behavior: "smooth", block: "start" }); return; }

  const pick = t.closest("[data-pick]");
  if (pick) { pickTop(pick.dataset.pick); return; }

  const ipick = t.closest('[data-kind="pick"]');
  if (ipick) { pickItem(ipick); return; }

  const add = t.closest("[data-add]");
  if (add) { addItem(add.dataset.add); return; }

  const card = t.closest("[data-card]");
  if (card) {
    const key = card.dataset.card, idx = +card.dataset.idx;
    if (t.closest("[data-edit]")) { openItem(key, idx); return; }
    if (t.closest("[data-del]")) {
      confirmDel(`Ștergi elementul nr. ${idx + 1} din lista „${app.fields[key].label || key}”?`, () => {
        app.lists[key].splice(idx, 1);
        app.open[key] = new Set();
        renderList(key);
        afterListChange(key);
      });
      return;
    }
    if (t.closest("[data-tog]")) {
      card.classList.toggle("open");
      const s = app.open[key] = app.open[key] || new Set();
      if (card.classList.contains("open")) s.add(idx); else s.delete(idx);
    }
    return;
  }

  const delrow = t.closest("[data-delrow]");
  if (delrow) {
    const row = delrow.closest("[data-row]"), key = row.dataset.row, idx = +row.dataset.idx;
    confirmDel(`Ștergi elementul nr. ${idx + 1} din lista „${app.fields[key].label || key}”?`, () => {
      app.lists[key].splice(idx, 1);
      renderList(key);
      afterListChange(key);
    });
    return;
  }

  if (t.closest("[data-close-item]")) { closeItem(); return; }
}

document.addEventListener("input", onInput);
document.addEventListener("change", onInput);
document.addEventListener("click", onClick);
document.addEventListener("keydown", e => {
  if (e.key !== "Escape") return;
  if ($("#ovMsg").classList.contains("show")) { const b = $("#msgBtns .btn"); if (b) b.click(); return; }
  if ($("#ovConf").classList.contains("show")) { closeConf(); return; }
  if ($("#ovCl").classList.contains("show")) { closeClEditor(); return; }
  if ($("#ovItem").classList.contains("show")) closeItem();
});

function afterListChange(key) {
  const w = $(`[data-fw="${CSS.escape(key)}"]`);
  if (w) w.classList.remove("invalid");
  updateNav();
  schedulePreview();
}

// ── adaugare / editare elemente ──────────────────────────────
function addItem(key) {
  const f = app.fields[key];
  if (!f) return;
  if (isDialogList(f)) { openItem(key, -1); return; }
  const it = newItem(f);
  applyAllOnSelect(f, it);
  app.lists[key].push(it);
  renderList(key);
  afterListChange(key);
  const rows = $$(`[data-row="${CSS.escape(key)}"]`);
  const last = rows[rows.length - 1];
  const inp = last && $("input,select", last);
  if (inp) inp.focus();
}

function openItem(key, idx) {
  const f = app.fields[key];
  const item = idx >= 0 ? clone(app.lists[key][idx]) : newItem(f);
  if (idx < 0) applyAllOnSelect(f, item);
  app.edit = { key, idx, item };
  const t = f.dialog_title || f.label || "Element";
  $("#itemTitle").textContent = (idx >= 0 ? "Editare — " : "Adăugare — ") + t + (idx >= 0 ? ` (nr. ${idx + 1})` : "");
  $("#itemBody").innerHTML = itemFormHtml(f, item);
  $("#ovItem").classList.add("show");
  setTimeout(() => { const i = $("#itemBody input:not([disabled]), #itemBody select"); if (i) i.focus(); }, 30);
}

function closeItem() {
  $("#ovItem").classList.remove("show");
  app.edit = null;
}

$("#btnSaveItem").onclick = () => {
  if (!app.edit) return;
  const { key, idx, item } = app.edit;
  if (idx >= 0) app.lists[key][idx] = item; else app.lists[key].push(item);
  closeItem();
  renderList(key);
  afterListChange(key);
  toast(idx >= 0 ? "Elementul a fost actualizat" : "Elementul a fost adăugat");
};

// ── persoane: selectorul web (picker.js); valorile "maps" le calculeaza C# ──
function pickVia(title, maps) {
  return PersonPicker.open({ title: "Selectare — " + title, subtitle: "Alege persoana pentru câmpul „" + title + "”" })
    .then(p => (p ? request({ type: "pickPerson", prsnId: p.id, maps: maps || {} }) : null));
}

// ── ferestre de mesaj (confirmare / succes / eroare) ─────────
// buttons: [[text, "acc" | "ghost", valoare]] -> Promise(valoarea butonului apasat)
let msgResolve = null;
function showMsg(o) {
  const box = $("#msgBox");
  box.className = "modal msg" + (o.kind === "ok" ? " ok" : o.kind === "err" ? " err" : "");
  $("#msgIco").textContent = o.kind === "ok" ? "✓" : o.kind === "err" ? "!" : "?";
  $("#msgTitle").textContent = o.title || "";
  $("#msgText").textContent = o.text || "";
  $("#msgText").hidden = !o.text;
  $("#msgDoc").textContent = o.doc || "";
  $("#msgDoc").hidden = !o.doc;
  const rows = (o.rows || []).filter(r => r[1]);
  $("#msgRows").innerHTML = rows.map(r => `<div><span>${esc(r[0])}</span><b>${esc(r[1])}</b></div>`).join("");
  $("#msgRows").hidden = !rows.length;
  $("#msgBtns").innerHTML = (o.buttons || [["OK", "acc", true]]).map((b, i) =>
    `<button type="button" class="btn btn-${b[1]}" data-mb="${i}">${esc(b[0])}</button>`).join("");
  $("#ovMsg").classList.add("show");
  const btns = o.buttons || [["OK", "acc", true]];
  setTimeout(() => { const last = $$("#msgBtns .btn").pop(); if (last) last.focus(); }, 30);
  return new Promise(res => {
    msgResolve = v => { $("#ovMsg").classList.remove("show"); msgResolve = null; res(v); };
    $$("#msgBtns .btn").forEach(b => { b.onclick = () => msgResolve(btns[+b.dataset.mb][2]); });
  });
}

// 1. C# a pregatit generarea: cerem confirmarea (cod + data de inregistrare)
function confirmGenerate(m) {
  showMsg({
    kind: "confirm",
    title: m.cod ? "Confirmare înregistrare document" : "Confirmare generare document",
    text: m.cod ? "Se va genera și înregistra documentul:" : "Se va genera documentul:",
    doc: m.title,
    rows: [["Cod înregistrare", m.cod], ["Data înregistrare", m.cod ? m.data : ""]],
    buttons: [["Anulează", "ghost", false], ["Da, continuă", "acc", true]]
  }).then(ok => {
    if (ok) { $("#btnGen").textContent = "⤓ Se generează…"; send({ type: "generateConfirmed" }); }
    else { send({ type: "generateCancelled" }); setBusy(false); }
  });
}

// 2. documentul a fost generat (si inregistrat): fereastra de succes, apoi inchidere
function generated(m) {
  showMsg({
    kind: "ok",
    title: m.cod ? "Document înregistrat și generat cu succes!" : "Document generat cu succes!",
    doc: m.title,
    rows: [["Cod înregistrare", m.cod], ["Data înregistrare", m.cod ? m.data : ""]],
    buttons: [["OK", "acc", true]]
  }).then(() => send({ type: "close" }));
}

function pickTop(k) {
  const f = app.fields[k];
  if (!f) return;
  const maps = f.maps || {};
  pickVia(f.label || k, maps).then(r => {
    if (!r || r.cancelled) return;
    app.vals[k] = r.numeComplet || "";
    app.vals[k + "_PrsnId"] = String(r.prsnId ?? "");
    const vals = r.values || {};
    Object.keys(vals).forEach(t => {
      if (t === k) app.vals[k] = vals[t];
      else setVal(t, vals[t]);
    });
    syncTop(k);
    afterChange(k);
  });
}

function pickItem(btn) {
  const ctx = itemCtx(btn);
  if (!ctx) return;
  const itf = itemField(ctx.f, btn.dataset.i);
  if (!itf) return;
  pickVia(itf.label || itf.key, itf.maps || {}).then(r => {
    if (!r || r.cancelled || !document.body.contains(ctx.root)) return;
    ctx.item.values[itf.key] = r.numeComplet || "";
    const vals = r.values || {};
    Object.keys(vals).forEach(t => {
      if (itemField(ctx.f, t)) ctx.item.values[t] = vals[t];
      else ctx.item.extra[t] = vals[t];
    });
    refreshItemCell(ctx, itf.key);
    Object.keys(vals).forEach(t => refreshItemCell(ctx, t));
    if (ctx.inline) afterListChange(ctx.f.key);
  });
}

// ── confirmare Da / Nu ───────────────────────────────────────
let confCb = null;
function confirmDel(msg, cb, title) {
  $("#confMsg").textContent = msg;
  $("#confTitle").textContent = title || "Confirmare ștergere";
  confCb = cb;
  $("#ovConf").classList.add("show");
  setTimeout(() => $("#confNo").focus(), 30);
}
function closeConf() { $("#ovConf").classList.remove("show"); confCb = null; }
$("#confNo").onclick = closeConf;
$("#confYes").onclick = () => { const cb = confCb; closeConf(); if (cb) cb(); };

// ── previzualizare / generare (C# completeaza DOCX-ul si face PDF-ul) ──
// Listele se trimit cu valorile brute; C# le converteste in formatul documentului.
// "required" = campurile simple obligatorii acum (vizibile / active): in
// previzualizare, daca sunt goale, apar ca linie punctata rosiatica.
function requiredKeys() {
  return Object.values(app.fields)
    .filter(f => f.required && f.type !== "dynamic_list" && f.type !== "clauze_editor" && fieldVisible(f))
    .map(f => f.key);
}
function collectLists() {
  const o = {};
  Object.keys(app.lists).forEach(k => {
    o[k] = (app.lists[k] || []).map(it => ({ values: it.values, extra: it.extra }));
  });
  return o;
}

let busy = false;
function setBusy(on, label) {
  busy = on;
  ["#btnPrev", "#btnGen", "#btnCancel"].forEach(s => { $(s).disabled = on; });
  $("#btnPrev").textContent = on && label === "prev" ? "⟳ Se generează…" : "⟳ Previzualizează";
  $("#btnGen").textContent = on && label === "gen" ? "⤓ Se generează…" : "⤓ Generează PDF";
}

// ── previzualizare automata ──────────────────────────────────
// Dupa ~1,2 s fara modificari se cere un PDF nou (C# il face in fundal).
// Daca se modifica ceva cat se genereaza, dupa terminare se mai face unul.
const PREVIEW_DELAY = 1200;
let pvTimer = null, pvDirty = false, pvRunning = false;

function setPvStatus(state, text) {
  const el = $("#pvSt");
  el.className = "pv-st " + state;
  $("span", el).textContent = text;
}

function schedulePreview(delay) {
  if (!app.def) return;
  pvDirty = true;
  clearTimeout(pvTimer);
  pvTimer = setTimeout(autoPreview, delay ?? PREVIEW_DELAY);
  if (!pvRunning) setPvStatus("wait", "Previzualizarea se actualizează…");
}

function autoPreview() {
  if (busy) { pvTimer = setTimeout(autoPreview, PREVIEW_DELAY); return; }   // generare in curs
  if (pvRunning) return;                // se reia din previewDone
  pvRunning = true;
  pvDirty = false;
  setPvStatus("busy", "Se generează previzualizarea…");
  send({ type: "preview", auto: true, values: collectSimple(), lists: collectLists(), clauze: collectClauze(), required: requiredKeys() });
}

function previewDone(msg) {
  pvRunning = false;
  if (busy && $("#btnPrev").textContent.indexOf("Se generează") >= 0) setBusy(false);
  if (msg.ok) setPvStatus("ok", "Previzualizare actualizată");
  else {
    setPvStatus("err", "Previzualizarea a eșuat — încearcă butonul");
    // la previzualizarea ceruta cu butonul aratam si motivul
    if (msg.auto === false) showMsg({ kind: "err", title: "Previzualizarea a eșuat", text: msg.message || "", buttons: [["OK", "acc", true]] });
  }
  if (pvDirty) { clearTimeout(pvTimer); pvTimer = setTimeout(autoPreview, 500); }
}

$("#btnCancel").onclick = () => { if (!busy) send({ type: "cancel" }); };
$("#btnPrev").onclick = () => {
  if (busy || !app.def) return;
  clearTimeout(pvTimer);
  pvRunning = true;
  pvDirty = false;
  setBusy(true, "prev");
  setPvStatus("busy", "Se generează previzualizarea…");
  send({ type: "preview", values: collectSimple(), lists: collectLists(), clauze: collectClauze(), required: requiredKeys() });
};
$("#btnGen").onclick = () => {
  if (busy || !app.def || !validate()) return;
  setBusy(true, "gen");
  send({ type: "generate", values: collectSimple(), lists: collectLists(), clauze: collectClauze() });
};

function toast(t) {
  const el = $("#toast");
  el.textContent = t; el.classList.add("show");
  clearTimeout(el._t); el._t = setTimeout(() => el.classList.remove("show"), 2200);
}

// ── pornire: cerem datele de la C# ───────────────────────────
send({ type: "ready" });
let readyTries = 1;
const readyTimer = setInterval(() => {
  if (app.def) { clearInterval(readyTimer); return; }
  if (!host) { $("#docTitle").textContent = "Pagina nu rulează în Softone (WebView2)"; clearInterval(readyTimer); return; }
  if (readyTries >= 4) { $("#docTitle").textContent = "Nu s-au primit datele de la Softone"; clearInterval(readyTimer); return; }
  readyTries++;
  send({ type: "ready" });
}, 1500);
