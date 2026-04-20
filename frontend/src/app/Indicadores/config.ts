export type TipoIndicador = "MetasSanitarias" | "IAAPS" | "Convenios";

interface IndicadorConfig {
  tipoId: number;
  titulo: string;
  navLabel: string;
  mensajeCargando: string;
  mensajeVacio: string;
}

export const indicadoresConfig: Record<TipoIndicador, IndicadorConfig> = {
  MetasSanitarias: {
    tipoId: 2,
    titulo: "Resumen de Metas Sanitarias",
    navLabel: "Metas Sanitarias",
    mensajeCargando: "Cargando metas...",
    mensajeVacio: "No hay metas para mostrar.",
  },
  IAAPS: {
    tipoId: 3,
    titulo: "Resumen de IAAPS",
    navLabel: "IAAPS",
    mensajeCargando: "Cargando indicadores IAAPS...",
    mensajeVacio: "No hay indicadores para mostrar.",
  },
  Convenios: {
    tipoId: 1,
    titulo: "Resumen de Convenios",
    navLabel: "Convenios",
    mensajeCargando: "Cargando convenios...",
    mensajeVacio: "No hay convenios para mostrar.",
  },
};

export const TIPOS_INDICADOR = Object.keys(indicadoresConfig) as TipoIndicador[];

export function isTipoIndicador(value: string): value is TipoIndicador {
  return value in indicadoresConfig;
}
