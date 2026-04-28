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
  year: number
) {
  const [data, setData] = useState<ConvenioDetalle | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!convenioId) return;
    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        if (!API) throw new Error("NO_API");
        const res = await fetch(`${API}getConvenioIndicadores/${convenioId}/${year}`);
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
  }, [convenioId, year]);

  const avanceGeneral = useMemo(() => {
    if (!data?.indicadores?.length) return 0;
    const total = data.indicadores.reduce((acc, item) => acc + (item.aporte ?? 0), 0);
    return total > 1 ? 100 : total * 100;
  }, [data]);

  return { data, loading, error, avanceGeneral };
}
