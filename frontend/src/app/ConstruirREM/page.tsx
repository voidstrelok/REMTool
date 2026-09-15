"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import {
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  Download,
  FileSpreadsheet,
  Loader2,
  RotateCcw,
  Trash2,
  Upload,
  XCircle,
} from "lucide-react";
import Breadcrumbs from "../components/Breadcrumbs";
import { apiUrl } from "@/lib/api";
import "../ConsultarREM/ConsultarREM.css";

const STORAGE_KEY = "remtool_construir_rem_v1";

type Establecimiento = { codDeis: string; nombre: string };
type PrestacionDatos = { prestacion: string; valores: string[] };
type PrestacionSeccion = { codigo: string; nombre: string; orden?: number; cantidadValores: number };
type Seccion = { codigo: string; nombre: string; hoja: string; hojaCodigo?: string; hojaOrden?: number; orden: number; prestaciones: PrestacionSeccion[] };
type Parte = {
  id: string;
  nombre: string;
  nombreArchivo: string;
  fechaSubida: string;
  serie: string;
  version: string;
  secciones: Seccion[];
  datos: PrestacionDatos[];
};
type Workspace = {
  codDeis: string;
  establecimientoNombre: string;
  mes: number;
  version: string;
  serie: string;
  partes: Parte[];
};
type WorkspaceBackup = {
  formato: "remtool-construir-rem";
  versionFormato: 1;
  exportadoEn: string;
  workspace: Workspace;
};
type VistaSeccion = {
  id: number;
  hoja: string;
  hojaCodigo: string;
  hojaOrden: number;
  seccion: string;
  codigo: string;
  orden: number;
  html: string;
  valores: Record<string, string>;
};
type Revision = {
  analisis?: { errores?: string[]; advertencias?: string[] };
  secciones?: VistaSeccion[];
};

const MESES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

function leerWorkspace(): Workspace | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Workspace;
    if (!parsed || !Array.isArray(parsed.partes)) return null;
    return parsed;
  } catch {
    return null;
  }
}

function formatFecha(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString("es-CL");
}

function claveSeccion(hojaCodigo: string | undefined, codigo: string) {
  return `${hojaCodigo ?? ""}|${codigo}`;
}

function agruparSeccionesPorHoja(secciones: Seccion[]) {
  const groups = new Map<string, { codigo: string; nombre: string; orden: number; secciones: Seccion[] }>();
  for (const seccion of secciones) {
    const codigo = seccion.hojaCodigo || seccion.hoja;
    const current = groups.get(codigo) ?? {
      codigo,
      nombre: seccion.hoja,
      orden: seccion.hojaOrden ?? Number.MAX_SAFE_INTEGER,
      secciones: [],
    };
    current.secciones.push(seccion);
    groups.set(codigo, current);
  }
  return Array.from(groups.values())
    .map((hoja) => ({
      ...hoja,
      secciones: hoja.secciones.slice().sort((a, b) => a.orden - b.orden || a.codigo.localeCompare(b.codigo)),
    }))
    .sort((a, b) => a.orden - b.orden || a.codigo.localeCompare(b.codigo));
}

function descargar(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

function validarWorkspaceImportado(value: unknown): Workspace | null {
  if (!value || typeof value !== "object") return null;
  const envelope = value as Partial<WorkspaceBackup> & { workspace?: unknown };
  const candidate = envelope.workspace && typeof envelope.workspace === "object" ? envelope.workspace : value;
  if (!candidate || typeof candidate !== "object") return null;
  const workspace = candidate as Partial<Workspace>;
  if (
    typeof workspace.codDeis !== "string" ||
    typeof workspace.mes !== "number" ||
    workspace.mes < 1 ||
    workspace.mes > 12 ||
    typeof workspace.version !== "string" ||
    typeof workspace.serie !== "string" ||
    !Array.isArray(workspace.partes) ||
    workspace.partes.length === 0
  ) return null;
  if (workspace.serie !== "A") return null;
  if (workspace.partes.some((parte) => (
    !parte ||
    typeof parte !== "object" ||
    typeof parte.id !== "string" ||
    typeof parte.nombre !== "string" ||
    typeof parte.version !== "string" ||
    !Array.isArray(parte.secciones) ||
    !Array.isArray(parte.datos)
  ))) return null;
  return workspace as Workspace;
}

export default function ConstruirREMPage() {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const backupInputRef = useRef<HTMLInputElement>(null);
  const [establecimientos, setEstablecimientos] = useState<Establecimiento[]>([]);
  const [workspace, setWorkspace] = useState<Workspace | null>(null);
  const [latestVersion, setLatestVersion] = useState("");
  const [selectedCodDeis, setSelectedCodDeis] = useState("");
  const [selectedMes, setSelectedMes] = useState(0);
  const [nombreParte, setNombreParte] = useState("");
  const [expandedParts, setExpandedParts] = useState<Set<string>>(new Set());
  const [expandedSheets, setExpandedSheets] = useState<Set<string>>(new Set());
  const [expandedSections, setExpandedSections] = useState<Set<string | number>>(new Set());
  const [revision, setRevision] = useState<Revision | null>(null);
  const [loadingRevision, setLoadingRevision] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState("");
  const [storageError, setStorageError] = useState("");
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    setWorkspace(leerWorkspace());
    setHydrated(true);
    fetch(apiUrl("getEstablecimientos"))
      .then((response) => response.json())
      .then((data) => setEstablecimientos(Array.isArray(data) ? data : []))
      .catch(() => setEstablecimientos([]));
    fetch(apiUrl("construirREM/version-actual"))
      .then(async (response) => {
        if (!response.ok) throw new Error(await response.text());
        return response.json() as Promise<{ version?: string }>;
      })
      .then((data) => setLatestVersion(data.version ?? ""))
      .catch(() => setLatestVersion(""));
  }, []);

  useEffect(() => {
    if (!hydrated) return;
    if (!workspace) {
      localStorage.removeItem(STORAGE_KEY);
      return;
    }
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(workspace));
      setStorageError("");
    } catch {
      setStorageError("No fue posible guardar el armado en este navegador. Libere espacio e inténtelo nuevamente.");
    }
  }, [hydrated, workspace]);

  useEffect(() => {
    if (!workspace || workspace.partes.length === 0) {
      setRevision(null);
      setLoadingRevision(false);
      return;
    }

    const controller = new AbortController();
    setLoadingRevision(true);
    setError("");
    fetch(apiUrl("construirREM/revision"), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      signal: controller.signal,
      body: JSON.stringify({
        codDeis: workspace.codDeis,
        mes: workspace.mes,
        version: workspace.version,
        partes: workspace.partes.map((parte) => ({
          id: parte.id,
          nombre: parte.nombre,
          datos: parte.datos,
        })),
      }),
    })
      .then(async (response) => {
        if (!response.ok) throw new Error(await response.text());
        return response.json() as Promise<Revision>;
      })
      .then(setRevision)
      .catch((reason) => {
        if (reason?.name !== "AbortError") setError(reason instanceof Error ? reason.message : "No se pudo revisar el armado.");
      })
      .finally(() => setLoadingRevision(false));

    return () => controller.abort();
  }, [workspace]);

  const selectedEstablishmentName = useMemo(
    () => establecimientos.find((item) => item.codDeis === (workspace?.codDeis ?? selectedCodDeis))?.nombre ?? "",
    [establecimientos, selectedCodDeis, workspace?.codDeis]
  );
  const activeCodDeis = workspace?.codDeis ?? selectedCodDeis;
  const activeMes = workspace?.mes ?? selectedMes;
  const outdatedWorkspace = Boolean(workspace?.version && latestVersion && workspace.version !== latestVersion);
  const errores = revision?.analisis?.errores ?? [];
  const advertencias = revision?.analisis?.advertencias ?? [];
  const hasWorkspace = Boolean(workspace && workspace.partes.length > 0);
  const estructuraPorClave = useMemo(
    () => new Map((revision?.secciones ?? []).map((seccion) => [claveSeccion(seccion.hojaCodigo, seccion.codigo), seccion] as const)),
    [revision?.secciones]
  );
  const estructuraPorCodigo = useMemo(
    () => new Map((revision?.secciones ?? []).map((seccion) => [seccion.codigo, seccion] as const)),
    [revision?.secciones]
  );
  const hojasEstructuradas = useMemo(() => {
    const groups = new Map<string, { codigo: string; nombre: string; orden: number; secciones: VistaSeccion[] }>();
    for (const seccion of revision?.secciones ?? []) {
      const key = seccion.hojaCodigo || seccion.hoja;
      const current = groups.get(key) ?? { codigo: seccion.hojaCodigo, nombre: seccion.hoja, orden: seccion.hojaOrden, secciones: [] };
      current.secciones.push(seccion);
      groups.set(key, current);
    }
    return Array.from(groups.values())
      .map((hoja) => ({ ...hoja, secciones: hoja.secciones.slice().sort((a, b) => a.orden - b.orden || a.codigo.localeCompare(b.codigo)) }))
      .sort((a, b) => a.orden - b.orden || a.codigo.localeCompare(b.codigo));
  }, [revision?.secciones]);

  const prepararTablaHtml = (html: string, valores: Record<string, string>) => {
    if (typeof window === "undefined") return html;
    const documentHtml = new DOMParser().parseFromString(html, "text/html");
    documentHtml.querySelectorAll<HTMLElement>("tr.fila-titulo").forEach((fila) => fila.remove());
    documentHtml.querySelectorAll<HTMLElement>("[data-celda]").forEach((celda) => {
      const coordenada = celda.dataset.celda ?? "";
      if (Object.prototype.hasOwnProperty.call(valores, coordenada)) {
        celda.textContent = valores[coordenada];
        celda.classList.add("rem-valor-cargado");
      } else if (celda.dataset.tipo === "total" || celda.classList.contains("total")) {
        celda.textContent = "—";
      }
    });
    return documentHtml.body.innerHTML;
  };

  const togglePart = (id: string) => {
    setExpandedParts((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleSheet = (key: string) => {
    setExpandedSheets((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  };

  const toggleSection = (id: string | number) => {
    setExpandedSections((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleExportWorkspace = () => {
    if (!workspace || workspace.partes.length === 0) return;
    const backup: WorkspaceBackup = {
      formato: "remtool-construir-rem",
      versionFormato: 1,
      exportadoEn: new Date().toISOString(),
      workspace,
    };
    const fileName = "REMTool-" + workspace.codDeis + "-A" + String(workspace.mes).padStart(2, "0") + "-armado.json";
    descargar(new Blob([JSON.stringify(backup, null, 2)], { type: "application/json;charset=utf-8" }), fileName);
  };

  const handleImportWorkspace = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    try {
      const imported = validarWorkspaceImportado(JSON.parse(await file.text()));
      if (!imported) {
        setError("El archivo no contiene un armado Serie A válido.");
        return;
      }
      if (workspace && !window.confirm("Cargar este archivo reemplazará el armado actual en este navegador. ¿Desea continuar?")) return;
      setWorkspace(imported);
      setSelectedCodDeis("");
      setSelectedMes(0);
      setNombreParte("");
      setRevision(null);
      setError("");
      setStorageError("");
      setExpandedParts(new Set(imported.partes.map((parte) => parte.id)));
      setExpandedSheets(new Set());
      setExpandedSections(new Set());
    } catch {
      setError("No se pudo leer el archivo JSON del armado.");
    }
  };

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    if (!nombreParte.trim()) {
      setError("Ingrese un nombre identificador para la parte.");
      return;
    }
    if (!activeCodDeis || !activeMes) {
      setError("Seleccione el establecimiento y el mes antes de subir una parte.");
      return;
    }
    if (!file.name.toLowerCase().endsWith(".xlsm")) {
      setError("Sólo se permiten archivos con extensión .xlsm.");
      return;
    }
    if (workspace?.partes.some((parte) => parte.nombre.trim().toLowerCase() === nombreParte.trim().toLowerCase())) {
      setError("Ya existe una parte con ese nombre identificador.");
      return;
    }

    setUploading(true);
    setError("");
    try {
      const formData = new FormData();
      formData.append("archivo", file);
      formData.append("nombre", nombreParte.trim());
      const expectedVersion = workspace?.version || latestVersion;
      if (expectedVersion) formData.append("versionEsperada", expectedVersion);

      const response = await fetch(apiUrl("construirREM/parte"), { method: "POST", body: formData });
      if (!response.ok) throw new Error(await response.text());
      const parte = await response.json() as Parte;
      const nextWorkspace: Workspace = workspace ?? {
        codDeis: activeCodDeis,
        establecimientoNombre: selectedEstablishmentName,
        mes: activeMes,
        version: parte.version,
        serie: parte.serie,
        partes: [],
      };
      setWorkspace({ ...nextWorkspace, partes: [...nextWorkspace.partes, parte] });
      setNombreParte("");
      setExpandedParts((current) => new Set(current).add(parte.id));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "No se pudo subir la parte.");
    } finally {
      setUploading(false);
    }
  };

  const handleRemove = (id: string) => {
    if (!workspace) return;
    const partes = workspace.partes.filter((parte) => parte.id !== id);
    if (partes.length === 0) {
      setWorkspace(null);
      setSelectedCodDeis("");
      setSelectedMes(0);
    } else {
      setWorkspace({ ...workspace, partes });
    }
  };

  const handleReset = () => {
    if (hasWorkspace && !window.confirm("¿Reiniciar el armado actual? Se eliminarán sus partes guardadas en este navegador.")) return;
    setWorkspace(null);
    setSelectedCodDeis("");
    setSelectedMes(0);
    setNombreParte("");
    setRevision(null);
    setError("");
    setStorageError("");
    setExpandedParts(new Set());
    setExpandedSheets(new Set());
    setExpandedSections(new Set());
  };

  const handleExport = async () => {
    if (!workspace || workspace.partes.length === 0 || exporting) return;
    if (outdatedWorkspace) {
      setError(`El armado guardado usa ${workspace.version}, pero la última versión disponible es ${latestVersion}. Inicie un nuevo armado.`);
      return;
    }
    if (errores.length > 0 || advertencias.length > 0) {
      const confirmed = window.confirm(`El armado tiene ${errores.length} error(es) y ${advertencias.length} advertencia(s). ¿Desea exportarlo de todos modos?`);
      if (!confirmed) return;
    }

    setExporting(true);
    setError("");
    try {
      const response = await fetch(apiUrl("construirREM/exportar"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          codDeis: workspace.codDeis,
          mes: workspace.mes,
          version: workspace.version,
          partes: workspace.partes.map((parte) => ({ id: parte.id, nombre: parte.nombre, datos: parte.datos })),
        }),
      });
      if (!response.ok) throw new Error(await response.text());
      const disposition = response.headers.get("content-disposition") ?? "";
      const encodedName = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
      const plainName = disposition.match(/filename="?([^";]+)"?/i)?.[1];
      descargar(await response.blob(), encodedName ? decodeURIComponent(encodedName) : plainName ?? `${workspace.codDeis}A${String(workspace.mes).padStart(2, "0")}-construido.xlsm`);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "No se pudo exportar el armado.");
    } finally {
      setExporting(false);
    }
  };

  return (
    <div className="py-6 px-4 sm:px-6 max-w-[1100px] mx-auto">
      <Breadcrumbs items={[{ label: "Operación REM" }, { label: "Construir REM Serie A" }]} />
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div>
          <h1 className="text-2xl font-bold mb-1" style={{ color: "var(--text)" }}>Construir REM Serie A</h1>
          <p className="text-sm" style={{ color: "var(--text-light)" }}>Arme progresivamente el reporte de un establecimiento y mes a partir de sus partes.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button type="button" onClick={handleExportWorkspace} disabled={!hasWorkspace} className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed" style={{ color: "var(--text)", border: "1px solid var(--border)" }}>
            <Download size={15} /> Exportar armado JSON
          </button>
          <label className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium cursor-pointer" style={{ color: "var(--text)", border: "1px solid var(--border)" }}>
            <Upload size={15} /> Cargar armado JSON
            <input ref={backupInputRef} type="file" accept=".json,application/json" onChange={handleImportWorkspace} className="hidden" />
          </label>
          <button type="button" onClick={handleReset} className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium" style={{ color: "var(--text)", border: "1px solid var(--border)" }}>
            <RotateCcw size={15} /> Nuevo armado
          </button>
        </div>
      </div>

      <section className="rounded-xl border p-4 mb-5" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
            Establecimiento
            <select value={workspace?.codDeis ?? selectedCodDeis} disabled={hasWorkspace || uploading} onChange={(event) => setSelectedCodDeis(event.target.value)} className="px-3 py-2 rounded-lg text-sm" style={{ border: "1px solid var(--border)", background: "var(--bg)", color: "var(--text)" }}>
              <option value="">-- Seleccione establecimiento --</option>
              {establecimientos.map((item) => <option key={item.codDeis} value={item.codDeis}>{item.nombre}</option>)}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
            Mes del reporte
            <select value={workspace?.mes ?? selectedMes} disabled={hasWorkspace || uploading} onChange={(event) => setSelectedMes(Number(event.target.value))} className="px-3 py-2 rounded-lg text-sm" style={{ border: "1px solid var(--border)", background: "var(--bg)", color: "var(--text)" }}>
              <option value={0}>-- Seleccione mes --</option>
              {MESES.map((mes, index) => <option key={mes} value={index + 1}>{mes}</option>)}
            </select>
          </label>
        </div>
        {!workspace && <div className="mt-4 text-xs" style={{ color: "var(--text-light)" }}>Última versión disponible de Serie A: <strong style={{ color: "var(--primary)" }}>{latestVersion || "cargando..."}</strong></div>}
        {workspace && <div className="mt-4 flex flex-wrap gap-2 text-xs" style={{ color: "var(--text-light)" }}><span className="rounded-full px-2.5 py-1" style={{ background: "var(--accent-light)", color: "var(--primary)" }}>Serie {workspace.serie}</span><span className="rounded-full px-2.5 py-1" style={{ background: "var(--accent-light)", color: "var(--primary)" }}>Versión {workspace.version}</span><span>Última disponible: {latestVersion || "cargando..."}.</span></div>}
        {outdatedWorkspace && <div className="mt-3 rounded-lg border p-3 text-sm" style={{ color: "#92400e", borderColor: "#f59e0b", background: "#fffbeb" }}>La versión de este armado ya no es la última disponible. Reinícielo para comenzar con {latestVersion}.</div>}
      </section>

      <section className="rounded-xl border p-4 mb-5" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
        <div className="flex items-center gap-2 mb-3"><Upload size={17} style={{ color: "var(--primary)" }} /><h2 className="font-semibold" style={{ color: "var(--text)" }}>Agregar parte</h2></div>
        <div className="flex flex-col sm:flex-row gap-2">
          <input value={nombreParte} disabled={uploading || outdatedWorkspace || !activeMes || !activeCodDeis} onChange={(event) => setNombreParte(event.target.value)} placeholder="Nombre identificador, por ejemplo: Nutricionistas" className="flex-1 px-3 py-2 rounded-lg text-sm" style={{ border: "1px solid var(--border)", background: "var(--bg)", color: "var(--text)" }} />
          <label className={`inline-flex items-center justify-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold cursor-pointer ${uploading || outdatedWorkspace || !nombreParte.trim() || !activeMes || !activeCodDeis ? "opacity-50 cursor-not-allowed" : ""}`} style={{ background: "var(--primary)", color: "white" }}>
            {uploading ? <Loader2 size={16} className="animate-spin" /> : <FileSpreadsheet size={16} />}{uploading ? "Procesando..." : "Seleccionar XLSM"}
            <input ref={fileInputRef} type="file" accept=".xlsm" disabled={uploading || outdatedWorkspace || !nombreParte.trim() || !activeMes || !activeCodDeis} onChange={handleUpload} className="hidden" />
          </label>
        </div>
      </section>

      {(error || storageError) && <div className="mb-5 rounded-lg border p-3 text-sm" style={{ color: "var(--error-dark)", borderColor: "var(--error)", background: "var(--error-bg)" }}>{error || storageError}</div>}

      <section className="rounded-xl border p-4 mb-5" style={{ background: "var(--surface)", borderColor: "var(--border)", display: "none" }}>
        <div className="flex items-center justify-between gap-3 mb-3"><div><h2 className="font-semibold" style={{ color: "var(--text)" }}>Partes incluidas ({workspace?.partes.length ?? 0})</h2><p className="text-xs mt-1" style={{ color: "var(--text-light)" }}>Expanda cada parte para revisar las secciones y prestaciones que aportó.</p></div></div>
        {!workspace || workspace.partes.length === 0 ? <p className="text-sm" style={{ color: "var(--text-light)" }}>Todavía no se han agregado partes.</p> : <div className="flex flex-col gap-2">
          {workspace.partes.map((parte) => {
            const expanded = expandedParts.has(parte.id);
            const totalPrestaciones = parte.secciones.reduce((total, seccion) => total + seccion.prestaciones.length, 0);
            return <div key={parte.id} className="rounded-lg border" style={{ borderColor: "var(--border)" }}>
              <div className="flex items-center gap-2 p-3"><button type="button" onClick={() => togglePart(parte.id)} className="flex min-w-0 flex-1 items-center gap-2 text-left"><ChevronDown size={16} className={`shrink-0 transition-transform ${expanded ? "rotate-180" : ""}`} style={{ color: "var(--primary)" }} /><span className="min-w-0"><strong className="block truncate" style={{ color: "var(--text)" }}>{parte.nombre}</strong><span className="block truncate text-xs" style={{ color: "var(--text-light)" }}>{parte.nombreArchivo} · {formatFecha(parte.fechaSubida)} · {parte.secciones.length} sección(es) · {totalPrestaciones} prestación(es)</span></span></button><CheckCircle2 size={17} style={{ color: "var(--success, #15803d)" }} /><button type="button" onClick={() => handleRemove(parte.id)} className="p-1 rounded hover:bg-black/5" aria-label={`Eliminar ${parte.nombre}`}><Trash2 size={15} style={{ color: "var(--error-dark)" }} /></button></div>
              {expanded && <div className="border-t px-3 py-3 flex flex-col gap-3" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>{parte.secciones.length === 0 ? <p className="text-xs" style={{ color: "var(--text-light)" }}>La parte no contiene prestaciones con datos.</p> : parte.secciones.slice().sort((a, b) => (a.hojaOrden ?? Number.MAX_SAFE_INTEGER) - (b.hojaOrden ?? Number.MAX_SAFE_INTEGER) || a.orden - b.orden || a.codigo.localeCompare(b.codigo)).map((seccion) => <div key={`${parte.id}-${seccion.codigo}`}><div className="text-sm font-semibold" style={{ color: "var(--text)" }}>{seccion.nombre || seccion.codigo}</div><div className="text-xs" style={{ color: "var(--text-light)" }}>Hoja: {seccion.hoja} · {seccion.prestaciones.length} prestación(es)</div><ul className="mt-1 pl-4 list-disc text-xs" style={{ color: "var(--text-light)" }}>{seccion.prestaciones.slice().sort((a, b) => (a.orden ?? Number.MAX_SAFE_INTEGER) - (b.orden ?? Number.MAX_SAFE_INTEGER) || a.codigo.localeCompare(b.codigo)).map((prestacion) => <li key={prestacion.codigo}>{prestacion.codigo}{prestacion.nombre ? ` · ${prestacion.nombre}` : ""}</li>)}</ul></div>)}</div>}
            </div>;
          })}
        </div>}
      </section>

      {hasWorkspace && <section className="rounded-xl border p-4 mb-5" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
        <div className="flex items-center justify-between gap-3 mb-4"><div><h2 className="font-semibold" style={{ color: "var(--text)" }}>Revisión del armado</h2><p className="text-xs mt-1" style={{ color: "var(--text-light)" }}>Los hallazgos se calculan sobre la suma de todas las partes.</p></div>{loadingRevision && <Loader2 size={18} className="animate-spin" style={{ color: "var(--primary)" }} />}</div>
        {!loadingRevision && errores.length === 0 && advertencias.length === 0 && <div className="flex items-center gap-2 text-sm" style={{ color: "var(--success, #15803d)" }}><CheckCircle2 size={17} /> Sin errores ni advertencias detectados.</div>}
        {errores.length > 0 && <div className="mb-4"><div className="flex items-center gap-2 mb-2 font-semibold text-sm" style={{ color: "var(--error-dark)" }}><XCircle size={17} /> Errores ({errores.length})</div><ul className="flex flex-col gap-2">{errores.map((item, index) => <li key={index} className="rounded-lg p-3 text-sm" style={{ color: "var(--error-dark)", border: "1px solid var(--error)", background: "var(--error-bg)" }}>{item}</li>)}</ul></div>}
       {advertencias.length > 0 && <div><div className="flex items-center gap-2 mb-2 font-semibold text-sm" style={{ color: "#a16207" }}><AlertTriangle size={17} /> Advertencias ({advertencias.length})</div><ul className="flex flex-col gap-2">{advertencias.map((item, index) => <li key={index} className="rounded-lg p-3 text-sm bg-amber-50 border border-amber-300 text-amber-800">{item}</li>)}</ul></div>}
      </section>}

      <section className="rounded-xl border p-4 mb-5 construir-rem-panel-consolidado" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
        <div className="mb-4"><h2 className="font-semibold" style={{ color: "var(--text)" }}>Partes y datos consolidados ({workspace?.partes.length ?? 0})</h2><p className="text-xs mt-1" style={{ color: "var(--text-light)" }}>Navegue por parte, hoja, sección y estructura para revisar el aporte y el resultado consolidado.</p></div>
        {!workspace || workspace.partes.length === 0 ? <p className="text-sm" style={{ color: "var(--text-light)" }}>Todavía no se han agregado partes.</p> : <div className="flex flex-col gap-2">
          {workspace.partes.map((parte) => {
            const parteAbierta = expandedParts.has(parte.id);
            const hojasParte = agruparSeccionesPorHoja(parte.secciones);
            const totalPrestaciones = parte.secciones.reduce((total, seccion) => total + seccion.prestaciones.length, 0);
            return <div key={parte.id} className="rounded-lg border" style={{ borderColor: "var(--border)" }}>
              <div className="flex items-center gap-2 p-3"><button type="button" onClick={() => togglePart(parte.id)} className="flex min-w-0 flex-1 items-center gap-2 text-left"><ChevronDown size={16} className={"shrink-0 transition-transform " + (parteAbierta ? "rotate-180" : "")} style={{ color: "var(--primary)" }} /><span className="min-w-0"><strong className="block truncate" style={{ color: "var(--text)" }}>{parte.nombre}</strong><span className="block truncate text-xs" style={{ color: "var(--text-light)" }}>{parte.nombreArchivo} · {formatFecha(parte.fechaSubida)} · {parte.secciones.length} sección(es) · {totalPrestaciones} prestación(es)</span></span></button><CheckCircle2 size={17} style={{ color: "var(--success, #15803d)" }} /><button type="button" onClick={() => handleRemove(parte.id)} className="p-1 rounded hover:bg-black/5" aria-label={"Eliminar " + parte.nombre}><Trash2 size={15} style={{ color: "var(--error-dark)" }} /></button></div>
              {parteAbierta && <div className="border-t px-3 py-3 flex flex-col gap-2" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
                {hojasParte.length === 0 ? <p className="text-xs" style={{ color: "var(--text-light)" }}>La parte no contiene prestaciones con datos.</p> : hojasParte.map((hoja) => {
                  const hojaKey = parte.id + "|" + hoja.codigo;
                  const hojaAbierta = expandedSheets.has(hojaKey);
                  return <div key={hojaKey} className="rounded-lg border" style={{ borderColor: "var(--border)" }}>
                    <button type="button" onClick={() => toggleSheet(hojaKey)} className="flex w-full items-center gap-2 p-3 text-left"><ChevronDown size={15} className={"shrink-0 transition-transform " + (hojaAbierta ? "rotate-180" : "")} style={{ color: "var(--primary)" }} /><span><strong className="block" style={{ color: "var(--text)" }}>{hoja.nombre || hoja.codigo}</strong><span className="text-xs" style={{ color: "var(--text-light)" }}>{hoja.codigo} · {hoja.secciones.length} sección(es)</span></span></button>
                    {hojaAbierta && <div className="border-t p-2 flex flex-col gap-2" style={{ borderColor: "var(--border)", background: "var(--surface)" }}>
                      {hoja.secciones.map((seccion) => {
                        const structure = estructuraPorClave.get(claveSeccion(seccion.hojaCodigo, seccion.codigo)) ?? estructuraPorCodigo.get(seccion.codigo);
                        const sectionKey = parte.id + "|" + claveSeccion(seccion.hojaCodigo ?? seccion.hoja, seccion.codigo);
                        const seccionAbierta = expandedSections.has(sectionKey);
                        return <div key={hojaKey + "-" + seccion.codigo} className="rounded-lg border" style={{ borderColor: "var(--border)" }}>
                          <button type="button" onClick={() => toggleSection(sectionKey)} className="flex w-full items-center gap-2 p-3 text-left"><ChevronDown size={15} className={"shrink-0 transition-transform " + (seccionAbierta ? "rotate-180" : "")} style={{ color: "var(--primary)" }} /><span><strong className="block text-sm" style={{ color: "var(--text)" }}>{seccion.nombre || seccion.codigo}</strong><span className="text-xs" style={{ color: "var(--text-light)" }}>{seccion.codigo} · {seccion.prestaciones.length} prestación(es)</span></span></button>
                          {seccionAbierta && <div className="border-t p-2" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
                            <div className="mb-2 text-xs font-semibold" style={{ color: "var(--text-light)" }}>Estructura consolidada</div>
                            {structure ? <div className="overflow-x-auto"><div className="consulta-rem-tabla" dangerouslySetInnerHTML={{ __html: prepararTablaHtml(structure.html, structure.valores) }} /></div> : <p className="text-xs" style={{ color: "var(--text-light)" }}>La estructura consolidada estará disponible al finalizar la revisión.</p>}
                            <div className="mt-3 text-xs font-semibold" style={{ color: "var(--text-light)" }}>Prestaciones incluidas en esta parte</div>
                            <ul className="mt-1 pl-4 list-disc text-xs" style={{ color: "var(--text-light)" }}>{seccion.prestaciones.slice().sort((a, b) => (a.orden ?? Number.MAX_SAFE_INTEGER) - (b.orden ?? Number.MAX_SAFE_INTEGER) || a.codigo.localeCompare(b.codigo)).map((prestacion) => <li key={prestacion.codigo}>{prestacion.codigo}{prestacion.nombre ? " · " + prestacion.nombre : ""}</li>)}</ul>
                          </div>}
                        </div>;
                      })}
                    </div>}
                  </div>;
                })}
              </div>}
            </div>;
          })}
        </div>}
      </section>

      {false && hasWorkspace && revision && <section className="rounded-xl border p-4 mb-5" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
        <div className="mb-4"><h2 className="font-semibold" style={{ color: "var(--text)" }}>Datos consolidados por estructura</h2><p className="text-xs mt-1" style={{ color: "var(--text-light)" }}>Las hojas y secciones siguen el orden definido para la versión del REM. Expanda una sección para ver sus filas, columnas, celdas y totales.</p></div>
        {hojasEstructuradas.length === 0 ? <p className="text-sm" style={{ color: "var(--text-light)" }}>No hay estructura disponible para esta versión.</p> : <div className="flex flex-col gap-2">
          {hojasEstructuradas.map((hoja) => {
            const hojaKey = hoja.codigo || hoja.nombre;
            const hojaAbierta = expandedSheets.has(hojaKey);
            return <div key={hojaKey} className="rounded-lg border" style={{ borderColor: "var(--border)" }}>
              <button type="button" onClick={() => toggleSheet(hojaKey)} className="flex w-full items-center gap-2 p-3 text-left"><ChevronDown size={16} className={`shrink-0 transition-transform ${hojaAbierta ? "rotate-180" : ""}`} style={{ color: "var(--primary)" }} /><span><strong className="block" style={{ color: "var(--text)" }}>{hoja.nombre || hoja.codigo}</strong><span className="text-xs" style={{ color: "var(--text-light)" }}>{hoja.codigo} · {hoja.secciones.length} sección(es)</span></span></button>
              {hojaAbierta && <div className="border-t p-2 flex flex-col gap-2" style={{ borderColor: "var(--border)", background: "var(--bg)" }}>
                {hoja.secciones.map((seccion) => {
                  const seccionAbierta = expandedSections.has(seccion.id);
                  return <div key={`${hojaKey}-${seccion.id}`} className="rounded-lg border" style={{ borderColor: "var(--border)" }}>
                    <button type="button" onClick={() => toggleSection(seccion.id)} className="flex w-full items-center gap-2 p-3 text-left"><ChevronDown size={15} className={`shrink-0 transition-transform ${seccionAbierta ? "rotate-180" : ""}`} style={{ color: "var(--primary)" }} /><span><strong className="block text-sm" style={{ color: "var(--text)" }}>{seccion.seccion || seccion.codigo}</strong><span className="text-xs" style={{ color: "var(--text-light)" }}>{seccion.codigo}</span></span></button>
                    {seccionAbierta && <div className="border-t p-2 overflow-x-auto" style={{ borderColor: "var(--border)", background: "var(--surface)" }}><div className="consulta-rem-tabla" dangerouslySetInnerHTML={{ __html: prepararTablaHtml(seccion.html, seccion.valores) }} /></div>}
                  </div>;
                })}
              </div>}
            </div>;
          })}
        </div>}
      </section>}

      <div className="flex justify-end"><button type="button" onClick={handleExport} disabled={!hasWorkspace || loadingRevision || exporting} className="inline-flex items-center gap-2 px-5 py-3 rounded-lg text-sm font-semibold disabled:opacity-50 disabled:cursor-not-allowed" style={{ background: "var(--primary)", color: "white" }}>{exporting ? <Loader2 size={17} className="animate-spin" /> : <Download size={17} />}{exporting ? "Exportando..." : "Exportar XLSM consolidado"}</button></div>
    </div>
  );
}
