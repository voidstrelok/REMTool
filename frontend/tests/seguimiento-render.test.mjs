import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import path from "node:path";
import { createRequire, Module } from "node:module";
import { fileURLToPath } from "node:url";
import ts from "typescript";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../src");
const modules = new Map();
function load(filename) {
  if (modules.has(filename)) return modules.get(filename).exports;
  const nativeRequire = createRequire(filename);
  const module = new Module(filename);
  modules.set(filename, module);
  module.require = specifier => {
    if (specifier.endsWith(".css")) return {};
    if (!specifier.startsWith(".") && !specifier.startsWith("@/")) return nativeRequire(specifier);
    const resolved = specifier.startsWith("@/") ? path.join(root, specifier.slice(2)) : path.resolve(path.dirname(filename), specifier);
    const target = [resolved, `${resolved}.ts`, `${resolved}.tsx`].find(p => existsSync(p));
    return target ? load(target) : nativeRequire(specifier);
  };
  module._compile(ts.transpileModule(readFileSync(filename, "utf8"), { compilerOptions: {
    target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, esModuleInterop: true,
  } }).outputText, filename);
  return module.exports;
}
const { Detalle, IndicadoresTabla, ResumenPanel } = load(path.join(root, "app/components/seguimiento/Seguimiento.tsx"));
const evaluacion = { mesCorte: 7, mesEvaluacion: 7, ultimoMesDatos: 7, provisional: false, tieneDatos: true,
  tieneRegistroPeriodo: true, numerador: 0, denominador: 100, meta: 80, resultado: 0, cumplimiento: 0, esperado: 46.7, brecha: -46.7, estado: "Critico", unidad: "%" };
const indicador = { id: 1, nombre: "Indicador de prueba", año: 2026, orden: 1, tipoindicador: 2, mensual: true, isTasa: false,
  isColaborativo: false, isDenFijo: false, esPeriodoOctubreSep: false, detalle: "Descripción real del indicador", peso: .5, ajuste: 0, aporte: 0,
  avance: 0, numerador: 0, denominador: 100, meta: .8, actual: 0, evaluacion, evolucion: [evaluacion], resultados: [],
  establecimientos: [{ id: 1, nombre: "Establecimiento con cero", sector: "Sector norte", evaluacion, meses: [] }], analisis: ["Producción registrada en cero."] };
test("detalle mantiene establecimientos con cero y explica resultado, esperado y brecha", () => {
  const html = renderToStaticMarkup(React.createElement(Detalle, { item: indicador }));
  for (const text of ["Establecimiento con cero", "0,0 %", "Esperado al corte", "Brecha al esperado", "Descripción real del indicador", "Evolución acumulada", "Ver datos de la evolución", "Sector norte"]) assert.ok(html.includes(text), text);
  assert.ok(!html.includes("NaN") && !html.includes("Infinity"));
});
test("colaborativo muestra aportes sin cumplimiento individual", () => {
  const html = renderToStaticMarkup(React.createElement(Detalle, { item: { ...indicador, isColaborativo: true } }));
  assert.ok(html.includes("Producción aportada")); assert.ok(html.includes("Denominador comunal"));
  assert.ok(!html.includes("<th>Cumplimiento</th>"));
});
test("tabla conserva sobrecumplimiento y enlaces con contexto", () => {
  const item = { ...indicador, evaluacion: { ...evaluacion, resultado: 192.9, cumplimiento: 241.125, estado: "Cumplida" } };
  const html = renderToStaticMarkup(React.createElement(IndicadoresTabla, { items: [item], f: { params: new URLSearchParams(), cambiar() {} }, detalleHref: () => "/Indicadores/DetalleIndicador?id=1&sectorId=2&mesCorte=7" }));
  assert.ok(html.includes("192,9 %") && html.includes("241,1 %"));
  assert.ok(html.includes('width:100%')); assert.ok(html.includes("sectorId=2&amp;mesCorte=7"));
  assert.ok(html.includes('aria-expanded="false"')); assert.ok(html.includes('data-label="Resultado"'));
});
test("resumen muestra sin datos y no confunde pendientes con alertas", () => {
  const resumen = { cumplimiento: 56.25, metodo: "Suma ponderada", total: 4, cumplidas: 1, enCurso: 1, enRiesgo: 0, criticos: 1, sinDatos: 1, sinDenominador: 0, sinMeta: 0 };
  const html = renderToStaticMarkup(React.createElement(ResumenPanel, { resumen }));
  for (const text of ["56,3 %", "Suma ponderada", "Sin datos", "Requieren atención"]) assert.ok(html.includes(text));
});
