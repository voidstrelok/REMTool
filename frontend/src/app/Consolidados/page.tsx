"use client";

import { useEffect, useState } from "react";
import { Download, FileSpreadsheet, RefreshCw } from "lucide-react";
import Breadcrumbs from "../components/Breadcrumbs";
import Loading from "../components/Loading";
import { consolidadoDownloadUrl, parseConsolidadoCatalog, type ConsolidadoFile } from "@/lib/consolidados";

const MONTHS = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];

export default function ConsolidadosPage() {
  const [files, setFiles] = useState<ConsolidadoFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true); setError("");
    (async () => {
      try {
        const response = await fetch("/consolidados/catalogo.json", { cache: "no-cache", signal: controller.signal });
        if (response.status === 404) { setFiles([]); return; }
        if (!response.ok) throw new Error("No se pudo cargar el catálogo de descargas.");
        setFiles(parseConsolidadoCatalog(await response.json()));
      } catch (e) {
        if (!controller.signal.aborted) setError(e instanceof Error ? e.message : "No se pudo cargar el catálogo.");
      } finally { if (!controller.signal.aborted) setLoading(false); }
    })();
    return () => controller.abort();
  }, [attempt]);

  return (
    <div className="mx-auto max-w-5xl py-4">
      <Breadcrumbs items={[{ label: "Consolidados Excel" }]} />
      <div className="page-intro flex items-start gap-3">
        <div className="rounded-lg p-2" style={{ background: "var(--accent-light)" }}><FileSpreadsheet size={22} style={{ color: "var(--primary)" }} aria-hidden="true" /></div>
        <div><h1 className="text-3xl font-bold">Consolidados Excel</h1><p>Descargue el consolidado y filtre por establecimiento, mes y sector dentro de Excel.</p></div>
      </div>
      {loading && <Loading message="Cargando descargas..." />}
      {!loading && error && <div role="alert" className="rounded-xl border p-5" style={{ borderColor: "var(--error)", background: "var(--error-bg)" }}>
        <p>{error}</p><button type="button" className="mt-3 inline-flex items-center gap-2 font-semibold" onClick={() => setAttempt(a => a + 1)}><RefreshCw size={16} />Reintentar</button>
      </div>}
      {!loading && !error && files.length === 0 && <div className="rounded-xl border p-8 text-center" style={{ borderColor: "var(--border)", background: "var(--surface)" }}>Todavía no hay consolidados publicados.</div>}
      {!loading && !error && files.length > 0 && <div className="grid gap-5 md:grid-cols-2">{files.map(file => <article key={`${file.year}-${file.series}`} className="flex flex-col rounded-xl border p-6" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
        <h2 className="text-xl font-bold">REM Serie {file.series} · {file.year}</h2>
        <p className="mt-2 text-sm" style={{ color: "var(--text-light)" }}>Datos al <time dateTime={file.generatedAt}>{new Intl.DateTimeFormat("es-CL", { dateStyle: "medium", timeStyle: "short", timeZone: "America/Santiago" }).format(new Date(file.generatedAt))}</time></p>
        <p className="mt-4 text-sm leading-6"><strong>Meses con registros:</strong> {file.months.map(m => MONTHS[m - 1]).join(", ")}.</p>
        <p className="mt-2 text-sm" style={{ color: "var(--text-light)" }}>{file.templateVersion} · {(file.bytes / 1048576).toLocaleString("es-CL", { maximumFractionDigits: 1 })} MB</p>
        {file.warnings.length > 0 && <details className="mt-4 text-sm"><summary className="cursor-pointer font-semibold">Observaciones del archivo</summary><ul className="mt-2 list-disc space-y-2 pl-5">{file.warnings.map((warning, i) => <li key={i}>{warning}</li>)}</ul></details>}
        <a href={consolidadoDownloadUrl(file)} download className="mt-6 inline-flex items-center justify-center gap-2 rounded-lg px-4 py-3 font-semibold focus-visible:outline-2 focus-visible:outline-offset-2" style={{ background: "var(--primary)", color: "white" }}><Download size={18} aria-hidden="true" />Descargar Serie {file.series} {file.year}</a>
      </article>)}</div>}
      <p className="mt-6 text-sm leading-6" style={{ color: "var(--text-light)" }}>Cada archivo contiene los datos disponibles en la fecha indicada. Consulte su hoja COBERTURA para interpretar los períodos sin actividad. Para obtener datos posteriores, descargue una nueva publicación.</p>
    </div>
  );
}
