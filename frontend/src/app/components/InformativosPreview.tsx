"use client";

import Link from "next/link";
import { ArrowRight, ExternalLink, Newspaper } from "lucide-react";
import { formatInformativoDate, useInformativos } from "@/lib/hooks/useInformativos";

export default function InformativosPreview() {
  const { items, loading } = useInformativos();
  const visibles = items.slice(0, 3);

  if (loading || visibles.length === 0) return null;

  return (
    <section className="mb-9 rounded-xl border p-5" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
      <div className="mb-4 flex items-start justify-between gap-4">
        <div className="flex items-start gap-3">
          <div className="rounded-lg p-2" style={{ background: "var(--accent-light)" }}><Newspaper size={19} style={{ color: "var(--primary)" }} aria-hidden="true" /></div>
          <div><h2 className="text-xl font-bold" style={{ color: "var(--text)" }}>Noticias y avisos</h2><p className="text-sm" style={{ color: "var(--text-light)" }}>Información vigente para la red.</p></div>
        </div>
        <Link href="/Informativos" className="inline-flex shrink-0 items-center gap-1 text-sm font-semibold" style={{ color: "var(--primary)" }}>Ver todos <ArrowRight size={14} aria-hidden="true" /></Link>
      </div>
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        {visibles.map((item) => <article key={item.id} className="rounded-lg border p-4" style={{ borderColor: "var(--border-light)", background: "var(--surface-alt)" }}><div className="flex items-center justify-between gap-2"><span className="text-[11px] font-semibold uppercase tracking-wide" style={{ color: "var(--primary)" }}>{item.tipo}</span><time className="text-[11px]" dateTime={item.fecha_publicacion} style={{ color: "var(--text-light)" }}>{formatInformativoDate(item.fecha_publicacion)}</time></div><h3 className="mt-2 line-clamp-2 text-sm font-bold" style={{ color: "var(--text)" }}>{item.titulo}</h3><p className="mt-2 line-clamp-3 text-xs leading-5" style={{ color: "var(--text-light)", whiteSpace: "pre-wrap" }}>{item.contenido}</p>{item.url && <a href={item.url} target="_blank" rel="noopener noreferrer" className="mt-3 inline-flex items-center gap-1 text-xs font-semibold" style={{ color: "var(--primary)" }}>{item.texto_enlace || "Ver recurso"}<ExternalLink size={12} aria-hidden="true" /></a>}</article>)}
      </div>
    </section>
  );
}
