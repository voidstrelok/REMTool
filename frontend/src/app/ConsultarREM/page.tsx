"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { AlertCircle, Database, Loader2, Search } from "lucide-react";
import "./ConsultarREM.css";
import Breadcrumbs from "../components/Breadcrumbs";

const API = process.env.NEXT_PUBLIC_API ?? "";

const MESES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

interface SerieOption {
  id: number;
  nombre: string;
}

interface SectorOption {
  id: number;
  nombre: string;
}

interface EstablecimientoOption {
  id: number;
  id_sector: number;
  nombre: string;
  codDeis?: string;
}

interface HojaOption {
  id: number;
  codigo: string;
  nombre: string;
}

interface AnioOption {
  anio: number;
  meses: number[];
}

interface SeccionOption {
  id: number;
  hojaId: number;
  hoja: string;
  hojaCodigo: string;
  nombre: string;
  codigo: string;
  orden: number;
}

interface TablaConsulta {
  seccionId: number;
  hojaId: number;
  hoja: string;
  hojaCodigo: string;
  seccion: string;
  codigo: string;
  html: string;
  valores: Record<string, string>;
}

interface ConsultaResponse {
  anio: number;
  meses: number[];
  establecimientos: string[];
  reportesEncontrados: number;
  reporteEncontrado: boolean;
  tablas: TablaConsulta[];
}

interface MultiOption {
  value: string;
  label: string;
}

function selectStyle() {
  return {
    border: "1px solid var(--border)",
    background: "var(--bg)",
    color: "var(--text)",
  };
}

function MultiSelectField({
  label,
  options,
  values,
  onChange,
  placeholder,
}: {
  label: string;
  options: MultiOption[];
  values: string[];
  onChange: (values: string[]) => void;
  placeholder: string;
}) {
  const [abierto, setAbierto] = useState(false);
  const [busqueda, setBusqueda] = useState("");
  const contenedor = useRef<HTMLDivElement>(null);

  const opcionesVisibles = options.filter((option) =>
    option.label.toLocaleLowerCase().includes(busqueda.toLocaleLowerCase()),
  );
  const seleccionadas = options.filter((option) => values.includes(option.value));
  const todasVisiblesSeleccionadas = opcionesVisibles.length > 0
    && opcionesVisibles.every((option) => values.includes(option.value));

  useEffect(() => {
    if (!abierto) return;

    const cerrar = (event: MouseEvent) => {
      if (contenedor.current && !contenedor.current.contains(event.target as Node)) {
        setAbierto(false);
      }
    };

    document.addEventListener("mousedown", cerrar);
    return () => document.removeEventListener("mousedown", cerrar);
  }, [abierto]);

  const cambiarOpcion = (value: string) => {
    onChange(values.includes(value)
      ? values.filter((item) => item !== value)
      : [...values, value]);
  };

  const cambiarTodasVisibles = () => {
    const visibles = new Set(opcionesVisibles.map((option) => option.value));
    onChange(todasVisiblesSeleccionadas
      ? values.filter((value) => !visibles.has(value))
      : Array.from(new Set([...values, ...visibles])));
  };

  const resumen = seleccionadas.length === 0
    ? placeholder
    : seleccionadas.length <= 2
      ? seleccionadas.map((option) => option.label).join(", ")
      : `${seleccionadas.length} seleccionados`;

  return (
    <div className="consulta-rem-filtro" ref={contenedor}>
      <span className="consulta-rem-filtro-label">{label}</span>
      <button
        type="button"
        className={`consulta-rem-multiselect-trigger ${abierto ? "is-open" : ""}`}
        onClick={() => setAbierto((actual) => !actual)}
        aria-expanded={abierto}
      >
        <span className={seleccionadas.length === 0 ? "is-placeholder" : ""}>{resumen}</span>
        <span className="consulta-rem-chevron">⌄</span>
      </button>

      {abierto && (
        <div className="consulta-rem-multiselect-menu">
          <div className="consulta-rem-multiselect-tools">
            <input
              type="search"
              value={busqueda}
              onChange={(event) => setBusqueda(event.target.value)}
              placeholder="Buscar..."
              aria-label={`Buscar ${label.toLocaleLowerCase()}`}
            />
            <button type="button" onClick={cambiarTodasVisibles}>
              {todasVisiblesSeleccionadas ? "Quitar visibles" : "Seleccionar visibles"}
            </button>
          </div>

          <div className="consulta-rem-multiselect-options">
            {opcionesVisibles.length === 0 ? (
              <span className="consulta-rem-multiselect-empty">Sin coincidencias</span>
            ) : (
              opcionesVisibles.map((option) => (
                <label key={option.value} className="consulta-rem-multiselect-option">
                  <input
                    type="checkbox"
                    checked={values.includes(option.value)}
                    onChange={() => cambiarOpcion(option.value)}
                  />
                  <span>{option.label}</span>
                </label>
              ))
            )}
          </div>

          <div className="consulta-rem-multiselect-footer">
            <span>{values.length} seleccionado(s)</span>
            <button type="button" onClick={() => onChange([])}>Limpiar</button>
          </div>
        </div>
      )}
    </div>
  );
}

function prepararTablaHtml(
  html: string,
  valores: Record<string, string>,
  reporteEncontrado: boolean,
) {
  if (typeof window === "undefined") return html;

  const documentHtml = new DOMParser().parseFromString(html, "text/html");
  documentHtml.querySelectorAll<HTMLElement>("tr.fila-titulo").forEach((fila) => fila.remove());

  documentHtml.querySelectorAll<HTMLElement>("[data-celda]").forEach((celda) => {
    const coordenada = celda.dataset.celda ?? "";
    const esTotal = celda.dataset.tipo === "total" || celda.classList.contains("total");

    if (Object.prototype.hasOwnProperty.call(valores, coordenada)) {
      celda.textContent = valores[coordenada];
      celda.classList.add("rem-valor-cargado");
    } else if (esTotal) {
      // El valor de la plantilla base suele ser el caché de una fórmula y
      // normalmente es 0. Nunca debe mostrarse como dato consolidado.
      celda.textContent = "—";
    } else if (!reporteEncontrado && celda.classList.contains("prestacion")) {
      celda.textContent = "—";
    }
  });

  return documentHtml.body.innerHTML;
}

export default function ConsultarREMPage() {
  const [series, setSeries] = useState<SerieOption[]>([]);
  const [sectores, setSectores] = useState<SectorOption[]>([]);
  const [establecimientos, setEstablecimientos] = useState<EstablecimientoOption[]>([]);
  const [hojas, setHojas] = useState<HojaOption[]>([]);
  const [secciones, setSecciones] = useState<SeccionOption[]>([]);
  const [anios, setAnios] = useState<AnioOption[]>([]);
  const [serieId, setSerieId] = useState("");
  const [anio, setAnio] = useState("");
  const [sectorId, setSectorId] = useState("");
  const [establecimientoIds, setEstablecimientoIds] = useState<string[]>([]);
  const [meses, setMeses] = useState<string[]>([]);
  const [hojaId, setHojaId] = useState("");
  const [seccionId, setSeccionId] = useState("");
  const [loadingOptions, setLoadingOptions] = useState(false);
  const [loadingQuery, setLoadingQuery] = useState(false);
  const [error, setError] = useState("");
  const [resultado, setResultado] = useState<ConsultaResponse | null>(null);

  useEffect(() => {
    let activo = true;

    Promise.all([
      fetch(`${API}GetSeries`).then((response) => response.json()),
      fetch(`${API}getSectores`).then((response) => response.json()),
      fetch(`${API}getEstablecimientos?incluirTodos=true`).then((response) => response.json()),
    ])
      .then(([seriesData, sectoresData, establecimientosData]) => {
        if (!activo) return;

        const seriesNormalizadas = Array.isArray(seriesData) ? seriesData : [];
        setSeries(seriesNormalizadas);
        setSectores(Array.isArray(sectoresData) ? sectoresData : []);

        const establecimientosNormalizados = Array.isArray(establecimientosData)
          ? establecimientosData
          : [];
        setEstablecimientos(establecimientosNormalizados);
        setEstablecimientoIds(
          establecimientosNormalizados.map((establecimiento) => String(establecimiento.id)),
        );

        if (seriesNormalizadas.length > 0) {
          setSerieId(String(seriesNormalizadas[0].id));
        }
      })
      .catch(() => {
        if (activo) setError("No se pudieron cargar las opciones de consulta.");
      });

    return () => { activo = false; };
  }, []);

  useEffect(() => {
    if (!serieId) {
      setHojas([]);
      setSecciones([]);
      setAnios([]);
      setAnio("");
      setMeses([]);
      return;
    }

    let activo = true;
    setAnios([]);
    setAnio("");
    setMeses([]);
    setLoadingOptions(true);
    setError("");

    fetch(`${API}consultarREM/opciones?serieId=${encodeURIComponent(serieId)}`)
      .then(async (response) => {
        if (!response.ok) throw new Error(await response.text());
        return response.json();
      })
      .then((data) => {
        if (!activo) return;

        const hojasDisponibles = (Array.isArray(data.hojas) ? data.hojas : []) as HojaOption[];
        setHojas([...hojasDisponibles].sort((a, b) =>
          String(a.codigo || a.nombre).localeCompare(
            String(b.codigo || b.nombre),
            undefined,
            { numeric: true, sensitivity: "base" },
          )));
        setSecciones(Array.isArray(data.secciones) ? data.secciones : []);
        const aniosDisponibles: AnioOption[] = Array.isArray(data.anios)
          ? data.anios
            .map((opcion: { anio?: number; meses?: number[] }) => ({
              anio: Number(opcion.anio),
              meses: Array.isArray(opcion.meses)
                ? opcion.meses.map(Number).filter((mes) => mes >= 1 && mes <= 12)
                : [],
            }))
            .filter((opcion: AnioOption) => opcion.anio > 0 && opcion.meses.length > 0)
          : [];
        setAnios(aniosDisponibles);
        setAnio(aniosDisponibles.length > 0 ? String(aniosDisponibles[0].anio) : "");
        setMeses([]);
        setHojaId("");
        setSeccionId("");
      })
      .catch((reason) => {
        if (activo) {
          setError(reason instanceof Error
            ? reason.message
            : "No se pudo cargar la estructura REM.");
        }
      })
      .finally(() => {
        if (activo) setLoadingOptions(false);
      });

    return () => { activo = false; };
  }, [serieId]);

  const seccionesFiltradas = useMemo(
    () => secciones.filter((seccion) =>
      !hojaId || String(seccion.hojaId) === hojaId),
    [hojaId, secciones],
  );

  const establecimientosFiltrados = useMemo(
    () => establecimientos.filter((establecimiento) =>
      !sectorId || String(establecimiento.id_sector) === sectorId),
    [establecimientos, sectorId],
  );

  const mesesDisponibles = useMemo(
    () => anios.find((opcion) => String(opcion.anio) === anio)?.meses ?? [],
    [anio, anios],
  );

  const consultar = async (event: FormEvent) => {
    event.preventDefault();

    if (!serieId || !anio || establecimientoIds.length === 0 || meses.length === 0) {
      setError("Selecciona una serie, año, establecimiento y al menos un mes para consultar.");
      return;
    }

    setLoadingQuery(true);
    setError("");

    try {
      const params = new URLSearchParams({ serieId, anio });
      meses.forEach((mes) => params.append("meses", mes));
      establecimientoIds.forEach((id) => params.append("establecimientoIds", id));

      if (sectorId) params.set("sectorId", sectorId);
      if (hojaId) params.set("hojaId", hojaId);
      if (seccionId) params.set("seccionId", seccionId);

      const response = await fetch(`${API}consultarREM?${params.toString()}`);
      if (!response.ok) throw new Error(await response.text());

      setResultado(await response.json() as ConsultaResponse);
    } catch (reason) {
      setResultado(null);
      setError(reason instanceof Error ? reason.message : "No se pudo consultar el REM.");
    } finally {
      setLoadingQuery(false);
    }
  };

  return (
    <div className="py-6 px-4 sm:px-6 max-w-[1400px] mx-auto">
      <Breadcrumbs items={[{ label: "Operación REM" }, { label: "Consultar REM" }]} />
      <div className="mb-6">
        <div className="flex items-center gap-2 mb-1">
          <Database size={22} style={{ color: "var(--primary)" }} />
          <h1 className="text-2xl font-bold" style={{ color: "var(--text)" }}>
            Consultar REM
          </h1>
        </div>
        <p className="text-sm" style={{ color: "var(--text-light)" }}>
          Consolida la información de los meses y establecimientos seleccionados.
        </p>
      </div>

      <form
        onSubmit={consultar}
        className="consulta-rem-filtros p-4 rounded-xl mb-6"
        style={{ background: "var(--surface)", border: "1px solid var(--border)" }}
      >
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
            Serie
            <select
              value={serieId}
              onChange={(event) => setSerieId(event.target.value)}
              className="px-3 py-2 rounded-lg text-sm"
              style={selectStyle()}
            >
              <option value="">Seleccionar serie</option>
              {series.map((serie) => (
                <option key={serie.id} value={serie.id}>{serie.nombre}</option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
            Sector
            <select
              value={sectorId}
              onChange={(event) => {
                const nuevoSectorId = event.target.value;
                setSectorId(nuevoSectorId);
                setEstablecimientoIds(
                  establecimientos
                    .filter((establecimiento) =>
                      !nuevoSectorId || String(establecimiento.id_sector) === nuevoSectorId,
                    )
                    .map((establecimiento) => String(establecimiento.id)),
                );
              }}
              className="px-3 py-2 rounded-lg text-sm"
              style={selectStyle()}
            >
              <option value="">Todos los sectores</option>
              {sectores.map((sector) => (
                <option key={sector.id} value={sector.id}>{sector.nombre}</option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
            Año
            <select
              value={anio}
              onChange={(event) => {
                setAnio(event.target.value);
                setMeses([]);
              }}
              disabled={loadingOptions || anios.length === 0}
              className="px-3 py-2 rounded-lg text-sm disabled:opacity-60"
              style={selectStyle()}
            >
              <option value="">Seleccionar año</option>
              {anios.map((opcion) => (
                <option key={opcion.anio} value={opcion.anio}>{opcion.anio}</option>
              ))}
            </select>
          </label>

          <MultiSelectField
            label="Establecimientos"
            placeholder={establecimientosFiltrados.length > 0
              ? "Seleccionar establecimientos"
              : "Sin establecimientos"}
            values={establecimientoIds}
            onChange={setEstablecimientoIds}
            options={establecimientosFiltrados.map((establecimiento) => ({
              value: String(establecimiento.id),
              label: `${establecimiento.nombre}${establecimiento.codDeis
                ? ` (${establecimiento.codDeis})`
                : ""}`,
            }))}
          />

          <MultiSelectField
            label="Meses"
            placeholder={mesesDisponibles.length > 0 ? "Seleccionar meses" : "Sin meses cargados"}
            values={meses}
            onChange={setMeses}
            options={mesesDisponibles.map((mes) => ({
              value: String(mes),
              label: MESES[mes - 1],
            }))}
          />

          <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
            Hoja
            <select
              value={hojaId}
              onChange={(event) => {
                setHojaId(event.target.value);
                setSeccionId("");
              }}
              disabled={loadingOptions || hojas.length === 0}
              className="px-3 py-2 rounded-lg text-sm disabled:opacity-60"
              style={selectStyle()}
            >
              <option value="">Todas las hojas</option>
              {hojas.map((hoja) => (
                <option key={hoja.id} value={hoja.id}>
                  {hoja.codigo && hoja.codigo !== hoja.nombre
                    ? `${hoja.nombre}`
                    : hoja.nombre || hoja.codigo}
                </option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
            Sección
            <select
              value={seccionId}
              onChange={(event) => setSeccionId(event.target.value)}
              disabled={loadingOptions || seccionesFiltradas.length === 0}
              className="px-3 py-2 rounded-lg text-sm disabled:opacity-60"
              style={selectStyle()}
            >
              <option value="">Todas las secciones</option>
              {seccionesFiltradas.map((seccion) => (
                <option key={seccion.id} value={seccion.id}>
                  {seccion.nombre || seccion.codigo}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="flex items-center justify-end gap-3 flex-wrap mt-4">
          <button
            type="submit"
            disabled={loadingQuery || !serieId
              || !anio || establecimientoIds.length === 0 || meses.length === 0}
            className="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold text-white disabled:opacity-50 disabled:cursor-not-allowed"
            style={{ background: "var(--primary)" }}
          >
            {loadingQuery
              ? <Loader2 size={15} className="animate-spin" />
              : <Search size={15} />}
            {loadingQuery ? "Consolidando..." : "Consultar REM"}
          </button>
        </div>
      </form>

      {error && (
        <div
          className="flex items-start gap-2 p-3 rounded-lg mb-6 text-sm"
          style={{
            color: "var(--error-dark)",
            background: "var(--error-bg)",
            border: "1px solid var(--error)",
          }}
        >
          <AlertCircle size={16} className="mt-0.5 shrink-0" />
          {error}
        </div>
      )}

      {resultado && (
        <section className="consulta-rem-resultado">
          <div className="consulta-rem-resultado-header">
            <div>
              <h2>Informe consolidado</h2>
              <p>
                Año {resultado.anio} · Meses: {resultado.meses.map((mes) => MESES[mes - 1]).join(", ")}
                {resultado.establecimientos.length > 0
                  ? ` · ${resultado.establecimientos.length} establecimiento(s) con datos`
                  : " · sin reportes encontrados"}
              </p>
            </div>
            <span className={`consulta-rem-estado ${resultado.reporteEncontrado
              ? "encontrado"
              : "sin-datos"}`}>
              {resultado.reporteEncontrado ? "Datos consolidados" : "Sólo estructura"}
            </span>
          </div>

          {resultado.tablas.length === 0 ? (
            <div className="consulta-rem-vacio">
              No hay secciones para los filtros seleccionados.
            </div>
          ) : (
            <div className="flex flex-col gap-6">
              {resultado.tablas.map((tabla) => (
                <article className="consulta-rem-tabla-card" key={`${tabla.seccionId}-${tabla.codigo}`}>
                  <div className="consulta-rem-tabla-header">
                    <div className="consulta-rem-tabla-heading">
                      <span className="consulta-rem-tabla-sheet">{tabla.hojaCodigo || tabla.hoja}</span>
                      {tabla.hoja && tabla.hoja !== tabla.hojaCodigo && (
                        <span className="consulta-rem-tabla-sheet-name">{tabla.hoja}</span>
                      )}
                      <h3>{tabla.seccion || tabla.codigo}</h3>
                    </div>
                    <span className="consulta-rem-tabla-hint">Valores consolidados</span>
                  </div>
                  <div
                    className="consulta-rem-tabla"
                    dangerouslySetInnerHTML={{
                      __html: prepararTablaHtml(
                        tabla.html,
                        tabla.valores,
                        resultado.reporteEncontrado,
                      ),
                    }}
                  />
                </article>
              ))}
            </div>
          )}
        </section>
      )}
    </div>
  );
}
