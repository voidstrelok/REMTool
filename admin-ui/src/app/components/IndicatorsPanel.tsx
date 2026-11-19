"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { api, Catalog, Run } from "../lib/api";
import FormulaEditor, { parseFormula, problems, split, describe } from "./FormulaEditor";

export type Indicator = { id: number; nombre: string; formula: string; año: number; meta: number; peso: number; mensual: boolean; orden: number; detalle?: string | null; isDenFijo: boolean; isTasa: boolean; formulaDenFijo?: string | null; tipoindicador?: number | null; esPeriodoOctubreSep: boolean; isColaborativo: boolean; convenioIds: number[] };
export type Convenio = { id: number; nombre: string; indicatorIds: number[] };
type Draft = { nombre: string; formula: string; year: number; meta: number; peso: number; mensual: boolean; orden: number; detalle: string; isDenFijo: boolean; isTasa: boolean; formulaDenFijo: string; tipoIndicador: number | null; esPeriodoOctubreSep: boolean; isColaborativo: boolean; convenioIds: number[] };

const nowYear = new Date().getFullYear();
const emptyFormula = JSON.stringify({ type: "op", op: "div", args: [{ type: "op", op: "sum", args: [{ type: "value", prestacion: "", columna: 1 }] }, { type: "variable", name: "FONASA", filters: {} }] });
const empty = (year: number, tipo: number | null, orden: number): Draft => ({ nombre: "", formula: emptyFormula, year, meta: 0, peso: 0, mensual: false, orden, detalle: "", isDenFijo: false, isTasa: false, formulaDenFijo: "", tipoIndicador: tipo, esPeriodoOctubreSep: false, isColaborativo: false, convenioIds: [] });
const toDraft = (x: Indicator): Draft => ({ nombre: x.nombre, formula: x.formula, year: x.año, meta: x.meta, peso: x.peso, mensual: x.mensual, orden: x.orden, detalle: x.detalle ?? "", isDenFijo: x.isDenFijo, isTasa: x.isTasa, formulaDenFijo: x.formulaDenFijo ?? "", tipoIndicador: x.tipoindicador ?? null, esPeriodoOctubreSep: x.esPeriodoOctubreSep, isColaborativo: x.isColaborativo, convenioIds: [...x.convenioIds].sort() });
const pct = (v: number) => `${Math.round(v * 10000) / 100} %`;

const options: [keyof Draft, string, string][] = [
  ["mensual", "Mensual", "Se evalúa mes a mes en lugar de acumulado."],
  ["isTasa", "Tasa", "El resultado se muestra como tasa, no como porcentaje de cumplimiento."],
  ["isDenFijo", "Denominador fijo", "El denominador se calcula una sola vez (corte de población) y no por mes."],
  ["esPeriodoOctubreSep", "Período oct. – sep.", "El denominador usa octubre del año anterior a septiembre del año en curso."],
  ["isColaborativo", "Colaborativo", "El denominador es comunal; los establecimientos sólo aportan numerador."],
];

export default function IndicatorsPanel({ items, convenios, catalog, busy, run }: { items: Indicator[]; convenios: Convenio[]; catalog: Catalog | null; busy: boolean; run: Run }) {
  const years = useMemo(() => [...new Set(items.map(x => x.año))].sort((a, b) => b - a), [items]);
  const [year, setYear] = useState(""); const [tipo, setTipo] = useState(""); const [convenio, setConvenio] = useState(""); const [search, setSearch] = useState("");
  useEffect(() => { if (!year && years[0]) setYear(String(years[0])); }, [years, year]);
  const tipos = catalog?.indicatorTypes ?? [];
  const tipoName = (id?: number | null) => tipos.find(t => t.id === id)?.nombre;

  const [selectedId, setSelectedId] = useState<number | null>(null);
  const selected = items.find(x => x.id === selectedId) ?? null;
  const [draft, setDraft] = useState<Draft>(() => empty(nowYear, null, 1));
  const [baseline, setBaseline] = useState<Draft>(draft);
  const dirty = JSON.stringify(draft) !== JSON.stringify(baseline);
  const form = useRef<HTMLFormElement>(null);

  const term = search.trim().toLowerCase();
  const filtered = items.filter(x => (!year || String(x.año) === year) && (!tipo || String(x.tipoindicador ?? "") === tipo) && (!convenio || x.convenioIds.includes(Number(convenio))) && (!term || `${x.nombre} ${x.detalle ?? ""} #${x.id}`.toLowerCase().includes(term)));
  const pesoTotal = filtered.reduce((s, x) => s + x.peso, 0);

  const root = parseFormula(draft.formula);
  const formulaOk = !!root && problems(root).length === 0;
  const den = root ? split(root).den : null;
  const set = <K extends keyof Draft>(k: K, v: Draft[K]) => setDraft(d => ({ ...d, [k]: v }));

  const load = (next: Draft, id: number | null) => { setSelectedId(id); setDraft(next); setBaseline(next); };
  const discardOk = () => !dirty || confirm("Hay cambios sin guardar. ¿Descartarlos?");
  const choose = (x: Indicator) => { if (discardOk()) load(toDraft(x), x.id); };
  const startNew = () => { if (!discardOk()) return; const y = Number(year) || nowYear; const n = empty(y, tipo ? Number(tipo) : null, Math.max(0, ...items.filter(x => x.año === y).map(x => x.orden)) + 1); load(n, null); };
  const duplicate = () => { if (!selected || !discardOk()) return; setSelectedId(null); setDraft({ ...toDraft(selected), nombre: `${selected.nombre} (copia)` }); setBaseline(empty(nowYear, null, 0)); };

  // Ctrl+S guarda desde cualquier campo.
  useEffect(() => { const h = (e: KeyboardEvent) => { if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s") { e.preventDefault(); form.current?.requestSubmit(); } }; window.addEventListener("keydown", h); return () => window.removeEventListener("keydown", h); }, []);

  const submit = (e: FormEvent) => {
    e.preventDefault(); if (!dirty || !formulaOk) return;
    void run(async () => {
      const r = await api<{ id?: number }>(selected ? `/indicators/${selected.id}` : "/indicators", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) });
      setSelectedId(selected?.id ?? r.id ?? null); setBaseline(draft);
    }, selected ? "Indicador actualizado." : "Indicador creado.");
  };
  const remove = () => {
    if (!selected || !confirm(`¿Eliminar "${selected.nombre}"? También se eliminan sus resultados calculados. Esta acción no se puede deshacer.`)) return;
    void run(async () => { await api(`/indicators/${selected.id}`, { method: "DELETE" }); load(empty(nowYear, null, 1), null); }, "Indicador eliminado.");
  };

  return <section className="rl">
    <header className="rlHead">
      <div><h2>Indicadores</h2><p>Metas sanitarias, IAAP y convenios: fórmula, meta, peso y comportamiento del cálculo.</p></div>
      <button type="button" onClick={startNew}>+ Nuevo indicador</button>
    </header>

    <div className="rlFilters">
      <label>Año<select value={year} onChange={e => setYear(e.target.value)}><option value="">Todos</option>{years.map(y => <option key={y}>{y}</option>)}</select></label>
      <label>Tipo<select value={tipo} onChange={e => setTipo(e.target.value)}><option value="">Todos</option>{tipos.map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}</select></label>
      <label>Convenio<select value={convenio} onChange={e => setConvenio(e.target.value)}><option value="">Todos</option>{convenios.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}</select></label>
      <label className="grow">Buscar<input value={search} placeholder="Nombre, detalle o #id…" onChange={e => setSearch(e.target.value)} /></label>
    </div>

    <div className="rlBody">
      <aside className="rlList" aria-label="Listado de indicadores">
        <p className="rlCount">{filtered.length} de {items.length} indicadores{tipo && year ? <> · peso total <b className={Math.abs(pesoTotal - 1) < 0.001 ? "okText" : "warnText"}>{pct(pesoTotal)}</b></> : null}</p>
        {filtered.length === 0 && <p className="rlEmpty">Ningún indicador coincide con los filtros.</p>}
        {filtered.map(x => <button type="button" key={x.id} className={`rlItem${x.id === selected?.id ? " selected" : ""}`} onClick={() => choose(x)}>
          <span className="rlItemTop"><b>{x.orden}.</b>{tipoName(x.tipoindicador) && <em>{tipoName(x.tipoindicador)}</em>}<i>{x.año} · #{x.id}</i></span>
          <span className="rlItemName">{x.nombre}</span>
          <span className="rlItemMsg">Meta {pct(x.meta)} · Peso {pct(x.peso)}{x.convenioIds.length > 0 && ` · ${x.convenioIds.length} convenio(s)`}</span>
        </button>)}
      </aside>

      <form ref={form} className="rlEditor" onSubmit={submit}>
        <div className="rlEditorHead">
          <h3>{selected ? `Indicador #${selected.id}` : "Nuevo indicador"}</h3>
          {dirty && <span className="badge">Cambios sin guardar</span>}
        </div>

        <fieldset className="block"><legend>General</legend>
          <label>Nombre<input required value={draft.nombre} onChange={e => set("nombre", e.target.value)} placeholder="META N°1: …" /></label>
          <div className="rlRow three">
            <label>Año<input type="number" required min={2000} max={2100} value={draft.year} onChange={e => set("year", Number(e.target.value))} /></label>
            <label>Tipo<select value={draft.tipoIndicador ?? ""} onChange={e => set("tipoIndicador", e.target.value ? Number(e.target.value) : null)}><option value="">Sin tipo</option>{tipos.map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}</select></label>
            <label>Orden<input type="number" value={draft.orden} onChange={e => set("orden", Number(e.target.value))} /></label>
          </div>
          <label>Detalle <small>(texto explicativo que aparece en el informe de seguimiento)</small><textarea rows={3} value={draft.detalle} onChange={e => set("detalle", e.target.value)} /></label>
        </fieldset>

        <fieldset className="block"><legend>Fórmula</legend>
          <FormulaEditor value={draft.formula} onChange={v => set("formula", v)} />
        </fieldset>

        <fieldset className="block"><legend>Meta y cálculo</legend>
          <div className="rlRow">
            <label>Meta <small>(fracción: 0,4 = 40 %)</small><span className="withHint"><input type="number" step="any" value={draft.meta} onChange={e => set("meta", Number(e.target.value))} /><em>{pct(draft.meta)}</em></span></label>
            <label>Peso <small>(fracción del agregado)</small><span className="withHint"><input type="number" step="any" min={0} max={1} value={draft.peso} onChange={e => set("peso", Number(e.target.value))} /><em>{pct(draft.peso)}</em></span></label>
          </div>
          <div className="optGrid">{options.map(([k, label, help]) => <label key={k} className={`opt${draft[k] ? " on" : ""}`}><input type="checkbox" checked={Boolean(draft[k])} onChange={e => set(k, e.target.checked as never)} /><span><b>{label}</b><small>{help}</small></span></label>)}</div>
          {draft.isDenFijo && <label>Descripción del denominador fijo
            <span className="withHint"><input value={draft.formulaDenFijo} placeholder="FONASA{edad=6}" onChange={e => set("formulaDenFijo", e.target.value)} />{den && <button type="button" className="ghost sm" onClick={() => set("formulaDenFijo", describe(den))}>Usar denominador</button>}</span></label>}
        </fieldset>

        <fieldset className="block"><legend>Convenios</legend>
          {convenios.length === 0 ? <p className="hint">No hay convenios. Créalos en la pestaña Convenios.</p>
            : <div className="chips">{convenios.map(c => { const on = draft.convenioIds.includes(c.id); return <button type="button" key={c.id} className={`chip${on ? " on" : ""}`} aria-pressed={on} onClick={() => set("convenioIds", on ? draft.convenioIds.filter(i => i !== c.id) : [...draft.convenioIds, c.id].sort())}>{on ? "✓ " : "+ "}{c.nombre}</button>; })}</div>}
        </fieldset>

        <div className="rlActions sticky">
          <button disabled={busy || !dirty || !formulaOk} title="Ctrl+S">{selected ? "Guardar cambios" : "Crear indicador"}</button>
          {selected && <button type="button" className="ghost" disabled={busy} onClick={duplicate}>Duplicar</button>}
          {dirty && <button type="button" className="ghost" onClick={() => setDraft(baseline)}>Descartar</button>}
          {!formulaOk && <small className="err">Corrige la fórmula para guardar.</small>}
          {selected && <button type="button" className="danger push" disabled={busy} onClick={remove}>Eliminar</button>}
        </div>
      </form>
    </div>
  </section>;
}
