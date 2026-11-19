"use client";

import { DragEvent, FormEvent, useMemo, useState } from "react";
import {
  AlertCircle, CheckCircle2, Download, FileSpreadsheet, GitCompareArrows, HeartPulse, IdCard, ListChecks,
  Loader2, Search, ShieldCheck, Upload, X, XCircle,
} from "lucide-react";
import "./ComprobadorRut.css";
import Breadcrumbs from "../../components/Breadcrumbs";
import { Button } from "@/components/ui/button";
import InternoGate, { internoFetch } from "../InternoGate";

type Fonasa = { run: string; dv: string | null; corte: number; establecimiento: string; tramo: string | null };
type Item = { entrada: string; rut: string; valido: boolean; dvEsperado: string; fonasa: Fonasa | null; enCruce: boolean | null };
type Result = { conCruce: boolean; totalCruce: number; items: Item[] };
type Filtro = "todos" | "invalidos" | "sinFonasa" | "fueraCruce";
type Lista = { texto: string; archivo: File | null };

const MAX_FILAS = 2000;
const vacia: Lista = { texto: "", archivo: null };
const fmt = (n: number) => n.toLocaleString("es-CL");
const pct = (n: number, total: number) => (total ? Math.round((n / total) * 100) : 0);
// Mismo criterio que la API: separa por línea, ; o , y descarta lo que no tenga dígitos.
const contar = (texto: string) => texto.split(/[\n\r;,]+/).filter(x => /\d/.test(x)).length;
const esExcel = (f: File) => /\.(xlsx|xlsm)$/i.test(f.name);

function ListaCard({ titulo, ayuda, icon: Icon, value, onChange }: {
  titulo: string; ayuda: string; icon: typeof ListChecks; value: Lista; onChange: (v: Lista) => void;
}) {
  const [over, setOver] = useState(false);
  const n = contar(value.texto);
  const setArchivo = (f: File | undefined) => f && onChange({ ...value, archivo: f });
  const drop = (e: DragEvent) => { e.preventDefault(); setOver(false); setArchivo([...e.dataTransfer.files].find(esExcel)); };

  return (
    <section className="rut-card">
      <div className="rut-card-head">
        <span className="rut-icon"><Icon size={17} aria-hidden="true" /></span>
        <div><h2>{titulo}</h2><p>{ayuda}</p></div>
        <span className={`rut-count${n || value.archivo ? " has" : ""}`}>
          {value.archivo ? (n ? `${fmt(n)} + Excel` : "Excel") : `${fmt(n)} RUT`}
        </span>
      </div>
      <textarea
        className="rut-textarea"
        aria-label={`${titulo}: RUT pegados`}
        placeholder={"Pegue un RUT por línea\n12.345.678-5\n9876543-3\n…"}
        value={value.texto}
        onChange={e => onChange({ ...value, texto: e.target.value })}
      />
      {value.archivo ? (
        <div className="rut-file">
          <FileSpreadsheet size={18} style={{ color: "var(--success)" }} aria-hidden="true" />
          <div><div className="font-semibold">{value.archivo.name}</div><small>{fmt(Math.ceil(value.archivo.size / 1024))} KB</small></div>
          <button type="button" aria-label="Quitar archivo" onClick={() => onChange({ ...value, archivo: null })}><X size={16} /></button>
        </div>
      ) : (
        <label className={`rut-drop${over ? " over" : ""}`} onDragOver={e => { e.preventDefault(); setOver(true); }} onDragLeave={() => setOver(false)} onDrop={drop}>
          <Upload size={16} aria-hidden="true" />
          <span>Arrastre un Excel o <strong>selecciónelo</strong> (.xlsx)</span>
          <input type="file" accept=".xlsx,.xlsm" className="sr-only" onChange={e => setArchivo(e.target.files?.[0])} />
        </label>
      )}
    </section>
  );
}

function Stat({ id, label, value, total, tone, icon: Icon, foot, active, onSelect }: {
  id: Filtro; label: string; value: number; total: number; tone?: "error" | "warning"; icon: typeof ListChecks;
  foot: string; active: boolean; onSelect: (f: Filtro) => void;
}) {
  return (
    <button type="button" className={`rut-stat ${tone ?? ""}${active ? " active" : ""}`} aria-pressed={active} onClick={() => onSelect(active ? "todos" : id)}>
      <span className="rut-stat-label"><Icon size={14} aria-hidden="true" />{label}</span>
      <span className="rut-stat-value">{fmt(value)}{id !== "todos" && <small>{pct(value, total)}%</small>}</span>
      <span className="rut-stat-bar"><span style={{ width: `${id === "todos" ? 100 : pct(value, total)}%` }} /></span>
      <span className="rut-stat-foot">{foot}</span>
    </button>
  );
}

function Pill({ tone, children }: { tone: "ok" | "bad" | "warn"; children: React.ReactNode }) {
  const Icon = tone === "ok" ? CheckCircle2 : XCircle;
  return <span className={`rut-pill ${tone}`}><Icon size={12} aria-hidden="true" />{children}</span>;
}

function Comprobador() {
  const [lista, setLista] = useState<Lista>(vacia);
  const [cruce, setCruce] = useState<Lista>(vacia);
  const [cruzar, setCruzar] = useState(false);
  const [result, setResult] = useState<Result | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [filtro, setFiltro] = useState<Filtro>("todos");
  const [busqueda, setBusqueda] = useState("");

  const listo = contar(lista.texto) > 0 || !!lista.archivo;

  const submit = async (e: FormEvent) => {
    e.preventDefault(); setBusy(true); setError(""); setFiltro("todos"); setBusqueda("");
    const form = new FormData();
    form.append("lista", lista.texto);
    if (lista.archivo) form.append("archivo", lista.archivo);
    if (cruzar) { form.append("cruce", cruce.texto); if (cruce.archivo) form.append("archivoCruce", cruce.archivo); }
    try { setResult(await internoFetch<Result>("interno/rut", { method: "POST", body: form })); }
    catch (err) { setError(err instanceof Error ? err.message : String(err)); }
    finally { setBusy(false); }
  };

  const resumen = useMemo(() => {
    const items = result?.items ?? [];
    return {
      total: items.length,
      invalidos: items.filter(i => !i.valido).length,
      sinFonasa: items.filter(i => !i.fonasa).length,
      fueraCruce: items.filter(i => i.enCruce === false).length,
    };
  }, [result]);

  const visibles = useMemo(() => {
    const q = busqueda.trim().toLowerCase();
    const qDigits = q.replace(/[^0-9k]/g, "");
    return (result?.items ?? []).filter(i =>
      (filtro === "invalidos" ? !i.valido : filtro === "sinFonasa" ? !i.fonasa : filtro === "fueraCruce" ? i.enCruce === false : true)
      && (!q || i.entrada.toLowerCase().includes(q) || (qDigits && i.rut.toLowerCase().replace(/[^0-9k]/g, "").includes(qDigits))
        || i.fonasa?.establecimiento.toLowerCase().includes(q)));
  }, [result, filtro, busqueda]);

  const descargar = () => {
    if (!result) return;
    const cell = (v: unknown) => `"${String(v ?? "").replace(/"/g, '""')}"`;
    const header = ["Entrada", "RUT", "DV válido", "DV esperado", "Fonasa", "Corte Fonasa", "Establecimiento", "Tramo", ...(result.conCruce ? ["En lista cruce"] : [])];
    const rows = result.items.map(i => [i.entrada, i.rut, i.valido ? "SI" : "NO", i.dvEsperado.trim(), i.fonasa ? "SI" : "NO", i.fonasa?.corte, i.fonasa?.establecimiento, i.fonasa?.tramo, ...(result.conCruce ? [i.enCruce ? "SI" : "NO"] : [])]);
    const csv = "﻿" + [header, ...rows].map(r => r.map(cell).join(";")).join("\r\n");
    const a = Object.assign(document.createElement("a"), { href: URL.createObjectURL(new Blob([csv], { type: "text/csv" })), download: "comprobacion_rut.csv" });
    a.click(); URL.revokeObjectURL(a.href);
  };

  return (
    <div className="flex flex-col gap-5">
      <form onSubmit={submit} className="flex flex-col gap-4">
        <div className={`grid gap-4 ${cruzar ? "lg:grid-cols-2" : ""}`}>
          <ListaCard titulo="Lista a comprobar" icon={ListChecks} value={lista} onChange={setLista}
            ayuda="Con o sin puntos y guion. En Excel se usa la columna RUT/RUN (y DV si está al lado)." />
          {cruzar && <ListaCard titulo="Lista de cruce" icon={GitCompareArrows} value={cruce} onChange={setCruce}
            ayuda="Se marca si cada RUT aparece aquí. Se compara sin DV, puede venir sin él." />}
        </div>
        <div className="rut-toolbar">
          <label className="rut-switch">
            <input type="checkbox" checked={cruzar} onChange={e => setCruzar(e.target.checked)} />
            <span className="rut-switch-track" aria-hidden="true" />
            <span>Cruzar con otra lista<small>Indica si cada RUT está en una segunda lista</small></span>
          </label>
          <div className="ml-auto flex items-center gap-2">
            {(listo || result) && <Button type="button" variant="outline" disabled={busy}
              onClick={() => { setLista(vacia); setCruce(vacia); setResult(null); setError(""); }}>Limpiar</Button>}
            <Button type="submit" size="lg" disabled={busy || !listo}>
              {busy ? <Loader2 className="animate-spin" aria-hidden="true" /> : <ShieldCheck aria-hidden="true" />}
              {busy ? "Comprobando…" : "Comprobar"}
            </Button>
          </div>
        </div>
      </form>

      {error && <div role="alert" className="rut-error"><AlertCircle size={18} aria-hidden="true" />{error}</div>}

      {!result && !error && (
        <div className="rut-empty">
          <IdCard size={28} aria-hidden="true" />
          <p>Pegue o suba una lista de RUT y presione <strong>Comprobar</strong>. Para cada RUT se revisa:</p>
          <ul>
            <li><ShieldCheck size={14} aria-hidden="true" />Dígito verificador</li>
            <li><HeartPulse size={14} aria-hidden="true" />Inscripción Fonasa (último corte)</li>
            <li><GitCompareArrows size={14} aria-hidden="true" />Presencia en lista de cruce (opcional)</li>
          </ul>
        </div>
      )}

      {result && (
        <section className="flex flex-col gap-4" aria-label="Resultados">
          <div className="rut-stats">
            <Stat id="todos" label="Revisados" icon={ListChecks} value={resumen.total} total={resumen.total} active={filtro === "todos"} onSelect={setFiltro}
              foot={`${fmt(resumen.total - resumen.invalidos)} válidos · ${fmt(resumen.total - resumen.sinFonasa)} en Fonasa`} />
            <Stat id="invalidos" label="DV inválido" icon={XCircle} tone="error" value={resumen.invalidos} total={resumen.total} active={filtro === "invalidos"} onSelect={setFiltro}
              foot="Dígito verificador o formato incorrecto" />
            <Stat id="sinFonasa" label="Sin Fonasa" icon={HeartPulse} tone="warning" value={resumen.sinFonasa} total={resumen.total} active={filtro === "sinFonasa"} onSelect={setFiltro}
              foot="No aparecen en ningún corte cargado" />
            {result.conCruce && <Stat id="fueraCruce" label="Fuera del cruce" icon={GitCompareArrows} tone="warning" value={resumen.fueraCruce} total={resumen.total} active={filtro === "fueraCruce"} onSelect={setFiltro}
              foot={`Lista de cruce: ${fmt(result.totalCruce)} RUT`} />}
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="rut-search">
              <Search size={15} aria-hidden="true" />
              <input type="search" placeholder="Buscar RUT o establecimiento…" aria-label="Buscar en resultados" value={busqueda} onChange={e => setBusqueda(e.target.value)} />
            </div>
            <span className="text-sm" style={{ color: "var(--text-light)" }}>{fmt(visibles.length)} de {fmt(resumen.total)}</span>
            <Button type="button" variant="outline" className="ml-auto" onClick={descargar}><Download aria-hidden="true" />Descargar CSV</Button>
          </div>

          <div className="rut-table-wrap">
            <table className="rut-table">
              <thead><tr><th>#</th><th>RUT</th><th>Dígito verificador</th><th>Fonasa</th>{result.conCruce && <th>Cruce</th>}</tr></thead>
              <tbody>
                {/* ponytail: sin virtualizar; se muestran 2.000 filas y el CSV trae todo. */}
                {visibles.slice(0, MAX_FILAS).map((i, n) => (
                  <tr key={n} className={i.valido ? "" : "bad"}>
                    <td className="rut-num">{n + 1}</td>
                    <td>
                      <span className="rut-mono">{i.rut || "—"}</span>
                      {i.entrada !== i.rut && <span className="rut-sub">Ingresado: {i.entrada}</span>}
                    </td>
                    <td>{i.valido ? <Pill tone="ok">Válido</Pill>
                      : i.rut ? <><Pill tone="bad">Inválido</Pill><span className="rut-sub">Debería ser <strong>{i.dvEsperado}</strong></span></>
                        : <Pill tone="bad">Formato inválido</Pill>}</td>
                    <td>{i.fonasa ? <>
                      <Pill tone="ok">Inscrito</Pill>
                      <span className="rut-sub">{i.fonasa.establecimiento}</span>
                      <span className="rut-chip">Corte {i.fonasa.corte}</span>
                      {i.fonasa.tramo && <span className="rut-chip">Tramo {i.fonasa.tramo}</span>}
                      {i.fonasa.dv && !i.valido && <span className="rut-chip">DV registrado {i.fonasa.dv}</span>}
                    </> : <Pill tone="warn">No inscrito</Pill>}</td>
                    {result.conCruce && <td>{i.enCruce ? <Pill tone="ok">Está</Pill> : <Pill tone="warn">No está</Pill>}</td>}
                  </tr>
                ))}
                {visibles.length === 0 && <tr><td colSpan={5} className="py-8 text-center" style={{ color: "var(--text-light)" }}>Sin resultados para este filtro.</td></tr>}
              </tbody>
            </table>
            {visibles.length > MAX_FILAS && <p className="p-3 text-sm" style={{ color: "var(--text-light)" }}>Mostrando {fmt(MAX_FILAS)} de {fmt(visibles.length)}. Descargue el CSV para ver todo.</p>}
          </div>
        </section>
      )}
    </div>
  );
}

export default function ComprobadorRutPage() {
  return (
    <div className="mx-auto max-w-6xl py-4">
      <Breadcrumbs items={[{ label: "Panel interno", href: "/Interno" }, { label: "Comprobador de RUT" }]} />
      <InternoGate>
        <div className="page-intro flex items-start gap-3">
          <div className="rounded-lg p-2" style={{ background: "var(--accent-light)" }}><IdCard size={21} style={{ color: "var(--primary)" }} aria-hidden="true" /></div>
          <div><h1 className="text-3xl font-bold">Comprobador de RUT</h1><p>Valide dígitos verificadores, verifique inscripción en Fonasa y cruce contra otra lista.</p></div>
        </div>
        <Comprobador />
      </InternoGate>
    </div>
  );
}
