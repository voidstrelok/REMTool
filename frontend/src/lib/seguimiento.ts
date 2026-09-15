export const estados = {
  Cumplida: "Cumplida", EnCurso: "En curso", EnRiesgo: "En riesgo", Critico: "Crítico",
  SinDatos: "Sin datos", SinDenominador: "Sin denominador", SinMeta: "Sin meta",
} as const;
export type Estado = keyof typeof estados;
export interface Evaluacion {
  mesCorte: number; mesEvaluacion: number; ultimoMesDatos: number | null;
  provisional: boolean; tieneDatos: boolean; tieneRegistroPeriodo: boolean;
  numerador: number; denominador: number; meta: number; resultado: number | null;
  cumplimiento: number | null; esperado: number; brecha: number | null; estado: Estado; unidad: string;
}
export interface Registro {
  mes: number; numerador: number; denominador: number; numeradorP: number; denominadorP: number;
  establecimientoNombre: string; sectorId: number; establecimientoId: number;
}
export interface EstablecimientoDetalle {
  id: number; nombre: string; sector: string; evaluacion: Evaluacion; meses: Registro[];
}
export interface Indicador {
  criterios: string;
  id: number; nombre: string; año: number; orden: number; tipoindicador: number;
  mensual: boolean; isTasa: boolean; isColaborativo: boolean; isDenFijo: boolean; esPeriodoOctubreSep: boolean;
  detalle: string | null; peso: number; ajuste: number; aporte: number; avance: number;
  numerador: number; denominador: number; meta: number; actual: number;
  evaluacion: Evaluacion; evolucion: Evaluacion[]; establecimientos: EstablecimientoDetalle[];
  resultados: Registro[]; analisis: string[];
}
export interface Resumen {
  cumplimiento: number; metodo: string; total: number; cumplidas: number; enCurso: number;
  enRiesgo: number; criticos: number; sinDatos: number; sinDenominador: number; sinMeta: number;
}
export interface Convenio {
  criterios: string;
  id: number; nombre: string; año: number; mesCorte: number; avance: number; indicadorCount: number;
  indicadores: Indicador[]; resumen: Resumen;
}
export interface ListaIndicadores { items: Indicador[]; resumen: Resumen; mesCorte: number | null; criterios: string }
export interface ListaConvenios { items: Convenio[]; cumplimiento: number; mesCorte: number | null; criterios: string }
export const meses = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];
export const mesNombre = (mes: number | null | undefined) => mes ? meses[mes - 1] ?? "—" : "Sin registros";
export const numero = (value: number | null | undefined, decimals = 1) => value == null || !Number.isFinite(value) ? "—"
  : new Intl.NumberFormat("es-CL", { minimumFractionDigits: decimals, maximumFractionDigits: decimals }).format(value);
export const valor = (value: number | null | undefined, tasa = false) => numero(value) + (value != null && !tasa ? " %" : "");
export const brecha = (value: number | null, tasa = false) => value == null ? "—" : `${value > 0 ? "+" : ""}${numero(value)} ${tasa ? "u." : "pp"}`;

export function queryContexto(params: URLSearchParams, corte?: number | null) {
  const query = new URLSearchParams();
  for (const key of ["sectorId", "establecimientoId", "mesCorte"])
    if (params.get(key) && params.get(key) !== "0") query.set(key, params.get(key)!);
  if (corte) query.set("mesCorte", String(corte));
  return query;
}
export function retornoSeguro(value: string | null, fallback: string) {
  if (!value) return fallback;
  let decoded = value;
  // Accept old double-encoded links as well as the canonical single encoding.
  try { if (decoded.startsWith("%2F")) decoded = decodeURIComponent(decoded); } catch { return fallback; }
  return /^\/(Indicadores|Convenios)(?:\/|\?|$)/.test(decoded) ? decoded : fallback;
}
export function seleccionarIndicadores(items: Indicador[], buscar: string, estado: string, orden: string) {
  const normalizar = (s: string) => s.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("es");
  return items.filter(i => normalizar(i.nombre).includes(normalizar(buscar)) && (!estado || i.evaluacion.estado === estado))
    .sort((a, b) => {
      if (orden === "brecha" || orden === "cumplimiento") {
        // Brechas of different units are not comparable; keep percentages and rates in separate groups.
        if (orden === "brecha" && a.isTasa !== b.isTasa) return Number(a.isTasa) - Number(b.isTasa);
        const av = a.evaluacion[orden], bv = b.evaluacion[orden];
        if (av == null) return bv == null ? a.orden - b.orden : 1;
        if (bv == null) return -1;
        return av - bv || a.orden - b.orden;
      }
      return a.orden - b.orden || a.id - b.id;
    });
}
