using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace RemTools.Consolidados;

/// <summary>
/// EPPlus 8.1 creates empty cache records for new pivots. Persist the complete
/// cache and initial layout so the downloaded workbook needs no first refresh.
/// </summary>
public static class ConsolidadoPivotCacheWriter
{
    public static void Complete(string path, IReadOnlyList<object[]> rows, DateTimeOffset capturedAt, CancellationToken ct)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Update);
        var s = ConsolidadoTemplate.S;
        XDocument Read(ZipArchiveEntry entry) { using var stream = entry.Open(); return XDocument.Load(stream); }
        void Write(ZipArchiveEntry entry, XDocument doc)
        {
            using var stream = entry.Open(); stream.SetLength(0); doc.Save(stream);
        }
        var definition = zip.Entries.Single(e => e.FullName.StartsWith("xl/pivotCache/pivotCacheDefinition") && e.FullName.EndsWith(".xml"));
        var cache = Read(definition);
        cache.Root!.SetAttributeValue("recordCount", rows.Count);
        cache.Root.SetAttributeValue("saveData", "1"); cache.Root.SetAttributeValue("refreshOnLoad", "0");
        cache.Root.SetAttributeValue("refreshedBy", "REMTool");
        cache.Root.SetAttributeValue("refreshedDate", capturedAt.UtcDateTime.ToOADate().ToString("R", CultureInfo.InvariantCulture));
        var fields = cache.Descendants(s + "cacheField").ToArray();
        var indices = new Dictionary<string, int>?[fields.Length];
        for (int i = 0; i < fields.Length; i++)
        {
            var shared = fields[i].Element(s + "sharedItems")!;
            if (rows[0][i] is string)
            {
                indices[i] = shared.Elements().Select((e, j) => (Value: (string)e.Attribute("v")!, Index: j)).ToDictionary(x => x.Value, x => x.Index);
                shared.SetAttributeValue("count", indices[i]!.Count);
            }
            else
            {
                shared.RemoveAll();
                shared.SetAttributeValue("count", 0); shared.SetAttributeValue("containsSemiMixedTypes", 0);
                shared.SetAttributeValue("containsString", 0); shared.SetAttributeValue("containsNumber", 1);
                shared.SetAttributeValue("containsInteger", rows.All(r => Convert.ToDouble(r[i]) % 1 == 0) ? 1 : 0);
            }
        }
        Write(definition, cache);
        var records = zip.Entries.Single(e => e.FullName.StartsWith("xl/pivotCache/pivotCacheRecords") && e.FullName.EndsWith(".xml"));
        using (var stream = records.Open())
        {
            stream.SetLength(0);
            using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new System.Text.UTF8Encoding(false), CloseOutput = false });
            writer.WriteStartElement("pivotCacheRecords", s.NamespaceName); writer.WriteAttributeString("count", rows.Count.ToString());
            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                writer.WriteStartElement("r", s.NamespaceName);
                for (int i = 0; i < row.Length; i++)
                {
                    writer.WriteStartElement(indices[i] == null ? "n" : "x", s.NamespaceName);
                    writer.WriteAttributeString("v", indices[i] == null
                        ? Convert.ToDouble(row[i]).ToString("R", CultureInfo.InvariantCulture)
                        : indices[i]![(string)row[i]].ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        var pivotEntry = zip.Entries.Single(e => e.FullName.StartsWith("xl/pivotTables/pivotTable") && e.FullName.EndsWith(".xml"));
        var pivot = Read(pivotEntry);
        string[] order = ["location", "pivotFields", "rowFields", "rowItems", "colFields", "colItems", "pageFields", "dataFields", "formats", "conditionalFormats", "chartFormats", "pivotHierarchies", "pivotTableStyleInfo", "filters", "rowHierarchiesUsage", "colHierarchiesUsage", "extLst"];
        var children = pivot.Root!.Elements().OrderBy(e => { var index = Array.IndexOf(order, e.Name.LocalName); return index < 0 ? 99 : index; }).ToList();
        pivot.Root.ReplaceNodes(children);
        Write(pivotEntry, pivot);
        // New slicers created by EPPlus can reuse drawing id=2. Assign one unique
        // id per anchor, shared by its Choice/Fallback representations.
        foreach (var drawing in zip.Entries.Where(e => e.FullName.StartsWith("xl/drawings/drawing") && e.FullName.EndsWith(".xml")).ToArray())
        {
            var doc = Read(drawing);
            if (!doc.Descendants().Any(e => e.Name.LocalName == "slicer")) continue;
            int id = 1;
            foreach (var anchor in doc.Root!.Elements())
            {
                foreach (var property in anchor.Descendants().Where(e => e.Name.LocalName == "cNvPr")) property.SetAttributeValue("id", id);
                id++;
            }
            Write(drawing, doc);
        }
    }
}
