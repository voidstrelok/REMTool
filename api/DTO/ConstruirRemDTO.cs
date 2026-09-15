using RemTool;

namespace RemTool.DTO;

public sealed class ParteConstruirRemDTO
{
    public string Id { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string NombreArchivo { get; init; } = string.Empty;
    public DateTime FechaSubida { get; init; }
    public string Serie { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public List<SeccionConstruirRemDTO> Secciones { get; init; } = [];
    public List<PrestacionDatosDTO> Datos { get; init; } = [];
}

public sealed class SeccionConstruirRemDTO
{
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Hoja { get; init; } = string.Empty;
    public string HojaCodigo { get; init; } = string.Empty;
    public int HojaOrden { get; init; }
    public int Orden { get; init; }
    public List<PrestacionSeccionDTO> Prestaciones { get; init; } = [];
}

public sealed class PrestacionSeccionDTO
{
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public int Orden { get; init; }
    public int CantidadValores { get; init; }
}

public sealed class ConstruirRemRequest
{
    public string CodDeis { get; init; } = string.Empty;
    public int Mes { get; init; }
    public string Version { get; init; } = string.Empty;
    public List<ParteConstruirRemPayload> Partes { get; init; } = [];
}

public sealed class ParteConstruirRemPayload
{
    public string Id { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public List<PrestacionDatosDTO> Datos { get; init; } = [];
}

public sealed class ConstruirRemRevisionDTO
{
    public AnalisisRemDTO Analisis { get; init; } = new();
    public List<SeccionConstruirRemVistaDTO> Secciones { get; init; } = [];
}

public sealed class SeccionConstruirRemVistaDTO
{
    public long Id { get; init; }
    public string Hoja { get; init; } = string.Empty;
    public string HojaCodigo { get; init; } = string.Empty;
    public int HojaOrden { get; init; }
    public string Seccion { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public int Orden { get; init; }
    public string Html { get; init; } = string.Empty;
    public Dictionary<string, string> Valores { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
