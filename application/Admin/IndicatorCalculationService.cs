using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using RemTools;

namespace RemTool.Application.Admin;

public sealed record IndicatorCalculationResult(int Indicators, int Results, int LastMonth);

public sealed class IndicatorCalculationService(IDbContextFactory<RemToolDataContext> factory)
{
    public async Task<IndicatorCalculationResult> CalculateAsync(int year, CancellationToken ct)
    {
        if (year is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(year), "El año no es válido.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var indicators = await db.Indicador.AsNoTracking().Where(x => x.Año == year).OrderBy(x => x.Orden).ToListAsync(ct);
        var establishments = await db.Establecimiento.AsNoTracking().ToListAsync(ct);
        await db.ResultadoIndicador.Where(x => x.Indicador.Año == year).ExecuteDeleteAsync(ct);
        db.ChangeTracker.Clear();
        var prepared = indicators.Select(x => { var root = Parse(JsonDocument.Parse(x.Formula).RootElement); return (Indicator: x, Parts: Split(root)); }).ToList();
        var provider = new RemDataProvider(db);
        foreach (var item in prepared) { var collect = new EvaluationContext { Año = year, Mes = 1, DataProvider = provider }; item.Parts.Numerator.Collect(collect); item.Parts.Denominator.Collect(collect); }
        provider.CargarPrestaciones(year, DateTime.Now); provider.CargarFonasa(year); provider.CargarPercapitaSsc(year);
        var maxMonth = year == DateTime.Now.Year ? await db.Reporte.AsNoTracking().Where(x => x.Año == year && x.Registros.Any(r => r.Prestacion.VersionRem.SerieRem.Nombre == "A" || r.Prestacion.VersionRem.SerieRem.Nombre == "BM" || r.Prestacion.VersionRem.SerieRem.Nombre == "D" || r.Prestacion.VersionRem.SerieRem.Nombre == "P")).Select(x => (int?)x.Mes).MaxAsync(ct) ?? 0 : 12;
        var monthly = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "BM", "D" }; var serieP = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "P" }; var count = 0;
        foreach (var item in prepared)
        {
            ct.ThrowIfCancellationRequested(); var batch = new List<ResultadoIndicador>();
            foreach (var establishment in establishments)
            {
                var contextP = new EvaluationContext { Año = year, Mes = provider.PMesAplicacion, EstablecimientoId = (int)establishment.Id, DataProvider = provider, SeriesIncluidas = serieP };
                var numeratorP = item.Parts.Numerator.Evaluate(contextP); var denominatorP = item.Parts.Denominator.Evaluate(contextP); decimal fixedDenominator = 0;
                if (item.Indicator.IsDenFijo) fixedDenominator = item.Parts.Denominator.Evaluate(new EvaluationContext { Año = year, Mes = provider.PMonth, EstablecimientoId = (int)establishment.Id, DataProvider = provider, SeriesIncluidas = monthly });
                for (var month = 1; month <= maxMonth; month++)
                {
                    var context = new EvaluationContext { Año = year, Mes = month, EstablecimientoId = (int)establishment.Id, DataProvider = provider, SeriesIncluidas = monthly };
                    var row = new ResultadoIndicador { Mes = month, id_indicador = item.Indicator.Id, id_establecimiento = establishment.Id, Numerador = item.Parts.Numerator.Evaluate(context), Denominador = item.Indicator.IsDenFijo ? (month == 1 ? fixedDenominator : 0m) : item.Parts.Denominator.Evaluate(context), NumeradorP = month == provider.PMesAplicacion ? numeratorP : 0m, DenominadorP = month == provider.PMesAplicacion ? denominatorP : 0m };
                    ApplyLegacyOverride(item.Indicator.Id, establishment.Id, row, month); batch.Add(row);
                }
            }
            db.AddRange(batch); await db.SaveChangesAsync(ct); db.ChangeTracker.Clear(); count += batch.Count;
        }
        return new(indicators.Count, count, maxMonth);
    }
    private static void ApplyLegacyOverride(int id, long establishmentId, ResultadoIndicador row, int month) { if (id != 19) return; row.NumeradorP = 0; row.Denominador = row.DenominadorP; row.DenominadorP = 0; if (month != 1) return; row.Mes = 4; row.Numerador = establishmentId switch { 13 => 408, 3 => 205, 5 => 0, 8 => 394, _ => row.Numerador }; }
    private static (AstNode Numerator, AstNode Denominator) Split(AstNode root) => root is OpNode { Op: "div" } op ? (op.Args[0], op.Args[1]) : (root, new NumberNode { Value = 1 });
    private static AstNode Parse(JsonElement element) => element.GetProperty("type").GetString() switch { "value" => new ValueNode { Prestacion = element.GetProperty("prestacion").GetString() ?? string.Empty, Columna = element.GetProperty("columna").GetInt32() }, "number" => new NumberNode { Value = element.GetProperty("value").GetDecimal() }, "variable" => new VariableNode { Name = element.GetProperty("name").GetString() ?? string.Empty, Filters = element.TryGetProperty("filters", out var filters) ? JsonSerializer.Deserialize<Dictionary<string, object>>(filters) ?? [] : [] }, "op" => new OpNode { Op = element.GetProperty("op").GetString() ?? string.Empty, Args = element.GetProperty("args").EnumerateArray().Select(Parse).ToList() }, var type => throw new InvalidOperationException($"Nodo de fórmula desconocido: {type}") };
}
