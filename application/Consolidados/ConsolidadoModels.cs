namespace RemTools.Consolidados;

public sealed record GenerationRequest(int Year, int VersionId, string TemplatePath, string OutputDirectory);
public sealed record VersionOption(int Id, string Name, DateOnly Date)
{
    public override string ToString() => $"{Name} ({Date:yyyy-MM-dd})";
}
public sealed record EstablishmentInfo(long Id, string Name, string Code, string Sector);
public sealed record ReportInfo(int Id, long EstablishmentId, long ComunaId, int Month);
public sealed record ColumnDefinition(string Code, string Address, bool IsTotal = false);
public sealed record PrestacionDefinition(int Id, int VersionId, string Code, string Name,
    string Sheet, long? SectionId, List<string> LegacyCoordinates)
{
    public List<ColumnDefinition> Columns { get; set; } = [];
    public string StructureSignature { get; set; } = "";
}
public sealed record SourceRecord(int ReportId, int PrestacionId, decimal[] Values);
public sealed record ActivityRow(string Code, long EstablishmentId, int Month, decimal[] Values);
public sealed record ConsolidadoSnapshot(int Year, VersionOption Version, DateTimeOffset CapturedAt,
    List<EstablishmentInfo> Establishments, List<ReportInfo> Reports,
    List<PrestacionDefinition> Definitions, List<SourceRecord> Records);
public sealed record MappedConsolidado(ConsolidadoSnapshot Source, List<ActivityRow> Rows,
    Dictionary<string, PrestacionDefinition> Target, int OmittedZeroRows);
public sealed record GenerationResult(string FilePath, int RowCount, long Bytes, TimeSpan Elapsed, List<string> Warnings);
public sealed record CatalogEntry(string Series, int Year, string File, DateTimeOffset GeneratedAt,
    int[] Months, string TemplateVersion, long Bytes, string Sha256, string[] Warnings);
public sealed record DownloadCatalog(int SchemaVersion, List<CatalogEntry> Files);
