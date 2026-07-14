const STORAGE_KEY = "remtool_panel";

export interface PanelEntry {
  key: string; // `${codDeis}_${serie}_${mes}_${año}` — unique identifier for upsert
  codDeis: string;
  nombreEstablecimiento: string;
  idSector: number;
  nombreSector: string;
  serie: string;
  version: string;
  mes: number;
  año: number;
  nombreArchivo: string;
  fechaSubida: string; // ISO string
  errores: string[];
  advertencias: string[];
  datos: { prestacion: string; valores: string[] }[];
}

function readAll(): PanelEntry[] {
  if (typeof window === "undefined") return [];
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "[]") as PanelEntry[];
  } catch {
    return [];
  }
}

export function usePanelREM() {
  const getEntries = (serie: string, mes: number, año: number): PanelEntry[] =>
    readAll().filter((e) => e.serie === serie && e.mes === mes && e.año === año);

  const getAllEntries = (): PanelEntry[] => readAll();

  const upsertEntry = (entry: PanelEntry): void => {
    const rest = readAll().filter((e) => e.key !== entry.key);
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...rest, entry]));
  };

  const clearAll = (): void => {
    localStorage.removeItem(STORAGE_KEY);
  };

  return { getEntries, getAllEntries, upsertEntry, clearAll };
}
