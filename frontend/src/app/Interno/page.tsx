"use client";

import Link from "next/link";
import { IdCard, Lock } from "lucide-react";
import Breadcrumbs from "../components/Breadcrumbs";
import InternoGate from "./InternoGate";

// Herramientas del panel interno: agregar aquí las nuevas.
const TOOLS = [
  { href: "/Interno/ComprobadorRut", label: "Comprobador de RUT", description: "Valida DV, verifica inscripción Fonasa y cruza contra otra lista.", icon: IdCard },
];

export default function InternoPage() {
  return (
    <div className="mx-auto max-w-4xl py-4">
      <Breadcrumbs items={[{ label: "Panel interno" }]} />
      <InternoGate>
        <div className="page-intro flex items-start gap-3">
          <div className="rounded-lg p-2" style={{ background: "var(--accent-light)" }}><Lock size={21} style={{ color: "var(--primary)" }} aria-hidden="true" /></div>
          <div><h1 className="text-3xl font-bold">Panel interno</h1><p>Herramientas de uso interno del departamento.</p></div>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          {TOOLS.map(({ href, label, description, icon: Icon }) => (
            <Link key={href} href={href} className="flex gap-3 rounded-xl border p-5 hover:shadow-md" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
              <Icon size={22} style={{ color: "var(--primary)" }} aria-hidden="true" />
              <div><h2 className="font-bold">{label}</h2><p className="mt-1 text-sm" style={{ color: "var(--text-light)" }}>{description}</p></div>
            </Link>
          ))}
        </div>
      </InternoGate>
    </div>
  );
}
