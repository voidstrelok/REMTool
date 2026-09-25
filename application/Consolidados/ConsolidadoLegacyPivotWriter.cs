using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace RemTools.Consolidados;

/// <summary>
/// Updates the cache used by the original FILTRO pivot table without loading or
/// rewriting the workbook through EPPlus. All worksheet formulas, formatting,
/// slicers and pivot layout remain owned by the supplied template.
/// </summary>
public sealed class ConsolidadoLegacyPivotWriter
{
    private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CL");

    public int Write(string templatePath, string destinationPath, ConsolidadoSnapshot snapshot, IProgress<string> progress, CancellationToken ct)
    {
        File.Copy(templatePath, destinationPath, true);
        var definitions = snapshot.Definitions.ToDictionary(p => p.Id);
        var reports = snapshot.Reports.ToDictionary(r => r.Id);
        var establishments = snapshot.Establishments.ToDictionary(e => e.Id);
        var rows = snapshot.Records.Where(r => definitions.ContainsKey(r.PrestacionId) && reports.ContainsKey(r.ReportId)).ToArray();
        if (rows.Length == 0) throw new InvalidOperationException("No hay registros Serie A para actualizar la tabla dinámica.");

        var codes = rows.Select(r => definitions[r.PrestacionId].Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var establishmentNames = rows.Select(r => establishments[reports[r.ReportId].EstablishmentId].Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var sectors = rows.Select(r => establishments[reports[r.ReportId].EstablishmentId].Sector).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var months = rows.Select(r => MonthName(reports[r.ReportId].Month)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        progress.Report($"Actualizando la caché de FILTRO con {rows.Length:N0} registros de la base...");
        using var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Update);
        var definitionEntry = archive.Entries.SingleOrDefault(e => e.FullName.StartsWith("xl/pivotCache/pivotCacheDefinition", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("La plantilla no contiene una caché de tabla dinámica.");
        var recordsEntry = archive.Entries.SingleOrDefault(e => e.FullName.StartsWith("xl/pivotCache/pivotCacheRecords", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("La plantilla no contiene registros de caché para FILTRO.");

        var definition = Read(definitionEntry);
        var fields = definition.Descendants(S + "cacheField").ToDictionary(f => (string)f.Attribute("name")!, StringComparer.Ordinal);
        foreach (var required in new[] { "id", "id_prestacion", "id_reporte", "prestacion.codigo_prestacion", "reporte.mes", "reporte.establecimiento.nombre", "reporte.establecimiento.sector.nombre", "NMes" })
            if (!fields.ContainsKey(required)) throw new InvalidOperationException($"La caché de la plantilla no contiene el campo requerido {required}.");

        // PivotTable XML refers to these shared-item indices. Preserve every
        // existing item and only append missing values so those references stay valid.
        var codeIndexes = MergeSharedItems(fields["prestacion.codigo_prestacion"], codes);
        var establishmentIndexes = MergeSharedItems(fields["reporte.establecimiento.nombre"], establishmentNames);
        var sectorIndexes = MergeSharedItems(fields["reporte.establecimiento.sector.nombre"], sectors);
        var monthIndexes = MergeSharedItems(fields["NMes"], months);
        definition.Root!.SetAttributeValue("recordCount", rows.Length);
        definition.Root.SetAttributeValue("saveData", "1");
        definition.Root.SetAttributeValue("refreshOnLoad", "1");
        definition.Root.SetAttributeValue("refreshedBy", "REMTool");
        definition.Root.SetAttributeValue("refreshedDate", snapshot.CapturedAt.UtcDateTime.ToOADate().ToString("R", CultureInfo.InvariantCulture));
        Write(definitionEntry, definition);
        SyncPivotItems(archive, new Dictionary<int, IReadOnlyDictionary<string, int>>
        {
            [51] = codeIndexes,
            [55] = establishmentIndexes,
            [56] = sectorIndexes,
            [57] = monthIndexes
        });

        using (var stream = recordsEntry.Open())
        {
            stream.SetLength(0);
            using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new System.Text.UTF8Encoding(false), CloseOutput = false });
            writer.WriteStartDocument(true);
            writer.WriteStartElement("pivotCacheRecords", S.NamespaceName);
            writer.WriteAttributeString("count", rows.Length.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < rows.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                var row = rows[i];
                var definitionRow = definitions[row.PrestacionId];
                var report = reports[row.ReportId];
                var establishment = establishments[report.EstablishmentId];
                var month = MonthName(report.Month);
                writer.WriteStartElement("r", S.NamespaceName);
                Number(writer, i + 1);
                Number(writer, row.PrestacionId);
                Number(writer, row.ReportId);
                for (var valueIndex = 0; valueIndex < 48; valueIndex++)
                    Number(writer, valueIndex < row.Values.Length ? row.Values[valueIndex] : 0m);
                SharedIndex(writer, codeIndexes[definitionRow.Code]);
                writer.WriteStartElement("d", S.NamespaceName); writer.WriteAttributeString("v", snapshot.Version.Date.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)); writer.WriteEndElement();
                writer.WriteStartElement("s", S.NamespaceName); writer.WriteAttributeString("v", "A"); writer.WriteEndElement();
                Number(writer, report.Month);
                SharedIndex(writer, establishmentIndexes[establishment.Name]);
                SharedIndex(writer, sectorIndexes[establishment.Sector]);
                SharedIndex(writer, monthIndexes[month]);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        progress.Report("Caché actualizada. Excel recalculará la tabla dinámica al abrir el archivo.");
        return rows.Length;
    }

    private static string MonthName(int month) => new DateTime(2000, month, 1).ToString("MMMM", Spanish);
    private static void Number(XmlWriter writer, int value) => Number(writer, (decimal)value);
    private static void Number(XmlWriter writer, decimal value)
    {
        writer.WriteStartElement("n", S.NamespaceName);
        writer.WriteAttributeString("v", value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }
    private static void SharedIndex(XmlWriter writer, int index)
    {
        writer.WriteStartElement("x", S.NamespaceName);
        writer.WriteAttributeString("v", index.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }
    private static XDocument Read(ZipArchiveEntry entry) { using var stream = entry.Open(); return XDocument.Load(stream); }
    private static void Write(ZipArchiveEntry entry, XDocument document) { using var stream = entry.Open(); stream.SetLength(0); document.Save(stream); }
    private static Dictionary<string, int> MergeSharedItems(XElement field, IEnumerable<string> values)
    {
        var shared = field.Element(S + "sharedItems") ?? new XElement(S + "sharedItems");
        if (shared.Parent == null) field.Add(shared);
        // The position is the position among *all* child items. A sharedItems
        // list may include <m/> blanks between or after <s/> values; omitting
        // those would assign new values to an existing blank index.
        var indexes = shared.Elements().Select((item, index) => (Value: item.Name == S + "s" ? (string?)item.Attribute("v") : null, Index: index))
            .Where(x => x.Value != null).ToDictionary(x => x.Value!, x => x.Index, StringComparer.Ordinal);
        foreach (var value in values)
            if (!indexes.ContainsKey(value))
            {
                indexes[value] = shared.Elements().Count();
                shared.Add(new XElement(S + "s", new XAttribute("v", value)));
            }
        shared.SetAttributeValue("count", shared.Elements().Count());
        return indexes;
    }

    private static void SyncPivotItems(ZipArchive archive, IReadOnlyDictionary<int, IReadOnlyDictionary<string, int>> indexesByField)
    {
        foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("xl/pivotTables/pivotTable", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal)))
        {
            var table = Read(entry);
            var pivotFields = table.Root?.Element(S + "pivotFields")?.Elements(S + "pivotField").ToArray();
            if (pivotFields == null) continue;
            foreach (var (fieldIndex, indexes) in indexesByField)
            {
                if (fieldIndex >= pivotFields.Length) continue;
                var items = pivotFields[fieldIndex].Element(S + "items");
                if (items == null) continue;
                var existing = items.Elements(S + "item").Select(i => (int?)i.Attribute("x")).Where(i => i.HasValue).Select(i => i!.Value).ToHashSet();
                foreach (var index in indexes.Values.Distinct().Order())
                    if (existing.Add(index))
                    {
                        var item = new XElement(S + "item", new XAttribute("x", index));
                        var defaultItem = items.Elements(S + "item").FirstOrDefault(i => (string?)i.Attribute("t") == "default");
                        if (defaultItem == null) items.Add(item); else defaultItem.AddBeforeSelf(item);
                    }
                items.SetAttributeValue("count", items.Elements(S + "item").Count());
            }
            Write(entry, table);
        }
    }
}
