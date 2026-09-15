using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RemTools.Consolidados;

public static class ConsolidadoCatalogWriter
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public static DownloadCatalog Read(string root)
    {
        var path = Path.Combine(root, "catalogo.json");
        if (!File.Exists(path)) return new DownloadCatalog(1, []);
        var catalog = JsonSerializer.Deserialize<DownloadCatalog>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("El catálogo local está vacío o dañado.");
        if (catalog.SchemaVersion != 1 || catalog.Files == null) throw new InvalidOperationException("Versión de catálogo no admitida.");
        if (catalog.Files.GroupBy(f => (f.Year, f.Series)).Any(g => g.Count() > 1))
            throw new InvalidOperationException("El catálogo tiene publicaciones duplicadas por serie/año.");
        foreach (var entry in catalog.Files)
        {
            if (entry.Series != "A" || !Regex.IsMatch(entry.File ?? "", @"^\d{4}/A/consolidado-rem-a-\d{4}-[a-zA-Z0-9-]+\.xlsx$"))
                throw new InvalidOperationException("El catálogo contiene una ruta o serie no admitida.");
            var full = Path.Combine(root, entry.File.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full)) throw new InvalidOperationException($"Falta {entry.File}. Sincronice la carpeta de publicaciones antes de actualizar su catálogo.");
            using var stream = File.OpenRead(full);
            if (stream.Length != entry.Bytes || !Convert.ToHexString(SHA256.HashData(stream)).Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{entry.File} no coincide con su catálogo. Revise la carpeta de publicaciones.");
        }
        return catalog;
    }

    public static void Commit(string root, DownloadCatalog previous, CatalogEntry entry)
    {
        var entries = previous.Files.Where(f => f.Year != entry.Year || f.Series != entry.Series).Append(entry)
            .OrderByDescending(f => f.Year).ThenBy(f => f.Series).ToList();
        var temporary = Path.Combine(root, $"catalogo-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new DownloadCatalog(1, entries), JsonOptions));
            File.Move(temporary, Path.Combine(root, "catalogo.json"), true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
