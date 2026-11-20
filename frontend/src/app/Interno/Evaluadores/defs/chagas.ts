import type { Evaluador, Registro, Seccion } from "../tipos";
import { ESTABLECIMIENTO, fecha, lista } from "./comun";
import data from "./chagas.json";

const PERSONAS: [string, string][] = [["nombre", "Nombre"], ["cargo", "Cargo"], ["correo", "Correo electrónico"], ["fono", "Fono contacto"]];
const A: Record<string, number> = { "1": 0, "0": 1, NA: 2, NE: 3 };
const B: Record<string, number> = { "1": 0, "0": 1, NA: 2 };

type Legado = {
  id: string; updated?: number; gen: Record<string, string>; estab?: { nombre?: string };
  prof?: Record<string, string>[]; part?: Record<string, string>[]; firmas?: Record<string, string>[];
  A: Record<string, { v?: string; obs?: string; casos?: { txt?: string }[] }>;
  B?: { gest?: { id: string; nombre: string }[]; rn?: { id: string; nombre: string }[]; ans?: Record<string, string>; obs?: Record<string, string> };
  comA?: string; comB?: string;
};

function desdeLegado(ev: Legado): Partial<Registro> {
  const r: Partial<Registro> & Pick<Registro, "resp" | "obs" | "casos"> = {
    id: ev.id, actualizado: fecha(ev.updated),
    campos: { establecimiento: ev.estab?.nombre ?? "", fecha: ev.gen.fecha ?? "", periodo: ev.gen.periodo ?? "", responsable: ev.gen.responsable ?? "" },
    tablas: { prof: ev.prof ?? [{}], part: ev.part ?? [{}], firmas: ev.firmas ?? [{}] },
    listas: { gest: ev.B?.gest ?? [], rn: ev.B?.rn ?? [] },
    textos: { "objetivo:A": ev.gen.objA ?? data.A.objetivo, "objetivo:B": ev.gen.objB ?? data.B.objetivo, "comentario:A": ev.comA ?? "", "comentario:B": ev.comB ?? "" },
    resp: {}, obs: { ...ev.B?.obs }, casos: {},
  };
  for (const [n, a] of Object.entries(ev.A)) {
    if (a.v && a.v in A) r.resp[`A${n}`] = A[a.v];
    if (a.obs) r.obs[`A${n}`] = a.obs;
    if (a.casos?.length) r.casos[`A${n}`] = a.casos.map(c => c.txt ?? "");
  }
  for (const [k, v] of Object.entries(ev.B?.ans ?? {})) if (v in B) r.resp[k] = B[v];
  return r;
}

const chagas: Evaluador = {
  id: "chagas",
  titulo: "Evaluador Programa de Chagas",
  descripcion: "Fiscalización de la regulación de Chagas en APS (Pauta A) y del proceso gestante y RN/lactante (Pauta B).",
  campos: [
    ESTABLECIMIENTO,
    { id: "fecha", label: "Fecha de fiscalización", tipo: "fecha", requerido: true },
    { id: "periodo", label: "Periodo a evaluar", placeholder: "Ej.: Año 2023" },
    { id: "responsable", label: "Responsable de fiscalización", tipo: "area", placeholder: "Nombre, correo, anexo, fono y cargo" },
  ],
  tablas: [
    { id: "prof", titulo: "Profesionales asociados al Programa de Chagas", columnas: PERSONAS },
    { id: "part", titulo: "Participantes de visita representantes del establecimiento", columnas: PERSONAS, copiarDe: "prof" },
    { id: "firmas", titulo: "Registro de firmas", columnas: [["nombre", "Nombre"], ["cargo", "Cargo"], ["inst", "Institución"]], firma: true },
  ],
  pautas: [
    {
      id: "A", titulo: "Pauta A", tituloImpreso: data.A.tituloImpreso, objetivo: data.A.objetivo,
      ayuda: "«Cumple» (1) y «No cumple» (0) entran al puntaje; «NA» y «No evaluado» no. % = obtenido ÷ puntaje que aplica.",
      opciones: [{ label: "Cumple · 1", pts: 1 }, { label: "No cumple · 0", pts: 0 }, { label: "NA", pts: null }, { label: "No evaluado", pts: null }],
      base: "respondidos", comentario: true, imprimirDetalles: true,
      secciones: data.A.secciones as Seccion[],
    },
    {
      id: "B", titulo: "Pauta B", tituloImpreso: data.B.tituloImpreso, objetivo: data.B.objetivo,
      ayuda: "Cada criterio se responde por caso y cada respuesta es un punto. Identifique los casos solo con iniciales o un código.",
      opciones: [{ label: "Sí · 1", pts: 1 }, { label: "No · 0", pts: 0 }, { label: "NA", pts: null }],
      base: "respondidos", comentario: true, imprimirDetalles: true,
      secciones: data.B.secciones as Seccion[],
    },
  ],
  // Respaldo del HTML original: una evaluación { id, gen, estab, A, B, ... } o el almacenamiento { items: {...} }.
  importarLegado: d => {
    const l = (d as Legado)?.A ? [d as Legado] : (lista(d) as unknown as Legado[] | null);
    return l?.filter(x => x?.A && x.gen).map(desdeLegado) ?? null;
  },
};
export default chagas;
