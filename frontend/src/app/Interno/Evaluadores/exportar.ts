import type { Evaluador, Registro } from "./tipos";
import { avisos, opcionElegida, puntajePauta, unidades } from "./puntaje";

export function descargar(data: BlobPart, nombre: string, mime: string) {
  const a = Object.assign(document.createElement("a"), { href: URL.createObjectURL(new Blob([data], { type: mime })), download: nombre });
  document.body.appendChild(a); a.click(); a.remove();
  setTimeout(() => URL.revokeObjectURL(a.href), 2000);
}

export const nombreArchivo = (ev: Evaluador, r: Registro) =>
  [ev.titulo.replace(/^Evaluador\s+/i, ""), r.campos.establecimiento || "establecimiento", r.campos.fecha || ""]
    .filter(Boolean).join("_").normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/[^A-Za-z0-9]+/g, "_").replace(/^_|_$/g, "");

/* ---------- XLSX sin librerías: ZIP "store" + SpreadsheetML ---------- */

// Estilos: 0 normal · 1 negrita · 2 título · 3 encabezado · 4 celda · 5 porcentaje · 6 sección · 7 centrado
type Celda = string | number | null | { v: string | number | null; s: number };
type Hoja = { nombre: string; filas: Celda[][]; anchos?: number[]; fijar?: number };

const CRC = (() => { const t = new Uint32Array(256); for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; t[n] = c >>> 0; } return t; })();
const crc32 = (b: Uint8Array) => { let c = 0xffffffff; for (const x of b) c = CRC[(c ^ x) & 0xff] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };

function zip(files: { name: string; text: string }[]) {
  const enc = new TextEncoder(), parts: Uint8Array[] = [], central: Uint8Array[] = [];
  let offset = 0;
  for (const f of files) {
    const name = enc.encode(f.name), data = enc.encode(f.text), crc = crc32(data), size = data.length;
    const lh = new DataView(new ArrayBuffer(30));
    lh.setUint32(0, 0x04034b50, true); lh.setUint16(4, 20, true); lh.setUint16(6, 0x0800, true); lh.setUint16(12, 0x21, true);
    lh.setUint32(14, crc, true); lh.setUint32(18, size, true); lh.setUint32(22, size, true); lh.setUint16(26, name.length, true);
    parts.push(new Uint8Array(lh.buffer), name, data);
    const ch = new DataView(new ArrayBuffer(46));
    ch.setUint32(0, 0x02014b50, true); ch.setUint16(4, 20, true); ch.setUint16(6, 20, true); ch.setUint16(8, 0x0800, true); ch.setUint16(14, 0x21, true);
    ch.setUint32(16, crc, true); ch.setUint32(20, size, true); ch.setUint32(24, size, true); ch.setUint16(28, name.length, true); ch.setUint32(42, offset, true);
    central.push(new Uint8Array(ch.buffer), name);
    offset += 30 + name.length + size;
  }
  const cd = central.reduce((s, x) => s + x.length, 0), eo = new DataView(new ArrayBuffer(22));
  eo.setUint32(0, 0x06054b50, true); eo.setUint16(8, files.length, true); eo.setUint16(10, files.length, true); eo.setUint32(12, cd, true); eo.setUint32(16, offset, true);
  const all = [...parts, ...central, new Uint8Array(eo.buffer)], out = new Uint8Array(all.reduce((s, x) => s + x.length, 0));
  let p = 0; for (const x of all) { out.set(x, p); p += x.length; }
  return out;
}

const xml = (s: string) => s.replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F]/g, "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
const col = (i: number) => { let s = ""; i++; while (i > 0) { const m = (i - 1) % 26; s = String.fromCharCode(65 + m) + s; i = Math.floor((i - 1) / 26); } return s; };

function hojaXml({ filas, anchos, fijar }: Hoja) {
  let x = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">';
  if (fijar) x += `<sheetViews><sheetView workbookViewId="0"><pane ySplit="${fijar}" topLeftCell="A${fijar + 1}" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>`;
  if (anchos) x += "<cols>" + anchos.map((w, i) => `<col min="${i + 1}" max="${i + 1}" width="${w}" customWidth="1"/>`).join("") + "</cols>";
  x += "<sheetData>";
  filas.forEach((fila, ri) => {
    x += `<row r="${ri + 1}">`;
    fila.forEach((c, ci) => {
      const { v, s } = c !== null && typeof c === "object" ? c : { v: c, s: 0 }, ref = col(ci) + (ri + 1);
      if (v === null || v === "") x += `<c r="${ref}" s="${s}"/>`;
      else if (typeof v === "number") x += `<c r="${ref}" s="${s}"><v>${v}</v></c>`;
      else x += `<c r="${ref}" s="${s}" t="inlineStr"><is><t xml:space="preserve">${xml(v)}</t></is></c>`;
    });
    x += "</row>";
  });
  return x + "</sheetData></worksheet>";
}

const ESTILOS = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">' +
  '<numFmts count="1"><numFmt numFmtId="164" formatCode="0.0%"/></numFmts>' +
  '<fonts count="3"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="14"/><name val="Calibri"/></font></fonts>' +
  '<fills count="4"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill>' +
  '<fill><patternFill patternType="solid"><fgColor rgb="FFD9E5F1"/><bgColor indexed="64"/></patternFill></fill>' +
  '<fill><patternFill patternType="solid"><fgColor rgb="FFE8EBEF"/><bgColor indexed="64"/></patternFill></fill></fills>' +
  '<borders count="2"><border><left/><right/><top/><bottom/><diagonal/></border>' +
  '<border><left style="thin"><color rgb="FF999999"/></left><right style="thin"><color rgb="FF999999"/></right><top style="thin"><color rgb="FF999999"/></top><bottom style="thin"><color rgb="FF999999"/></bottom><diagonal/></border></borders>' +
  '<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="8">' +
  '<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>' +
  '<xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>' +
  '<xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1"/>' +
  '<xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>' +
  '<xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf>' +
  '<xf numFmtId="164" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="top"/></xf>' +
  '<xf numFmtId="0" fontId="1" fillId="3" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>' +
  '<xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="top" wrapText="1"/></xf>' +
  '</cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>';

function libro(hojas: Hoja[]) {
  const R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships", PKG = "http://schemas.openxmlformats.org/package/2006/relationships";
  const head = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>';
  // Nombres de hoja: máx. 31 caracteres, sin []:*?/\ y sin repetir.
  const nombres = hojas.map((h, i) => h.nombre.replace(/[[\]:*?/\\]/g, " ").slice(0, 28) + (hojas.slice(0, i).some(o => o.nombre === h.nombre) ? ` ${i}` : ""));
  return zip([
    { name: "[Content_Types].xml", text: `${head}<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>${hojas.map((_, i) => `<Override PartName="/xl/worksheets/sheet${i + 1}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>`).join("")}<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>` },
    { name: "_rels/.rels", text: `${head}<Relationships xmlns="${PKG}"><Relationship Id="rId1" Type="${R}/officeDocument" Target="xl/workbook.xml"/></Relationships>` },
    { name: "xl/workbook.xml", text: `${head}<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="${R}"><sheets>${nombres.map((n, i) => `<sheet name="${xml(n)}" sheetId="${i + 1}" r:id="rId${i + 1}"/>`).join("")}</sheets></workbook>` },
    { name: "xl/_rels/workbook.xml.rels", text: `${head}<Relationships xmlns="${PKG}">${hojas.map((_, i) => `<Relationship Id="rId${i + 1}" Type="${R}/worksheet" Target="worksheets/sheet${i + 1}.xml"/>`).join("")}<Relationship Id="rId${hojas.length + 1}" Type="${R}/styles" Target="styles.xml"/></Relationships>` },
    { name: "xl/styles.xml", text: ESTILOS },
    ...hojas.map((h, i) => ({ name: `xl/worksheets/sheet${i + 1}.xml`, text: hojaXml(h) })),
  ]);
}

/* ---------- Libro de una evaluación ---------- */

export function excelEvaluacion(ev: Evaluador, r: Registro) {
  const T = (v: string) => ({ v, s: 2 }), H = (v: string) => ({ v, s: 3 }), C = (v: string | number | null) => ({ v, s: 4 }),
    CC = (v: string | number | null) => ({ v, s: 7 }), S = (v: string | number | null) => ({ v, s: 6 }),
    P = (p: number | null) => (p === null ? CC("—") : { v: p / 100, s: 5 });

  const resumen: Celda[][] = [[T(ev.titulo)], []];
  for (const c of ev.campos) resumen.push([C(c.label), C(r.campos[c.id] ?? "")]);
  for (const p of ev.pautas) {
    const res = puntajePauta(p, r);
    resumen.push([], [S(p.titulo), S(""), S(""), S("")], [H("Aspecto evaluado"), H("Puntaje que aplica"), H("Puntaje obtenido"), H("% de cumplimiento")]);
    for (const s of res.secciones) resumen.push([C(s.sec.titulo), CC(s.max), CC(s.obt), P(s.pct)]);
    resumen.push([S("Total"), S(res.max), S(res.obt), P(res.pct)]);
    if (res.nivel) resumen.push([C("Nivel de logro"), C(res.nivel.label)]);
    if (res.certificacion) resumen.push([C("Certificación"), C(res.certificacion.ok ? "Cumple las condiciones de certificación" : "No certifica")]);
    if (p.objetivo) resumen.push([C("Objetivo"), C(r.textos[`objetivo:${p.id}`] ?? "")]);
    if (p.comentario) resumen.push([C("Comentarios"), C(r.textos[`comentario:${p.id}`] ?? "")]);
  }
  const w = avisos(ev, r);
  if (w.length) resumen.push([], [S("Revisión"), S("")], ...w.map(x => [C(""), C(x)]));

  const hojas: Hoja[] = [{ nombre: "Resumen", filas: resumen, anchos: [40, 70, 18, 18] }];

  for (const p of ev.pautas) {
    const items = p.secciones.flatMap(s => s.items);
    const etiquetas = [...new Set(items.flatMap(i => (i.detalles ?? []).map(d => d[0])))];
    const conGrupo = items.some(i => i.grupo), conCaso = p.secciones.some(s => s.porCaso), conNotas = items.some(i => i.detalleCasos);
    const filas: Celda[][] = [[H("N°"), H("Sección"), ...(conGrupo ? [H("Grupo")] : []), H("Ítem"), ...etiquetas.map(H), ...(conCaso ? [H("Caso")] : []),
      H("Respuesta"), H("Puntaje"), H("Observación"), ...(conNotas ? [H("Detalle de casos (sin puntaje)")] : [])]];
    for (const s of p.secciones) for (const u of unidades(s, r)) {
      const o = opcionElegida(p, u, r), d = Object.fromEntries(u.item.detalles ?? []);
      filas.push([CC(u.item.n ?? ""), C(s.titulo), ...(conGrupo ? [C(u.item.grupo ?? "")] : []), C(u.item.texto), ...etiquetas.map(e => C(d[e] ?? "")),
        ...(conCaso ? [C(u.caso ? `Caso ${u.caso.n}${u.caso.nombre ? ` · ${u.caso.nombre}` : ""}` : "")] : []),
        CC(o?.label ?? "Sin responder"), CC(o?.pts ?? null), C(r.obs[u.item.id] ?? ""),
        ...(conNotas ? [C((r.casos[u.item.id] ?? []).map((t, i) => t.trim() && `Caso ${i + 1}: ${t.trim()}`).filter(Boolean).join("\n"))] : [])]);
    }
    hojas.push({ nombre: p.titulo, filas, fijar: 1, anchos: [6, 24, ...(conGrupo ? [18] : []), 60, ...etiquetas.map(() => 40), ...(conCaso ? [18] : []), 22, 10, 50, ...(conNotas ? [50] : [])] });
  }

  if (ev.anexos?.length) {
    const filas: Celda[][] = [[H("Anexo"), H("Elemento"), H("Disponibilidad"), H("Observación")]];
    ev.anexos.forEach((a, ai) => a.items.forEach((it, ii) => {
      const k = `${ai}-${ii}`, v = r.anexos[k];
      filas.push([C(a.titulo), C(it), CC(v === undefined ? "Sin registrar" : a.opciones[v]), C(r.anexosObs[k] ?? "")]);
    }));
    hojas.push({ nombre: "Anexos", filas, fijar: 1, anchos: [44, 50, 16, 50] });
  }

  if (ev.tablas?.length) {
    const filas: Celda[][] = [];
    for (const t of ev.tablas) {
      filas.push([S(t.titulo)], t.columnas.map(c => H(c[1])));
      for (const f of r.tablas[t.id] ?? []) if (t.columnas.some(c => f[c[0]]?.trim())) filas.push(t.columnas.map(c => C(f[c[0]] ?? "")));
      filas.push([]);
    }
    hojas.push({ nombre: "Personas", filas, anchos: [34, 34, 36, 18] });
  }
  return libro(hojas);
}

