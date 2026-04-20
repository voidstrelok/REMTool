"use client";
import { useRouter } from "next/navigation";
import React, { useEffect, useState } from "react";
import { Upload, Settings, RotateCcw, ArrowLeft, CheckCircle2, Loader2, X, AlertCircle } from "lucide-react";

const months = [1,2,3,4,5,6,7,8,9, 10, 11, 12];
const API = process.env.NEXT_PUBLIC_API;

interface Establecimiento {
  cod: string;
  nombre: string;
}

interface UploadedFile {
  file: File;
  customName: string;
  status: "pendiente" | "subiendo" | "subido" | "error";
  error?: string;
}
function nombreMes(numero: number): string {
  // El mes es de 0 (Enero) a 11 (Diciembre) en JS, así que restamos 1
  const date = new Date(2000, numero - 1, 1);
  return new Intl.DateTimeFormat("es-ES", { month: "long" }).format(date);
}
export default function CompilarRemPage() {
  const [selectedEstablishment, setSelectedEstablishment] = useState("0");
  const [establecimientos, setEstablecimientos] = useState<Establecimiento[]>(
    []
  );
  const [selectedMonth, setSelectedMonth] = useState("0");
  const [files, setFiles] = useState<UploadedFile[]>([]);
  const [compiling, setCompiling] = useState(false);
  const [loading, setLoading] = useState(false);
  const [subido, setSubido] = useState(false);
  const [subiendo, setSubiendo] = useState(false);
  const [uploadedFiles, setUploadedFiles] = useState<UploadedFile[]>([]);
  const [customName, setCustomName] = useState("");

  const router = useRouter();

  useEffect(() => {
    localStorage.clear();
    fetch(API + "getEstablecimientos")
      .then((res) => res.json())
      .then((data) => {
        // API now returns an array of { codDeis: string, nombre: string }
        const parsed = Array.isArray(data)
          ? data.map((d: any) => ({ cod: d.codDeis, nombre: d.nombre }))
          : [];
        setEstablecimientos(parsed);
      })
      .catch((err) => {
        console.error("Error fetching establecimientos:", err);
        setEstablecimientos([]);
      });
  }, []);

  const updateFileStatus = (
    idx: number,
    status: UploadedFile["status"],
    error?: string
  ) => {
    setUploadedFiles((prev) =>
      prev.map((f, i) => (i === idx ? { ...f, status, error } : f))
    );
  };
// Borra el archivo de la lista y de localStorage
  const handleRemoveFile = (idx: number) => {
    setUploadedFiles((prev) => {
      const fileToRemove = prev[idx];
      // Borra de localStorage si existe
      localStorage.removeItem(fileToRemove.customName);
      // Borra del array
      if (files.length === 0) setSubido(false);
      return prev.filter((_, i) => i !== idx);
    });
  };

  // Subida secuencial, inicia justo después de seleccionar archivos
  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files || e.target.files.length === 0) return;
    if (!customName.trim()) {
      alert("Por favor, ingrese un nombre para el archivo.");
      e.target.value = ""; // Reset file input
      return;
    }

    const file = e.target.files[0];

    if (file.name.endsWith(".xlsm") === false) {
      alert("Solo se permiten archivos .xlsm");
      e.target.value = ""; // Reset file input
      return;
    }

    if (uploadedFiles.some((f) => f.customName === customName)) {
      alert("Ya existe un archivo con este nombre. Por favor, elija otro.");
      e.target.value = ""; // Reset file input
      return;
    }

    const newFile: UploadedFile = {
      file,
      customName,
      status: "pendiente",
    };

    setUploadedFiles((prev) => [...prev, newFile]);
    const newFileIndex = uploadedFiles.length;

    setSubiendo(true);
    updateFileStatus(newFileIndex, "subiendo");

    try {
      const formData = new FormData();
      formData.append("archivo", file);
      const res = await fetch(API + "RecolectarREM", {
        method: "POST",
        body: formData,
      });
      if (!res.ok) throw new Error(await res.text());
      const json = await res.json();
      localStorage.setItem(customName, JSON.stringify(json));
      updateFileStatus(newFileIndex, "subido", json.errores || "");
    } catch (e) {
      updateFileStatus(newFileIndex, "error", (e as Error).message);
    } finally {
      setSubiendo(false);
      setCustomName(""); // Reset custom name input
      e.target.value = ""; // Reset file input for next upload
    }
  };

  // Reset page and local storage for uploaded files
  const handleReset = () => {
    localStorage.clear();
    setUploadedFiles([]);
    setSubido(false);
    setSubiendo(false);
    setCompiling(false);
    setSelectedEstablishment("0");
    setSelectedMonth("0");
  };

  const canCompile = uploadedFiles.length > 0 && uploadedFiles.every(f => f.status === 'subido');

  const handleCompile = async () => {
    if (!selectedEstablishment || !selectedMonth) {
      alert("Seleccione establecimiento y mes");
      return;
    }

    // Recoge todos los items del localStorage (solo los archivos subidos)
    const estrategias: any[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (!key) continue;
      try {
        const value = localStorage.getItem(key);
        if (value) estrategias.push(JSON.parse(value));
      } catch (e) {
        continue;
      }
    }

    const payload = {
      codDEIS: selectedEstablishment,
      mes: Number(selectedMonth),
      estrategias,
    };
    setCompiling(true);
    try {
      const res = await fetch(API + "CompilarREM", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });
      if (!res.ok) throw new Error("Error en la compilación");
      const blob = await res.blob();
      // Descarga automática del archivo
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = selectedEstablishment+"A"+selectedMonth.toString().padStart(2, '0')+"-compilado.xlsm";
      a.click();
      setCompiling(false);
    } catch (err: any) {
      alert("Error al compilar: " + err.message);
    }
  };

  return (
    <main className="flex-1 p-6 sm:p-10">
      <div style={{ maxWidth: 640, margin: "0 auto" }}>
        <h1 className="text-2xl font-bold mb-1" style={{ color: "var(--text)" }}>
          Compilar REM Serie A
        </h1>
        <p className="text-sm mb-5" style={{ color: "var(--text-light)" }}>
          Suba los archivos REM y compile la planilla consolidada.
        </p>

        {/* Instructions */}
        <div
          className="mb-5 p-4 rounded-xl text-sm"
          style={{
            background: "var(--surface)",
            border: "1px solid var(--border)",
            color: "var(--text-light)",
          }}
        >
          <ol className="list-decimal list-inside flex flex-col gap-1">
            <li>Seleccione el establecimiento y mes que desea compilar.</li>
            <li>Suba los archivos correspondientes y asegúrese que la versión sea la correcta.</li>
            <li>Si todos los archivos son válidos, haga clic en <strong style={{color:"var(--text)"}}>Compilar</strong> para descargar el archivo consolidado.</li>
          </ol>
          <p className="mt-2 font-semibold" style={{ color: "var(--text)" }}>
            VERSIÓN ACTUAL: 1.2 Febrero 2026
          </p>
        </div>

        {/* Selectors — stacked on mobile */}
        <p className="text-sm font-medium mb-2" style={{ color: "var(--text)" }}>
          {canCompile
            ? "Compilando para:"
            : "Seleccione el establecimiento y mes para compilar"}
        </p>
        <div className="flex flex-col sm:flex-row gap-2 mb-4">
          <select
            className="rounded-md p-2 flex-1"
            style={{
              border: "1px solid var(--border)",
              background: "var(--surface)",
              color: "var(--text)",
              fontWeight: canCompile ? 700 : 400,
            }}
            value={selectedEstablishment}
            disabled={subiendo || subido || canCompile}
            onChange={(e) => setSelectedEstablishment(e.target.value)}
            aria-label="Establecimiento"
          >
            <option value="0">-- Establecimiento --</option>
            {establecimientos.map((est) => (
              <option key={est.cod} value={est.cod}>
                {est.nombre}
              </option>
            ))}
          </select>
          <select
            className="rounded-md p-2 flex-1"
            style={{
              border: "1px solid var(--border)",
              background: "var(--surface)",
              color: "var(--text)",
              fontWeight: canCompile ? 700 : 400,
            }}
            value={selectedMonth}
            disabled={subiendo || subido || canCompile}
            onChange={(e) => setSelectedMonth(e.target.value)}
            aria-label="Mes"
          >
            <option value="0">-- Mes --</option>
            {months.map((m) => (
              <option key={m} value={m}>
                {nombreMes(m)}
              </option>
            ))}
          </select>
        </div>

        {/* File upload row */}
        {selectedEstablishment !== "0" && selectedMonth !== "0" && (
          <div className="flex flex-col sm:flex-row gap-2 mb-4">
            <div className="flex-1">
              <label
                htmlFor="compilar-custom-name"
                className="block text-xs font-medium mb-1"
                style={{ color: "var(--text-light)" }}
              >
                Nombre para el archivo
              </label>
              <input
                id="compilar-custom-name"
                type="text"
                value={customName}
                onChange={(e) => setCustomName(e.target.value)}
                placeholder="Ej: Estrategia Cardiovascular"
                className="w-full rounded-md p-2 text-sm"
                style={{
                  border: "1px solid var(--border)",
                  background: "var(--surface)",
                  color: "var(--text)",
                }}
                disabled={subiendo}
                aria-describedby="compilar-name-hint"
              />
              <p id="compilar-name-hint" className="text-xs mt-0.5" style={{ color: "var(--text-light)" }}>
                Identificador interno del archivo.
              </p>
            </div>
            <div className="flex items-end">
              <label
                htmlFor="file-upload"
                className="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold text-white cursor-pointer transition"
                style={{
                  background: subiendo || !customName.trim() ? "var(--text-light)" : "var(--primary)",
                  cursor: subiendo || !customName.trim() ? "not-allowed" : "pointer",
                  boxShadow: "0 2px 8px rgba(0,0,132,0.12)",
                  whiteSpace: "nowrap",
                }}
                aria-disabled={subiendo || !customName.trim()}
              >
                {subiendo ? <Loader2 size={15} className="animate-spin" aria-hidden /> : <Upload size={15} aria-hidden />}
                {subiendo ? "Subiendo..." : "Subir archivo"}
              </label>
              <input
                disabled={subiendo || !customName.trim()}
                id="file-upload"
                className="excel-input"
                type="file"
                accept=".xlsm"
                onChange={handleFileChange}
                aria-label="Subir archivo REM .xlsm"
              />
            </div>
          </div>
        )}

        {/* Uploaded files list */}
        {uploadedFiles.length > 0 && (
          <div
            className="mb-4 p-4 rounded-xl text-sm"
            style={{
              background: "var(--surface)",
              border: "1px solid var(--border)",
            }}
          >
            <ul className="flex flex-col gap-2">
              {uploadedFiles.map((f, i) => (
                <li
                  key={i}
                  className="flex items-start justify-between gap-2"
                  style={{ color: "var(--text)" }}
                >
                  <span className="flex items-center gap-2 flex-1 min-w-0">
                    {f.status === "subiendo" && (
                      <Loader2 size={14} className="animate-spin flex-shrink-0" style={{ color: "var(--accent)" }} aria-hidden />
                    )}
                    {f.status === "pendiente" && (
                      <AlertCircle size={14} className="flex-shrink-0" style={{ color: "var(--text-light)" }} aria-hidden />
                    )}
                    {f.status === "error" && (
                      <X size={14} className="flex-shrink-0" style={{ color: "var(--error)" }} aria-hidden />
                    )}
                    {f.status === "subido" && !f.error && (
                      <CheckCircle2 size={14} className="flex-shrink-0" style={{ color: "var(--success)" }} aria-hidden />
                    )}
                    {f.status === "subido" && f.error && (
                      <AlertCircle size={14} className="flex-shrink-0" style={{ color: "var(--warning)" }} aria-hidden />
                    )}
                    <span className="font-semibold truncate">{f.customName}</span>
                    <span className="truncate" style={{ color: "var(--text-light)" }}>({f.file.name})</span>
                    {f.status === "subiendo" && <span style={{ color: "var(--accent)" }}>Subiendo...</span>}
                    {f.status === "pendiente" && <span style={{ color: "var(--text-light)" }}>Por subir</span>}
                    {f.status === "error" && <span style={{ color: "var(--error)" }}>{f.error}</span>}
                    {f.status === "subido" && f.error && <span style={{ color: "var(--warning)" }}>{f.error}</span>}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleRemoveFile(i)}
                    className="flex-shrink-0 p-1 rounded transition"
                    style={{ color: "var(--error)" }}
                    title="Eliminar archivo"
                    aria-label={`Eliminar ${f.customName}`}
                  >
                    <X size={16} aria-hidden />
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Compile button */}
        {canCompile && (
          <button
            onClick={handleCompile}
            disabled={compiling}
            className="flex items-center justify-center gap-2 w-full py-3 px-6 rounded-xl text-sm font-semibold text-white transition mb-3"
            style={{
              background: compiling ? "var(--text-light)" : "var(--primary)",
              cursor: compiling ? "not-allowed" : "pointer",
              boxShadow: "0 2px 8px rgba(0,0,132,0.15)",
            }}
          >
            {compiling
              ? <><Loader2 size={15} className="animate-spin" aria-hidden /> Compilando...</>
              : <><Settings size={15} aria-hidden /> Compilar REM</>}
          </button>
        )}

        {/* Secondary actions */}
        <div className="flex gap-2 flex-wrap">
          <button
            onClick={() => router.back()}
            className="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition"
            style={{
              background: "var(--surface)",
              border: "1px solid var(--border)",
              color: "var(--text)",
            }}
          >
            <ArrowLeft size={15} aria-hidden />
            Volver
          </button>
          <button
            type="button"
            onClick={handleReset}
            className="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition"
            style={{
              background: "var(--surface)",
              border: "1px solid var(--border)",
              color: "var(--text)",
            }}
          >
            <RotateCcw size={15} aria-hidden />
            Reiniciar
          </button>
        </div>
      </div>
    </main>
  );
}
