using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace RemTool.Services;

public static class MatrizMensualExcel
{
    private const string Azul = "000084";
    private const string AzulClaro = "E8F0FE";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CL");

    public static byte[] Generar(IndicadorSeguimiento indicador, string contexto)
    {
        using var package = new ExcelPackage();
        var hoja = package.Workbook.Worksheets.Add("Matriz mensual");
        var evaluacion = indicador.Evaluacion;
        // The matrix reports through the selected cut. Formal evaluation can be earlier
        // for semestral indicators.
        var meses = Enumerable.Range(1, evaluacion.MesCorte).ToList();
        var ultimaColumna = meses.Count + 2;

        hoja.Cells[1, 1, 1, ultimaColumna].Merge = true;
        hoja.Cells[1, 1].Value = indicador.Nombre;
        hoja.Cells[1, 1].Style.Font.Bold = true;
        hoja.Cells[1, 1].Style.Font.Size = 16;
        hoja.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
        hoja.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        hoja.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#" + Azul));
        hoja.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

        hoja.Cells[2, 1, 2, ultimaColumna].Merge = true;
        hoja.Cells[2, 1].Value = $"Año: {indicador.Año} · Corte: {NombreMes(evaluacion.MesCorte)} · Evaluación: {NombreMes(evaluacion.MesEvaluacion)}" +
            (evaluacion.Provisional ? " · Provisional" : "");
        hoja.Cells[3, 1, 3, ultimaColumna].Merge = true;
        hoja.Cells[3, 1].Value = contexto;
        hoja.Cells[4, 1, 4, ultimaColumna].Merge = true;
        hoja.Cells[4, 1].Value = indicador.IsColaborativo
            ? "Cada mes muestra la producción aportada. El denominador es comunal y no se suma por establecimiento."
            : indicador.IsDenFijo
                ? "Cada mes muestra el numerador. El denominador corresponde al objetivo calculado y se informa en el acumulado."
                : "Cada mes muestra Numerador / Denominador. P identifica la serie P. La fila final suma los establecimientos del mes.";
        hoja.Cells[4, 1].Style.Font.Italic = true;
        hoja.Cells[4, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(71, 85, 105));

        const int filaEncabezado = 6;
        hoja.Cells[filaEncabezado, 1].Value = "Establecimiento";
        hoja.Cells[filaEncabezado, 2].Value = "Sector";
        foreach (var mes in meses)
            hoja.Cells[filaEncabezado, mes + 2].Value = NombreMes(mes);
        hoja.Cells[filaEncabezado, ultimaColumna].Value = "Acumulado";
        var encabezado = hoja.Cells[filaEncabezado, 1, filaEncabezado, ultimaColumna];
        encabezado.Style.Font.Bold = true;
        encabezado.Style.Font.Color.SetColor(System.Drawing.Color.White);
        encabezado.Style.Fill.PatternType = ExcelFillStyle.Solid;
        encabezado.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#" + Azul));
        encabezado.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        encabezado.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        hoja.Row(filaEncabezado).Height = 26;

        var primeraFilaDatos = filaEncabezado + 1;
        var fila = primeraFilaDatos;
        foreach (var establecimiento in indicador.Establecimientos)
        {
            hoja.Cells[fila, 1].Value = establecimiento.Nombre;
            hoja.Cells[fila, 2].Value = establecimiento.Sector;
            foreach (var mes in meses)
                hoja.Cells[fila, mes + 2].Value = FormatearMes(
                    establecimiento.Meses.Where(r => r.Mes == mes), indicador.IsColaborativo, indicador.IsDenFijo);
            hoja.Cells[fila, ultimaColumna].Value = FormatearAcumulado(establecimiento.Evaluacion, indicador.IsColaborativo);
            fila++;
        }

        var filaTotal = fila;
        if (indicador.Establecimientos.Count > 0)
        {
            hoja.Cells[filaTotal, 1].Value = "Total mensual";
            foreach (var mes in meses)
                hoja.Cells[filaTotal, mes + 2].Value = FormatearMes(
                    indicador.Establecimientos.SelectMany(e => e.Meses).Where(r => r.Mes == mes),
                    indicador.IsColaborativo, indicador.IsDenFijo);
            hoja.Cells[filaTotal, ultimaColumna].Value = FormatearAcumulado(evaluacion, indicador.IsColaborativo);
            var total = hoja.Cells[filaTotal, 1, filaTotal, ultimaColumna];
            total.Style.Font.Bold = true;
            total.Style.Fill.PatternType = ExcelFillStyle.Solid;
            total.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#" + AzulClaro));
            total.Style.Border.Top.Style = ExcelBorderStyle.Medium;
            total.Style.Border.Top.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#" + Azul));
        }
        else
        {
            hoja.Cells[primeraFilaDatos, 1, primeraFilaDatos, ultimaColumna].Merge = true;
            hoja.Cells[primeraFilaDatos, 1].Value = "No hay registros de establecimientos en el período evaluado.";
            hoja.Cells[primeraFilaDatos, 1].Style.Font.Italic = true;
        }

        var ultimaFila = Math.Max(filaTotal, primeraFilaDatos);
        var tabla = hoja.Cells[filaEncabezado, 1, ultimaFila, ultimaColumna];
        tabla.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        tabla.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(203, 213, 225));
        tabla.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        tabla.Style.Border.Left.Color.SetColor(System.Drawing.Color.FromArgb(226, 232, 240));
        tabla.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        tabla.Style.Border.Right.Color.SetColor(System.Drawing.Color.FromArgb(226, 232, 240));
        tabla.Style.WrapText = true;
        tabla.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
        hoja.Cells[primeraFilaDatos, 3, ultimaFila, ultimaColumna].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        hoja.Column(1).Width = 42;
        hoja.Column(2).Width = 20;
        for (var col = 3; col <= ultimaColumna; col++) hoja.Column(col).Width = 17;
        for (var current = primeraFilaDatos; current <= ultimaFila; current++) hoja.Row(current).Height = 34;
        hoja.View.FreezePanes(filaEncabezado + 1, 3);
        hoja.Cells[1, 1, ultimaFila, ultimaColumna].AutoFilter = false;
        hoja.Cells[filaEncabezado, 1, ultimaFila, ultimaColumna].AutoFilter = true;
        hoja.Workbook.Properties.Title = $"Matriz mensual - {indicador.Nombre}";
        hoja.Workbook.Properties.Author = "REMTool";
        return package.GetAsByteArray();
    }

    private static string FormatearMes(IEnumerable<RegistroSeguimiento> registros, bool colaborativo, bool denFijo)
    {
        var lista = registros.ToList();
        if (lista.Count == 0) return "—";
        var numerador = lista.Sum(r => r.Numerador);
        var denominador = lista.Sum(r => r.Denominador);
        var numeradorP = lista.Sum(r => r.NumeradorP);
        var denominadorP = lista.Sum(r => r.DenominadorP);
        var valor = colaborativo || denFijo ? Numero(numerador) : $"{Numero(numerador)} / {Numero(denominador)}";
        return numeradorP == 0m && denominadorP == 0m
            ? valor
            : valor + Environment.NewLine + (colaborativo || denFijo
                ? $"P: {Numero(numeradorP)}"
                : $"P: {Numero(numeradorP)} / {Numero(denominadorP)}");
    }

    private static string FormatearAcumulado(EvaluacionSeguimiento evaluacion, bool colaborativo) =>
        colaborativo ? Numero(evaluacion.Numerador) : $"{Numero(evaluacion.Numerador)} / {Numero(evaluacion.Denominador)}";

    private static string Numero(decimal valor) => valor.ToString("N0", Cultura);
    private static string NombreMes(int mes) => Cultura.DateTimeFormat.GetMonthName(mes);
}
