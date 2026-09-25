"use client";

import { FormEvent, useEffect, useRef, useState } from "react";

type Indicator = { id: number; nombre: string; formula: string; meta: number; peso: number; mensual: boolean; orden: number; detalle?: string | null; isDenFijo: boolean; isTasa: boolean; formulaDenFijo?: string | null; tipoindicador?: number | null; esPeriodoOctubreSep: boolean; isColaborativo: boolean; [key: string]: unknown };
type Rule = { id: number; id_version: number; idTipoRegla: number; expresion: string; mensaje: string; version: string; serie: string; tipo: string };
type SummaryPoint = { id: number; nombre: string; categoria: string; expresion: string; idSerieRem: number; serie: string };
type Convenio = { id: number; nombre: string; indicatorIds: number[] };
type Filter = { id: number; id_establecimiento: number; id_indicador?: number | null; tipo: number; establishment: string; indicator?: string | null };
type Report = { id: number; establecimiento: string; comuna: string; records: number; [key: string]: unknown };
type Informativo = { id: number; tipo: number; titulo: string; contenido: string; url?: string | null; textoEnlace?: string | null; fechaPublicacion: string; vigente: boolean; destacado: boolean };
type Catalog = { series: { id: number; nombre: string }[]; versions: { id: number; nombre: string; serie: string }[]; ruleTypes: { id: number; nombre: string }[]; establishments: { id: number; nombre: string; codDeis: string }[]; indicators: { id: number; nombre: string; [key: string]: unknown }[] };

const yearKey = "a\u00c3\u00b1o";
const nowYear = new Date().getFullYear();
const emptyIndicator = { nombre: "", formula: "", year: nowYear, meta: 0, peso: 0, mensual: false, orden: 0, detalle: "", isDenFijo: false, isTasa: false, formulaDenFijo: "", tipoIndicador: null as number | null, esPeriodoOctubreSep: false, isColaborativo: false };
const emptyRule = { versionId: 0, ruleTypeId: 0, expresion: "", mensaje: "" };
const emptyPoint = { nombre: "", categoria: "", expresion: "", seriesId: 0 };
const emptyFilter = { establishmentId: 0, indicatorId: null as number | null, tipo: 0 };

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/admin-api${path}`, init);
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.error ?? "No se pudo completar la operación.");
  return body as T;
}
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
  const nav = [["operations", "Operaciones"], ["indicators", "Indicadores"], ["rules", "Reglas"], ["points", "Puntos de resumen"], ["convenios", "Convenios"], ["filters", "Filtros por establecimiento"], ["information", "Informativos"], ["reports", "Registros"], ["consolidations", "Consolidados"]];
  return <main>
    <header className="appHeader"><div className="brand"><span className="brandMark">R</span><div><p className="eyebrow">ADMINISTRACIÓN LOCAL · 127.0.0.1</p><h1>REM Tool</h1><p>Configuración y operaciones resguardadas en este equipo.</p></div></div><p className="status" aria-live="polite"><span />{message}</p></header>
    <nav aria-label="Áreas de administración">{nav.map(([key, label]) => <Button key={key} className={area === key ? "active" : ""} onClick={() => setArea(key)}>{label}</Button>)}</nav>
    {area === "indicators" && <IndicatorPanel items={indicators} busy={busy} run={run} />}
    {area === "rules" && <RulesPanel items={rules} catalog={catalog} busy={busy} run={run} />}
    {area === "points" && <PointsPanel items={points} catalog={catalog} busy={busy} run={run} />}
    {area === "convenios" && <ConveniosPanel items={convenios} catalog={catalog} busy={busy} run={run} />}
    {area === "filters" && <FiltersPanel items={filters} catalog={catalog} busy={busy} run={run} />}
    {area === "information" && <InformationPanel items={informativos} busy={busy} run={run} />}
    {area === "reports" && <ReportsPanel items={reports} setItems={setReports} busy={busy} run={run} />}
    {area === "operations" && <OperationsPanel busy={busy} run={run} />}
    {area === "consolidations" && <ConsolidationsPanel busy={busy} run={run} />}
  </main>;
}

function IndicatorPanel({ items, busy, run }: { items: Indicator[]; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [selected, setSelected] = useState<Indicator | null>(null); const [draft, setDraft] = useState(emptyIndicator);
  const choose = (item: Indicator) => { setSelected(item); setDraft({ nombre: item.nombre, formula: item.formula, year: Number(item[yearKey]), meta: item.meta, peso: item.peso, mensual: item.mensual, orden: item.orden, detalle: item.detalle ?? "", isDenFijo: item.isDenFijo, isTasa: item.isTasa, formulaDenFijo: item.formulaDenFijo ?? "", tipoIndicador: item.tipoindicador ?? null, esPeriodoOctubreSep: item.esPeriodoOctubreSep, isColaborativo: item.isColaborativo }); };
  const submit = (event: FormEvent) => { event.preventDefault(); void run(async () => { JSON.parse(draft.formula); await api(selected ? `/indicators/${selected.id}` : "/indicators", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) }); setSelected(null); setDraft(emptyIndicator); }, "Indicador guardado."); };
  return <section className="workspace"><List title="Indicadores" items={items} selected={selected?.id} label={x => <>{x.nombre}<small><Year item={x} /> / #{x.id}</small></>} onNew={() => { setSelected(null); setDraft(emptyIndicator); }} onSelect={choose} />
    <form onSubmit={submit} className="editor"><h2>{selected ? "Editar indicador" : "Nuevo indicador"}</h2><Fields value={draft} setValue={setDraft} fields={[["nombre", "Nombre"], ["year", "Año", "number"], ["meta", "Meta", "number"], ["peso", "Peso", "number"], ["orden", "Orden", "number"], ["detalle", "Detalle"]]} /><label>Fórmula JSON<textarea value={draft.formula} onChange={e => setDraft({ ...draft, formula: e.target.value })} required /></label><label>Fórmula de denominador fijo<textarea value={draft.formulaDenFijo} onChange={e => setDraft({ ...draft, formulaDenFijo: e.target.value })} /></label><Checks value={draft} setValue={setDraft} keys={[["mensual", "Mensual"], ["isDenFijo", "Denominador fijo"], ["isTasa", "Tasa"], ["esPeriodoOctubreSep", "Período oct. - sep."], ["isColaborativo", "Colaborativo"]]} /><Button disabled={busy}>Guardar indicador</Button>{selected && <Button type="button" className="danger" disabled={busy} onClick={() => { if (confirm(`¿Eliminar ${selected.nombre}?`)) void run(() => api(`/indicators/${selected.id}`, { method: "DELETE" }).then(() => undefined), "Indicador eliminado."); }}>Eliminar</Button>}</form></section>;
}

function RulesPanel({ items, catalog, busy, run }: { items: Rule[]; catalog: Catalog | null; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [selected, setSelected] = useState<Rule | null>(null); const [draft, setDraft] = useState(emptyRule);
  const choose = (x: Rule) => { setSelected(x); setDraft({ versionId: x.id_version, ruleTypeId: x.idTipoRegla, expresion: x.expresion, mensaje: x.mensaje }); };
  const submit = (e: FormEvent) => { e.preventDefault(); void run(async () => { await api(selected ? `/rules/${selected.id}` : "/rules", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) }); setSelected(null); setDraft(emptyRule); }, "Regla guardada."); };
  return <section className="workspace"><List title="Reglas" items={items} selected={selected?.id} label={x => <>{x.tipo}<small>{x.serie} / {x.version}</small></>} onNew={() => { setSelected(null); setDraft(emptyRule); }} onSelect={choose} /><form onSubmit={submit} className="editor"><h2>{selected ? "Editar regla" : "Nueva regla"}</h2><Select label="Versión" value={draft.versionId} onChange={v => setDraft({ ...draft, versionId: Number(v) })} options={catalog?.versions ?? []} /><Select label="Tipo de regla" value={draft.ruleTypeId} onChange={v => setDraft({ ...draft, ruleTypeId: Number(v) })} options={catalog?.ruleTypes ?? []} /><label>Expresión<textarea required value={draft.expresion} onChange={e => setDraft({ ...draft, expresion: e.target.value })} /></label><label>Mensaje<textarea required value={draft.mensaje} onChange={e => setDraft({ ...draft, mensaje: e.target.value })} /></label><Button disabled={busy}>Guardar regla</Button>{selected && <DeleteButton name="la regla" busy={busy} onDelete={() => run(() => api(`/rules/${selected.id}`, { method: "DELETE" }).then(() => undefined), "Regla eliminada.")} />}</form></section>;
}

function PointsPanel({ items, catalog, busy, run }: { items: SummaryPoint[]; catalog: Catalog | null; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [selected, setSelected] = useState<SummaryPoint | null>(null); const [draft, setDraft] = useState(emptyPoint);
  const choose = (x: SummaryPoint) => { setSelected(x); setDraft({ nombre: x.nombre, categoria: x.categoria, expresion: x.expresion, seriesId: x.idSerieRem }); };
  const submit = (e: FormEvent) => { e.preventDefault(); void run(async () => { await api(selected ? `/summary-points/${selected.id}` : "/summary-points", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) }); setSelected(null); setDraft(emptyPoint); }, "Punto de resumen guardado."); };
  return <section className="workspace"><List title="Puntos de resumen" items={items} selected={selected?.id} label={x => <>{x.nombre}<small>{x.categoria} / {x.serie}</small></>} onNew={() => { setSelected(null); setDraft(emptyPoint); }} onSelect={choose} /><form onSubmit={submit} className="editor"><h2>{selected ? "Editar punto de resumen" : "Nuevo punto de resumen"}</h2><Fields value={draft} setValue={setDraft} fields={[["nombre", "Nombre"], ["categoria", "Categoría"]]} /><Select label="Serie" value={draft.seriesId} onChange={v => setDraft({ ...draft, seriesId: Number(v) })} options={catalog?.series ?? []} /><label>Expresión<textarea required value={draft.expresion} onChange={e => setDraft({ ...draft, expresion: e.target.value })} /></label><Button disabled={busy}>Guardar punto</Button>{selected && <DeleteButton name="el punto de resumen" busy={busy} onDelete={() => run(() => api(`/summary-points/${selected.id}`, { method: "DELETE" }).then(() => undefined), "Punto de resumen eliminado.")} />}</form></section>;
}

function ConveniosPanel({ items, catalog, busy, run }: { items: Convenio[]; catalog: Catalog | null; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [selected, setSelected] = useState<Convenio | null>(null); const [name, setName] = useState(""); const [ids, setIds] = useState<number[]>([]);
  const choose = (x: Convenio) => { setSelected(x); setName(x.nombre); setIds(x.indicatorIds); };
  const submit = (e: FormEvent) => { e.preventDefault(); void run(async () => { await api(selected ? `/convenios/${selected.id}` : "/convenios", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ nombre: name, indicatorIds: ids }) }); setSelected(null); setName(""); setIds([]); }, "Convenio guardado."); };
  return <section className="workspace"><List title="Convenios" items={items} selected={selected?.id} label={x => <>{x.nombre}<small>{x.indicatorIds.length} indicadores</small></>} onNew={() => { setSelected(null); setName(""); setIds([]); }} onSelect={choose} /><form onSubmit={submit} className="editor"><h2>{selected ? "Editar convenio" : "Nuevo convenio"}</h2><label>Nombre<input value={name} required onChange={e => setName(e.target.value)} /></label><fieldset><legend>Indicadores</legend>{(catalog?.indicators ?? []).map(x => <label className="check" key={x.id}><input type="checkbox" checked={ids.includes(x.id)} onChange={e => setIds(e.target.checked ? [...ids, x.id] : ids.filter(id => id !== x.id))} />{x.nombre} (<Year item={x} />)</label>)}</fieldset><Button disabled={busy}>Guardar convenio</Button>{selected && <DeleteButton name="el convenio" busy={busy} onDelete={() => run(() => api(`/convenios/${selected.id}`, { method: "DELETE" }).then(() => undefined), "Convenio eliminado.")} />}</form></section>;
}

function FiltersPanel({ items, catalog, busy, run }: { items: Filter[]; catalog: Catalog | null; busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [selected, setSelected] = useState<Filter | null>(null); const [draft, setDraft] = useState(emptyFilter);
  const choose = (x: Filter) => { setSelected(x); setDraft({ establishmentId: x.id_establecimiento, indicatorId: x.id_indicador ?? null, tipo: x.tipo }); };
  const submit = (e: FormEvent) => { e.preventDefault(); void run(async () => { await api(selected ? `/filters/${selected.id}` : "/filters", { method: selected ? "PUT" : "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(draft) }); setSelected(null); setDraft(emptyFilter); }, "Filtro guardado."); };
  return <section className="workspace"><List title="Filtros por establecimiento" items={items} selected={selected?.id} label={x => <>{x.establishment}<small>{x.indicator ?? "Todos los indicadores"} / {x.tipo === 1 ? "Incluir" : "Excluir"}</small></>} onNew={() => { setSelected(null); setDraft(emptyFilter); }} onSelect={choose} /><form onSubmit={submit} className="editor"><h2>{selected ? "Editar filtro" : "Nuevo filtro"}</h2><Select label="Establecimiento" value={draft.establishmentId} onChange={v => setDraft({ ...draft, establishmentId: Number(v) })} options={catalog?.establishments ?? []} /><label>Indicador<select value={draft.indicatorId ?? ""} onChange={e => setDraft({ ...draft, indicatorId: e.target.value ? Number(e.target.value) : null })}><option value="">Todos los indicadores</option>{(catalog?.indicators ?? []).map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label><label>Modo<select value={draft.tipo} onChange={e => setDraft({ ...draft, tipo: Number(e.target.value) })}><option value={0}>Excluir</option><option value={1}>Incluir</option></select></label><Button disabled={busy}>Guardar filtro</Button>{selected && <DeleteButton name="el filtro" busy={busy} onDelete={() => run(() => api(`/filters/${selected.id}`, { method: "DELETE" }).then(() => undefined), "Filtro eliminado.")} />}</form></section>;
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
  return <section className="operations"><div><h2>Operaciones</h2><p>La revisión REM no se incluye aquí porque ya está cubierta por el flujo existente.</p></div><article><h3>Importar archivos REM</h3><label>Año<input type="number" value={year} onChange={e => setYear(e.target.value)} /></label><label>Serie<select value={series} onChange={e => setSeries(e.target.value)}><option>A</option><option>BM</option><option>D</option><option>P</option></select></label><input ref={importFiles} type="file" accept=".xlsm" multiple /><Button type="button" disabled={busy} onClick={importRem}>Validar e importar</Button></article><article><h3>Integridad</h3><p>Revisa los archivos seleccionados sin reemplazar datos.</p><input ref={integrityFiles} type="file" accept=".xlsm" multiple /><Button type="button" disabled={busy} onClick={checkIntegrity}>Revisar archivos</Button></article><article><h3>Indicadores</h3><p>Ejecuta las reglas de cálculo conservadas para el año seleccionado.</p><Button type="button" disabled={busy} onClick={calculate}>Calcular {year}</Button></article>{result && <pre className="result">{result}</pre>}</section>;
}

type ConsolidadoVersion = { id: number; nombre: string; fecha: string };
function ConsolidationsPanel({ busy, run }: { busy: boolean; run: (a: () => Promise<void>, s: string) => Promise<void> }) {
  const [versions, setVersions] = useState<ConsolidadoVersion[]>([]); const [year, setYear] = useState(String(nowYear)); const [versionId, setVersionId] = useState(""); const [result, setResult] = useState(""); const template = useRef<HTMLInputElement>(null);
  useEffect(() => { void api<ConsolidadoVersion[]>("/consolidados/versions").then(x => { setVersions(x); if (x[0]) setVersionId(String(x[0].id)); }).catch(error => setResult(error instanceof Error ? error.message : "No se pudieron cargar las versiones.")); }, []);
  const generate = () => void run(async () => { const file = template.current?.files?.[0]; if (!file) throw new Error("Selecciona la plantilla de Excel."); if (!versionId) throw new Error("Selecciona una versión de la serie A."); const form = new FormData(); form.set("year", year); form.set("versionId", versionId); form.set("template", file); setResult(JSON.stringify(await api("/consolidados/generate", { method: "POST", body: form }), null, 2)); }, "Consolidado generado.");
  return <section className="operations"><div><h2>Consolidados</h2><p>Los datos de origen se leen en una transacción. El resultado se escribe solo después de validar la plantilla.</p></div><article><label>Año<input type="number" value={year} onChange={e => setYear(e.target.value)} /></label><label>Versión serie A<select value={versionId} onChange={e => setVersionId(e.target.value)}>{versions.map(x => <option key={x.id} value={x.id}>{x.nombre} / {x.fecha}</option>)}</select></label><label>Plantilla de Excel<input ref={template} type="file" accept=".xlsx" /></label><Button type="button" disabled={busy} onClick={generate}>Generar consolidado</Button></article>{result && <pre className="result">{result}</pre>}</section>;
}

function List<T extends { id: number }>({ title, items, selected, label, onNew, onSelect }: { title: string; items: T[]; selected?: number; label: (x: T) => React.ReactNode; onNew: () => void; onSelect: (x: T) => void }) { return <aside className="list"><div><h2>{title}</h2><Button type="button" onClick={onNew}>Nuevo</Button></div><p>{items.length} registros</p>{items.map(x => <Button type="button" className={x.id === selected ? "selected" : ""} key={x.id} onClick={() => onSelect(x)}>{label(x)}</Button>)}</aside>; }
function Select({ label, value, onChange, options }: { label: string; value: number; onChange: (value: string) => void; options: { id: number; nombre: string }[] }) { return <label>{label}<select value={value || ""} onChange={e => onChange(e.target.value)} required><option value="" disabled>Selecciona...</option>{options.map(x => <option key={x.id} value={x.id}>{x.nombre}</option>)}</select></label>; }
function Fields({ value, setValue, fields }: { value: Record<string, unknown>; setValue: (value: any) => void; fields: [string, string, string?][] }) { return <>{fields.map(([key, label, type]) => <label key={key}>{label}<input type={type ?? "text"} value={String(value[key] ?? "")} required={key === "nombre"} onChange={e => setValue({ ...value, [key]: type === "number" ? Number(e.target.value) : e.target.value })} /></label>)}</>; }
function Checks({ value, setValue, keys }: { value: Record<string, unknown>; setValue: (value: any) => void; keys: [string, string][] }) { return <fieldset className="checks"><legend>Opciones</legend>{keys.map(([key, label]) => <label className="check" key={key}><input type="checkbox" checked={Boolean(value[key])} onChange={e => setValue({ ...value, [key]: e.target.checked })} />{label}</label>)}</fieldset>; }
function DeleteButton({ name, busy, onDelete }: { name: string; busy: boolean; onDelete: () => void }) { return <Button type="button" className="danger" disabled={busy} onClick={() => { if (confirm(`¿Eliminar ${name}? Esta acción no se puede deshacer.`)) onDelete(); }}>Eliminar</Button>; }
