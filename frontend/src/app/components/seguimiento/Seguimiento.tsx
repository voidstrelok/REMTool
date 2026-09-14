"use client";
import { Fragment, useEffect, useId, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { ChevronDown, ChevronRight, Download, ArrowUpRight, Search } from "lucide-react";
import Breadcrumbs from "../Breadcrumbs";
import { apiUrl } from "@/lib/api";
import { useFiltrosSeguimiento, useRecurso } from "@/lib/hooks/useSeguimiento";
import { Indicador, Convenio, Evaluacion, Resumen, ListaIndicadores, ListaConvenios, estados,
  meses, mesNombre, numero, valor, brecha, queryContexto, retornoSeguro, seleccionarIndicadores } from "@/lib/seguimiento";
import { indicadoresConfig, isTipoIndicador } from "../../Indicadores/config";
import "./seguimiento.css";

type Filtros = ReturnType<typeof useFiltrosSeguimiento>;
type Vista = "indicadores" | "convenios" | "convenio" | "indicador";

function Estado({ e }: { e: Evaluacion }) {
  return <span className={`sg-state sg-${e.estado}`}>{estados[e.estado]}</span>;
}
function Cumplimiento({ value }: { value: number | null }) {
  return <div className="sg-compliance"><strong>{valor(value)}</strong><span className="sg-track" aria-hidden="true"><span style={{ width: `${Math.max(0, Math.min(100, value ?? 0))}%` }} /></span></div>;
}
function FiltrosBar({ f, añoFijo }: { f: Filtros; añoFijo?: number }) {
  const years = [...new Set([new Date().getFullYear() - 1, new Date().getFullYear(), f.year])].sort((a, b) => b - a);
  return <section className="sg-filters" aria-label="Filtros de seguimiento">
    {añoFijo ? <div><span className="sg-label">Año</span><strong>{añoFijo}</strong></div> : <label>Año<select value={f.year} onChange={e => f.cambiar({ ano: e.target.value })}>{years.map(y => <option key={y}>{y}</option>)}</select></label>}
    <label>Corte<select value={f.corte} onChange={e => f.cambiar({ mesCorte: e.target.value })}><option value="">Último disponible</option>{meses.map((m, i) => <option key={m} value={i + 1}>{m}</option>)}</select></label>
    <label>Sector<select value={f.sector} disabled={f.sectores.loading} onChange={e => f.cambiar({ sectorId: e.target.value, establecimientoId: "" })}><option value="">Todos</option>{f.sectores.data?.map(s => <option key={s.id} value={s.id}>{s.nombre}</option>)}</select></label>
    <label className="sg-filter-est">Establecimiento<select value={f.establecimiento} disabled={f.establecimientos.loading} onChange={e => f.cambiar({ establecimientoId: e.target.value })}><option value="">Todos</option>{f.establecimientos.data?.map(s => <option key={s.id} value={s.id}>{s.nombre}</option>)}</select></label>
    {(f.sector || f.establecimiento || f.corte) && <button className="sg-button" onClick={() => f.cambiar({ sectorId: "", establecimientoId: "", mesCorte: "" })}>Limpiar filtros</button>}
    {(f.sectores.error || f.establecimientos.error) && <p className="sg-filter-error" role="alert">No se pudieron cargar los filtros. <button onClick={() => { f.sectores.reintentar(); f.establecimientos.reintentar(); }}>Reintentar</button></p>}
  </section>;
}
function Exportar({ path, disabled }: { path: string; disabled: boolean }) {
  const [busy, setBusy] = useState(false), [error, setError] = useState("");
  useEffect(() => setError(""), [path]);
  async function descargar() {
    setBusy(true); setError("");
    try {
      const response = await fetch(apiUrl(path));
      if (!response.ok) throw new Error("No se pudo generar el respaldo. Intenta nuevamente.");
      const url = URL.createObjectURL(await response.blob());
      const link = document.createElement("a"); link.href = url; link.download = "respaldo-seguimiento.pdf";
      document.body.appendChild(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (e) { setError(e instanceof Error ? e.message : "No se pudo descargar."); }
    finally { setBusy(false); }
  }
  return <div className="sg-export"><button className="sg-button" onClick={descargar} disabled={disabled || busy}><Download size={16} />{busy ? "Generando respaldo…" : "Descargar respaldo PDF"}</button>{error && <p role="alert">{error}</p>}</div>;
}
export function ResumenPanel({ resumen }: { resumen: Resumen }) {
  return <section aria-label="Resumen del contexto" className="sg-summary">
    <div className="sg-kpi sg-kpi-primary"><span>Cumplimiento general</span><strong>{valor(resumen.cumplimiento)}</strong><small>{resumen.total} indicadores en el contexto</small></div>
    <div className="sg-kpi"><span>Metas cumplidas</span><strong>{resumen.cumplidas}</strong><small>{resumen.enCurso} en curso</small></div>
    <div className="sg-kpi"><span>Requieren atención</span><strong>{resumen.enRiesgo + resumen.criticos}</strong><small>{resumen.criticos} críticos · {resumen.enRiesgo} en riesgo</small></div>
    <div className="sg-kpi"><span>Sin datos</span><strong>{resumen.sinDatos}</strong><small>{resumen.sinDenominador} sin denominador · {resumen.sinMeta} sin meta</small></div>
    <p className="sg-summary-note">{resumen.metodo}</p>
  </section>;
}

export function IndicadoresTabla({ items, f, detalleHref }: { items: Indicador[]; f: Filtros; detalleHref: (i: Indicador) => string }) {
  const [expanded, setExpanded] = useState<number[]>([]);
  const buscar = f.params.get("buscar") ?? "", estado = f.params.get("estado") ?? "", orden = f.params.get("orden") ?? "";
  const [busqueda, setBusqueda] = useState(buscar);
  useEffect(() => setBusqueda(buscar), [buscar]);
  const visibles = seleccionarIndicadores(items, buscar, estado, orden);
  function toggle(id: number) { setExpanded(ids => ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id]); }
  return <section className="sg-panel" aria-label="Comparación de indicadores">
    <div className="sg-panel-heading"><div><h2>Indicadores</h2><p>{visibles.length} de {items.length} indicadores · Los valores conservan el sobrecumplimiento.</p></div></div>
    <div className="sg-table-tools">
      <label className="sg-search"><Search size={16} aria-hidden="true" /><span className="sg-sr">Buscar indicador</span><input type="search" placeholder="Buscar indicador…" value={busqueda} onChange={e => { setBusqueda(e.target.value); f.cambiar({ buscar: e.target.value }); }} /></label>
      <label><span className="sg-sr">Filtrar por estado</span><select value={estado} onChange={e => f.cambiar({ estado: e.target.value })}><option value="">Todos los estados</option>{Object.entries(estados).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
      <label><span className="sg-sr">Ordenar indicadores</span><select value={orden} onChange={e => f.cambiar({ orden: e.target.value })}><option value="">Orden del programa</option><option value="cumplimiento">Menor cumplimiento primero</option><option value="brecha">Menor brecha, por unidad</option></select></label>
    </div>
    {orden === "brecha" && <p className="sg-note">Las brechas se ordenan dentro de su unidad: porcentajes y luego tasas.</p>}
    <div className="sg-table-scroll"><table className="sg-table sg-indicator-table"><caption className="sg-sr">Resultados, metas y brechas de los indicadores al corte</caption>
      <thead><tr><th>Indicador</th><th>Resultado</th><th>Meta</th><th>Cumplimiento</th><th>Esperado</th><th>Brecha</th><th>Estado</th></tr></thead>
      <tbody>{visibles.map(i => <Fragment key={i.id}><tr>
        <td className="sg-name"><button className="sg-expand" onClick={() => toggle(i.id)} aria-expanded={expanded.includes(i.id)} aria-controls={`sg-expand-${i.id}`}>{expanded.includes(i.id) ? <ChevronDown size={17} /> : <ChevronRight size={17} />}<span><span className="sg-order">{i.orden}.</span> {i.nombre}</span></button>{i.evaluacion.provisional && <small>Evaluación provisional</small>}</td>
        <td data-label="Resultado"><strong>{valor(i.evaluacion.resultado, i.isTasa)}</strong></td><td data-label="Meta">{valor(i.evaluacion.meta, i.isTasa)}</td>
        <td data-label="Cumplimiento"><Cumplimiento value={i.evaluacion.cumplimiento} /></td><td data-label="Esperado">{valor(i.evaluacion.esperado, i.isTasa)}</td>
        <td data-label="Brecha">{brecha(i.evaluacion.brecha, i.isTasa)}</td><td data-label="Estado"><Estado e={i.evaluacion} /><Link className="sg-detail-link" href={detalleHref(i)}>Ver detalle <ArrowUpRight size={13} /></Link></td>
      </tr>{expanded.includes(i.id) && <tr className="sg-expanded" id={`sg-expand-${i.id}`}><td colSpan={7}><dl className="sg-facts">
        <div><dt>Numerador</dt><dd>{numero(i.numerador, 0)}</dd></div><div><dt>{i.isColaborativo ? "Denominador comunal" : "Denominador"}</dt><dd>{numero(i.denominador, 0)}</dd></div>
        <div><dt>Periodicidad</dt><dd>{i.mensual ? "Mensual" : "Semestral"}</dd></div><div><dt>Evaluación</dt><dd>{mesNombre(i.evaluacion.mesEvaluacion)}</dd></div><div><dt>Último registro al corte</dt><dd>{mesNombre(i.evaluacion.ultimoMesDatos)}</dd></div>
        {i.tipoindicador !== 1 && <><div><dt>Peso</dt><dd>{valor(i.peso * 100)}</dd></div><div><dt>Aporte al agregado</dt><dd>{valor(i.aporte * 100)}{i.ajuste > 0 ? ` (incluye ajuste de ${numero(i.ajuste * 100, 2)} pp)` : ""}</dd></div></>}
      </dl></td></tr>}</Fragment>)}</tbody></table></div>
    {!visibles.length && <div className="sg-empty">No hay indicadores que coincidan con esta consulta.</div>}
  </section>;
}

function Evolucion({ item }: { item: Indicador }) {
  const id = useId();
  const points = item.evolucion;
  const max = Math.max(1, ...points.flatMap(e => [e.resultado ?? 0, e.esperado, e.meta])) * 1.1;
  const x = (mes: number) => 60 + (mes - 1) / 11 * 690;
  const y = (value: number) => 235 - value / max * 205;
  const series = [{ key: "resultado", color: "var(--primary)", label: "Resultado", dash: undefined }, { key: "esperado", color: "#0284c7", label: "Esperado", dash: "6 4" }] as const;
  return <section className="sg-panel"><div className="sg-panel-heading"><div><h2>Evolución acumulada</h2><p>{item.mensual ? "Resultado mensual" : "Evaluación por cierre semestral"} · {item.isTasa ? "Tasa" : "Porcentaje"}</p></div><div className="sg-legend"><span>━ Resultado</span><span>┄ Esperado</span><span>┄ Meta</span></div></div>
    <div className="sg-chart"><svg viewBox="0 0 800 280" role="img" aria-labelledby={id}><title id={id}>Resultado acumulado, esperado y meta. Los valores exactos están en la tabla siguiente.</title>
      {[0, 1, 2, 3, 4].map(t => <g key={t}><line x1="60" x2="750" y1={y(max * t / 4)} y2={y(max * t / 4)} stroke="var(--border)" /><text x="50" y={y(max * t / 4) + 4} textAnchor="end">{numero(max * t / 4, 0)}</text></g>)}
      <line x1="60" x2="750" y1={y(item.evaluacion.meta)} y2={y(item.evaluacion.meta)} stroke="#15803d" strokeDasharray="4 4" /><text x="750" y={y(item.evaluacion.meta) - 7} textAnchor="end">Meta {valor(item.evaluacion.meta, item.isTasa)}</text>
      {series.map(s => <g key={s.key}>{points.map((p, index) => { const value = p[s.key]; const prev = points[index - 1]; return value == null ? null : <g key={p.mesEvaluacion}>
        {prev?.[s.key] != null && <line x1={x(prev.mesEvaluacion)} y1={y(prev[s.key]!)} x2={x(p.mesEvaluacion)} y2={y(value)} stroke={s.color} strokeWidth="2.5" strokeDasharray={s.dash} />}
        <circle cx={x(p.mesEvaluacion)} cy={y(value)} r="4" fill={s.color}><title>{`${mesNombre(p.mesEvaluacion)}: ${s.label} ${valor(value, item.isTasa)}`}</title></circle></g>; })}</g>)}
      {points.map(p => <text key={p.mesEvaluacion} x={x(p.mesEvaluacion)} y="263" textAnchor="middle">{mesNombre(p.mesEvaluacion).slice(0, 3)}</text>)}
    </svg></div>
    <details className="sg-disclosure"><summary>Ver datos de la evolución</summary><div className="sg-table-scroll"><table className="sg-table"><caption className="sg-sr">Valores acumulados por evaluación</caption><thead><tr><th>Evaluación</th><th>Numerador</th><th>Denominador</th><th>Resultado</th><th>Esperado</th><th>Brecha</th><th>Estado</th></tr></thead><tbody>{points.map(e => <tr key={e.mesEvaluacion}><td>{mesNombre(e.mesEvaluacion)}{e.provisional ? " (provisional)" : ""}{!e.tieneRegistroPeriodo && e.tieneDatos ? " · Sin registros nuevos" : ""}</td><td>{e.tieneDatos ? numero(e.numerador, 0) : "—"}</td><td>{e.tieneDatos ? numero(e.denominador, 0) : "—"}</td><td>{valor(e.resultado, item.isTasa)}</td><td>{valor(e.esperado, item.isTasa)}</td><td>{brecha(e.brecha, item.isTasa)}</td><td><Estado e={e} /></td></tr>)}</tbody></table></div></details>
  </section>;
}

export function Detalle({ item }: { item: Indicador }) {
  const e = item.evaluacion;
  const [matriz, setMatriz] = useState(false);
  return <>
    <section className="sg-detail-kpis" aria-label="Resultado del indicador">{[["Resultado", valor(e.resultado, item.isTasa)], ["Meta", valor(e.meta, item.isTasa)], ["Cumplimiento", valor(e.cumplimiento)], ["Esperado al corte", valor(e.esperado, item.isTasa)], ["Brecha al esperado", brecha(e.brecha, item.isTasa)]].map(([label, value]) => <div className="sg-kpi" key={label}><span>{label}</span><strong>{value}</strong></div>)}</section>
    <div className="sg-evaluation"><Estado e={e} /><span>Evaluación: <strong>{mesNombre(e.mesEvaluacion)}</strong>{e.provisional ? " · Provisional, antes del primer cierre" : ""}</span><span>Último registro al corte: {mesNombre(e.ultimoMesDatos)}</span></div>
    <dl className="sg-facts sg-detail-facts"><div><dt>Numerador</dt><dd>{numero(e.numerador, 0)}</dd></div><div><dt>{item.isColaborativo ? "Denominador comunal" : "Denominador"}</dt><dd>{numero(e.denominador, 0)}</dd></div><div><dt>Periodicidad</dt><dd>{item.mensual ? "Mensual" : "Semestral"}</dd></div>{item.esPeriodoOctubreSep && <div><dt>Período del denominador</dt><dd>Octubre {item.año - 1} – septiembre {item.año}, hasta el corte</dd></div>}</dl>
    <Evolucion item={item} />
    <section className="sg-panel"><div className="sg-panel-heading"><div><h2>Comparación por establecimiento</h2><p>{item.isColaborativo ? "Producción aportada al objetivo comunal; no representa cumplimiento individual." : "Totales y estado al mismo período de evaluación."}</p></div><button className="sg-button" aria-pressed={matriz} onClick={() => setMatriz(v => !v)}>{matriz ? "Ver comparación" : "Ver matriz mensual"}</button></div>
      <div className="sg-table-scroll"><table className="sg-table sg-est-table"><caption className="sg-sr">{matriz ? "Registros mensuales y serie P por establecimiento" : "Resultados por establecimiento"}</caption><thead><tr><th>Establecimiento</th>{matriz ? <>{meses.slice(0, e.mesEvaluacion).map(m => <th key={m}>{m.slice(0, 3)}</th>)}<th>Acumulado</th></> : <><th>Sector</th><th>{item.isColaborativo ? "Producción aportada" : "Numerador"}</th>{!item.isColaborativo && <><th>Denominador</th><th>Resultado</th><th>Cumplimiento</th><th>Estado</th></>}</>}</tr></thead>
        <tbody>{item.establecimientos.map(s => <tr key={s.id}><th scope="row">{s.nombre}</th>{matriz ? <>{meses.slice(0, e.mesEvaluacion).map((m, index) => {
          const records = s.meses.filter(r => r.mes === index + 1);
          return <td key={m}>{records.length ? records.map((r, j) => <div key={j}>{numero(r.numerador, 0)}{!item.isColaborativo && !item.isDenFijo ? ` / ${numero(r.denominador, 0)}` : ""}{(r.numeradorP !== 0 || r.denominadorP !== 0) && <small className="sg-serie">P: {numero(r.numeradorP, 0)}{!item.isColaborativo && !item.isDenFijo ? ` / ${numero(r.denominadorP, 0)}` : ""}</small>}</div>) : "—"}</td>;
        })}<td>{numero(s.evaluacion.numerador, 0)}{!item.isColaborativo ? ` / ${numero(s.evaluacion.denominador, 0)}` : ""}</td></> : <><td>{s.sector}</td><td>{numero(s.evaluacion.numerador, 0)}</td>{!item.isColaborativo && <><td>{numero(s.evaluacion.denominador, 0)}</td><td>{valor(s.evaluacion.resultado, item.isTasa)}</td><td><Cumplimiento value={s.evaluacion.cumplimiento} /></td><td><Estado e={s.evaluacion} /></td></>}</>}</tr>)}</tbody></table></div>
      {!item.establecimientos.length && <p className="sg-empty">No hay registros de establecimientos en el período evaluado.</p>}
      {matriz && <p className="sg-note">Cada celda muestra numerador / denominador, o solo producción cuando corresponde. P identifica aportes de serie P; “—” indica ausencia de registros y 0 es producción registrada. El acumulado aplica las reglas del indicador y puede incorporar denominadores del año anterior.</p>}
    </section>
    <section className="sg-panel sg-analysis"><h2>Análisis y contexto</h2><ul>{item.analisis.map(text => <li key={text}>{text}</li>)}</ul>{item.detalle && <div className="sg-description"><h3>Descripción del indicador</h3><p>{item.detalle}</p></div>}</section>
  </>;
}

export default function SeguimientoPage({ vista }: { vista: Vista }) {
  const router = useRouter();
  const params = useSearchParams();
  const f = useFiltrosSeguimiento(vista === "indicador" ? params.get("id") ?? undefined : undefined);
  const id = f.params.get("id");
  const tipoValue = f.params.get("tipo");
  const tipo = tipoValue && isTipoIndicador(tipoValue) ? tipoValue : null;
  const config = tipo ? indicadoresConfig[tipo] : null;
  const endpoint = vista === "indicadores" ? config && tipo !== "Convenios" ? `getIndicadores/${f.year}/${config.tipoId}?${f.query}&incluirResumen=true` : null
    : vista === "convenios" ? `getConvenios/${f.year}?${f.query}&incluirResumen=true`
    : !id ? null : vista === "convenio" ? `getConvenioIndicadores/${id}/${f.year}?${f.query}` : `getIndicador/${id}?${f.query}`;
  const resource = useRecurso<ListaIndicadores | ListaConvenios | Convenio | Indicador>(endpoint);
  useEffect(() => {
    if (vista === "indicadores" && tipo === "Convenios") { const q = new URLSearchParams(f.params.toString()); q.delete("tipo"); router.replace(`/Convenios?${q}`); }
  }, [vista, tipo, f.params, router]);
  if (vista === "indicadores" && !tipo) return <div className="sg-page"><Breadcrumbs items={[{ label: "Indicadores" }]} /><h1>Seguimiento de indicadores</h1><p>Selecciona el programa para consultar resultados y brechas.</p><div className="sg-programs">{Object.entries(indicadoresConfig).map(([key, c]) => <Link key={key} href={key === "Convenios" ? "/Convenios" : `/Indicadores?tipo=${key}`}><h2>{c.navLabel}</h2><span>Consultar seguimiento <ArrowUpRight size={18} /></span></Link>)}</div></div>;
  const data = resource.data;
  const indicador = vista === "indicador" ? data as Indicador | null : null;
  const convenio = vista === "convenio" ? data as Convenio | null : null;
  const lista = vista === "indicadores" ? data as ListaIndicadores | null : null;
  const listaConvenios = vista === "convenios" ? data as ListaConvenios | null : null;
  const corte = indicador?.evaluacion.mesCorte ?? convenio?.mesCorte ?? lista?.mesCorte ?? listaConvenios?.mesCorte;
  const contexto = queryContexto(new URLSearchParams(f.params.toString()), corte);
  const año = indicador?.año ?? f.year;
  const titulo = indicador?.nombre ?? convenio?.nombre ?? (vista === "convenios" ? "Seguimiento de convenios" : config?.navLabel ?? "Seguimiento de indicadores");
  const back = retornoSeguro(f.params.get("back"), `${tipo === "Convenios" || vista === "convenio" ? "/Convenios" : "/Indicadores"}?${new URLSearchParams({ ...Object.fromEntries(contexto), ano: String(año), ...(tipo && tipo !== "Convenios" ? { tipo } : {}) })}`);
  const backActual = `${f.pathname}?${new URLSearchParams({ ...Object.fromEntries(f.params), ...Object.fromEntries(contexto), ano: String(año) })}`;
  function detalleHref(item: Indicador) {
    const params = new URLSearchParams({ ...Object.fromEntries(contexto), ano: String(item.año), tipo: item.tipoindicador === 1 ? "Convenios" : item.tipoindicador === 2 ? "MetasSanitarias" : "IAAPS", id: String(item.id), back: backActual });
    return `/Indicadores/DetalleIndicador?${params}`;
  }
  const exportQuery = new URLSearchParams(contexto);
  for (const key of ["buscar", "estado", "orden"]) if (f.params.get(key)) exportQuery.set(key, f.params.get(key)!);
  if (vista === "convenio" && id) exportQuery.set("convenioId", id);
  const exportPath = vista === "indicador" ? `detalleIndicador/${id}?${exportQuery}` : vista === "convenios" ? `informeConvenios/${año}?${exportQuery}` : `informeMensual/${vista === "convenio" ? 1 : config?.tipoId}/${año}?${exportQuery}`;
  const resumen = lista?.resumen ?? convenio?.resumen;
  const criterios = (data && "criterios" in data ? data.criterios : null) as string | null;
  const items = lista?.items ?? convenio?.indicadores ?? [];
  return <div className="sg-page">
    <Breadcrumbs items={vista === "indicador" || vista === "convenio" ? [{ label: vista === "convenio" ? "Convenios" : config?.navLabel ?? "Indicadores", href: back }, { label: titulo }] : [{ label: titulo }]} />
    <header className="sg-header"><div><p className="sg-eyebrow">Seguimiento · {año}</p><h1>{titulo}</h1><p>Resultados, metas y brechas para orientar el seguimiento de la red.</p></div><div className="sg-header-actions">{(vista === "indicador" || vista === "convenio") && <Link className="sg-button" href={back}>Volver</Link>}<Exportar path={exportPath} disabled={!data || resource.loading || !!resource.error} /></div></header>
    <FiltrosBar f={f} añoFijo={vista === "indicador" ? indicador?.año ?? año : undefined} />
    {resource.loading && <div className="sg-loading" role="status">Cargando resultados del contexto seleccionado…</div>}
    {resource.error && <div className="sg-error" role="alert">{resource.error} <button className="sg-button" onClick={resource.reintentar}>Reintentar</button></div>}
    {!id && (vista === "indicador" || vista === "convenio") && <p className="sg-empty">Falta el identificador del {vista}.</p>}
    {data && <><p className="sg-context">Corte consultado: <strong>{mesNombre(corte)} {año}</strong> · {f.establecimientos.data?.find(e => String(e.id) === f.establecimiento)?.nombre ?? f.sectores.data?.find(s => String(s.id) === f.sector)?.nombre ?? "Red comunal"}</p>
      {resumen && <ResumenPanel resumen={resumen} />}
      {listaConvenios && <><section className="sg-summary"><div className="sg-kpi sg-kpi-primary"><span>Cumplimiento general</span><strong>{valor(listaConvenios.cumplimiento)}</strong><small>{listaConvenios.items.length} convenios</small></div><p className="sg-summary-note">Promedio simple del cumplimiento de los convenios. Incluye convenios sin indicadores con aporte cero.</p></section>
        <section className="sg-panel"><div className="sg-panel-heading"><h2>Convenios</h2></div><div className="sg-table-scroll"><table className="sg-table sg-convenio-table"><thead><tr><th>Convenio</th><th>Indicadores</th><th>Cumplimiento</th><th>Cumplidas</th><th>En curso</th><th>Atención</th><th>Sin evaluar</th></tr></thead><tbody>{listaConvenios.items.map(c => <tr key={c.id}><td className="sg-name"><Link href={`/Convenios/DetalleConvenio?${new URLSearchParams({ ...Object.fromEntries(contexto), id: String(c.id), ano: String(año), back: backActual })}`}>{c.nombre} <ArrowUpRight size={14} /></Link></td><td data-label="Indicadores">{c.indicadorCount}</td><td data-label="Cumplimiento"><Cumplimiento value={c.resumen.cumplimiento} /></td><td data-label="Cumplidas">{c.resumen.cumplidas}</td><td data-label="En curso">{c.resumen.enCurso}</td><td data-label="Atención">{c.resumen.criticos} críticos · {c.resumen.enRiesgo} en riesgo</td><td data-label="Sin evaluar">{c.resumen.sinDatos} sin datos · {c.resumen.sinDenominador + c.resumen.sinMeta} sin evaluación</td></tr>)}</tbody></table></div>{!listaConvenios.items.length && <p className="sg-empty">No hay convenios para esta consulta.</p>}</section></>}
      {(lista || convenio) && <IndicadoresTabla items={items} f={f} detalleHref={detalleHref} />}
      {indicador && <Detalle item={indicador} />}
      {criterios && <details className="sg-panel sg-criteria"><summary>Cómo interpretar estos resultados</summary><p>{criterios}</p><p>Las barras representan cumplimiento de la meta y se limitan visualmente a 100%. Los valores numéricos conservan el sobrecumplimiento.</p></details>}
    </>}
  </div>;
}
