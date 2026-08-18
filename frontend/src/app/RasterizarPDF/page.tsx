"use client";
import React, { useState, useRef } from "react";
import { useRouter } from "next/navigation";
import {
  ArrowLeft,
  Upload,
  FileText,
  Loader2,
  Download,
  AlertCircle,
  CheckCircle2,
  RotateCcw,
} from "lucide-react";

// ── Oficio dimensions ──────────────────────────────────────────────────────────
// 8.5 × 13 inches | 150 DPI for rendering | 72 pt/in for PDF output
const OFICIO_W_PX = 1275;
const OFICIO_H_PX = 1950;
const OFICIO_W_PT = 612;
const OFICIO_H_PT = 936;

// ── Limits ────────────────────────────────────────────────────────────────────
const MAX_PAGES = 30;
const PROCESSING_TIMEOUT_MS = 3 * 60 * 1000; // 3 minutes

// ── Compression thresholds ─────────────────────────────────────────────────────
const COMPRESSION_THRESHOLD = 15 * 1024 * 1024; // 15 MB
const QUALITY_STEPS = [0.9, 0.75, 0.6] as const;

// ── Types ──────────────────────────────────────────────────────────────────────
type Phase = "idle" | "processing" | "compressing" | "done" | "error";
interface Progress {
  current: number;
  total: number;
}

// ── Core rendering helpers ─────────────────────────────────────────────────────

// eslint-disable-next-line @typescript-eslint/no-explicit-any
async function renderPageToJpeg(page: any, quality: number): Promise<Uint8Array> {
  // Scale page to fit within Oficio canvas, preserving aspect ratio
  const origViewport = page.getViewport({ scale: 1 });
  const scale = Math.min(OFICIO_W_PX / origViewport.width, OFICIO_H_PX / origViewport.height);
  const viewport = page.getViewport({ scale });

  // Render page into a temporary canvas at its natural scaled size
  const tmpCanvas = document.createElement("canvas");
  tmpCanvas.width = Math.round(viewport.width);
  tmpCanvas.height = Math.round(viewport.height);
  await page.render({ canvasContext: tmpCanvas.getContext("2d")!, viewport }).promise;

  // Composite the rendered page centered on an Oficio-sized white canvas
  const mainCanvas = document.createElement("canvas");
  mainCanvas.width = OFICIO_W_PX;
  mainCanvas.height = OFICIO_H_PX;
  const ctx = mainCanvas.getContext("2d")!;
  ctx.fillStyle = "#ffffff";
  ctx.fillRect(0, 0, OFICIO_W_PX, OFICIO_H_PX);
  ctx.drawImage(
    tmpCanvas,
    Math.round((OFICIO_W_PX - viewport.width) / 2),
    Math.round((OFICIO_H_PX - viewport.height) / 2),
  );

  // Encode to JPEG
  const blob = await new Promise<Blob>((resolve) =>
    mainCanvas.toBlob((b) => resolve(b!), "image/jpeg", quality),
  );
  return new Uint8Array(await blob.arrayBuffer());
}

interface AbortSignal {
  aborted: boolean;
}

async function buildRasterizedPdf(
  file: File,
  quality: number,
  signal: AbortSignal,
  onProgress: (p: Progress) => void,
): Promise<Uint8Array> {
  // Lazy-load heavy libraries so the initial page bundle stays small
  const [pdfjsLib, { PDFDocument }] = await Promise.all([
    import("pdfjs-dist"),
    import("pdf-lib"),
  ]);

  // Worker served from public/ for static-export compatibility
  pdfjsLib.GlobalWorkerOptions.workerSrc = "/pdf.worker.min.mjs";

  const data = new Uint8Array(await file.arrayBuffer());
  const sourcePdf = await pdfjsLib.getDocument({ data }).promise;
  const total = sourcePdf.numPages;

  if (total > MAX_PAGES) {
    throw new Error(`PAGE_LIMIT:${total}`);
  }

  const outDoc = await PDFDocument.create();

  for (let i = 1; i <= total; i++) {
    if (signal.aborted) throw new Error("TIMEOUT");
    onProgress({ current: i, total });
    const page = await sourcePdf.getPage(i);
    const jpegBytes = await renderPageToJpeg(page, quality);
    if (signal.aborted) throw new Error("TIMEOUT");
    const jpgImage = await outDoc.embedJpg(jpegBytes);
    const outPage = outDoc.addPage([OFICIO_W_PT, OFICIO_H_PT]);
    outPage.drawImage(jpgImage, { x: 0, y: 0, width: OFICIO_W_PT, height: OFICIO_H_PT });
  }

  return outDoc.save();
}

// ── Page component ─────────────────────────────────────────────────────────────
export default function RasterizarPDFPage() {
  const router = useRouter();
  const inputRef = useRef<HTMLInputElement>(null);

  const [file, setFile] = useState<File | null>(null);
  const [phase, setPhase] = useState<Phase>("idle");
  const [progress, setProgress] = useState<Progress>({ current: 0, total: 0 });
  const [resultBytes, setResultBytes] = useState<Uint8Array | null>(null);
  const [errorMsg, setErrorMsg] = useState("");
  const abortSignal = useRef<{ aborted: boolean }>({ aborted: false });

  const isProcessing = phase === "processing" || phase === "compressing";

  // ── Handlers ────────────────────────────────────────────────────────────────

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (!f) return;
    if (!f.name.toLowerCase().endsWith(".pdf")) {
      setErrorMsg("Solo se aceptan archivos PDF (.pdf).");
      return;
    }
    setFile(f);
    setResultBytes(null);
    setPhase("idle");
    setErrorMsg("");
  };

  const handleRasterize = async () => {
    if (!file) return;
    setPhase("processing");
    setProgress({ current: 0, total: 0 });
    setErrorMsg("");

    // Reset abort signal and arm the timeout
    abortSignal.current = { aborted: false };
    const timeoutId = setTimeout(
      () => { abortSignal.current.aborted = true; },
      PROCESSING_TIMEOUT_MS,
    );

    try {
      let result: Uint8Array | null = null;

      for (let qi = 0; qi < QUALITY_STEPS.length; qi++) {
        if (qi > 0) {
          // Previous pass exceeded threshold — retry with lower quality
          setPhase("compressing");
          setProgress({ current: 0, total: 0 });
        }

        const bytes = await buildRasterizedPdf(
          file,
          QUALITY_STEPS[qi],
          abortSignal.current,
          (p) => setProgress(p),
        );

        if (bytes.length <= COMPRESSION_THRESHOLD || qi === QUALITY_STEPS.length - 1) {
          result = bytes;
          break;
        }
      }

      setResultBytes(result!);
      setPhase("done");
    } catch (err) {
      console.error(err);
      const msg = err instanceof Error ? err.message : "";
      if (msg === "TIMEOUT") {
        setErrorMsg(
          `El procesamiento superó el límite de ${PROCESSING_TIMEOUT_MS / 60000} minutos. Intente con un PDF más pequeño.`,
        );
      } else if (msg.startsWith("PAGE_LIMIT:")) {
        const pages = msg.split(":")[1];
        setErrorMsg(
          `El PDF tiene ${pages} páginas. El límite máximo es ${MAX_PAGES} páginas.`,
        );
      } else {
        setErrorMsg(
          "Error al procesar el PDF. Verifique que el archivo no esté protegido con contraseña.",
        );
      }
      setPhase("error");
    } finally {
      clearTimeout(timeoutId);
    }
  };

  const handleDownload = () => {
    if (!resultBytes || !file) return;
    const blob = new Blob([resultBytes], { type: "application/pdf" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = file.name.replace(/\.pdf$/i, "_rasterizado.pdf");
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleReset = () => {
    setFile(null);
    setPhase("idle");
    setResultBytes(null);
    setErrorMsg("");
    setProgress({ current: 0, total: 0 });
    if (inputRef.current) inputRef.current.value = "";
  };

  // ── Render ───────────────────────────────────────────────────────────────────
  return (
    <div className="max-w-2xl mx-auto py-8 px-4">
      {/* Page header */}
      <div className="flex items-center gap-3 mb-8">
        <button
          onClick={() => router.back()}
          className="p-2 rounded-lg transition-colors hover:bg-gray-100"
          style={{ color: "var(--text-light)" }}
        >
          <ArrowLeft size={20} />
        </button>
        <div>
          <h1 className="text-2xl font-bold" style={{ color: "var(--text)" }}>
            Rasterizar PDF
          </h1>
          <p className="text-sm mt-0.5" style={{ color: "var(--text-light)" }}>
            Convierte las capas y anotaciones del PDF en imagen permanente.
          </p>
        </div>
      </div>

      {/* Main card */}
      <div
        className="rounded-xl border p-6"
        style={{ background: "var(--surface)", borderColor: "var(--border)" }}
      >
        {/* Drop zone */}
        <div className="mb-5">
          <label
            htmlFor="pdf-input"
            className="flex flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed py-10 px-6 cursor-pointer transition-colors hover:bg-gray-50"
            style={{ borderColor: isProcessing ? "transparent" : "var(--border)" }}
          >
            <div
              className="p-3 rounded-full"
              style={{ background: "var(--accent-light)" }}
            >
              <Upload size={24} style={{ color: "var(--primary)" }} />
            </div>
            <div className="text-center">
              <p className="text-sm font-medium" style={{ color: "var(--text)" }}>
                {file ? file.name : "Haz clic para seleccionar un PDF"}
              </p>
              {file ? (
                <p className="text-xs mt-0.5" style={{ color: "var(--text-light)" }}>
                  {(file.size / 1024 / 1024).toFixed(2)} MB — haz clic para cambiar
                </p>
              ) : (
                <p className="text-xs mt-0.5" style={{ color: "var(--text-light)" }}>
                  Solo archivos .pdf
                </p>
              )}
            </div>
          </label>
          <input
            ref={inputRef}
            id="pdf-input"
            type="file"
            accept=".pdf"
            className="hidden"
            onChange={handleFileChange}
            disabled={isProcessing}
          />
        </div>

        {/* Info notice */}
        <div
          className="flex items-start gap-2.5 rounded-lg p-3 mb-5 text-sm"
          style={{ background: "var(--accent-light)" }}
        >
          <FileText size={15} className="mt-0.5 flex-shrink-0" style={{ color: "var(--primary)" }} />
          <span style={{ color: "var(--text-light)" }}>
            Cada página queda normalizada a tamaño{" "}
            <strong style={{ color: "var(--text)" }}>Oficio (8.5 × 13")</strong>, centrada
            sobre fondo blanco. Cualquier forma, cuadro de texto o anotación de Acrobat
            queda fusionada en la imagen y ya no puede moverse ni eliminarse.{" "}
            Límite: <strong style={{ color: "var(--text)" }}>{MAX_PAGES} páginas</strong> por archivo.
          </span>
        </div>

        {/* Error banner */}
        {errorMsg && (
          <div
            className="flex items-start gap-2 rounded-lg border p-3 mb-5 text-sm"
            style={{ borderColor: "#fca5a5", background: "#fef2f2", color: "#b91c1c" }}
          >
            <AlertCircle size={15} className="mt-0.5 flex-shrink-0" />
            <span>{errorMsg}</span>
          </div>
        )}

        {/* Progress bar */}
        {isProcessing && (
          <div className="mb-5">
            <div className="flex items-center justify-between mb-1.5">
              <span
                className="text-sm font-medium flex items-center gap-2"
                style={{ color: "var(--text)" }}
              >
                <Loader2 size={13} className="animate-spin" />
                {phase === "compressing"
                  ? "Optimizando tamaño del archivo..."
                  : progress.total > 0
                    ? `Procesando página ${progress.current} de ${progress.total}`
                    : "Iniciando..."}
              </span>
              {progress.total > 0 && (
                <span className="text-xs tabular-nums" style={{ color: "var(--text-light)" }}>
                  {Math.round((progress.current / progress.total) * 100)}%
                </span>
              )}
            </div>
            <div
              className="w-full rounded-full h-1.5 overflow-hidden"
              style={{ background: "var(--border)" }}
            >
              <div
                className="h-1.5 rounded-full transition-all duration-300"
                style={{
                  width:
                    progress.total > 0
                      ? `${(progress.current / progress.total) * 100}%`
                      : "0%",
                  background: "var(--primary)",
                }}
              />
            </div>
          </div>
        )}

        {/* Success banner */}
        {phase === "done" && resultBytes && (
          <div
            className="flex items-center gap-2 rounded-lg border p-3 mb-5 text-sm"
            style={{ borderColor: "#bbf7d0", background: "#f0fdf4", color: "#15803d" }}
          >
            <CheckCircle2 size={15} className="flex-shrink-0" />
            <span>
              Listo. Tamaño final:{" "}
              <strong>{(resultBytes.length / 1024 / 1024).toFixed(2)} MB</strong>
            </span>
          </div>
        )}

        {/* Action buttons */}
        <div className="flex gap-3">
          {phase !== "done" ? (
            <button
              onClick={handleRasterize}
              disabled={!file || isProcessing}
              className="flex-1 flex items-center justify-center gap-2 rounded-lg py-2.5 text-sm font-semibold text-white transition-opacity disabled:opacity-40 disabled:cursor-not-allowed"
              style={{ background: "var(--primary)" }}
            >
              {isProcessing ? (
                <>
                  <Loader2 size={15} className="animate-spin" />
                  Procesando...
                </>
              ) : (
                <>
                  <FileText size={15} />
                  Rasterizar PDF
                </>
              )}
            </button>
          ) : (
            <>
              <button
                onClick={handleDownload}
                className="flex-1 flex items-center justify-center gap-2 rounded-lg py-2.5 text-sm font-semibold text-white"
                style={{ background: "var(--primary)" }}
              >
                <Download size={15} />
                Descargar PDF rasterizado
              </button>
              <button
                onClick={handleReset}
                className="flex items-center justify-center gap-1.5 px-4 rounded-lg border text-sm font-medium transition-colors hover:bg-gray-50"
                style={{ borderColor: "var(--border)", color: "var(--text-light)" }}
              >
                <RotateCcw size={14} />
                Nuevo
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
