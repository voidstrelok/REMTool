using System.Diagnostics;
using System.Security.Cryptography;

namespace RemTools.Consolidados;

public sealed class ConsolidadoGenerationService(ConsolidadoDataReader reader)
{
    public async Task<GenerationResult> GenerateAsync(GenerationRequest request, IProgress<string> progress, CancellationToken ct)
    {
        if (!File.Exists(request.TemplatePath)) throw new InvalidOperationException("La plantilla no existe.");
        if (string.IsNullOrWhiteSpace(request.OutputDirectory)) throw new InvalidOperationException("Seleccione una carpeta de publicación.");
        var root = Path.GetFullPath(request.OutputDirectory);
        Directory.CreateDirectory(root);
        // Exclusive local lock prevents concurrent generators from losing catalog entries.
        using var publicationLock = new FileStream(Path.Combine(root, ".generacion.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var catalog = ConsolidadoCatalogWriter.Read(root);
        var clock = Stopwatch.StartNew();
        var snapshot = await reader.ReadAsync(request, progress, ct);
        return GenerateSnapshot(request, snapshot, catalog, progress, clock, ct);
    }

    public static GenerationResult GenerateSnapshot(GenerationRequest request, ConsolidadoSnapshot snapshot,
        DownloadCatalog catalog, IProgress<string> progress, Stopwatch clock, CancellationToken ct)
    {
        var root = Path.GetFullPath(request.OutputDirectory);
        var folder = Path.Combine(root, request.Year.ToString(), "A");
        Directory.CreateDirectory(folder);
        var id = $"{DateTime.UtcNow:yyyyMMddTHHmmss}-{Guid.NewGuid():N}";
        var fileName = $"consolidado-rem-a-{request.Year}-{id}.xlsx";
        var path = Path.Combine(folder, fileName);
        var temporary = Path.Combine(folder, $".{id}.tmp.xlsx");
        bool committed = false;
        try
        {
            // Keep the supplied workbook intact. Only the cached source records
            // of FILTRO's existing pivot table are replaced.
            var recordCount = new ConsolidadoLegacyPivotWriter().Write(request.TemplatePath, temporary, snapshot, progress, ct);
            var warnings = new List<string>();
            ct.ThrowIfCancellationRequested();
            string hash;
            using (var stream = File.OpenRead(temporary)) hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            var bytes = new FileInfo(temporary).Length;
            ct.ThrowIfCancellationRequested();
            // Commit is deliberately not cancellable between the two atomic file moves.
            File.Move(temporary, path);
            var entry = new CatalogEntry("A", request.Year, $"{request.Year}/A/{fileName}", snapshot.CapturedAt,
                snapshot.Reports.Select(r => r.Month).Distinct().Order().ToArray(), snapshot.Version.Name, bytes, hash, warnings.ToArray());
            ConsolidadoCatalogWriter.Commit(root, catalog, entry);
            committed = true;
            progress.Report($"Terminado: {recordCount:N0} registros, {bytes / 1048576d:F1} MB, {clock.Elapsed.TotalSeconds:F1} s. Suba el Excel y después catalogo.json.");
            return new GenerationResult(path, recordCount, bytes, clock.Elapsed, warnings);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            if (!committed && File.Exists(path)) File.Delete(path);
        }
    }
}
