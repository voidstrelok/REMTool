export type ConsolidadoFile = {
  series: string;
  year: number;
  file: string;
  generatedAt: string;
  months: number[];
  templateVersion: string;
  bytes: number;
  sha256: string;
  warnings: string[];
};

export function parseConsolidadoCatalog(value: unknown): ConsolidadoFile[] {
  if (!value || typeof value !== "object") throw new Error("El catálogo de descargas no es válido.");
  const catalog = value as Record<string, unknown>;
  if (catalog.schemaVersion !== 1 || !Array.isArray(catalog.files)) throw new Error("El catálogo de descargas no es compatible.");
  const seen = new Set<string>();
  const files = catalog.files.map((raw: unknown) => {
    if (!raw || typeof raw !== "object") throw new Error("El catálogo contiene una publicación inválida.");
    const item = raw as Record<string, unknown>;
    if (item.series !== "A" || !Number.isInteger(item.year) || Number(item.year) < 2000 || Number(item.year) > 2100 ||
        typeof item.file !== "string" || !new RegExp(`^${item.year}/A/consolidado-rem-a-${item.year}-[a-zA-Z0-9-]+\\.xlsx$`).test(item.file) ||
        typeof item.generatedAt !== "string" || !Number.isFinite(Date.parse(item.generatedAt)) ||
        !Array.isArray(item.months) || item.months.length === 0 || item.months.some(m => !Number.isInteger(m) || m < 1 || m > 12) ||
        typeof item.templateVersion !== "string" || !item.templateVersion.trim() ||
        typeof item.bytes !== "number" || !Number.isSafeInteger(item.bytes) || item.bytes <= 0 ||
        typeof item.sha256 !== "string" || !/^[a-f0-9]{64}$/i.test(item.sha256) ||
        !Array.isArray(item.warnings) || item.warnings.some(w => typeof w !== "string")) {
      throw new Error("El catálogo contiene una publicación inválida.");
    }
    const key = `${item.year}-${item.series}`;
    if (seen.has(key)) throw new Error("El catálogo contiene publicaciones duplicadas.");
    seen.add(key);
    return { ...item, months: [...new Set(item.months)].sort((a, b) => a - b) } as ConsolidadoFile;
  });
  return files.sort((a, b) => b.year - a.year || a.series.localeCompare(b.series));
}

export function consolidadoDownloadUrl(file: ConsolidadoFile): string {
  return `/consolidados/${file.file}`;
}
