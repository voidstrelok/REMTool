using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

// Usuarios del panel interno del sitio (/Interno). Tabla "REMTool".usuario_interno, creada por la migración
// AddUsuarioInterno de la API; sin entidad EF, SQL directo. Hash compatible con InternoController (PasswordHasher).
public static class UsuariosInternosRoutes
{
    public static void MapUsuariosInternos(this RouteGroupBuilder api)
    {
        api.MapGet("/usuarios-internos", async (RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.Database.SqlQuery<UsuarioInterno>($"""
            SELECT id AS "Id", usuario AS "Usuario", nombre AS "Nombre", activo AS "Activo", creado AS "Creado"
            FROM "REMTool".usuario_interno ORDER BY usuario
            """).ToListAsync(ct)));

        api.MapPost("/usuarios-internos", async (UsuarioInternoRequest r, RemToolDataContext db, CancellationToken ct) =>
        {
            var usuario = r.Usuario?.Trim().ToLowerInvariant() ?? "";
            if (usuario.Length < 3) throw new ArgumentException("El usuario debe tener al menos 3 caracteres.");
            if (string.IsNullOrWhiteSpace(r.Nombre)) throw new ArgumentException("El nombre es obligatorio.");
            var hash = Hash(usuario, r.Password ?? throw new ArgumentException("La contraseña es obligatoria."));
            var existe = await db.Database.SqlQuery<int>($"""SELECT 1 AS "Value" FROM "REMTool".usuario_interno WHERE lower(usuario) = {usuario}""").AnyAsync(ct);
            if (existe) throw new ArgumentException($"El usuario '{usuario}' ya existe.");
            await db.Database.ExecuteSqlAsync($"""INSERT INTO "REMTool".usuario_interno (usuario, nombre, password_hash) VALUES ({usuario}, {r.Nombre.Trim()}, {hash})""", ct);
            return Results.Ok(new { usuario });
        });

        // Actualiza nombre/activo; la contraseña sólo si viene.
        api.MapPut("/usuarios-internos/{id:int}", async (int id, UsuarioInternoRequest r, RemToolDataContext db, CancellationToken ct) =>
        {
            var usuario = await db.Database.SqlQuery<string>($"""SELECT usuario AS "Value" FROM "REMTool".usuario_interno WHERE id = {id}""").FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");
            if (string.IsNullOrWhiteSpace(r.Nombre)) throw new ArgumentException("El nombre es obligatorio.");
            await db.Database.ExecuteSqlAsync($"""UPDATE "REMTool".usuario_interno SET nombre = {r.Nombre.Trim()}, activo = {r.Activo ?? true} WHERE id = {id}""", ct);
            if (!string.IsNullOrEmpty(r.Password))
                await db.Database.ExecuteSqlAsync($"""UPDATE "REMTool".usuario_interno SET password_hash = {Hash(usuario, r.Password)} WHERE id = {id}""", ct);
            return Results.Ok(new { id });
        });

        api.MapDelete("/usuarios-internos/{id:int}", async (int id, RemToolDataContext db, CancellationToken ct) =>
        {
            await db.Database.ExecuteSqlAsync($"""DELETE FROM "REMTool".usuario_interno WHERE id = {id}""", ct);
            return Results.NoContent();
        });
    }

    static string Hash(string usuario, string password) => password.Length >= 8
        ? new PasswordHasher<string>().HashPassword(usuario, password)
        : throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
}

public sealed record UsuarioInterno(int Id, string Usuario, string Nombre, bool Activo, DateTime Creado);
public sealed record UsuarioInternoRequest(string? Usuario, string? Nombre, string? Password, bool? Activo);
