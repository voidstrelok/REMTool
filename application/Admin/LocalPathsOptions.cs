namespace RemTool.Application.Admin;

/// <summary>Filesystem boundaries for the local administrator application.</summary>
public sealed class LocalPathsOptions
{
    public string? PublicationsDirectory { get; init; }
    public string? WorkingDirectory { get; init; }
}
