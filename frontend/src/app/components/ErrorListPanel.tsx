"use client";

import { AlertTriangle, FileText, Loader2, Trash2, Upload, X, XCircle } from "lucide-react";
import type { PanelEntry } from "@/lib/hooks/usePanelREM";

interface Props {
  entry: PanelEntry | null;
  onClose: () => void;
  onRemove: () => void;
  onAttach?: () => void;
  onRemoveAttachment?: (attachmentKey: string) => void;
  attaching?: boolean;
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

export default function ErrorListPanel({ entry, onClose, onRemove, onAttach, onRemoveAttachment, attaching }: Props) {
  if (!entry) return null;

  const hasErrores = entry.errores.length > 0;
  const hasAdvertencias = entry.advertencias.length > 0;

  return (
    <>
      <div className="fixed inset-0 z-40 bg-black/30" onClick={onClose} aria-hidden />
      <aside className="fixed right-0 top-0 z-50 h-full w-full max-w-md flex flex-col shadow-xl" style={{ background: "var(--surface)", borderLeft: "1px solid var(--border)" }}>
        <div className="flex items-start justify-between gap-3 p-5" style={{ borderBottom: "1px solid var(--border)" }}>
          <div className="min-w-0">
            <h2 className="font-semibold text-base truncate" style={{ color: "var(--text)" }}>{entry.nombreEstablecimiento}</h2>
            <div className="flex items-center gap-1.5 mt-1 text-xs" style={{ color: "var(--text-light)" }}>
              <FileText size={12} />
              <span className="truncate">{entry.nombreArchivo}</span>
              <span>·</span>
              <span>{formatFecha(entry.fechaSubida)}</span>
            </div>
          </div>
          <button onClick={onClose} className="p-1 rounded hover:bg-black/5" aria-label="Cerrar"><X size={18} style={{ color: "var(--text-light)" }} /></button>
        </div>

        <div className="flex-1 overflow-y-auto p-5 flex flex-col gap-5">
          <section className="grid grid-cols-2 gap-2 text-xs">
            {[
              ["Serie", entry.serie],
              ["Versión", entry.version],
              ["Mes", String(entry.mes)],
              ["Año", String(entry.año)],
              ["CodDEIS", entry.codDeis],
              ["Sector", entry.nombreSector || "—"],
            ].map(([label, value]) => <div key={label} className="p-2 rounded-lg" style={{ background: "var(--bg)", border: "1px solid var(--border)" }}><div style={{ color: "var(--text-light)" }}>{label}</div><div className="font-semibold truncate" style={{ color: "var(--text)" }}>{value}</div></div>)}
          </section>

          {!entry.incluidoEnResumen && <div className="p-3 rounded-lg text-sm text-amber-800 bg-amber-50 border border-amber-300"><strong>Excluida del resumen.</strong> La planilla no coincide con uno o más filtros seleccionados.</div>}

          {entry.validaciones.length > 0 && <section><h3 className="font-semibold text-sm mb-2 text-amber-700">Inconsistencias de selección</h3><ul className="flex flex-col gap-2">{entry.validaciones.map((validation, index) => <li key={`${validation.campo}-${index}`} className="p-3 rounded-lg text-sm text-amber-800 bg-amber-50 border border-amber-300"><strong>{validation.campo}:</strong> {validation.mensaje}<div className="text-xs mt-1">Esperado: {validation.esperado} · Encontrado: {validation.encontrado}</div></li>)}</ul></section>}

          {hasErrores && <section><div className="flex items-center gap-2 mb-3"><XCircle size={16} style={{ color: "var(--error)" }} /><h3 className="font-semibold text-sm" style={{ color: "var(--error-dark)" }}>Errores ({entry.errores.length})</h3></div><ul className="flex flex-col gap-2">{entry.errores.map((error, index) => <li key={index} className="p-3 rounded-lg text-sm" style={{ background: "var(--error-bg)", border: "1px solid var(--error)", color: "var(--error-dark)" }}>{error}</li>)}</ul></section>}

          {hasAdvertencias && <section><div className="flex items-center gap-2 mb-3"><AlertTriangle size={16} className="text-amber-600" /><h3 className="font-semibold text-sm text-amber-700">Advertencias ({entry.advertencias.length})</h3></div><ul className="flex flex-col gap-2">{entry.advertencias.map((warning, index) => <li key={index} className="p-3 rounded-lg text-sm bg-amber-50 border border-amber-300 text-amber-800">{warning}</li>)}</ul></section>}

          {!hasErrores && !hasAdvertencias && <p className="text-sm" style={{ color: "var(--text-light)" }}>Sin errores ni advertencias detectados.</p>}

          <section>
            <div className="flex items-center justify-between gap-3 mb-2">
              <div>
                <h3 className="font-semibold text-sm" style={{ color: "var(--text)" }}>Planillas complementarias</h3>
                <p className="text-xs mt-1" style={{ color: "var(--text-light)" }}>Sus valores se sumarán a esta planilla al descargar.</p>
              </div>
              {onAttach && entry.serie.toUpperCase() === "A" && <button type="button" onClick={onAttach} disabled={attaching} className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium disabled:opacity-50" style={{ color: "var(--primary)", border: "1px solid var(--primary)" }}>
                {attaching ? <Loader2 size={13} className="animate-spin" /> : <Upload size={13} />}
                {attaching ? "Adjuntando..." : "Adjuntar"}
              </button>}
            </div>
            {entry.complementarias.length === 0 ? <p className="text-xs" style={{ color: "var(--text-light)" }}>No hay planillas complementarias.</p> : <div className="flex flex-col gap-2">
              {entry.complementarias.map((attachment) => <div key={attachment.key} className="flex items-center justify-between gap-2 p-2 rounded-lg" style={{ background: "var(--bg)", border: "1px solid var(--border)" }}>
                <div className="min-w-0"><div className="flex items-center gap-1.5 text-sm"><FileText size={13} style={{ color: "var(--primary)" }} /><span className="truncate" style={{ color: "var(--text)" }}>{attachment.nombreArchivo}</span></div><div className="text-xs mt-1" style={{ color: attachment.errores.length > 0 ? "var(--error-dark)" : "var(--text-light)" }}>{attachment.errores.length > 0 ? `${attachment.errores.length} error(es)` : attachment.advertencias.length > 0 ? `${attachment.advertencias.length} advertencia(s)` : "Analizada"}</div></div>
                {onRemoveAttachment && <button type="button" onClick={() => onRemoveAttachment(attachment.key)} className="p-1 rounded hover:bg-black/5" aria-label={`Eliminar ${attachment.nombreArchivo}`}><Trash2 size={14} style={{ color: "var(--error-dark)" }} /></button>}
              </div>)}
            </div>}
          </section>
        </div>

        <div className="p-5" style={{ borderTop: "1px solid var(--border)" }}><button onClick={onRemove} className="flex items-center gap-2 px-3 py-2 rounded-lg text-sm" style={{ color: "var(--error-dark)", border: "1px solid var(--error)" }}><Trash2 size={15} />Eliminar de la revisión</button></div>
      </aside>
    </>
  );
}
