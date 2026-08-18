"use client";

import React, { useRef, useState } from "react";
import {
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Loader2,
  Upload,
  X,
  Minus,
} from "lucide-react";
import type { PanelEntry } from "@/lib/hooks/usePanelREM";

interface Establecimiento {
  id: number;
  nombre: string;
  codDeis: string;
}

interface Props {
  establecimiento: Establecimiento;
  entry: PanelEntry | null;
  uploading: boolean;
  uploadError?: string;
  onFileDrop: (file: File, codDeis: string) => void;
  onSelect: (entry: PanelEntry) => void;
  onRemove: () => void;
}

export default function EstablecimientoUploadCard({
  establecimiento,
  entry,
  uploading,
  uploadError,
  onFileDrop,
  onSelect,
  onRemove,
}: Props) {
  const [isDragging, setIsDragging] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files?.[0];
    if (file?.name.endsWith(".xlsm")) onFileDrop(file, establecimiento.codDeis);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) onFileDrop(file, establecimiento.codDeis);
    e.target.value = "";
  };

  type Status = "uploading" | "upload_error" | "empty" | "ok" | "warning" | "error";

  const status: Status = uploading
    ? "uploading"
    : uploadError
    ? "upload_error"
    : !entry
    ? "empty"
    : entry.errores.length > 0
    ? "error"
    : entry.advertencias.length > 0
    ? "warning"
    : "ok";

  const borderColor =
    status === "error"
      ? "var(--error)"
      : status === "upload_error"
      ? "var(--error)"
      : status === "warning"
      ? "#f59e0b"
      : status === "ok"
      ? "var(--success)"
      : isDragging
      ? "var(--primary)"
      : "var(--border)";

  const bgColor =
    status === "error"
      ? "var(--error-bg)"
      : status === "upload_error"
      ? "var(--error-bg)"
      : status === "warning"
      ? "#fffbeb"
      : status === "ok"
      ? "var(--success-bg)"
      : isDragging
      ? "var(--accent-light)"
      : "var(--surface)";

  return (
    <div
      className="relative p-4 rounded-xl flex flex-col gap-2 transition-colors"
      style={{
        border: `1px solid ${borderColor}`,
        background: bgColor,
        minHeight: 100,
      }}
      onDragOver={(e) => {
        e.preventDefault();
        setIsDragging(true);
      }}
      onDragLeave={() => setIsDragging(false)}
      onDrop={handleDrop}
    >
      {/* Remove button */}
      {entry && !uploading && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            onRemove();
          }}
          className="absolute top-2 right-2 p-1 rounded hover:bg-black/10 transition"
          title="Eliminar archivo"
          aria-label="Eliminar"
        >
          <X size={12} style={{ color: "var(--text-light)" }} />
        </button>
      )}

      <span
        className="text-sm font-medium leading-tight pr-5"
        style={{ color: "var(--text)" }}
      >
        {establecimiento.nombre}
      </span>
      <span className="text-xs" style={{ color: "var(--text-light)" }}>
        {establecimiento.codDeis}
      </span>

      {/* Status / action area */}
      <div className="mt-auto pt-1">
        {status === "uploading" && (
          <span
            className="flex items-center gap-1 text-xs"
            style={{ color: "var(--text-light)" }}
          >
            <Loader2 size={12} className="animate-spin" />
            Procesando...
          </span>
        )}

        {status === "empty" && (
          <button
            onClick={() => fileRef.current?.click()}
            className="flex items-center gap-1 text-xs font-medium hover:underline transition"
            style={{ color: "var(--text-light)" }}
          >
            <Upload size={12} />
            Subir archivo
          </button>
        )}

        {status === "upload_error" && (
          <span
            className="flex items-center gap-1 text-xs leading-tight"
            style={{ color: "var(--error-dark)" }}
          >
            <XCircle size={12} className="shrink-0" />
            <span className="line-clamp-2">{uploadError}</span>
          </span>
        )}

        {status === "ok" && (
          <span
            className="flex items-center gap-1 text-xs font-medium"
            style={{ color: "var(--success-text)" }}
          >
            <CheckCircle2 size={12} />
            Sin errores
          </span>
        )}

        {status === "warning" && (
          <button
            onClick={() => entry && onSelect(entry)}
            className="flex items-center gap-1 text-xs font-medium text-amber-700 hover:underline transition"
          >
            <AlertTriangle size={12} />
            {entry!.advertencias.length} advertencia
            {entry!.advertencias.length !== 1 ? "s" : ""}
          </button>
        )}

        {status === "error" && (
          <button
            onClick={() => entry && onSelect(entry)}
            className="flex items-center gap-1 text-xs font-medium hover:underline transition"
            style={{ color: "var(--error-dark)" }}
          >
            <XCircle size={12} />
            {entry!.errores.length} error
            {entry!.errores.length !== 1 ? "es" : ""}
          </button>
        )}
      </div>

      <input
        ref={fileRef}
        type="file"
        accept=".xlsm"
        className="hidden"
        onChange={handleFileChange}
      />
    </div>
  );
}
