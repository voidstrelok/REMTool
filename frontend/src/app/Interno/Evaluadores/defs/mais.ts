import type { Evaluador, Seccion } from "../tipos";
import { ESTABLECIMIENTO, FECHA, fecha, lista } from "./comun";
import data from "./mais.json";

const secciones = data.secciones as Seccion[];
const opciones = new Map(secciones.flatMap(s => s.items.map(i => [i.id, i.opciones ?? []] as const)));

const mais: Evaluador = {
  id: "mais",
  titulo: "Evaluador MAIS",
  descripcion: "Modelo de Atención Integral de Salud Familiar y Comunitaria: puntaje por formulario y certificación.",
  campos: [ESTABLECIMIENTO, { id: "evaluador", label: "Evaluador/a (opcional)" }, FECHA],
  pautas: [{
    id: "mais",
    titulo: "Modelo de Atención Integral de Salud Familiar y Comunitaria",
    ayuda: "Certifica con 100% de lo evaluable respondido, cumplimiento de 65% o más y ningún ítem obligatorio en 0 puntos. «No aplica» se descuenta del máximo.",
    opciones: [], // cada ítem trae sus niveles de cumplimiento
    base: "evaluables",
    certifica: { minPct: 65 },
    secciones,
  }],
  // Respaldo del HTML original: { records: [{ id, establishment, establishmentLabel, evaluator, date, answers, observations }] }
  // answers guarda el puntaje elegido (número) o "na-<índice de opción>".
  importarLegado: d => lista(d)?.filter(x => "observations" in x).map(x => ({
    id: String(x.id ?? ""), actualizado: fecha(x.updatedAt),
    campos: { establecimiento: String(x.establishmentLabel ?? x.establishment ?? ""), evaluador: String(x.evaluator ?? ""), fecha: String(x.date ?? "") },
    resp: Object.fromEntries(Object.entries((x.answers ?? {}) as Record<string, unknown>).map(([k, v]) =>
      [k, typeof v === "number" ? (opciones.get(k) ?? []).findIndex(o => o.pts === v) : Number(String(v).replace("na-", ""))] as const)
      .filter(([, i]) => i >= 0)),
    obs: (x.observations ?? {}) as Record<string, string>,
  })) ?? null,
};
export default mais;
