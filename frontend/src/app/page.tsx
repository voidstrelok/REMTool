import Link from "next/link";
import { ExternalLink, FileDown, Lock } from "lucide-react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { apiUrl } from "@/lib/api";
import { NAVIGATION_SECTIONS, type NavigationItem } from "./components/navigation";
import InformativosPreview from "./components/InformativosPreview";

const informesMensuales = [
  { nombre: "Metas Sanitarias", tipoId: 2 },
  { nombre: "IAAPS", tipoId: 3 },
];

type HomeParameters = {
  monitoreo_enabled?: boolean;
  ultima_actualizacion?: string;
  ultimo_rem_cargado?: { ano: number; mes: number } | null;
};

async function getParametros(): Promise<HomeParameters> {
  try {
    const res = await fetch(apiUrl("getUltimaActualizacion"), { cache: "no-store" });
    if (!res.ok) return { monitoreo_enabled: true };
    return await res.json();
  } catch {
    return { monitoreo_enabled: true };
  }
}

function formatDate(value?: string) {
  const match = value?.match(/^(\d{4})-(\d{2})-(\d{2})/);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : value ?? "Sin información";
}

function ModuleCard({ item, disabled }: { item: NavigationItem; disabled: boolean }) {
  const Icon = item.icon;
  const content = (
    <Card className="h-full transition-all duration-200 group-hover:-translate-y-0.5 group-hover:shadow-md" style={{ border: "1px solid var(--border)", background: "var(--surface)", opacity: disabled ? 0.5 : 1 }}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-3">
          <div className="flex min-w-0 items-center gap-3">
            <div className="shrink-0 rounded-lg p-2.5" style={{ background: "var(--accent-light)" }}>
              <Icon size={21} style={{ color: "var(--primary)" }} aria-hidden="true" />
            </div>
            <span className="font-semibold leading-tight" style={{ color: "var(--text)" }}>{item.label}</span>
          </div>
          {disabled ? <Lock size={14} className="mt-1 shrink-0" style={{ color: "var(--text-light)" }} aria-label="No disponible" /> : item.external ? <ExternalLink size={14} className="mt-1 shrink-0" style={{ color: "var(--text-light)" }} aria-label="Enlace externo" /> : item.badge ? <span className="rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide" style={{ color: "var(--primary)", background: "var(--accent-light)", border: "1px solid var(--border)" }}>{item.badge}</span> : null}
        </div>
      </CardHeader>
      <CardContent className="pt-0"><p className="text-sm" style={{ color: "var(--text-light)" }}>{item.description}</p></CardContent>
    </Card>
  );

  if (disabled) return <div className="cursor-not-allowed rounded-xl" aria-disabled="true">{content}</div>;
  return <Link href={item.href} target={item.external ? "_blank" : undefined} rel={item.external ? "noopener noreferrer" : undefined} className="group rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring)]">{content}</Link>;
}

export default async function Home() {
  const currentYear = new Date().getFullYear();
  const parametros = await getParametros();
  const monitoreoEnabled = parametros.monitoreo_enabled ?? true;
  const quickItems = ["panel-rem", "revisar-rem", "consultar-rem"];

  return (
    <div className="py-4">

      <InformativosPreview />

      {NAVIGATION_SECTIONS.map((section) => {
        const SectionIcon = section.icon;
        return (
          <section key={section.id} className="mb-9">
            <div className="mb-4 flex items-start gap-3">
              <div className="rounded-lg p-2" style={{ background: "var(--accent-light)" }}><SectionIcon size={19} style={{ color: "var(--primary)" }} aria-hidden="true" /></div>
              <div><h2 className="text-xl font-bold" style={{ color: "var(--text)" }}>{section.label}</h2><p className="text-sm" style={{ color: "var(--text-light)" }}>Herramientas para {section.id === "operacion-rem" ? "trabajar con archivos y registros REM" : section.id === "seguimiento" ? "consultar avances y resultados" : "resolver tareas complementarias"}.</p></div>
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {section.items.map((item) => <ModuleCard key={item.id} item={item} disabled={Boolean(item.requiresMonitoreo && !monitoreoEnabled)} />)}
            </div>
          </section>
        );
      })}

      <section className="rounded-xl border p-5" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
        <div className="mb-4"><h2 className="text-xl font-bold" style={{ color: "var(--text)" }}>Descargas mensuales</h2><p className="text-sm" style={{ color: "var(--text-light)" }}>Informes PDF del año {currentYear}.</p></div>
        <div className="flex flex-wrap gap-3">
          {informesMensuales.map(({ nombre, tipoId }) => <a key={tipoId} href={apiUrl(`informeMensual/${tipoId}/${currentYear}`)} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-semibold transition-colors hover:bg-[var(--surface-alt)]" style={{ color: "var(--primary)", borderColor: "var(--border)" }}><FileDown size={16} aria-hidden="true" />{nombre} {currentYear}</a>)}
        </div>
      </section>
    </div>
  );
}
