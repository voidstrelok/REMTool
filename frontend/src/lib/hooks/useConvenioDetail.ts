import { useState, useEffect, useMemo } from "react";
import type { MetaItem } from "./useIndicadoresList";

const API = process.env.NEXT_PUBLIC_API;

export interface ConvenioDetalle {
  id: number;
  nombre: string;
  año: number;
  indicadores: MetaItem[];
}

export function useConvenioDetail(
  convenioId: number | string | undefined,
  year: number,
  sectorId?: string,
  establecimientoId?: string
) {
  const [data, setData] = useState<ConvenioDetalle | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!convenioId) {
      setLoading(false);
      setData(null);
      setError("Convenio no encontrado.");
      return;
    }
    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        if (!API) throw new Error("NO_API");
         const params = new URLSearchParams();
         if (sectorId && sectorId !== "0") params.set("sectorId", sectorId);
         if (establecimientoId) params.set("establecimientoId", establecimientoId);
         const query = params.toString() ? `?${params.toString()}` : "";
         const res = await fetch(`${API}getConvenioIndicadores/${convenioId}/${year}${query}`);
        if (!res.ok) throw new Error("Fetch failed");
        const json = await res.json();
        setData({
          id: json.id,
          nombre: json.nombre,
          año: json.año,
          indicadores: Array.isArray(json.indicadores) ? json.indicadores : [],
        });
      } catch {
        setError("Servicio no disponible o convenio no encontrado.");
        setData(null);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [convenioId, year, sectorId, establecimientoId]);

  const avanceGeneral = useMemo(() => {
    if (!data?.indicadores?.length) return 0;
    const promedio = data.indicadores.reduce(
      (acc, item) => acc + (item.avance ?? 0),
      0
    ) / data.indicadores.length;
    return Math.max(0, Math.min(promedio * 100, 100));
  }, [data]);

  return { data, loading, error, avanceGeneral };
}
