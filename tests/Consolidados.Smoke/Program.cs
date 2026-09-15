using System.Diagnostics;
using System.Xml.Linq;
using OfficeOpenXml;
using RemTools.Consolidados;

try
{
ExcelPackage.License.SetNonCommercialOrganization("DESAM Monte Patria");
var root = Path.GetFullPath(Path.Combine("tmp", "consolidados-test", Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(root);
var progress = new Progress<string>(Console.WriteLine);
int passed = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); passed++; }
void Reject(Action action, string contains)
{
    try { action(); } catch (InvalidOperationException ex) when (ex.Message.Contains(contains)) { passed++; return; }
    throw new Exception("Expected rejection: " + contains);
}
var version = new VersionOption(1, "Versión prueba", new DateOnly(2026, 1, 1));
var definitions = new List<PrestacionDefinition>();
for (var row = 11; row <= 13; row++)
{
    var code = row == 11 ? "001" : row == 12 ? "002" : "003";
    definitions.Add(new PrestacionDefinition(row - 10, 1, code, "Prestación " + code, "A01", 1, []) {
        Columns = Enumerable.Range(0, 48).Select(i => new ColumnDefinition($"COL{i + 1:00}", new ExcelCellAddress(row, i + 3).Address, i == 0)).ToList(),
        StructureSignature = "same-section"
    });
}
var templatePath = Path.Combine(root, "plantilla.xlsx");
using (var template = new ExcelPackage())
{
    var f = template.Workbook.Worksheets.Add("FILTRO");
    for (int i = 1; i <= 48; i++) f.Cells[8, i + 1].Value = i;
    var a = template.Workbook.Worksheets.Add("A01");
    foreach (var definition in definitions)
    {
        int row = definition.Id + 10;
        a.Cells[row, 1].Value = definition.Code;
        a.Cells[row, 2].Value = definition.Name;
        a.Cells[row, 3].Formula = $"SUM(D{row}:AX{row})";
        for (int i = 2; i <= 48; i++) a.Cells[row, i + 2].Formula =
            $"IFERROR(VLOOKUP($A{row},FILTRO!$A$9:$AW$9999,FILTRO!{ExcelCellAddress.GetColumnLetter(i + 1)}$8+1,FALSE),0)";
    }
    template.SaveAs(templatePath);
}
decimal[] Values(int n)
{
    var values = new decimal[48]; values[1] = n; values[45] = 6; values[46] = 7; values[47] = 8;
    values[0] = values.Skip(1).Sum(); return values;
}
var snapshot = new ConsolidadoSnapshot(2026, version, DateTimeOffset.UtcNow,
    [new(1, "Uno", "000001", "Norte"), new(2, "Dos", "000002", "Sur"), new(3, "Sin reporte", "000003", "Sur")],
    [new(10, 1, 1, 1), new(11, 2, 1, 2), new(12, 1, 1, 2)], definitions,
    [new(10, 1, Values(5)), new(11, 1, Values(3)), new(10, 2, Values(4)), new(12, 3, new decimal[48])]);
var legacyTemplate = args.SkipWhile(a => a != "--legacy").Skip(1).FirstOrDefault();
if (legacyTemplate != null)
{
    var legacyOutput = Path.Combine(root, "legacy.xlsx");
    var augustSnapshot = snapshot with { Reports = [new(10, 1, 1, 8), new(11, 2, 1, 8), new(12, 1, 1, 8)] };
    var count = new ConsolidadoLegacyPivotWriter().Write(legacyTemplate, legacyOutput, augustSnapshot, progress, default);
    Check(count == snapshot.Records.Count, "Legacy cache receives every record");
    using var archive = System.IO.Compression.ZipFile.OpenRead(legacyOutput);
    var entry = archive.Entries.Single(e => e.FullName.StartsWith("xl/pivotCache/pivotCacheRecords"));
    using var stream = entry.Open(); using var reader = new StreamReader(stream); var xml = reader.ReadToEnd();
    Check(xml.Contains("count=\"4\""), "Legacy cache record count persisted");
    var ns = (XNamespace)"http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    var cacheDefinition = XDocument.Parse(new StreamReader(archive.Entries.Single(e => e.FullName == "xl/pivotCache/pivotCacheDefinition1.xml").Open()).ReadToEnd());
    var augustIndex = cacheDefinition.Descendants(ns + "cacheField").Single(f => (string?)f.Attribute("name") == "NMes").Element(ns + "sharedItems")!.Elements().ToList().FindIndex(i => (string?)i.Attribute("v") == "agosto");
    Check(augustIndex >= 0, "August is added to the month cache");
    var pivot = XDocument.Parse(new StreamReader(archive.Entries.Single(e => e.FullName == "xl/pivotTables/pivotTable1.xml").Open()).ReadToEnd());
    Check(pivot.Descendants(ns + "pivotFields").Elements(ns + "pivotField").ElementAt(57).Element(ns + "items")!.Elements(ns + "item").Any(i => (int?)i.Attribute("x") == augustIndex), "August is available in the pivot month filter");
    using var originalArchive = System.IO.Compression.ZipFile.OpenRead(legacyTemplate);
    var originalSheet = originalArchive.Entries.Single(e => e.FullName == "xl/worksheets/sheet4.xml");
    var generatedSheet = archive.Entries.Single(e => e.FullName == "xl/worksheets/sheet4.xml");
    using var originalStream = originalSheet.Open(); using var generatedStream = generatedSheet.Open();
    Check(System.Security.Cryptography.SHA256.HashData(originalStream).SequenceEqual(System.Security.Cryptography.SHA256.HashData(generatedStream)), "Workbook formulas and sheets remain untouched");
    Console.WriteLine("Legacy workbook: " + legacyOutput);
    return;
}
Check(ConsolidadoDataReader.SelectLatestReports([new(1, 1, 1, 1), new(2, 1, 1, 1), new(3, 2, 1, 1)]).Select(r => r.Id).SequenceEqual(new[] { 2, 3 }), "Latest report per establishment");
var mapped = ConsolidadoMapper.Map(snapshot, CancellationToken.None);
Check(mapped.Rows.Count == 3 && mapped.OmittedZeroRows == 1, "Sparse rows retain zero report evidence");
Reject(() => ConsolidadoMapper.Map(snapshot with { Records = [.. snapshot.Records, snapshot.Records[0]] }, default), "duplicada");
Reject(() => ConsolidadoMapper.Map(snapshot with { Records = [new(10, 1, new decimal[47])] }, default), "47 valores");
Reject(() => ConsolidadoMapper.Map(snapshot with { Records = [new(10, 1, new decimal[48])] }, default), "No hay actividad");
var incompatible = definitions[0] with { Id = 99, VersionId = 2, StructureSignature = "changed-section" };
Reject(() => ConsolidadoMapper.Map(snapshot with { Definitions = [.. definitions, incompatible], Records = [new(10, 99, Values(2))] }, default), "incompatible");
var compatible = incompatible with { StructureSignature = "same-section" };
Check(ConsolidadoMapper.Map(snapshot with { Definitions = [.. definitions, compatible], Records = [new(10, 99, Values(2))] }, default).Rows.Count == 1, "Verified identical section across versions");
if (args.Length == 0)
{
    Console.WriteLine($"PASS: {passed} mapping checks. Use --legacy <template.xlsx> to verify pivot-cache replacement.");
    return;
}
var request = new GenerationRequest(2026, 1, templatePath, Path.Combine(root, "publicacion"));
if (args.Contains("--debug-write"))
{
    var debugPath = Path.Combine(root, "debug.xlsx");
    try { new ConsolidadoExcelWriter().Write(templatePath, debugPath, mapped, progress, default); }
    finally { Console.WriteLine("DEBUG FILE: " + debugPath); }
    return;
}
var result = ConsolidadoGenerationService.GenerateSnapshot(request, snapshot, new(1, []), progress, Stopwatch.StartNew(), default);
Check(File.Exists(result.FilePath), "Workbook produced");
var catalog = ConsolidadoCatalogWriter.Read(request.OutputDirectory);
Check(catalog.Files.Count == 1 && catalog.Files[0].Months.SequenceEqual(new[] { 1, 2 }), "Catalog references complete workbook");
Console.WriteLine("Workbook: " + result.FilePath);
using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(result.FilePath, false))
{
    var errors = new DocumentFormat.OpenXml.Validation.OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Microsoft365).Validate(document).Take(15).ToArray();
    foreach (var error in errors) Console.WriteLine($"OpenXml {error.Part?.Uri} {error.Path?.XPath}: {error.Description}");
    Check(errors.Length == 0, "Workbook has valid OpenXML");
}
using (var generated = new ExcelPackage(new FileInfo(result.FilePath)))
{
    var a = generated.Workbook.Worksheets["A01"];
    Check(Convert.ToDecimal(a.Cells["D11"].Value) == 8, "Initial VLOOKUP values");
    Check(Convert.ToDecimal(a.Cells["AX11"].Value) == 16, "Column 48 is numeric and summed");
    Check(Convert.ToDecimal(a.Cells["C11"].Value) == 50, "Calculated row total reconciles");
    Check(Convert.ToDecimal(a.Cells["D13"].Value) == 0 && a.Cells["A13"].Text == "003", "Zero-only code retains layout and leading zeros");
    var filter = generated.Workbook.Worksheets["FILTRO"];
    var pivot = filter.PivotTables[0];
    Check(filter.Drawings.Count == 3 && pivot.CacheDefinition.SourceRange.Worksheet.Name == "DATOS", "Three slicers with internal data source");
    pivot.Calculate(true);
    Check(Convert.ToDecimal(pivot.CalculatedData.SelectField("Codigo", "001").GetValue("Suma de valor.2")) == 8, "Saved pivot recomputes same sum independently");
    Check(generated.Workbook.Worksheets["COBERTURA"].Dimension.Rows >= 42, "Coverage includes all establishments and months");
}
var before = File.ReadAllBytes(Path.Combine(request.OutputDirectory, "catalogo.json"));
using (var cancellation = new CancellationTokenSource())
{
    cancellation.Cancel();
    try { ConsolidadoGenerationService.GenerateSnapshot(request, snapshot, catalog, progress, Stopwatch.StartNew(), cancellation.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { passed++; }
}
Check(before.SequenceEqual(File.ReadAllBytes(Path.Combine(request.OutputDirectory, "catalogo.json"))), "Cancellation preserves catalog");
Reject(() => ConsolidadoGenerationService.GenerateSnapshot(request, snapshot with { Records = [new(10, 1, new decimal[48])] }, catalog, progress, Stopwatch.StartNew(), default), "No hay actividad");
Check(before.SequenceEqual(File.ReadAllBytes(Path.Combine(request.OutputDirectory, "catalogo.json"))), "Empty activity preserves catalog");
if (args.Length > 0)
{
    using var clean = ConsolidadoTemplate.Prepare(args[0], default);
    using var original = new ExcelPackage(clean);
    Check(original.Workbook.Worksheets.Count >= 28 && original.Workbook.Worksheets["FILTRO"].PivotTables.Count == 0, "Real template stripped without old cache");
    Check(original.Workbook.Worksheets["A01"].Cells["E11"].Formula.Contains("VLOOKUP"), "Real REM formulas preserved");
    Console.WriteLine($"Real template prepared: {clean.Length:N0} bytes; sheets {original.Workbook.Worksheets.Count}");
}
Console.WriteLine($"PASS: {passed} checks. Test files: {root}");
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
