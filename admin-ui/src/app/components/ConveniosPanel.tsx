"use client";

import { FormEvent, useMemo, useState } from "react";
import { api, Catalog, Run } from "../lib/api";
import type { Convenio, Indicator } from "./IndicatorsPanel";

type Draft = { nombre: string; indicatorIds: number[] };
const toDraft = (c: Convenio | null): Draft => ({ nombre: c?.nombre ?? "", indicatorIds: [...(c?.indicatorIds ?? [])].sort((a, b) => a - b) });

export default function ConveniosPanel({ items, indicators, catalog, busy, run }: { items: Convenio[]; indicators: Indicator[]; catalog: Catalog | null; busy: boolean; run: Run }) {
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const selected = items.find(x => x.id === selectedId) ?? null;
  const [draft, setDraft] = useState<Draft>(toDraft(null)); const [baseline, setBaseline] = useState<Draft>(draft);
  const dirty = JSON.stringify(draft) !== JSON.stringify(baseline);

  const years = useMemo(() => [...new Set(indicators.map(x => x.año))].sort((a, b) => b - a), [indicators]);
  const [year, setYear] = useState(""); const [tipo, setTipo] = useState(""); const [search, setSearch] = useState(""); const [onlySelected, setOnlySelected] = useState(false);
  const tipos = catalog?.indicatorTypes ?? [];
  const term = search.trim().toLowerCase();
  const visible = indicators.filter(x => (!onlySelected || draft.indicatorIds.includes(x.id)) && (!year || String(x.año) === year) && (!tipo || String(x.tipoindicador ?? "") === tipo) && (!term || `${x.nombre} #${x.id}`.toLowerCase().includes(term)));
  const byId = new Map(indicators.map(x => [x.id, x]));

  const setIds = (ids: number[]) => setDraft(d => ({ ...d, indicatorIds: [...new Set(ids)].sort((a, b) => a - b) }));
  const toggle = (id: number) => setIds(draft.indicatorIds.includes(id) ? draft.indicatorIds.filter(i => i !== id) : [...draft.indicatorIds, id]);
  const load = (c: Convenio | null) => { if (dirty && !confirm("Hay cambios sin guardar. ¿Descartarlos?")) return; const d = toDraft(c); setSelectedId(c?.id ?? null); setDraft(d); setBaseline(d); };

  const submit = (e: FormEvent) => {
    e.preventDefault();
    void run(async () => {
      const r = await api<{ id?: number }>(selected ? `/convenios/${selected.id}` : "/convenios", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) });
      setSelectedId(selected?.id ?? r.id ?? null); setBaseline(draft);
    }, "Convenio guardado.");
  };
  const remove = () => { if (selected && confirm(`¿Eliminar el convenio "${selected.nombre}"? Los indicadores no se eliminan.`)) void run(async () => { await api(`/convenios/${selected.id}`, { method: "DELETE" }); load(null); }, "Convenio eliminado."); };

  return <section className="rl">
    <header className="rlHead">
      <div><h2>Convenios</h2><p>Agrupa indicadores bajo un convenio. También puedes asignarlos desde el editor de cada indicador.</p></div>
      <button type="button" onClick={() => load(null)}>+ Nuevo convenio</button>
    </header>

    <div className="rlBody">
      <aside className="rlList" aria-label="Listado de convenios">
        <p className="rlCount">{items.length} convenios</p>
        {items.length === 0 && <p className="rlEmpty">Aún no hay convenios.</p>}
        {items.map(c => { const ys = [...new Set(c.indicatorIds.map(i => byId.get(i)?.año).filter(Boolean))].sort().join(", "); return <button type="button" key={c.id} className={`rlItem${c.id === selectedId ? " selected" : ""}`} onClick={() => load(c)}>
          <span className="rlItemName">{c.nombre}</span>
          <span className="rlItemMsg">{c.indicatorIds.length} indicadores{ys && ` · ${ys}`}</span>
        </button>; })}
      </aside>

      <form className="rlEditor" onSubmit={submit}>
        <div className="rlEditorHead"><h3>{selected ? `Convenio #${selected.id}` : "Nuevo convenio"}</h3>{dirty && <span className="badge">Cambios sin guardar</span>}</div>
        <label>Nombre<input required value={draft.nombre} onChange={e => setDraft({ ...draft, nombre: e.target.value })} /></label>

        <fieldset className="block"><legend>Indicadores asignados ({draft.indicatorIds.length})</legend>
          {draft.indicatorIds.length === 0 ? <p className="hint">Ninguno. Selecciónalos abajo.</p>
            : <div className="chips">{draft.indicatorIds.map(id => { const x = byId.get(id); return <button type="button" key={id} className="chip on" title="Quitar" onClick={() => toggle(id)}>{x ? `${x.año} · ${x.nombre}` : `#${id}`} ×</button>; })}</div>}
        </fieldset>

        <fieldset className="block"><legend>Agregar indicadores</legend>
          <div className="rlFilters flat">
            <label>Año<select value={year} onChange={e => setYear(e.target.value)}><option value="">Todos</option>{years.map(y => <option key={y}>{y}</option>)}</select></label>
            <label>Tipo<select value={tipo} onChange={e => setTipo(e.target.value)}><option value="">Todos</option>{tipos.map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}</select></label>
            <label className="grow">Buscar<input value={search} placeholder="Nombre o #id…" onChange={e => setSearch(e.target.value)} /></label>
            <label className="check"><input type="checkbox" checked={onlySelected} onChange={e => setOnlySelected(e.target.checked)} />Sólo asignados</label>
          </div>
          <div className="pickBar"><span>{visible.length} visibles</span>
            <button type="button" className="ghost sm" disabled={!visible.length} onClick={() => setIds([...draft.indicatorIds, ...visible.map(x => x.id)])}>Marcar visibles</button>
            <button type="button" className="ghost sm" disabled={!visible.length} onClick={() => setIds(draft.indicatorIds.filter(i => !visible.some(v => v.id === i)))}>Desmarcar visibles</button></div>
          <div className="pickList">{visible.map(x => <label className="check" key={x.id}><input type="checkbox" checked={draft.indicatorIds.includes(x.id)} onChange={() => toggle(x.id)} /><span><b>{x.año}</b> · {x.nombre}</span></label>)}</div>
        </fieldset>

        <div className="rlActions sticky">
          <button disabled={busy || !dirty}>{selected ? "Guardar cambios" : "Crear convenio"}</button>
          {dirty && <button type="button" className="ghost" onClick={() => setDraft(baseline)}>Descartar</button>}
          {selected && <button type="button" className="danger push" disabled={busy} onClick={remove}>Eliminar</button>}
        </div>
      </form>
    </div>
  </section>;
}
