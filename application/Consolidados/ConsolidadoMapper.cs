namespace RemTools.Consolidados;

public static class ConsolidadoMapper
{
    public static MappedConsolidado Map(ConsolidadoSnapshot source, CancellationToken ct)
    {
        var targets = source.Definitions.Where(p => p.VersionId == source.Version.Id).ToList();
        var duplicate = targets.GroupBy(p => p.Code, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null) throw new InvalidOperationException($"Código ambiguo en la versión de salida: {duplicate.Key}.");
        var target = targets.ToDictionary(p => p.Code, StringComparer.Ordinal);
        var definitions = source.Definitions.ToDictionary(p => p.Id);
        var reports = source.Reports.ToDictionary(r => r.Id);
        var seen = new HashSet<(int, int)>();
        var seenCode = new HashSet<(int, string)>();
        var rows = new Dictionary<(string Code, long Establishment, int Month), decimal[]>();
        int omitted = 0;
        foreach (var record in source.Records)
        {
            ct.ThrowIfCancellationRequested();
            if (!seen.Add((record.ReportId, record.PrestacionId)))
                throw new InvalidOperationException($"Prestación duplicada {record.PrestacionId} en reporte {record.ReportId}.");
            var definition = definitions[record.PrestacionId];
            if (!seenCode.Add((record.ReportId, definition.Code)))
                throw new InvalidOperationException($"El reporte {record.ReportId} mezcla versiones del código {definition.Code}.");
            if (record.Values.Length != definition.Columns.Count)
                throw new InvalidOperationException($"{definition.Code}: {record.Values.Length} valores y {definition.Columns.Count} coordenadas en versión {definition.VersionId}.");
            if (record.Values.All(v => v == 0)) { omitted++; continue; }
            if (!target.TryGetValue(definition.Code, out var destination))
                throw new InvalidOperationException($"El código {definition.Code} tiene datos y no existe en la versión de salida.");
            if (definition.VersionId != destination.VersionId &&
                (definition.StructureSignature.Length == 0 || definition.StructureSignature != destination.StructureSignature ||
                 definition.Name != destination.Name || definition.Sheet != destination.Sheet ||
                 !definition.Columns.SequenceEqual(destination.Columns)))
                throw new InvalidOperationException($"{definition.Code}: estructura incompatible entre versiones {definition.VersionId} y {destination.VersionId}. Revise la equivalencia antes de consolidar.");
            if (destination.Columns.Count != record.Values.Length)
                throw new InvalidOperationException($"{definition.Code}: la longitud no corresponde a la versión de salida.");
            if (record.Values.Any(v => decimal.Abs(v) > 999999999999999m))
                throw new InvalidOperationException($"{definition.Code}: valor fuera de la precisión numérica admitida en Excel.");
            var report = reports[record.ReportId];
            var key = (definition.Code, report.EstablishmentId, report.Month);
            if (!rows.TryGetValue(key, out var values)) rows[key] = values = new decimal[record.Values.Length];
            for (int i = 0; i < values.Length; i++) values[i] += record.Values[i];
        }
        if (rows.Count == 0) throw new InvalidOperationException("No hay actividad distinta de cero. Se conserva la publicación anterior.");
        return new MappedConsolidado(source, rows.OrderBy(x => x.Key.Code, StringComparer.Ordinal)
            .ThenBy(x => x.Key.Establishment).ThenBy(x => x.Key.Month)
            .Select(x => new ActivityRow(x.Key.Code, x.Key.Establishment, x.Key.Month, x.Value)).ToList(), target, omitted);
    }
}
