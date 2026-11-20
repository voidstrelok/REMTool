import type { Anexo, Evaluador, Seccion } from "../tipos";
import { ESTABLECIMIENTO, FECHA, fecha, lista } from "./comun";
import data from "./cpu-2026.json";

const RESP: Record<string, number> = { "1": 0, "0": 1, na: 2 };
const ANEXO: Record<string, number> = { yes: 0, no: 1, na: 2 };
const mapa = (o: unknown, m: Record<string, number>) =>
  Object.fromEntries(Object.entries((o ?? {}) as Record<string, unknown>).filter(([, v]) => String(v) in m).map(([k, v]) => [k, m[String(v)]]));

const cpu: Evaluador = {
  id: "cpu-2026",
  titulo: "Evaluador CPU 2026",
  descripcion: "Pauta de evaluación clínica del Programa de Cuidados Paliativos Universales, con anexos de verificación.",
  campos: [ESTABLECIMIENTO, { id: "evaluador", label: "Evaluador/a", placeholder: "Nombre y cargo" }, FECHA],
  pautas: [{
    id: "cpu",
    titulo: "Pauta de evaluación clínica CPU",
    ayuda: "Cada ítem cumplido suma 1 punto sobre 25. «No aplica» no suma, pero el máximo se mantiene en 25.",
    // NA vale 0 y cuenta en el máximo: la pauta siempre se mide sobre 25 puntos.
    opciones: [{ label: "Cumple", pts: 1 }, { label: "No cumple", pts: 0 }, { label: "No aplica", pts: 0, tono: "neu" }],
    base: "evaluables",
    niveles: [
      { minPct: 96, label: "Excelente · 24 a 25 puntos", tono: "ok" },
      { minPct: 88, label: "Bueno · 22 a 23 puntos", tono: "partial" },
      { minPct: 0, label: "Deficiente · menos de 22 puntos", tono: "bad" },
    ],
    secciones: data.secciones as Seccion[],
  }],
  anexos: data.anexos as Anexo[],
  // Respaldo del HTML original: [{ id, establishment, evaluator, date, answers, notes, annex, annexNotes, updated }]
  importarLegado: d => lista(d)?.filter(x => "annex" in x).map(x => ({
    id: String(x.id ?? ""), actualizado: fecha(x.updated),
    campos: { establecimiento: String(x.establishment ?? ""), evaluador: String(x.evaluator ?? ""), fecha: String(x.date ?? "") },
    resp: mapa(x.answers, RESP), obs: (x.notes ?? {}) as Record<string, string>,
    anexos: mapa(x.annex, ANEXO), anexosObs: (x.annexNotes ?? {}) as Record<string, string>,
  })) ?? null,
};
export default cpu;
