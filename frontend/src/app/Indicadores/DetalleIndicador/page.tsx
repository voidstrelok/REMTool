"use client";
import React, { Suspense, useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { FileDown } from "lucide-react";
import "../style.css";
import Loading from "../../components/Loading";
import ProgressBar from "../../components/ProgressBar";
import { isTipoIndicador } from "../config";
import { useIndicadorDetail } from "@/lib/hooks/useIndicadorDetail";

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

const monthName = (m: number) =>
  ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"][
    Math.max(0, Math.min(11, m - 1))
  ] || String(m);

function DetalleIndicadorInner() {
  const searchParams = useSearchParams();
  const id = searchParams.get("id") ?? undefined;
  const tipoParam = searchParams.get("tipo");
  const tipo = tipoParam && isTipoIndicador(tipoParam) ? tipoParam : null;
  const backParam = searchParams.get("back");
  const backHref = backParam
    ? decodeURIComponent(backParam)
    : tipo ? `/Indicadores?tipo=${tipo}` : "/Indicadores";

  const [showInfo, setShowInfo] = useState(false);
  const [selectedSector, setSelectedSector] = useState<string>("");
  const [selectedEstablecimiento, setSelectedEstablecimiento] = useState<string>("");
  const [sectores, setSectores] = useState<Sector[]>([]);
  const [filterEstablecimientos, setFilterEstablecimientos] = useState<EstablecimientoFilter[]>([]);

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
        setFilterEstablecimientos(parsed);
      })
      .catch(() => setFilterEstablecimientos([]));
  }, []);

  useEffect(() => {
    setSelectedEstablecimiento("");
    if (!selectedSector) {
      fetch(`${API_BASE}getEstablecimientos`)
        .then((res) => res.json())
        .then((data) => {
          const parsed = Array.isArray(data)
            ? data.map((d: { id: number; codDeis: string; nombre: string }) => ({ id: d.id, cod: d.codDeis, nombre: d.nombre }))
            : [];
          setFilterEstablecimientos(parsed);
        })
        .catch(() => setFilterEstablecimientos([]));
      return;
    }
    fetch(`${API_BASE}getEstablecimientos/${selectedSector}`)
      .then((res) => res.json())
      .then((data) => {
        const parsed = Array.isArray(data)
          ? data.map((d: { id: number; codDeis: string; nombre: string }) => ({ id: d.id, cod: d.codDeis, nombre: d.nombre }))
          : [];
        setFilterEstablecimientos(parsed);
      })
      .catch(() => setFilterEstablecimientos([]));
  }, [selectedSector]);

  const {
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
  } = useIndicadorDetail(id, selectedSector || undefined, selectedEstablecimiento || undefined);

  if (loading) {
    return (
      <div className="metas-page">
        <Loading message="Cargando indicador..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="metas-page">
        <div
          style={{
            backgroundColor: "var(--error-bg)",
            color: "var(--error-dark)",
            padding: "16px",
            borderRadius: "8px",
            fontSize: "0.95rem",
            textAlign: "center",
          }}
        >
          {error}
        </div>
        <Link
          href={backHref}
          style={{
            marginTop: 12,
            display: "inline-block",
            padding: "10px 14px",
            background: "var(--primary)",
            color: "#fff",
            borderRadius: 8,
            textDecoration: "none",
            fontWeight: 600,
          }}
        >
          Volver
        </Link>
      </div>
    );
  }

  return (
    <div className="metas-page">
      <div className="indicator-header">
        <div className="indicator-info">
          <h1 className="page-title" style={{ textAlign: "left" }}>
            {data.nombre}
          </h1>
          <div className="indicator-meta">
            <span><strong>Año:</strong> {data.año}</span>
            <span><strong>Meta:</strong> {isTasa ? metaPercent : `${Math.round(metaPercent)}%`}</span>
            <span><strong>Avance:</strong> {isTasa ? (overallNumerador/overallDenominador).toFixed(2) : `${overallPercent}%`}</span>
          </div>
          <button
            hidden
            style={{
              marginTop: 10,
              padding: "6px 16px",
              background: "var(--primary)",
              color: "#fff",
              border: "none",
              borderRadius: 6,
              cursor: "pointer",
              fontSize: "0.95rem",
              fontWeight: 500,
              boxShadow: "0 1px 2px rgba(0,0,0,0.04)",
            }}
            onClick={() => setShowInfo((v) => !v)}
            aria-expanded={showInfo}
            aria-controls="indicador-info-panel"
          >
            {showInfo ? "Ocultar información del indicador" : "Ver información del indicador"}
          </button>
          {showInfo && (
            <div
              id="indicador-info-panel"
              style={{
                marginTop: 12,
                background: "var(--surface-alt)",
                color: "var(--text)",
                borderRadius: 8,
                padding: 16,
                fontSize: "1rem",
                boxShadow: "0 2px 8px rgba(0,0,0,0.04)",
                maxWidth: 600,
              }}
            >
              <span>
                Aquí irá la información sobre el indicador. Puedes editar este
                texto para agregar la descripción, metodología, fuentes, o
                cualquier detalle relevante.
              </span>
            </div>
          )}
        </div>
        <Link href={backHref} className="btn-secondary">
          Volver
        </Link>
        {id && (
          <a
            href={(() => {
              const params = new URLSearchParams();
              if (selectedSector) params.set("sectorId", selectedSector);
              if (selectedEstablecimiento) params.set("establecimientoId", selectedEstablecimiento);
              const query = params.toString() ? `?${params.toString()}` : "";
              return `${process.env.NEXT_PUBLIC_API ?? ""}detalleIndicador/${id}${query}`;
            })()}
            target="_blank"
            rel="noopener noreferrer"
            className="btn-secondary inline-flex items-center gap-1.5"
          >
            <FileDown size={15} />
            Descargar PDF
          </a>
        )}
      </div>

      {/* Filtros */}
      <div className="filters-bar">
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
              <option value="">Todos</option>
              {sectores.map((s) => (
                <option key={s.id} value={String(s.id)}>{s.nombre}</option>
              ))}
            </select>
          </div>
        )}
        {filterEstablecimientos.length > 0 && (
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
              {filterEstablecimientos.map((e) => (
                <option key={e.cod} value={String(e.id)}>{e.nombre}</option>
              ))}
            </select>
          </div>
        )}
      </div>

      {/* Avance general del indicador */}
      <div className="overall-container">
        <div className="overall-header">
          <span>Avance general del indicador</span>
          <span className="overall-value">
            {isColaborativo
              ? `${Math.round(overallNumerador)} / ${Math.round(overallDenominador)}`
              : `${Math.round(overallNumerador)}/${Math.round(overallDenominador)}`}
            {" "}—{" "}
            {isTasa ? (overallNumerador/overallDenominador).toFixed(2) : `${overallPercent}%`}
          </span>
        </div>
        <div className="overall-progress">
          {isTasa ? (
            <ProgressBar
              numerador={overallPercent as number}
              denominador={metaPercent > 0 ? metaPercent : 1}
              percent={metaPercent > 0 ? ((overallPercent as number) / metaPercent) : 0}
              metaPercent={100}
              height={16}
              showLabel={false}
            />
          ) : (
            <ProgressBar
              numerador={overallNumerador}
              denominador={overallDenominador}
              metaPercent={metaPercent}
              height={16}
              showLabel={false}
            />
          )}
        </div>
        <div className="overall-meta">Meta: {isTasa ? metaPercent : `${Math.round(metaPercent)}%`}</div>
      </div>

      <div
        style={{
          marginTop: 16,
          overflowX: "auto",
          textAlign: "center",
          border: "1px solid var(--border)",
          borderRadius: "8px",
          scrollbarWidth: "none",
          msOverflowStyle: "none",
        }}
        className="hide-scrollbar"
      >
        <table className="metas-table">
          <thead>
            <tr style={{ backgroundColor: "var(--primary)", color: "#fff" }}>
              <th style={{ padding: "12px", textAlign: "left", minWidth: "200px", fontWeight: 600 }}>
                Establecimiento
              </th>
              {arrayMeses.map((mes) => (
                <th key={`header-${mes}`} style={{ padding: "10px", textAlign: "center", minWidth: "60px", fontWeight: 600 }}>
                  {monthName(mes)}
                </th>
              ))}
              <th style={{ padding: "10px", textAlign: "center", minWidth: "70px", fontWeight: 700, backgroundColor: "var(--accent)", color: "#fff" }}>
                Total
              </th>
            </tr>
          </thead>
          <tbody>
            {establecimientos.length > 0 && overallNumerador !== 0 ? (
              establecimientos.map((est, idx) => (
                <tr
                  key={`row-${est.key}`}
                  className="est-row"
                  style={{
                    borderBottom: "1px solid var(--border)",
                    backgroundColor: idx % 2 === 0 ? "var(--surface)" : "var(--surface-alt)",
                    transition: "background-color 0.2s",
                  }}
                >
                  <td style={{ padding: "10px", fontWeight: 600, minWidth: "200px", color: "var(--text)" }}>
                    {est.nombre}
                  </td>
                  {arrayMeses.map((mes) => {
                    const mesData = est.meses.find((m) => m.mes === mes);
                    if (!mesData) {
                      return (
                        <td key={`cell-${est.key}-${mes}`} style={{ padding: "10px", textAlign: "center", fontSize: "0.8rem", color: "var(--text-light)", fontWeight: 400 }}>
                          -
                        </td>
                      );
                    }
                    const numerador = mesData.numerador ?? 0;
                    const denominador = mesData.denominador ?? 0;
                    // Mostrar solo numerador si es colaborativo
                    const showDen = isColaborativo ? false : !data?.isDenFijo;
                    const cellContent = showDen ? `${numerador}/${denominador}` : `${Math.round(numerador)}`;
                    const isCero = showDen ? denominador === 0 && numerador === 0 : numerador === 0;
                    return (
                      <td key={`cell-${est.key}-${mes}`} style={{ padding: "10px", textAlign: "center", fontSize: "0.8rem", color: isCero ? "var(--text-light)" : "var(--text)", fontWeight: 500 }}>
                        {cellContent}
                      </td>
                    );
                  })}
                  <td style={{ padding: "10px", textAlign: "center", verticalAlign: "middle" }}>
                    {isColaborativo ? (
                      <span style={{ fontWeight: 700, color: "var(--accent)" }}>{Math.round(est.numeradorTotal)}</span>
                    ) : est.denominadorTotal === 0 ? (
                      <span style={{ color: "var(--text-light)" }}>Sin datos</span>
                    ) : isTasa ? (
                      <span style={{ fontWeight: 700, color: "var(--accent)" }}>
                        {Math.round(est.numeradorTotal)}/{Math.round(est.denominadorTotal)}<br></br>
                        {(est.numeradorTotal / est.denominadorTotal).toFixed(1)}
                      </span>
                    ) : (
                      <div>
                        <div style={{ fontWeight: 700, color: "var(--accent)", marginBottom: 8 }}>
                          {Math.round(est.numeradorTotal)}/{Math.round(est.denominadorTotal)}
                        </div>
                        <ProgressBar
                          numerador={Math.round(est.numeradorTotal)}
                          denominador={Math.round(est.denominadorTotal)}
                          metaPercent={metaPercent}
                          width={180}
                          height={12}
                          showLabel={true}
                        />
                      </div>
                    )}
                  </td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={14} style={{ padding: 16, textAlign: "center", color: "var(--text-light)" }}>
                  No hay datos de establecimientos disponibles.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default function DetalleIndicadorPage() {
  return (
    <Suspense>
      <DetalleIndicadorInner />
    </Suspense>
  );
}
