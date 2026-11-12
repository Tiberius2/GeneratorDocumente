// ═══════════════════════════════════════════════════════════
//  Selectorul de documente (SelectorDialog)
//  init: { categories: [{ name, theme, docs: [{ id, title }] }], persons, prsnId }
//  -> C#: openDocument { doc, prsnId } | openFolder { prsnId } | close
//  <- C#: documentClosed { generated }
// ═══════════════════════════════════════════════════════════
"use strict";

const $ = (s, r) => (r || document).querySelector(s);
const $$ = (s, r) => [...(r || document).querySelectorAll(s)];
const esc = s => String(s ?? "").replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
const fold = s => String(s ?? "").normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

const host = window.chrome && window.chrome.webview ? window.chrome.webview : null;
const send = m => { if (host) host.postMessage(m); else console.log("[fara host]", m); };

const st = { cats: [], person: null, doc: null, busy: false, loaded: false };
const DOC_ICON = `<svg class="ic" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"><path d="M6 3h8l4 4v14H6z"/><path d="M14 3v4h4M9 12h6M9 15.5h6M9 19h4"/></svg>`;

if (host) host.addEventListener("message", e => {
  const m = e.data;
  if (m.type === "init") init(m);
  else if (m.type === "documentClosed") documentClosed(m);
});

function init(m) {
  if (st.loaded) return;
  st.loaded = true;
  st.cats = m.categories || [];
  PersonPicker.setPersons(m.persons || []);
  st.person = m.prsnId ? PersonPicker.find(m.prsnId) : null;
  renderCols();
  renderPerson();
  updateFoot();
}

// ── angajatul ────────────────────────────────────────────────
function initials(name) {
  const p = String(name || "").trim().split(/\s+/).filter(Boolean);
  if (!p.length) return "?";
  return (p.length === 1 ? p[0].slice(0, 2) : p[0][0] + p[1][0]).toUpperCase();
}

function renderPerson() {
  const b = $("#empBtn"), p = st.person;
  b.classList.toggle("none", !p);
  b.innerHTML = p
    ? `<span class="av">${esc(initials(p.nume))}</span><span class="tx"><small>ANGAJAT SELECTAT</small><b>${esc(p.nume)}${p.functie ? " — " + esc(p.functie) : ""}</b></span><span class="ed">✎</span>`
    : `<span class="dot"></span><span class="tx"><small>OBLIGATORIU</small><b>Selectează angajatul</b></span><span class="ed">▾</span>`;
  $("#dosarBtn").hidden = !p;
}

function pickPerson() {
  return PersonPicker.open({
    title: "Selectare angajat",
    subtitle: "Angajatul pentru care generezi documentul",
    selectedId: st.person ? st.person.id : 0
  }).then(p => {
    if (p) { st.person = p; renderPerson(); updateFoot(); }
    return p;
  });
}

// ── documentele ──────────────────────────────────────────────
function renderCols() {
  if (!st.cats.length) {
    $("#cols").innerHTML = `<div class="empty">Nu a fost găsit niciun document în folderul de șabloane.</div>`;
    return;
  }
  $("#cols").innerHTML = st.cats.map(c => {
    const t = c.theme || {};
    const style = `--accent:${t.accent};--accent-dark:${t.accentDark};--accent-pal:${t.accentPal};--accent-border:${t.accentBorder}`;
    const cards = (c.docs || []).map(d =>
      `<button type="button" class="card" data-doc="${d.id}" data-q="${esc(fold(d.title + " " + c.name))}">${DOC_ICON}<span>${esc(d.title)}</span></button>`).join("");
    return `<section class="cat" style="${style}">
      <div class="cat-h"><b>${esc(c.name)}</b><span>${(c.docs || []).length}</span></div>
      <div class="cat-b">${cards}<div class="none" hidden>Niciun document găsit</div></div>
    </section>`;
  }).join("");
}

function docTitle(id) {
  for (const c of st.cats) for (const d of c.docs || []) if (d.id === id) return d.title;
  return "";
}

function selectDoc(id) {
  st.doc = id;
  $$(".card").forEach(c => c.classList.toggle("on", +c.dataset.doc === id));
  updateFoot();
}

function filterDocs(q) {
  const words = fold(q).split(/\s+/).filter(Boolean);
  $$(".cat").forEach(cat => {
    let n = 0;
    $$(".card", cat).forEach(c => {
      const ok = words.every(w => c.dataset.q.indexOf(w) >= 0);
      c.hidden = !ok;
      if (ok) n++;
    });
    $(".none", cat).hidden = n > 0;
  });
}

// ── continuare ───────────────────────────────────────────────
function updateFoot() {
  const p = st.person, d = st.doc;
  $("#btnGo").disabled = st.busy || d == null;
  $("#selSt").innerHTML = st.busy ? "Documentul este deschis…"
    : d == null ? (p ? "Alege documentul" : "Alege angajatul și documentul")
    : `<b>${esc(docTitle(d))}</b>${p ? " pentru <b>" + esc(p.nume) + "</b>" : " — alege angajatul"}`;
}

function go() {
  if (st.busy || st.doc == null) return;
  if (!st.person) {
    pickPerson().then(p => { if (p) go(); });
    return;
  }
  st.busy = true;
  updateFoot();
  send({ type: "openDocument", doc: st.doc, prsnId: st.person.id });
}

function documentClosed(m) {
  st.busy = false;
  selectDoc(null);
  if (m.generated) toast("Documentul a fost generat și înregistrat");
}

function toast(t) {
  const el = $("#toast");
  el.textContent = t; el.classList.add("show");
  clearTimeout(el._t); el._t = setTimeout(() => el.classList.remove("show"), 2600);
}

// ── evenimente ───────────────────────────────────────────────
$("#empBtn").onclick = () => { if (!st.busy) pickPerson(); };
$("#dosarBtn").onclick = () => { if (st.person) send({ type: "openFolder", prsnId: st.person.id }); };
$("#btnClose").onclick = () => { if (!st.busy) send({ type: "close" }); };
$("#btnGo").onclick = go;
$("#docSearch").addEventListener("input", e => filterDocs(e.target.value));
$("#cols").addEventListener("click", e => {
  const c = e.target.closest(".card");
  if (c && !st.busy) selectDoc(+c.dataset.doc);
});
$("#cols").addEventListener("dblclick", e => {
  const c = e.target.closest(".card");
  if (c && !st.busy) { selectDoc(+c.dataset.doc); go(); }
});
document.addEventListener("keydown", e => {
  if (e.key === "Enter" && !e.target.closest("input") && st.doc != null) { e.preventDefault(); go(); }
});

// ── pornire: cerem datele de la C# ───────────────────────────
send({ type: "ready" });
