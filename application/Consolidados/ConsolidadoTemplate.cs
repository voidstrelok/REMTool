using System.IO.Compression;
using System.Xml.Linq;

namespace RemTools.Consolidados;

/// <summary>Strips obsolete pivot data before EPPlus reads the template, without touching the original.</summary>
public static class ConsolidadoTemplate
{
    internal static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static MemoryStream Prepare(string path, CancellationToken ct)
    {
        if (!File.Exists(path) || !Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Seleccione el consolidado plantilla en formato .xlsx.");
        using var input = ZipFile.OpenRead(path);
        XDocument Read(string name)
        {
            using var stream = (input.GetEntry(name) ?? throw new InvalidOperationException($"Falta {name} en la plantilla.")).Open();
            return XDocument.Load(stream);
        }
        var workbook = Read("xl/workbook.xml");
        var rels = Read("xl/_rels/workbook.xml.rels");
        var sheet = workbook.Descendants(S + "sheet").SingleOrDefault(s => (string?)s.Attribute("name") == "FILTRO")
            ?? throw new InvalidOperationException("La plantilla debe contener la hoja FILTRO y las hojas REM con BUSCARV.");
        var target = (string)rels.Root!.Elements().Single(r => (string?)r.Attribute("Id") == (string?)sheet.Attribute(R + "id")).Attribute("Target")!;
        var sheetPath = ResolvePart("xl/workbook.xml", target);
        var sheetRels = RelationshipsPath(sheetPath);
        var removed = input.Entries.Select(e => e.FullName).Where(n =>
            n.StartsWith("xl/pivotCache/") || n.StartsWith("xl/pivotTables/") ||
            n.StartsWith("xl/slicerCaches/") || n.StartsWith("xl/slicers/") ||
            n.StartsWith("xl/externalLinks/") || n.StartsWith("xl/queryTables/") ||
            n is "xl/connections.xml" or "xl/calcChain.xml").ToHashSet(StringComparer.Ordinal);
        removed.Add(sheetRels);
        if (input.GetEntry(sheetRels) != null)
            foreach (var rel in Read(sheetRels).Root!.Elements().Where(r => ((string?)r.Attribute("Type"))?.EndsWith("/drawing") == true))
            {
                var drawing = ResolvePart(sheetPath, (string)rel.Attribute("Target")!);
                removed.Add(drawing); removed.Add(RelationshipsPath(drawing));
            }
        var oldFilter = Read(sheetPath);
        var header = oldFilter.Descendants(S + "row").FirstOrDefault(r => (string?)r.Attribute("r") == "8");
        if (header == null) throw new InvalidOperationException("FILTRO no contiene la fila 8 que identifica las columnas de valores.");
        var filter = new XDocument(new XElement(S + "worksheet",
            new XElement(S + "sheetViews", new XElement(S + "sheetView", new XAttribute("workbookViewId", "0"))),
            new XElement(S + "sheetData", new XElement(header))));
        workbook.Descendants(S + "pivotCaches").Remove();
        workbook.Descendants().Where(e => e.Name.LocalName == "slicerCaches").ToList().ForEach(e => e.Parent!.Remove());
        workbook.Descendants(S + "definedName").Where(n => ((string?)n.Attribute("name"))?.StartsWith("SegmentaciónDeDatos", StringComparison.OrdinalIgnoreCase) == true).Remove();
        var output = new MemoryStream();
        try
        {
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
                foreach (var entry in input.Entries)
                {
                    ct.ThrowIfCancellationRequested();
                    if (removed.Contains(entry.FullName)) continue;
                    XDocument? document = entry.FullName switch
                    {
                        "xl/workbook.xml" => workbook,
                        _ when entry.FullName == sheetPath => filter,
                        _ => null
                    };
                    if (entry.FullName.EndsWith(".rels"))
                    {
                        document = Read(entry.FullName);
                        document.Root!.Elements().Where(r =>
                        {
                            var type = (string?)r.Attribute("Type") ?? "";
                            return new[] { "/pivot", "/slicer", "/externalLink", "/connections", "/queryTable", "/calcChain" }.Any(type.Contains);
                        }).Remove();
                    }
                    if (entry.FullName == "[Content_Types].xml")
                    {
                        document = Read(entry.FullName);
                        document.Root!.Elements().Where(e => removed.Contains(((string?)e.Attribute("PartName") ?? "").TrimStart('/'))).Remove();
                    }
                    using var destination = archive.CreateEntry(entry.FullName, CompressionLevel.Fastest).Open();
                    if (document != null) document.Save(destination);
                    else { using var source = entry.Open(); source.CopyTo(destination); }
                }
            output.Position = 0;
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    private static string ResolvePart(string source, string target) =>
        new Uri(new Uri("http://package/" + source), target).AbsolutePath.TrimStart('/');
    private static string RelationshipsPath(string part) =>
        part.Insert(part.LastIndexOf('/') + 1, "_rels/") + ".rels";
}
