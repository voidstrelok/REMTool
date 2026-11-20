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
/// Recibe el batch que publica el admin local: estructura completa + datos de un año (resultados, y opcionalmente per cápita / fonasa).
/// Todo corre en una transacción; si algo falla (p. ej. FK de registros a una prestación borrada) no queda nada a medias.
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
        var year = root.GetProperty("year").GetInt32();
        if (year is < 2000 or > 2100) return BadRequest(new { error = "El año no es válido." });
        var tables = root.GetProperty("tables");
        foreach (var name in IngestTables.Structure.Append(IngestTables.Results.Table))
            if (!tables.TryGetProperty(name, out _)) return BadRequest(new { error = $"Falta la tabla {name} en el batch." });
        // Resultados siempre; per cápita y fonasa sólo si el admin los incluyó.
        var slices = IngestTables.Slices.Where(s => tables.TryGetProperty(s.Table, out _)).ToList();

        db.Database.SetCommandTimeout(300);
        try { return await ApplyAsync(year, tables, slices, ct); }
        // La transacción se revierte al salir; el detalle de Postgres dice qué fila bloquea (p. ej. "Key (id)=(5) is still referenced from table registro").
        catch (PostgresException e) { return Conflict(new { error = $"Batch revertido, prod no cambió. {e.MessageText}. {e.Detail}" }); }
    }

    private async Task<IActionResult> ApplyAsync(int year, JsonElement tables, List<YearSlice> slices, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // 1) Datos del año: se borran primero para no dejar huérfanos al reemplazar la estructura.
        foreach (var slice in slices)
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM {Target(slice.Table)} x WHERE {slice.Filter}", [year], ct);

        // 2) Estructura: borrar filas que ya no existen en local (hijos primero), antes del upsert para que un
        //    registro recreado (p. ej. un filtro con el mismo establecimiento) no choque con índices únicos.
        //    Si prod tiene registros que usan una prestación borrada, falla y se revierte todo.
        foreach (var table in IngestTables.Structure.Reverse())
            await DeleteMissingAsync(table, tables.GetProperty(table).GetRawText(), ct);

        // 3) Estructura: upsert en orden padre → hijo, conservando los IDs locales.
        // ponytail: índices únicos no diferibles; intercambiar el "orden" de dos filas existentes puede fallar (rollback). Resolver si aparece.
        foreach (var table in IngestTables.Structure)
            await UpsertAsync(table, tables.GetProperty(table).GetRawText(), ct);

        // 4) Datos del año: se insertan sin id (la secuencia de prod asigna los suyos) y sólo las filas de ese año.
        foreach (var slice in slices)
            await InsertSliceAsync(slice, year, tables.GetProperty(slice.Table).GetRawText(), ct);

        await transaction.CommitAsync(ct);

        var counts = IngestTables.Structure.Concat(slices.Select(s => s.Table))
            .ToDictionary(name => name, name => tables.GetProperty(name).GetArrayLength());
        return Ok(new { year, tables = counts });
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
        var list = string.Join(", ", (await ColumnsAsync(slice.Table, ct)).Where(c => c != "\"id\""));
        // Filter usa {0} = año; el JSON va como {1}.
        var sql = $"INSERT INTO {target} ({list}) SELECT {list} FROM json_populate_recordset(NULL::{target}, {{1}}::json) x WHERE {slice.Filter}";
        await db.Database.ExecuteSqlRawAsync(sql, [year, new NpgsqlParameter(null, json)], ct);
    }

    private static string Target(string table) => $"\"REMTool\".{Quote(table)}";
    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
