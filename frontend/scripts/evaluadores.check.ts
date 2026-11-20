// Reglas de puntaje de los evaluadores contra lo que hacían los HTML originales.
// Ejecutar: node --experimental-strip-types scripts/evaluadores.check.ts
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { avisos, nuevoRegistro, puntajePauta } from "../src/app/Interno/Evaluadores/puntaje.ts";
import type { Evaluador, Pauta } from "../src/app/Interno/Evaluadores/tipos.ts";

const json = (f: string) => JSON.parse(readFileSync(new URL(`../src/app/Interno/Evaluadores/defs/${f}.json`, import.meta.url), "utf8"));
const ev = (pautas: Pauta[]): Evaluador => ({ id: "t", titulo: "t", descripcion: "", campos: [], pautas });

// CPU: siempre sobre 25; NA vale 0. Excelente ≥ 24, Bueno ≥ 22.
{
  const p: Pauta = { id: "cpu", titulo: "", base: "evaluables", secciones: json("cpu-2026").secciones,
    opciones: [{ label: "Cumple", pts: 1 }, { label: "No cumple", pts: 0 }, { label: "No aplica", pts: 0, tono: "neu" }],
    niveles: [{ minPct: 96, label: "Excelente", tono: "ok" }, { minPct: 88, label: "Bueno", tono: "partial" }, { minPct: 0, label: "Deficiente", tono: "bad" }] };
  const r = nuevoRegistro(ev([p])), ids = p.secciones.flatMap(s => s.items.map(i => i.id));
  assert.equal(ids.length, 25);
  ids.forEach(id => (r.resp[id] = 0));
  assert.equal(puntajePauta(p, r).nivel?.label, "Excelente");
  r.resp[ids[0]] = 2; r.resp[ids[1]] = 1;
  let x = puntajePauta(p, r);
  assert.deepEqual([x.obt, x.max, x.pct, x.nivel?.label], [23, 25, 92, "Bueno"]);
  r.resp[ids[2]] = 1; r.resp[ids[3]] = 1;
  assert.equal(puntajePauta(p, r).nivel?.label, "Deficiente");
  delete r.resp[ids[4]];                       // sin responder: igual cuenta en el máximo
  assert.equal(puntajePauta(p, r).max, 25);
  assert.ok(avisos(ev([p]), r).some(w => w.includes("sin observación")));
}

// MAIS: NA se descuenta del máximo; certifica con todo respondido, ≥ 65% y ninguna obligatoria en 0.
{
  const p: Pauta = { id: "mais", titulo: "", base: "evaluables", opciones: [], certifica: { minPct: 65 }, secciones: json("mais").secciones };
  const r = nuevoRegistro(ev([p])), items = p.secciones.flatMap(s => s.items);
  items.forEach(i => (r.resp[i.id] = 0));      // opción 0 = puntaje máximo
  let x = puntajePauta(p, r);
  assert.equal(x.obt, x.max); assert.equal(x.certificacion?.ok, true);
  const conNa = items.find(i => i.opciones!.some(o => o.pts === null))!;
  r.resp[conNa.id] = conNa.opciones!.findIndex(o => o.pts === null);
  assert.equal(puntajePauta(p, r).max, x.max - 1);
  const oblig = items.find(i => i.obligatoria)!;
  r.resp[oblig.id] = oblig.opciones!.findIndex(o => o.pts === 0);
  x = puntajePauta(p, r);
  assert.equal(x.certificacion?.obligatorias, false); assert.equal(x.certificacion?.ok, false);
  delete r.resp[items[0].id];
  assert.equal(puntajePauta(p, r).certificacion?.completo, false);
}

// Chagas: solo lo respondido con 1 o 0 entra; Pauta B cuenta cada caso × criterio.
{
  const d = json("chagas");
  const A: Pauta = { id: "A", titulo: "", base: "respondidos", secciones: d.A.secciones,
    opciones: [{ label: "Cumple", pts: 1 }, { label: "No cumple", pts: 0 }, { label: "NA", pts: null }, { label: "No evaluado", pts: null }] };
  const B: Pauta = { id: "B", titulo: "", base: "respondidos", secciones: d.B.secciones,
    opciones: [{ label: "Sí", pts: 1 }, { label: "No", pts: 0 }, { label: "NA", pts: null }] };
  const r = nuevoRegistro(ev([A, B]));
  Object.assign(r.resp, { A1: 0, A2: 1, A3: 2, A4: 3 });
  const a = puntajePauta(A, r);
  assert.deepEqual([a.obt, a.max, a.pct, a.respondidos, a.total], [1, 2, 50, 4, 23]);
  assert.equal(a.secciones[0].pct, 50); assert.equal(a.secciones[1].pct, null);
  r.listas.gest = [{ id: "g1", nombre: "" }, { id: "g2", nombre: "" }];
  Object.assign(r.resp, { "G1|g1": 0, "G1|g2": 1, "G2|g1": 0 });
  const b = puntajePauta(B, r);
  assert.deepEqual([b.secciones[0].obt, b.secciones[0].max, b.secciones[0].total], [2, 3, 8]);
}

console.log("evaluadores: ok");
