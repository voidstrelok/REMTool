"use client";

import { AlertTriangle, XCircle, FileText, X } from "lucide-react";
import type { PanelEntry } from "@/lib/hooks/usePanelREM";

interface Props {
  entry: PanelEntry | null;
  onClose: () => void;
}

function formatFecha(iso: string): string {
  try {
    return new Date(iso).toLocaleString("es-CL", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

export default function ErrorListPanel({ entry, onClose }: Props) {
  if (!entry) return null;

  const hasErrores = entry.errores.length > 0;
  const hasAdvertencias = entry.advertencias.length > 0;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-40 bg-black/30"
        onClick={onClose}
        aria-hidden
      />

      {/* Panel */}
      <aside
        className="fixed right-0 top-0 z-50 h-full w-full max-w-md flex flex-col shadow-xl"
        style={{ background: "var(--surface)", borderLeft: "1px solid var(--border)" }}
      >
        {/* Header */}
        <div
          className="flex items-start justify-between gap-3 p-5"
          style={{ borderBottom: "1px solid var(--border)" }}
        >
          <div className="min-w-0">
            <h2 className="font-semibold text-base truncate" style={{ color: "var(--text)" }}>
              {entry.nombreEstablecimiento}
            </h2>
            <div className="flex items-center gap-1.5 mt-1 text-xs" style={{ color: "var(--text-light)" }}>
              <FileText size={12} />
              <span className="truncate">{entry.nombreArchivo}</span>
              <span>·</span>
              <span>{formatFecha(entry.fechaSubida)}</span>
            </div>
          </div>
          <button
            onClick={onClose}
            className="flex-shrink-0 p-1 rounded hover:bg-black/5 transition"
            aria-label="Cerrar"
          >
            <X size={18} style={{ color: "var(--text-light)" }} />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-5 flex flex-col gap-5">

          {/* Errores */}
          {hasErrores && (
            <section>
              <div className="flex items-center gap-2 mb-3">
                <XCircle size={16} style={{ color: "var(--error)" }} />
                <h3 className="font-semibold text-sm" style={{ color: "var(--error-dark)" }}>
                  Errores ({entry.errores.length})
                </h3>
              </div>
              <ul className="flex flex-col gap-2">
                {entry.errores.map((e, i) => (
                  <li
                    key={i}
                    className="p-3 rounded-lg text-sm"
                    style={{
                      background: "var(--error-bg)",
                      border: "1px solid var(--error)",
                      color: "var(--error-dark)",
                    }}
                  >
                    {e}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {/* Advertencias */}
          {hasAdvertencias && (
            <section>
              <div className="flex items-center gap-2 mb-3">
                <AlertTriangle size={16} className="text-amber-600" />
                <h3 className="font-semibold text-sm text-amber-700">
                  Advertencias ({entry.advertencias.length})
                </h3>
              </div>
              <ul className="flex flex-col gap-2">
                {entry.advertencias.map((a, i) => (
                  <li
                    key={i}
                    className="p-3 rounded-lg text-sm bg-amber-50 border border-amber-300 text-amber-800"
                  >
                    {a}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {!hasErrores && !hasAdvertencias && (
            <p className="text-sm" style={{ color: "var(--text-light)" }}>
              Sin errores ni advertencias detectados.
            </p>
          )}
        </div>
      </aside>
    </>
  );
}
