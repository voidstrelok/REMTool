"use client";
import React, { useState, ChangeEvent } from "react";
import { useRouter } from "next/navigation";
import { Upload, RotateCcw, ArrowLeft, FileText, CheckCircle2, XCircle } from "lucide-react";
import "./RevisarREM.css";

const API = process.env.NEXT_PUBLIC_API;

interface FileResult {
  fileName: string;
  revisado?: string;
  errores: string[];
  isNetworkError: boolean;
}

const RevisarREM: React.FC = () => {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [uploaded, setUploaded] = useState(false);
  const [results, setResults] = useState<FileResult[]>([]);

  const handleFileUpload = async (e: ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    setLoading(true);
    setResults([]);
    const newResults: FileResult[] = [];

    for (let i = 0; i < files.length; i++) {
      const file = files[i];

      if (!file.name.endsWith(".xlsm")) {
        newResults.push({
          fileName: file.name,
          errores: ["Por favor selecciona un archivo .xlsm"],
          isNetworkError: false,
        });
        continue;
      }

      const formData = new FormData();
      formData.append("archivo", file);

      try {
        const res = await fetch(`${API}revisarREM`, {
          method: "POST",
          body: formData,
        });

        if (!res.ok) throw new Error(`Error: ${res.status}`);

        try {
          const result = JSON.parse(await res.text());
          newResults.push({
            fileName: file.name,
            revisado: result["revisado"] ?? undefined,
            errores: Array.isArray(result["errores"]) ? result["errores"] : [],
            isNetworkError: false,
          });
        } catch {
          newResults.push({
            fileName: file.name,
            errores: [],
            isNetworkError: false,
          });
        }
      } catch {
        newResults.push({
          fileName: file.name,
          errores: ["Error al subir el archivo. Solo se recibe serie A. Revisar que los datos en la hoja \u2018NOMBRE\u2019 est\u00e9n correctos."],
          isNetworkError: true,
        });
      }
    }

    setResults(newResults);
    setLoading(false);
  };

  const handleRestart = () => {
    setResults([]);
    const fileInput = document.getElementById("file-upload") as HTMLInputElement;
    if (fileInput) fileInput.value = "";
  };

  return (
    <main className="flex-1 flex items-center justify-center p-6 sm:p-10">
      <div style={{ width: "100%", maxWidth: 560 }}>
        <h1
          className="text-2xl font-bold mb-1"
          style={{ color: "var(--text)" }}
        >
          Revisor REM Serie A
        </h1>
        <p className="text-sm mb-6" style={{ color: "var(--text-light)" }}>
          Suba uno o varios archivos .xlsm para detectar inconsistencias.
        </p>

        {/* Upload button */}
        <label
          htmlFor="file-upload"
          className="flex items-center justify-center gap-2 w-full py-4 px-6 rounded-xl text-sm font-semibold text-white cursor-pointer transition"
          style={{
            background: loading ? "var(--text-light)" : "var(--primary)",
            cursor: loading ? "not-allowed" : "pointer",
            boxShadow: "0 2px 8px rgba(0,0,132,0.15)",
          }}
          aria-disabled={loading}
        >
          <Upload size={18} aria-hidden />
          {loading ? "Revisando archivo..." : "Subir Serie A (.xlsm)"}
        </label>
        <input
          id="file-upload"
          type="file"
          accept=".xlsm"
          multiple
          onChange={handleFileUpload}
          disabled={loading}
          className="excel-input"
          aria-label="Subir archivos REM Serie A"
        />

        {/* Instructions or results */}
        {results.length > 0 ? (
          <div className="mt-5 flex flex-col gap-3">
            {results.map((result, index) => {
              const hasErrors = result.isNetworkError || result.errores.length > 0;
              return (
                <div
                  key={index}
                  className="p-4 rounded-xl text-sm text-left"
                  style={{
                    background: hasErrors ? "var(--error-bg)" : "var(--success-bg)",
                    border: `1px solid ${hasErrors ? "var(--error)" : "var(--success)"}`,
                  }}
                >
                  <div
                    className="flex items-center gap-2 font-semibold mb-2"
                    style={{ color: hasErrors ? "var(--error-dark)" : "var(--success-text)" }}
                  >
                    {hasErrors ? (
                      <XCircle size={15} aria-hidden />
                    ) : (
                      <CheckCircle2 size={15} aria-hidden />
                    )}
                    <FileText size={14} aria-hidden />
                    {result.fileName}
                  </div>
                  {result.revisado && (
                    <p
                      className="font-medium mb-1"
                      style={{ color: "var(--text)" }}
                    >
                      {result.revisado}
                    </p>
                  )}
                  {result.errores.length === 0 && !result.isNetworkError ? (
                    <p style={{ color: "var(--success-text)" }}>Sin errores detectados.</p>
                  ) : (
                    <ul className="list-disc list-inside flex flex-col gap-0.5" style={{ color: "var(--error-dark)" }}>
                      {result.errores.map((err, i) => (
                        <li key={i}>{err}</li>
                      ))}
                    </ul>
                  )}
                </div>
              );
            })}
          </div>
        ) : (
          !uploaded && (
            <div
              className="mt-5 p-4 rounded-xl text-sm text-left"
              style={{
                background: "var(--surface)",
                border: "1px solid var(--border)",
                color: "var(--text-light)",
              }}
            >
              <strong style={{ color: "var(--text)" }}>Consideraciones:</strong>
              <ul className="mt-1 list-disc list-inside flex flex-col gap-0.5">
                <li>Asegúrese de que los datos en la hoja "NOMBRE" estén correctos.</li>
                <li>Solo se aceptan archivos .xlsm de Serie A.</li>
              </ul>
            </div>
          )
        )}

        <div className="mt-5 flex gap-2 flex-wrap">
          {results.length > 0 && (
            <button
              onClick={handleRestart}
              className="flex items-center gap-2 flex-1 justify-center px-4 py-2 rounded-lg text-sm font-semibold text-white transition"
              style={{ background: "var(--primary)", minWidth: 160 }}
            >
              <RotateCcw size={15} aria-hidden />
              Revisar más archivos
            </button>
          )}
          <button
            onClick={() => router.back()}
            className="flex items-center gap-2 flex-1 justify-center px-4 py-2 rounded-lg text-sm font-semibold transition"
            style={{
              background: "var(--surface)",
              border: "1px solid var(--border)",
              color: "var(--text)",
              minWidth: 120,
            }}
          >
            <ArrowLeft size={15} aria-hidden />
            Volver
          </button>
        </div>
      </div>
    </main>
  );
};

export default RevisarREM;