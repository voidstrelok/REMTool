import Link from "next/link";
import { Settings, CheckCircle2, BarChart2, Building2, FolderOpen, ExternalLink, FileDown, Lock, FileImage, ClipboardList } from "lucide-react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";

const allTools = [
  {
    name: "Panel REM",
    icon: ClipboardList,
    link: "/PanelREM",
    comment: "Estado de revisión de archivos REM por establecimiento y serie.",
    external: false,
    requiresMonitoreo: false,
  },
  {
    name: "Compilar REM Serie A",
    icon: Settings,
    link: "/CompilarREM",
    comment: "Consolidación de archivos REM Serie A en una única planilla.",
    external: false,
    requiresMonitoreo: false,
  },/*
  {
    name: "Compilar REM Serie P",
    icon: Settings,
    link: "/CompilarREMP",
    comment: "Consolidación de archivos REM Serie P en una única planilla.",
    external: false,
    requiresMonitoreo: false,
  },*/
  {
    name: "Revisar REM",
    icon: CheckCircle2,
    link: "/RevisarREM",
    comment: "Validación y detección de inconsistencias en archivos REM.",
    external: false,
    requiresMonitoreo: false,
  },
  {
    name: "Metas Sanitarias",
    icon: BarChart2,
    link: "/Indicadores?tipo=MetasSanitarias",
    comment: "Seguimiento del avance de metas sanitarias.",
    external: false,
    requiresMonitoreo: true,
  },
  {
    name: "IAAPS",
    icon: Building2,
    link: "/Indicadores?tipo=IAAPS",
    comment: "Seguimiento del avance de indicadores IAAPS.",
    external: false,
    requiresMonitoreo: true,
  },/*
  {
    name: "Convenios",
    icon: Building2,
    link: "/Convenios",
    comment: "Seguimiento del avance de convenios.",
    external: false,
    requiresMonitoreo: true,
  },*/
  {
    name: "Repositorio REM",
    icon: FolderOpen,
    link: "https://drive.google.com/drive/folders/1vlCWcZMrdayOv5n8-ev1PstQCQIoSQCT?usp=sharing",
    comment: "Archivos REM y consolidados en Google Drive.",
    external: true,
    requiresMonitoreo: false,
  },
  {
    name: "Rasterizar PDF",
    icon: FileImage,
    link: "/RasterizarPDF",
    comment: "Convierte las capas y anotaciones de un PDF en imagen permanente.",
    external: false,
    requiresMonitoreo: false,
  },
];

const informesMensuales = [
  { nombre: "Metas Sanitarias", tipoId: 2 },
  { nombre: "IAAPS", tipoId: 3 },
];

const API = process.env.NEXT_PUBLIC_API ?? "";

async function getParametros() {
  try {
    const res = await fetch(`${API}getUltimaActualizacion`, { cache: "no-store" });
    if (!res.ok) return { monitoreo_enabled: true };
    return await res.json();
  } catch {
    return { monitoreo_enabled: true };
  }
}

export default async function Home() {
  const currentYear = new Date().getFullYear();
  const parametros = await getParametros();
  const monitoreoEnabled: boolean = parametros.monitoreo_enabled ?? true;
  const tools = allTools.map((t) => ({
    ...t,
    disabled: t.requiresMonitoreo && !monitoreoEnabled,
  }));
  return (
    <div className="py-4">
      <div className="mb-8">
        <h2
          className="text-2xl font-bold mb-1"
          style={{ color: "var(--text)" }}
        >
          Módulos disponibles
        </h2>
        <p className="text-sm" style={{ color: "var(--text-light)" }}>
          Seleccione la herramienta que desea utilizar.
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
        {tools.map((tool) => {
          const Icon = tool.icon;
          const cardContent = (
            <Card
              className="h-full transition-all duration-200 group-hover:shadow-md"
              style={{
                border: "1px solid var(--border)",
                background: "var(--surface)",
                opacity: tool.disabled ? 0.45 : 1,
              }}
            >
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-3">
                    <div
                      className="p-2.5 rounded-lg flex-shrink-0"
                      style={{ background: "var(--accent-light)" }}
                    >
                      <Icon
                        size={22}
                        style={{ color: "var(--primary)" }}
                      />
                    </div>
                    <span
                      className="font-semibold text-base leading-tight"
                      style={{ color: "var(--text)" }}
                    >
                      {tool.name}
                    </span>
                  </div>
                  {tool.disabled ? (
                    <Lock size={14} className="flex-shrink-0 mt-1" style={{ color: "var(--text-light)" }} />
                  ) : tool.external ? (
                    <ExternalLink
                      size={14}
                      className="flex-shrink-0 mt-1"
                      style={{ color: "var(--text-light)" }}
                    />
                  ) : null}
                </div>
              </CardHeader>
              <CardContent className="pt-0">
                <p className="text-sm" style={{ color: "var(--text-light)" }}>
                  {tool.comment}
                </p>
              </CardContent>
            </Card>
          );
          if (tool.disabled) {
            return (
              <div key={tool.name} className="rounded-xl cursor-not-allowed">
                {cardContent}
              </div>
            );
          }
          return (
            <Link
              href={tool.link}
              key={tool.name}
              target={tool.external ? "_blank" : undefined}
              rel={tool.external ? "noopener noreferrer" : undefined}
              className="group focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring)] rounded-xl"
            >
              {cardContent}
            </Link>
          );
        })}
      </div>

      <div className="mt-10">
        <h2
          className="text-2xl font-bold mb-1"
          style={{ color: "var(--text)" }}
        >
          Informes Mensuales
        </h2>
        <p className="text-sm mb-5" style={{ color: "var(--text-light)" }}>
          Descargue el informe mensual en formato PDF.
        </p>
        <div className="flex flex-wrap gap-3">
          {informesMensuales.map(({ nombre, tipoId }) => (
            <a
              key={tipoId}
              href={`${API}informeMensual/${tipoId}/${currentYear}`}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors"
              style={{
                background: "var(--accent-light)",
                color: "var(--primary)",
                border: "1px solid var(--border)",
              }}
            >
              <FileDown size={16} />
              {nombre} {currentYear}
            </a>
          ))}
        </div>
      </div>
    </div>
  );
}






