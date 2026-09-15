"use client";

import Link from "next/link";
import { ExternalLink, Newspaper } from "lucide-react";
import Breadcrumbs from "../components/Breadcrumbs";
import Loading from "../components/Loading";
import { formatInformativoDate, useInformativos, type Informativo } from "@/lib/hooks/useInformativos";

function tipoLabel(tipo: string) {
  return tipo === "Noticia" || tipo === "Aviso" || tipo === "Informativo" ? tipo : "Informativo";
}

function InformativoCard({ item }: { item: Informativo }) {
  return (
    <article className="rounded-xl border p-5" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <span className="rounded-full px-2.5 py-1 text-xs font-semibold" style={{ color: "var(--primary)", background: "var(--accent-light)" }}>{tipoLabel(item.tipo)}</span>
        <time className="text-xs" dateTime={item.fecha_publicacion} style={{ color: "var(--text-light)" }}>{formatInformativoDate(item.fecha_publicacion)}</time>
      </div>
      <h2 className="text-xl font-bold" style={{ color: "var(--text)" }}>{item.titulo}</h2>
      <p className="mt-3 text-sm leading-6" style={{ color: "var(--text-light)", whiteSpace: "pre-wrap" }}>{item.contenido}</p>
      {item.url && <a href={item.url} target="_blank" rel="noopener noreferrer" className="mt-4 inline-flex items-center gap-2 text-sm font-semibold" style={{ color: "var(--primary)" }}>{item.texto_enlace || "Ver recurso"}<ExternalLink size={14} aria-hidden="true" /></a>}
    </article>
  );
}

export default function InformativosPage() {
  const { items, loading, error } = useInformativos();

  return (
    <div className="mx-auto max-w-4xl py-4">
      <Breadcrumbs items={[{ label: "Informativos" }]} />
      <div className="page-intro flex items-start gap-3">
        <div className="rounded-lg p-2" style={{ background: "var(--accent-light)" }}><Newspaper size={21} style={{ color: "var(--primary)" }} aria-hidden="true" /></div>
        <div><h1 className="text-3xl font-bold">Noticias, avisos e informativos</h1><p>Información relevante para la red comunal de salud.</p></div>
      </div>
      {loading && <Loading message="Cargando informativos..." />}
      {!loading && error && <div className="rounded-lg border p-4 text-sm" style={{ color: "var(--error-dark)", background: "var(--error-bg)", borderColor: "var(--error)" }}>{error}</div>}
      {!loading && !error && items.length === 0 && <div className="rounded-xl border p-8 text-center text-sm" style={{ color: "var(--text-light)", background: "var(--surface)", borderColor: "var(--border)" }}>No hay informativos vigentes para mostrar.</div>}
      {!loading && !error && items.length > 0 && <div className="flex flex-col gap-4">{items.map((item) => <InformativoCard key={item.id} item={item} />)}</div>}
      <Link href="/" className="mt-6 inline-flex text-sm font-semibold" style={{ color: "var(--primary)" }}>Volver al inicio</Link>
    </div>
  );
}
