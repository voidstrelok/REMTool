import type { Evaluador, Item, Nivel, Opcion, Pauta, Registro, Seccion, Tono } from "./tipos";

export type Puntaje = { max: number; obt: number; pct: number | null; respondidos: number; total: number };
export type Unidad = { item: Item; key: string; caso?: { id: string; nombre: string; n: number } };

export const opcionesDe = (p: Pauta, item: Item): Opcion[] => item.opciones ?? p.opciones;
export const maxPts = (ops: Opcion[]) => Math.max(0, ...ops.map(o => o.pts ?? 0));
export const tonoDe = (o: Opcion, max: number): Tono =>
  o.tono ?? (o.pts === null ? "neu" : o.pts === 0 ? "bad" : o.pts >= max ? "ok" : "partial");
/** Redondeo a un decimal; null si no hay puntaje que aplique. */
export const pct = (obt: number, max: number) => (max > 0 ? Math.round((obt / max) * 1000) / 10 : null);
export const fmtPct = (p: number | null) => (p === null ? "—" : `${p.toLocaleString("es-CL", { maximumFractionDigits: 1 })}%`);
export const fmtPts = (n: number) => n.toLocaleString("es-CL", { maximumFractionDigits: 2 });

/** Cada respuesta que entra al puntaje: un ítem, o un ítem por caso en secciones porCaso. */
export function unidades(sec: Seccion, reg: Registro): Unidad[] {
  if (!sec.porCaso) return sec.items.map(item => ({ item, key: item.id }));
  const casos = reg.listas[sec.id] ?? [];
  return sec.items.flatMap(item => casos.map((c, i) => ({ item, key: `${item.id}|${c.id}`, caso: { ...c, n: i + 1 } })));
}

export const opcionElegida = (p: Pauta, u: Unidad, reg: Registro): Opcion | undefined =>
  reg.resp[u.key] === undefined ? undefined : opcionesDe(p, u.item)[reg.resp[u.key]];

export function puntajeSeccion(p: Pauta, sec: Seccion, reg: Registro): Puntaje {
  let max = 0, obt = 0, respondidos = 0;
  const us = unidades(sec, reg);
  for (const u of us) {
    const o = opcionElegida(p, u, reg);
    if (o) respondidos++;
    if (o ? o.pts === null : p.base === "respondidos") continue;
    max += maxPts(opcionesDe(p, u.item));
    obt += o?.pts ?? 0;
  }
  return { max, obt, pct: pct(obt, max), respondidos, total: us.length };
}

export type ResultadoPauta = Puntaje & {
  secciones: (Puntaje & { sec: Seccion })[];
  fallidas: Item[];
  nivel?: Nivel;
  certificacion?: { ok: boolean; completo: boolean; umbral: boolean; obligatorias: boolean };
};

export function puntajePauta(p: Pauta, reg: Registro): ResultadoPauta {
  const secciones = p.secciones.map(sec => ({ sec, ...puntajeSeccion(p, sec, reg) }));
  const sum = (k: "max" | "obt" | "respondidos" | "total") => secciones.reduce((s, x) => s + x[k], 0);
  const max = sum("max"), obt = sum("obt"), total = pct(obt, max);
  const fallidas = p.secciones.flatMap(sec => unidades(sec, reg))
    .filter(u => u.item.obligatoria && opcionElegida(p, u, reg)?.pts === 0).map(u => u.item);
  const r: ResultadoPauta = { secciones, max, obt, pct: total, respondidos: sum("respondidos"), total: sum("total"), fallidas };
  if (p.niveles) r.nivel = p.niveles.find(n => (total ?? 0) >= n.minPct) ?? p.niveles[p.niveles.length - 1];
  if (p.certifica) {
    // Completo = todo lo que no es NA está respondido.
    const pendientes = p.secciones.flatMap(sec => unidades(sec, reg)).filter(u => !opcionElegida(p, u, reg)).length;
    const c = { completo: pendientes === 0, umbral: (total ?? 0) >= p.certifica.minPct, obligatorias: fallidas.length === 0 };
    r.certificacion = { ...c, ok: c.completo && c.umbral && c.obligatorias };
  }
  return r;
}

/** Revisión antes de exportar. */
export function avisos(ev: Evaluador, reg: Registro): string[] {
  const w: string[] = [];
  for (const c of ev.campos) if (c.requerido && !reg.campos[c.id]?.trim()) w.push(`Falta ${c.label.toLowerCase()}.`);
  for (const p of ev.pautas) {
    const r = puntajePauta(p, reg);
    const pre = ev.pautas.length > 1 ? `${p.titulo}: ` : "";
    if (r.total > r.respondidos) w.push(`${pre}${r.total - r.respondidos} respuesta(s) sin completar.`);
    const ceros = new Set(p.secciones.flatMap(s => unidades(s, reg))
      .filter(u => { const o = opcionElegida(p, u, reg); return o?.pts === 0 && o.tono !== "neu"; }).map(u => u.item.id));
    const sinObs = [...ceros].filter(id => !reg.obs[id]?.trim()).length;
    if (sinObs) w.push(`${pre}${sinObs} ítem(s) en 0 puntos sin observación.`);
    for (const item of new Set(r.fallidas)) w.push(`${pre}obligatorio en 0 puntos, no certifica: «${item.texto.length > 70 ? item.texto.slice(0, 70) + "…" : item.texto}».`);
    for (const s of r.secciones) {
      if (s.sec.porCaso && !(reg.listas[s.sec.id]?.length)) w.push(`${pre}faltan ${s.sec.porCaso.toLowerCase()}.`);
      else if (p.base === "respondidos" && s.total && s.respondidos === s.total && s.max === 0) w.push(`${pre}${s.sec.corto ?? s.sec.titulo}: todo es NA, no hay puntaje que aplique.`);
    }
  }
  return w;
}

export const uid = () => globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
const hoy = () => new Date().toLocaleDateString("sv-SE");

export function nuevoRegistro(ev: Evaluador): Registro {
  const r: Registro = { id: uid(), actualizado: Date.now(), campos: {}, tablas: {}, resp: {}, obs: {}, casos: {}, listas: {}, anexos: {}, anexosObs: {}, textos: {} };
  for (const c of ev.campos) if (c.tipo === "fecha") r.campos[c.id] = hoy();
  for (const t of ev.tablas ?? []) r.tablas[t.id] = [{}];
  for (const p of ev.pautas) {
    if (p.objetivo) r.textos[`objetivo:${p.id}`] = p.objetivo;
    for (const s of p.secciones) if (s.porCaso) r.listas[s.id] = [{ id: uid(), nombre: "" }];
  }
  return r;
}

/** Completa campos faltantes (respaldos parciales o de versiones anteriores). */
export const normalizar = (ev: Evaluador, raw: Partial<Registro>): Registro => {
  const base = nuevoRegistro(ev);
  // El respaldo viene de un archivo: todo lo que no sea un objeto se descarta.
  const o = <T,>(v: T | undefined) => (v && typeof v === "object" && !Array.isArray(v) ? v : {}) as T;
  return {
    id: typeof raw.id === "string" && raw.id ? raw.id : base.id,
    actualizado: typeof raw.actualizado === "number" ? raw.actualizado : Date.now(),
    campos: o(raw.campos), resp: o(raw.resp), obs: o(raw.obs), casos: o(raw.casos), anexos: o(raw.anexos), anexosObs: o(raw.anexosObs),
    tablas: { ...base.tablas, ...o(raw.tablas) }, listas: { ...base.listas, ...o(raw.listas) }, textos: { ...base.textos, ...o(raw.textos) },
  };
};

export const tituloRegistro = (r: Registro) =>
  `${r.campos.establecimiento?.trim() || "Sin establecimiento"} · ${r.campos.fecha?.split("-").reverse().join("-") || "sin fecha"}`;
