"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { api, Catalog, Rule, Run } from "../lib/api";

type Draft = { versionId: number; ruleTypeId: number; expresion: string; mensaje: string };
type Check = { state: "idle" | "checking" | "ok" | "error"; message: string };

const emptyDraft: Draft = { versionId: 0, ruleTypeId: 0, expresion: "", mensaje: "" };
const referencePattern = /(?<![A-Za-z0-9_])(?:[A-Za-z]+\d*)\[[A-Z]{1,3}\d+\]/gi;
const extractReferences = (text: string) => [...new Set(text.match(referencePattern) ?? [])].sort();
const toDraft = (rule: Rule): Draft => ({ versionId: rule.id_version, ruleTypeId: rule.idTipoRegla, expresion: rule.expresion, mensaje: rule.mensaje });

export default function RulesPanel({ items, catalog, busy, run }: { items: Rule[]; catalog: Catalog | null; busy: boolean; run: Run }) {
  const [selected, setSelected] = useState<Rule | null>(null);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [serie, setSerie] = useState(""); const [version, setVersion] = useState(""); const [tipo, setTipo] = useState(""); const [search, setSearch] = useState("");
  const [check, setCheck] = useState<Check>({ state: "idle", message: "" });

  const baseline = selected ? toDraft(selected) : emptyDraft;
  const dirty = JSON.stringify(draft) !== JSON.stringify(baseline);
  const references = useMemo(() => extractReferences(draft.expresion), [draft.expresion]);

  const versionsOfSerie = (catalog?.versions ?? []).filter(v => !serie || v.serie === serie);
  const filtered = items.filter(x =>
    (!serie || x.serie === serie) && (!version || String(x.id_version) === version) && (!tipo || String(x.idTipoRegla) === tipo) &&
    (!search || `${x.expresion} ${x.mensaje} ${x.id}`.toLowerCase().includes(search.toLowerCase())));
  const filtersActive = Boolean(serie || version || tipo || search);

  // Validación de sintaxis en vivo, con retardo para no consultar en cada tecla.
  useEffect(() => {
    if (!draft.expresion.trim()) { setCheck({ state: "idle", message: "" }); return; }
    setCheck({ state: "checking", message: "Validando…" });
    const timer = setTimeout(() => {
      api<{ isValid: boolean; message: string }>("/rules/validate", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ expression: draft.expresion }) })
        .then(r => setCheck({ state: r.isValid ? "ok" : "error", message: r.message }))
        .catch(e => setCheck({ state: "error", message: e instanceof Error ? e.message : "No se pudo validar la expresión." }));
    }, 450);
    return () => clearTimeout(timer);
  }, [draft.expresion]);

  const discardOk = () => !dirty || confirm("Hay cambios sin guardar. ¿Descartarlos?");
  const choose = (rule: Rule) => { if (!discardOk()) return; setSelected(rule); setDraft(toDraft(rule)); };
  const startNew = (from?: Rule) => {
    if (!discardOk()) return;
    setSelected(null);
    setDraft(from ? toDraft(from) : { ...emptyDraft, versionId: Number(version) || 0 });
  };
  const submit = (event: FormEvent) => {
    event.preventDefault();
    void run(async () => {
      await api(selected ? `/rules/${selected.id}` : "/rules", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) });
      setSelected(null); setDraft(emptyDraft);
    }, selected ? "Regla actualizada." : "Regla creada.");
  };
  const remove = () => {
    if (!selected || !confirm(`¿Eliminar la regla #${selected.id}? Esta acción no se puede deshacer.`)) return;
    void run(async () => { await api(`/rules/${selected.id}`, { method: "DELETE" }); setSelected(null); setDraft(emptyDraft); }, "Regla eliminada.");
  };

  return <section className="rl">
    <header className="rlHead">
      <div><h2>Reglas de validación</h2><p>Expresiones que revisan la coherencia de los datos REM de cada versión.</p></div>
      <button type="button" onClick={() => startNew()}>+ Nueva regla</button>
    </header>

    <div className="rlFilters">
      <label>Serie<select value={serie} onChange={e => { setSerie(e.target.value); setVersion(""); }}><option value="">Todas</option>{(catalog?.series ?? []).map(s => <option key={s.id} value={s.nombre}>{s.nombre}</option>)}</select></label>
      <label>Versión<select value={version} onChange={e => setVersion(e.target.value)}><option value="">Todas</option>{versionsOfSerie.map(v => <option key={v.id} value={v.id}>{v.serie} · {v.nombre}</option>)}</select></label>
      <label>Tipo de regla<select value={tipo} onChange={e => setTipo(e.target.value)}><option value="">Todos</option>{(catalog?.ruleTypes ?? []).map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}</select></label>
      <label className="grow">Buscar<input value={search} placeholder="Texto de la expresión o del mensaje…" onChange={e => setSearch(e.target.value)} /></label>
      {filtersActive && <button type="button" className="ghost" onClick={() => { setSerie(""); setVersion(""); setTipo(""); setSearch(""); }}>Limpiar filtros</button>}
    </div>

    <div className="rlBody">
      <aside className="rlList" aria-label="Listado de reglas">
        <p className="rlCount">{filtered.length} de {items.length} reglas</p>
        {filtered.length === 0 && <p className="rlEmpty">{items.length === 0 ? "Aún no hay reglas registradas." : "Ninguna regla coincide con los filtros."}</p>}
        {filtered.map(rule => <button type="button" key={rule.id} className={`rlItem${rule.id === selected?.id ? " selected" : ""}`} onClick={() => choose(rule)}>
          <span className="rlItemTop"><b>#{rule.id}</b><em>{rule.tipo}</em><i>{rule.serie} · {rule.version}</i></span>
          <code>{rule.expresion}</code>
          <span className="rlItemMsg">{rule.mensaje}</span>
        </button>)}
      </aside>

      <form className="rlEditor" onSubmit={submit}>
        <div className="rlEditorHead">
          <h3>{selected ? `Editar regla #${selected.id}` : "Nueva regla"}</h3>
          {dirty && <span className="badge">Cambios sin guardar</span>}
        </div>
        <div className="rlRow">
          <label>Versión<select required value={draft.versionId || ""} onChange={e => setDraft({ ...draft, versionId: Number(e.target.value) })}><option value="" disabled>Selecciona una versión…</option>{(catalog?.versions ?? []).map(v => <option key={v.id} value={v.id}>{v.serie} · {v.nombre} ({String(v.fecha).slice(0, 10)})</option>)}</select></label>
          <label>Tipo de regla<select required value={draft.ruleTypeId || ""} onChange={e => setDraft({ ...draft, ruleTypeId: Number(e.target.value) })}><option value="" disabled>Selecciona un tipo…</option>{(catalog?.ruleTypes ?? []).map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}</select></label>
        </div>
        <label>Expresión
          <textarea className="mono" spellCheck={false} required rows={5} value={draft.expresion} placeholder="Ejemplo: A01[C5] + A01[D5] = A01[E5]" onChange={e => setDraft({ ...draft, expresion: e.target.value })} />
        </label>
        <div className={`rlCheck ${check.state}`} aria-live="polite">{check.state === "idle" ? "Escribe una expresión para validar su sintaxis." : check.message}</div>
        {references.length > 0 && <div className="rlRefs"><span>Celdas referenciadas ({references.length})</span><div>{references.map(r => <code key={r}>{r}</code>)}</div></div>}
        <label>Mensaje de error<textarea required rows={3} value={draft.mensaje} placeholder="Texto que verá el usuario cuando la regla no se cumpla." onChange={e => setDraft({ ...draft, mensaje: e.target.value })} /></label>
        <div className="rlActions">
          <button disabled={busy || !dirty || check.state === "error"}>{selected ? "Guardar cambios" : "Crear regla"}</button>
          {selected && <button type="button" className="ghost" disabled={busy} onClick={() => startNew(selected)}>Duplicar</button>}
          {dirty && <button type="button" className="ghost" onClick={() => setDraft(baseline)}>Descartar cambios</button>}
          {selected && <button type="button" className="danger push" disabled={busy} onClick={remove}>Eliminar</button>}
        </div>
      </form>
    </div>
  </section>;
}
