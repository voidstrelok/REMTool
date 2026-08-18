
import type { Metadata } from "next";
import "./globals.css";
import Navbar from "./components/Navbar";

export const metadata: Metadata = {
  title: "REMTool - DESAM Monte Patria",
  description: "Herramientas REM DESAM Monte Patria",
  icons: { icon: "/favicon.svg" },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es">
      <body className="antialiased">
        <div className="min-h-screen flex flex-col" style={{ background: "var(--background)" }}>
          <Navbar />
          <main className="w-full max-w-[1500px] mx-auto px-4 sm:px-6 flex-1 py-6">
            {children}
          </main>
          <footer className="footer-rem">
            <span>
              Consultas y sugerencias{" "}
              <a className="font-semibold hover:underline" href="mailto:ricardocontreras@mpatria.cl">
                ricardocontreras@mpatria.cl
              </a>
            </span>
            <span>
              <a
                className="Anta"
                href="https://thepit.cl"
                target="_blank"
                rel="noopener noreferrer"
                style={{ color: "var(--primary)" }}
              >
                ThePit
              </a>
              {" "}para DESAM Monte Patria
            </span>
          </footer>
        </div>
      </body>
    </html>
  );
}
