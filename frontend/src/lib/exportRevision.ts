import type { PanelEntry } from "@/lib/hooks/usePanelREM";

interface UploadFailure {
  fileName: string;
  error: string;
}

interface ExportRevisionOptions {
  mes: number;
  year: number;
  nombreMes: string;
  sectorNombre?: string;
  uploadFailures?: UploadFailure[];
}

function escapeHtml(value: unknown): string {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

function issueBlock(title: string, items: string[], className: string): string {
  if (items.length === 0) return "";
  return `<div class="issue-group ${className}"><h3>${escapeHtml(title)} <span>${items.length}</span></h3><ul>${items.map((item) => `<li>${escapeHtml(item)}</li>`).join("")}</ul></div>`;
}

function entrySection(entry: PanelEntry, index: number): string {
  const estado = entry.incluidoEnResumen
    ? "Coincidente - incluida en el resumen"
    : "Inconsistente - excluida del resumen";
  const estadoClass = entry.incluidoEnResumen ? "ok" : "warning";
  const validaciones = entry.validaciones.map((validation) =>
    `${validation.campo}: ${validation.mensaje} (Esperado: ${validation.esperado} - Encontrado: ${validation.encontrado})`
  );
  const hallazgos = [
    issueBlock("Inconsistencias con los filtros", validaciones, "issue-validation"),
    issueBlock("Errores de revisión", entry.errores, "issue-error"),
    issueBlock("Advertencias", entry.advertencias, "issue-warning"),
  ].join("");

  return `
    <section class="file">
      <div class="file-header">
        <div class="file-heading"><h2>${escapeHtml(entry.nombreEstablecimiento || "Archivo sin nombre")}</h2></div>
      </div>
      ${hallazgos || '<div class="clean">Sin errores, advertencias ni inconsistencias de filtros.</div>'}
    </section><br>`;
}

export function exportRevisionWord(entries: PanelEntry[], options: ExportRevisionOptions): void {
  const sortedEntries = [...entries].sort((a, b) => b.fechaSubida.localeCompare(a.fechaSubida));
  const failures = options.uploadFailures ?? [];
  const fileSections = sortedEntries.map(entrySection).join("");
  const failureSections = failures.map((failure, index) => `
    <section class="file">
      <div class="file-header">
        <div class="file-heading"><div class="file-index">PLANILLA ${String(sortedEntries.length + index + 1).padStart(2, "0")}</div><h2>${escapeHtml(failure.fileName)}</h2></div>
        <div class="status error">No analizada</div>
      </div>
      ${issueBlock("Error de procesamiento", [failure.error], "issue-error")}
    </section>`).join("");
  const total = sortedEntries.length + failures.length;
  const included = sortedEntries.filter((entry) => entry.incluidoEnResumen).length;
  const excluded = total - included;
  const withErrors = sortedEntries.filter((entry) => entry.errores.length > 0).length + failures.length;
  const withWarnings = sortedEntries.filter((entry) => entry.advertencias.length > 0).length;

  const html = `<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <title>RevisiÃ³n de planillas REM</title>
  <style>
    @page { size: Letter; margin: 0.65in; }
    body { font-family: Aptos, Calibri, Arial, sans-serif; color: #243447; margin: 0; font-size: 10pt; line-height: 1.35; }
    .cover { background: #123b5d; color: #fff; padding: 26px 30px 24px; margin-bottom: 20px; }
    .eyebrow { color: #a7d8df; font-size: 8pt; font-weight: bold; letter-spacing: 1.4px; margin: 0 0 7px; text-transform: uppercase; }
    h1 { font-size: 24pt; line-height: 1.1; margin: 0 0 8px; }
    .subtitle { color: #d9eef0; font-size: 11pt; margin: 0; }
    .filter-line { color: #48677e; font-size: 9pt; margin: -12px 0 18px; }
    .summary { border: 1px solid #d8e3ea; background: #f7fafc; padding: 14px; margin-bottom: 24px; }
    .summary-title { color: #123b5d; font-size: 9pt; font-weight: bold; letter-spacing: 0.7px; margin: 0 0 10px; text-transform: uppercase; }
    .metrics { border-collapse: collapse; width: 100%; }
    .metric { border-right: 1px solid #d8e3ea; padding: 2px 12px; text-align: center; width: 20%; }
    .metric:last-child { border-right: 0; }
    .metric strong { color: #123b5d; display: block; font-size: 18pt; line-height: 1; }
    .metric span { color: #64748b; display: block; font-size: 8pt; margin-top: 4px; }
    .note { color: #718096; font-size: 8.5pt; margin: 12px 0 0; }
    .file { page-break-inside: avoid; border: 1px solid #d7e2e9; margin: 16px 0; padding: 0 18px 16px; }
    .file-header { border-bottom: 1px solid #d7e2e9; padding: 15px 0 12px; }
    .file-index { color: #1f7a8c; font-size: 8pt; font-weight: bold; letter-spacing: 1px; }
    h2 { color: #123b5d; font-size: 13pt; line-height: 1.2; margin: 3px 0 0; overflow-wrap: anywhere; }
    .status { display: inline-block; font-size: 8.5pt; font-weight: bold; margin-top: 8px; padding: 5px 8px; }
    .ok { background: #eaf7f0; color: #166534; }
    .warning { background: #fff6dc; color: #8a4b08; }
    .error { background: #fdecec; color: #9d2929; }
    .identity { background: #f7fafc; border-collapse: collapse; margin: 13px 0 15px; width: 100%; }
    .identity td { padding: 10px 12px; vertical-align: top; width: 33%; }
    .identity td + td { border-left: 1px solid #d7e2e9; }
    .identity span { color: #738496; display: block; font-size: 8pt; }
    .identity strong { color: #243447; display: block; font-size: 9.5pt; margin-top: 2px; }
    .issue-group { margin: 12px 0 0; padding: 9px 11px 7px; }
    .issue-group h3 { font-size: 9.5pt; margin: 0 0 5px; }
    .issue-group h3 span { border-radius: 10px; font-size: 8pt; margin-left: 4px; padding: 1px 6px; }
    .issue-error { background: #fff5f5; border-left: 3px solid #d9534f; color: #7f1d1d; }
    .issue-error h3 span { background: #f6d2d0; }
    .issue-warning { background: #fffbeb; border-left: 3px solid #e5a11a; color: #7c4a03; }
    .issue-warning h3 span, .issue-validation h3 span { background: #f5e4ad; }
    .issue-validation { background: #f4f8fb; border-left: 3px solid #5b8fa8; color: #36566a; }
    ul { margin: 4px 0 0; padding-left: 18px; } 
    li { margin: 4px 0; }
    .clean { background: #eef9f1; color: #22633b; font-size: 9pt; margin-top: 14px; padding: 9px 11px; }
  </style>
</head>
<body>
  ${options.sectorNombre ? `<p class="filter-line">Sector seleccionado: <strong>${escapeHtml(options.sectorNombre)}</strong></p>` : ""}
  <div class="cover"><p class="eyebrow">Panel REM DESAM Monte Patria- Informe de revisión</p><h1>Revisión de planillas REM</h1><p class="subtitle">Período de carga: <strong>${escapeHtml(options.nombreMes)} ${escapeHtml(options.year)}</strong></p></div>
  ${fileSections || "<p>No hay planillas analizadas para exportar.</p>"}
  ${failureSections}
</body>
</html>`;

  const blob = new Blob(["\ufeff", html], { type: "application/msword;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = `revision-rem-${options.year}-${String(options.mes).padStart(2, "0")}-${options.sectorNombre}.doc`;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}
