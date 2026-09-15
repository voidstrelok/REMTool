using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RemTool.Shared;

namespace RemTools.Consolidados;

public sealed class ConsolidadoDataReader(Func<RemToolDataContext> createContext)
{
    public async Task<List<VersionOption>> GetVersionsAsync(CancellationToken ct)
    {
        await using var db = createContext();
        return await db.VersionRem.AsNoTracking().Where(v => v.SerieRem.Nombre == "A")
            .OrderByDescending(v => v.Fecha)
            .Select(v => new VersionOption(v.Id, v.Nombre, v.Fecha)).ToListAsync(ct);
    }

    public async Task<ConsolidadoSnapshot> ReadAsync(GenerationRequest request, IProgress<string> progress, CancellationToken ct)
    {
        if (request.Year is < 2000 or > 2100) throw new InvalidOperationException("El año no es válido.");
        await using var db = createContext();
        db.Database.SetCommandTimeout(180);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        // Consistent snapshot, including metadata. This workflow never writes business data.
        await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", ct);
        var capturedAt = DateTimeOffset.UtcNow;
        var version = await db.VersionRem.AsNoTracking()
            .Where(v => v.Id == request.VersionId && v.SerieRem.Nombre == "A")
            .Select(v => new VersionOption(v.Id, v.Nombre, v.Fecha)).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Seleccione una versión de Serie A para la plantilla.");
        progress.Report("Leyendo reportes vigentes de Serie A...");
        var candidates = await db.Reporte.AsNoTracking()
            .Where(r => r.Año == request.Year && r.Registros.Any(g => g.Prestacion.VersionRem.SerieRem.Nombre == "A"))
            .Select(r => new ReportInfo(r.Id, r.id_establecimiento, r.id_comuna, r.Mes)).ToListAsync(ct);
        var reports = SelectLatestReports(candidates);
        if (reports.Count == 0) throw new InvalidOperationException("No hay registros de Serie A para ese año. No se reemplazó ninguna publicación.");
        if (reports.Any(r => r.Month is < 1 or > 12)) throw new InvalidOperationException("Hay reportes con un mes inválido.");
        var ids = reports.Select(r => r.Id).ToArray();
        var establishments = await db.Establecimiento.AsNoTracking().OrderBy(e => e.Nombre)
            .Select(e => new EstablishmentInfo(e.Id, e.Nombre, e.CodDeis ?? "", e.Sector.Nombre)).ToListAsync(ct);
        var versionIds = await db.Registro.AsNoTracking()
            .Where(g => ids.Contains(g.id_reporte) && g.Prestacion.VersionRem.SerieRem.Nombre == "A")
            .Select(g => g.Prestacion.id_version).Distinct().ToListAsync(ct);
        versionIds.Add(version.Id);
        var definitions = await db.Prestacion.AsNoTracking().Where(p => versionIds.Contains(p.id_version))
            .Select(p => new PrestacionDefinition(p.Id, p.id_version, p.CodigoPrestacion, p.Nombre,
                p.HojaRem.Nombre, p.IdSeccion, p.Coordenada)).ToListAsync(ct);
        var columns = await db.CoordenadaPrestacion.AsNoTracking()
            .Where(c => versionIds.Contains(c.Prestacion.id_version)).OrderBy(c => c.IdPrestacion).ThenBy(c => c.Orden)
            .Select(c => new { c.IdPrestacion, c.CodigoColumna, c.CeldaBase, c.Celda.EsTotal }).ToListAsync(ct);
        var byPrestacion = columns.ToLookup(c => c.IdPrestacion);
        // Cross-version mapping is allowed only for an identical section structure,
        // including labels and formulas. A matching ordinal alone is insufficient.
        var cells = await db.CeldaSeccionRem.AsNoTracking()
            .Where(c => c.Fila.Seccion.VersionHoja != null && versionIds.Contains(c.Fila.Seccion.VersionHoja.IdVersion))
            .OrderBy(c => c.Fila.IdSeccion).ThenBy(c => c.FilaOrigen).ThenBy(c => c.ColumnaOrigen)
            .Select(c => new { c.Fila.IdSeccion, c.FilaOrigen, c.ColumnaOrigen, c.Valor, c.CeldaBase,
                c.RowSpan, c.ColSpan, c.EsEntradaPrestacion, c.EsTotal, c.FormulaOrigen }).ToListAsync(ct);
        var signatures = cells.GroupBy(c => c.IdSeccion).ToDictionary(g => g.Key,
            g => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                g.Select(c => new { c.FilaOrigen, c.ColumnaOrigen, c.Valor, c.CeldaBase,
                    c.RowSpan, c.ColSpan, c.EsEntradaPrestacion, c.EsTotal, c.FormulaOrigen }))))));
        foreach (var p in definitions)
        {
            p.Columns = byPrestacion[p.Id].Select(c => new ColumnDefinition(c.CodigoColumna, c.CeldaBase, c.EsTotal)).ToList();
            // Some migrated versions contain a partial normalized coordinate set
            // while the legacy JSON still has the complete row. Keep the
            // complete mapping until the normalized set is known to cover it.
            if (p.Columns.Count < p.LegacyCoordinates.Count)
                p.Columns = p.LegacyCoordinates.Select((address, i) => new ColumnDefinition($"COL{i + 1:00}", address)).ToList();
            p.StructureSignature = p.SectionId.HasValue ? signatures.GetValueOrDefault(p.SectionId.Value, "") : "";
        }
        progress.Report($"Leyendo valores de {reports.Count:N0} reportes; meses {string.Join(", ", reports.Select(r => r.Month).Distinct().Order())}...");
        // Use typed PostgreSQL numeric[] directly so values are never round-tripped via text
        // and the reader does not truncate decimals through the legacy List<int> entity.
        var records = new List<SourceRecord>();
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandTimeout = 180;
        command.CommandText = """
            SELECT g.id_reporte, g.id_prestacion, g.valor
            FROM "REMTool".registro g
            JOIN "REMTool".prestacion p ON p.id = g.id_prestacion
            JOIN "REMTool".version_rem v ON v.id = p.id_version
            JOIN "REMTool".serie_rem s ON s.id = v.id_serie
            WHERE g.id_reporte = ANY(@ids) AND s.nombre = 'A'
            ORDER BY g.id_reporte, g.id_prestacion
            """;
        var parameter = command.CreateParameter(); parameter.ParameterName = "ids"; parameter.Value = ids;
        command.Parameters.Add(parameter);
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                if (reader.IsDBNull(2)) throw new InvalidOperationException("Hay registros con valores nulos.");
                records.Add(new SourceRecord(Convert.ToInt32(reader.GetValue(0)), Convert.ToInt32(reader.GetValue(1)), reader.GetFieldValue<decimal[]>(2)));
            }
        }
        await transaction.CommitAsync(ct);
        progress.Report($"Lectura finalizada: {records.Count:N0} filas. La generación continúa localmente.");
        return new ConsolidadoSnapshot(request.Year, version, capturedAt, establishments, reports, definitions, records);
    }

    public static List<ReportInfo> SelectLatestReports(IEnumerable<ReportInfo> candidates) => candidates
        .GroupBy(r => (r.EstablishmentId, r.ComunaId, r.Month)).Select(g => g.MaxBy(r => r.Id)!)
        .OrderBy(r => r.Month).ThenBy(r => r.EstablishmentId).ToList();
}
