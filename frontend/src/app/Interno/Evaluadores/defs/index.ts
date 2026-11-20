import type { Evaluador } from "../tipos";
import cpu from "./cpu-2026";
import mais from "./mais";
import chagas from "./chagas";

// Evaluadores disponibles en el panel interno: agregar aquí los nuevos.
export const EVALUADORES: Evaluador[] = [cpu, mais, chagas];
export const evaluador = (id: string) => EVALUADORES.find(e => e.id === id);
