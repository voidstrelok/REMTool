using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RemTool.Shared;

namespace RemTool.Application.Admin;

public sealed record IntegrityFileResult(string FileName, IReadOnlyList<string> Errors);
public sealed class RemIntegrityService(IDbContextFactory<RemToolDataContext> factory)
{
    private static readonly Regex NamePattern = new(@"^(?<code>\d{6})(?<series>[A-Z]{1,2})(?<month>\d{2})\.xlsm$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly IReadOnlyDictionary<string, string> ControlCells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["A"] = "D32", ["BM"] = "D7", ["D"] = "E13", ["P"] = "D17" };
    public async Task<IReadOnlyList<IntegrityFileResult>> CheckAsync(IEnumerable<string> paths, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var latest = await db.VersionRem.AsNoTracking().Include(x => x.SerieRem).GroupBy(x => x.SerieRem.Nombre).Select(x => new { Series = x.Key, Version = x.OrderByDescending(y => y.Fecha).Select(y => y.Nombre).First() }).ToDictionaryAsync(x => x.Series, x => x.Version, StringComparer.OrdinalIgnoreCase, ct);
        return paths.Select(path => CheckOne(path, latest)).ToList();
    }
    private static IntegrityFileResult CheckOne(string path, IReadOnlyDictionary<string, string> latest)
    {
        var errors = new List<string>(); var fileName = Path.GetFileName(path); var match = NamePattern.Match(fileName);
        if (!match.Success) return new(fileName, ["El nombre debe contener código DEIS, serie y mes (ejemplo: 105307A01.xlsm)."]);
        try
        {
            using var workbook = new ExcelPackage(path); var header = workbook.Workbook.Worksheets["NOMBRE"];
            if (header is null) return new(fileName, ["No se encontró la hoja NOMBRE."]);
            var series = ExtractSeries(Text(header, "B17")); var code = string.Concat(new[] { "C3", "D3", "E3", "F3", "G3", "H3" }.Select(x => Text(header, x))); var month = Text(header, "C6") + Text(header, "D6"); var version = Text(header, "A9");
            if (!match.Groups["series"].Value.Equals(series, StringComparison.OrdinalIgnoreCase)) errors.Add($"Serie incorrecta: nombre={match.Groups["series"].Value}, hoja={series}.");
            if (!match.Groups["code"].Value.Equals(code, StringComparison.OrdinalIgnoreCase)) errors.Add($"Código DEIS no coincide: nombre={match.Groups["code"].Value}, hoja={code}.");
            if (!match.Groups["month"].Value.Equals(month, StringComparison.OrdinalIgnoreCase)) errors.Add($"Mes no coincide: nombre={match.Groups["month"].Value}, hoja={month}.");
            if (!latest.TryGetValue(series, out var latestVersion)) errors.Add($"La serie {series} no existe en la base de datos."); else if (!version.Equals(latestVersion, StringComparison.OrdinalIgnoreCase)) errors.Add($"Versión desactualizada: archivo={version}, vigente={latestVersion}.");
            if (!ControlCells.TryGetValue(series, out var cell)) errors.Add($"No hay celda de control definida para la serie {series}."); else { var control = workbook.Workbook.Worksheets["CONTROL"]; if (control is null) errors.Add("No se encontró la hoja CONTROL."); else if (Convert.ToInt32(control.Cells[cell].Value ?? 0) != 0) errors.Add($"La hoja CONTROL registra errores en {cell}."); }
        }
        catch (Exception ex) { errors.Add($"No se pudo leer el archivo: {ex.Message}"); }
        return new(fileName, errors);
    }
    private static string Text(ExcelWorksheet sheet, string cell) => sheet.Cells[cell].Value?.ToString()?.Trim() ?? string.Empty;
    private static string ExtractSeries(string text) { var match = Regex.Match(text, @"\b(A|BM|D|P)\b", RegexOptions.IgnoreCase); return match.Success ? match.Value.ToUpperInvariant() : text.Trim().ToUpperInvariant(); }
}
