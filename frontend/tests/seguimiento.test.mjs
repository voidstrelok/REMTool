import { test } from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("../src/lib/seguimiento.ts", import.meta.url), "utf8");
const compiled = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
const { numero, valor, brecha, queryContexto, retornoSeguro, seleccionarIndicadores } = await import(`data:text/javascript;base64,${Buffer.from(compiled).toString("base64")}`);
test("unidades explícitas y valores superiores al 100%", () => {
  assert.equal(valor(192.9), "192,9 %"); assert.equal(valor(6.3, true), "6,3");
  assert.equal(brecha(-2.3), "-2,3 pp"); assert.equal(brecha(2.3, true), "+2,3 u.");
  for (const v of [null, undefined, NaN, Infinity]) assert.equal(numero(v), "—");
});
test("conserva contexto y fija el mismo corte en detalle y PDF", () => {
  assert.equal(queryContexto(new URLSearchParams("sectorId=0&establecimientoId=2&mesCorte=7&tipo=IAAPS"), 6).toString(), "establecimientoId=2&mesCorte=6");
});
test("retorno interno y compatibilidad con enlaces antiguos", () => {
  const path = "/Convenios/DetalleConvenio?id=11&sectorId=2&mesCorte=6";
  assert.equal(retornoSeguro(path, "/Indicadores"), path);
  assert.equal(retornoSeguro(encodeURIComponent(path), "/Indicadores"), path);
  for (const path of ["https://example.org", "//example.org", "javascript:alert(1)", "%2F%zz"])
    assert.equal(retornoSeguro(path, "/Indicadores"), "/Indicadores");
});
test("buscar sin acentos, filtrar estado y ordenar sin mezclar unidades", () => {
  const i = (id, nombre, isTasa, brecha, estado = "Critico") => ({ id, orden: id, nombre, isTasa, evaluacion: { estado, brecha, cumplimiento: brecha } });
  const items = [i(1, "Atención integral", false, -4), i(2, "Tasa de atención", true, -8), i(3, "Otro", false, null, "SinDatos"), i(4, "Prevención", false, -7)];
  assert.deepEqual(seleccionarIndicadores(items, "atencion", "", "").map(x => x.id), [1, 2]);
  assert.deepEqual(seleccionarIndicadores(items, "", "SinDatos", "").map(x => x.id), [3]);
  assert.deepEqual(seleccionarIndicadores(items, "", "", "brecha").map(x => x.id), [4, 1, 3, 2]);
  assert.deepEqual(seleccionarIndicadores(items, "", "", "cumplimiento").map(x => x.id), [2, 4, 1, 3]);
  assert.deepEqual(items.map(x => x.id), [1, 2, 3, 4]);
});
