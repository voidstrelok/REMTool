import type { LucideIcon } from "lucide-react";
import {
  BarChart3,
  Building2,
  CheckCircle2,
  ClipboardList,
  ExternalLink,
  FileImage,
  FolderOpen,
  Layers3,
  Newspaper,
  Search,
  Settings,
} from "lucide-react";

export type NavigationItem = {
  id: string;
  label: string;
  href: string;
  description: string;
  icon: LucideIcon;
  badge?: string;
  external?: boolean;
  tipoParam?: string;
  requiresMonitoreo?: boolean;
  secondary?: boolean;
};

export type NavigationSection = {
  id: string;
  label: string;
  icon: LucideIcon;
  items: readonly NavigationItem[];
};

export const NAVIGATION_SECTIONS: readonly NavigationSection[] = [
  {
    id: "operacion-rem",
    label: "Operación REM",
    icon: ClipboardList,
    items: [
      {
        id: "panel-rem",
        label: "Panel REM",
        href: "/PanelREM",
        description: "Estado de revisión de archivos REM por establecimiento y serie.",
        icon: ClipboardList,
      },
      {
        id: "revisar-rem",
        label: "Revisar REM",
        href: "/RevisarREM",
        description: "Validación y detección de inconsistencias en archivos REM.",
        icon: CheckCircle2,
      },
      {
        id: "consultar-rem",
        label: "Consultar REM",
        href: "/ConsultarREM",
        description: "Consulta de registros REM por periodo y establecimiento.",
        icon: Search,
        badge: "Nuevo",
      },
      {
        id: "construir-rem",
        label: "Construir REM Serie A",
        href: "/ConstruirREM",
        description: "Arme progresivamente un REM Serie A a partir de sus partes.",
        icon: Layers3,
        badge: "Nuevo",
      },
      {
        id: "compilar-rem",
        label: "Compilar REM Serie A",
        href: "/CompilarREM",
        description: "Consolidación de archivos REM Serie A en una única planilla.",
        icon: Settings,
      },
      {
        id: "compilar-rem-p",
        label: "Compilar REM Serie P",
        href: "/CompilarREMP",
        description: "Consolidación de archivos REM Serie P en una única planilla.",
        icon: Settings,
        secondary: true,
      },
    ],
  },
  {
    id: "seguimiento",
    label: "Seguimiento",
    icon: BarChart3,
    items: [
      {
        id: "metas-sanitarias",
        label: "Metas Sanitarias",
        href: "/Indicadores?tipo=MetasSanitarias",
        description: "Seguimiento del avance de metas sanitarias.",
        icon: BarChart3,
        tipoParam: "MetasSanitarias",
        requiresMonitoreo: true,
      },
      {
        id: "iaaps",
        label: "IAAPS",
        href: "/Indicadores?tipo=IAAPS",
        description: "Seguimiento del avance de indicadores IAAPS.",
        icon: Building2,
        tipoParam: "IAAPS",
        requiresMonitoreo: true,
      },
      {
        id: "convenios",
        label: "Convenios",
        href: "/Convenios",
        description: "Seguimiento del avance de convenios.",
        icon: Building2,
        requiresMonitoreo: true,
      },
    ],
  },
  {
    id: "utilidades",
    label: "Utilidades",
    icon: FolderOpen,
    items: [
      {
        id: "rasterizar-pdf",
        label: "Rasterizar PDF",
        href: "/RasterizarPDF",
        description: "Convierte las capas y anotaciones de un PDF en imagen permanente.",
        icon: FileImage,
      },
      {
        id: "informativos",
        label: "Informativos",
        href: "/Informativos",
        description: "Noticias, avisos e información de interés.",
        icon: Newspaper,
      },
      {
        id: "consolidados-excel",
        label: "Consolidados Excel",
        href: "/Consolidados",
        description: "Descargue los consolidados para filtrar por establecimiento, mes y sector en Excel.",
        icon: FolderOpen,
      },
      {
        id: "repositorio-rem",
        label: "Repositorio REM",
        href: "https://drive.google.com/drive/folders/1vlCWcZMrdayOv5n8-ev1PstQCQIoSQCT?usp=sharing",
        description: "Archivos REM y consolidados en Google Drive.",
        icon: ExternalLink,
        external: true,
      },
    ],
  },
];

export const NAVIGATION_ITEMS = NAVIGATION_SECTIONS.flatMap((section) => section.items);

export function isNavigationItemActive(
  item: NavigationItem,
  pathname: string | null,
  tipo: string | null,
) {
  if (item.external || !pathname) return false;
  if (item.tipoParam) {
    return pathname.startsWith("/Indicadores") && tipo === item.tipoParam;
  }
  return pathname === item.href || pathname.startsWith(`${item.href}/`);
}
