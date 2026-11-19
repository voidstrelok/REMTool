"use client";

import { useEffect, useState } from "react";
import { api, Catalog, Run } from "../lib/api";

type Row = { mes: number; numerador: number; denominador: number; numeradorP: number; denominadorP: number; esManual: boolean };
type Draft = { numerador: string; denominador: string };
const yearKey = "año";
const months = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];

export default function ManualResultsPanel({ catalog, busy, run }: { catalog: Catalog | null; busy: boolean; run: Run }) {
  const indicators = catalog?.indicators ?? [];
  const years = [...new Set(indicators.map(x => String(x[yearKey] ?? "")).filter(Boolean))].sort().reverse();
  const [year, setYear] = useState(""); const [indicatorId, setIndicatorId] = useState(""); const [establishmentId, setEstablishmentId] = useState("");
  const [rows, setRows] = useState<Row[]>([]); const [drafts, setDrafts] = useState<Record<number, Draft>>({});
  useEffect(() => { if (!year && years[0]) setYear(years[0]); }, [years, year]);

  const load = async () => {
    if (!indicatorId || !establishmentId) { setRows([]); setDrafts({}); return; }
    const data = await api<Row[]>(`/indicator-results?indicatorId=${indicatorId}&establishmentId=${establishmentId}`);
    setRows(data);
    setDrafts(Object.fromEntries(months.map((_, i) => { const r = data.filter(x => x.mes === i + 1); return [i + 1, r.length ? { numerador: String(r.reduce((s, x) => s + x.numerador, 0)), denominador: String(r.reduce((s, x) => s + x.denominador, 0)) } : { numerador: "", denominador: "" }]; })));
  };
  useEffect(() => { void load().catch(() => setRows([])); }, [indicatorId, establishmentId]);

  const save = (mes: number) => void run(async () => {
    const d = drafts[mes]; const numerador = Number(d.numerador); const denominador = Number(d.denominador);
    if (d.numerador === "" || d.denominador === "" || Number.isNaN(numerador) || Number.isNaN(denominador)) throw new Error("Ingresa numerador y denominador numéricos.");
    await api("/indicator-results", { method: "PUT", headers: { "content-type": "application/json" }, body: JSON.stringify({ indicatorId: Number(indicatorId), establishmentId: Number(establishmentId), mes, numerador, denominador }) });
    await load();
  }, `${months[mes - 1]} guardado como carga manual.`);
  const unmark = (mes: number) => void run(async () => {
    await api(`/indicator-results?indicatorId=${indicatorId}&establishmentId=${establishmentId}&mes=${mes}`, { method: "DELETE" });
    await load();
  }, `${months[mes - 1]} volverá a calcularse automáticamente.`);

  const visible = indicators.filter(x => !year || String(x[yearKey]) === year);
  return <section className="query">
    <h2>Carga manual de indicadores</h2>
    <p>Los valores guardados aquí quedan marcados como manuales y el cálculo automático no los sobrescribe. Usa «Quitar manual» para que el próximo cálculo los reemplace.</p>
    <div className="filters">
      <label>Año<select value={year} onChange={e => { setYear(e.target.value); setIndicatorId(""); }}>{years.map(y => <option key={y}>{y}</option>)}</select></label>
      <label>Indicador<select value={indicatorId} onChange={e => setIndicatorId(e.target.value)}><option value="">Selecciona...</option>{visible.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label>
      <label>Establecimiento<select value={establishmentId} onChange={e => setEstablishmentId(e.target.value)}><option value="">Selecciona...</option>{(catalog?.establishments ?? []).map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label>
    </div>
    {indicatorId && establishmentId && <div className="reportTable">{months.map((name, i) => {
      const mes = i + 1; const manual = rows.some(x => x.mes === mes && x.esManual); const d = drafts[mes] ?? { numerador: "", denominador: "" };
      const set = (patch: Partial<Draft>) => setDrafts({ ...drafts, [mes]: { ...d, ...patch } });
      return <article key={mes}>
        <div><strong>{name}</strong><small className={manual ? "warnText" : ""}>{manual ? "Carga manual" : rows.some(x => x.mes === mes) ? "Calculado" : "Sin datos"}</small></div>
        <div className="actions">
          <input type="number" step="any" min="0" aria-label={`Numerador ${name}`} placeholder="Numerador" value={d.numerador} onChange={e => set({ numerador: e.target.value })} />
          <input type="number" step="any" min="0" aria-label={`Denominador ${name}`} placeholder="Denominador" value={d.denominador} onChange={e => set({ denominador: e.target.value })} />
          <button type="button" disabled={busy} onClick={() => save(mes)}>Guardar</button>
          {manual && <button type="button" className="ghost" disabled={busy} onClick={() => unmark(mes)}>Quitar manual</button>}
        </div>
      </article>;
    })}</div>}
  </section>;
}
