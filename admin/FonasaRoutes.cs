using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using RemTool.Shared;

// Tabla "REMTool".fonasa (inscritos per cápita nominales). No tiene entidad EF a propósito:
// existe fuera de las migraciones, así que se accede con SQL directo.
public static class FonasaRoutes
{
    static readonly string[] Required = ["run", "dv", "nombres", "apellido_paterno", "apellido_materno", "fecha_nacimiento", "genero", "tramo", "cod_deis"];
    static readonly string[] DateFormats = ["yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "d/M/yyyy", "d-M-yyyy", "yyyy/MM/dd"];

    public static void MapFonasa(this RouteGroupBuilder api)
    {
        api.MapGet("/fonasa", async (RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.Database.SqlQueryRaw<FonasaYear>("""
            SELECT extract(year FROM fecha_corte)::int AS "Year", max(fecha_corte) AS "FechaCorte", count(*)::int AS "Personas", count(DISTINCT id_establecimiento)::int AS "Establecimientos"
            FROM "REMTool".fonasa GROUP BY 1 ORDER BY 1 DESC
            """).ToListAsync(ct)));

        api.MapGet("/fonasa/{year:int}", async (int year, RemToolDataContext db, CancellationToken ct) => Results.Ok(await db.Database.SqlQueryRaw<FonasaEst>("""
            SELECT e.nombre AS "Nombre", e.cod_deis AS "CodDeis", count(*) FILTER (WHERE f.genero = 'F')::int AS "F", count(*) FILTER (WHERE f.genero = 'M')::int AS "M",
                   string_agg(DISTINCT f.tramo, ', ') AS "Tramos"
            FROM "REMTool".fonasa f JOIN "REMTool".establecimiento e ON e.id = f.id_establecimiento
            WHERE extract(year FROM f.fecha_corte) = {0} GROUP BY e.nombre, e.cod_deis ORDER BY e.nombre
            """, year).ToListAsync(ct)));

        // Reemplaza completo el corte del año: borra y reinserta (COPY binario) en una transacción.
        api.MapPut("/fonasa/{year:int}", async (int year, HttpRequest request, RemToolDataContext db, CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? throw new ArgumentException("Debe adjuntar el archivo CSV.");
            if (!DateOnly.TryParseExact(form["fechaCorte"], "yyyy-MM-dd", out var corte)) throw new ArgumentException("La fecha de corte es obligatoria.");
            if (corte.Year != year) throw new ArgumentException($"La fecha de corte debe pertenecer al año {year}.");
            var ests = (await db.Establecimiento.AsNoTracking().Where(x => x.CodDeis != null).Select(x => new { x.Id, x.CodDeis }).ToListAsync(ct))
                .GroupBy(x => x.CodDeis!.Trim()).ToDictionary(g => g.Key, g => g.First().Id);
            await using var stream = file.OpenReadStream();
            var rows = Parse(await ReadText(stream, ct), corte, ests);

            await db.Database.OpenConnectionAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("""DELETE FROM "REMTool".fonasa WHERE extract(year FROM fecha_corte) = {0}""", [year], ct);
            var conn = (NpgsqlConnection)db.Database.GetDbConnection();
            await using (var copy = await conn.BeginBinaryImportAsync("""COPY "REMTool".fonasa (run, dv, nombres, apellido_paterno, apellido_materno, edad, fecha_nacimiento, genero, tramo, fecha_corte, id_establecimiento) FROM STDIN (FORMAT BINARY)""", ct))
            {
                foreach (var r in rows)
                {
                    await copy.StartRowAsync(ct);
                    await copy.WriteAsync(r.Run, NpgsqlDbType.Varchar, ct); await copy.WriteAsync(r.Dv, NpgsqlDbType.Varchar, ct);
                    await copy.WriteAsync(r.Nombres, NpgsqlDbType.Varchar, ct); await copy.WriteAsync(r.Paterno, NpgsqlDbType.Varchar, ct); await copy.WriteAsync(r.Materno, NpgsqlDbType.Varchar, ct);
                    await copy.WriteAsync(r.Edad, NpgsqlDbType.Integer, ct); await copy.WriteAsync(r.Nacimiento, NpgsqlDbType.Date, ct);
                    await copy.WriteAsync(r.Genero, NpgsqlDbType.Varchar, ct); await copy.WriteAsync(r.Tramo, NpgsqlDbType.Varchar, ct);
                    await copy.WriteAsync(corte, NpgsqlDbType.Date, ct); await copy.WriteAsync(r.Establecimiento, NpgsqlDbType.Bigint, ct);
                }
                await copy.CompleteAsync(ct);
            }
            await tx.CommitAsync(ct);
            return Results.Ok(new { rows = rows.Count });
        });

        api.MapDelete("/fonasa/{year:int}", async (int year, RemToolDataContext db, CancellationToken ct) =>
        {
            await db.Database.ExecuteSqlRawAsync("""DELETE FROM "REMTool".fonasa WHERE extract(year FROM fecha_corte) = {0}""", [year], ct);
            return Results.NoContent();
        });
    }

    // Excel guarda CSV en Windows-1252/Latin1 salvo que se elija UTF-8: se intenta UTF-8 estricto y si falla, Latin1.
    static async Task<string> ReadText(Stream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream(); await stream.CopyToAsync(ms, ct); var bytes = ms.ToArray();
        try { return new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿'); }
        catch (DecoderFallbackException) { return Encoding.Latin1.GetString(bytes); }
    }

    public static List<FonasaRow> Parse(string text, DateOnly corte, IReadOnlyDictionary<string, long> establishments)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count < 2) throw new ArgumentException("El archivo no contiene filas.");
        var sep = lines[0].Contains(';') ? ';' : ',';
        string[] Split(string l) => l.Split(sep).Select(v => v.Trim().Trim('"').Trim()).ToArray();
        var header = Split(lines[0]).Select(h => h.ToLowerInvariant().Replace(' ', '_')).ToList();
        var missing = Required.Where(c => !header.Contains(c)).ToList();
        if (missing.Count > 0) throw new ArgumentException($"Faltan columnas en el encabezado: {string.Join(", ", missing)}.");
        int Col(string name) => header.IndexOf(name);
        var edadCol = Col("edad");

        var rows = new List<FonasaRow>(lines.Count); var seen = new HashSet<string>();
        for (var i = 1; i < lines.Count; i++)
        {
            var v = Split(lines[i]); var line = i + 1;
            string Get(string name) { var c = Col(name); return c < v.Length ? v[c] : ""; }
            var run = new string(Get("run").Where(char.IsDigit).ToArray());
            if (run.Length == 0) throw new ArgumentException($"Fila {line}: RUN vacío.");
            if (!seen.Add(run)) throw new ArgumentException($"Fila {line}: RUN {run} repetido.");
            if (!establishments.TryGetValue(Get("cod_deis"), out var est)) throw new ArgumentException($"Fila {line}: código DEIS '{Get("cod_deis")}' no existe.");
            if (!DateOnly.TryParseExact(Get("fecha_nacimiento"), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var nac) || nac > corte)
                throw new ArgumentException($"Fila {line}: fecha de nacimiento inválida '{Get("fecha_nacimiento")}'.");
            var genero = Get("genero").ToUpperInvariant() switch { var g when g.StartsWith('F') => "F", var g when g.StartsWith('M') => "M", _ => throw new ArgumentException($"Fila {line}: género debe ser F o M.") };
            var edad = corte.Year - nac.Year - (corte < nac.AddYears(corte.Year - nac.Year) ? 1 : 0);
            if (edadCol >= 0 && edadCol < v.Length && v[edadCol] != "")
                edad = int.TryParse(v[edadCol], out var e) && e is >= 0 and <= 130 ? e : throw new ArgumentException($"Fila {line}: edad inválida.");
            rows.Add(new FonasaRow(run, Get("dv").ToUpperInvariant(), Get("nombres"), Get("apellido_paterno"), Get("apellido_materno"), edad, nac, genero, Get("tramo").ToUpperInvariant(), est));
        }
        return rows;
    }
}

public sealed record FonasaRow(string Run, string Dv, string Nombres, string Paterno, string Materno, int Edad, DateOnly Nacimiento, string Genero, string Tramo, long Establecimiento);
public sealed record FonasaYear(int Year, DateOnly FechaCorte, int Personas, int Establecimientos);
public sealed record FonasaEst(string Nombre, string? CodDeis, int F, int M, string? Tramos);
