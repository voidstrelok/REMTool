'use client';

import { useEffect, useState } from 'react';
import { apiUrl } from '@/lib/api';

type InfoResponse = {
  ultima_actualizacion: string;
  servicio_enabled: boolean;
  monitoreo_enabled: boolean;
  ultimo_rem_cargado: {
    ano: number;
    mes: number;
  } | null;
};

const NOMBRES_MESES = [
  'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
  'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre',
];

function formatFecha(fecha: string | null | undefined) {
  const match = fecha?.match(/^(\d{4})-(\d{2})-(\d{2})/);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : fecha ?? '—';
}

function formatPeriodo(ano: number, mes: number) {
  const nombreMes = NOMBRES_MESES[mes - 1];
  return nombreMes ? `${nombreMes} ${ano}` : String(ano);
}

export default function InfoActualizacion() {
  const [data, setData] = useState<InfoResponse | null>(null);
  const [checked, setChecked] = useState(false);
  const [healthStatus, setHealthStatus] = useState<'checking' | 'online' | 'offline'>('checking');

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      try {
        const res = await fetch(apiUrl('getUltimaActualizacion'), { cache: 'no-store', signal: controller.signal });
        if (!res.ok) throw new Error();
        const json: InfoResponse = await res.json();
        setData(json);
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') return;
        setData(null);
      } finally {
        if (!controller.signal.aborted) setChecked(true);
      }
    };

    const checkHealth = async () => {
      try {
        const res = await fetch(apiUrl('health'), { cache: 'no-store', signal: controller.signal });
        if (!res.ok) throw new Error();
        setHealthStatus('online');
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') return;
        setHealthStatus('offline');
      }
    };

    fetchData();
    checkHealth();

    return () => controller.abort();
  }, []);

  const healthLabel = healthStatus === 'online'
    ? 'API disponible'
    : healthStatus === 'offline'
      ? 'API no disponible'
      : 'Verificando API';

  const healthIndicator = (
    <span className="inline-flex items-center gap-1" title={healthLabel} aria-label={healthLabel} role="status">
      <span
        aria-hidden="true"
        className={`inline-block h-1.5 w-1.5 rounded-full ${healthStatus === 'online' ? 'bg-emerald-300' : healthStatus === 'offline' ? 'bg-red-300' : 'bg-white/40'}`}
      />
      <span className="sr-only">{healthLabel}</span>
    </span>
  );

  // No mostrar nada durante la carga inicial para evitar ruido visual en el header
  if (!checked) return null;

  if (healthStatus === 'offline') {
    return (
      <span className="inline-flex items-center gap-2 text-xs" style={{ color: 'rgba(255,255,255,0.55)' }}>
        {healthIndicator} API no disponible.
      </span>
    );
  }

  if (!data) {
    return (
      <span className="inline-flex items-center gap-2 text-xs" style={{ color: 'rgba(255,255,255,0.55)' }}>
        {healthIndicator} API disponible · Sin información de actualización.
      </span>
    );
  }

  if (!data.servicio_enabled) {
    return (
      <span className="inline-flex items-center gap-2 text-xs" style={{ color: 'rgba(255,255,255,0.55)' }}>
        {healthIndicator} Servicio en mantención.
      </span>
    );
  }

  return (
    <span className="inline-flex items-center gap-2 text-xs" style={{ color: 'rgba(255,255,255,0.50)' }}>
      {healthIndicator}
      Última actualización: {formatFecha(data.ultima_actualizacion)}
      {data.ultimo_rem_cargado && (
        <> · Último REM cargado: {formatPeriodo(data.ultimo_rem_cargado.ano, data.ultimo_rem_cargado.mes)}</>
      )}
    </span>
  );
}
