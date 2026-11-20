namespace RemTool.Shared.Ingest;

/// <summary>Datos de un año que se reemplazan completos. Filter usa el alias x y {0} = año; vive en código, nunca viene del batch.</summary>
public sealed record YearSlice(string Table, string Filter);

/// <summary>
/// Tablas que el admin local publica a producción. Los IDs de estructura deben coincidir entre local y prod
/// (registro/resultado_indicador referencian estas tablas por id), así que local es la fuente de verdad.
/// Orden: padres antes que hijos (upsert en este orden, borrado en orden inverso).
/// </summary>
public static class IngestTables
{
    public static readonly string[] Structure =
    [
        "serie_rem", "version_rem", "hoja_rem", "version_hoja_rem",
        "seccion_rem", "fila_seccion_rem", "celda_seccion_rem",
        "prestacion", "coordenada_prestacion",
        "comuna", "sector", "establecimiento",
        "tipo_indicador", "tipo_regla", "regla", "parametros", "punto_resumen", "informe_rem",
        "indicador", "convenio", "indicador_convenio", "filtro_establecimiento"
    ];

    public static readonly YearSlice Results = new("resultado_indicador", "x.id_indicador IN (SELECT id FROM \"REMTool\".indicador WHERE \"año\" = {0})");
    public static readonly YearSlice Percapita = new("PERCAPITA_SSC", "x.\"año_corte\" = {0}");
    public static readonly YearSlice Fonasa = new("fonasa", "extract(year FROM x.fecha_corte) = {0}");

    public static readonly YearSlice[] Slices = [Results, Percapita, Fonasa];
}
