"use client";

import React, { useEffect, useState, useCallback, useRef } from "react";
import { Upload, CheckCircle2, XCircle, AlertTriangle, Minus, Loader2, Trash2 } from "lucide-react";
import ErrorListPanel from "@/app/components/ErrorListPanel";
import { usePanelREM, type PanelEntry } from "@/lib/hooks/usePanelREM";

const API = process.env.NEXT_PUBLIC_API;

interface Serie { id: number; nombre: string }
interface Sector { id: number; nombre: string }
interface Establecimiento {
  id: number;
  id_sector: number;
  nombre: string;
  codDeis: string;
  sector: Sector;
}

interface UploadStatus {
  fileName: string;
  state: "uploading" | "saved" | "error";
  error?: string;
}

const MESES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

function currentYear() { return new Date().getFullYear(); }

export default function PanelREMPage() {
  const { getEntries, upsertEntry, clearAll } = usePanelREM();

  const [series, setSeries] = useState<Serie[]>([]);
  const [establecimientos, setEstablecimientos] = useState<Establecimiento[]>([]);
  const [selectedSerie, setSelectedSerie] = useState<string>("");
  const [selectedMes, setSelectedMes] = useState<number>(new Date().getMonth() + 1);
  const [selectedAño, setSelectedAño] = useState<number>(currentYear());
  const [panelEntries, setPanelEntries] = useState<PanelEntry[]>([]);
  const [uploadStatuses, setUploadStatuses] = useState<UploadStatus[]>([]);
  const [selectedEntry, setSelectedEntry] = useState<PanelEntry | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Load series and establecimientos
  useEffect(() => {
    fetch(`${API}GetSeries`)
      .then((r) => r.json())
      .then((data: Serie[]) => {
        setSeries(data);
        if (data.length > 0) setSelectedSerie(data[0].nombre);
      })
      .catch(() => {});

    fetch(`${API}getEstablecimientos`)
      .then((r) => r.json())
      .then((data: Establecimiento[]) => setEstablecimientos(data))
      .catch(() => {});
  }, []);

  // Reload panel entries from localStorage when filters change
  const refreshEntries = useCallback(() => {
    setPanelEntries(getEntries(selectedSerie, selectedMes, selectedAño));
  }, [selectedSerie, selectedMes, selectedAño, getEntries]);

  useEffect(() => { refreshEntries(); }, [refreshEntries]);

  // --- File upload ---
  const processFiles = async (files: FileList | File[]) => {
    const fileArr = Array.from(files).filter((f) => f.name.endsWith(".xlsm"));
    if (fileArr.length === 0) return;

    setUploadStatuses(fileArr.map((f) => ({ fileName: f.name, state: "uploading" })));

    for (let i = 0; i < fileArr.length; i++) {
      const file = fileArr[i];
      const formData = new FormData();
      formData.append("archivo", file);

      try {
        const res = await fetch(`${API}AnalizarREM`, { method: "POST", body: formData });
        if (!res.ok) {
          const msg = await res.text().catch(() => `Error ${res.status}`);
          setUploadStatuses((prev) =>
            prev.map((s, idx) => idx === i ? { ...s, state: "error", error: msg } : s)
          );
          continue;
        }

        const data = await res.json();
        const entry: PanelEntry = {
          key: `${data.codDeis}_${data.serie}_${data.mes}_${data.año}`,
          codDeis: data.codDeis,
          nombreEstablecimiento: data.nombreEstablecimiento,
          idSector: data.idSector,
          nombreSector: data.nombreSector,
          serie: data.serie,
          version: data.version,
          mes: data.mes,
          año: data.año,
          nombreArchivo: file.name,
          fechaSubida: new Date().toISOString(),
          errores: data.errores ?? [],
          advertencias: data.advertencias ?? [],
          datos: data.datos ?? [],
        };

        upsertEntry(entry);
        setUploadStatuses((prev) =>
          prev.map((s, idx) => idx === i ? { ...s, state: "saved" } : s)
        );

        // Auto-select serie/mes/año from first uploaded file
        if (i === 0) {
          setSelectedSerie(data.serie);
          setSelectedMes(data.mes);
          setSelectedAño(data.año);
        }
      } catch {
        setUploadStatuses((prev) =>
          prev.map((s, idx) => idx === i ? { ...s, state: "error", error: "Error de conexión" } : s)
        );
      }
    }

    refreshEntries();
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) processFiles(e.target.files);
    e.target.value = "";
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    if (e.dataTransfer.files) processFiles(e.dataTransfer.files);
  };

  // --- Grid logic ---
  // Group establecimientos by sector
  const grouped = React.useMemo(() => {
    const map = new Map<string, Establecimiento[]>();
    for (const e of establecimientos) {
      const key = e.sector?.nombre ?? "Sin sector";
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(e);
    }
    return map;
  }, [establecimientos]);

  const getEntry = (codDeis: string) =>
    panelEntries.find((p) => p.codDeis === codDeis) ?? null;

  const statusOf = (entry: PanelEntry | null) => {
    if (!entry) return "none";
    if (entry.errores.length > 0) return "error";
    if (entry.advertencias.length > 0) return "warning";
    return "ok";
  };

  const handleClearAll = () => {
    if (confirm("¿Limpiar todos los datos del panel en este navegador?")) {
      clearAll();
      setPanelEntries([]);
      setUploadStatuses([]);
    }
  };

  const yearsRange = Array.from({ length: 4 }, (_, i) => currentYear() - 1 + i);

  return (
    <div className="py-6 px-4 sm:px-6 max-w-[1400px] mx-auto">
      {/* Header */}
      <div className="flex items-start justify-between mb-6 gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold mb-1" style={{ color: "var(--text)" }}>
            Panel de Revisión REM
          </h1>
          <p className="text-sm" style={{ color: "var(--text-light)" }}>
            Sube archivos .xlsm para revisar su estado. Los resultados se guardan en este navegador.
          </p>
        </div>
        <button
          onClick={handleClearAll}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition hover:bg-red-50"
          style={{ color: "var(--error-dark)", border: "1px solid var(--error)" }}
        >
          <Trash2 size={13} />
          Limpiar panel
        </button>
      </div>

      {/* Drop zone + status */}
      <div
        onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className="mb-6 flex flex-col items-center justify-center gap-2 p-8 rounded-xl cursor-pointer transition"
        style={{
          border: `2px dashed ${isDragging ? "var(--primary)" : "var(--border)"}`,
          background: isDragging ? "var(--accent-light)" : "var(--surface)",
        }}
      >
        <Upload size={24} style={{ color: isDragging ? "var(--primary)" : "var(--text-light)" }} />
        <p className="text-sm font-medium" style={{ color: "var(--text)" }}>
          Arrastra archivos .xlsm aquí o haz clic para seleccionar
        </p>
        <p className="text-xs" style={{ color: "var(--text-light)" }}>
          Puedes subir varios archivos a la vez. La serie, establecimiento y mes se detectan automáticamente.
        </p>
        <input
          ref={fileInputRef}
          type="file"
          accept=".xlsm"
          multiple
          className="hidden"
          onChange={handleFileInput}
        />
      </div>

      {/* Upload status list */}
      {uploadStatuses.length > 0 && (
        <div className="mb-6 flex flex-col gap-1.5">
          {uploadStatuses.map((s, i) => (
            <div
              key={i}
              className="flex items-center gap-2 text-sm px-3 py-2 rounded-lg"
              style={{
                background:
                  s.state === "uploading" ? "var(--surface)"
                  : s.state === "saved" ? "var(--success-bg)"
                  : "var(--error-bg)",
                border: `1px solid ${
                  s.state === "uploading" ? "var(--border)"
                  : s.state === "saved" ? "var(--success)"
                  : "var(--error)"
                }`,
              }}
            >
              {s.state === "uploading" && <Loader2 size={14} className="animate-spin" style={{ color: "var(--text-light)" }} />}
              {s.state === "saved" && <CheckCircle2 size={14} style={{ color: "var(--success-text)" }} />}
              {s.state === "error" && <XCircle size={14} style={{ color: "var(--error)" }} />}
              <span className="font-medium truncate" style={{ color: "var(--text)" }}>{s.fileName}</span>
              {s.state === "error" && s.error && (
                <span className="truncate" style={{ color: "var(--error-dark)" }}>— {s.error}</span>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-6 items-center">
        {/* Serie tabs */}
        <div className="flex rounded-lg overflow-hidden" style={{ border: "1px solid var(--border)" }}>
          {series.map((s) => (
            <button
              key={s.id}
              onClick={() => setSelectedSerie(s.nombre)}
              className="px-4 py-1.5 text-sm font-medium transition"
              style={{
                background: selectedSerie === s.nombre ? "var(--primary)" : "var(--surface)",
                color: selectedSerie === s.nombre ? "white" : "var(--text)",
              }}
            >
              {s.nombre}
            </button>
          ))}
        </div>

        {/* Mes */}
        <select
          value={selectedMes}
          onChange={(e) => setSelectedMes(Number(e.target.value))}
          className="px-3 py-1.5 rounded-lg text-sm"
          style={{ border: "1px solid var(--border)", background: "var(--surface)", color: "var(--text)" }}
        >
          {MESES.map((m, i) => (
            <option key={i + 1} value={i + 1}>{m}</option>
          ))}
        </select>

        {/* Año */}
        <select
          value={selectedAño}
          onChange={(e) => setSelectedAño(Number(e.target.value))}
          className="px-3 py-1.5 rounded-lg text-sm"
          style={{ border: "1px solid var(--border)", background: "var(--surface)", color: "var(--text)" }}
        >
          {yearsRange.map((y) => (
            <option key={y} value={y}>{y}</option>
          ))}
        </select>

        {/* Summary */}
        <span className="text-xs ml-auto" style={{ color: "var(--text-light)" }}>
          {panelEntries.filter((e) => e.errores.length === 0 && e.advertencias.length === 0).length} aprobados ·{" "}
          {panelEntries.filter((e) => e.errores.length > 0).length} con errores ·{" "}
          {panelEntries.filter((e) => e.errores.length === 0 && e.advertencias.length > 0).length} con advertencias ·{" "}
          {establecimientos.length - panelEntries.length} sin archivo
        </span>
      </div>

      {/* Establishment grid grouped by sector */}
      <div className="flex flex-col gap-8">
        {Array.from(grouped.entries()).map(([sectorNombre, estabs]) => (
          <section key={sectorNombre}>
            <h2 className="text-sm font-semibold uppercase tracking-wide mb-3" style={{ color: "var(--text-light)" }}>
              {sectorNombre}
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
              {estabs.map((e) => {
                const entry = getEntry(e.codDeis);
                const status = statusOf(entry);
                return (
                  <button
                    key={e.id}
                    onClick={() => entry && setSelectedEntry(entry)}
                    className="text-left p-4 rounded-xl transition flex flex-col gap-2"
                    style={{
                      border: `1px solid ${
                        status === "error" ? "var(--error)"
                        : status === "warning" ? "#f59e0b"
                        : status === "ok" ? "var(--success)"
                        : "var(--border)"
                      }`,
                      background:
                        status === "error" ? "var(--error-bg)"
                        : status === "warning" ? "#fffbeb"
                        : status === "ok" ? "var(--success-bg)"
                        : "var(--surface)",
                      cursor: entry ? "pointer" : "default",
                    }}
                  >
                    <span className="text-sm font-medium leading-tight" style={{ color: "var(--text)" }}>
                      {e.nombre}
                    </span>
                    <span className="text-xs" style={{ color: "var(--text-light)" }}>
                      {e.codDeis}
                    </span>
                    <StatusBadge status={status} entry={entry} />
                  </button>
                );
              })}
            </div>
          </section>
        ))}
      </div>

      {/* Error / warning detail panel */}
      {selectedEntry && (
        <ErrorListPanel entry={selectedEntry} onClose={() => setSelectedEntry(null)} />
      )}
    </div>
  );
}

function StatusBadge({ status, entry }: { status: string; entry: PanelEntry | null }) {
  if (status === "none") {
    return (
      <span className="flex items-center gap-1 text-xs font-medium" style={{ color: "var(--text-light)" }}>
        <Minus size={12} /> Sin archivo
      </span>
    );
  }
  if (status === "ok") {
    return (
      <span className="flex items-center gap-1 text-xs font-medium" style={{ color: "var(--success-text)" }}>
        <CheckCircle2 size={12} /> Aprobado
      </span>
    );
  }
  if (status === "warning") {
    return (
      <span className="flex items-center gap-1 text-xs font-medium text-amber-700">
        <AlertTriangle size={12} /> {entry!.advertencias.length} advertencia{entry!.advertencias.length !== 1 ? "s" : ""}
      </span>
    );
  }
  return (
    <span className="flex items-center gap-1 text-xs font-medium" style={{ color: "var(--error-dark)" }}>
      <XCircle size={12} /> {entry!.errores.length} error{entry!.errores.length !== 1 ? "es" : ""}
    </span>
  );
}
