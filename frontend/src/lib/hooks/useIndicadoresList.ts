import { useState, useEffect, useMemo } from "react";
import { TipoIndicador, indicadoresConfig } from "@/app/Indicadores/config";

const API = process.env.NEXT_PUBLIC_API;

export interface MetaItem {
  id: number;
  nombre: string;
  año: number;
  numerador: number;
  denominador: number;
  meta: number;
  aporte: number;
  actual: number;
  mensual: boolean;
  isTasa?: boolean;
  isColaborativo?: boolean;
}

export function useIndicadoresList(
  tipo: TipoIndicador,
  selectedYear: number,
  sectorId?: string,
  establecimientoId?: string
) {
  const [items, setItems] = useState<MetaItem[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const { tipoId } = indicadoresConfig[tipo];

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        if (!API) throw new Error("NO_API");

        let url: string;
        if (sectorId || establecimientoId) {
          const params = new URLSearchParams();
          if (sectorId) params.set("sectorId", sectorId);
          if (establecimientoId) params.set("establecimientoId", establecimientoId);
          url = `${API}getIndicadores/${selectedYear}/${tipoId}/filtrado?${params.toString()}`;
        } else {
          url = `${API}getIndicadores/${selectedYear}/${tipoId}`;
        }

        const res = await fetch(url);
        if (!res.ok) throw new Error("Fetch failed");
        const data = await res.json();
        const arr: MetaItem[] = Array.isArray(data) ? data : [data];
        setItems(arr);
      } catch {
        setError("Servicio no disponible.");
        setItems(null);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [tipoId, selectedYear, sectorId, establecimientoId]);

  const avanceGeneral = useMemo(() => {
    if (!items?.length) return 0;
    const total = items.reduce((acc, item) => acc + (item.aporte ?? 0), 0);
    return total > 1 ? 100 : total * 100;
  }, [items]);

  return { items, loading, error, avanceGeneral };
}
