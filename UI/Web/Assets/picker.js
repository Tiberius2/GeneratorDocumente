// ═══════════════════════════════════════════════════════════
//  Selectorul de angajati (comun: selector.html si form.html)
//  PersonPicker.setPersons(list)  — [{id, nume, functie, cor, cnp, nrCim, dataCim, dep}]
//  PersonPicker.open({ title, subtitle, selectedId }) -> Promise<persoana | null>
//  Cautare dupa nume, functie, CNP, departament (fara diacritice);
//  sageti sus/jos, Enter = alege, Esc = renunta, dublu-clic = alege.
// ═══════════════════════════════════════════════════════════
(function () {
  "use strict";
  let persons = [], filtered = [], sel = -1, resolver = null, root = null;
  const MAX_ROWS = 400;     // randuri afisate deodata (cautarea restrange lista)

  const esc = s => String(s ?? "").replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
  const fold = s => String(s ?? "").normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

  function build() {
    root = document.createElement("div");
    root.className = "ov pp-ov";
    root.innerHTML = `<div class="modal pp">
      <div class="m-h"><div><div class="pp-t"></div><div class="pp-s"></div></div><button type="button" data-pp="x" title="Închide">✕</button></div>
      <div class="pp-search"><input class="inp" type="text" placeholder="Caută după nume, funcție, CNP sau departament…" autocomplete="off" spellcheck="false"><span class="pp-n"></span></div>
      <div class="pp-list"><table><thead><tr><th>Nume complet</th><th>Funcție</th><th>Departament</th><th>CNP</th><th>Nr. CIM</th><th>Data CIM</th></tr></thead><tbody></tbody></table></div>
      <div class="m-f"><span class="pp-hint">↑ ↓ pentru navigare · Enter pentru alegere · dublu-clic</span>
        <button class="btn btn-ghost" type="button" data-pp="x">Anulează</button>
        <button class="btn btn-acc" type="button" data-pp="ok" disabled>Selectează →</button></div>
    </div>`;
    document.body.appendChild(root);
    const inp = root.querySelector(".pp-search input");
    inp.addEventListener("input", () => filter(inp.value));
    root.addEventListener("click", e => {
      const b = e.target.closest("[data-pp]");
      if (b) { if (b.dataset.pp === "ok") choose(); else close(null); return; }
      const tr = e.target.closest("tr[data-i]");
      if (tr) select(+tr.dataset.i);
      else if (e.target === root) close(null);
    });
    root.addEventListener("dblclick", e => {
      const tr = e.target.closest("tr[data-i]");
      if (tr) { select(+tr.dataset.i); choose(); }
    });
    // tastele se trateaza aici (captura), ca sa nu ajunga la pagina de dedesubt
    document.addEventListener("keydown", e => {
      if (!root.classList.contains("show")) return;
      if (e.key === "Escape") { close(null); }
      else if (e.key === "ArrowDown") { select(Math.min(sel + 1, shown() - 1)); }
      else if (e.key === "ArrowUp") { select(Math.max(sel - 1, 0)); }
      else if (e.key === "Enter") { choose(); }
      else return;
      e.preventDefault();
      e.stopImmediatePropagation();
    }, true);
  }

  const shown = () => Math.min(filtered.length, MAX_ROWS);

  function filter(q) {
    const words = fold(q).split(/\s+/).filter(Boolean);
    filtered = !words.length ? persons.slice() : persons.filter(p => {
      const hay = fold([p.nume, p.functie, p.cnp, p.dep, p.cor].join(" "));
      return words.every(w => hay.indexOf(w) >= 0);
    });
    render();
    select(filtered.length ? 0 : -1);
  }

  function render() {
    const rows = filtered.slice(0, MAX_ROWS).map((p, i) =>
      `<tr data-i="${i}"><td><b>${esc(p.nume)}</b></td><td>${esc(p.functie)}</td><td>${esc(p.dep)}</td><td>${esc(p.cnp)}</td><td>${esc(p.nrCim)}</td><td>${esc(p.dataCim)}</td></tr>`).join("");
    root.querySelector("tbody").innerHTML = rows ||
      `<tr class="pp-empty"><td colspan="6">Niciun angajat nu se potrivește căutării.</td></tr>`;
    const n = filtered.length;
    root.querySelector(".pp-n").textContent = n > MAX_ROWS ? `${MAX_ROWS} din ${n} — restrânge căutarea` : `${n} angajați`;
  }

  function select(i) {
    sel = i;
    root.querySelectorAll("tr.on").forEach(t => t.classList.remove("on"));
    const tr = i >= 0 ? root.querySelector(`tr[data-i="${i}"]`) : null;
    if (tr) { tr.classList.add("on"); tr.scrollIntoView({ block: "nearest" }); }
    root.querySelector('[data-pp="ok"]').disabled = !tr;
  }

  function choose() {
    if (sel < 0 || sel >= filtered.length) return;
    close(filtered[sel]);
  }

  function close(p) {
    root.classList.remove("show");
    const r = resolver;
    resolver = null;
    if (r) r(p);
  }

  window.PersonPicker = {
    setPersons(list) { persons = Array.isArray(list) ? list : []; },
    find(id) { return persons.find(p => p.id === id) || null; },
    open(opts) {
      opts = opts || {};
      if (!root) build();
      if (resolver) close(null);
      root.querySelector(".pp-t").textContent = opts.title || "Selectare angajat";
      root.querySelector(".pp-s").textContent = opts.subtitle || "Caută și alege persoana din listă";
      const inp = root.querySelector(".pp-search input");
      inp.value = "";
      filter("");
      if (opts.selectedId) {
        const i = filtered.findIndex(p => p.id === opts.selectedId);
        if (i >= 0 && i < MAX_ROWS) select(i);
      }
      root.classList.add("show");
      setTimeout(() => inp.focus(), 30);
      return new Promise(res => { resolver = res; });
    }
  };
})();
