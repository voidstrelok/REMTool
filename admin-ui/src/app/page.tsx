"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { api, Catalog, Rule } from "./lib/api";
import RulesPanel from "./components/RulesPanel";
import IndicatorsPanel, { Indicator, Convenio } from "./components/IndicatorsPanel";
import ConveniosPanel from "./components/ConveniosPanel";
import ManualResultsPanel from "./components/ManualResultsPanel";
import PercapitaPanel from "./components/PercapitaPanel";
import FonasaPanel from "./components/FonasaPanel";
import UsuariosInternosPanel from "./components/UsuariosInternosPanel";

type SummaryPoint = { id: number; nombre: string; categoria: string; expresion: string; idSerieRem: number; serie: string };
type InformeRem = { id: number; nombre: string; idSerieRem: number; serie: string; seccionIds: number[] };
type SeccionRem = { id: number; hoja: string; nombre: string; codigo: string; version: string };
type Filter = { id: number; id_establecimiento: number; id_indicador?: number | null; tipo: number; establishment: string; indicator?: string | null };
type Report = { id: number; establecimiento: string; comuna: string; records: number; [key: string]: unknown };
type Informativo = { id: number; tipo: number; titulo: string; contenido: string; url?: string | null; textoEnlace?: string | null; fechaPublicacion: string; vigente: boolean; destacado: boolean };

const yearKey = "a\u00f1o";
const nowYear = new Date().getFullYear();
const emptyPoint = { nombre: "", categoria: "", expresion: "", seriesId: 0 };
const emptyFilter = { establishmentId: 0, indicatorId: null as number | null, tipo: 0 };

function Year({ item }: { item: { [key: string]: unknown } }) { return <>{String(item[yearKey] ?? "-")}</>; }
function Button({ children, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement>) { return <button {...props}>{children}</button>; }

export default function Home() {
  const [area, setArea] = useState("indicators");
  const [catalog, setCatalog] = useState<Catalog | null>(null);
  const [indicators, setIndicators] = useState<Indicator[]>([]);
  const [rules, setRules] = useState<Rule[]>([]);
  const [points, setPoints] = useState<SummaryPoint[]>([]);
  const [convenios, setConvenios] = useState<Convenio[]>([]);
  const [filters, setFilters] = useState<Filter[]>([]);
  const [reports, setReports] = useState<Report[]>([]);
  const [informativos, setInformativos] = useState<Informativo[]>([]);
  const [message, setMessage] = useState("Sistema listo.");
  const [busy, setBusy] = useState(false);

  const refresh = async () => {
    try {
      const [nextCatalog, nextIndicators, nextRules, nextPoints, nextConvenios, nextFilters, nextInformativos] = await Promise.all([
        api<Catalog>("/catalog"), api<Indicator[]>("/indicators"), api<Rule[]>("/rules"), api<SummaryPoint[]>("/summary-points"), api<Convenio[]>("/convenios"), api<Filter[]>("/filters"), api<Informativo[]>("/informativos")
      ]);
      setCatalog(nextCatalog); setIndicators(nextIndicators); setRules(nextRules); setPoints(nextPoints); setConvenios(nextConvenios); setFilters(nextFilters); setInformativos(nextInformativos);
    } catch (error) { setMessage(error instanceof Error ? error.message : "No se pudo cargar la configuración."); }
  };
  useEffect(() => { void refresh(); }, []);
  const run = async (action: () => Promise<void>, success: string) => {
    setBusy(true); setMessage("Procesando solicitud...");
    try { await action(); setMessage(success); await refresh(); }
    catch (error) { setMessage(error instanceof Error ? error.message : "No se pudo completar la operación."); }
    finally { setBusy(false); }
  };
  const nav = [["operations", "Operaciones"], ["indicators", "Indicadores"], ["manualResults", "Carga manual"], ["percapita", "Per cápita SSC"], ["fonasa", "Inscritos Fonasa"], ["rules", "Reglas"], ["points", "Puntos de resumen"], ["convenios", "Convenios"], ["remReports", "Informes REM"], ["filters", "Filtros por establecimiento"], ["information", "Informativos"], ["reports", "Registros"], ["consolidations", "Consolidados"], ["usuariosInternos", "Usuarios internos"]];
  return <main>
    <header className="appHeader"><div className="brand"><span className="brandMark">R</span><div><p className="eyebrow">ADMINISTRACIÓN LOCAL · 127.0.0.1</p><h1>REM Tool</h1><p>Configuración y operaciones resguardadas en este equipo.</p></div></div><p className="status" aria-live="polite"><span />{message}</p></header>
    <nav aria-label="Áreas de administración">{nav.map(([key, label]) => <Button key={key} className={area === key ? "active" : ""} onClick={() => setArea(key)}>{label}</Button>)}</nav>
    {area === "indicators" && <IndicatorsPanel items={indicators} convenios={convenios} catalog={catalog} busy={busy} run={run} />}
    {area === "manualResults" && <ManualResultsPanel catalog={catalog} busy={busy} run={run} />}
    {area === "fonasa" && <FonasaPanel busy={busy} run={run} />}
    {area === "usuariosInternos" && <UsuariosInternosPanel busy={busy} run={run} />}
    {area === "percapita" && <PercapitaPanel busy={busy} run={run} />}
    {area === "rules" && <RulesPanel items={rules} catalog={catalog} busy={busy} run={run} />}
    {area === "points" && <PointsPanel items={points} catalog={catalog} busy={busy} run={run} />}
    {area === "convenios" && <ConveniosPanel items={convenios} indicators={indicators} catalog={catalog} busy={busy} run={run} />}
    {area === "remReports" && <InformesRemPanel catalog={catalog} busy={busy} run={run} />}
    {area === "filters" && <FiltersPanel items={filters} catalog={catalog} busy={busy} run={run} />}
    {area === "information" && <InformationPanel items={informativos} busy={busy} run={run} />}
    {area === "reports" && <ReportsPanel items={reports} setItems={setReports} busy={busy} run={run} />}
    {area === "operations" && <OperationsPanel busy={busy} run={run} />}
    {area === "consolidations" && <ConsolidationsPanel busy={busy} run={run} />}
  </main>;
}

function PointsPanel({ items, catalog, busy, run }: { items: SummaryPoint[]; catalog: Catalog | null; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [selected, setSelected] = useState<SummaryPoint | null>(null); const [draft, setDraft] = useState(emptyPoint);
  const choose = (x: SummaryPoint) => { setSelected(x); setDraft({ nombre: x.nombre, categoria: x.categoria, expresion: x.expresion, seriesId: x.idSerieRem }); };
  const submit = (e: FormEvent) => { e.preventDefault(); void run(async () => { await api(selected ? `/summary-points/${selected.id}` : "/summary-points", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) }); setSelected(null); setDraft(emptyPoint); }, "Punto de resumen guardado."); };
  return <section className="workspace"><List title="Puntos de resumen" items={items} selected={selected?.id} label={x => <>{x.nombre}<small>{x.categoria} / {x.serie}</small></>} onNew={() => { setSelected(null); setDraft(emptyPoint); }} onSelect={choose} /><form onSubmit={submit} className="editor"><h2>{selected ? "Editar punto de resumen" : "Nuevo punto de resumen"}</h2><Fields value={draft} setValue={setDraft} fields={[["nombre", "Nombre"], ["categoria", "Categoría"]]} /><Select label="Serie" value={draft.seriesId} onChange={v => setDraft({ ...draft, seriesId: Number(v) })} options={catalog?.series ?? []} /><label>Expresión<textarea required value={draft.expresion} onChange={e => setDraft({ ...draft, expresion: e.target.value })} /></label><Button disabled={busy}>Guardar punto</Button>{selected && <DeleteButton name="el punto de resumen" busy={busy} onDelete={() => run(() => api(`/summary-points/${selected.id}`, { method: "DELETE" }).then(() => undefined), "Punto de resumen eliminado.")} />}</form></section>;
}

function InformesRemPanel({ catalog, busy, run }: { catalog: Catalog | null; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [items, setItems] = useState<InformeRem[]>([]); const [selected, setSelected] = useState<InformeRem | null>(null);
  const [name, setName] = useState(""); const [seriesId, setSeriesId] = useState(0); const [ids, setIds] = useState<number[]>([]);
  const [sections, setSections] = useState<SeccionRem[]>([]); const [search, setSearch] = useState("");
  const load = () => api<InformeRem[]>("/rem-informes").then(setItems).catch(() => setItems([]));
  useEffect(() => { void load(); }, []);
  useEffect(() => { if (!seriesId) { setSections([]); return; } void api<SeccionRem[]>(`/rem-informes/secciones?serieId=${seriesId}`).then(setSections).catch(() => setSections([])); }, [seriesId]);
  const reset = (x: InformeRem | null) => { setSelected(x); setName(x?.nombre ?? ""); setSeriesId(x?.idSerieRem ?? 0); setIds(x?.seccionIds ?? []); setSearch(""); };
  const term = search.trim().toLowerCase();
  const visible = sections.filter(x => ids.includes(x.id) || !term || `${x.hoja} ${x.nombre} ${x.codigo} ${x.version}`.toLowerCase().includes(term));
  const submit = (e: FormEvent) => { e.preventDefault(); void run(async () => { await api(selected ? `/rem-informes/${selected.id}` : "/rem-informes", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ nombre: name, seriesId, seccionIds: ids }) }); reset(null); await load(); }, "Informe guardado."); };
  return <section className="workspace"><List title="Informes REM" items={items} selected={selected?.id} label={x => <>{x.nombre}<small>{x.serie} / {x.seccionIds.length} secciones</small></>} onNew={() => reset(null)} onSelect={reset} /><form onSubmit={submit} className="editor"><h2>{selected ? "Editar informe" : "Nuevo informe"}</h2><p>Conjunto de secciones que se carga al elegir el informe en Consultar REM.</p><label>Nombre<input value={name} required onChange={e => setName(e.target.value)} /></label><Select label="Serie" value={seriesId} onChange={v => { setSeriesId(Number(v)); setIds([]); }} options={catalog?.series ?? []} /><label>Buscar sección<input value={search} placeholder="Hoja, nombre, código o versión…" onChange={e => setSearch(e.target.value)} /></label><fieldset><legend>Secciones ({ids.length} seleccionadas)</legend>{visible.map(x => <label className="check" key={x.id}><input type="checkbox" checked={ids.includes(x.id)} onChange={e => setIds(e.target.checked ? [...ids, x.id] : ids.filter(id => id !== x.id))} />{x.hoja} · {x.nombre || x.codigo} <small>({x.version})</small></label>)}</fieldset><Button disabled={busy || ids.length === 0}>Guardar informe</Button>{selected && <DeleteButton name="el informe" busy={busy} onDelete={() => run(async () => { await api(`/rem-informes/${selected.id}`, { method: "DELETE" }); reset(null); await load(); }, "Informe eliminado.")} />}</form></section>;
}

function FiltersPanel({ items, catalog, busy, run }: { items: Filter[]; catalog: Catalog | null; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [selected, setSelected] = useState<Filter | null>(null); const [draft, setDraft] = useState(emptyFilter);
  const [indYear, setIndYear] = useState(""); const [indSearch, setIndSearch] = useState("");
  const allIndicators = catalog?.indicators ?? [];
  const indYears = [...new Set(allIndicators.map(x => x[yearKey]).filter(y => y != null).map(String))].sort().reverse();
  const term = indSearch.trim().toLowerCase();
  const visibleIndicators = allIndicators.filter(x => x.id === draft.indicatorId || ((!indYear || String(x[yearKey]) === indYear) && (!term || x.nombre.toLowerCase().includes(term))));
  const [lYear, setLYear] = useState(""); const [lTipo, setLTipo] = useState(""); const [lSearch, setLSearch] = useState("");
  const yearOf = (id?: number | null) => id == null ? undefined : String(allIndicators.find(i => i.id === id)?.[yearKey] ?? "");
  const lTerm = lSearch.trim().toLowerCase();
  const visibleItems = items.filter(x => (!lYear || (lYear === "global" ? x.id_indicador == null : yearOf(x.id_indicador) === lYear)) && (!lTipo || String(x.tipo) === lTipo) && (!lTerm || x.establishment.toLowerCase().includes(lTerm) || (x.indicator ?? "").toLowerCase().includes(lTerm)));
  const listFilters = <div className="listFilters"><select value={lYear} onChange={e => setLYear(e.target.value)}><option value="">Todos los años</option><option value="global">Solo globales</option>{indYears.map(y => <option key={y} value={y}>{y}</option>)}</select><select value={lTipo} onChange={e => setLTipo(e.target.value)}><option value="">Excluir e incluir</option><option value="0">Excluir</option><option value="1">Incluir</option></select><input value={lSearch} placeholder="Buscar establecimiento o indicador…" onChange={e => setLSearch(e.target.value)} /></div>;
  const choose = (x: Filter) => { setSelected(x); setDraft({ establishmentId: x.id_establecimiento, indicatorId: x.id_indicador ?? null, tipo: x.tipo }); };
  const submit = (e: FormEvent) => { e.preventDefault(); void run(async () => { await api(selected ? `/filters/${selected.id}` : "/filters", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) }); setSelected(null); setDraft(emptyFilter); }, "Filtro guardado."); };
  return <section className="workspace"><List title="Filtros por establecimiento" toolbar={listFilters} items={visibleItems} selected={selected?.id} label={x => <>{x.establishment}<small>{x.indicator ?? "Todos los indicadores"} / {x.tipo === 1 ? "Incluir" : "Excluir"}</small></>} onNew={() => { setSelected(null); setDraft(emptyFilter); }} onSelect={choose} /><form onSubmit={submit} className="editor"><h2>{selected ? "Editar filtro" : "Nuevo filtro"}</h2><Select label="Establecimiento" value={draft.establishmentId} onChange={v => setDraft({ ...draft, establishmentId: Number(v) })} options={catalog?.establishments ?? []} /><label>Año del indicador<select value={indYear} onChange={e => setIndYear(e.target.value)}><option value="">Todos</option>{indYears.map(y => <option key={y} value={y}>{y}</option>)}</select></label><label>Buscar indicador<input value={indSearch} placeholder="Nombre del indicador…" onChange={e => setIndSearch(e.target.value)} /></label><label>Indicador ({visibleIndicators.length} disponibles)<select value={draft.indicatorId ?? ""} onChange={e => setDraft({ ...draft, indicatorId: e.target.value ? Number(e.target.value) : null })}><option value="">Todos los indicadores</option>{visibleIndicators.map(x => <option key={x.id} value={x.id}>{x.nombre} ({String(x[yearKey] ?? "-")})</option>)}</select></label><label>Modo<select value={draft.tipo} onChange={e => setDraft({ ...draft, tipo: Number(e.target.value) })}><option value={0}>Excluir</option><option value={1}>Incluir</option></select></label><Button disabled={busy}>Guardar filtro</Button>{selected && <DeleteButton name="el filtro" busy={busy} onDelete={() => run(() => api(`/filters/${selected.id}`, { method: "DELETE" }).then(() => undefined), "Filtro eliminado.")} />}</form></section>;
}

function InformationPanel({ items, busy, run }: { items: Informativo[]; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const today = new Date().toISOString().slice(0, 10); const blank = { tipo: 1, titulo: "", contenido: "", url: "", textoEnlace: "", fechaPublicacion: today, vigente: true, destacado: false };
  const [selected, setSelected] = useState<Informativo | null>(null); const [draft, setDraft] = useState(blank);
  const choose = (x: Informativo) => { setSelected(x); setDraft({ tipo: x.tipo, titulo: x.titulo, contenido: x.contenido, url: x.url ?? "", textoEnlace: x.textoEnlace ?? "", fechaPublicacion: x.fechaPublicacion, vigente: x.vigente, destacado: x.destacado }); };
  const submit = (e: FormEvent) => { e.preventDefault(); void run(async () => { await api(selected ? `/informativos/${selected.id}` : "/informativos", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) }); setSelected(null); setDraft(blank); }, "Informativo guardado."); };
  return <section className="workspace"><List title="Informativos" items={items} selected={selected?.id} label={x => <>{x.titulo}<small>{x.fechaPublicacion} / {x.vigente ? "Vigente" : "Oculto"}</small></>} onNew={() => { setSelected(null); setDraft(blank); }} onSelect={choose} /><form onSubmit={submit} className="editor"><h2>{selected ? "Editar informativo" : "Nuevo informativo"}</h2><label>Tipo<select value={draft.tipo} onChange={e => setDraft({ ...draft, tipo: Number(e.target.value) })}><option value={1}>Noticia</option><option value={2}>Aviso</option><option value={3}>Información</option></select></label><Fields value={draft} setValue={setDraft} fields={[["titulo", "Título"], ["fechaPublicacion", "Fecha de publicación", "date"], ["url", "URL del enlace"], ["textoEnlace", "Texto del enlace"]]} /><label>Contenido<textarea required value={draft.contenido} onChange={e => setDraft({ ...draft, contenido: e.target.value })} /></label><Checks value={draft} setValue={setDraft} keys={[["vigente", "Vigente"], ["destacado", "Destacado"]]} /><Button disabled={busy}>Guardar informativo</Button>{selected && <DeleteButton name="el informativo" busy={busy} onDelete={() => run(() => api(`/informativos/${selected.id}`, { method: "DELETE" }).then(() => undefined), "Informativo eliminado.")} />}</form></section>;
}

function ReportsPanel({ items, setItems, busy, run }: { items: Report[]; setItems: (items: Report[]) => void; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [year, setYear] = useState(String(nowYear)); const [month, setMonth] = useState(""); const [search, setSearch] = useState(""); const [detail, setDetail] = useState("");
  const load = () => void run(async () => { const params = new URLSearchParams(); if (year) params.set("year", year); if (month) params.set("month", month); if (search) params.set("search", search); setItems(await api<Report[]>(`/reports?${params}`)); }, "Registros cargados.");
  return <section className="query"><h2>Registros</h2><p>La búsqueda está limitada a 500 reportes. Al eliminar un reporte también se eliminan sus registros.</p><div className="filters"><label>Año<input type="number" value={year} onChange={e => setYear(e.target.value)} /></label><label>Mes<input type="number" min="1" max="12" value={month} onChange={e => setMonth(e.target.value)} /></label><label>Establecimiento<input value={search} onChange={e => setSearch(e.target.value)} /></label><Button type="button" disabled={busy} onClick={load}>Buscar</Button></div><div className="reportTable">{items.map(x => <article key={x.id}><div><strong>{x.establecimiento}</strong><small><Year item={x} /> / {String(x["mes"])} / {x.comuna} / {x.records} registros</small></div><div className="actions"><Button type="button" disabled={busy} onClick={() => void run(async () => setDetail(JSON.stringify(await api(`/reports/${x.id}/records`), null, 2)), `Reporte #${x.id} cargado.`)}>Ver detalle</Button><DeleteButton name={`el reporte #${x.id}`} busy={busy} onDelete={() => run(() => api(`/reports/${x.id}`, { method: "DELETE" }).then(() => undefined), "Reporte eliminado. Vuelve a buscar para actualizar la lista.")} /></div></article>)}</div>{detail && <pre className="result">{detail}</pre>}</section>;
}

function OperationsPanel({ busy, run }: { busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [year, setYear] = useState(String(nowYear)); const [series, setSeries] = useState("A"); const [result, setResult] = useState("");
  const importFiles = useRef<HTMLInputElement>(null); const integrityFiles = useRef<HTMLInputElement>(null);
  const importRem = () => void run(async () => { const files = importFiles.current?.files; if (!files?.length) throw new Error("Selecciona uno o más archivos REM."); const form = new FormData(); form.set("year", year); form.set("series", series); Array.from(files).forEach(file => form.append("files", file)); setResult(JSON.stringify(await api("/rem/import", { method: "POST", body: form }), null, 2)); }, "Importación finalizada.");
  const checkIntegrity = () => void run(async () => { const files = integrityFiles.current?.files; if (!files?.length) throw new Error("Selecciona uno o más archivos REM."); const form = new FormData(); Array.from(files).forEach(file => form.append("files", file)); setResult(JSON.stringify(await api("/rem/integrity", { method: "POST", body: form }), null, 2)); }, "Revisión de integridad finalizada.");
  const calculate = () => void run(async () => { setResult(JSON.stringify(await api(`/indicators/calculate/${year}`, { method: "POST" }), null, 2)); }, "Indicadores calculados.");
  const [pubPercapita, setPubPercapita] = useState(false); const [pubFonasa, setPubFonasa] = useState(false);
  const publish = () => { const extra = [pubPercapita && "per cápita", pubFonasa && "Fonasa"].filter(Boolean).join(" y "); if (!confirm(`¿Publicar estructura y resultados ${year}${extra ? ` (más ${extra})` : ""} en producción? Reemplaza esos datos en el servidor.`)) return; void run(async () => { setResult(JSON.stringify(await api(`/publish/${year}?percapita=${pubPercapita}&fonasa=${pubFonasa}`, { method: "POST" }), null, 2)); }, "Publicación finalizada."); };
  return <section className="operations"><div><h2>Operaciones</h2><p>La revisión REM no se incluye aquí porque ya está cubierta por el flujo existente.</p></div><article><h3>Importar archivos REM</h3><label>Año<input type="number" value={year} onChange={e => setYear(e.target.value)} /></label><label>Serie<select value={series} onChange={e => setSeries(e.target.value)}><option>A</option><option>BM</option><option>D</option><option>P</option></select></label><input ref={importFiles} type="file" accept=".xlsm" multiple /><Button type="button" disabled={busy} onClick={importRem}>Validar e importar</Button></article><article><h3>Integridad</h3><p>Revisa los archivos seleccionados sin reemplazar datos.</p><input ref={integrityFiles} type="file" accept=".xlsm" multiple /><Button type="button" disabled={busy} onClick={checkIntegrity}>Revisar archivos</Button></article><article><h3>Indicadores</h3><p>Ejecuta las reglas de cálculo conservadas para el año seleccionado.</p><Button type="button" disabled={busy} onClick={calculate}>Calcular {year}</Button></article><article><h3>Publicar en producción</h3><p>Sube la estructura completa y los resultados de {year} ya calculados en este equipo. Prod reemplaza esos datos.</p><label className="check"><input type="checkbox" checked={pubPercapita} onChange={e => setPubPercapita(e.target.checked)} />Incluir per cápita SSC {year} (si se recargó)</label><label className="check"><input type="checkbox" checked={pubFonasa} onChange={e => setPubFonasa(e.target.checked)} />Incluir inscritos Fonasa {year} (si se recargó)</label><Button type="button" disabled={busy} onClick={publish}>Publicar {year}</Button></article>{result && <pre className="result">{result}</pre>}</section>;
}

type ConsolidadoVersion = { id: number; nombre: string; fecha: string };
function ConsolidationsPanel({ busy, run }: { busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [versions, setVersions] = useState<ConsolidadoVersion[]>([]); const [year, setYear] = useState(String(nowYear)); const [versionId, setVersionId] = useState(""); const [result, setResult] = useState(""); const template = useRef<HTMLInputElement>(null);
  useEffect(() => { void api<ConsolidadoVersion[]>("/consolidados/versions").then(x => { setVersions(x); if (x[0]) setVersionId(String(x[0].id)); }).catch(error => setResult(error instanceof Error ? error.message : "No se pudieron cargar las versiones.")); }, []);
  const generate = () => void run(async () => { const file = template.current?.files?.[0]; if (!file) throw new Error("Selecciona la plantilla de Excel."); if (!versionId) throw new Error("Selecciona una versión de la serie A."); const form = new FormData(); form.set("year", year); form.set("versionId", versionId); form.set("template", file); setResult(JSON.stringify(await api("/consolidados/generate", { method: "POST", body: form }), null, 2)); }, "Consolidado generado.");
  return <section className="operations"><div><h2>Consolidados</h2><p>Los datos de origen se leen en una transacción. El resultado se escribe solo después de validar la plantilla.</p></div><article><label>Año<input type="number" value={year} onChange={e => setYear(e.target.value)} /></label><label>Versión serie A<select value={versionId} onChange={e => setVersionId(e.target.value)}>{versions.map(x => <option key={x.id} value={x.id}>{x.nombre} / {x.fecha}</option>)}</select></label><label>Plantilla de Excel<input ref={template} type="file" accept=".xlsx" /></label><Button type="button" disabled={busy} onClick={generate}>Generar consolidado</Button></article>{result && <pre className="result">{result}</pre>}</section>;
}

function List<T extends { id: number }>({ title, items, selected, label, onNew, onSelect, toolbar }: { toolbar?: React.ReactNode; title: string; items: T[]; selected?: number; label: (x: T) => React.ReactNode; onNew: () => void; onSelect: (x: T) => void }) { return <aside className="list"><div><h2>{title}</h2><Button type="button" onClick={onNew}>Nuevo</Button></div>{toolbar}<p>{items.length} registros</p>{items.map(x => <Button type="button" className={x.id === selected ? "selected" : ""} key={x.id} onClick={() => onSelect(x)}>{label(x)}</Button>)}</aside>; }
function Select({ label, value, onChange, options }: { label: string; value: number; onChange: (value: string) => void; options: { id: number; nombre: string }[] }) { return <label>{label}<select value={value || ""} onChange={e => onChange(e.target.value)} required><option value="" disabled>Selecciona...</option>{options.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label>; }
function Fields({ value, setValue, fields }: { value: Record<string, unknown>; setValue: (value: any) => void; fields: [string, string, string?][] }) { return <>{fields.map(([key, label, type]) => <label key={key}>{label}<input type={type ?? "text"} value={String(value[key] ?? "")} required={key === "nombre"} onChange={e => setValue({ ...value, [key]: type === "number" ? Number(e.target.value) : e.target.value })} /></label>)}</>; }
function Checks({ value, setValue, keys }: { value: Record<string, unknown>; setValue: (value: any) => void; keys: [string, string][] }) { return <fieldset className="checks"><legend>Opciones</legend>{keys.map(([key, label]) => <label className="check" key={key}><input type="checkbox" checked={Boolean(value[key])} onChange={e => setValue({ ...value, [key]: e.target.checked })} />{label}</label>)}</fieldset>; }
function DeleteButton({ name, busy, onDelete }: { name: string; busy: boolean; onDelete: () => void }) { return <Button type="button" className="danger" disabled={busy} onClick={() => { if (confirm(`¿Eliminar ${name}? Esta acción no se puede deshacer.`)) onDelete(); }}>Eliminar</Button>; }
