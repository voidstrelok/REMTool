"use client";
import React, { Suspense, useState } from "react";
import Link from "next/link";
import Loading from "../components/Loading";
import ProgressBar from "../components/ProgressBar";
import "../Indicadores/style.css";
import { useConveniosList, ConvenioItem } from "@/lib/hooks/useConveniosList";

const currentYear = new Date().getFullYear();

function renderConvenio(convenio: ConvenioItem, year: number) {
  const percent = Math.min(convenio.avance * 100, 100);
  const statusClass = percent >= 100 ? "ok" : "pending";
  const statusLabel = statusClass === "ok" ? "Cumplido" : "Pendiente";

  return (
    <div className="progress-wrapper" key={convenio.id}>
      <div className="meta-header">
        <div style={{ flex: "1 1 auto", minWidth: 0, overflow: "hidden" }}>
          <h3 className="meta-title">{convenio.nombre}</h3>
          <div className="meta-year">{convenio.indicadorCount} indicadores</div>
        </div>
        <div className="meta-actions">
          <div className="meta-badges">
            <span className={`status-badge ${statusClass}`}>{statusLabel}</span>
          </div>
          <Link href={`/Convenios/DetalleConvenio?id=${convenio.id}&ano=${year}`} className="detail-link">
            Ver indicadores
          </Link>
        </div>
      </div>

      <div className="overall-progress">
        <ProgressBar
          numerador={percent}
          denominador={100}
          percent={percent}
          metaPercent={100}
          width="100%"
          height={14}
          showLabel={false}
        />
      </div>

      <div className="meta-numbers">
        <div />
        <div style={{ textAlign: "right" }}>
          <div>Avance: <strong>{percent.toFixed(1)}%</strong></div>
        </div>
      </div>
    </div>
  );
}

function ConveniosInner() {
  const yearOptions = Array.from({ length: 2 }, (_, i) => currentYear - 1 + i);
  const [selectedYear, setSelectedYear] = useState<number>(currentYear);
  const { items, loading, error } = useConveniosList(selectedYear);

  if (loading) {
    return (
      <div className="metas-page">
        <Loading message="Cargando convenios..." />
      </div>
    );
  }

  return (
    <div className="metas-page">
      <h1 className="page-title">Seguimiento de Convenios</h1>

      <div className="filters-bar">
        <div className="filter-item">
          <label htmlFor="year-select">Año:</label>
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
      </div>

      {error ? (
        <div style={{ background: "var(--error-bg)", color: "var(--error-dark)", padding: 16, borderRadius: 8 }}>
          {error}
        </div>
      ) : !items?.length ? (
        <p style={{ color: "var(--text-muted)", marginTop: 24 }}>No hay convenios para mostrar.</p>
      ) : (
        <div className="convenios-grid">
          {items.map((convenio) => renderConvenio(convenio, selectedYear))}
        </div>
      )}
    </div>
  );
}

export default function ConveniosPage() {
  return (
    <Suspense fallback={<div className="metas-page"><Loading message="Cargando convenios..." /></div>}>
      <ConveniosInner />
    </Suspense>
  );
}
