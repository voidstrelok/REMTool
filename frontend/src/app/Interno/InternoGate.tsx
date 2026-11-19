"use client";

import { FormEvent, ReactNode, useEffect, useState } from "react";
import { Lock, LogOut } from "lucide-react";
import { apiUrl } from "@/lib/api";
import { Button } from "@/components/ui/button";

const TOKEN_KEY = "interno_token";
const LOGOUT_EVENT = "interno-logout";

function readToken() { try { return localStorage.getItem(TOKEN_KEY); } catch { return null; } }
function logout() { try { localStorage.removeItem(TOKEN_KEY); } catch { } window.dispatchEvent(new Event(LOGOUT_EVENT)); }

/** fetch a la API con el token del panel interno. 401/403 cierra la sesión y vuelve al login. */
export async function internoFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(apiUrl(path), { ...init, headers: { ...init?.headers, Authorization: `Bearer ${readToken()}` } });
  if (response.status === 401 || response.status === 403) { logout(); throw new Error("Sesión expirada. Vuelva a ingresar."); }
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.error ?? (response.status === 429 ? "Demasiados intentos, espere un minuto." : "No se pudo completar la operación."));
  return body as T;
}

export const inputStyle = { background: "var(--surface)", borderColor: "var(--border)", color: "var(--text)" };

/** Muestra el login si no hay sesión; si la hay, el contenido con la barra de usuario. */
export default function InternoGate({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<{ nombre: string } | null | undefined>(undefined);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const check = () => readToken()
    ? internoFetch<{ nombre: string }>("interno/me").then(setUser).catch(() => setUser(null))
    : setUser(null);

  useEffect(() => {
    void check();
    const onLogout = () => setUser(null);
    window.addEventListener(LOGOUT_EVENT, onLogout);
    return () => window.removeEventListener(LOGOUT_EVENT, onLogout);
  }, []);

  const login = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault(); setBusy(true); setError("");
    const data = new FormData(e.currentTarget);
    try {
      const r = await fetch(apiUrl("interno/login"), { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ usuario: data.get("usuario"), password: data.get("password") }) });
      if (r.status === 429) throw new Error("Demasiados intentos, espere un minuto.");
      if (!r.ok) throw new Error("Usuario o contraseña incorrectos.");
      localStorage.setItem(TOKEN_KEY, (await r.json()).accessToken);
      await check();
    } catch (err) { setError(err instanceof Error ? err.message : String(err)); }
    finally { setBusy(false); }
  };

  if (user === undefined) return null;
  if (user === null) return (
    <form onSubmit={login} className="mx-auto mt-10 flex max-w-sm flex-col gap-4 rounded-xl border p-6" style={{ background: "var(--surface)", borderColor: "var(--border)" }}>
      <div className="flex items-center gap-2"><Lock size={18} style={{ color: "var(--primary)" }} aria-hidden="true" /><h1 className="text-xl font-bold">Panel interno</h1></div>
      <p className="text-sm" style={{ color: "var(--text-light)" }}>Acceso exclusivo del departamento. Solicite su usuario al administrador.</p>
      <label className="flex flex-col gap-1 text-sm font-semibold">Usuario<input name="usuario" required autoComplete="username" className="rounded-md border px-3 py-2 font-normal" style={inputStyle} /></label>
      <label className="flex flex-col gap-1 text-sm font-semibold">Contraseña<input name="password" type="password" required autoComplete="current-password" className="rounded-md border px-3 py-2 font-normal" style={inputStyle} /></label>
      {error && <p role="alert" className="text-sm" style={{ color: "var(--error-dark)" }}>{error}</p>}
      <Button type="submit" disabled={busy}>{busy ? "Ingresando..." : "Ingresar"}</Button>
    </form>
  );
  return <>
    <div className="mb-2 flex items-center justify-end gap-3 text-sm" style={{ color: "var(--text-light)" }}>
      <span>{user.nombre}</span>
      <button type="button" onClick={logout} className="inline-flex items-center gap-1 font-semibold" style={{ color: "var(--primary)" }}><LogOut size={14} aria-hidden="true" />Salir</button>
    </div>
    {children}
  </>;
}
