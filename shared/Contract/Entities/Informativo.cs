using RemTool.Shared.Enum;

namespace RemTool.Shared;

public class Informativo
{
    public int Id { get; set; }

    public TipoInformativo Tipo { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Contenido { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? TextoEnlace { get; set; }

    public DateOnly FechaPublicacion { get; set; }

    public bool Vigente { get; set; }

    public bool Destacado { get; set; }
}
