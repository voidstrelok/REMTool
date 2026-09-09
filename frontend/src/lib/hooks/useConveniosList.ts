import { useState, useEffect, useMemo } from "react";

const API = process.env.NEXT_PUBLIC_API;

export interface ConvenioItem {
  id: number;
  nombre: string;
  año: number;
  avance: number;
  indicadorCount: number;
}

export function useConveniosList(
  year: number,
  sectorId?: string,
  establecimientoId?: string
) {
  const [items, setItems] = useState<ConvenioItem[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        if (!API) throw new Error("NO_API");
         const params = new URLSearchParams();
         if (sectorId && sectorId !== "0") params.set("sectorId", sectorId);
         if (establecimientoId) params.set("establecimientoId", establecimientoId);
         const query = params.toString() ? `?${params.toString()}` : "";
         const res = await fetch(`${API}getConvenios/${year}${query}`);
        if (!res.ok) throw new Error("Fetch failed");
        const data = await res.json();
        setItems(Array.isArray(data) ? data : []);
      } catch {
        setError("Servicio no disponible.");
        setItems(null);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [year, sectorId, establecimientoId]);

  const avanceGeneral = useMemo(() => {
    if (!items?.length) return 0;
    const promedio = items.reduce((acc, item) => acc + (item.avance ?? 0), 0) / items.length;
    return Math.max(0, Math.min(promedio * 100, 100));
  }, [items]);

  return { items, loading, error, avanceGeneral };
}
