import { useState, useEffect } from "react";

const API = process.env.NEXT_PUBLIC_API;

export interface ConvenioItem {
  id: number;
  nombre: string;
  año: number;
  avance: number;
  indicadorCount: number;
}

export function useConveniosList(year: number) {
  const [items, setItems] = useState<ConvenioItem[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        if (!API) throw new Error("NO_API");
        const res = await fetch(`${API}getConvenios/${year}`);
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
  }, [year]);

  return { items, loading, error };
}
