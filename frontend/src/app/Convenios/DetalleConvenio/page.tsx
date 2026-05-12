"use client";
import React, { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import Loading from "../../components/Loading";
import ProgressBar from "../../components/ProgressBar";
import "../../Indicadores/style.css";
import { useConvenioDetail } from "@/lib/hooks/useConvenioDetail";
import { MetaItem } from "@/lib/hooks/useIndicadoresList";

const currentYear = new Date().getFullYear();

function ConvenioDetalleInner() {
  const searchParams = useSearchParams();
  const id = searchParams.get("id") ?? undefined;
  const initialYear = Number(searchParams.get("ano") || currentYear);
  const [selectedYear, setSelectedYear] = useState<number>(initialYear);
  const yearOptions = Array.from({ length: 2 }, (_, i) => currentYear - 1 + i);

  const { data, loading, error, avanceGeneral } = useConvenioDetail(id, selectedYear);

  const renderProgress = (item: MetaItem) => {
    const isTasa = item.isTasa === true;
    const percent = isTasa
      ? item.meta > 0 ? Math.min((item.actual / item.meta) * 100, 100) : 0
      : item.actual >= 1 ? 100 : Math.min(item.actual * 100, 100);
    const targetPercent = isTasa
      ? 100
      : item.meta && item.meta > 0 && item.meta <= 1 ? item.meta * 100 : item.meta;
    const statusClass = isTasa
      ? item.actual >= item.meta ? "ok" : "pending"
      : percent >= targetPercent ? "ok" : "pending";
    const statusLabel = statusClass === "ok" ? "Cumplida" : "Pendiente";
    const mensualLabel = item.mensual === true ? "Mensual" : "Semestral";
    const actualLabel = isTasa ? String(item.actual) : `${Math.round(percent)}%`;
    const metaLabel = isTasa ? String(item.meta) : `${Math.round(targetPercent)}%`;

    const backParam = encodeURIComponent(
      `/Convenios/DetalleConvenio?id=${data!.id}&ano=${selectedYear}`
    );

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
              href={`/Indicadores/DetalleIndicador?tipo=Convenios&id=${item.id}&back=${backParam}`}
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
            <div>
              {item.isColaborativo ? "Denominador comunal" : "Denominador"}:{" "}
              <strong>{item.denominador}</strong>
            </div>
          </div>
          <div style={{ textAlign: "right" }}>
            <div>Actual: <strong>{actualLabel}</strong></div>
            <div>Meta: <strong>{metaLabel}</strong></div>
          </div>
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="metas-page">
        <Loading message="Cargando convenio..." />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="metas-page">
        <div style={{ background: "var(--error-bg)", color: "var(--error-dark)", padding: 16, borderRadius: 8 }}>
          {error ?? "Convenio no encontrado."}
        </div>
        <Link href="/Convenios" className="btn-secondary" style={{ marginTop: 12, display: "inline-block" }}>
          Volver
        </Link>
      </div>
    );
  }

  return (
    <div className="metas-page">
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 8 }}>
        <h1 className="page-title" style={{ margin: 0 }}>{data.nombre}</h1>
        <Link href="/Convenios" className="btn-secondary">Volver</Link>
      </div>

      <div className="filters-bar" style={{ marginTop: 12 }}>
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

      <div className="overall-container" style={{ marginTop: 16 }}>
        <div className="overall-header">
          <span>Avance general del convenio</span>
          <span className="overall-value">{avanceGeneral.toFixed(1)}%</span>
        </div>
        <div className="overall-progress">
          <ProgressBar
            numerador={avanceGeneral}
            denominador={100}
            percent={avanceGeneral}
            metaPercent={100}
            height={16}
            showLabel={false}
          />
        </div>
        <div className="overall-meta">Meta: 100%</div>
      </div>

      <div style={{ marginTop: 24 }}>
        {!data.indicadores?.length ? (
          <p style={{ color: "var(--text-muted)" }}>No hay indicadores para este convenio en el año seleccionado.</p>
        ) : (
          data.indicadores.map(renderProgress)
        )}
      </div>
    </div>
  );
}

export default function ConvenioDetallePage() {
  return (
    <Suspense fallback={<div className="metas-page"><Loading message="Cargando convenio..." /></div>}>
      <ConvenioDetalleInner />
    </Suspense>
  );
}
