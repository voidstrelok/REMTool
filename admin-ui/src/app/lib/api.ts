export type Rule = { id: number; id_version: number; idTipoRegla: number; expresion: string; mensaje: string; version: string; serie: string; tipo: string };
export type Catalog = { series: { id: number; nombre: string }[]; versions: { id: number; nombre: string; fecha: string; serie: string }[]; ruleTypes: { id: number; nombre: string }[]; indicatorTypes: { id: number; nombre: string }[]; establishments: { id: number; nombre: string; codDeis: string }[]; indicators: { id: number; nombre: string; [key: string]: unknown }[] };
export type Run = (action: () => Promise<void>, success: string) => Promise<void>;

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/admin-api${path}`, init);
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.error ?? "No se pudo completar la operación.");
  return body as T;
}
