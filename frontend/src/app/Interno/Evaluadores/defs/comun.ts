import type { Campo } from "../tipos";

// Sugerencias del campo establecimiento (se puede escribir otro).
export const ESTABLECIMIENTOS = [
  "Centro de Salud Familiar Monte Patria", "Centro de Salud Familiar Carén", "Centro de Salud Familiar Chañaral Alto",
  "Centro de Salud Familiar El Palqui", "SAR Monte Patria", "SUR El Palqui", "Clínica Dental Móvil Monte Patria",
  "Posta de Salud Rural Chilecito", "Posta de Salud Rural El Maitén", "Posta de Salud Rural Hacienda Valdivia",
  "Posta de Salud Rural Huatulame", "Posta de Salud Rural Mialqui", "Posta de Salud Rural Pedregal",
  "Posta de Salud Rural Ramadas de Tulahuén", "Posta de Salud Rural Rapel", "Posta de Salud Rural Tulahuén",
];

export const ESTABLECIMIENTO: Campo = { id: "establecimiento", label: "Establecimiento", tipo: "establecimiento", requerido: true };
export const FECHA: Campo = { id: "fecha", label: "Fecha de evaluación", tipo: "fecha", requerido: true };

/** Respaldos antiguos: un arreglo, { records: [...] } o { items: {...} }. */
export const lista = (d: unknown): Record<string, unknown>[] | null => {
  const o = d as { records?: unknown; items?: unknown };
  const l = Array.isArray(d) ? d : Array.isArray(o?.records) ? o.records : o?.items && typeof o.items === "object" ? Object.values(o.items) : null;
  return l?.filter(x => x && typeof x === "object") as Record<string, unknown>[] | null;
};

export const fecha = (v: unknown) => (typeof v === "number" ? v : Date.parse(String(v ?? "")) || Date.now());
