"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, Run } from "../lib/api";

type Usuario = { id: number; usuario: string; nombre: string; activo: boolean; creado: string };
const json = (method: string, body: unknown): RequestInit => ({ method, headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });

export default function UsuariosInternosPanel({ busy, run }: { busy: boolean; run: Run }) {
  const [items, setItems] = useState<Usuario[]>([]);
  const [draft, setDraft] = useState({ usuario: "", nombre: "", password: "" });
  const load = () => api<Usuario[]>("/usuarios-internos").then(setItems).catch(() => setItems([]));
  useEffect(() => { void load(); }, []);

  const create = (e: FormEvent) => {
    e.preventDefault();
    void run(async () => { await api("/usuarios-internos", json("POST", draft)); setDraft({ usuario: "", nombre: "", password: "" }); await load(); }, `Usuario ${draft.usuario} creado.`);
  };
  const update = (u: Usuario, changes: Partial<{ activo: boolean; password: string }>, msg: string) =>
    void run(async () => { await api(`/usuarios-internos/${u.id}`, json("PUT", { nombre: u.nombre, activo: u.activo, ...changes })); await load(); }, msg);
  const resetPassword = (u: Usuario) => { const password = prompt(`Nueva contraseña para ${u.usuario} (mínimo 8 caracteres):`); if (password) update(u, { password }, `Contraseña de ${u.usuario} actualizada.`); };
  const remove = (u: Usuario) => { if (confirm(`¿Eliminar el usuario ${u.usuario}?`)) void run(async () => { await api(`/usuarios-internos/${u.id}`, { method: "DELETE" }); await load(); }, `Usuario ${u.usuario} eliminado.`); };

  return <section className="query">
    <h2>Usuarios del panel interno</h2>
    <p>Acceso a las herramientas internas del sitio (<code>/Interno</code>: comprobador de RUT, etc.). Desactivar un usuario corta su acceso de inmediato.</p>
    <form className="filters" onSubmit={create}>
      <label>Usuario<input required minLength={3} autoComplete="off" value={draft.usuario} onChange={e => setDraft({ ...draft, usuario: e.target.value })} /></label>
      <label>Nombre<input required value={draft.nombre} onChange={e => setDraft({ ...draft, nombre: e.target.value })} /></label>
      <label>Contraseña<input type="password" required minLength={8} autoComplete="new-password" value={draft.password} onChange={e => setDraft({ ...draft, password: e.target.value })} /></label>
      <button disabled={busy}>Crear usuario</button>
    </form>
    <div className="reportTable">{items.map(u => <article key={u.id}>
      <div><strong>{u.usuario}</strong><small>{u.nombre} · {u.activo ? "activo" : "desactivado"} · creado {new Date(u.creado).toLocaleDateString("es-CL")}</small></div>
      <div className="actions">
        <button type="button" className="ghost" disabled={busy} onClick={() => update(u, { activo: !u.activo }, `Usuario ${u.usuario} ${u.activo ? "desactivado" : "activado"}.`)}>{u.activo ? "Desactivar" : "Activar"}</button>
        <button type="button" className="ghost" disabled={busy} onClick={() => resetPassword(u)}>Cambiar contraseña</button>
        <button type="button" className="ghost" disabled={busy} onClick={() => remove(u)}>Eliminar</button>
      </div>
    </article>)}</div>
  </section>;
}
