"use client";
import React, { Suspense, useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { FileDown } from "lucide-react";
import "./style.css";
import Loading from "../components/Loading";
import ProgressBar from "../components/ProgressBar";
import {
  isTipoIndicador,
  indicadoresConfig,
  TIPOS_INDICADOR,
} from "./config";
import { useIndicadoresList, MetaItem } from "@/lib/hooks/useIndicadoresList";

interface Sector {
  id: number;
  nombre: string;
}

interface EstablecimientoFilter {
  id: number;
  cod: string;
  nombre: string;
}

const API_BASE = process.env.NEXT_PUBLIC_API ?? "";

function IndicadoresInner() {
  const searchParams = useSearchParams();
  const tipoParam = searchParams.get("tipo");
  const tipo = tipoParam && isTipoIndicador(tipoParam) ? tipoParam : null;

  const currentYear = new Date().getFullYear();
  const yearOptions = Array.from({ length: 2 }, (_, i) => currentYear - 1 + i);
  const [selectedYear, setSelectedYear] = useState<number>(currentYear);
  const [selectedSector, setSelectedSector] = useState<string>("");
  const [selectedEstablecimiento, setSelectedEstablecimiento] = useState<string>("");
  const [sectores, setSectores] = useState<Sector[]>([]);
  const [establecimientos, setEstablecimientos] = useState<EstablecimientoFilter[]>([]);

  useEffect(() => {
    fetch(`${API_BASE}getSectores`)
      .then((res) => res.json())
      .then((data) => setSectores(Array.isArray(data) ? data : []))
      .catch(() => setSectores([]));
    fetch(`${API_BASE}getEstablecimientos`)
      .then((res) => res.json())
      .then((data) => {
        const parsed = Array.isArray(data)
          ? data.map((d: { id: number; codDeis: string; nombre: string }) => ({ id: d.id, cod: d.codDeis, nombre: d.nombre }))
          : [];
        setEstablecimientos(parsed);
      })
      .catch(() => setEstablecimientos([]));
  }, []);

  useEffect(() => {
    setSelectedEstablecimiento("");
    if (!selectedSector) return;
    fetch(`${API_BASE}getEstablecimientos/${selectedSector}`)
      .then((res) => res.json())
      .then((data) => {
        const parsed = Array.isArray(data)
          ? data.map((d: { id: number, codDeis: string; nombre: string }) => ({ id: d.id, cod: d.codDeis, nombre: d.nombre }))
          : [];
        setEstablecimientos(parsed);
      })
      .catch(() => setEstablecimientos([]));
  }, [selectedSector]);

  const config = tipo ? indicadoresConfig[tipo] : null;
  const { items, loading, error, avanceGeneral } = useIndicadoresList(
    tipo ?? TIPOS_INDICADOR[0],
    selectedYear,
    selectedSector || undefined,
    selectedEstablecimiento || undefined
  );

  if (!tipo) {
    return (
      <div className="metas-page">
        <h1 className="page-title">Indicadores</h1>
        <div style={{ display: "flex", flexDirection: "column", gap: 12, marginTop: 16 }}>
          {TIPOS_INDICADOR.map((t) => (
            <Link
              key={t}
              href={`/Indicadores?tipo=${t}`}
              className="detail-link"
              style={{ textAlign: "center" }}
            >
              {indicadoresConfig[t].titulo}
            </Link>
          ))}
        </div>
      </div>
    );
  }

  const renderProgress = (item: MetaItem) => {
    const isTasa = item.isTasa === true;

    // Para tasas: actual y meta están en la misma escala (ej. 5.2 vs 6.3)
    // Para porcentajes: actual y meta son decimales 0-1
    const percent = isTasa
      ? item.meta > 0 ? Math.min((item.actual / item.meta) * 100, 100) : 0
      : item.actual >= 1 ? 100 : Math.min(item.actual * 100, 100);

    const targetPercent = isTasa
      ? 100 // la meta siempre es el 100% de la barra para tasas
      : item.meta && item.meta > 0 && item.meta <= 1
        ? item.meta * 100
        : item.meta;

    const statusClass = isTasa
      ? item.actual >= item.meta ? "ok" : "pending"
      : percent >= targetPercent ? "ok" : "pending";
    const statusLabel = statusClass === "ok" ? "Cumplida" : "Pendiente";
    const mensualLabel = item.mensual === true ? "Mensual" : "Semestral";

    const actualLabel = isTasa ? String(item.actual) : `${Math.round(percent)}%`;
    const metaLabel = isTasa ? String(item.meta) : `${Math.round(targetPercent)}%`;
    return (
      <div className="progress-wrapper" key={item.id}>
        <div className="meta-header">
          <div style={{ flex: "1 1 auto" }}>
            <h3 className="meta-title">{item.nombre}</h3>
            <div className="meta-year">Año: {item.año}</div>
          </div>
          <div className="meta-actions">
            <div className="meta-badges">
              <span className="status-badge info">{mensualLabel}</span>
              <span className={`status-badge ${statusClass}`}>{statusLabel}</span>
            </div>
            <Link
              href={`/Indicadores/DetalleIndicador?tipo=${tipo}&id=${item.id}`}
              className="detail-link"
            >
              Ver detalle
            </Link>
          </div>
        </div>

        <div className="overall-progress">
          <ProgressBar
            numerador={item.numerador}
            denominador={item.denominador}
            percent={percent}
            metaPercent={isTasa ? 100 : Math.round(targetPercent)}
            width="100%"
            height={14}
            showLabel={false}
          />
        </div>

        <div className="meta-numbers">
          <div>
            <div>Numerador: <strong>{item.numerador}</strong></div>
            <div>Denominador: <strong>{item.denominador}</strong></div>
          </div>
          <div style={{ textAlign: "right" }}>
            <div>Actual: <strong>{actualLabel}</strong></div>
            <div>Meta: <strong>{metaLabel}</strong></div>
          </div>
        </div>
      </div>
    );
  };

  return (
    <div className="metas-page">
      <h1 className="page-title">{config!.titulo}</h1>
      <div className="filters-bar">
        <div className="filter-item">
          <label htmlFor="year-select">
            Año:
          </label>
          <select
            className="year-select"
            id="year-select"
            value={selectedYear}
            onChange={(e) => setSelectedYear(Number(e.target.value))}
          >
            {yearOptions.map((y) => (
              <option key={y} value={y}>{y}</option>
            ))}
          </select>
        </div>
        {sectores.length > 0 && (
          <div className="filter-item">
            <label htmlFor="sector-select">
              Sector:
            </label>
            <select
              className="year-select"
              id="sector-select"
              value={selectedSector}
              onChange={(e) => setSelectedSector(e.target.value)}
            >
              <option value="0">Todos</option>
              {sectores.map((s) => (
                <option key={s.id} value={String(s.id)}>{s.nombre}</option>
              ))}
            </select>
          </div>
        )}
        {establecimientos.length > 0 && (
          <div className="filter-item">
            <label htmlFor="est-select">
              Establecimiento:
            </label>
            <select
              className="year-select"
              id="est-select"
              value={selectedEstablecimiento}
              onChange={(e) => setSelectedEstablecimiento(e.target.value)}
            >
              <option value="">Todos</option>
              {establecimientos.map((e) => (
                <option key={e.cod} value={e.id}>{e.nombre}</option>
              ))}
            </select>
          </div>
        )}
        <a
          href={(() => {
            const params = new URLSearchParams();
            if (selectedSector) params.set("sectorId", selectedSector);
            if (selectedEstablecimiento) params.set("establecimientoId", selectedEstablecimiento);
            const query = params.toString() ? `?${params.toString()}` : "";
            return `${API_BASE}informeMensual/${config!.tipoId}/${selectedYear}${query}`;
          })()}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1.5 btn-secondary"
        >
          <FileDown size={15} />
          Descargar PDF
        </a>
      </div>

      {loading && <Loading message={config!.mensajeCargando} />}
      {error && <div className="status error">{error}</div>}

      {!loading && items && items.length > 0 ? (
        <div className="resumen-general">
          <div style={{ fontWeight: 600, fontSize: 16, marginBottom: 8 }}>
            Avance General
          </div>
          <div style={{ fontSize: 22, fontWeight: 800, marginBottom: 8 }}>
            {avanceGeneral.toFixed(1)}%
          </div>
          <ProgressBar
            numerador={avanceGeneral}
            denominador={100}
            metaPercent={100}
            width="100%"
            height={18}
            showLabel={false}
            showMetaLine={false}
          />
        </div>
      ) : null}

      <div className="metas-list">
        {items && items.length > 0 ? (
          items.map((it) => renderProgress(it))
        ) : (
          !loading && <div className="status">{config!.mensajeVacio}</div>
        )}
      </div>
    </div>
  );
}

export default function IndicadoresPage() {
  return (
    <Suspense>
      <IndicadoresInner />
    </Suspense>
  );
}
