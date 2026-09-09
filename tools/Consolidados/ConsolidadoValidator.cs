using System.IO.Compression;
using System.Xml.Linq;

namespace RemTools.Consolidados;

public static class ConsolidadoValidator
{
    public static void Validate(string path, int rows, int lookups)
    {
        using var zip = ZipFile.OpenRead(path);
        var s = ConsolidadoTemplate.S;
        XDocument Read(string name) { using var stream = zip.GetEntry(name)!.Open(); return XDocument.Load(stream); }
        if (zip.Entries.Any(e => e.FullName.StartsWith("xl/externalLinks/") || e.FullName is "xl/connections.xml"))
            throw new InvalidOperationException("El archivo guardado conserva conexiones externas.");
        var definitions = zip.Entries.Where(e => e.FullName.StartsWith("xl/pivotCache/pivotCacheDefinition") && e.FullName.EndsWith(".xml")).ToArray();
        if (definitions.Length != 1) throw new InvalidOperationException("El archivo debe contener exactamente una caché.");
        var cache = Read(definitions[0].FullName);
        if ((int?)cache.Root!.Attribute("recordCount") != rows)
            throw new InvalidOperationException($"La caché guardada declara {cache.Root.Attribute("recordCount")?.Value ?? "sin cantidad"} filas; se esperaban {rows}. Origen: {cache.Descendants(s + "worksheetSource").Single()}");
        var source = cache.Descendants(s + "worksheetSource").Single();
        if ((string?)source.Attribute("sheet") != "DATOS" && (string?)source.Attribute("name") != "DatosConsolidado")
            throw new InvalidOperationException("La tabla dinámica no apunta a DATOS.");
        if (zip.Entries.Count(e => e.FullName.StartsWith("xl/slicerCaches/slicerCache") && e.FullName.EndsWith(".xml")) != 3)
            throw new InvalidOperationException("No se guardaron los tres segmentadores.");
        var pivotPart = zip.Entries.Single(e => e.FullName.StartsWith("xl/pivotTables/pivotTable") && e.FullName.EndsWith(".xml"));
        var pivot = Read(pivotPart.FullName);
        if (pivot.Descendants(s + "rowItems").Single().Elements().Count() == 0 ||
            (string?)pivot.Descendants(s + "location").Single().Attribute("ref") == "A9")
            throw new InvalidOperationException("No se guardó la vista inicial de la tabla dinámica.");
        int found = 0;
        foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/sheet") && e.FullName.EndsWith(".xml")))
        {
            var sheet = Read(entry.FullName);
            // EPPlus may serialize shared formulas. Count their anchor ranges once.
            foreach (var formula in sheet.Descendants(s + "f").Where(f => f.Value.Contains("VLOOKUP(") && f.Value.Contains("FILTRO!")))
            {
                var reference = (string?)formula.Attribute("ref");
                if (reference == null) found++;
                else { var a = new OfficeOpenXml.ExcelAddress(reference); found += a.Rows * a.Columns; }
            }
        }
        if (found != lookups) throw new InvalidOperationException($"Se esperaban {lookups} BUSCARV y se guardaron {found}.");
    }
}
