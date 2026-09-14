"use client";
import { Suspense } from "react";
import SeguimientoPage from "@/app/components/seguimiento/Seguimiento";

export default function Page() {
  return <Suspense fallback={<div role="status">Cargando seguimiento…</div>}><SeguimientoPage vista="convenio" /></Suspense>;
}
