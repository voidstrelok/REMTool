using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RemTool.Shared;
using RemTool.Shared.Ingest;

namespace RemTool.Application.Admin;

/// <summary>
/// Calcula todo en local y sube a producción un batch con la estructura completa y los datos de un año:
/// resultados siempre; per cápita y fonasa sólo cuando se piden (se recargaron o modificaron).
/// Prod reemplaza esos datos con lo recibido; no ejecuta cálculos ni cargas pesadas.
/// </summary>
public sealed class ProductionPublisher(IDbContextFactory<RemToolDataContext> factory)
{
    public async Task<JsonElement> PublishAsync(int year, bool percapita, bool fonasa, string baseUrl, string apiKey, CancellationToken ct)
    {
        if (year is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(year), "El año no es válido.");
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Configure Production:BaseUrl y Production:IngestApiKey en adminsettings.json.");

        var slices = new List<YearSlice> { IngestTables.Results };
        if (percapita) slices.Add(IngestTables.Percapita);
        if (fonasa) slices.Add(IngestTables.Fonasa);

        var parts = new List<string>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            db.Database.SetCommandTimeout(300);
            foreach (var table in IngestTables.Structure)
                parts.Add(Entry(table, await JsonAsync(db, table, "TRUE", year, ct)));
            foreach (var slice in slices)
                parts.Add(Entry(slice.Table, await JsonAsync(db, slice.Table, slice.Filter, year, ct)));
        }
        var body = $"{{\"year\":{year},\"tables\":{{{string.Join(',', parts)}}}}}";

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

    // ponytail: una query por tabla y todo el JSON en memoria. Si fonasa crece mucho, pasar a streaming + gzip.
    private static Task<string> JsonAsync(RemToolDataContext db, string table, string filter, int year, CancellationToken ct) =>
        db.Database.SqlQueryRaw<string>(
            $"SELECT COALESCE(json_agg(x), '[]'::json)::text AS \"Value\" FROM \"REMTool\".\"{table}\" x WHERE {filter}", year)
            .SingleAsync(ct);
}
