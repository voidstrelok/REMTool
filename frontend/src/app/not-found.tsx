import Link from "next/link";
import { Home, AlertCircle } from "lucide-react";

export default function NotFound() {
  return (
    <div
      style={{
        minHeight: "60vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 24,
      }}
    >
      <div style={{ maxWidth: 520, textAlign: "center" }}>
        <div
          style={{
            display: "inline-flex",
            alignItems: "center",
            justifyContent: "center",
            width: 64,
            height: 64,
            borderRadius: "50%",
            background: "var(--accent-light)",
            marginBottom: 20,
          }}
        >
          <AlertCircle size={32} style={{ color: "var(--primary)" }} />
        </div>

        <h1
          style={{
            fontSize: 28,
            fontWeight: 800,
            margin: "0 0 8px 0",
            color: "var(--text)",
          }}
        >
          404 — Página no encontrada
        </h1>

        <p style={{ color: "var(--text-light)", marginTop: 8, fontSize: 15 }}>
          Lo sentimos, no hemos encontrado la página que buscas.
        </p>

        <div
          style={{
            display: "flex",
            gap: 12,
            justifyContent: "center",
            marginTop: 28,
          }}
        >
          <Link
            href="/"
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 8,
              padding: "10px 20px",
              background: "var(--primary)",
              color: "#fff",
              borderRadius: 8,
              textDecoration: "none",
              fontWeight: 600,
              fontSize: 14,
            }}
          >
            <Home size={16} aria-hidden />
            Ir al inicio
          </Link>
        </div>

        <p
          style={{
            color: "var(--text-light)",
            marginTop: 20,
            fontSize: 12,
          }}
        >
          Si crees que esto es un error, revisa la URL o vuelve al inicio.
        </p>
      </div>
    </div>
  );
}
