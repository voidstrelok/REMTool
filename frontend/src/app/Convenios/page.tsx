"use client";
import React, { Suspense, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { FileDown } from "lucide-react";
import Loading from "../components/Loading";
import ProgressBar from "../components/ProgressBar";
import "../Indicadores/style.css";
import { useConveniosList, ConvenioItem } from "@/lib/hooks/useConveniosList";
import Breadcrumbs from "../components/Breadcrumbs";

const currentYear = new Date().getFullYear();
const API_BASE = process.env.NEXT_PUBLIC_API ?? "";

interface Sector {
  id: number;
  nombre: string;
}

interface EstablecimientoFilter {
  id: number;
  cod: string;
  nombre: string;
}

function renderConvenio(
  convenio: ConvenioItem,
  year: number,
  sectorId: string,
  establecimientoId: string
) {
  const percent = Math.max(0, Math.min(convenio.avance * 100, 100));
  const statusClass = percent >= 100 ? "ok" : "pending";
  const statusLabel = statusClass === "ok" ? "Cumplido" : "Pendiente";
  const params = new URLSearchParams({ id: String(convenio.id), ano: String(year) });
  if (sectorId && sectorId !== "0") params.set("sectorId", sectorId);
  if (establecimientoId) params.set("establecimientoId", establecimientoId);

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
          <Link href={`/Convenios/DetalleConvenio?${params.toString()}`} className="detail-link">
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
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const router = useRouter();
  const yearOptions = Array.from({ length: 2 }, (_, i) => currentYear - 1 + i);
  const [selectedYear, setSelectedYear] = useState<number>(() => Number(searchParams.get("ano")) || currentYear);
  const [selectedSector, setSelectedSector] = useState<string>(() => searchParams.get("sectorId") ?? "");
  const [selectedEstablecimiento, setSelectedEstablecimiento] = useState<string>(() => searchParams.get("establecimientoId") ?? "");
  const [sectores, setSectores] = useState<Sector[]>([]);
  const [establecimientos, setEstablecimientos] = useState<EstablecimientoFilter[]>([]);
  const { items, loading, error, avanceGeneral } = useConveniosList(
    selectedYear,
    selectedSector,
    selectedEstablecimiento
  );

  useEffect(() => {
    fetch(`${API_BASE}getSectores`)
      .then((res) => res.json())
      .then((data) => setSectores(Array.isArray(data) ? data : []))
      .catch(() => setSectores([]));
    fetch(`${API_BASE}getEstablecimientos`)
      .then((res) => res.json())
      .then((data) => {
        const parsed = Array.isArray(data)
          ? data.map((d: { id: number; codDeis: string; nombre: string }) => ({
              id: d.id,
              cod: d.codDeis,
              nombre: d.nombre,
            }))
          : [];
        setEstablecimientos(parsed);
      })
      .catch(() => setEstablecimientos([]));
  }, []);

  useEffect(() => {
    if (!selectedSector || selectedSector === "0") return;
    fetch(`${API_BASE}getEstablecimientos/${selectedSector}`)
      .then((res) => res.json())
      .then((data) => {
        const parsed = Array.isArray(data)
          ? data.map((d: { id: number; codDeis: string; nombre: string }) => ({
              id: d.id,
              cod: d.codDeis,
              nombre: d.nombre,
            }))
          : [];
        setEstablecimientos(parsed);
      })
      .catch(() => setEstablecimientos([]));
  }, [selectedSector]);

  useEffect(() => {
    const params = new URLSearchParams(searchParams.toString());
    if (selectedYear === currentYear) params.delete("ano");
    else params.set("ano", String(selectedYear));
    if (selectedSector && selectedSector !== "0") params.set("sectorId", selectedSector);
    else params.delete("sectorId");
    if (selectedEstablecimiento) params.set("establecimientoId", selectedEstablecimiento);
    else params.delete("establecimientoId");
    const query = params.toString();
    router.replace(`${pathname}${query ? `?${query}` : ""}`, { scroll: false });
  }, [pathname, router, searchParams, selectedYear, selectedSector, selectedEstablecimiento]);

  if (loading) {
    return (
      <div className="metas-page">
        <Loading message="Cargando convenios..." />
      </div>
    );
  }

  return (
    <div className="metas-page">
      <Breadcrumbs items={[{ label: "Convenios" }]} />
      <div className="page-intro">
        <h1 className="page-title" style={{ textAlign: "left", marginBottom: 4 }}>Seguimiento de Convenios</h1>
        <p>Consulta el avance de los convenios y sus indicadores asociados.</p>
      </div>

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
        {sectores.length > 0 && (
          <div className="filter-item">
            <label htmlFor="sector-select">Sector:</label>
            <select
              className="year-select"
              id="sector-select"
              value={selectedSector}
              onChange={(e) => {
                setSelectedSector(e.target.value);
                setSelectedEstablecimiento("");
              }}
            >
              <option value="">Todos</option>
              {sectores.map((sector) => (
                <option key={sector.id} value={String(sector.id)}>{sector.nombre}</option>
              ))}
            </select>
          </div>
        )}
        {establecimientos.length > 0 && (
          <div className="filter-item">
            <label htmlFor="est-select">Establecimiento:</label>
            <select
              className="year-select"
              id="est-select"
              value={selectedEstablecimiento}
              onChange={(e) => setSelectedEstablecimiento(e.target.value)}
            >
              <option value="">Todos</option>
              {establecimientos.map((establecimiento) => (
                <option key={establecimiento.cod} value={establecimiento.id}>
                  {establecimiento.nombre}
                </option>
              ))}
            </select>
          </div>
        )}
        <a
          href={(() => {
            const params = new URLSearchParams();
            if (selectedSector && selectedSector !== "0") params.set("sectorId", selectedSector);
            if (selectedEstablecimiento) params.set("establecimientoId", selectedEstablecimiento);
            return `${API_BASE}informeMensual/1/${selectedYear}?${params.toString()}`;
          })()}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1.5 btn-secondary"
        >
          <FileDown size={15} />
          Descargar PDF
        </a>
      </div>

      {error ? (
        <div style={{ background: "var(--error-bg)", color: "var(--error-dark)", padding: 16, borderRadius: 8 }}>
          {error}
        </div>
      ) : !items?.length ? (
        <p style={{ color: "var(--text-muted)", marginTop: 24 }}>No hay convenios para mostrar.</p>
      ) : (
        <>
          <div className="resumen-general">
            <div style={{ fontWeight: 600, fontSize: 16, marginBottom: 8 }}>Avance General</div>
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
          <div className="convenios-grid">
            {items.map((convenio) => renderConvenio(
              convenio,
              selectedYear,
              selectedSector,
              selectedEstablecimiento
            ))}
          </div>
        </>
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
