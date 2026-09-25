using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using OfficeOpenXml;
using RemTool.Shared;

namespace RemTool.Application.Admin;

public sealed record RemImportFileResult(string FileName, bool Imported, string Message, int Records);
public sealed record RemImportResult(IReadOnlyList<RemImportFileResult> Files);

/// <summary>Imports uploaded REM workbooks without dialogs, console output, or hard-coded folders.</summary>
public sealed class RemImportService(IDbContextFactory<RemToolDataContext> factory)
{
    public async Task<RemImportResult> ImportAsync(IEnumerable<string> paths, string series, int year, CancellationToken ct)
    {
        if (year is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(year), "El año no es válido.");
        var normalizedSeries = series.Trim().ToUpperInvariant();
        var results = new List<RemImportFileResult>();
        foreach (var path in paths)
        {
            ct.ThrowIfCancellationRequested();
            try { results.Add(await ImportOneAsync(path, normalizedSeries, year, ct)); }
            catch (Exception ex) { results.Add(new(Path.GetFileName(path), false, ex.Message, 0)); }
        }
        return new(results);
    }

    private async Task<RemImportFileResult> ImportOneAsync(string path, string series, int year, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        using var workbook = new ExcelPackage(path);
        var header = workbook.Workbook.Worksheets["NOMBRE"] ?? throw new InvalidOperationException("El archivo no contiene la hoja NOMBRE.");
        var version = Text(header, "A9"); var code = string.Concat(new[] { "C3", "D3", "E3", "F3", "G3", "H3" }.Select(x => Text(header, x)));
        var comunaCode = string.Concat(new[] { "C2", "D2", "E2", "F2", "G2" }.Select(x => Text(header, x)));
        if (!int.TryParse(string.Concat(Text(header, "C6"), Text(header, "D6")), out var month) || month is < 1 or > 12)
            throw new InvalidOperationException("No se pudo leer un mes válido desde NOMBRE.");
        var establishment = await db.Establecimiento.AsNoTracking().SingleOrDefaultAsync(x => x.CodDeis == code, ct)
            ?? throw new InvalidOperationException($"No existe el establecimiento {code}.");
        var comuna = await db.Comuna.AsNoTracking().SingleOrDefaultAsync(x => x.CodDeis == comunaCode, ct)
            ?? throw new InvalidOperationException($"No existe la comuna {comunaCode}.");
        var versionId = await db.VersionRem.AsNoTracking().Where(x => x.Nombre == version && x.SerieRem.Nombre == series).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No existe estructura para la versión '{version}' y serie '{series}'.");
        var definitions = await db.Prestacion.AsNoTracking().Where(x => x.id_version == versionId).OrderBy(x => x.Orden)
            .Select(x => new { x.Id, Sheet = x.HojaRem.Nombre, Legacy = x.Coordenada }).ToListAsync(ct);
        var coordinates = await db.CoordenadaPrestacion.AsNoTracking().Where(x => x.Prestacion.id_version == versionId).OrderBy(x => x.Orden)
            .Select(x => new { x.IdPrestacion, x.CeldaBase }).ToListAsync(ct);
        var coordinateMap = coordinates.GroupBy(x => x.IdPrestacion).ToDictionary(x => x.Key, x => x.Select(y => y.CeldaBase).ToArray());
        var records = new List<Registro>();
        foreach (var definition in definitions)
        {
            var sheet = workbook.Workbook.Worksheets[definition.Sheet] ?? throw new InvalidOperationException($"Falta la hoja '{definition.Sheet}'.");
            IEnumerable<string> cells = coordinateMap.TryGetValue(definition.Id, out var normalized) && normalized.Length > 0
                ? normalized
                : definition.Legacy;
            var values = cells.Select(cell => ReadNumber(sheet.Cells[cell].Value, series)).ToList();
            if (values.Any(x => x != 0)) records.Add(new Registro { id_prestacion = definition.Id, Valor = values });
        }
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var previous = await db.Reporte.Where(x => x.id_establecimiento == establishment.Id && x.id_comuna == comuna.Id && x.Año == year && x.Mes == month && x.Registros.Any(r => r.Prestacion.id_version == versionId)).Select(x => x.Id).ToListAsync(ct);
        if (previous.Count > 0) { await db.Registro.Where(x => previous.Contains(x.id_reporte)).ExecuteDeleteAsync(ct); await db.Reporte.Where(x => previous.Contains(x.Id)).ExecuteDeleteAsync(ct); }
        if (records.Count == 0) { await transaction.CommitAsync(ct); return new(Path.GetFileName(path), false, "No se encontraron prestaciones con datos; no se modificó la base de datos.", 0); }
        var report = new Reporte { id_establecimiento = establishment.Id, id_comuna = comuna.Id, Año = year, Mes = month };
        db.Reporte.Add(report); await db.SaveChangesAsync(ct);
        await BulkInsertAsync(db, report.Id, records, ct); await transaction.CommitAsync(ct);
        return new(Path.GetFileName(path), true, $"Importado: {establishment.Nombre}, mes {month:00}, versión {version}.", records.Count);
    }

    private static string Text(ExcelWorksheet sheet, string cell) => sheet.Cells[cell].Value?.ToString()?.Trim() ?? string.Empty;
    private static int ReadNumber(object? value, string series) { if (value is null || string.IsNullOrWhiteSpace(value.ToString())) return 0; if (!decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.CurrentCulture, out var number)) throw new InvalidOperationException($"El valor '{value}' no es numérico."); return series == "D" ? (int)number : Convert.ToInt32(number); }
    private static async Task BulkInsertAsync(RemToolDataContext db, int reportId, IEnumerable<Registro> records, CancellationToken ct)
    {
        if (db.Database.GetDbConnection() is not NpgsqlConnection connection) throw new InvalidOperationException("La conexión configurada no es PostgreSQL.");
        await connection.OpenAsync(ct); await using var importer = await connection.BeginBinaryImportAsync("COPY \"REMTool\".registro (id_prestacion, id_reporte, valor) FROM STDIN (FORMAT BINARY)", ct);
        foreach (var record in records) { await importer.StartRowAsync(ct); await importer.WriteAsync((long)record.id_prestacion, NpgsqlDbType.Bigint, ct); await importer.WriteAsync((long)reportId, NpgsqlDbType.Bigint, ct); await importer.WriteAsync(record.Valor.Select(x => (decimal)x).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Numeric, ct); }
        await importer.CompleteAsync(ct);
    }
}
