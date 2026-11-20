using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RemTool.Shared;
using RemTool.Shared.Ingest;

namespace RemTool.Api.Controllers;

/// <summary>
/// Recibe el batch que publica el admin local: configuración siempre, y opcionalmente estructura REM y datos de un año
/// (registros, per cápita, fonasa). Todo en una transacción: si algo falla, prod no cambia.
/// </summary>
[ApiController]
[Route("api/ingest")]
[AllowAnonymous]
public sealed class IngestController(RemToolDataContext db, IConfiguration configuration) : ControllerBase
{
    [HttpPost("batch")]
    [RequestSizeLimit(300_000_000)]
    public async Task<IActionResult> Batch(CancellationToken ct)
    {
        if (!IsAuthorized()) return Unauthorized(new { error = "Clave de ingesta inválida." });

        using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
        var root = doc.RootElement;
        var tables = root.GetProperty("tables");
        var names = tables.EnumerateObject().Select(p => p.Name).ToHashSet();

        var unknown = names.Except(IngestTables.FullTables).Except(IngestTables.Slices.Select(s => s.Table)).ToList();
        if (unknown.Count > 0) return BadRequest(new { error = $"Tablas desconocidas: {string.Join(", ", unknown)}." });
        // Grupos completos o nada: una tabla suelta de estructura dejaría prod inconsistente.
        if (!IngestTables.Config.All(names.Contains)) return BadRequest(new { error = "El batch debe incluir toda la configuración." });
        if (IngestTables.Structure.Any(names.Contains) && !IngestTables.Structure.All(names.Contains)) return BadRequest(new { error = "La estructura REM debe ir completa." });
        if (names.Contains(IngestTables.Reporte.Table) != names.Contains(IngestTables.Registro.Table)) return BadRequest(new { error = "reporte y registro deben ir juntos." });

        var full = IngestTables.FullTables.Where(names.Contains).ToList();
        var slices = IngestTables.Slices.Where(s => names.Contains(s.Table)).ToList();
        int year = 0;
        if (slices.Count > 0 && !(root.TryGetProperty("year", out var y) && y.TryGetInt32(out year) && year is >= 2000 and <= 2100))
            return BadRequest(new { error = "Los datos por año requieren un año válido." });

        db.Database.SetCommandTimeout(300);
        try { return await ApplyAsync(year, tables, full, slices, ct); }
        // La transacción se revierte al salir; el mensaje de Postgres dice qué constraint bloquea.
        catch (PostgresException e) { return Conflict(new { error = $"Batch revertido, prod no cambió. {e.MessageText}. {e.Detail}" }); }
    }

    private async Task<IActionResult> ApplyAsync(int year, JsonElement tables, List<string> full, List<YearSlice> slices, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        string Json(string table) => tables.GetProperty(table).GetRawText();

        // 1) Datos del año: borrar (hijos primero) para no dejar huérfanos al reemplazar estructura.
        foreach (var slice in Enumerable.Reverse(slices))
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM {Target(slice.Table)} x WHERE {slice.Filter}", [year], ct);

        // 2) Tablas completas: borrar lo que ya no existe en local (hijos primero), antes del upsert para que algo
        //    recreado (p. ej. un filtro con el mismo establecimiento) no choque con índices únicos.
        foreach (var table in Enumerable.Reverse(full))
            await DeleteMissingAsync(table, Json(table), ct);

        // 3) Tablas completas: upsert padre → hijo, conservando los IDs locales.
        // ponytail: índices únicos no diferibles; intercambiar el "orden" de dos filas existentes puede fallar (rollback). Resolver si aparece.
        foreach (var table in full)
            await UpsertAsync(table, Json(table), ct);

        // 4) Datos del año: insertar con los IDs locales (registro referencia reporte.id). Sólo filas de ese año.
        foreach (var slice in slices)
            await InsertSliceAsync(slice, year, Json(slice.Table), ct);

        await transaction.CommitAsync(ct);
        var counts = full.Concat(slices.Select(s => s.Table)).ToDictionary(t => t, t => tables.GetProperty(t).GetArrayLength());
        return Ok(new { year = slices.Count > 0 ? year : (int?)null, tables = counts });
    }

    private bool IsAuthorized()
    {
        var expected = configuration["Ingest:ApiKey"];
        if (string.IsNullOrEmpty(expected) || !Request.Headers.TryGetValue("X-Ingest-Key", out var provided)) return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided.ToString()), Encoding.UTF8.GetBytes(expected));
    }

    // Columnas leídas de information_schema (no del JSON) para que nada del body entre al SQL como identificador.
    private async Task<List<string>> ColumnsAsync(string table, CancellationToken ct) =>
        (await db.Database.SqlQueryRaw<string>(
            "SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_schema = 'REMTool' AND table_name = {0} ORDER BY ordinal_position",
            table).ToListAsync(ct)).Select(Quote).ToList();

    private async Task UpsertAsync(string table, string json, CancellationToken ct)
    {
        var target = Target(table);
        var cols = await ColumnsAsync(table, ct);
        var list = string.Join(", ", cols);
        var set = string.Join(", ", cols.Where(c => c != "\"id\"").Select(c => $"{c} = EXCLUDED.{c}"));
        var sql = $"INSERT INTO {target} ({list}) SELECT {list} FROM json_populate_recordset(NULL::{target}, {{0}}::json) " +
                  $"ON CONFLICT (\"id\") DO UPDATE SET {set}";
        await db.Database.ExecuteSqlRawAsync(sql, [new NpgsqlParameter(null, json)], ct);
    }

    private Task DeleteMissingAsync(string table, string json, CancellationToken ct)
    {
        var target = Target(table);
        var sql = $"DELETE FROM {target} WHERE \"id\" NOT IN (SELECT r.\"id\" FROM json_populate_recordset(NULL::{target}, {{0}}::json) r)";
        return db.Database.ExecuteSqlRawAsync(sql, [new NpgsqlParameter(null, json)], ct);
    }

    private async Task InsertSliceAsync(YearSlice slice, int year, string json, CancellationToken ct)
    {
        var target = Target(slice.Table);
        var list = string.Join(", ", await ColumnsAsync(slice.Table, ct));
        // Filter usa {0} = año; el JSON va como {1}.
        var sql = $"INSERT INTO {target} ({list}) SELECT {list} FROM json_populate_recordset(NULL::{target}, {{1}}::json) x WHERE {slice.Filter}";
        await db.Database.ExecuteSqlRawAsync(sql, [year, new NpgsqlParameter(null, json)], ct);
    }

    private static string Target(string table) => $"\"REMTool\".{Quote(table)}";
    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
