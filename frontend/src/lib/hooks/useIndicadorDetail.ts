import { useState, useEffect } from "react";

const API = process.env.NEXT_PUBLIC_API;

export interface MesData {
  mes: number;
  numerador: number;
  denominador: number;
}

export interface Establecimiento {
  key: string;
  nombre: string;
  sectorId: number | null;
  numeradorTotal: number;
  denominadorTotal: number;
  meses: MesData[];
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function useIndicadorDetail(
  id: string | undefined,
  sectorId?: string,
  establecimientoId?: string
) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        if (!API) throw new Error("NO_API");
        let url = `${API}getIndicador/${id}`;
        if (sectorId || establecimientoId) {
          const params = new URLSearchParams();
          if (sectorId) params.set("sectorId", sectorId);
          if (establecimientoId) params.set("establecimientoId", establecimientoId);
          url += `?${params.toString()}`;
        }
        const res = await fetch(url);
        if (!res.ok) throw new Error("Fetch failed");
        const json = await res.json();
        setData(json);
      } catch {
        setError("Servicio no disponible o el indicador no existe.");
        setData(null);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [id, sectorId, establecimientoId]);

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const resultados: any[] = data?.resultados ?? [];

  const resultadosPorEstablecimiento = resultados.reduce(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (acc: Record<string, any>, r: any) => {
      const key = `${r.establecimientoNombre ?? "Establecimiento"}-${r.sectorId ?? "-"}`;
      if (!acc[key]) {
        acc[key] = {
          key,
          nombre: r.establecimientoNombre ?? `Establecimiento ${key}`,
          sectorId: r.sectorId ?? null,
          numeradorTotal: 0,
          denominadorTotal: 0,
          meses: [] as MesData[],
        };
      }
      acc[key].numeradorTotal += r.numerador || 0;
      acc[key].denominadorTotal += r.denominador || 0;
      acc[key].meses.push({
        mes: r.mes,
        numerador: r.numerador || 0,
        denominador: r.denominador || 0,
      });
      return acc;
    },
    {}
  );

  const establecimientos: Establecimiento[] = Object.values(
    resultadosPorEstablecimiento
  );

  const overallNumerador = establecimientos.reduce(
    (s, e) => s + (e.numeradorTotal || 0),
    0
  );

  const isColaborativo = data?.isColaborativo === true;

  // Para indicadores colaborativos el denominador es comunal (un único valor para toda la
  // comuna) y los result-rows tienen denominador = 0. Usamos el campo `denominador` del DTO
  // en vez de sumar los ceros de los establecimientos.
  const overallDenominador = isColaborativo
    ? (data?.denominador ?? 0)
    : establecimientos.reduce((s, e) => s + (e.denominadorTotal || 0), 0);
  const arrayMeses =
    data?.mensual === true
      ? Array.from({ length: 12 }, (_, i) => i + 1)
      : [6, 12];

  const isTasa = data?.isTasa === true;

  // Para tasas: meta ya está en escala de % (ej. 6.3 = 6.3%). Para porcentajes: meta es decimal 0-1.
  const metaPercent = isTasa ? (data?.meta ?? 0) : (data?.meta ?? 0) * 100;

  // El avance siempre se calcula desde los establecimientos (num/den * 100)
  const actualPercent =
    overallDenominador > 0
      ? (overallNumerador / overallDenominador) * 100
      : 0;
  const overallPercent = actualPercent > 100 ? 100 : Number(actualPercent.toFixed(2));
  
  return {
    data,
    loading,
    error,
    establecimientos,
    overallNumerador,
    overallDenominador,
    arrayMeses,
    isTasa,
    overallPercent,
    metaPercent,
    isColaborativo,
  };
}
