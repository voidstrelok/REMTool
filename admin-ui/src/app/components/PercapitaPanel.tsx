"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, Run } from "../lib/api";

type YearSummary = { year: number; rows: number; establishments: number; inscritos: number };
type EstSummary = { nombre: string; codDeis: string | null; f: number; m: number };
type Row = { codDeis: string; edad: number; sexo: string; inscritos: number };
const columns = ["cod_deis", "edad", "sexo", "inscritos"];

// CSV de Excel (";" o ","), con encabezado cod_deis;edad;sexo;inscritos en cualquier orden.
function parse(text: string): Row[] {
  const lines = text.replace(/^﻿/, "").split(/\r?\n/).filter(l => l.trim());
  if (lines.length < 2) throw new Error("El archivo no contiene filas.");
  const sep = lines[0].includes(";") ? ";" : ",";
  const header = lines[0].split(sep).map(h => h.trim().toLowerCase());
  const idx = columns.map(c => header.indexOf(c));
  if (idx.some(i => i < 0)) throw new Error(`El encabezado debe incluir: ${columns.join(sep)}`);
  return lines.slice(1).map((l, n) => {
    const v = l.split(sep).map(x => x.trim()); const edad = Number(v[idx[1]]); const inscritos = Number(v[idx[3]]);
    if (!Number.isInteger(edad) || !Number.isInteger(inscritos)) throw new Error(`Fila ${n + 2}: edad e inscritos deben ser enteros.`);
    return { codDeis: v[idx[0]], edad, sexo: v[idx[2]], inscritos };
  });
}

export default function PercapitaPanel({ busy, run }: { busy: boolean; run: Run }) {
  const [years, setYears] = useState<YearSummary[]>([]); const [year, setYear] = useState(String(new Date().getFullYear()));
  const [file, setFile] = useState<File | null>(null); const [detail, setDetail] = useState<EstSummary[]>([]);
  const load = () => api<YearSummary[]>("/percapita").then(setYears).catch(() => setYears([]));
  useEffect(() => { void load(); }, []);
  useEffect(() => { void api<EstSummary[]>(`/percapita/${year}`).then(setDetail).catch(() => setDetail([])); }, [year, years]);
  const exists = years.some(y => String(y.year) === year);

  const submit = (e: FormEvent) => {
    e.preventDefault(); if (!file) return;
    if (exists && !confirm(`Ya hay población cargada para ${year}. ¿Reemplazarla completa?`)) return;
    void run(async () => {
      const rows = parse(await file.text());
      await api(`/percapita/${year}`, { method: "PUT", headers: { "content-type": "application/json" }, body: JSON.stringify(rows) });
      setFile(null); (e.target as HTMLFormElement).reset(); await load();
    }, `Población ${year} cargada.`);
  };
  const remove = () => { if (confirm(`¿Eliminar toda la población del año ${year}?`)) void run(async () => { await api(`/percapita/${year}`, { method: "DELETE" }); await load(); }, `Población ${year} eliminada.`); };

  return <section className="query">
    <h2>Población per cápita SSC</h2>
    <p>Sube un CSV con encabezado <code>cod_deis;edad;sexo;inscritos</code> (sexo F o M). La carga reemplaza toda la población del año seleccionado.</p>
    <form className="filters" onSubmit={submit}>
      <label>Año de corte<input type="number" min="2000" max="2100" required value={year} onChange={e => setYear(e.target.value)} /></label>
      <label>Archivo CSV<input type="file" accept=".csv,text/csv" required onChange={e => setFile(e.target.files?.[0] ?? null)} /></label>
      <button disabled={busy || !file}>{exists ? "Reemplazar población" : "Cargar población"}</button>
      {exists && <button type="button" className="ghost" disabled={busy} onClick={remove}>Eliminar año</button>}
    </form>
    <div className="reportTable">{years.map(y => <article key={y.year}><div><strong>{y.year}</strong><small>{y.establishments} establecimientos · {y.rows} filas</small></div><div className="actions"><span>{y.inscritos.toLocaleString("es-CL")} inscritos</span><button type="button" className="ghost" onClick={() => setYear(String(y.year))}>Ver</button></div></article>)}</div>
    {detail.length > 0 && <><h3>Detalle {year}</h3><div className="reportTable">{detail.map(d => <article key={`${d.codDeis}-${d.nombre}`}><div><strong>{d.nombre}</strong><small>{d.codDeis ?? "sin código DEIS"}</small></div><div className="actions"><span>F {d.f.toLocaleString("es-CL")}</span><span>M {d.m.toLocaleString("es-CL")}</span><span>Total {(d.f + d.m).toLocaleString("es-CL")}</span></div></article>)}</div></>}
  </section>;
}
