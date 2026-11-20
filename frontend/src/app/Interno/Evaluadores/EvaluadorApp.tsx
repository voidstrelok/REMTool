"use client";

import { ChangeEvent, Fragment, useEffect, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import {
  AlertTriangle, CheckCircle2, ChevronLeft, ChevronRight, Copy, FileJson, FileSpreadsheet, Plus, Printer, Trash2, Upload, X, XCircle,
} from "lucide-react";
import "./Evaluadores.css";
import { Button } from "@/components/ui/button";
import type { Campo, Evaluador, Item, Opcion, Pauta, Registro, Seccion, Tabla } from "./tipos";
import { evaluador } from "./defs";
import { ESTABLECIMIENTOS } from "./defs/comun";
import {
  avisos, fmtPct, fmtPts, maxPts, normalizar, nuevoRegistro, opcionElegida, opcionesDe, puntajePauta, puntajeSeccion, ResultadoPauta,
  tituloRegistro, tonoDe, uid, unidades,
} from "./puntaje";
import { descargar, excelEvaluacion, nombreArchivo } from "./exportar";

type Upd = (fn: (r: Registro) => void) => void;
const KEY = (id: string) => `evaluador:${id}`;
const ordenar = (l: Registro[]) => [...l].sort((a, b) => b.actualizado - a.actualizado);

function leer(ev: Evaluador): Registro[] {
  try {
    const l = JSON.parse(localStorage.getItem(KEY(ev.id)) ?? "[]");
    return Array.isArray(l) ? l.map(x => normalizar(ev, x)) : [];
  } catch { return []; }
}

export default function EvaluadorApp({ id }: { id: string }) {
  const ev = evaluador(id)!;
  const [regs, setRegs] = useState<Registro[] | null>(null);
  const [curId, setCurId] = useState("");
  const [tab, setTab] = useState("");
  const [secIdx, setSecIdx] = useState<Record<string, number>>({});
  const [estado, setEstado] = useState<{ t: string; err?: boolean }>({ t: "" });

  useEffect(() => {
    const l = ordenar(leer(ev));
    if (!l.length) l.push(nuevoRegistro(ev));
    setRegs(l); setCurId(l[0].id);
  }, [ev]);

  useEffect(() => {
    if (!regs) return;
    try {
      localStorage.setItem(KEY(ev.id), JSON.stringify(regs));
      setEstado({ t: `Guardado en este navegador · ${new Date().toLocaleTimeString("es-CL", { hour: "2-digit", minute: "2-digit" })}` });
    } catch { setEstado({ t: "No se pudo guardar: el almacenamiento del navegador está lleno o bloqueado. Descargue un respaldo.", err: true }); }
  }, [regs, ev.id]);

  const reg = regs?.find(r => r.id === curId);
  const upd: Upd = fn => setRegs(l => l!.map(r => {
    if (r.id !== curId) return r;
    const c = structuredClone(r); fn(c); c.actualizado = Date.now(); return c;
  }));

  const tablasDatos = (ev.tablas ?? []).filter(t => !t.firma);
  const tabs: [string, string][] = [
    ...(tablasDatos.length ? [["datos", "Antecedentes"] as [string, string]] : []),
    ...ev.pautas.map(p => [`p:${p.id}`, p.titulo] as [string, string]),
    ...(ev.anexos?.length ? [["anexos", "Anexos"] as [string, string]] : []),
    ["res", "Resultados"],
  ];
  const tabActual = tabs.some(t => t[0] === tab) ? tab : tabs[0][0];
  const irA = (t: string) => { setTab(t); window.scrollTo({ top: 0, behavior: "smooth" }); };

  if (!regs || !reg) return null;

  const agregar = (r: Registro) => { setRegs(l => [r, ...l!]); setCurId(r.id); setTab(tabs[0][0]); setSecIdx({}); };
  const eliminar = () => {
    if (!confirm(`¿Eliminar la evaluación «${tituloRegistro(reg)}»? No se puede deshacer.`)) return;
    const resto = regs.filter(r => r.id !== reg.id);
    if (!resto.length) resto.push(nuevoRegistro(ev));
    setRegs(resto); setCurId(ordenar(resto)[0].id);
  };
  const imprimir = () => {
    const t = document.title;
    document.title = nombreArchivo(ev, reg);
    window.addEventListener("afterprint", () => { document.title = t; }, { once: true });
    window.print();
  };
  const respaldo = () => descargar(JSON.stringify({ evaluador: ev.id, version: 1, exportado: new Date().toISOString(), registros: regs }, null, 1),
    `respaldo-${ev.id}-${new Date().toLocaleDateString("sv-SE")}.json`, "application/json");
  const importar = async (e: ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0]; e.target.value = "";
    if (!f) return;
    try {
      const o = JSON.parse(await f.text());
      if (o?.evaluador && o.evaluador !== ev.id) throw new Error(`El respaldo corresponde a otro evaluador («${evaluador(o.evaluador)?.titulo ?? o.evaluador}»).`);
      const l: Partial<Registro>[] | null = o?.evaluador ? (Array.isArray(o.registros) ? o.registros : null) : ev.importarLegado?.(o) ?? null;
      if (!l?.length) throw new Error("El archivo no contiene evaluaciones válidas de este evaluador.");
      // Nunca se sobrescribe: si el id ya existe, entra como copia.
      const ids = new Set(regs.map(r => r.id));
      const nuevos = l.map(x => { const n = normalizar(ev, x); if (ids.has(n.id)) n.id = uid(); ids.add(n.id); return n; });
      setRegs(ordenar([...nuevos, ...regs])); setCurId(nuevos[0].id);
      setEstado({ t: `${nuevos.length} evaluación(es) importada(s).` });
    } catch (err) {
      setEstado({ t: err instanceof SyntaxError ? "El archivo no es un JSON válido." : err instanceof Error ? err.message : String(err), err: true });
    }
  };

  const pauta = ev.pautas.find(p => `p:${p.id}` === tabActual);

  return (
    <div className="flex flex-col gap-4">
      <section className="ev-card ev-toolbar" aria-label="Evaluaciones guardadas">
        <label className="ev-field ev-grow">
          <span>Evaluación</span>
          <select value={reg.id} onChange={e => setCurId(e.target.value)}>
            {ordenar(regs).map(r => <option key={r.id} value={r.id}>{tituloRegistro(r)}{r.campos.evaluador ? ` · ${r.campos.evaluador}` : ""}</option>)}
          </select>
        </label>
        <div className="ev-actions">
          <Button type="button" variant="outline" onClick={() => agregar(nuevoRegistro(ev))}><Plus aria-hidden="true" />Nueva</Button>
          <Button type="button" variant="outline" onClick={() => agregar({ ...structuredClone(reg), id: uid(), actualizado: Date.now(), campos: { ...reg.campos, establecimiento: `${reg.campos.establecimiento ?? ""} (copia)`.trim() } })}><Copy aria-hidden="true" />Duplicar</Button>
          <Button type="button" variant="destructive" onClick={eliminar}><Trash2 aria-hidden="true" />Eliminar</Button>
        </div>
        <div className="ev-actions">
          <Button type="button" onClick={() => descargar(excelEvaluacion(ev, reg), `${nombreArchivo(ev, reg)}.xlsx`, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")}><FileSpreadsheet aria-hidden="true" />Excel</Button>
          <Button type="button" onClick={imprimir}><Printer aria-hidden="true" />PDF</Button>
          <Button type="button" variant="outline" onClick={respaldo} title="Descarga todas las evaluaciones de este evaluador"><FileJson aria-hidden="true" />Respaldo</Button>
          <label className="ev-file-btn"><Upload size={15} aria-hidden="true" />Importar<input type="file" accept=".json,application/json" className="sr-only" onChange={importar} /></label>
        </div>
        <p className={`ev-status${estado.err ? " err" : ""}`} role="status">{estado.t}</p>
      </section>

      {!tablasDatos.length && <section className="ev-card"><Campos campos={ev.campos} reg={reg} upd={upd} /></section>}

      <nav className="ev-tabs" role="tablist">
        {tabs.map(([k, label]) => {
          const p = ev.pautas.find(x => `p:${x.id}` === k), r = p && puntajePauta(p, reg);
          return <button key={k} type="button" role="tab" aria-selected={k === tabActual} className={k === tabActual ? "on" : ""} onClick={() => irA(k)}>
            <b>{label}</b>{r && <small>{r.respondidos}/{r.total} · {fmtPct(r.pct)}</small>}
          </button>;
        })}
      </nav>

      {tabActual === "datos" && <>
        <section className="ev-card"><h2>Antecedentes</h2><Campos campos={ev.campos} reg={reg} upd={upd} /></section>
        {tablasDatos.map(t => <TablaEditor key={t.id} t={t} reg={reg} upd={upd} />)}
      </>}
      {pauta && <PautaView key={pauta.id} p={pauta} reg={reg} upd={upd} idx={Math.min(secIdx[pauta.id] ?? 0, pauta.secciones.length - 1)}
        setIdx={i => { setSecIdx(s => ({ ...s, [pauta.id]: i })); window.scrollTo({ top: 0, behavior: "smooth" }); }}
        siguiente={() => irA(tabs[tabs.findIndex(t => t[0] === tabActual) + 1]?.[0] ?? tabActual)} />}
      {tabActual === "anexos" && <Anexos ev={ev} reg={reg} upd={upd} />}
      {tabActual === "res" && <Resultados ev={ev} reg={reg} upd={upd} />}

      {createPortal(<Impresion ev={ev} reg={reg} />, document.body)}
    </div>
  );
}

/* ---------- Antecedentes ---------- */

function Campos({ campos, reg, upd }: { campos: Campo[]; reg: Registro; upd: Upd }) {
  const set = (id: string, v: string) => upd(r => { r.campos[id] = v; });
  return (
    <div className="ev-campos">
      {campos.map(c => (
        <label key={c.id} className={`ev-field${c.tipo === "area" ? " ev-wide" : ""}`}>
          <span>{c.label}{c.requerido && <em aria-hidden="true"> *</em>}</span>
          {c.tipo === "area"
            ? <textarea rows={2} value={reg.campos[c.id] ?? ""} placeholder={c.placeholder} onChange={e => set(c.id, e.target.value)} />
            : <input type={c.tipo === "fecha" ? "date" : "text"} list={c.tipo === "establecimiento" ? "ev-establecimientos" : undefined}
              value={reg.campos[c.id] ?? ""} placeholder={c.placeholder ?? (c.tipo === "establecimiento" ? "Seleccione o escriba…" : undefined)}
              required={c.requerido} onChange={e => set(c.id, e.target.value)} />}
        </label>
      ))}
      <datalist id="ev-establecimientos">{ESTABLECIMIENTOS.map(e => <option key={e} value={e} />)}</datalist>
    </div>
  );
}

function TablaEditor({ t, reg, upd }: { t: Tabla; reg: Registro; upd: Upd }) {
  const filas = reg.tablas[t.id] ?? [];
  return (
    <section className="ev-card">
      <h2>{t.titulo}</h2>
      {t.firma && <p className="ev-muted">Nombre, cargo e institución salen impresos; la columna «Firma» queda en blanco para firmar el PDF impreso.</p>}
      <div className="ev-table-wrap">
        <table className="ev-table ev-edit">
          <thead><tr>{t.columnas.map(c => <th key={c[0]}>{c[1]}</th>)}<th aria-label="Acciones" /></tr></thead>
          <tbody>
            {filas.map((f, i) => (
              <tr key={i}>
                {t.columnas.map(c => <td key={c[0]} data-label={c[1]}><input aria-label={c[1]} value={f[c[0]] ?? ""} onChange={e => upd(r => { r.tablas[t.id][i][c[0]] = e.target.value; })} /></td>)}
                <td><button type="button" className="ev-icon" aria-label="Quitar fila" onClick={() => upd(r => { r.tablas[t.id].splice(i, 1); })}><X size={15} /></button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="ev-actions">
        <Button type="button" variant="outline" size="sm" onClick={() => upd(r => { (r.tablas[t.id] ??= []).push({}); })}><Plus aria-hidden="true" />Agregar fila</Button>
        {t.copiarDe && <Button type="button" variant="ghost" size="sm" onClick={() => upd(r => { r.tablas[t.id] = structuredClone(r.tablas[t.copiarDe!] ?? []); })}>
          <Copy aria-hidden="true" />Copiar desde «{t.copiarDe === "prof" ? "profesionales" : t.copiarDe}»</Button>}
      </div>
    </section>
  );
}

/* ---------- Pauta ---------- */

function PautaView({ p, reg, upd, idx, setIdx, siguiente }: { p: Pauta; reg: Registro; upd: Upd; idx: number; setIdx: (i: number) => void; siguiente: () => void }) {
  const sec = p.secciones[idx], s = puntajeSeccion(p, sec, reg);
  let grupo = "";
  return (
    <>
      {(p.objetivo || p.ayuda) && (
        <section className="ev-card">
          {p.tituloImpreso && <h2 className="ev-sub">{p.tituloImpreso}</h2>}
          {p.objetivo && <label className="ev-field"><span>Objetivo</span>
            <textarea rows={2} value={reg.textos[`objetivo:${p.id}`] ?? ""} onChange={e => upd(r => { r.textos[`objetivo:${p.id}`] = e.target.value; })} /></label>}
          {p.ayuda && <p className="ev-muted">{p.ayuda}</p>}
        </section>
      )}
      {p.secciones.length > 1 && (
        <div className="ev-chips">
          {p.secciones.map((x, i) => {
            const ps = puntajeSeccion(p, x, reg);
            return <button key={x.id} type="button" className={i === idx ? "on" : ""} aria-pressed={i === idx} onClick={() => setIdx(i)}>
              <b>{x.corto ?? x.titulo}</b><small>{ps.respondidos}/{ps.total} · {fmtPct(ps.pct)}</small>
              <span className="ev-bar"><i style={{ width: `${ps.total ? (ps.respondidos / ps.total) * 100 : 0}%` }} /></span>
            </button>;
          })}
        </div>
      )}
      <section className="ev-card ev-sechead">
        <div>
          <h2>{sec.titulo}</h2>
          <p className="ev-muted">{sec.subtitulo ? `${sec.subtitulo} · ` : ""}{s.respondidos} de {s.total} respondidos</p>
          <span className="ev-bar"><i style={{ width: `${s.total ? (s.respondidos / s.total) * 100 : 0}%` }} /></span>
        </div>
        <div className="ev-score"><b>{fmtPts(s.obt)} / {fmtPts(s.max)}</b><small>{fmtPct(s.pct)}</small></div>
      </section>
      {sec.porCaso && <CasosEditor sec={sec} reg={reg} upd={upd} />}
      {sec.items.map(item => {
        const head = item.grupo && item.grupo !== grupo ? item.grupo : null;
        if (item.grupo) grupo = item.grupo;
        return <Fragment key={item.id}>{head && <h3 className="ev-grupo">{head}</h3>}<ItemCard p={p} sec={sec} item={item} reg={reg} upd={upd} /></Fragment>;
      })}
      <div className="ev-nav">
        <Button type="button" variant="outline" disabled={idx === 0} onClick={() => setIdx(idx - 1)}><ChevronLeft aria-hidden="true" />Anterior</Button>
        <Button type="button" onClick={() => (idx < p.secciones.length - 1 ? setIdx(idx + 1) : siguiente())}>Siguiente<ChevronRight aria-hidden="true" /></Button>
      </div>
    </>
  );
}

function CasosEditor({ sec, reg, upd }: { sec: Seccion; reg: Registro; upd: Upd }) {
  const casos = reg.listas[sec.id] ?? [];
  return (
    <section className="ev-card">
      <h3>{sec.porCaso}</h3>
      <p className="ev-muted">Use solo iniciales o un código; el Excel y el PDF quedan con lo que escriba aquí.</p>
      {casos.map((c, i) => (
        <div key={c.id} className="ev-caso">
          <span className="ev-badge">Caso {i + 1}</span>
          <input aria-label={`Identificación del caso ${i + 1}`} placeholder="Iniciales o código" value={c.nombre}
            onChange={e => upd(r => { r.listas[sec.id][i].nombre = e.target.value; })} />
          <button type="button" className="ev-icon" aria-label={`Quitar caso ${i + 1}`} onClick={() => {
            if (confirm(`¿Quitar el caso ${i + 1} y sus respuestas?`)) upd(r => {
              r.listas[sec.id].splice(i, 1);
              for (const it of sec.items) delete r.resp[`${it.id}|${c.id}`];
            });
          }}><X size={15} /></button>
        </div>
      ))}
      <Button type="button" variant="outline" size="sm" onClick={() => upd(r => { (r.listas[sec.id] ??= []).push({ id: uid(), nombre: "" }); })}><Plus aria-hidden="true" />Agregar caso</Button>
    </section>
  );
}

function Opciones({ ops, value, onChange, label }: { ops: Opcion[]; value: number | undefined; onChange: (v: number | undefined) => void; label: string }) {
  const max = maxPts(ops), lista = ops.some(o => o.label.length > 28);
  return (
    <div className={`ev-opts${lista ? " lista" : ""}`} role="radiogroup" aria-label={label}>
      {ops.map((o, i) => (
        <button key={i} type="button" role="radio" aria-checked={value === i} className={`${tonoDe(o, max)}${value === i ? " on" : ""}`}
          onClick={() => onChange(value === i ? undefined : i)}>
          <span>{o.label}</span>{lista && <small>{o.pts === null ? "No aplica" : `${fmtPts(o.pts)} pt`}</small>}
        </button>
      ))}
    </div>
  );
}

function ItemCard({ p, sec, item, reg, upd }: { p: Pauta; sec: Seccion; item: Item; reg: Registro; upd: Upd }) {
  const ops = opcionesDe(p, item), us = unidades(sec, reg).filter(u => u.item === item);
  const setResp = (key: string, v: number | undefined) => upd(r => { if (v === undefined) delete r.resp[key]; else r.resp[key] = v; });
  const fallida = item.obligatoria && us.some(u => opcionElegida(p, u, reg)?.pts === 0);
  const notas = reg.casos[item.id] ?? [];
  return (
    <article className={`ev-card ev-item${item.obligatoria ? " oblig" : ""}`}>
      <div className="ev-item-head">
        {item.n && <span className="ev-num">{item.n}</span>}
        <h3>{item.texto}{item.obligatoria && <span className="ev-tag">Obligatorio</span>}</h3>
      </div>
      {!!item.detalles?.length && (
        <details className="ev-detalles" open>
          <summary>Detalles y verificación</summary>
          <dl>{item.detalles.map(([k, v]) => <div key={k}><dt>{k}</dt><dd>{v}</dd></div>)}</dl>
        </details>
      )}
      {sec.porCaso
        ? us.length ? us.map(u => (
          <div key={u.key} className="ev-caso-resp">
            <span className="ev-caso-label">{u.caso!.nombre || `Caso ${u.caso!.n}`}</span>
            <Opciones ops={ops} value={reg.resp[u.key]} onChange={v => setResp(u.key, v)} label={`${item.texto} · caso ${u.caso!.n}`} />
          </div>
        )) : <p className="ev-muted">Agregue casos arriba para responder este criterio.</p>
        : <Opciones ops={ops} value={reg.resp[item.id]} onChange={v => setResp(item.id, v)} label={item.texto} />}
      {fallida && <p className="ev-alert" role="alert"><AlertTriangle size={16} aria-hidden="true" />Ítem obligatorio en 0 puntos: el establecimiento no certifica.</p>}
      <textarea className="ev-obs" rows={2} placeholder="Observación / hallazgo…" aria-label={`Observación: ${item.texto}`} value={reg.obs[item.id] ?? ""}
        onChange={e => upd(r => { r.obs[item.id] = e.target.value; })} />
      {!!item.detalleCasos && (
        <div className="ev-notas">
          <div className="ev-muted">Detalle de casos <small>(solo registro, no afecta el puntaje)</small> · {notas.length}/{item.detalleCasos}</div>
          {notas.map((t, i) => (
            <div key={i} className="ev-caso">
              <span className="ev-badge">Caso {i + 1}</span>
              <textarea rows={1} value={t} aria-label={`Comentario del caso ${i + 1}`} onChange={e => upd(r => { r.casos[item.id][i] = e.target.value; })} />
              <button type="button" className="ev-icon" aria-label={`Quitar caso ${i + 1}`} onClick={() => upd(r => { r.casos[item.id].splice(i, 1); })}><X size={15} /></button>
            </div>
          ))}
          {notas.length < item.detalleCasos && <Button type="button" variant="ghost" size="sm" onClick={() => upd(r => { (r.casos[item.id] ??= []).push(""); })}><Plus aria-hidden="true" />Agregar caso</Button>}
        </div>
      )}
    </article>
  );
}

/* ---------- Anexos ---------- */

function Anexos({ ev, reg, upd }: { ev: Evaluador; reg: Registro; upd: Upd }) {
  return <>{ev.anexos!.map((a, ai) => {
    const hechos = a.items.filter((_, ii) => reg.anexos[`${ai}-${ii}`] !== undefined).length;
    return (
      <details key={ai} className="ev-card ev-anexo" open={ai === 0}>
        <summary><b>{a.titulo}</b><small>{hechos} de {a.items.length} registrados</small></summary>
        <div className="ev-table-wrap">
          <table className="ev-table ev-edit">
            <thead><tr><th>Elemento</th><th>Disponibilidad</th><th>Observación</th></tr></thead>
            <tbody>{a.items.map((it, ii) => {
              const k = `${ai}-${ii}`;
              return <tr key={k}>
                <td>{it}</td>
                <td><select aria-label={`Disponibilidad: ${it}`} value={reg.anexos[k] ?? ""} onChange={e => upd(r => { if (e.target.value === "") delete r.anexos[k]; else r.anexos[k] = Number(e.target.value); })}>
                  <option value="">Sin registrar</option>{a.opciones.map((o, i) => <option key={o} value={i}>{o}</option>)}
                </select></td>
                <td><input aria-label={`Observación: ${it}`} value={reg.anexosObs[k] ?? ""} onChange={e => upd(r => { r.anexosObs[k] = e.target.value; })} /></td>
              </tr>;
            })}</tbody>
          </table>
        </div>
      </details>
    );
  })}</>;
}

/* ---------- Resultados ---------- */

function TablaResultado({ r }: { r: ResultadoPauta }) {
  return (
    <div className="ev-table-wrap">
      <table className="ev-table">
        <thead><tr><th>Aspecto evaluado</th><th className="n">Puntaje que aplica</th><th className="n">Puntaje obtenido</th><th>% de cumplimiento</th></tr></thead>
        <tbody>
          {r.secciones.map(s => <tr key={s.sec.id}><td>{s.sec.titulo}</td><td className="n">{fmtPts(s.max)}</td><td className="n">{fmtPts(s.obt)}</td>
            <td><span className="ev-pct"><span className="ev-bar"><i style={{ width: `${s.pct ?? 0}%` }} /></span>{fmtPct(s.pct)}</span></td></tr>)}
          <tr className="tot"><td>Total</td><td className="n">{fmtPts(r.max)}</td><td className="n">{fmtPts(r.obt)}</td>
            <td><span className="ev-pct"><span className="ev-bar"><i style={{ width: `${r.pct ?? 0}%` }} /></span>{fmtPct(r.pct)}</span></td></tr>
        </tbody>
      </table>
    </div>
  );
}

function Veredicto({ p, r }: { p: Pauta; r: ResultadoPauta }) {
  if (r.nivel) return <div className={`ev-veredicto ${r.nivel.tono}`}><b>Nivel de logro: {r.nivel.label}</b><span>{fmtPts(r.obt)} de {fmtPts(r.max)} puntos · {fmtPct(r.pct)} · {r.total - r.respondidos ? `faltan ${r.total - r.respondidos} por responder` : "todo respondido"}</span></div>;
  if (!r.certificacion) return null;
  const c = r.certificacion, Check = ({ ok, children }: { ok: boolean; children: React.ReactNode }) =>
    <li className={ok ? "ok" : "bad"}>{ok ? <CheckCircle2 size={15} aria-hidden="true" /> : <XCircle size={15} aria-hidden="true" />}{children}</li>;
  return (
    <div className={`ev-veredicto ${c.ok ? "ok" : "bad"}`}>
      <b>{c.ok ? "El establecimiento cumple las condiciones de certificación" : "El establecimiento no certifica"}</b>
      <ul>
        <Check ok={c.completo}>Todo lo evaluable respondido ({r.respondidos}/{r.total})</Check>
        <Check ok={c.umbral}>Cumplimiento de {p.certifica!.minPct}% o más ({fmtPct(r.pct)})</Check>
        <Check ok={c.obligatorias}>Ningún ítem obligatorio en 0 puntos{r.fallidas.length ? ` (${r.fallidas.length} en 0)` : ""}</Check>
      </ul>
    </div>
  );
}

function Resultados({ ev, reg, upd }: { ev: Evaluador; reg: Registro; upd: Upd }) {
  const w = useMemo(() => avisos(ev, reg), [ev, reg]);
  return <>
    {ev.pautas.map(p => {
      const r = puntajePauta(p, reg);
      return (
        <section key={p.id} className="ev-card">
          <h2>Resultados · {p.titulo}</h2>
          <Veredicto p={p} r={r} />
          <TablaResultado r={r} />
          {p.comentario && <label className="ev-field"><span>Comentarios</span>
            <textarea rows={3} value={reg.textos[`comentario:${p.id}`] ?? ""} onChange={e => upd(x => { x.textos[`comentario:${p.id}`] = e.target.value; })} /></label>}
        </section>
      );
    })}
    <section className="ev-card">
      <h2>Revisión antes de exportar</h2>
      {w.length ? <ul className="ev-avisos">{w.map(x => <li key={x}><AlertTriangle size={15} aria-hidden="true" />{x}</li>)}</ul>
        : <p className="ev-muted"><CheckCircle2 size={15} className="inline" aria-hidden="true" /> Sin observaciones: todo está completo.</p>}
    </section>
    {(ev.tablas ?? []).filter(t => t.firma).map(t => <TablaEditor key={t.id} t={t} reg={reg} upd={upd} />)}
  </>;
}

/* ---------- Documento para imprimir / guardar como PDF ---------- */

function Impresion({ ev, reg }: { ev: Evaluador; reg: Registro }) {
  const personas = (t: Tabla) => {
    const filas = (reg.tablas[t.id] ?? []).filter(f => t.columnas.some(c => f[c[0]]?.trim()));
    while (t.firma && filas.length < 5) filas.push({});
    return (
      <Fragment key={t.id}>
        <h3>{t.titulo}</h3>
        <table><thead><tr>{t.columnas.map(c => <th key={c[0]}>{c[1]}</th>)}{t.firma && <th>Firma</th>}</tr></thead>
          <tbody>{(filas.length ? filas : [{}]).map((f, i) => <tr key={i} className={t.firma ? "firma" : ""}>{t.columnas.map(c => <td key={c[0]}>{f[c[0]]}</td>)}{t.firma && <td />}</tr>)}</tbody></table>
      </Fragment>
    );
  };
  return (
    <div className="ev-print" aria-hidden="true">
      {ev.pautas.map((p, pi) => {
        const r = puntajePauta(p, reg), etiquetas = p.imprimirDetalles ? [...new Set(p.secciones.flatMap(s => s.items.flatMap(i => (i.detalles ?? []).map(d => d[0]))))] : [];
        return (
          <section key={p.id} className={pi ? "salto" : ""}>
            <h1>{p.tituloImpreso ?? `${ev.titulo} · ${p.titulo}`}</h1>
            <table className="kv"><tbody>
              {ev.campos.map(c => <tr key={c.id}><th>{c.label}</th><td>{c.tipo === "fecha" ? reg.campos[c.id]?.split("-").reverse().join("-") : reg.campos[c.id]}</td></tr>)}
              {p.objetivo && <tr><th>Objetivo</th><td>{reg.textos[`objetivo:${p.id}`]}</td></tr>}
            </tbody></table>
            {(ev.tablas ?? []).filter(t => !t.firma).map(personas)}
            <h3>Desarrollo</h3>
            {p.secciones.map(s => {
              const casos = reg.listas[s.id] ?? [];
              if (s.porCaso) return (
                <table key={s.id}>
                  <thead><tr><th colSpan={2 + etiquetas.length + casos.length} className="sec">{s.titulo}</th></tr>
                    <tr><th>Criterio</th>{etiquetas.map(e => <th key={e}>{e}</th>)}{casos.map((c, i) => <th key={c.id}>Caso {i + 1}{c.nombre && <><br />{c.nombre}</>}</th>)}<th>Observación</th></tr></thead>
                  <tbody>{s.items.map(it => <tr key={it.id}><td>{it.n}. {it.texto}</td>{etiquetas.map(e => <td key={e}>{it.detalles?.find(d => d[0] === e)?.[1]}</td>)}
                    {casos.map(c => { const o = opcionElegida(p, { item: it, key: `${it.id}|${c.id}` }, reg); return <td key={c.id} className="c">{o ? (o.pts ?? o.label) : ""}</td>; })}
                    <td>{reg.obs[it.id]}</td></tr>)}</tbody>
                </table>
              );
              let grupo = "";
              return (
                <table key={s.id}>
                  <thead><tr><th colSpan={5 + etiquetas.length} className="sec">{s.titulo}</th></tr>
                    <tr><th>N°</th><th>Ítem</th>{etiquetas.map(e => <th key={e}>{e}</th>)}<th>Respuesta</th><th>Pts</th><th>Observación</th></tr></thead>
                  <tbody>{s.items.map(it => {
                    const o = opcionElegida(p, { item: it, key: it.id }, reg), head = it.grupo && it.grupo !== grupo;
                    if (it.grupo) grupo = it.grupo;
                    const notas = (reg.casos[it.id] ?? []).filter(t => t.trim());
                    return <Fragment key={it.id}>
                      {head && <tr><td colSpan={5 + etiquetas.length} className="grupo">{it.grupo}</td></tr>}
                      <tr><td className="c">{it.n}</td><td>{it.texto}{it.obligatoria && " (obligatorio)"}</td>{etiquetas.map(e => <td key={e}>{it.detalles?.find(d => d[0] === e)?.[1]}</td>)}
                        <td>{o?.label ?? "Sin responder"}</td><td className="c">{o?.pts ?? "—"}</td>
                        <td>{reg.obs[it.id]}{notas.map((t, i) => <div key={i}><b>Caso {i + 1}:</b> {t}</div>)}</td></tr>
                    </Fragment>;
                  })}</tbody>
                </table>
              );
            })}
            <h3>Resultados</h3>
            <table><thead><tr><th>Aspecto evaluado</th><th>Puntaje que aplica</th><th>Puntaje obtenido</th><th>% de cumplimiento</th></tr></thead>
              <tbody>{r.secciones.map(s => <tr key={s.sec.id}><td>{s.sec.titulo}</td><td className="c">{fmtPts(s.max)}</td><td className="c">{fmtPts(s.obt)}</td><td className="c">{fmtPct(s.pct)}</td></tr>)}
                <tr className="tot"><td>Total</td><td className="c">{fmtPts(r.max)}</td><td className="c">{fmtPts(r.obt)}</td><td className="c">{fmtPct(r.pct)}</td></tr></tbody></table>
            {r.nivel && <p><b>Nivel de logro:</b> {r.nivel.label}</p>}
            {r.certificacion && <p><b>{r.certificacion.ok ? "El establecimiento cumple las condiciones de certificación." : "El establecimiento no certifica."}</b></p>}
            {p.comentario && <><h3>Comentarios</h3><div className="box">{reg.textos[`comentario:${p.id}`]}</div></>}
            {pi === ev.pautas.length - 1 && ev.anexos?.map((a, ai) => (
              <Fragment key={ai}><h3>{a.titulo}</h3>
                <table><thead><tr><th>Elemento</th><th>Disponibilidad</th><th>Observación</th></tr></thead>
                  <tbody>{a.items.map((it, ii) => { const v = reg.anexos[`${ai}-${ii}`]; return <tr key={ii}><td>{it}</td><td>{v === undefined ? "Sin registrar" : a.opciones[v]}</td><td>{reg.anexosObs[`${ai}-${ii}`]}</td></tr>; })}</tbody></table>
              </Fragment>
            ))}
            {(ev.tablas ?? []).filter(t => t.firma).map(personas)}
          </section>
        );
      })}
    </div>
  );
}
