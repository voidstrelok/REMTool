namespace RemTool.Shared.Ingest;

/// <summary>Datos de un año que se reemplazan completos. Filter usa el alias x y {0} = año; vive en código, nunca viene del batch.</summary>
public sealed record YearSlice(string Table, string Filter);

/// <summary>
/// Qué publica el admin local a producción. Prod no escribe en estas tablas: local es la fuente de verdad
/// y prod queda como espejo con los mismos IDs (registro → prestacion/reporte, resultado → indicador, etc.).
/// </summary>
public static class IngestTables
{
    /// <summary>Estructura REM: pesada, sólo se envía con el botón de estructura.</summary>
    public static readonly string[] Structure =
    [
        "serie_rem", "version_rem", "hoja_rem", "version_hoja_rem",
        "seccion_rem", "fila_seccion_rem", "celda_seccion_rem",
        "prestacion", "coordenada_prestacion",
        "comuna", "sector", "establecimiento"
    ];

    /// <summary>Todo lo que se edita en el panel admin: liviano, va en cada publicación.</summary>
    public static readonly string[] Config =
    [
        "tipo_indicador", "tipo_regla", "regla", "parametros", "punto_resumen", "informe_rem",
        "indicador", "convenio", "indicador_convenio", "filtro_establecimiento",
        "informativo", "usuario_interno", "resultado_indicador"
    ];

    /// <summary>Tablas completas en orden padre → hijo (upsert en este orden, borrado en orden inverso).</summary>
    public static readonly string[] FullTables = [.. Structure, .. Config];

    public static readonly YearSlice Reporte = new("reporte", "x.\"año\" = {0}");
    public static readonly YearSlice Registro = new("registro", "x.id_reporte IN (SELECT id FROM \"REMTool\".reporte WHERE \"año\" = {0})");
    public static readonly YearSlice Percapita = new("PERCAPITA_SSC", "x.\"año_corte\" = {0}");
    public static readonly YearSlice Fonasa = new("fonasa", "extract(year FROM x.fecha_corte) = {0}");

    /// <summary>Datos por año, en orden de inserción (borrado en orden inverso).</summary>
    public static readonly YearSlice[] Slices = [Reporte, Registro, Percapita, Fonasa];
}
