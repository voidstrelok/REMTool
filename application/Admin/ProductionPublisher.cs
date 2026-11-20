using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using RemTool.Shared.Ingest;

namespace RemTool.Application.Admin;

/// <summary>Qué subir además de la configuración (que va siempre). Registros/per cápita/fonasa son del año indicado.</summary>
public sealed record PublishOptions(bool Structure = false, int? Year = null, bool Registros = false, bool Percapita = false, bool Fonasa = false);

/// <summary>
/// Todo se procesa en local; esto sube a producción un batch que prod reemplaza tal cual (sin cálculos ni cargas pesadas allá).
/// La configuración del panel va siempre; estructura REM y datos por año sólo cuando se piden.
/// </summary>
public sealed class ProductionPublisher(IDbContextFactory<RemToolDataContext> factory)
{
    public async Task<JsonElement> PublishAsync(PublishOptions options, string baseUrl, string apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Configure Production:BaseUrl y Production:IngestApiKey en adminsettings.json.");

        var slices = new List<YearSlice>();
        if (options.Registros) slices.AddRange([IngestTables.Reporte, IngestTables.Registro]);
        if (options.Percapita) slices.Add(IngestTables.Percapita);
        if (options.Fonasa) slices.Add(IngestTables.Fonasa);
        var year = options.Year ?? 0;
        if (slices.Count > 0 && year is < 2000 or > 2100) throw new ArgumentException("Indique un año válido para los datos por año.");

        var full = options.Structure ? IngestTables.FullTables : IngestTables.Config;
        var parts = new List<string>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            db.Database.SetCommandTimeout(300);
            foreach (var table in full) parts.Add(Entry(table, await JsonAsync(db, table, "TRUE", year, ct)));
            foreach (var slice in slices) parts.Add(Entry(slice.Table, await JsonAsync(db, slice.Table, slice.Filter, year, ct)));
        }
        var body = $"{{\"year\":{(slices.Count > 0 ? year.ToString() : "null")},\"tables\":{{{string.Join(',', parts)}}}}}";

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + "/api/ingest/batch")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Ingest-Key", apiKey);
        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Producción rechazó el batch ({(int)response.StatusCode}): {text}");
        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    private static string Entry(string table, string json) => $"{JsonSerializer.Serialize(table)}:{json}";

    // ponytail: una query por tabla y todo el JSON en memoria. Si fonasa/registros crecen mucho, pasar a streaming + gzip.
    private static Task<string> JsonAsync(RemToolDataContext db, string table, string filter, int year, CancellationToken ct) =>
        db.Database.SqlQueryRaw<string>(
            $"SELECT COALESCE(json_agg(x), '[]'::json)::text AS \"Value\" FROM \"REMTool\".\"{table}\" x WHERE {filter}", year)
            .SingleAsync(ct);
}
