using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;

namespace RemTools.Consolidados;

public sealed class ConsolidadoExcelWriter
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CL");
    private static readonly Regex Lookup = new(
        @"^IFERROR\(VLOOKUP\(\$?A(?<row>\d+),(?:'FILTRO'|FILTRO)!\$?A\$?\d+:\$?[A-Z]+\$?\d+,(?:'FILTRO'|FILTRO)!(?<column>\$?[A-Z]+\$?8)\+1,FALSE\),0\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private sealed record Binding(string Sheet, string Address, string Code, int Index, int LookupRow, bool RewriteFormula = true);

    public List<string> Write(string template, string destination, MappedConsolidado mapped, IProgress<string> progress, CancellationToken ct)
    {
        progress.Report("Preparando la plantilla sin la caché ni los vínculos antiguos...");
        using var clean = ConsolidadoTemplate.Prepare(template, ct);
        using var package = new ExcelPackage(clean);
        var workbook = package.Workbook;
        var filter = workbook.Worksheets["FILTRO"];
        var warnings = new List<string>();
        var missingColumns = new HashSet<(string Code, int Index)>();
        var inheritedErrors = new HashSet<string>();
        var bindings = new List<Binding>();
        var zeroBasedLookupHeaders = false;
        foreach (var headerCell in filter.Cells[8, 2, 8, 200])
        {
            if (headerCell.Value == null) continue;
            if (int.TryParse(Convert.ToString(headerCell.Value, CultureInfo.InvariantCulture), out var firstHeader))
            {
                zeroBasedLookupHeaders = firstHeader == 0;
                break;
            }
        }
        foreach (var ws in workbook.Worksheets.Where(s => s.Name != "FILTRO"))
        {
            if (ws.Dimension == null) continue;
            foreach (var cell in ws.Cells[ws.Dimension.Address])
            {
                ct.ThrowIfCancellationRequested();
                if (cell.Value is ExcelErrorValue || cell.Formula.Contains("#REF!"))
                    inheritedErrors.Add($"{ws.Name}!{cell.Address}");
                if (string.IsNullOrEmpty(cell.Formula)) continue;
                if (cell.Formula.Contains('[') && Regex.IsMatch(cell.Formula, @"\[[^]]+\][^!]*!"))
                    throw new InvalidOperationException($"Vínculo externo en {ws.Name}!{cell.Address}.");
                if (!cell.Formula.Contains("FILTRO!", StringComparison.OrdinalIgnoreCase) && !cell.Formula.Contains("'FILTRO'!", StringComparison.OrdinalIgnoreCase)) continue;
                var match = Lookup.Match(cell.Formula);
                if (!match.Success) throw new InvalidOperationException($"Fórmula de consulta no admitida en {ws.Name}!{cell.Address}: revise la plantilla.");
                var code = ws.Cells[int.Parse(match.Groups["row"].Value), 1].Text.Trim();
                var index = Convert.ToInt32(filter.Cells[match.Groups["column"].Value].Value, CultureInfo.InvariantCulture);
                if (zeroBasedLookupHeaders) index++;
                if (index < 1) index = 1;
                if (!mapped.Target.TryGetValue(code, out var definition))
                {
                    if (index < 1) throw new InvalidOperationException($"{ws.Name}!{cell.Address}: columna {index} inválida para el código {code}.");
                    if (missingColumns.Add((code, index)))
                        warnings.Add($"{ws.Name}!{cell.Address}: el código {code} no existe en la versión elegida; se completará con cero.");
                    bindings.Add(new Binding(ws.Name, cell.Address, code, index, int.Parse(match.Groups["row"].Value)));
                    continue;
                }
                // Prefer the physical column registered for this prestación.
                // Some legacy templates have shifted or stale row-8 ordinals.
                var formulaColumn = new ExcelCellAddress(cell.Address).Column;
                var coordinateIndex = definition.Columns.FindIndex(c =>
                {
                    try { return new ExcelCellAddress(c.Address).Column == formulaColumn; }
                    catch (Exception) { return false; }
                });
                if (coordinateIndex >= 0) index = coordinateIndex + 1;
                if (definition.Sheet != ws.Name || index < 1)
                    throw new InvalidOperationException($"{ws.Name}!{cell.Address}: columna {index} incompatible con {code} en la versión elegida (hoja registrada '{definition.Sheet}', {definition.Columns.Count} columnas disponibles).");
                if (index > definition.Columns.Count && missingColumns.Add((code, index)))
                    warnings.Add($"{ws.Name}!{cell.Address}: la versión no registra la columna {index} de {code}; se completará con cero.");
                bindings.Add(new Binding(ws.Name, cell.Address, code, index, int.Parse(match.Groups["row"].Value)));
            }
        }
        if (bindings.Count == 0) throw new InvalidOperationException("La plantilla no contiene BUSCARV compatibles con FILTRO.");
        var represented = bindings.Select(b => (b.Code, b.Index)).ToHashSet();
        var computedBindings = new List<Binding>();
        foreach (var group in mapped.Rows.GroupBy(r => r.Code))
        {
            var row = group.First();
            for (var i = 0; i < row.Values.Length; i++)
                if (group.Any(r => r.Values[i] != 0) && !represented.Contains((row.Code, i + 1)))
                {
                    var definition = mapped.Target[row.Code];
                    // Synthetic bindings for columns absent from the selected
                    // version cannot provide a coordinate for total offsets.
                    var examples = bindings.Where(b => b.Code == row.Code && b.Index <= definition.Columns.Count).ToArray();
                    if (!definition.Columns[i].IsTotal || examples.Length == 0)
                    {
                        int baseColumn;
                        try { baseColumn = new ExcelCellAddress(definition.Columns[i].Address).Column; }
                        catch (Exception) { throw new InvalidOperationException($"La plantilla no representa {row.Code}, columna {i + 1} y la coordenada {definition.Columns[i].Address} no es vÃ¡lida."); }
                        var targetSheet = workbook.Worksheets[definition.Sheet];
                        if (targetSheet == null || targetSheet.Dimension == null)
                            throw new InvalidOperationException($"La plantilla no contiene la hoja {definition.Sheet} para escribir {row.Code}, columna {i + 1}.");
                        var targetRows = Enumerable.Range(targetSheet.Dimension?.Start.Row ?? 1, targetSheet.Dimension?.Rows ?? 0)
                            .Where(r => targetSheet.Cells[r, 1].Text.Trim() == row.Code).ToArray();
                        if (targetRows.Length == 0)
                            throw new InvalidOperationException($"La plantilla no contiene una fila para {row.Code}; no se puede escribir la columna {i + 1} con datos.");
                        foreach (var targetRow in targetRows)
                            computedBindings.Add(new Binding(definition.Sheet, targetSheet.Cells[targetRow, baseColumn].Address, row.Code, i + 1, targetRow));
                        continue;
                    }
                    var offsets = examples.Select(b => new ExcelCellAddress(b.Address).Column - new ExcelCellAddress(definition.Columns[b.Index - 1].Address).Column).Distinct().ToArray();
                    if (offsets.Length != 1)
                    {
                        warnings.Add($"No se pudo ubicar el total {row.Code}, columna {i + 1} en la plantilla; se conserva la fórmula existente.");
                        continue;
                    }
                    foreach (var example in examples.DistinctBy(b => b.LookupRow))
                    {
                        int baseColumn;
                        try { baseColumn = new ExcelCellAddress(definition.Columns[i].Address).Column; }
                        catch (Exception) {
                            warnings.Add($"La coordenada {definition.Columns[i].Address} del total {row.Code} no es válida; se conserva la plantilla.");
                            continue;
                        }
                        var col = baseColumn + offsets[0];
                        if (col < 1 || col > ExcelPackage.MaxColumns)
                        {
                            warnings.Add($"La coordenada calculada para el total {row.Code}, columna {i + 1} no es válida; se conserva la plantilla.");
                            continue;
                        }
                        var cell = workbook.Worksheets[example.Sheet].Cells[example.LookupRow, col];
                        if (string.IsNullOrWhiteSpace(cell.Formula))
                        {
                            warnings.Add($"Falta la fórmula del total {example.Sheet}!{cell.Address}; se conserva la plantilla.");
                            continue;
                        }
                        computedBindings.Add(new Binding(example.Sheet, cell.Address, row.Code, i + 1, example.LookupRow, false));
                    }
                }
        }
        if (inheritedErrors.Count > 0)
            warnings.Add("Errores heredados de la plantilla: " + string.Join(", ", inheritedErrors.Order()));
        foreach (var name in new[] { "DATOS", "COBERTURA" })
            if (workbook.Worksheets[name] != null) workbook.Worksheets.Delete(name);
        var data = workbook.Worksheets.Add("DATOS");
        var width = Math.Max(bindings.Max(b => b.Index), mapped.Rows.Max(r => r.Values.Length));
        var headers = new object[width + 4];
        headers[0] = "Codigo";
        for (int i = 0; i < width; i++) headers[i + 1] = $"Valor{i + 1:00}";
        headers[width + 1] = "Establecimiento"; headers[width + 2] = "Mes"; headers[width + 3] = "Sector";
        data.Cells[1, 1].LoadFromArrays(new[] { headers });
        var establishments = mapped.Source.Establishments.ToDictionary(e => e.Id);
        var arrays = new List<object[]>(mapped.Rows.Count);
        foreach (var row in mapped.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var values = new object[headers.Length]; values[0] = row.Code;
            for (int i = 0; i < width; i++) values[i + 1] = i < row.Values.Length ? (double)row.Values[i] : 0d;
            var e = establishments[row.EstablishmentId];
            values[width + 1] = EstablishmentLabel(e);
            values[width + 2] = MonthLabel(row.Month); values[width + 3] = e.Sector;
            arrays.Add(values);
        }
        if (arrays.Count + 1 > ExcelPackage.MaxRows) throw new InvalidOperationException("Los datos superan el límite de filas de Excel.");
        progress.Report($"Escribiendo {arrays.Count:N0} filas compactas ({mapped.OmittedZeroRows:N0} filas cero omitidas)...");
        data.Cells[2, 1].LoadFromArrays(arrays);
        data.Column(1).Style.Numberformat.Format = "@";
        var table = data.Tables.Add(data.Cells[1, 1, arrays.Count + 1, headers.Length], "DatosConsolidado");
        table.TableStyle = TableStyles.Medium2;
        data.Hidden = eWorkSheetHidden.Hidden;
        var sums = mapped.Rows.GroupBy(r => r.Code).OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => Enumerable.Range(0, width).Select(i => g.Sum(r => i < r.Values.Length ? r.Values[i] : 0m)).ToArray());
        var lastRow = sums.Count + 9;
        filter.Cells.Clear();
        filter.View.ShowGridLines = false;
        filter.Cells[1, 1].Value = $"Consolidado REM Serie A {mapped.Source.Year}";
        filter.Cells[1, 1].Style.Font.Bold = true;
        filter.Cells[2, 1].Value = $"Datos al {mapped.Source.CapturedAt:yyyy-MM-dd HH:mm} UTC. Versión: {mapped.Source.Version.Name}";
        filter.Cells[3, 1].Value = "Seleccione establecimiento, mes y sector. Consulte COBERTURA para interpretar la ausencia de datos.";
        // Controls are above the pivot; reserve enough height without moving its A9 contract.
        for (int row = 4; row <= 7; row++) filter.Row(row).Height = 46;
        // Row 8 stores the one-based data ordinal used by the legacy
        // formulas; VLOOKUP adds one for the Codigo column.
        for (int i = 1; i <= width; i++) filter.Cells[8, i + 1].Value = i;
        filter.Row(8).Hidden = true;
        filter.Column(1).Width = 18;
        for (int i = 2; i <= width + 1; i++) filter.Column(i).Width = 13;
        var pivot = filter.PivotTables.Add(filter.Cells[9, 1, lastRow, width + 1], table.Range, "ConsolidadoREM");
        pivot.DataOnRows = false; pivot.RowGrandTotals = false; pivot.ColumnGrandTotals = false;
        pivot.Compact = false; pivot.CompactData = false; pivot.Outline = false;
        var rowField = pivot.RowFields.Add(pivot.Fields["Codigo"]);
        rowField.Sort = eSortType.Ascending; rowField.SubTotalFunctions = eSubTotalFunctions.None;
        for (int i = 1; i <= width; i++)
        {
            var field = pivot.DataFields.Add(pivot.Fields[$"Valor{i:00}"]);
            field.Function = DataFieldFunctions.Sum; field.Name = $"Suma de valor.{i}"; field.Format = "#,##0.########";
        }
        pivot.CacheDefinition.Refresh();
        pivot.CacheDefinition.SaveData = true;
        foreach (var (name, column) in new[] { ("Establecimiento", 0), ("Mes", 4), ("Sector", 8) })
        {
            var slicer = pivot.Fields[name].AddSlicer();
            slicer.SetPosition(3, 0, column, 0); slicer.SetSize(name == "Establecimiento" ? 350 : 250, 240);
        }
        pivot.Calculate(true);
        filter.Cells[9, 1].Value = "Código";
        for (int i = 1; i <= width; i++) filter.Cells[9, i + 1].Value = $"Suma de valor.{i}";
        int outputRow = 10;
        foreach (var (code, values) in sums)
        {
            ct.ThrowIfCancellationRequested();
            filter.Cells[outputRow, 1].Value = code;
            for (int i = 0; i < width; i++) filter.Cells[outputRow, i + 2].Value = (double)values[i];
            outputRow++;
        }
        // Materialize a consistent initial grid. Excel owns subsequent slicer recalculation.
        CompletePivotLayout(pivot, sums.Keys.ToArray(), width, lastRow);
        filter.Cells[9, 1, 9, width + 1].Style.Font.Bold = true;
        filter.Cells[10, 2, lastRow, width + 1].Style.Numberformat.Format = "#,##0.########";
        filter.View.FreezePanes(10, 2);
        foreach (var binding in bindings.Concat(computedBindings).Where(b => b.RewriteFormula))
        {
            var cell = workbook.Worksheets[binding.Sheet].Cells[binding.Address];
            cell.Value = null;
            cell.Formula = $"IFERROR(VLOOKUP($A{binding.LookupRow},FILTRO!$A$10:${ExcelCellAddress.GetColumnLetter(width + 1)}${lastRow},{binding.Index + 1},FALSE),0)";
        }
        AddCoverage(workbook, mapped, warnings);
        progress.Report($"Calculando {bindings.Count:N0} BUSCARV y fórmulas de la plantilla...");
        ct.ThrowIfCancellationRequested();
        workbook.Calculate();
        ct.ThrowIfCancellationRequested();
        foreach (var binding in bindings.Concat(computedBindings))
        {
            var targetCell = workbook.Worksheets[binding.Sheet].Cells[binding.Address];
            targetCell.Calculate();
            var actual = targetCell.Value;
            var expected = sums.TryGetValue(binding.Code, out var value) && binding.Index <= value.Length ? value[binding.Index - 1] : 0m;
            if (actual is ExcelErrorValue || actual == null || Math.Abs(Convert.ToDecimal(actual) - expected) > 0.0000001m)
                throw new InvalidOperationException($"No concilia {binding.Sheet}!{binding.Address} (código {binding.Code}, columna {binding.Index}): esperado {expected}, obtenido {actual}. Fórmula: {targetCell.Formula}");
        }
        var newErrors = workbook.Worksheets.Where(s => s.Name != "DATOS" && s.Dimension != null)
            .SelectMany(ws => ws.Cells[ws.Dimension.Address].Where(c => c.Value is ExcelErrorValue)
                .Select(c => $"{ws.Name}!{c.Address}")).Where(a => !inheritedErrors.Contains(a)).Take(15).ToArray();
        if (newErrors.Length > 0) throw new InvalidOperationException("Errores nuevos de cálculo: " + string.Join(", ", newErrors));
        workbook.CalcMode = ExcelCalcMode.Automatic;
        workbook.FullCalcOnLoad = true;
        workbook.Worksheets.MoveToStart("FILTRO");
        workbook.View.ActiveTab = 0;
        progress.Report("Guardando y verificando el archivo...");
        package.SaveAs(new FileInfo(destination));
        ConsolidadoPivotCacheWriter.Complete(destination, arrays, mapped.Source.CapturedAt, ct);
        ConsolidadoValidator.Validate(destination, mapped.Rows.Count, bindings.Count);
        return warnings;
    }

    private static void CompletePivotLayout(ExcelPivotTable pivot, string[] codes, int width, int lastRow)
    {
        var xml = pivot.PivotTableXml;
        var ns = new XmlNamespaceManager(xml.NameTable); ns.AddNamespace("s", ConsolidadoTemplate.S.NamespaceName);
        var location = (XmlElement)xml.SelectSingleNode("/s:pivotTableDefinition/s:location", ns)!;
        location.SetAttribute("ref", $"A9:{ExcelCellAddress.GetColumnLetter(width + 1)}{lastRow}");
        location.SetAttribute("firstHeaderRow", "0"); location.SetAttribute("firstDataRow", "1"); location.SetAttribute("firstDataCol", "1");
        // Field items refer to shared-cache indices, while rowItems refer to field-item indices.
        var shared = pivot.RowFields[0].Cache.SharedItems
            .Select((value, i) => (Code: Convert.ToString(value, CultureInfo.InvariantCulture)!, Index: i))
            .ToDictionary(x => x.Code, x => x.Index);
        XmlElement Element(string name) => xml.CreateElement(name, ConsolidadoTemplate.S.NamespaceName);
        var fieldNode = (XmlElement)xml.SelectSingleNode("/s:pivotTableDefinition/s:pivotFields/s:pivotField[1]", ns)!;
        var items = fieldNode["items"] ?? (XmlElement)fieldNode.AppendChild(Element("items"))!;
        items.RemoveAll(); items.SetAttribute("count", codes.Length.ToString());
        foreach (var code in codes) { var item = Element("item"); item.SetAttribute("x", shared[code].ToString()); items.AppendChild(item); }
        foreach (var kind in new[] { "rowItems", "colItems" }) xml.SelectSingleNode($"/s:pivotTableDefinition/s:{kind}", ns)?.ParentNode?.RemoveChild(xml.SelectSingleNode($"/s:pivotTableDefinition/s:{kind}", ns)!);
        var rows = Element("rowItems"); rows.SetAttribute("count", codes.Length.ToString());
        for (int i = 0; i < codes.Length; i++) { var item = Element("i"); var x = Element("x"); x.SetAttribute("v", i.ToString()); item.AppendChild(x); rows.AppendChild(item); }
        xml.DocumentElement!.InsertAfter(rows, xml.SelectSingleNode("/s:pivotTableDefinition/s:rowFields", ns));
        var cols = Element("colItems"); cols.SetAttribute("count", width.ToString());
        for (int i = 0; i < width; i++) { var item = Element("i"); item.SetAttribute("i", i.ToString()); var x = Element("x"); x.SetAttribute("v", i.ToString()); item.AppendChild(x); cols.AppendChild(item); }
        xml.DocumentElement.InsertAfter(cols, xml.SelectSingleNode("/s:pivotTableDefinition/s:colFields", ns));
    }

    private static void AddCoverage(ExcelWorkbook workbook, MappedConsolidado data, List<string> warnings)
    {
        var sheet = workbook.Worksheets.Add("COBERTURA");
        sheet.Cells[1, 1].Value = $"Cobertura disponible de Serie A {data.Source.Year}";
        sheet.Cells[2, 1].Value = "Sin evidencia en la base no significa sin entrega. Los archivos completamente en cero pueden no estar registrados.";
        sheet.Cells[3, 1].Value = "Los segmentadores de FILTRO muestran actividad. Esta hoja incluye todos los establecimientos y meses del año.";
        sheet.Cells[4, 1].Value = warnings.Count == 0 ? "" : string.Join(" | ", warnings);
        var reportGroups = data.Source.Reports.ToLookup(r => (r.EstablishmentId, r.Month));
        var activity = data.Rows.Select(r => (r.EstablishmentId, r.Month)).ToHashSet();
        sheet.Cells[6, 1].LoadFromArrays(new[] { new object[] { "Código DEIS", "Establecimiento", "Sector", "Mes", "Evidencia", "Reportes" } });
        var rows = data.Source.Establishments.SelectMany(e => Enumerable.Range(1, 12).Select(month => new object[] {
            e.Code, e.Name, e.Sector, MonthLabel(month),
            activity.Contains((e.Id, month)) ? "Con actividad registrada" : reportGroups[(e.Id, month)].Any() ? "Reporte con valores cero registrados" : "Sin evidencia en la base",
            string.Join(", ", reportGroups[(e.Id, month)].Select(r => r.Id)) }));
        sheet.Cells[7, 1].LoadFromArrays(rows);
        sheet.Cells[1, 1].Style.Font.Bold = true;
        sheet.Cells[6, 1, 6, 6].Style.Font.Bold = true;
        sheet.Column(1).Width = 16; sheet.Column(2).Width = 45; sheet.Column(3).Width = 24;
        sheet.Column(4).Width = 18; sheet.Column(5).Width = 40; sheet.Column(6).Width = 22;
        for (int i = 2; i <= 4; i++) { sheet.Cells[i, 1, i, 6].Merge = true; sheet.Cells[i, 1].Style.WrapText = true; sheet.Row(i).Height = i == 4 ? 45 : 32; }
        sheet.View.FreezePanes(7, 2); sheet.View.ShowGridLines = false;
    }

    public static string MonthLabel(int month) => $"{month:00} {Spanish.DateTimeFormat.GetMonthName(month)}";
    private static string EstablishmentLabel(EstablishmentInfo e) => $"{e.Name} ({(string.IsNullOrWhiteSpace(e.Code) ? e.Id.ToString() : e.Code)})";
}
