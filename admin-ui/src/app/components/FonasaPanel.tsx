"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, Run } from "../lib/api";

type YearSummary = { year: number; fechaCorte: string; personas: number; establecimientos: number };
type EstSummary = { nombre: string; codDeis: string | null; f: number; m: number; tramos: string | null };
const fmt = (n: number) => n.toLocaleString("es-CL");

export default function FonasaPanel({ busy, run }: { busy: boolean; run: Run }) {
  const [years, setYears] = useState<YearSummary[]>([]); const [year, setYear] = useState(String(new Date().getFullYear()));
  const [fechaCorte, setFechaCorte] = useState(`${new Date().getFullYear()}-09-30`);
  const [file, setFile] = useState<File | null>(null); const [detail, setDetail] = useState<EstSummary[]>([]);
  const load = () => api<YearSummary[]>("/fonasa").then(setYears).catch(() => setYears([]));
  useEffect(() => { void load(); }, []);
  useEffect(() => { void api<EstSummary[]>(`/fonasa/${year}`).then(setDetail).catch(() => setDetail([])); }, [year, years]);
  const exists = years.some(y => String(y.year) === year);
  const pickYear = (y: string) => { setYear(y); setFechaCorte(`${y}-${fechaCorte.slice(5)}`); };

  const submit = (e: FormEvent) => {
    e.preventDefault(); if (!file) return;
    if (exists && !confirm(`Ya hay inscritos Fonasa cargados para ${year}. ¿Reemplazarlos completos?`)) return;
    const form = e.target as HTMLFormElement;
    void run(async () => {
      const body = new FormData(); body.append("file", file); body.append("fechaCorte", fechaCorte);
      const r = await api<{ rows: number }>(`/fonasa/${year}`, { method: "PUT", body });
      setFile(null); form.reset(); await load();
      alert(`${fmt(r.rows)} inscritos cargados para ${year}.`);
    }, `Inscritos Fonasa ${year} cargados.`);
  };
  const remove = () => { if (confirm(`¿Eliminar todos los inscritos Fonasa del año ${year}?`)) void run(async () => { await api(`/fonasa/${year}`, { method: "DELETE" }); await load(); }, `Inscritos Fonasa ${year} eliminados.`); };

  return <section className="query">
    <h2>Inscritos Fonasa</h2>
    <p>Sube un CSV (separado por <code>;</code> o <code>,</code>) con encabezado <code>run;dv;nombres;apellido_paterno;apellido_materno;fecha_nacimiento;genero;tramo;cod_deis</code>. La columna <code>edad</code> es opcional: si no viene, se calcula a la fecha de corte. La carga reemplaza todos los inscritos del año.</p>
    <form className="filters" onSubmit={submit}>
      <label>Año<input type="number" min="2000" max="2100" required value={year} onChange={e => pickYear(e.target.value)} /></label>
      <label>Fecha de corte<input type="date" required value={fechaCorte} onChange={e => setFechaCorte(e.target.value)} /></label>
      <label>Archivo CSV<input type="file" accept=".csv,.txt,text/csv" required onChange={e => setFile(e.target.files?.[0] ?? null)} /></label>
      <button disabled={busy || !file}>{exists ? "Reemplazar inscritos" : "Cargar inscritos"}</button>
      {exists && <button type="button" className="ghost" disabled={busy} onClick={remove}>Eliminar año</button>}
    </form>
    <div className="reportTable">{years.map(y => <article key={y.year}><div><strong>{y.year}</strong><small>Corte {y.fechaCorte} · {y.establecimientos} establecimientos</small></div><div className="actions"><span>{fmt(y.personas)} inscritos</span><button type="button" className="ghost" onClick={() => pickYear(String(y.year))}>Ver</button></div></article>)}</div>
    {detail.length > 0 && <><h3>Detalle {year}</h3><div className="reportTable">{detail.map(d => <article key={`${d.codDeis}-${d.nombre}`}><div><strong>{d.nombre}</strong><small>{d.codDeis ?? "sin código DEIS"} · tramos {d.tramos ?? "-"}</small></div><div className="actions"><span>F {fmt(d.f)}</span><span>M {fmt(d.m)}</span><span>Total {fmt(d.f + d.m)}</span></div></article>)}</div></>}
  </section>;
}
