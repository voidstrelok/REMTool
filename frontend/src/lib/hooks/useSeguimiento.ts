"use client";
import { useEffect, useState, useCallback } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { apiUrl } from "../api";
import { queryContexto } from "../seguimiento";

export function useRecurso<T>(path: string | null) {
  const [intento, setIntento] = useState(0);
  const key = `${path}:${intento}`;
  const [state, setState] = useState<{ key: string; data: T | null; error: string | null }>({ key: "", data: null, error: null });
  useEffect(() => {
    if (!path) return;
    const abort = new AbortController();
    fetch(apiUrl(path), { signal: abort.signal }).then(async response => {
      if (!response.ok) throw new Error(response.status === 404 ? "No se encontró información para esta consulta." : "No se pudo cargar la información. Intenta nuevamente.");
      return response.json() as Promise<T>;
    }).then(data => { if (!abort.signal.aborted) setState({ key, data, error: null }); })
      .catch((error: Error) => { if (!abort.signal.aborted) setState({ key, data: null, error: error.message }); });
    return () => abort.abort();
  }, [path, key]);
  return { data: state.key === key ? state.data : null, loading: !!path && state.key !== key,
    error: state.key === key ? state.error : null, reintentar: () => setIntento(i => i + 1) };
}

export interface OpcionSector { id: number; nombre: string }
export interface OpcionEstablecimiento extends OpcionSector { codDeis: string }

export function useFiltrosSeguimiento(indicadorId?: string) {
  const params = useSearchParams();
  const pathname = usePathname();
  const router = useRouter();
  const raw = params.toString();
  const year = Number(params.get("ano")) || new Date().getFullYear();
  const sector = params.get("sectorId") === "0" ? "" : params.get("sectorId") ?? "";
  const establecimiento = params.get("establecimientoId") === "0" ? "" : params.get("establecimientoId") ?? "";
  const corte = params.get("mesCorte") ?? "";
  const cambiar = useCallback((changes: Record<string, string>) => {
    const next = new URLSearchParams(raw);
    for (const [key, value] of Object.entries(changes)) { if (value && value !== "0") next.set(key, value); else next.delete(key); }
    router.replace(`${pathname}?${next}`, { scroll: false });
  }, [raw, pathname, router]);
  const sectores = useRecurso<OpcionSector[]>("getSectores");
  const establecimientos = useRecurso<OpcionEstablecimiento[]>(`getEstablecimientos${sector ? `/${sector}` : ""}${indicadorId ? `?indicadorId=${indicadorId}` : ""}`);
  useEffect(() => {
    if (establecimiento && establecimientos.data && !establecimientos.data.some(e => String(e.id) === establecimiento))
      cambiar({ establecimientoId: "" });
  }, [establecimiento, establecimientos.data, cambiar]);
  return { params, pathname, year, sector, establecimiento, corte, cambiar, sectores, establecimientos,
    query: queryContexto(new URLSearchParams(raw)).toString() };
}
