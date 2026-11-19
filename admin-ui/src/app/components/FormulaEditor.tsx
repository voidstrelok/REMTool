"use client";

import { useState } from "react";

// Mismo AST que interpreta IndicatorCalculationService (application/Admin).
export type FNode =
  | { type: "value"; prestacion: string; columna: number }
  | { type: "number"; value: number }
  | { type: "variable"; name: string; filters: Record<string, string | number> }
  | { type: "op"; op: Op; args: FNode[] };
type Op = "sum" | "sub" | "mul" | "div";

const ops: Record<Op, [string, string]> = { sum: ["Suma", "+"], sub: ["Resta", "−"], mul: ["Multiplicación", "×"], div: ["División", "÷"] };
const variables = ["FONASA", "DM2", "HTA", "EPOC"];
const types: [FNode["type"], string][] = [["value", "Prestación REM"], ["op", "Operación"], ["variable", "Población"], ["number", "Número"]];

const blank = (type: FNode["type"], from?: FNode): FNode =>
  type === "value" ? { type, prestacion: "", columna: 1 } :
  type === "number" ? { type, value: 1 } :
  type === "variable" ? { type, name: "FONASA", filters: {} } :
  { type: "op", op: "sum", args: from ? [from] : [blank("value")] };

export function parseFormula(text: string): FNode | null { try { const n = JSON.parse(text); return n && typeof n === "object" ? n as FNode : null; } catch { return null; } }

// El cálculo separa numerador/denominador sólo si la raíz es una división.
export function split(root: FNode): { num: FNode; den: FNode | null } { return root.type === "op" && root.op === "div" && root.args.length === 2 ? { num: root.args[0], den: root.args[1] } : { num: root, den: null }; }
const join = (num: FNode, den: FNode | null): FNode => den ? { type: "op", op: "div", args: [num, den] } : num;

export function problems(n: FNode, path = "Fórmula"): string[] {
  switch (n.type) {
    case "value": return [...(!n.prestacion.trim() ? [`${path}: falta el código de prestación.`] : []), ...(!(n.columna >= 1) ? [`${path}: la columna debe ser 1 o mayor.`] : [])];
    case "number": return Number.isFinite(n.value) ? [] : [`${path}: número inválido.`];
    case "variable": return variables.includes(n.name) ? [] : [`${path}: variable desconocida ${n.name}.`];
    case "op": return [
      ...(n.op === "sum" ? (n.args.length ? [] : [`${path}: la suma está vacía.`]) : n.args.length !== 2 ? [`${path}: ${ops[n.op]?.[0] ?? n.op} requiere exactamente 2 términos.`] : []),
      ...n.args.flatMap((a, i) => problems(a, `${path} › ${i + 1}`))];
    default: return [`${path}: tipo de nodo desconocido.`];
  }
}

// Texto legible; agrupa columnas consecutivas de la misma prestación (03500364·C4–13).
export function describe(n: FNode): string {
  switch (n.type) {
    case "value": return `${n.prestacion}·C${n.columna}`;
    case "number": return String(n.value);
    case "variable": { const f = Object.entries(n.filters ?? {}).map(([k, v]) => `${k}=${v}`).join(", "); return f ? `${n.name}{${f}}` : n.name; }
    case "op": {
      const parts: string[] = [];
      for (let i = 0; i < n.args.length; i++) {
        const a = n.args[i]; let j = i;
        if (n.op === "sum" && a.type === "value") while (j + 1 < n.args.length) { const b = n.args[j + 1]; if (b.type === "value" && b.prestacion === a.prestacion && b.columna === (n.args[j] as { columna: number }).columna + 1) j++; else break; }
        parts.push(j > i ? `${a.type === "value" ? a.prestacion : ""}·C${(a as { columna: number }).columna}–${(n.args[j] as { columna: number }).columna}` : describe(a)); i = j;
      }
      return parts.length === 1 ? parts[0] : `(${parts.join(` ${ops[n.op]?.[1] ?? n.op} `)})`;
    }
  }
}

// "03500364:4-13, P4180300 1" → nodos de valor (una línea o coma por entrada; admite pegar desde Excel).
export function parseBulk(text: string): FNode[] | string {
  const out: FNode[] = [];
  for (const raw of text.split(/[\n,;]+/).map(x => x.trim()).filter(Boolean)) {
    const m = raw.match(/^([A-Za-z0-9]+)\s*[:\s]\s*(\d+)(?:\s*-\s*(\d+))?$/);
    if (!m) return `No se entiende "${raw}". Usa CÓDIGO:COLUMNA o CÓDIGO:DESDE-HASTA.`;
    const from = Number(m[2]), to = Number(m[3] ?? m[2]);
    if (to < from || to - from > 100) return `Rango inválido en "${raw}".`;
    for (let c = from; c <= to; c++) out.push({ type: "value", prestacion: m[1].toUpperCase(), columna: c });
  }
  return out;
}

export default function FormulaEditor({ value, onChange }: { value: string; onChange: (json: string) => void }) {
  const [raw, setRaw] = useState(false);
  const root = parseFormula(value);
  const parts = root ? split(root) : null;
  const set = (num: FNode, den: FNode | null) => onChange(JSON.stringify(join(num, den)));
  const issues = root ? problems(root) : value.trim() ? ["El JSON no es válido."] : ["La fórmula está vacía."];

  return <div className="fx">
    <div className="fxBar">
      <div className="seg"><button type="button" className={raw ? "" : "on"} onClick={() => setRaw(false)}>Visual</button><button type="button" className={raw ? "on" : ""} onClick={() => setRaw(true)}>JSON</button></div>
      {raw && root && <button type="button" className="ghost sm" onClick={() => onChange(JSON.stringify(root, null, 2))}>Formatear</button>}
    </div>
    {raw || !parts
      ? <>{!raw && <p className="fxNote">La fórmula actual no se puede editar visualmente. Corrígela en JSON o <button type="button" className="link" onClick={() => set(blank("op"), null)}>empieza una nueva</button>.</p>}
          <textarea className="mono" rows={12} spellCheck={false} value={value} onChange={e => onChange(e.target.value)} /></>
      : <div className="fxParts">
          <section><h4>Numerador</h4><NodeEditor node={parts.num} onChange={n => set(n, parts.den)} /></section>
          <section><h4>Denominador {!parts.den && <small>(ninguno: el resultado es el numerador)</small>}</h4>
            {parts.den ? <NodeEditor node={parts.den} onChange={n => set(parts.num, n)} onRemove={() => set(parts.num, null)} />
              : <button type="button" className="ghost sm" onClick={() => set(parts.num, blank("variable"))}>+ Agregar denominador</button>}</section>
        </div>}
    {root && <p className="fxPreview"><span>Vista previa</span><code>{parts?.den ? `${describe(parts.num)} ÷ ${describe(parts.den)}` : describe(root)}</code></p>}
    {issues.length > 0 && <ul className="fxIssues">{issues.slice(0, 6).map(x => <li key={x}>{x}</li>)}{issues.length > 6 && <li>… y {issues.length - 6} más.</li>}</ul>}
  </div>;
}

function NodeEditor({ node, onChange, onRemove }: { node: FNode; onChange: (n: FNode) => void; onRemove?: () => void }) {
  const typeSelect = <select className="fxType" value={node.type} onChange={e => onChange(blank(e.target.value as FNode["type"], node))}>{types.map(([k, l]) => <option key={k} value={k}>{l}</option>)}</select>;
  const remove = onRemove && <button type="button" className="x" title="Quitar término" onClick={onRemove}>×</button>;

  if (node.type === "value") return <div className="fxRow">{typeSelect}
    <input className="mono code" placeholder="Código" value={node.prestacion} onChange={e => onChange({ ...node, prestacion: e.target.value.trim().toUpperCase() })} />
    <label className="inline">Col.<input type="number" min={1} value={node.columna} onChange={e => onChange({ ...node, columna: Number(e.target.value) })} /></label>{remove}</div>;

  if (node.type === "number") return <div className="fxRow">{typeSelect}<input type="number" step="any" value={node.value} onChange={e => onChange({ ...node, value: Number(e.target.value) })} />{remove}</div>;

  if (node.type === "variable") {
    const f = node.filters ?? {};
    const setF = (k: string, v: string) => { const next = { ...f }; if (v === "") delete next[k]; else next[k] = k === "sexo" ? v : Number(v); onChange({ ...node, filters: next }); };
    return <div className="fxRow wrap">{typeSelect}
      <select value={node.name} onChange={e => onChange({ ...node, name: e.target.value, filters: e.target.value === "FONASA" ? f : {} })}>{variables.map(v => <option key={v}>{v}</option>)}</select>
      {node.name === "FONASA" ? <>
        <label className="inline">Edad mín.<input type="number" min={0} value={String(f.edad_min ?? "")} onChange={e => setF("edad_min", e.target.value)} /></label>
        <label className="inline">Edad máx.<input type="number" min={0} value={String(f.edad_max ?? "")} onChange={e => setF("edad_max", e.target.value)} /></label>
        <label className="inline">Edad exacta<input type="number" min={0} value={String(f.edad ?? "")} onChange={e => setF("edad", e.target.value)} /></label>
        <label className="inline">Sexo<select value={String(f.sexo ?? "")} onChange={e => setF("sexo", e.target.value)}><option value="">Ambos</option><option value="F">F</option><option value="M">M</option></select></label>
      </> : <small className="hint">Prevalencia estimada sobre inscritos (coeficientes fijos).</small>}{remove}</div>;
  }

  return <OpEditor node={node} typeSelect={typeSelect} remove={remove} onChange={onChange} />;
}

function OpEditor({ node, typeSelect, remove, onChange }: { node: Extract<FNode, { type: "op" }>; typeSelect: React.ReactNode; remove: React.ReactNode; onChange: (n: FNode) => void }) {
  const [bulk, setBulk] = useState(""); const [bulkError, setBulkError] = useState("");
  const setArg = (i: number, n: FNode) => onChange({ ...node, args: node.args.map((a, j) => j === i ? n : a) });
  const removeArg = (i: number) => onChange({ ...node, args: node.args.filter((_, j) => j !== i) });
  const setOp = (op: Op) => {
    let args = node.args;
    if (op !== "sum") args = args.length > 2 ? [{ type: "op", op: "sum", args: args.slice(0, -1) }, args[args.length - 1]] : args.length < 2 ? [...args, blank("number")] : args;
    onChange({ ...node, op, args });
  };
  const addBulk = () => { const r = parseBulk(bulk); if (typeof r === "string") { setBulkError(r); return; } onChange({ ...node, args: [...node.args, ...r] }); setBulk(""); setBulkError(""); };

  return <div className="fxOp">
    <div className="fxRow">{typeSelect}<select value={node.op} onChange={e => setOp(e.target.value as Op)}>{Object.entries(ops).map(([k, [l]]) => <option key={k} value={k}>{l}</option>)}</select><small className="hint">{node.args.length} términos</small>{remove}</div>
    <ol className="fxArgs">{node.args.map((a, i) => <li key={i}>
      {i > 0 && <span className="sym">{ops[node.op]?.[1]}</span>}
      <NodeEditor node={a} onChange={n => setArg(i, n)} onRemove={node.op === "sum" && node.args.length > 1 ? () => removeArg(i) : undefined} />
    </li>)}</ol>
    {node.op === "sum" && <div className="fxAdd">
      <button type="button" className="ghost sm" onClick={() => onChange({ ...node, args: [...node.args, blank("value")] })}>+ Término</button>
      <input className="mono" value={bulk} placeholder="Agregar varias: 03500364:4-13, P4180300:1" onChange={e => { setBulk(e.target.value); setBulkError(""); }} onKeyDown={e => { if (e.key === "Enter") { e.preventDefault(); addBulk(); } }} />
      <button type="button" className="ghost sm" disabled={!bulk.trim()} onClick={addBulk}>Agregar</button>
      {bulkError && <small className="err">{bulkError}</small>}
    </div>}
  </div>;
}
