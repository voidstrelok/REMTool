"use client";
import { useRouter } from "next/navigation";
import React, { useEffect, useState } from "react";

const establishments = ["Establecimiento 1", "Establecimiento 2"];
const months = [9, 10, 11, 12];
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
export default function ConstruyeRemPage() {
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
    <main className="flex-1 flex items-center justify-center text-center p-8">
      <div>
        <h2 className="text-3xl font-semibold text-gray-700">
          Compilar REM Serie A
        </h2>
        <div className="excel-msg mt-1 mb-2 p-4 rounded-xl text-sm text-left bg-gray-50 border border-gray-200 text-gray-700">
              <b>1.-</b>Seleccione el establecimiento y mes que desea compilar. <br />
              <b>2.-</b>Suba los archivos correspondientes, asegurar que la version sea la correcta. <br />
              <b>3.-</b>Si todos los archivos son válidos, haga clic en "Compilar" para iniciar el proceso. Se descargará su archivo compilado.<br />
            </div>
        <span className="text-md font-medium text-gray-700">
          {canCompile ? "Compilando para:" : "Seleccione el establecimiento y mes para compilar"}
        </span>
        <br></br>

        <select
          className={`rounded-md p-2 me-2 ${canCompile ? "appearance-none font-bold" : "bg-white border border-gray-300"}`}
          value={selectedEstablishment}
          disabled={subiendo || subido || canCompile}
          onChange={(e) => setSelectedEstablishment(e.target.value)}
        >
          <option value="0">-- Seleccione establecimiento --</option>
          {establecimientos.map((est) => (
            <option key={est.cod} value={est.cod}>
              {est.nombre}
            </option>
          ))}
        </select>
        <select
          className={`rounded-md p-2 me-2 ${canCompile ? "appearance-none font-bold" : "bg-white border border-gray-300"}`}
          value={selectedMonth}
          disabled={subiendo || subido || canCompile}
          onChange={(e) => setSelectedMonth(e.target.value)}
        >
          <option onSelect={() => localStorage.clear()} value="0">-- Seleccione mes --</option>
          {months.map((m) => (
            <option key={m} value={m}>
              {nombreMes(m)}
            </option>
          ))}
        </select>
        <div
          hidden={selectedEstablishment === "0" || selectedMonth === "0"}
          className="mt-4"
        >
          <input
            type="text"
            value={customName}
            onChange={(e) => setCustomName(e.target.value)}
            placeholder="Nombre para el archivo"
            className="rounded-md p-2 border border-gray-300 bg-white"
            disabled={subiendo}
          />
          <label
            htmlFor="file-upload"
            className={`shadow inline-block py-2 px-4 ml-2 rounded-xl text-md font-medium text-white cursor-pointer transition ${
              subiendo || !customName.trim()
                ? "bg-gray-400 cursor-not-allowed"
                : "bg-emerald-600 hover:bg-emerald-700"
            }`}
          >
            {subiendo ? "Subiendo..." : "📂 Subir Archivo"}
          </label>
          <input
            disabled={subiendo || !customName.trim()}
            id="file-upload"
            className="excel-input"
            type="file"
            accept=".xlsm"
            onChange={handleFileChange}
          />
        </div>

        <div
          hidden={uploadedFiles.length === 0}
          className="excel-msg mt-4 p-5 rounded-xl text-sm text-left bg-gray-50 border border-gray-200 text-gray-700"
        >
          <ul>
            {uploadedFiles.map((f, i) => (
              <li
                className="flex items-center font-semibold justify-between gap-2 py-1"
                key={i}
              >
                <span>
                  {f.customName} ({f.file.name}) -
                  {f.status === "subiendo" && (
                    <span className="text-blue-500">🔄️ Subiendo...</span>
                  )}
                  {f.status === "pendiente" && (
                    <span className="text-gray-500">⏹️ Por subir {f.error}</span>
                  )}
                  {f.status === "error" && (
                    <span className="text-red-500">🔴 {f.error}</span>
                  )}
                  {!f.error && f.status === "subido" && (
                    <span className="text-green-500">🟢 Subido</span>
                  )}
                  {f.status === "subido" && f.error && (
                    <span className="text-orange-500">🟡 {f.error}</span>
                  )}
                </span>
                
                <button 
                  type="button"
                  onClick={() => handleRemoveFile(i)}
                  className="bg-transparent border-none text-red-600 font-bold cursor-pointer text-lg hover:text-red-800"
                  title="Eliminar archivo"
                >
                  ×
                </button>
              </li>
            ))}
          </ul>
        </div>
        <br></br>
        <button
          onClick={handleCompile} hidden={!canCompile}
          className={` shadow inline-block w-full py-4 px-6 rounded-xl text-md font-medium text-white cursor-pointer transition ${
            !compiling
              ? "bg-emerald-600 hover:bg-emerald-700"
              : "bg-gray-400 cursor-not-allowed"
          }`}
        >
          {compiling ? "🔄 Compilando..." : "⚙️ Compilar"}
        </button>
        <button
          onClick={() => router.back()}
          className="me-4 px-4 py-2 bg-gray-200 hover:bg-gray-300 rounded-lg text-gray-700 font-medium shadow cursor-pointer"
        >
          ← Volver
        </button>
        <button
          type="button"
          className="mt-4 px-4 py-2 bg-blue-200 hover:bg-blue-300 rounded-lg text-gray-700 font-medium shadow cursor-pointer"
          onClick={handleReset}
        >
          🔄️ Reiniciar
        </button>
      </div>
    </main>
  );
}
