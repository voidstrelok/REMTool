"use client";

import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  AlertTriangle,
  BarChart3,
  CheckCircle2,
  Download,
  FileText,
  Filter,
  FolderOpen,
  Loader2,
  Trash2,
  Upload,
  XCircle,
} from "lucide-react";
import ErrorListPanel from "@/app/components/ErrorListPanel";
import { usePanelREM, type PanelAttachment, type PanelEntry } from "@/lib/hooks/usePanelREM";
import { exportRevisionWord } from "@/lib/exportRevision";

const API = process.env.NEXT_PUBLIC_API ?? "";

interface PuntoResumenResultado {
  nombre: string;
  categoria: string;
  valor: number;
  planillasConsideradas: number;
}

interface ResumenResponse {
  serie: string;
  mes?: number;
  año?: number;
  cobertura: {
    planillasRecibidas: number;
    planillasConsideradas: number;
    planillasExcluidas: number;
    planillasConErrores: number;
    planillasConAdvertencias: number;
  };
  puntos: PuntoResumenResultado[];
  erroresCalculo: string[];
}

interface UploadItem {
  id: string;
  fileName: string;
  mes: number;
  year: number;
  status: "pendiente" | "analizando" | "revisado" | "error";
  error?: string;
  entry?: PanelEntry;
}

interface SectorOption {
  key: string;
  nombre: string;
}

const MESES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

function currentYear() {
  return new Date().getFullYear();
}

function previousPeriod() {
  const today = new Date();
  const isJanuary = today.getMonth() === 0;
  return {
    mes: isJanuary ? 12 : today.getMonth(),
    year: isJanuary ? today.getFullYear() - 1 : today.getFullYear(),
  };
}

function entryKey(serie: string, codDeis: string, mes: number, año: number) {
  return `${serie}_${codDeis}_${mes}_${año}`;
}

function statusLabel(item: UploadItem) {
  if (item.status === "pendiente") return "Pendiente";
  if (item.status === "analizando") return "Analizando...";
  if (item.status === "error") return item.error ?? "Error";
  if (item.entry && !item.entry.incluidoEnResumen) return "Revisado · excluido del resumen";
  return "Revisado";
}

function parseNumber(value: string): number | null {
  const normalized = value.trim().replace(",", ".");
  if (!normalized) return 0;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : null;
}

function combinarDatos(
  entries: Array<{ datos: { prestacion: string; valores: string[] }[] }>
) {
  const combined = new Map<string, string[]>();

  for (const entry of entries) {
    for (const dato of entry.datos ?? []) {
      const current = combined.get(dato.prestacion) ?? [];
      const length = Math.max(current.length, dato.valores.length);

      for (let index = 0; index < length; index += 1) {
        const existing = current[index] ?? "0";
        const incoming = dato.valores[index] ?? "0";
        const existingNumber = parseNumber(existing);
        const incomingNumber = parseNumber(incoming);

        if (existingNumber !== null && incomingNumber !== null) {
          current[index] = String(existingNumber + incomingNumber);
        } else if (current[index] === undefined) {
          current[index] = incoming;
        }
      }

      combined.set(dato.prestacion, current);
    }
  }

  return Array.from(combined.entries()).map(([prestacion, valores]) => ({ prestacion, valores }));
}

export default function PanelREMPage() {
  const { getAllEntries, upsertEntry, removeEntry, clearAll } = usePanelREM();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const attachmentInputRef = useRef<HTMLInputElement>(null);

  const [selectedMes, setSelectedMes] = useState(() => previousPeriod().mes);
  const [selectedAño, setSelectedAño] = useState(() => previousPeriod().year);
  const [selectedSector, setSelectedSector] = useState("");
  const [sectores, setSectores] = useState<{ id: number; nombre: string }[]>([]);
  const [allEntries, setAllEntries] = useState<PanelEntry[]>([]);
  const [resumenes, setResumenes] = useState<ResumenResponse[]>([]);
  const [uploadQueue, setUploadQueue] = useState<UploadItem[]>([]);
  const [resumenLoading, setResumenLoading] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [selectedEntry, setSelectedEntry] = useState<PanelEntry | null>(null);
  const [attachmentTargetKey, setAttachmentTargetKey] = useState<string | null>(null);
  const [attachingKey, setAttachingKey] = useState<string | null>(null);
  const [downloadingKey, setDownloadingKey] = useState<string | null>(null);

  const yearsRange = useMemo(
    () => Array.from({ length: 4 }, (_, index) => currentYear() - 1 + index),
    []
  );

  const periodEntries = useMemo(
    () => allEntries.filter((entry) => {
      if (entry.contexto) {
        return entry.contexto.mes === selectedMes && entry.contexto.año === selectedAño;
      }
      return entry.mes === selectedMes && entry.año === selectedAño;
    }),
    [allEntries, selectedAño, selectedMes]
  );

  const sectorOptions = useMemo<SectorOption[]>(() => {
    const options = new Map<string, string>();
    sectores.forEach((sector) => options.set(String(sector.id), sector.nombre));
    allEntries.forEach((entry) => {
      if (!entry.nombreSector) return;
      const key = entry.idSector ? String(entry.idSector) : `nombre:${entry.nombreSector}`;
      if (!options.has(key)) options.set(key, entry.nombreSector);
    });
    return Array.from(options.entries())
      .map(([key, nombre]) => ({ key, nombre }))
      .sort((a, b) => a.nombre.localeCompare(b.nombre, "es"));
  }, [allEntries, sectores]);

  const activeEntries = useMemo(
    () => periodEntries.filter((entry) => {
      if (!selectedSector) return true;
      const entryKey = entry.idSector ? String(entry.idSector) : `nombre:${entry.nombreSector || ""}`;
      return entryKey === selectedSector;
    }),
    [periodEntries, selectedSector]
  );

  const groupedActiveEntries = useMemo(() => {
    const groups = new Map<string, { nombre: string; series: Map<string, { nombre: string; entries: PanelEntry[] }> }>();
    activeEntries.forEach((entry) => {
      const nombre = entry.nombreSector || "Sector no identificado";
      const key = entry.idSector ? String(entry.idSector) : `nombre:${nombre}`;
      if (!groups.has(key)) groups.set(key, { nombre, series: new Map() });
      const sectorGroup = groups.get(key)!;
      const serieNombre = entry.serie || "Serie no identificada";
      if (!sectorGroup.series.has(serieNombre)) sectorGroup.series.set(serieNombre, { nombre: serieNombre, entries: [] });
      sectorGroup.series.get(serieNombre)!.entries.push(entry);
    });
    return Array.from(groups.values())
      .map((group) => ({ ...group, series: Array.from(group.series.values()).sort((a, b) => a.nombre.localeCompare(b.nombre, "es")) }))
      .sort((a, b) => a.nombre.localeCompare(b.nombre, "es"));
  }, [activeEntries]);

  const refreshAllEntries = useCallback(() => {
    setAllEntries(getAllEntries());
  }, [getAllEntries]);

  useEffect(() => {
    refreshAllEntries();
  }, [refreshAllEntries]);

  useEffect(() => {
    fetch(`${API}getSectores`)
      .then((response) => response.json())
      .then((data) => setSectores(Array.isArray(data) ? data : []))
      .catch(() => setSectores([]));
  }, []);

  const recalcularResumen = useCallback(async (entries: PanelEntry[]) => {
    if (entries.length === 0) {
      setResumenes([]);
      return;
    }

    const porSerie = new Map<string, PanelEntry[]>();
    for (const entry of entries) {
      const serie = entry.serie || "Sin serie";
      if (!porSerie.has(serie)) porSerie.set(serie, []);
      porSerie.get(serie)!.push(entry);
    }

    setResumenLoading(true);
    const respuestas = await Promise.all(
      Array.from(porSerie.entries()).map(async ([serie, serieEntries]): Promise<ResumenResponse> => {
        try {
          const response = await fetch(`${API}ComputarResumen`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              serieNombre: serie,
              mes: selectedMes,
              año: selectedAño,
              entries: serieEntries.map((entry) => ({
                codDeis: entry.codDeis,
                incluidoEnResumen: entry.incluidoEnResumen,
                tieneErrores: entry.errores.length > 0,
                tieneAdvertencias: entry.advertencias.length > 0,
                datos: combinarDatos([entry, ...entry.complementarias]),
              })),
            }),
          });

          if (!response.ok) throw new Error(await response.text());
          return await response.json() as ResumenResponse;
        } catch (error) {
          return {
            serie,
            mes: selectedMes,
            año: selectedAño,
            cobertura: {
              planillasRecibidas: serieEntries.length,
              planillasConsideradas: 0,
              planillasExcluidas: serieEntries.length,
              planillasConErrores: serieEntries.filter((entry) => entry.errores.length > 0).length,
              planillasConAdvertencias: serieEntries.filter((entry) => entry.advertencias.length > 0).length,
            },
            puntos: [],
            erroresCalculo: [error instanceof Error ? error.message : "No se pudo calcular el resumen."],
          };
        }
      })
    );

    setResumenes(respuestas.sort((a, b) => a.serie.localeCompare(b.serie)));
    setResumenLoading(false);
  }, [selectedAño, selectedMes]);

  useEffect(() => {
    void recalcularResumen(activeEntries);
  }, [activeEntries, recalcularResumen]);

  useEffect(() => {
    if (selectedEntry && !activeEntries.some((entry) => entry.key === selectedEntry.key)) {
      setSelectedEntry(null);
    }
  }, [activeEntries, selectedEntry]);

  const updateQueueItem = (id: string, changes: Partial<UploadItem>) => {
    setUploadQueue((current) => current.map((item) => item.id === id ? { ...item, ...changes } : item));
  };

  const processFiles = useCallback(async (files: File[]) => {
    if (files.length === 0) return;

    const items = files.map((file, index) => ({
      id: `${Date.now()}-${index}-${file.name}`,
      fileName: file.name,
      mes: selectedMes,
      year: selectedAño,
      status: "pendiente" as const,
    }));
    setUploadQueue(items);

    for (let index = 0; index < files.length; index += 1) {
      const file = files[index];
      const item = items[index];

      if (!file.name.toLowerCase().endsWith(".xlsm")) {
        updateQueueItem(item.id, { status: "error", error: "Sólo se permiten archivos .xlsm." });
        continue;
      }

      updateQueueItem(item.id, { status: "analizando" });
      const formData = new FormData();
      formData.append("archivo", file);
      formData.append("mesEsperado", String(selectedMes));
      formData.append("añoEsperado", String(selectedAño));

      try {
        const response = await fetch(`${API}AnalizarREM`, { method: "POST", body: formData });
        if (!response.ok) throw new Error(await response.text());

        const data = await response.json();
        const serie = data.serie || "Sin serie";
        const codDeis = data.codDeis || "Sin CodDEIS";
        const mesDetectado = Number(data.mes) || selectedMes;
        const añoDetectado = Number(data.año) || selectedAño;
        const entry: PanelEntry = {
          key: entryKey(serie, codDeis, mesDetectado, añoDetectado),
          contexto: { mes: selectedMes, año: selectedAño },
          codDeis: data.codDeis,
          nombreEstablecimiento: data.nombreEstablecimiento,
          idSector: data.idSector,
          nombreSector: data.nombreSector,
          serie,
          version: data.version,
          mes: data.mes,
          año: data.año,
          nombreArchivo: file.name,
          fechaSubida: new Date().toISOString(),
          incluidoEnResumen: data.incluidoEnResumen !== false,
          validaciones: data.validaciones ?? [],
          errores: data.errores ?? [],
          advertencias: data.advertencias ?? [],
          datos: data.datos ?? [],
          complementarias: [],
        };

        upsertEntry(entry);
        updateQueueItem(item.id, { status: "revisado", entry });
      } catch (error) {
        updateQueueItem(item.id, {
          status: "error",
          error: error instanceof Error ? error.message : "No se pudo analizar la planilla.",
        });
      }
    }

    refreshAllEntries();
    if (fileInputRef.current) fileInputRef.current.value = "";
  }, [refreshAllEntries, selectedAño, selectedMes, upsertEntry]);

  const handleFileInput = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? []);
    void processFiles(files);
  };

  const handleDrop = (event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
    void processFiles(Array.from(event.dataTransfer.files ?? []));
  };

  const handleRemove = (entry: PanelEntry) => {
    removeEntry(entry.key);
    if (selectedEntry?.key === entry.key) setSelectedEntry(null);
    refreshAllEntries();
  };

  const handleDownload = async (entry: PanelEntry) => {
    if (entry.serie.toUpperCase() !== "A") return;
    if (downloadingKey) return;

    setDownloadingKey(entry.key);
    try {
      const response = await fetch(`${API}CompilarREM`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          codDEIS: entry.codDeis,
          mes: entry.mes,
          estrategias: [entry, ...entry.complementarias].map((source) => ({
            datos: source.datos,
            errores: source.errores.join("; "),
          })),
        }),
      });

      if (!response.ok) throw new Error(await response.text());

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      const disposition = response.headers.get("content-disposition") ?? "";
      const encodedName = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
      const plainName = disposition.match(/filename="?([^";]+)"?/i)?.[1];
      anchor.href = url;
      anchor.download = encodedName
        ? decodeURIComponent(encodedName)
        : plainName ?? `${entry.codDeis}A${String(entry.mes).padStart(2, "0")}-compilado.xlsm`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.URL.revokeObjectURL(url);
    } catch (error) {
      window.alert(error instanceof Error ? error.message : "No se pudo generar la planilla.");
    } finally {
      setDownloadingKey(null);
    }
  };

  const handleAttachmentRequest = (entry: PanelEntry) => {
    if (entry.serie.toUpperCase() !== "A" || attachingKey) return;
    setAttachmentTargetKey(entry.key);
    attachmentInputRef.current?.click();
  };

  const handleAttachmentChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    const targetKey = attachmentTargetKey;
    event.target.value = "";
    setAttachmentTargetKey(null);
    if (!file || !targetKey) return;

    if (!file.name.toLowerCase().endsWith(".xlsm")) {
      window.alert("Sólo se permiten archivos .xlsm.");
      return;
    }

    const base = getAllEntries().find((entry) => entry.key === targetKey);
    if (!base) {
      window.alert("No se encontró la planilla base seleccionada.");
      return;
    }

    setAttachingKey(targetKey);
    try {
      const formData = new FormData();
      formData.append("archivo", file);
      formData.append("serieEsperada", base.serie);
      formData.append("versionEsperada", base.version);
      formData.append("esComplementaria", "true");
      const response = await fetch(`${API}AnalizarREM`, { method: "POST", body: formData });
      if (!response.ok) throw new Error(await response.text());

      const data = await response.json();
      const serieCoincide = String(data.serie ?? "").trim().toUpperCase() === base.serie.trim().toUpperCase();
      const versionCoincide = String(data.version ?? "").trim().toUpperCase() === base.version.trim().toUpperCase();
      if (!serieCoincide || !versionCoincide) {
        const diferencias = [
          !serieCoincide ? `serie esperada: ${base.serie}; encontrada: ${data.serie ?? "sin serie"}` : "",
          !versionCoincide ? `versión esperada: ${base.version}; encontrada: ${data.version ?? "sin versión"}` : "",
        ].filter(Boolean).join(". ");
        throw new Error(`La planilla complementaria no coincide con la planilla base (${diferencias}).`);
      }
      const attachment: PanelAttachment = {
        key: `${base.key}_complementaria_${Date.now()}_${file.name}`,
        codDeis: data.codDeis ?? "",
        nombreEstablecimiento: data.nombreEstablecimiento ?? "",
        idSector: data.idSector ?? 0,
        nombreSector: data.nombreSector ?? "",
        serie: data.serie ?? "",
        version: data.version ?? "",
        mes: Number(data.mes) || 0,
        año: Number(data.año) || 0,
        nombreArchivo: file.name,
        fechaSubida: new Date().toISOString(),
        validaciones: data.validaciones ?? [],
        errores: data.errores ?? [],
        advertencias: data.advertencias ?? [],
        datos: data.datos ?? [],
      };

      upsertEntry({
        ...base,
        complementarias: [...base.complementarias, attachment],
      });
      refreshAllEntries();
      setSelectedEntry((current) => current?.key === base.key
        ? { ...base, complementarias: [...base.complementarias, attachment] }
        : current);
    } catch (error) {
      window.alert(error instanceof Error ? error.message : "No se pudo adjuntar la planilla.");
    } finally {
      setAttachingKey(null);
    }
  };

  const handleRemoveAttachment = (entry: PanelEntry, attachmentKey: string) => {
    const updated = {
      ...entry,
      complementarias: entry.complementarias.filter((attachment) => attachment.key !== attachmentKey),
    };
    upsertEntry(updated);
    refreshAllEntries();
    setSelectedEntry((current) => current?.key === entry.key ? updated : current);
  };

  const handleClearAll = () => {
    if (!window.confirm("¿Limpiar todas las revisiones guardadas en este navegador?")) return;
    clearAll();
    setAllEntries([]);
    setResumenes([]);
    setUploadQueue([]);
    setSelectedEntry(null);
  };

  const handleExportRevision = () => {
    const uploadFailures = uploadQueue
      .filter((item) => item.status === "error" && item.mes === selectedMes && item.year === selectedAño)
      .map((item) => ({ fileName: item.fileName, error: item.error ?? "No se pudo analizar la planilla." }));

    exportRevisionWord(activeEntries, {
      mes: selectedMes,
      year: selectedAño,
      nombreMes: MESES[selectedMes - 1],
      sectorNombre: selectedSector ? sectorOptions.find((sector) => sector.key === selectedSector)?.nombre : "Todos los sectores",
      uploadFailures,
    });
  };

  const coberturaTotal = useMemo(() => resumenes.reduce((total, resumen) => ({
    planillasRecibidas: total.planillasRecibidas + resumen.cobertura.planillasRecibidas,
    planillasConsideradas: total.planillasConsideradas + resumen.cobertura.planillasConsideradas,
    planillasExcluidas: total.planillasExcluidas + resumen.cobertura.planillasExcluidas,
    planillasConErrores: total.planillasConErrores + resumen.cobertura.planillasConErrores,
    planillasConAdvertencias: total.planillasConAdvertencias + resumen.cobertura.planillasConAdvertencias,
  }), {
    planillasRecibidas: 0,
    planillasConsideradas: 0,
    planillasExcluidas: 0,
    planillasConErrores: 0,
    planillasConAdvertencias: 0,
  }), [resumenes]);

  return (
    <div className="py-6 px-4 sm:px-6 max-w-[1400px] mx-auto">
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div>
          <h1 className="text-2xl font-bold mb-1" style={{ color: "var(--text)" }}>Panel de Revisión REM</h1>
          <p className="text-sm" style={{ color: "var(--text-light)" }}>
            Selecciona mes, año y sector. Luego carga una o varias planillas .xlsm; la serie y el establecimiento se detectan automáticamente.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button onClick={handleExportRevision} disabled={activeEntries.length === 0 && !uploadQueue.some((item) => item.status === "error" && item.mes === selectedMes && item.year === selectedAño)} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium disabled:opacity-50 disabled:cursor-not-allowed" style={{ color: "var(--primary)", border: "1px solid var(--primary)" }}>
            <Download size={13} /> Exportar revisión
          </button>
          <button onClick={handleClearAll} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium" style={{ color: "var(--error-dark)", border: "1px solid var(--error)" }}>
            <Trash2 size={13} /> Limpiar revisiones
          </button>
        </div>
      </div>

      <section className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6 p-4 rounded-xl" style={{ background: "var(--surface)", border: "1px solid var(--border)" }}>
        <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
          Mes
          <select value={selectedMes} onChange={(event) => setSelectedMes(Number(event.target.value))} className="px-3 py-2 rounded-lg text-sm" style={{ border: "1px solid var(--border)", background: "var(--bg)", color: "var(--text)" }}>
            {MESES.map((mes, index) => <option key={mes} value={index + 1}>{mes}</option>)}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
          Año
          <select value={selectedAño} onChange={(event) => setSelectedAño(Number(event.target.value))} className="px-3 py-2 rounded-lg text-sm" style={{ border: "1px solid var(--border)", background: "var(--bg)", color: "var(--text)" }}>
            {yearsRange.map((year) => <option key={year} value={year}>{year}</option>)}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
          Sector
          <select value={selectedSector} onChange={(event) => setSelectedSector(event.target.value)} className="px-3 py-2 rounded-lg text-sm" style={{ border: "1px solid var(--border)", background: "var(--bg)", color: "var(--text)" }}>
            <option value="">Todos los sectores</option>
            {sectorOptions.map((sector) => <option key={sector.key} value={sector.key}>{sector.nombre}</option>)}
          </select>
        </label>
      </section>

      <section
        onDragOver={(event) => { event.preventDefault(); setIsDragging(true); }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className="mb-6 p-9 rounded-xl flex flex-col items-center justify-center gap-3 text-center cursor-pointer"
        style={{ border: `2px dashed ${isDragging ? "var(--primary)" : "var(--border)"}`, background: isDragging ? "var(--accent-light)" : "var(--surface)" }}
      >
        <Upload size={30} style={{ color: "var(--primary)" }} />
        <p className="text-sm font-semibold" style={{ color: "var(--text)" }}>Arrastra una o varias planillas aquí o haz clic para seleccionarlas</p>
        <p className="text-xs" style={{ color: "var(--text-light)" }}>Se analizarán individualmente para {MESES[selectedMes - 1]} {selectedAño}.</p>
        <input ref={fileInputRef} type="file" accept=".xlsm" multiple className="hidden" onChange={handleFileInput} />
      </section>
      <input ref={attachmentInputRef} type="file" accept=".xlsm" className="hidden" onChange={handleAttachmentChange} />

      {uploadQueue.length > 0 && (
        <section className="mb-8 p-4 rounded-xl" style={{ background: "var(--surface)", border: "1px solid var(--border)" }}>
          <div className="flex items-center justify-between gap-3 mb-3">
            <h2 className="text-base font-semibold" style={{ color: "var(--text)" }}>Archivos procesados</h2>
            <span className="text-xs" style={{ color: "var(--text-light)" }}>{uploadQueue.length} archivo(s) en esta carga</span>
          </div>
          <div className="flex flex-col gap-2">
            {uploadQueue.map((item) => <div key={item.id} className="flex items-center justify-between gap-3 p-2 rounded-lg text-sm" style={{ background: "var(--bg)" }}><span className="flex items-center gap-2 min-w-0"><FileText size={15} style={{ color: "var(--primary)" }} /><span className="truncate" style={{ color: "var(--text)" }}>{item.fileName}</span></span><span className="flex items-center gap-1.5 text-xs shrink-0" style={{ color: item.status === "error" ? "var(--error-dark)" : item.status === "revisado" ? "var(--success-text)" : "var(--text-light)" }}>{item.status === "analizando" && <Loader2 size={13} className="animate-spin" />}{item.status === "error" && <XCircle size={13} />}{item.status === "revisado" && (item.entry?.incluidoEnResumen ? <CheckCircle2 size={13} /> : <AlertTriangle size={13} />)}{statusLabel(item)}</span></div>)}
          </div>
        </section>
      )}

      <section className="mb-8">
        <div className="flex items-center justify-between gap-3 mb-4">
          <div><h2 className="text-base font-semibold" style={{ color: "var(--text)" }}>Resumen del período</h2><p className="text-xs" style={{ color: "var(--text-light)" }}>{activeEntries.length} archivo(s) para {MESES[selectedMes - 1]} {selectedAño}{resumenLoading ? " · calculando..." : ""}</p></div>
          <BarChart3 size={18} style={{ color: "var(--primary)" }} />
        </div>

        {activeEntries.length === 0 ? <div className="p-6 rounded-xl text-sm" style={{ color: "var(--text-light)", background: "var(--surface)", border: "1px solid var(--border)" }}>Todavía no hay planillas cargadas para este período.</div> : <>
          <div className="grid grid-cols-2 sm:grid-cols-5 gap-2 mb-6" hidden>{[
            ["Recibidas", coberturaTotal.planillasRecibidas],
            ["Consideradas", coberturaTotal.planillasConsideradas],
            ["Excluidas", coberturaTotal.planillasExcluidas],
            ["Con errores", coberturaTotal.planillasConErrores],
            ["Con advertencias", coberturaTotal.planillasConAdvertencias],
          ].map(([label, value]) => <div key={label} className="p-3 rounded-lg" style={{ background: "var(--surface)", border: "1px solid var(--border)" }}><div className="text-xl font-bold" style={{ color: "var(--text)" }}>{value}</div><div className="text-xs" style={{ color: "var(--text-light)" }}>{label}</div></div>)}</div>
          <div className="flex flex-col gap-8">{resumenes.map((resumen) => { const grouped = new Map<string, PuntoResumenResultado[]>(); for (const punto of resumen.puntos) { const categoria = punto.categoria || "General"; if (!grouped.has(categoria)) grouped.set(categoria, []); grouped.get(categoria)!.push(punto); } return <div key={resumen.serie}><h3 className="text-sm font-semibold mb-3" style={{ color: "var(--primary)" }}>Serie {resumen.serie}</h3>{resumen.erroresCalculo.length > 0 && <div className="mb-3 p-3 rounded-lg text-sm" style={{ color: "var(--error-dark)", background: "var(--error-bg)", border: "1px solid var(--error)" }}>{resumen.erroresCalculo.map((error) => <div key={error}>{error}</div>)}</div>}<div className="flex flex-col gap-5">{Array.from(grouped.entries()).map(([categoria, puntos]) => <div key={categoria}><h4 className="text-xs font-semibold uppercase tracking-widest mb-2" style={{ color: "var(--text-light)" }}>{categoria}</h4><div className="flex flex-wrap gap-3">{puntos.map((punto) => <div key={punto.nombre} className="px-4 py-3 rounded-xl min-w-[150px]" style={{ background: "var(--surface)", border: "1px solid var(--border)" }}><div className="text-2xl font-bold tabular-nums" style={{ color: "var(--text)" }}>{punto.valor.toLocaleString("es-CL")}</div><div className="text-xs" style={{ color: "var(--text-light)" }}>{punto.nombre}</div><div className="text-[11px] mt-1" style={{ color: "var(--text-light)" }}>{punto.planillasConsideradas} planilla(s)</div></div>)}</div></div>)}</div></div>; })}</div>
        </>}
      </section>

      <section className="mb-8">
        <div className="flex items-center justify-between gap-3 mb-3">
          <div>
            <div className="flex items-center gap-2"><Filter size={17} style={{ color: "var(--primary)" }} /><h2 className="text-base font-semibold" style={{ color: "var(--text)" }}>Planillas que cumplen los filtros</h2></div>
            <p className="text-xs mt-1" style={{ color: "var(--text-light)" }}>Selecciona una planilla para revisar sus errores, advertencias y datos detectados.</p>
          </div>
          <span className="text-xs" style={{ color: "var(--text-light)" }}>{activeEntries.length} archivo(s)</span>
        </div>
        {activeEntries.length === 0 ? (
          <div className="p-6 rounded-xl text-sm" style={{ color: "var(--text-light)", background: "var(--surface)", border: "1px solid var(--border)" }}>
            No hay planillas que coincidan con el mes, año y sector seleccionados.
          </div>
        ) : (
          <div className="flex flex-col gap-5">
            {groupedActiveEntries.map((group) => (
              <div key={group.nombre}>
                <div className="flex items-center gap-2 mb-2"><FolderOpen size={15} style={{ color: "var(--primary)" }} /><h3 className="text-sm font-semibold" style={{ color: "var(--primary)" }}>{group.nombre}</h3><span className="text-xs" style={{ color: "var(--text-light)" }}>{group.series.reduce((total, serie) => total + serie.entries.length, 0)} archivo(s)</span></div>
                <div className="flex flex-col gap-2">
                  {group.series.map((serie) => {
                    const errores = serie.entries.filter((entry) => entry.errores.length > 0).length;
                    const advertencias = serie.entries.filter((entry) => entry.advertencias.length > 0).length;
                    return (
                      <details key={serie.nombre} className="rounded-xl overflow-hidden" style={{ background: "var(--surface)", border: "1px solid var(--border)" }}>
                        <summary className="cursor-pointer list-none p-3 flex items-center justify-between gap-3" style={{ color: "var(--text)" }}>
                          <span className="flex items-center gap-2 min-w-0"><FileText size={16} style={{ color: "var(--primary)" }} /><span className="font-semibold truncate">Serie {serie.nombre}</span></span>
                          <span className="flex items-center gap-2 text-xs shrink-0" style={{ color: "var(--text-light)" }}><span>{serie.entries.length} archivo(s)</span>{errores > 0 && <span className="text-red-700">{errores} con error(es)</span>}{advertencias > 0 && <span className="text-amber-700">{advertencias} con advertencia(s)</span>}</span>
                        </summary>
                        <div className="px-2 pb-2 flex flex-col gap-2" style={{ borderTop: "1px solid var(--border)" }}>
                          {serie.entries.map((entry) => {
                            const isSerieA = entry.serie.toUpperCase() === "A";
                            const isDownloading = downloadingKey === entry.key;
                            const isAttaching = attachingKey === entry.key;
                            return <div key={entry.key} onClick={() => setSelectedEntry(entry)} role="button" tabIndex={0} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") setSelectedEntry(entry); }} className="text-left p-3 rounded-lg flex items-center justify-between gap-3 cursor-pointer" style={{ background: entry.incluidoEnResumen ? "var(--bg)" : "#fffbeb", border: `1px solid ${entry.incluidoEnResumen ? "var(--border)" : "#f59e0b"}` }}>
                              <span className="flex items-center gap-3 min-w-0"><FileText size={15} style={{ color: "var(--primary)" }} /><span className="min-w-0"><span className="block text-sm font-semibold truncate" style={{ color: "var(--text)" }}>{entry.nombreArchivo}</span><span className="block text-xs truncate" style={{ color: "var(--text-light)" }}>{entry.nombreEstablecimiento || "Establecimiento no identificado"}{entry.complementarias.length > 0 ? ` · ${entry.complementarias.length} complementaria(s)` : ""}</span></span></span>
                              <span className="flex items-center gap-2 shrink-0"><span className="flex items-center gap-2">{!entry.incluidoEnResumen && <span className="text-xs text-amber-700">Excluida</span>}{entry.errores.length > 0 ? <XCircle size={17} style={{ color: "var(--error)" }} /> : entry.advertencias.length > 0 ? <AlertTriangle size={17} className="text-amber-600" /> : <CheckCircle2 size={17} style={{ color: "var(--success)" }} />}</span><span className="flex items-center gap-1.5" onClick={(event) => event.stopPropagation()}><button type="button" onClick={() => void handleDownload(entry)} disabled={!isSerieA || Boolean(downloadingKey) || Boolean(attachingKey)} title={isSerieA ? "Descargar REM consolidado" : "Disponible inicialmente sólo para Serie A"} className="flex items-center gap-1 px-2 py-1.5 rounded-lg text-xs font-medium disabled:opacity-50 disabled:cursor-not-allowed" style={{ color: "var(--primary)", border: "1px solid var(--primary)" }}>{isDownloading ? <Loader2 size={13} className="animate-spin" /> : <Download size={13} />}{isDownloading ? "Generando..." : "Descargar"}</button><button type="button" onClick={() => handleAttachmentRequest(entry)} disabled={!isSerieA || Boolean(attachingKey) || Boolean(downloadingKey)} title={isSerieA ? "Adjuntar y sumar planilla" : "Disponible inicialmente sólo para Serie A"} className="flex items-center gap-1 px-2 py-1.5 rounded-lg text-xs font-medium disabled:opacity-50 disabled:cursor-not-allowed" style={{ color: "var(--text)", border: "1px solid var(--border)" }}>{isAttaching ? <Loader2 size={13} className="animate-spin" /> : <Upload size={13} />}{isAttaching ? "Adjuntando..." : "Adjuntar / sumar"}</button></span></span>
                            </div>;
                          })}
                        </div>
                      </details>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {selectedEntry && <ErrorListPanel entry={selectedEntry} onClose={() => setSelectedEntry(null)} onRemove={() => handleRemove(selectedEntry)} onAttach={() => handleAttachmentRequest(selectedEntry)} onRemoveAttachment={(attachmentKey) => handleRemoveAttachment(selectedEntry, attachmentKey)} attaching={attachingKey === selectedEntry.key} />}
    </div>
  );
}
