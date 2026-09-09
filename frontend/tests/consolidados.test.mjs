import { test } from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("../src/lib/consolidados.ts", import.meta.url), "utf8");
const compiled = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
const { parseConsolidadoCatalog, consolidadoDownloadUrl } = await import(`data:text/javascript;base64,${Buffer.from(compiled).toString("base64")}`);
const item = { series: "A", year: 2026, file: "2026/A/consolidado-rem-a-2026-test.xlsx", generatedAt: "2026-09-09T12:00:00Z", months: [2, 1, 2], templateVersion: "v1", bytes: 2000, sha256: "a".repeat(64), warnings: [] };
const catalog = files => ({ schemaVersion: 1, files });
test("normalizes months and links directly to static download", () => {
  const files = parseConsolidadoCatalog(catalog([item]));
  assert.deepEqual(files[0].months, [1, 2]);
  assert.equal(consolidadoDownloadUrl(files[0]), "/consolidados/2026/A/consolidado-rem-a-2026-test.xlsx");
});
test("empty publications supported", () => assert.deepEqual(parseConsolidadoCatalog(catalog([])), []));
test("rejects paths outside the publication, inconsistent years and remote URLs", () => {
  for (const file of ["../../secrets.xlsx", "https://example.org/file.xlsx", "2025/A/consolidado-rem-a-2026-test.xlsx", "2026/A/consolidado-rem-a-2026-%2e%2e.xlsx"])
    assert.throws(() => parseConsolidadoCatalog(catalog([{ ...item, file }])));
});
test("rejects malformed metadata and duplicate publications", () => {
  for (const patch of [{ months: [0] }, { generatedAt: "invalid" }, { bytes: -1 }, { sha256: "" }, { warnings: null }])
    assert.throws(() => parseConsolidadoCatalog(catalog([{ ...item, ...patch }])));
  assert.throws(() => parseConsolidadoCatalog(catalog([item, item])));
  assert.throws(() => parseConsolidadoCatalog({ schemaVersion: 2, files: [] }));
});
