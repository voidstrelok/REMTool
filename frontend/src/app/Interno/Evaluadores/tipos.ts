/**
 * Definición de un evaluador. Para agregar uno nuevo: crear defs/<id>.ts con un `Evaluador`
 * y sumarlo a defs/index.ts. El motor (pantalla, puntaje, Excel, PDF y respaldo) es el mismo para todos.
 */
export type Tono = "ok" | "partial" | "bad" | "neu";

/** pts null = no entra al puntaje (NA, No evaluado). */
export type Opcion = { label: string; pts: number | null; tono?: Tono };

export type Item = {
  id: string;                     // estable: es la clave de la respuesta guardada
  n?: string;                     // número visible
  texto: string;
  grupo?: string;                 // encabezado que agrupa ítems consecutivos (ej. componente MAIS)
  detalles?: [string, string][];  // verificador, regulación, etc.
  obligatoria?: boolean;          // responder 0 puntos impide certificar
  opciones?: Opcion[];            // si no, las de la pauta
  detalleCasos?: number;          // máx. de notas por caso (solo registro, sin puntaje)
};

export type Seccion = {
  id: string;
  titulo: string;
  subtitulo?: string;
  corto?: string;
  /** Si existe, cada ítem se responde una vez por caso; el valor es la etiqueta de la lista de casos. */
  porCaso?: string;
  items: Item[];
};

export type Nivel = { minPct: number; label: string; tono: Tono };

export type Pauta = {
  id: string;
  titulo: string;
  tituloImpreso?: string;
  objetivo?: string;              // texto editable por evaluación, con este valor inicial
  ayuda?: string;
  opciones: Opcion[];
  /** "respondidos": solo lo respondido suma al máximo. "evaluables": todo lo que no sea NA suma al máximo. */
  base: "respondidos" | "evaluables";
  niveles?: Nivel[];              // de mayor a menor
  certifica?: { minPct: number }; // además exige todo respondido y ninguna obligatoria en 0
  comentario?: boolean;
  imprimirDetalles?: boolean;     // columnas de detalles en el PDF
  secciones: Seccion[];
};

export type Campo = { id: string; label: string; tipo?: "texto" | "area" | "fecha" | "establecimiento"; placeholder?: string; requerido?: boolean };
export type Tabla = { id: string; titulo: string; columnas: [string, string][]; firma?: boolean; copiarDe?: string };
export type Anexo = { titulo: string; opciones: string[]; items: string[] };

export type Registro = {
  id: string;
  actualizado: number;
  campos: Record<string, string>;
  tablas: Record<string, Record<string, string>[]>;
  resp: Record<string, number>;                         // item.id (o item.id|caso.id) → índice de opción
  obs: Record<string, string>;                          // item.id → observación
  casos: Record<string, string[]>;                      // item.id → notas por caso
  listas: Record<string, { id: string; nombre: string }[]>; // seccion.id → casos
  anexos: Record<string, number>;                       // "anexo-item" → índice de opción
  anexosObs: Record<string, string>;
  textos: Record<string, string>;                       // objetivo:<pauta>, comentario:<pauta>
};

export type Evaluador = {
  id: string;
  titulo: string;
  descripcion: string;
  campos: Campo[];
  tablas?: Tabla[];
  pautas: Pauta[];
  anexos?: Anexo[];
  /** Convierte respaldos .json del HTML antiguo. Devuelve null si no los reconoce. */
  importarLegado?: (data: unknown) => Partial<Registro>[] | null;
};
