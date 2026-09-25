namespace RemTool.Application.Admin;

public sealed class LocalPathPolicy(LocalPathsOptions options)
{
    public string PublicationsDirectory => Required(options.PublicationsDirectory, "Paths:PublicationsDirectory");
    public string WorkingDirectory => Required(options.WorkingDirectory, "Paths:WorkingDirectory");

    public string CreateWorkingFile(string extension)
    {
        Directory.CreateDirectory(WorkingDirectory);
        return Path.Combine(WorkingDirectory, $"{Guid.NewGuid():N}{extension}");
    }

    private static string Required(string? path, string setting)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"Debe configurar {setting} en la configuración local.");
        return Path.GetFullPath(path);
    }
}
