"use client";

import { Suspense, useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { ChevronDown, ExternalLink, Menu, Stethoscope } from "lucide-react";
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import InfoActualizacion from "../InfoActualizacion";
import {
  isNavigationItemActive,
  NAVIGATION_SECTIONS,
  type NavigationItem,
} from "./navigation";

function NavigationLink({ item, mobile = false, onNavigate }: {
  item: NavigationItem;
  mobile?: boolean;
  onNavigate?: () => void;
}) {
  const className = mobile
    ? "flex items-center justify-between gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors"
    : "flex items-center gap-1.5 rounded-md px-3 py-2 text-sm font-medium transition-colors";

  return (
    <Link
      href={item.href}
      target={item.external ? "_blank" : undefined}
      rel={item.external ? "noopener noreferrer" : undefined}
      onClick={onNavigate}
      className={className}
    >
      <span>{item.label}</span>
      {item.external && <ExternalLink size={13} aria-hidden="true" className="opacity-60" />}
    </Link>
  );
}

function NavbarContent() {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [openDesktopSection, setOpenDesktopSection] = useState<string | null>(null);
  const tipo = searchParams.get("tipo");

  useEffect(() => {
    setOpenDesktopSection(null);
  }, [pathname, tipo]);

  useEffect(() => {
    const closeOnOutsideClick = (event: PointerEvent) => {
      const target = event.target;
      if (target instanceof Element && !target.closest("[data-desktop-nav]")) {
        setOpenDesktopSection(null);
      }
    };

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpenDesktopSection(null);
    };

    document.addEventListener("pointerdown", closeOnOutsideClick);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOnOutsideClick);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, []);

  const sectionIsActive = (sectionId: string) => {
    const section = NAVIGATION_SECTIONS.find((candidate) => candidate.id === sectionId);
    return section?.items.some((item) => isNavigationItemActive(item, pathname, tipo)) ?? false;
  };

  return (
    <header style={{ background: "var(--primary)" }} className="shadow-md">
      <div className="max-w-[1500px] mx-auto px-4 sm:px-6 flex items-center justify-between min-h-14 gap-4">
        <Link href="/" className="flex min-w-0 items-center gap-2 font-bold text-lg text-white tracking-tight hover:opacity-90 transition-opacity">
          <Stethoscope size={20} aria-hidden="true" className="shrink-0" />
          <span className="truncate">Estadísticas Red Comunal de Salud Monte Patria</span>
        </Link>

        <nav className="hidden md:flex items-center gap-1" aria-label="Navegación principal" data-desktop-nav>
          {NAVIGATION_SECTIONS.map((section) => {
            const active = sectionIsActive(section.id);
            const SectionIcon = section.icon;
            const isOpen = openDesktopSection === section.id;
            return (
              <div key={section.id} className="relative">
                <button
                  type="button"
                  aria-expanded={isOpen}
                  aria-haspopup="menu"
                  onClick={() => setOpenDesktopSection((current) => current === section.id ? null : section.id)}
                  className={`flex cursor-pointer items-center gap-1.5 rounded-md px-3 py-2 text-sm font-medium transition-colors ${active || isOpen ? "bg-white/20 text-white" : "text-white/80 hover:bg-white/10 hover:text-white"}`}
                >
                  <SectionIcon size={15} aria-hidden="true" />
                  <span>{section.label}</span>
                  <ChevronDown size={14} aria-hidden="true" className={`transition-transform ${isOpen ? "rotate-180" : ""}`} />
                </button>
                {isOpen && <div className="absolute right-0 z-30 mt-1 min-w-72 rounded-xl border p-2 shadow-xl" role="menu" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
                  {section.items.map((item) => {
                    const itemActive = isNavigationItemActive(item, pathname, tipo);
                    return (
                      <Link
                        key={item.id}
                        href={item.href}
                        target={item.external ? "_blank" : undefined}
                        rel={item.external ? "noopener noreferrer" : undefined}
                        role="menuitem"
                        onClick={() => setOpenDesktopSection(null)}
                        className="block rounded-lg p-3 transition-colors hover:bg-black/5"
                        style={{ background: itemActive ? "var(--accent-light)" : undefined }}
                      >
                        <span className="flex items-center justify-between gap-3 text-sm font-semibold" style={{ color: itemActive ? "var(--primary)" : "var(--text)" }}>
                          <span>{item.label}</span>
                          {item.external && <ExternalLink size={13} aria-hidden="true" className="shrink-0 opacity-60" />}
                        </span>
                        <span className="mt-1 block text-xs" style={{ color: "var(--text-light)" }}>{item.description}</span>
                      </Link>
                    );
                  })}
                </div>}
              </div>
            );
          })}
        </nav>

        <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
          <SheetTrigger className="md:hidden shrink-0 rounded-md p-2 text-white transition-colors hover:bg-white/10" aria-label="Abrir menú de navegación">
            <Menu size={22} aria-hidden="true" />
          </SheetTrigger>
          <SheetContent side="left" className="w-80 max-w-[88vw] p-0">
            <div className="flex items-center gap-2 border-b px-5 py-4" style={{ background: "var(--primary)", borderColor: "rgba(255,255,255,0.1)" }}>
              <Stethoscope size={18} className="text-white" aria-hidden="true" />
              <span className="font-bold text-base text-white">REMTool</span>
            </div>
            <nav className="flex flex-col gap-2 overflow-y-auto p-3" aria-label="Navegación móvil">
              {NAVIGATION_SECTIONS.map((section) => {
                const active = sectionIsActive(section.id);
                const SectionIcon = section.icon;
                return (
                  <details key={section.id} open={active} className="rounded-xl border" style={{ borderColor: "var(--border)" }}>
                    <summary className="flex cursor-pointer list-none items-center justify-between gap-2 px-3 py-3 text-sm font-semibold" style={{ color: "var(--text)" }}>
                      <span className="flex items-center gap-2"><SectionIcon size={16} style={{ color: "var(--primary)" }} aria-hidden="true" />{section.label}</span>
                      <ChevronDown size={15} aria-hidden="true" />
                    </summary>
                    <div className="flex flex-col gap-1 border-t p-2" style={{ borderColor: "var(--border)" }}>
                      {section.items.map((item) => {
                        const itemActive = isNavigationItemActive(item, pathname, tipo);
                        return (
                          <div key={item.id} style={itemActive ? { background: "var(--primary)", borderRadius: 8 } : undefined}>
                            <div style={itemActive ? { color: "white" } : { color: "var(--text)" }}>
                              <NavigationLink item={item} mobile onNavigate={() => setMobileOpen(false)} />
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </details>
                );
              })}
            </nav>
          </SheetContent>
        </Sheet>
      </div>

      <div className="border-t px-4 py-1 sm:px-6" style={{ borderColor: "rgba(255,255,255,0.08)" }}>
        <div className="max-w-[1500px] mx-auto"><InfoActualizacion /></div>
      </div>
    </header>
  );
}

export default function Navbar() {
  return (
    <Suspense fallback={<header style={{ background: "var(--primary)" }} className="h-14 shadow-md"><div className="max-w-[1500px] mx-auto flex h-full items-center px-4 sm:px-6"><span className="flex items-center gap-2 font-bold text-lg text-white"><Stethoscope size={20} aria-hidden="true" />REMTool</span></div></header>}>
      <NavbarContent />
    </Suspense>
  );
}
