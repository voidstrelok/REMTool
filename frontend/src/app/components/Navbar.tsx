"use client";
import { Suspense } from "react";
import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { Menu, Stethoscope, ExternalLink } from "lucide-react";
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import InfoActualizacion from "../InfoActualizacion";

interface NavLink {
  href: string;
  label: string;
  tipoParam?: string;
  external?: boolean;
}

const navLinks: NavLink[] = [
  { href: "/CompilarREM", label: "Compilar REM" },
  { href: "/RevisarREM", label: "Revisar REM" },
  { href: "/RasterizarPDF", label: "Rasterizar PDF" },
  /*{ href: "/ConstruyeREM", label: "Construye REM" },*/
  { href: "/Indicadores?tipo=MetasSanitarias", label: "Metas Sanitarias", tipoParam: "MetasSanitarias" },
  { href: "/Indicadores?tipo=IAAPS", label: "IAAPS", tipoParam: "IAAPS" },
  /*{ href: "/Convenios", label: "Convenios" },*/
  {
    href: "https://drive.google.com/drive/folders/1vlCWcZMrdayOv5n8-ev1PstQCQIoSQCT?usp=sharing",
    label: "Repositorio",
    external: true,
  },
];

function NavbarContent() {
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const isActive = (link: NavLink) => {
    if (link.external) return false;
    if (link.tipoParam) {
      return (
        pathname?.startsWith("/Indicadores") &&
        searchParams.get("tipo") === link.tipoParam
      );
    }
    return pathname?.startsWith(link.href) ?? false;
  };

  const linkClass = (link: NavLink) =>
    `px-3 py-1.5 rounded-md text-sm font-medium transition-colors flex items-center gap-1.5 ${
      isActive(link)
        ? "bg-white/20 text-white"
        : "text-white/80 hover:text-white hover:bg-white/10"
    }`;

  return (
    <header style={{ background: "var(--primary)" }} className="shadow-md">
      {/* Main nav row */}
      <div className="max-w-[1500px] mx-auto px-4 sm:px-6 flex items-center justify-between h-14">
        {/* Brand */}
        <Link
          href="/"
          className="flex items-center gap-2 font-bold text-lg text-white tracking-tight hover:opacity-90 transition-opacity"
        >
          <Stethoscope size={20} />
          <span>Estadísticas Red Comunal de Salud Monte Patria</span>
        </Link>

        {/* Desktop nav */}
        <nav className="hidden md:flex items-center gap-0.5">
          {navLinks.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              target={link.external ? "_blank" : undefined}
              rel={link.external ? "noopener noreferrer" : undefined}
              className={linkClass(link)}
            >
              {link.label}
              {link.external && <ExternalLink size={12} className="opacity-60" />}
            </Link>
          ))}
        </nav>

        {/* Mobile hamburger */}
        <Sheet>
          <SheetTrigger
            className="md:hidden p-2 rounded-md text-white hover:bg-white/10 transition-colors"
            aria-label="Abrir menú de navegación"
          >
            <Menu size={22} />
          </SheetTrigger>
          <SheetContent side="left" className="w-64 p-0">
            <div
              className="flex items-center gap-2 px-5 py-4 border-b"
              style={{ background: "var(--primary)", borderColor: "rgba(255,255,255,0.1)" }}
            >
              <Stethoscope size={18} className="text-white" />
              <span className="font-bold text-base text-white">REMTool</span>
            </div>
            <nav className="flex flex-col gap-1 p-3">
              {navLinks.map((link) => {
                const active = isActive(link);
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    target={link.external ? "_blank" : undefined}
                    rel={link.external ? "noopener noreferrer" : undefined}
                    className={`flex items-center justify-between px-3 py-2.5 rounded-md text-sm font-medium transition-colors ${
                      active
                        ? "text-white"
                        : "text-gray-700 hover:bg-gray-100"
                    }`}
                    style={active ? { background: "var(--primary)" } : {}}
                  >
                    {link.label}
                    {link.external && <ExternalLink size={13} className="opacity-50" />}
                  </Link>
                );
              })}
            </nav>
          </SheetContent>
        </Sheet>
      </div>

      {/* Update info strip */}
      <div
        className="border-t px-4 sm:px-6 py-1 max-w-[1500px] mx-auto"
        style={{ borderColor: "rgba(255,255,255,0.08)" }}
      >
        
          <InfoActualizacion />
        
      </div>
    </header>
  );
}

export default function Navbar() {
  return (
    <Suspense
      fallback={
        <header style={{ background: "var(--primary)" }} className="shadow-md">
          <div className="max-w-[1500px] mx-auto px-4 sm:px-6 flex items-center h-14">
            <span className="flex items-center gap-2 font-bold text-lg text-white tracking-tight">
              <Stethoscope size={20} />
              <span>REMTool</span>
            </span>
          </div>
        </header>
      }
    >
      <NavbarContent />
    </Suspense>
  );
}
