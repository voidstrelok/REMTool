using System.Globalization;
using System.Security;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace RemTool.Services;

/// <summary>Presentation only: all numbers, states and analysis come from the shared evaluation.</summary>
public static class SeguimientoPdf
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CL");
    private const string Azul = "#000084";
    private const string Gris = "#475569";
    public static string Numero(decimal? value) => value?.ToString("N1", Cultura) ?? "—";
    public static string Valor(decimal? value, bool tasa) => Numero(value) + (value.HasValue && !tasa ? " %" : "");
    public static string Brecha(decimal? value, bool tasa) => value.HasValue ? (value > 0 ? "+" : "") + Numero(value) + (tasa ? " u." : " pp") : "—";
    public static string Mes(int? mes) => mes.HasValue ? Cultura.DateTimeFormat.GetMonthName(mes.Value) : "sin registros";
    public static string Estado(string estado) => estado switch
    {
        "Cumplida" => "Cumplida", "EnCurso" => "En curso", "EnRiesgo" => "En riesgo", "Critico" => "Crítico",
        "SinDenominador" => "Sin denominador", "SinMeta" => "Sin meta", _ => "Sin datos"
    };
    private static string Color(string estado) => estado switch
    {
        "Cumplida" => "#157347", "EnCurso" => "#1565C0", "EnRiesgo" => "#92400E", "Critico" => "#B42318", _ => Gris
    };

    public static byte[] Generar(string titulo, string contexto, int año, int? corte,
        IReadOnlyList<IndicadorSeguimiento> items, ResumenSeguimiento? resumen = null,
        IReadOnlyList<ConvenioSeguimiento>? convenios = null, bool soloDetalle = false)
    {
        var generado = DateTimeOffset.Now;
        return Document.Create(document =>
        {
            void Pagina(string nombre, Action<ColumnDescriptor> contenido) => document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(s => s.FontSize(9).FontColor("#172554"));
                page.Header().PaddingBottom(14).Column(c =>
                {
                    c.Item().Text(nombre).FontSize(15).Bold().FontColor(Azul);
                    c.Item().PaddingTop(4).Text($"{contexto} · Año {año} · Corte: {Mes(corte)}").FontColor(Gris);
                    c.Item().Text($"Generado: {generado:dd/MM/yyyy HH:mm zzz} · Fuente: REMTool").FontSize(8).FontColor(Gris);
                });
                page.Content().Column(contenido);
                page.Footer().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem().Text("Área de Estadísticas · Salud Monte Patria").FontSize(7).FontColor(Gris);
                    row.AutoItem().Text(t => { t.Span("Página "); t.CurrentPageNumber(); t.Span(" de "); t.TotalPages(); });
                });
            });

            if (!soloDetalle) Pagina(titulo, col =>
            {
                col.Spacing(12);
                if (resumen != null) Resumen(col, resumen);
                if (convenios != null)
                {
                    col.Item().Text($"Cumplimiento general: {Numero(convenios.Count > 0 ? convenios.Average(c => c.Resumen.Cumplimiento) : 0m)} %").Bold();
                    col.Item().Text("Promedio simple de los convenios, incluidos los que no tienen indicadores (aporte cero).").FontColor(Gris);
                    Tabla(col.Item(), ["Convenio", "Indicadores", "Cumplimiento", "Cumplidas", "En curso", "Atención", "Sin evaluar"], [3, 1, 1.4f, 1, 1, 1, 1.2f],
                        convenios.Select(c => new[] { c.Nombre, c.IndicadorCount.ToString(), Numero(c.Resumen.Cumplimiento) + " %", c.Resumen.Cumplidas.ToString(), c.Resumen.EnCurso.ToString(),
                            (c.Resumen.EnRiesgo + c.Resumen.Criticos).ToString(), (c.Resumen.SinDatos + c.Resumen.SinDenominador + c.Resumen.SinMeta).ToString() }));
                    foreach (var convenio in convenios)
                    {
                        col.Item().EnsureSpace(100).Column(group =>
                        {
                            group.Spacing(8);
                            group.Item().Text(convenio.Nombre).Bold().FontSize(12);
                            Resumen(group, convenio.Resumen);
                            TablaIndicadores(group.Item(), convenio.Indicadores);
                        });
                    }
                }
                else TablaIndicadores(col.Item(), items);
                col.Item().EnsureSpace(90).Column(c =>
                {
                    c.Item().Text("Cómo leer este respaldo").Bold();
                    c.Item().Text(EvaluadorSeguimiento.Criterios).FontSize(8).FontColor(Gris);
                });
            });

            foreach (var item in items) Pagina(item.Nombre, col =>
            {
                col.Spacing(8);
                var e = item.Evaluacion;
                col.Item().Text($"Evaluación: {Mes(e.MesEvaluacion)} · Último registro al corte: {Mes(e.UltimoMesDatos)}" +
                    (e.Provisional ? " · PROVISIONAL" : "")).FontColor(Gris);
                col.Item().Row(row =>
                {
                    void Kpi(string label, string value) => row.RelativeItem().Border(1).BorderColor("#CBD5E1").Padding(7).Column(c =>
                    { c.Item().Text(label).FontSize(8).FontColor(Gris); c.Item().Text(value).Bold().FontSize(12); });
                    Kpi("Resultado", Valor(e.Resultado, item.IsTasa));
                    Kpi("Meta", Valor(e.Meta, item.IsTasa));
                    Kpi("Cumplimiento", Valor(e.Cumplimiento, false));
                    Kpi("Esperado", Valor(e.Esperado, item.IsTasa));
                    Kpi("Brecha", Brecha(e.Brecha, item.IsTasa));
                });
                col.Item().Text($"{Estado(e.Estado)} · Numerador: {e.Numerador.ToString("N0", Cultura)} · {(item.IsColaborativo ? "Denominador comunal" : "Denominador")}: {e.Denominador.ToString("N0", Cultura)}")
                    .FontColor(Color(e.Estado)).Bold();
                if (item.Tipoindicador != 1) col.Item().Text($"Peso: {Valor(item.Peso * 100m, false)} · Aporte al agregado: {Valor(item.Aporte * 100m, false)} · Ajuste incluido: {(item.Ajuste * 100m).ToString("N2", Cultura)} pp").FontSize(8).FontColor(Gris);
                col.Item().EnsureSpace(155).Column(c =>
                {
                    c.Item().Text("Evolución acumulada").Bold().FontSize(12);
                    c.Item().Height(130).Svg(Chart(item));
                });
                Tabla(col.Item(), ["Evaluación", "Numerador", "Denominador", "Resultado", "Esperado", "Brecha", "Estado"], [1.1f, 1.1f, 1.2f, 1, 1, 1, 1.3f],
                    item.Evolucion.Select(p => new[] { Mes(p.MesEvaluacion), p.TieneDatos ? p.Numerador.ToString("N0", Cultura) : "—", p.TieneDatos ? p.Denominador.ToString("N0", Cultura) : "—",
                        Valor(p.Resultado, item.IsTasa), Valor(p.Esperado, item.IsTasa), Brecha(p.Brecha, item.IsTasa), Estado(p.Estado) }));
                col.Item().EnsureSpace(100).Column(c =>
                {
                    c.Spacing(8);
                    c.Item().Text("Desglose por establecimiento").Bold().FontSize(12);
                    if (item.IsColaborativo)
                        Tabla(c.Item(), ["Establecimiento", "Sector", "Producción aportada"], [3, 1.5f, 1.5f],
                            item.Establecimientos.Select(s => new[] { s.Nombre, s.Sector, s.Evaluacion.Numerador.ToString("N0", Cultura) }));
                    else Tabla(c.Item(), ["Establecimiento", "Sector", "Num.", "Den.", "Resultado", "Cumplimiento", "Estado"], [2.7f, 1.3f, 1, 1, 1.2f, 1.4f, 1.4f],
                        item.Establecimientos.Select(s => new[] { s.Nombre, s.Sector, s.Evaluacion.Numerador.ToString("N0", Cultura), s.Evaluacion.Denominador.ToString("N0", Cultura),
                            Valor(s.Evaluacion.Resultado, item.IsTasa), Valor(s.Evaluacion.Cumplimiento, false), Estado(s.Evaluacion.Estado) }));
                });
                col.Item().EnsureSpace(110).Column(c =>
                {
                    c.Spacing(5);
                    c.Item().Text("Análisis y contexto del indicador").Bold().FontSize(12);
                    foreach (var texto in item.Analisis) c.Item().Text(texto);
                    if (!string.IsNullOrWhiteSpace(item.Detalle)) c.Item().PaddingTop(5).Text(item.Detalle);
                });
                if (soloDetalle) col.Item().EnsureSpace(80).Column(c =>
                {
                    c.Item().Text("Criterios de seguimiento").Bold();
                    c.Item().Text(EvaluadorSeguimiento.Criterios).FontSize(8).FontColor(Gris);
                });
            });
        }).GeneratePdf();
    }

    private static void Resumen(ColumnDescriptor col, ResumenSeguimiento r)
    {
        col.Item().Text($"Cumplimiento general: {Numero(r.Cumplimiento)} %").FontSize(14).Bold();
        col.Item().Text($"Contexto completo: {r.Total} indicadores · {r.Cumplidas} cumplidas · {r.EnCurso} en curso · {r.EnRiesgo} en riesgo · {r.Criticos} críticos · {r.SinDatos} sin datos · {r.SinDenominador} sin denominador · {r.SinMeta} sin meta");
        col.Item().Text(r.Metodo).FontSize(8).FontColor(Gris);
    }

    private static void TablaIndicadores(IContainer container, IReadOnlyList<IndicadorSeguimiento> items) =>
        Tabla(container, ["Indicador", "Resultado", "Meta", "Cumplimiento", "Esperado", "Brecha", "Estado"], [3, 1.1f, 1, 1.3f, 1.1f, 1, 1.3f],
            items.Select(i => new[] { $"{i.Orden}. {i.Nombre}" + (i.Evaluacion.Provisional ? " (provisional)" : ""), Valor(i.Evaluacion.Resultado, i.IsTasa),
                Valor(i.Evaluacion.Meta, i.IsTasa), Valor(i.Evaluacion.Cumplimiento, false), Valor(i.Evaluacion.Esperado, i.IsTasa),
                Brecha(i.Evaluacion.Brecha, i.IsTasa), Estado(i.Evaluacion.Estado) }));

    private static void Tabla(IContainer container, string[] headers, float[] widths, IEnumerable<string[]> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols => { foreach (var width in widths) cols.RelativeColumn(width); });
            table.Header(head => { foreach (var text in headers) head.Cell().Background(Azul).Padding(5).Text(text).FontSize(8).FontColor(Colors.White).Bold(); });
            int index = 0;
            foreach (var row in rows)
            {
                foreach (var text in row) table.Cell().Background(index % 2 == 0 ? "#F1F5F9" : Colors.White)
                    .BorderBottom(.5f).BorderColor("#CBD5E1").Padding(4).Text(text).FontSize(8);
                index++;
            }
            if (index == 0) table.Cell().ColumnSpan((uint)headers.Length).Padding(10).Text("No hay registros en este período.").FontColor(Gris);
        });
    }

    private static string Chart(IndicadorSeguimiento item)
    {
        const decimal width = 520, height = 150, left = 45, right = 15, top = 18, bottom = 30;
        var points = item.Evolucion;
        decimal max = Math.Max(1m, points.SelectMany(p => new[] { p.Resultado ?? 0, p.Esperado, p.Meta }).DefaultIfEmpty(1).Max()) * 1.1m;
        string F(decimal n) => n.ToString("0.##", CultureInfo.InvariantCulture);
        decimal X(int m) => left + (m - 1) / 11m * (width - left - right);
        decimal Y(decimal n) => height - bottom - n / max * (height - top - bottom);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}'>");
        for (var t = 0; t <= 4; t++)
        {
            var value = max * t / 4m;
            svg.Append($"<line x1='{left}' x2='{width - right}' y1='{F(Y(value))}' y2='{F(Y(value))}' stroke='#E2E8F0'/><text x='0' y='{F(Y(value) + 3)}' font-size='8' fill='{Gris}'>{SecurityElement.Escape(Numero(value))}</text>");
        }
        var yMeta = Y(item.Evaluacion.Meta);
        svg.Append($"<line x1='{left}' x2='{width - right}' y1='{F(yMeta)}' y2='{F(yMeta)}' stroke='#157347' stroke-dasharray='4,3'/>");
        void Serie(Func<EvaluacionSeguimiento, decimal?> select, string color, bool dashed)
        {
            var segments = new List<string>();
            foreach (var p in points)
            {
                var value = select(p);
                if (!value.HasValue) { segments.Clear(); continue; }
                var xy = $"{F(X(p.MesEvaluacion))},{F(Y(value.Value))}";
                if (segments.Count > 0) svg.Append($"<polyline points='{segments[^1]} {xy}' fill='none' stroke='{color}' stroke-width='2' {(dashed ? "stroke-dasharray='4,3'" : "")}/>");
                svg.Append($"<circle cx='{F(X(p.MesEvaluacion))}' cy='{F(Y(value.Value))}' r='2.5' fill='{color}'/>");
                segments.Add(xy);
            }
        }
        Serie(p => p.Resultado, Azul, false);
        Serie(p => p.Esperado, "#0284C7", true);
        foreach (var p in points) svg.Append($"<text x='{F(X(p.MesEvaluacion))}' y='134' text-anchor='middle' font-size='8'>{SecurityElement.Escape(Mes(p.MesEvaluacion)[..3])}</text>");
        svg.Append($"<text x='{left}' y='148' font-size='8' fill='{Azul}'>Resultado</text><text x='130' y='148' font-size='8' fill='#0284C7'>Esperado</text><text x='210' y='148' font-size='8' fill='#157347'>Meta</text></svg>");
        return svg.ToString();
    }
}
