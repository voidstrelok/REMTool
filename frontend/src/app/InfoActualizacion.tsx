'use client';

import { useEffect, useState } from 'react';

const API = process.env.NEXT_PUBLIC_API;

type InfoResponse = {
  ultima_actualizacion: string;
  servicio_enabled: boolean;
  monitoreo_enabled: boolean;
};

export default function InfoActualizacion() {
  const [data, setData] = useState<InfoResponse | null>(null);
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const res = await fetch(API + 'getUltimaActualizacion');
        if (!res.ok) throw new Error();
        const json: InfoResponse = await res.json();
        setData(json);
      } catch {
        setData(null);
      } finally {
        setChecked(true);
      }
    };
    fetchData();
  }, []);

  // No mostrar nada durante la carga inicial para evitar ruido visual en el header
  if (!checked) return null;

  if (!data || !data.servicio_enabled) {
    return (
      <span className="text-xs" style={{ color: 'rgba(255,255,255,0.45)' }}>
        Servicio no disponible o en mantención.
      </span>
    );
  }

  return (
    <span className="text-xs" style={{ color: 'rgba(255,255,255,0.50)' }}>
      Última actualización: {data.ultima_actualizacion}
    </span>
  );
}