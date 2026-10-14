"use client";

import {
  MouseEvent as ReactMouseEvent,
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";

interface Celda {
  el: HTMLTableCellElement;
  r: number;
  c: number;
  rs: number;
  cs: number;
}

interface Rango {
  r1: number;
  c1: number;
  r2: number;
  c2: number;
}

const BORDE_AUTOSCROLL = 40;
const VELOCIDAD_MAX = 28;
const FIJAS_MAX_ANCHO = 0.45;

// Posición (fila, columna) de cada celda, respetando rowspan y colspan.
function mapear(tabla: HTMLTableElement): Celda[] {
  const celdas: Celda[] = [];
  const ocupado: boolean[][] = [];

  Array.from(tabla.rows).forEach((fila, r) => {
    ocupado[r] ??= [];
    let c = 0;
    Array.from(fila.cells).forEach((el) => {
      while (ocupado[r][c]) c++;
      const rs = el.rowSpan || 1;
      const cs = el.colSpan || 1;
      for (let i = 0; i < rs; i++) {
        ocupado[r + i] ??= [];
        for (let j = 0; j < cs; j++) ocupado[r + i][c + j] = true;
      }
      celdas.push({ el, r, c, rs, cs });
      c += cs;
    });
  });

  return celdas;
}

function rangoDe(a: Celda, b: Celda): Rango {
  return {
    r1: Math.min(a.r, b.r),
    c1: Math.min(a.c, b.c),
    r2: Math.max(a.r + a.rs - 1, b.r + b.rs - 1),
    c2: Math.max(a.c + a.cs - 1, b.c + b.cs - 1),
  };
}

function valorNumerico(el: HTMLElement): number | null {
  if (!/\b(total|prestacion|editable)\b/.test(el.className)) return null;
  const texto = el.textContent?.trim() ?? "";
  return /^-?\d+([.,]\d+)?$/.test(texto) ? Number(texto.replace(",", ".")) : null;
}

function empuje(distancia: number) {
  const factor = Math.min(1, Math.max(0, (BORDE_AUTOSCROLL - distancia) / BORDE_AUTOSCROLL));
  return Math.ceil(factor * VELOCIDAD_MAX);
}

export default function TablaRem({ html }: { html: string }) {
  const contenedor = useRef<HTMLDivElement>(null);
  const globo = useRef<HTMLDivElement>(null);
  const celdas = useRef<Celda[]>([]);
  const mapa = useRef(new Map<Element, Celda>());
  const rangos = useRef<Rango[]>([]);
  const seleccionadas = useRef<Set<Celda>>(new Set());
  const ancla = useRef<Celda | null>(null);
  const foco = useRef<Celda | null>(null);
  const cursor = useRef({ x: 0, y: 0 });
  const anchoFijo = useRef(0);
  const detenerArrastre = useRef<(() => void) | null>(null);
  const [suma, setSuma] = useState<number | null>(null);
  const contenido = useMemo(() => ({ __html: html }), [html]);

  const posicionarGlobo = useCallback(() => {
    const g = globo.current;
    const f = foco.current;
    const cont = contenedor.current;
    if (!g || !f || !cont) return;

    const celda = f.el.getBoundingClientRect();
    const caja = cont.getBoundingClientRect();
    const visible = celda.right > caja.left && celda.left < caja.right
      && celda.bottom > 0 && celda.top < window.innerHeight;
    g.style.visibility = visible ? "visible" : "hidden";

    const derecha = Math.min(celda.right, caja.right);
    let left = derecha + 6;
    let top = celda.bottom + 6;
    if (left + g.offsetWidth > window.innerWidth - 8) {
      left = Math.max(8, Math.max(celda.left, caja.left) - g.offsetWidth - 6);
    }
    if (top + g.offsetHeight > window.innerHeight - 8) {
      top = Math.max(8, celda.top - g.offsetHeight - 6);
    }
    g.style.left = `${left}px`;
    g.style.top = `${top}px`;
  }, []);

  const pintar = useCallback(() => {
    const sel = new Set<Celda>();
    for (const celda of celdas.current) {
      if (rangos.current.some((r) =>
        celda.r <= r.r2 && celda.r + celda.rs - 1 >= r.r1
        && celda.c <= r.c2 && celda.c + celda.cs - 1 >= r.c1)) {
        sel.add(celda);
      }
    }

    let total = 0;
    let numericas = 0;
    for (const celda of celdas.current) {
      const marcada = sel.has(celda);
      celda.el.classList.toggle("rem-sel", marcada);
      celda.el.classList.toggle("rem-sel-ancla", marcada && ancla.current === celda);
      if (marcada) {
        const valor = valorNumerico(celda.el);
        if (valor !== null) {
          total += valor;
          numericas++;
        }
      }
    }

    seleccionadas.current = sel;
    setSuma(numericas > 0 ? Math.round(total * 100) / 100 : null);
    posicionarGlobo();
  }, [posicionarGlobo]);

  const limpiar = useCallback(() => {
    detenerArrastre.current?.();
    rangos.current = [];
    ancla.current = null;
    foco.current = null;
    pintar();
  }, [pintar]);

  const copiar = useCallback(() => {
    const sel = Array.from(seleccionadas.current);
    if (sel.length === 0) return;

    const r0 = Math.min(...sel.map((c) => c.r));
    const c0 = Math.min(...sel.map((c) => c.c));
    const r1 = Math.max(...sel.map((c) => c.r));
    const c1 = Math.max(...sel.map((c) => c.c));
    const filas = Array.from({ length: r1 - r0 + 1 }, () => Array<string>(c1 - c0 + 1).fill(""));
    sel.forEach((c) => { filas[c.r - r0][c.c - c0] = c.el.textContent?.trim() ?? ""; });

    navigator.clipboard?.writeText(filas.map((fila) => fila.join("\t")).join("\n"));
  }, []);

  const celdaEn = useCallback((x: number, y: number) => {
    const cont = contenedor.current;
    if (!cont) return null;

    const caja = cont.getBoundingClientRect();
    const izq = caja.left + cont.clientLeft;
    const arriba = caja.top + cont.clientTop;
    const px = Math.min(Math.max(x, izq + 1), izq + cont.clientWidth - 2);
    const py = Math.min(
      Math.max(y, arriba + 1, 0),
      arriba + cont.clientHeight - 2,
      window.innerHeight - 1,
    );

    const el = document.elementFromPoint(px, py)?.closest("td,th");
    return el ? mapa.current.get(el) ?? null : null;
  }, []);

  const actualizarFoco = useCallback(() => {
    const celda = celdaEn(cursor.current.x, cursor.current.y);
    if (!celda || celda === foco.current || !ancla.current) return;

    foco.current = celda;
    rangos.current = [...rangos.current.slice(0, -1), rangoDe(ancla.current, celda)];
    pintar();
  }, [celdaEn, pintar]);

  const autoscroll = useCallback(() => {
    const cont = contenedor.current;
    if (!cont) return;

    const caja = cont.getBoundingClientRect();
    const { x, y } = cursor.current;
    const bordeIzq = caja.left + anchoFijo.current;

    if (x < bordeIzq + BORDE_AUTOSCROLL) cont.scrollLeft -= empuje(x - bordeIzq);
    else if (x > caja.right - BORDE_AUTOSCROLL) cont.scrollLeft += empuje(caja.right - x);

    if (y < BORDE_AUTOSCROLL) window.scrollBy(0, -empuje(y));
    else if (y > window.innerHeight - BORDE_AUTOSCROLL) window.scrollBy(0, empuje(window.innerHeight - y));
  }, []);

  const alPresionar = (event: ReactMouseEvent) => {
    if (event.button !== 0) return;

    const td = (event.target as HTMLElement).closest("td,th");
    const celda = td ? mapa.current.get(td) : undefined;
    if (!celda) return;

    event.preventDefault();
    if (event.shiftKey && ancla.current) {
      rangos.current = [...rangos.current.slice(0, -1), rangoDe(ancla.current, celda)];
    } else {
      ancla.current = celda;
      const nuevo = rangoDe(celda, celda);
      rangos.current = event.ctrlKey || event.metaKey ? [...rangos.current, nuevo] : [nuevo];
    }
    foco.current = celda;
    cursor.current = { x: event.clientX, y: event.clientY };
    pintar();

    detenerArrastre.current?.();
    const mover = (e: MouseEvent) => {
      cursor.current = { x: e.clientX, y: e.clientY };
      actualizarFoco();
    };
    let raf = 0;
    const paso = () => {
      autoscroll();
      actualizarFoco();
      raf = requestAnimationFrame(paso);
    };
    const soltar = () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("mousemove", mover);
      window.removeEventListener("mouseup", soltar);
      detenerArrastre.current = null;
    };
    window.addEventListener("mousemove", mover);
    window.addEventListener("mouseup", soltar);
    raf = requestAnimationFrame(paso);
    detenerArrastre.current = soltar;
  };

  // Mapa de celdas y columnas fijas a la izquierda; se rehace con cada HTML nuevo.
  useLayoutEffect(() => {
    const cont = contenedor.current;
    const tabla = cont?.querySelector("table");
    if (!cont || !tabla) return;

    const lista = mapear(tabla);
    celdas.current = lista;
    mapa.current = new Map(lista.map((celda) => [celda.el, celda]));
    rangos.current = [];
    ancla.current = null;
    foco.current = null;
    pintar();

    const fijar = () => {
      lista.forEach(({ el }) => {
        el.classList.remove("rem-fija", "rem-fija-ultima");
        el.style.left = "";
      });
      anchoFijo.current = 0;
      if (cont.clientWidth === 0) return;

      const columnas = Math.max(...lista.map((celda) => celda.c + celda.cs));
      const izquierda = (col: number) =>
        lista.find((celda) => celda.c === col)?.el.offsetLeft;

      // Columnas iniciales sin datos editables: nombres de prestación, totales, etc.
      let n = 0;
      while (n < columnas) {
        const inicio = lista.filter((celda) => celda.c === n);
        if (inicio.length === 0
          || inicio.some((celda) => /\b(editable|prestacion)\b/.test(celda.el.className))) break;
        n++;
      }
      while (n > 0 && (izquierda(n) ?? tabla.offsetWidth) > cont.clientWidth * FIJAS_MAX_ANCHO) n--;
      if (n === 0) return;

      anchoFijo.current = izquierda(n) ?? tabla.offsetWidth;
      lista.forEach(({ el, c, cs }) => {
        if (c + cs > n) return;
        el.classList.add("rem-fija");
        if (c + cs === n) el.classList.add("rem-fija-ultima");
        el.style.left = `${izquierda(c) ?? 0}px`;
      });
    };

    fijar();
    const observador = new ResizeObserver(fijar);
    observador.observe(cont);
    observador.observe(tabla);
    return () => observador.disconnect();
  }, [html, pintar]);

  useLayoutEffect(() => {
    posicionarGlobo();
  }, [suma, posicionarGlobo]);

  useEffect(() => {
    const fuera = (event: MouseEvent) => {
      if (rangos.current.length > 0 && !contenedor.current?.contains(event.target as Node)) limpiar();
    };
    const tecla = (event: KeyboardEvent) => {
      if (rangos.current.length === 0) return;
      const activo = document.activeElement?.tagName;
      if (activo === "INPUT" || activo === "TEXTAREA") return;

      if (event.key === "Escape") limpiar();
      else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "c") copiar();
    };
    const reposicionar = () => {
      if (foco.current) posicionarGlobo();
    };

    document.addEventListener("mousedown", fuera);
    document.addEventListener("keydown", tecla);
    window.addEventListener("scroll", reposicionar, { passive: true });
    window.addEventListener("resize", reposicionar);
    return () => {
      document.removeEventListener("mousedown", fuera);
      document.removeEventListener("keydown", tecla);
      window.removeEventListener("scroll", reposicionar);
      window.removeEventListener("resize", reposicionar);
      detenerArrastre.current?.();
    };
  }, [limpiar, copiar, posicionarGlobo]);

  return (
    <>
      <div
        ref={contenedor}
        className="consulta-rem-tabla"
        onMouseDown={alPresionar}
        onScroll={posicionarGlobo}
        dangerouslySetInnerHTML={contenido}
      />
      {suma !== null && (
        <div ref={globo} className="consulta-rem-globo">
          Suma: {suma.toLocaleString("es-CL")}
        </div>
      )}
    </>
  );
}
