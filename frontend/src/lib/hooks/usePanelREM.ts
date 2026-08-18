import { useCallback } from "react";

const STORAGE_KEY = "remtool_panel";

export interface ValidacionRem {
  campo: string;
  esperado: string;
  encontrado: string;
  mensaje: string;
}

export interface PanelAttachment {
  key: string;
  codDeis: string;
  nombreEstablecimiento: string;
  idSector: number;
  nombreSector: string;
  serie: string;
  version: string;
  mes: number;
  año: number;
  nombreArchivo: string;
  fechaSubida: string;
  validaciones: ValidacionRem[];
  errores: string[];
  advertencias: string[];
  datos: { prestacion: string; valores: string[] }[];
}

export interface PanelEntry {
  key: string;
  contexto?: {
    mes: number;
    año: number;
  };
  codDeis: string;
  nombreEstablecimiento: string;
  idSector: number;
  nombreSector: string;
  serie: string;
  version: string;
  mes: number;
  año: number;
  nombreArchivo: string;
  fechaSubida: string;
  incluidoEnResumen: boolean;
  validaciones: ValidacionRem[];
  errores: string[];
  advertencias: string[];
  datos: { prestacion: string; valores: string[] }[];
  complementarias: PanelAttachment[];
}

function readAll(): PanelEntry[] {
  if (typeof window === "undefined") return [];

  try {
    const raw = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "[]");
    if (!Array.isArray(raw)) return [];

    return raw.map((entry) => ({
      ...entry,
      incluidoEnResumen: entry.incluidoEnResumen !== false,
      validaciones: Array.isArray(entry.validaciones) ? entry.validaciones : [],
      errores: Array.isArray(entry.errores) ? entry.errores : [],
      advertencias: Array.isArray(entry.advertencias) ? entry.advertencias : [],
      datos: Array.isArray(entry.datos) ? entry.datos : [],
      complementarias: Array.isArray(entry.complementarias)
        ? entry.complementarias.map((attachment: PanelAttachment) => ({
          ...attachment,
          validaciones: Array.isArray(attachment.validaciones) ? attachment.validaciones : [],
          errores: Array.isArray(attachment.errores) ? attachment.errores : [],
          advertencias: Array.isArray(attachment.advertencias) ? attachment.advertencias : [],
          datos: Array.isArray(attachment.datos) ? attachment.datos : [],
        }))
        : [],
    })) as PanelEntry[];
  } catch {
    return [];
  }
}

export function usePanelREM() {
  const getEntries = useCallback((mes: number, año: number, serie?: string): PanelEntry[] =>
    readAll().filter((entry) => {
      if (entry.contexto) {
        return entry.contexto.mes === mes
          && entry.contexto.año === año
          && (!serie || entry.serie === serie);
      }
      return entry.mes === mes && entry.año === año && (!serie || entry.serie === serie);
    }), []);

  const getAllEntries = useCallback((): PanelEntry[] => readAll(), []);

  const upsertEntry = useCallback((entry: PanelEntry): void => {
    const rest = readAll().filter((current) => current.key !== entry.key);
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...rest, entry]));
  }, []);

  const removeEntry = useCallback((key: string): void => {
    const rest = readAll().filter((entry) => entry.key !== key);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(rest));
  }, []);

  const clearAll = useCallback((): void => {
    localStorage.removeItem(STORAGE_KEY);
  }, []);

  return { getEntries, getAllEntries, upsertEntry, removeEntry, clearAll };
}
