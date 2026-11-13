namespace RemTool.Shared;

/// <summary>Conjunto predefinido de secciones REM para Consultar REM (por ejemplo, ECICEP).</summary>
public class InformeRem
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public long IdSerieRem { get; set; }

    // ponytail: ids de seccion_rem guardados como bigint[]; las secciones son por versión,
    // así que una versión REM nueva obliga a reasignarlas. Migrar a (hoja, código) si eso molesta.
    public List<long> SeccionIds { get; set; } = [];

    public virtual SerieRem SerieRem { get; set; } = null!;
}
