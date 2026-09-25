using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using OfficeOpenXml;

namespace RemTools;

internal sealed class RemStructureDefinition
{
    public string Version { get; init; } = string.Empty;
    public string Serie { get; init; } = string.Empty;
    public List<RemSectionDefinition> Sections { get; } = new();
    public Dictionary<string, string> TitulosHojas { get; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class RemSectionDefinition
{
    public string Hoja { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string RangoDiccionario { get; init; } = string.Empty;
    public string RangoBase { get; init; } = string.Empty;
    public int Orden { get; init; }
    public int FilaInicio { get; init; }
    public int ColumnaInicio { get; init; }
    public int FilaFin { get; init; }
    public int ColumnaFin { get; init; }
    public int OffsetPrestacion { get; init; }
    public int OffsetColumna { get; init; }
    public List<RemRowDefinition> Filas { get; } = new();
}

internal sealed class RemRowDefinition
{
    public int Orden { get; init; }
    public int FilaOrigen { get; init; }
    public string TipoFila { get; set; } = string.Empty;
    public string? CodigoPrestacion { get; set; }
    public string NombrePrestacion { get; set; } = string.Empty;
    public List<RemCellDefinition> Celdas { get; } = new();
    public List<RemCoordinateDefinition> Coordenadas { get; } = new();
}

internal sealed class RemCellDefinition
{
    public int FilaOrigen { get; init; }
    public int ColumnaOrigen { get; init; }
    public string Valor { get; init; } = string.Empty;
    public string CeldaDiccionario { get; init; } = string.Empty;
    public string CeldaBase { get; init; } = string.Empty;
    public int RowSpan { get; init; } = 1;
    public int ColSpan { get; init; } = 1;
    public bool EsCeldaAncla { get; init; }
    public bool EsEditable { get; init; }
    public bool EsEntradaPrestacion { get; init; }
    public bool EsTotal { get; init; }
    public string TipoTotal { get; init; } = string.Empty;
    public string FormulaOrigen { get; init; } = string.Empty;
    public string DependenciasTotal { get; init; } = string.Empty;
    public string OperacionTotal { get; init; } = string.Empty;
    public int EstiloOrigen { get; init; }
    public string ColorFondo { get; init; } = string.Empty;
}

internal sealed class RemCoordinateDefinition
{
    public int Orden { get; init; }
    public string CodigoColumna { get; init; } = string.Empty;
    public string CeldaDiccionario { get; init; } = string.Empty;
    public string CeldaBase { get; init; } = string.Empty;
    public RemCellDefinition Celda { get; init; } = null!;
}

internal static class RemStructureImporter
{
    private static readonly Regex CodigoPrestacionRegex = new("^[A-Za-z0-9]{8,12}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex CodigoColumnaRegex = new("^COL\\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ReferenciaCeldaRegex = new(
        @"(?<![A-Z0-9_])\$?(?<col>[A-Z]{1,3})\$?(?<row>\d+)(?:\s*:\s*\$?(?<endCol>[A-Z]{1,3})\$?(?<endRow>\d+))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static RemStructureDefinition Build(
        ExcelPackage workbookBase,
        ExcelPackage workbookDiccionario,
        JsonNode parametrosJson,
        string version,
        string serie)
    {
        var versionNode = parametrosJson[version] as JsonObject
            ?? throw new InvalidOperationException($"No se encontró la versión '{version}' en el archivo de parámetros.");

        var tablas = versionNode["Tablas"] as JsonArray
            ?? throw new InvalidOperationException("El archivo de parámetros no contiene la colección 'Tablas'.");

        var result = new RemStructureDefinition { Version = version, Serie = serie };
        var orden = 0;

        foreach (var tablaNode in tablas)
        {
            if (tablaNode is not JsonObject tabla)
                continue;

            var hojaNombre = tabla["hoja"]?.GetValue<string>()?.Trim() ?? string.Empty;
            var rango = tabla["rango"]?.GetValue<string>()?.Trim() ?? string.Empty;
            var offsetPrestacion = tabla["offsetPrest"]?.GetValue<int>() ?? 0;
            var offsetColumna = tabla["offsetCol"]?.GetValue<int>() ?? 0;

            if (string.IsNullOrWhiteSpace(hojaNombre) || string.IsNullOrWhiteSpace(rango))
                throw new InvalidOperationException("Existe una tabla sin hoja o rango en el archivo de parámetros.");

            var hojaDiccionario = workbookDiccionario.Workbook.Worksheets[hojaNombre]
                ?? throw new InvalidOperationException($"No se encontró la hoja '{hojaNombre}' en el diccionario.");
            var hojaBase = workbookBase.Workbook.Worksheets[hojaNombre]
                ?? throw new InvalidOperationException($"No se encontró la hoja '{hojaNombre}' en la plantilla base.");

            var rangoDiccionario = hojaDiccionario.Cells[rango];
            var rangoBase = ConstruirRangoBase(hojaBase, rangoDiccionario);
            if (string.IsNullOrWhiteSpace(rangoBase))
                throw new InvalidOperationException($"No se pudo construir el rango base para la tabla '{hojaNombre}:{rango}'.");

            var rangoBaseExcel = hojaBase.Cells[rangoBase];
            var merges = ConstruirMapaCombinaciones(hojaBase, rangoBaseExcel);
            var tituloHoja = TextoCelda(hojaBase.Cells[6, 1]);
            if (!string.IsNullOrWhiteSpace(tituloHoja))
                result.TitulosHojas[hojaNombre] = tituloHoja;
            var tituloDiccionario = ExtraerTitulo(hojaDiccionario, rangoDiccionario);
            var tituloBase = ExtraerTitulo(hojaBase, rangoBaseExcel);
            var titulo = string.IsNullOrWhiteSpace(tituloBase) ? tituloDiccionario : tituloBase;

            var section = new RemSectionDefinition
            {
                Hoja = hojaNombre,
                Codigo = $"{hojaNombre}:{rango}",
                Titulo = titulo,
                RangoDiccionario = rango,
                RangoBase = rangoBase,
                Orden = ++orden,
                FilaInicio = rangoBaseExcel.Start.Row,
                ColumnaInicio = rangoBaseExcel.Start.Column,
                FilaFin = rangoBaseExcel.End.Row,
                ColumnaFin = rangoBaseExcel.End.Column,
                OffsetPrestacion = offsetPrestacion,
                OffsetColumna = offsetColumna
            };

            // Los offsets se expresan sobre el rango del diccionario:
            // - offsetPrestacion: cantidad de filas previas a la primera prestación.
            // - offsetColumna: cantidad de columnas previas a la primera Col01.
            var primeraFilaPrestacion = rangoDiccionario.Start.Row + offsetPrestacion;
            var primeraColumnaPrestacion = rangoDiccionario.Start.Column + offsetColumna;

            // La base define las filas y celdas que se almacenan. El diccionario
            // sólo aporta la identificación de prestaciones y sus columnas.
            for (var fila = rangoBaseExcel.Start.Row; fila <= rangoBaseExcel.End.Row; fila++)
            {
                var row = new RemRowDefinition
                {
                    Orden = fila - rangoBaseExcel.Start.Row + 1,
                    FilaOrigen = fila
                };

                var codigo = fila >= primeraFilaPrestacion
                    ? TextoCelda(hojaDiccionario.Cells[fila, rangoDiccionario.Start.Column])
                    : string.Empty;
                var tienePrestacion = EsCodigoPrestacion(codigo);

                for (var columnaBase = rangoBaseExcel.Start.Column; columnaBase <= rangoBaseExcel.End.Column; columnaBase++)
                {
                    var columnaDiccionario = columnaBase + 1;
                    var tieneCorrespondenciaDiccionario = columnaDiccionario >= rangoDiccionario.Start.Column
                        && columnaDiccionario <= rangoDiccionario.End.Column;
                    var merge = merges.TryGetValue((fila, columnaBase), out var mergeInfo)
                        ? mergeInfo
                        : null;
                    if (merge is not null && !merge.EsAncla)
                        continue;

                    var cellBase = hojaBase.Cells[fila, columnaBase];
                    var textoBase = TextoCelda(cellBase);
                    var formula = cellBase.Formula?.Trim() ?? string.Empty;
                    var textoDiccionario = tieneCorrespondenciaDiccionario
                        ? TextoCelda(hojaDiccionario.Cells[fila, columnaDiccionario])
                        : string.Empty;
                    var editable = EsEditable(cellBase, textoBase);
                    var esColumnaPosteriorAlOffset = columnaDiccionario >= primeraColumnaPrestacion;
                    var esColumnaDeDatos = string.IsNullOrWhiteSpace(textoDiccionario)
                        || CodigoColumnaRegex.IsMatch(textoDiccionario.Trim());
                    var dependenciasTotal = ExtraerDependenciasTotal(
                        formula,
                        fila,
                        columnaBase,
                        rangoBaseExcel);
                    var esColumnaPrestacionIdentificada = CodigoColumnaRegex.IsMatch(textoDiccionario.Trim());

                    // Una celda con fórmula puede ser un total de la propia
                    // prestación (por ejemplo, Col01 = suma de sus columnas
                    // por sexo). No debe perderse como coordenada sólo por
                    // estar marcada como total. Los totales independientes,
                    // al no pertenecer a una fila con prestación, quedan como
                    // celdas estructurales y no se convierten en prestaciones.
                    var entrada = tieneCorrespondenciaDiccionario
                        && tienePrestacion
                        && esColumnaPosteriorAlOffset
                        && esColumnaDeDatos
                        && ((editable && string.IsNullOrWhiteSpace(formula))
                            || (esColumnaPrestacionIdentificada && dependenciasTotal.Count > 0));

                    var esTotal = dependenciasTotal.Count > 0;
                    var operacionTotal = esTotal && EsFormulaSuma(formula) ? "SUMA" : string.Empty;

                    var cellDefinition = new RemCellDefinition
                    {
                        FilaOrigen = fila,
                        ColumnaOrigen = columnaBase,
                        Valor = textoBase,
                        CeldaDiccionario = tieneCorrespondenciaDiccionario
                            ? hojaDiccionario.Cells[fila, columnaDiccionario].Address
                            : string.Empty,
                        CeldaBase = cellBase.Address,
                        RowSpan = merge?.RowSpan ?? 1,
                        ColSpan = merge?.ColSpan ?? 1,
                        EsCeldaAncla = true,
                        EsEditable = editable,
                        EsEntradaPrestacion = entrada,
                        EsTotal = esTotal,
                        TipoTotal = esTotal ? TipoTotal(fila, columnaBase, dependenciasTotal, tienePrestacion) : string.Empty,
                        FormulaOrigen = formula,
                        DependenciasTotal = string.Join(";", dependenciasTotal),
                        OperacionTotal = operacionTotal,
                        EstiloOrigen = cellBase.StyleID,
                        ColorFondo = ColorFondo(cellBase)
                    };
                    row.Celdas.Add(cellDefinition);

                    if (entrada)
                    {
                        var codigoColumna = CodigoColumna(textoDiccionario, row.Coordenadas.Count + 1);
                        row.Coordenadas.Add(new RemCoordinateDefinition
                        {
                            Orden = row.Coordenadas.Count + 1,
                            CodigoColumna = codigoColumna,
                            CeldaDiccionario = hojaDiccionario.Cells[fila, columnaDiccionario].Address,
                            CeldaBase = cellBase.Address,
                            Celda = cellDefinition
                        });
                    }
                }

                if (tienePrestacion)
                {
                    row.CodigoPrestacion = codigo;
                    row.NombrePrestacion = ExtraerNombrePrestacion(
                        hojaDiccionario,
                        fila,
                        rangoDiccionario.Start.Column,
                        rangoDiccionario.End.Column);
                    row.TipoFila = "Prestacion";
                }
                else
                {
                    row.TipoFila = ClasificarFila(row);
                }

                section.Filas.Add(row);
            }

            result.Sections.Add(section);
        }

        return result;
    }

    private static List<string> ExtraerDependenciasTotal(
        string formula,
        int filaObjetivo,
        int columnaObjetivo,
        ExcelRangeBase rangoSeccion)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return new List<string>();

        var dependencias = new List<string>();
        var existentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in ReferenciaCeldaRegex.Matches(formula))
        {
            var columnaInicio = ColumnaNumero(match.Groups["col"].Value);
            if (!int.TryParse(match.Groups["row"].Value, out var filaInicio)
                || columnaInicio <= 0)
                continue;

            var columnaFin = match.Groups["endCol"].Success
                ? ColumnaNumero(match.Groups["endCol"].Value)
                : columnaInicio;
            var filaFin = match.Groups["endRow"].Success
                && int.TryParse(match.Groups["endRow"].Value, out var filaFinal)
                ? filaFinal
                : filaInicio;

            var filaMinima = Math.Min(filaInicio, filaFin);
            var filaMaxima = Math.Max(filaInicio, filaFin);
            var columnaMinima = Math.Min(columnaInicio, columnaFin);
            var columnaMaxima = Math.Max(columnaInicio, columnaFin);

            // Una fórmula de total debe resolverse contra la estructura de la
            // misma sección. Las referencias externas no pueden ser rellenadas
            // desde los registros consolidados de esta tabla.
            filaMinima = Math.Max(filaMinima, rangoSeccion.Start.Row);
            filaMaxima = Math.Min(filaMaxima, rangoSeccion.End.Row);
            columnaMinima = Math.Max(columnaMinima, rangoSeccion.Start.Column);
            columnaMaxima = Math.Min(columnaMaxima, rangoSeccion.End.Column);

            for (var fila = filaMinima; fila <= filaMaxima; fila++)
            for (var columna = columnaMinima; columna <= columnaMaxima; columna++)
            {
                if (fila == filaObjetivo && columna == columnaObjetivo)
                    continue;

                var direccion = DireccionCelda(fila, columna);
                if (existentes.Add(direccion))
                    dependencias.Add(direccion);
            }
        }

        return dependencias;
    }

    private static string TipoTotal(
        int filaObjetivo,
        int columnaObjetivo,
        IReadOnlyCollection<string> dependencias,
        bool tienePrestacion)
    {
        var coordenadas = dependencias
            .Select(AnalizarDireccion)
            .ToList();

        if (coordenadas.Count > 0 && coordenadas.All(coordenada => coordenada.Row == filaObjetivo))
            return "Fila";

        if (coordenadas.Count > 0 && coordenadas.All(coordenada => coordenada.Column == columnaObjetivo))
            return "Columna";

        return tienePrestacion ? "Sexo" : "Derivado";
    }

    private static bool EsFormulaSuma(string formula)
    {
        var normalizada = formula
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
        normalizada = normalizada.TrimStart('=');

        return normalizada.StartsWith("SUM(", StringComparison.Ordinal)
            || normalizada.StartsWith("SUMA(", StringComparison.Ordinal)
            || Regex.IsMatch(
                normalizada,
                @"^[A-Z]{1,3}\d+(\+[A-Z]{1,3}\d+)+$",
                RegexOptions.CultureInvariant);
    }

    private static int ColumnaNumero(string columna)
    {
        var resultado = 0;
        foreach (var caracter in columna.Trim().ToUpperInvariant())
        {
            if (caracter is < 'A' or > 'Z')
                return 0;
            resultado = resultado * 26 + caracter - 'A' + 1;
        }

        return resultado;
    }

    private static string DireccionCelda(int fila, int columna)
    {
        var letras = new StringBuilder();
        var valor = columna;
        while (valor > 0)
        {
            valor--;
            letras.Insert(0, (char)('A' + valor % 26));
            valor /= 26;
        }

        return $"{letras}{fila}";
    }

    private static (int Row, int Column) AnalizarDireccion(string direccion)
    {
        var match = Regex.Match(
            direccion,
            @"^(?<col>[A-Z]{1,3})(?<row>\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups["row"].Value, out var fila)
            ? (fila, ColumnaNumero(match.Groups["col"].Value))
            : (0, 0);
    }

    private static string ConstruirRangoBase(ExcelWorksheet hojaBase, ExcelRangeBase rangoDiccionario)
    {
        var startColumn = Math.Max(1, rangoDiccionario.Start.Column - 1);
        var endColumn = rangoDiccionario.End.Column - 1;
        if (startColumn < 1 || endColumn < 1)
            return string.Empty;

        // Algunas secciones de la planilla base tienen celdas combinadas que
        // se extienden más allá del rango equivalente del diccionario. El
        // HTML debe conservar la combinación completa para no desarmar la fila.
        foreach (var address in hojaBase.MergedCells)
        {
            var merged = hojaBase.Cells[address];
            if (merged.End.Row < rangoDiccionario.Start.Row
                || merged.Start.Row > rangoDiccionario.End.Row
                || merged.End.Column < startColumn
                || merged.Start.Column > endColumn)
                continue;

            endColumn = Math.Max(endColumn, merged.End.Column);
        }

        return hojaBase.Cells[rangoDiccionario.Start.Row, startColumn, rangoDiccionario.End.Row, endColumn].Address;
    }

    private static string ExtraerTitulo(ExcelWorksheet hoja, ExcelRangeBase rango)
    {
        for (var columna = rango.Start.Column; columna <= rango.End.Column; columna++)
        {
            var texto = TextoCelda(hoja.Cells[rango.Start.Row, columna]);
            if (!string.IsNullOrWhiteSpace(texto))
                return texto.Trim();
        }

        return string.Empty;
    }

    private static string ExtraerNombrePrestacion(
        ExcelWorksheet hojaDiccionario,
        int fila,
        int columnaInicio,
        int columnaFin)
    {
        for (var columna = columnaInicio + 1; columna <= columnaFin; columna++)
        {
            var texto = TextoCelda(hojaDiccionario.Cells[fila, columna]);
            if (string.IsNullOrWhiteSpace(texto))
                continue;
            if (CodigoColumnaRegex.IsMatch(texto.Trim()))
                continue;
            return texto.Trim();
        }

        return string.Empty;
    }

    private static string ClasificarFila(RemRowDefinition row)
    {
        var texto = string.Join(" ", row.Celdas.Select(c => c.Valor)).Trim();
        if (row.Orden == 1)
            return "Titulo";
        if (texto.Contains("TOTAL", StringComparison.OrdinalIgnoreCase))
            return "Total";
        if (row.Celdas.Any(c => !string.IsNullOrWhiteSpace(c.Valor)))
            return "Encabezado";
        return "Otro";
    }

    private static bool EsCodigoPrestacion(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && CodigoPrestacionRegex.IsMatch(value.Trim())
            && value.Any(char.IsDigit);
    }

    private static string CodigoColumna(string valor, int orden)
    {
        var texto = valor.Trim();
        return CodigoColumnaRegex.IsMatch(texto)
            ? texto.ToUpperInvariant()
            : $"COL{orden.ToString("D2", CultureInfo.InvariantCulture)}";
    }

    private static string TextoCelda(ExcelRangeBase celda)
    {
        if (!string.IsNullOrWhiteSpace(celda.Text))
            return celda.Text.Trim();
        return Convert.ToString(celda.Value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static string ColorFondo(ExcelRangeBase celda)
    {
        var pattern = celda.Style.Fill.PatternColor.Rgb;
        var background = celda.Style.Fill.BackgroundColor.Rgb;
        return string.Join(";", new[] { pattern, background }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static bool EsEditable(ExcelRangeBase celda, string texto)
    {
        if (CodigoColumnaRegex.IsMatch(texto.Trim()))
            return true;

        if (!celda.Style.Locked)
            return true;

        var colores = ColorFondo(celda);
        return EsAmbar(colores);
    }

    private static bool EsAmbar(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        var normalized = color.Replace("#", string.Empty, StringComparison.Ordinal)
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal);

        return normalized.Contains("FFC000", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("F4B183", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("FFCC99", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("FFE699", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<(int Row, int Column), MergeInfo> ConstruirMapaCombinaciones(
        ExcelWorksheet hoja,
        ExcelRangeBase rango)
    {
        var map = new Dictionary<(int Row, int Column), MergeInfo>();

        foreach (var address in hoja.MergedCells)
        {
            var merged = hoja.Cells[address];
            if (merged.End.Row < rango.Start.Row || merged.Start.Row > rango.End.Row
                || merged.End.Column < rango.Start.Column || merged.Start.Column > rango.End.Column)
                continue;

            var startRow = Math.Max(merged.Start.Row, rango.Start.Row);
            var endRow = Math.Min(merged.End.Row, rango.End.Row);
            var startColumn = Math.Max(merged.Start.Column, rango.Start.Column);
            var endColumn = Math.Min(merged.End.Column, rango.End.Column);
            var info = new MergeInfo(
                merged.Start.Row,
                merged.Start.Column,
                endRow - startRow + 1,
                endColumn - startColumn + 1);

            for (var row = startRow; row <= endRow; row++)
            for (var column = startColumn; column <= endColumn; column++)
                map[(row, column)] = info with { EsAncla = row == merged.Start.Row && column == merged.Start.Column };
        }

        return map;
    }

    private sealed record MergeInfo(int StartRow, int StartColumn, int RowSpan, int ColSpan, bool EsAncla = false);
}

internal static class RemStructureHtmlRenderer
{
    public static string Render(RemSectionDefinition section)
    {
        var builder = new StringBuilder();
        builder.Append("<table class=\"rem-seccion\" data-seccion=\"")
            .Append(WebUtility.HtmlEncode(section.Codigo))
            .Append("\">");

        foreach (var row in section.Filas.OrderBy(x => x.Orden))
        {
            builder.Append("<tr data-fila=\"")
                .Append(row.FilaOrigen)
                .Append("\" class=\"fila-")
                .Append(row.TipoFila.ToLowerInvariant())
                .Append("\">");

            foreach (var cell in row.Celdas.OrderBy(x => x.ColumnaOrigen))
            {
                builder.Append("<td data-celda=\"")
                    .Append(WebUtility.HtmlEncode(cell.CeldaBase))
                    .Append("\"");

                if (cell.RowSpan > 1)
                    builder.Append(" rowspan=\"").Append(cell.RowSpan).Append("\"");
                if (cell.ColSpan > 1)
                    builder.Append(" colspan=\"").Append(cell.ColSpan).Append("\"");

                if (cell.EsTotal)
                    builder.Append(" data-tipo=\"total\"");

                builder.Append(" class=\"")
                    .Append(cell.EsTotal ? "total" : cell.EsEntradaPrestacion ? "prestacion" : cell.EsEditable ? "editable" : "estructura")
                    .Append("\">")
                    .Append(cell.EsTotal ? string.Empty : WebUtility.HtmlEncode(cell.Valor))
                    .Append("</td>");
            }

            builder.Append("</tr>");
        }

        return builder.Append("</table>").ToString();
    }
}
