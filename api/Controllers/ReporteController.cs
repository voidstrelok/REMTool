using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RemTool.Enum;

namespace RemTool.Controllers
{
    [ApiController]
    [Route("api")]
    public class ReporteController : ControllerBase
    {
        private readonly VoidDataContext _db;
        private List<long> EstablecimientosExcluidos = new List<long> { (long)EnumEstablecimiento.ClinicaDentalMovilMontePatria, (long)EnumEstablecimiento.SARMontePatria, (long)EnumEstablecimiento.SURElPalqui };

        public ReporteController(VoidDataContext db)
        {
            _db = db;
        }

        // ── DTOs internos ────────────────────────────────────────────────────

        private enum EstadoIndicador { Cumplida, EnCurso, EnRiesgo, Critico }

        private record FilaInforme(
            int Orden,
            string Nombre,
            float Meta,
            bool IsTasa,
            float Actual,
            float EsperadoAlMes,
            float Brecha,
            EstadoIndicador Estado,
            float Den
        );

        // ── Lógica de cálculo ────────────────────────────────────────────────

        private static FilaInforme BuildFila(Indicador ind, int ano, int mes,
            IEnumerable<ResultadoIndicador>? resultadosSource = null,
            decimal denPrevAnoOctDic = 0m)
        {
            var resultados = resultadosSource ?? ind.ResultadoIndicadors;

            var hastaCorte = resultados.Where(r => r.Mes <= mes).ToList();

            // Numerador acumulado al mes de corte
            decimal num = decimal.Round(hastaCorte.Sum(r => r.Numerador + r.NumeradorP));

            // Denominador: período estándar (ene–mes) o período oct(año-1)–sep(año actual)
            decimal den = ind.EsPeriodoOctubreSep
                ? decimal.Round(hastaCorte.Where(r => r.Mes < 10).Sum(r => r.Denominador + r.DenominadorP)) + denPrevAnoOctDic
                : decimal.Round(hastaCorte.Sum(r => r.Denominador + r.DenominadorP));

            // Ajuste para denominador fijo: mostrar target (den * meta) con meta al 100%
            if (ind.IsDenFijo && den != 0)
                den = decimal.Round(den * (decimal)ind.Meta);
            float metaEfectiva = ind.IsDenFijo ? 1.0f : ind.Meta;

            float actual = den != 0 ? MathF.Round((float)(num / den), 4) : 0f;

            // Avance esperado al mes de corte:
            // - Mensual  (Mensual=true):   progresa linealmente → meta × (N/12)
            // - Semestral (Mensual=false):  el objetivo es por semestre; en cualquier mes
            //   del semestre ya se evalúa contra la meta completa del semestre:
            //   S1 (ene–jun) → meta × 0.5   |   S2 (jul–dic) → meta × 1.0
            float esperadoAlMes = ind.Mensual
                ? MathF.Round(metaEfectiva * (mes / 12f), 4)
                : MathF.Round(metaEfectiva * (mes <= 6 ? 0.5f : 1.0f), 4);

            // Brecha: diferencia entre lo logrado y lo esperado a esta altura del año.
            // Positivo → adelantado; negativo → atrasado respecto al ritmo necesario.
            float brecha = MathF.Round(actual - esperadoAlMes, 4);

            // Estado: se compara el avance real contra el esperado proporcional al mes
            EstadoIndicador estado;
            if (actual >= metaEfectiva)
                estado = EstadoIndicador.Cumplida;
            else if (actual >= esperadoAlMes)
                estado = EstadoIndicador.EnCurso;
            else if (actual >= esperadoAlMes * 0.90f)
                estado = EstadoIndicador.EnRiesgo;
            else
                estado = EstadoIndicador.Critico;

            return new FilaInforme(ind.Orden, ind.Nombre, metaEfectiva, ind.IsTasa,
                                   actual, esperadoAlMes, brecha, estado, (float)den);
        }

        // ── Helpers de formato ───────────────────────────────────────────────

        private static string FormatTasa(float valor, bool isTasa) =>
            isTasa
                ? $"{valor:F1} "
                : $"{valor * 100f:F1} %";

        private static string FormatVariacion(float variacion, bool isTasa)
        {
            string flecha = variacion >= 0 ? "▲" : "▼";
            float abs = MathF.Abs(variacion);
            string valor = isTasa ? $"{abs:F1} " : $"{abs * 100f:F1} %";
            return $"{flecha} {valor}";
        }

        // Esperado expresado en unidades de producción (num esperado)
        private static string FormatEsperado(float esperado, float den, bool isTasa) =>
            isTasa
                ? $"{esperado:F1}"
                : $"{MathF.Round(esperado * den):N0}";

        // Brecha expresada en unidades de producción (diferencia de num)
        private static string FormatBrechaVsMeta(float brecha, float den, bool isTasa)
        {
            string flecha = brecha >= 0 ? "▲" : "▼";
            return isTasa
                ? $"{flecha} {MathF.Abs(brecha):F1}"
                : $"{flecha} {MathF.Round(MathF.Abs(brecha * den)):N0}";
        }

        private static (string label, string hex) EstadoStyle(EstadoIndicador estado) => estado switch
        {
            EstadoIndicador.Cumplida  => ("Cumplida",  "#1A9E5C"),
            EstadoIndicador.EnCurso   => ("En Curso",  "#1565C0"),
            EstadoIndicador.EnRiesgo  => ("En Riesgo", "#E67E22"),
            EstadoIndicador.Critico   => ("Crítico",   "#CB4335"),
            _                         => ("—",         "#95A5A6")
        };

        // ── Denominador oct–dic del año anterior ─────────────────────────────

        /// <summary>
        /// Para indicadores con período oct(año-1)–sep(año), devuelve la suma de
        /// denominadores de los meses 10, 11 y 12 del indicador equivalente del año anterior
        /// (mismo Tipoindicador y mismo Orden). Devuelve 0 si no existe o el indicador
        /// no usa ese período.
        /// </summary>
        private async Task<decimal> GetDenOctDicPrevAnoAsync(Indicador ind)
        {
            if (!ind.EsPeriodoOctubreSep) return 0m;

            var prevResultados = await _db.Indicador
                .Where(i => i.Año == ind.Año - 1
                         && i.Tipoindicador == ind.Tipoindicador
                         && i.Orden == ind.Orden)
                .SelectMany(i => i.ResultadoIndicadors)
                .Where(r => r.Mes >= 10)
                .ToListAsync();

            return decimal.Round(prevResultados.Sum(r => r.Denominador + r.DenominadorP));
        }

        // ── Helper: página de detalle de un indicador ────────────────────────

        private static void AddDetalleIndicadorPage(
            IDocumentContainer container,
            Indicador indicador,
            int mes,
            int ano,
            IEnumerable<ResultadoIndicador> resultadosSrc,
            decimal denPrevAnoOctDic,
            string ubicacion)
        {
            float metaEfectiva = indicador.IsDenFijo ? 1.0f : indicador.Meta;

            var mesesData = Enumerable.Range(1, mes).Select(m =>
            {
                var hastaM = resultadosSrc.Where(r => r.Mes <= m).ToList();
                decimal num = decimal.Round(hastaM.Sum(r => r.Numerador + r.NumeradorP));
                decimal den = indicador.EsPeriodoOctubreSep
                    ? decimal.Round(hastaM.Where(r => r.Mes < 10).Sum(r => r.Denominador + r.DenominadorP)) + denPrevAnoOctDic
                    : decimal.Round(hastaM.Sum(r => r.Denominador + r.DenominadorP));

                if (indicador.IsDenFijo && den != 0)
                    den = decimal.Round(den * (decimal)indicador.Meta);

                float actual   = den != 0 ? MathF.Round((float)(num / den), 4) : 0f;
                float esperado = indicador.Mensual
                    ? MathF.Round(metaEfectiva * (m / 12f), 4)
                    : MathF.Round(metaEfectiva * (m <= 6 ? 0.5f : 1.0f), 4);

                return (Mes: m, Num: num, Den: den, Actual: actual,
                        Esperado: esperado, Brecha: MathF.Round(actual - esperado, 4));
            }).ToList();

            var currentMesEntry = mesesData[^1];

            if (!indicador.Mensual)
                mesesData = mesesData.Where(m => m.Mes == 6 || m.Mes == 12).ToList();

            var establecimientos = resultadosSrc
                .Where(r => r.Mes <= mes)
                .GroupBy(r => r.id_establecimiento)
                .Select(g =>
                {
                    var first = g.First();
                    decimal num = decimal.Round(g.Sum(r => r.Numerador + r.NumeradorP));
                    decimal den = decimal.Round(g.Sum(r => r.Denominador + r.DenominadorP));

                    if (indicador.IsDenFijo && den != 0)
                        den = decimal.Round(den * (decimal)indicador.Meta);

                    float val = den != 0 ? MathF.Round((float)(num / den), 4) : 0f;
                    return (
                        Nombre: first.Establecimiento?.Nombre ?? "—",
                        Sector: first.Establecimiento?.Sector?.Nombre ?? "—",
                        Num: num, Den: den, Valor: val
                    );
                })
                .OrderBy(e => e.Sector)
                .ThenBy(e => e.Nombre)
                .ToList();

            var fila = BuildFila(indicador, ano, mes, resultadosSrc, denPrevAnoOctDic);
            var (estadoLabel, estadoHex) = EstadoStyle(fila.Estado);

            var dfi = new System.Globalization.CultureInfo("es-CL").DateTimeFormat;
            string NombreMes(int m) { var s = dfi.GetMonthName(m); return char.ToUpper(s[0]) + s[1..]; }
            string nombreMes = NombreMes(mes);

            float[] chartActual   = mesesData.Select(m => m.Actual).ToArray();
            float[] chartEsperado = mesesData.Select(m => m.Esperado).ToArray();
            bool    isTasaCapt    = indicador.IsTasa;

            var mejorMes  = mesesData.Count > 0 ? mesesData.MaxBy(m => m.Actual) : currentMesEntry;
            var peorMes   = mesesData.Count > 0 ? mesesData.MinBy(m => m.Actual) : currentMesEntry;
            float tendencia = mesesData.Count >= 2 ? mesesData[^1].Actual - mesesData[^2].Actual : 0f;

            var establsBajoMeta = establecimientos
                .Where(e => e.Valor < metaEfectiva * 0.9f)
                .OrderBy(e => e.Valor)
                .Take(3)
                .ToList();
            var mejorEstab = establecimientos.Count > 0 ? establecimientos.MaxBy(e => e.Valor) : default;

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(ts => ts.FontFamily("DejaVu Sans").FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item()
                                .Text($"{indicador.Nombre}")
                                .Bold().FontSize(11).FontColor("#0A2342");
                            inner.Item()
                                .Text($"Indicadores {ano}  · {ubicacion}")
                                .FontSize(9).FontColor("#0D3B6E");
                        });
                        row.ConstantItem(120).AlignRight().Column(inner =>
                        {
                            inner.Item()
                                .Text($"Corte: {nombreMes} {ano}")
                                .FontSize(8.5f).FontColor("#3D5A80");
                            inner.Item()
                                .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(7f).FontColor("#607B96");
                        });
                    });
                    col.Item().PaddingTop(4).BorderBottom(1).BorderColor("#1565C0");
                    col.Item().Height(6);
                });

                page.Footer().BorderTop(1).BorderColor("#C5D9F2").PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().AlignLeft().Column(c =>
                    {
                        c.Item().Text("Área de Estadísticas · Departamento de Salud Monte Patria")
                            .FontSize(6.5f).FontColor("#607B96");
                        c.Item().Text("ricardocontreras@mpatria.cl")
                            .FontSize(6f).FontColor("#95A5A6");
                    });
                    row.ConstantItem(100).AlignCenter().AlignMiddle().Text(txt =>
                    {
                        txt.Span("Página ").FontColor("#607B96").FontSize(7.5f);
                        txt.CurrentPageNumber().FontColor("#607B96").FontSize(7.5f);
                        txt.Span(" de ").FontColor("#607B96").FontSize(7.5f);
                        txt.TotalPages().FontColor("#607B96").FontSize(7.5f);
                    });
                    row.RelativeItem().AlignRight().AlignMiddle()
                        .Text("Comentarios o sugerencias: ricardocontreras@mpatria.cl")
                        .FontSize(6f).FontColor("#95A5A6");
                });

                page.Content().Column(col =>
                {
                    // ── Tarjetas KPI ───────────────────────────────────────
                    col.Item().Row(kpiRow =>
                    {
                        static void AddCard(RowDescriptor row, string titulo, string valor,
                            string hexColor, string? sub = null, bool last = false, string? bgColor = null)
                        {
                            row.RelativeItem()
                                .Border(1).BorderColor("#C5D9F2")
                                .Background(bgColor ?? hexColor + "22")
                                .Padding(7)
                                .Column(c =>
                                {
                                    c.Item().Text(titulo).FontSize(6.5f).FontColor("#607B96");
                                    c.Item().Text(valor).Bold().FontSize(11).FontColor(hexColor);
                                    if (sub is not null)
                                        c.Item().Text(sub).FontSize(6f).FontColor("#8AA3BE");
                                });
                            if (!last) row.ConstantItem(4);
                        }

                        var cur = currentMesEntry;
                        AddCard(kpiRow, "Avance Actual",
                            FormatTasa(fila.Actual, indicador.IsTasa), "#1565C0",
                            $"Num: {cur.Num:N0}  ·  Den: {cur.Den:N0}");
                        AddCard(kpiRow, "Meta Anual",
                            FormatTasa(metaEfectiva, indicador.IsTasa), "#1A9E5C",
                            indicador.Mensual ? "Progresión lineal" : "Evaluación semestral");
                        AddCard(kpiRow, $"Esp. {nombreMes}",
                            FormatEsperado(fila.EsperadoAlMes, fila.Den, indicador.IsTasa), "#0277BD",
                            "Avance esperado a este mes");
                        AddCard(kpiRow, "Brecha",
                            FormatBrechaVsMeta(fila.Brecha, fila.Den, indicador.IsTasa),
                            fila.Brecha >= 0 ? "#1A9E5C" : "#CB4335",
                            fila.Brecha >= 0 ? "Adelantado" : "Atrasado",
                            bgColor: fila.Brecha >= 0 ? "#E8F8EE" : "#FDECEA");

                        kpiRow.RelativeItem()
                            .Background(estadoHex)
                            .Padding(7).AlignCenter().AlignMiddle()
                            .Column(c =>
                            {
                                c.Item().Text("Estado").FontSize(6.5f).FontColor(Colors.White);
                                c.Item().Text(estadoLabel).Bold().FontSize(11).FontColor(Colors.White);
                                c.Item()
                                    .Text(indicador.IsTasa
                                        ? $"{fila.Actual:F1} alcanzado"
                                        : $"{fila.Actual / metaEfectiva * 100f:F1}% cumplido")
                                    .FontSize(6f).FontColor(Colors.White);
                            });
                    });

                    col.Item().Height(14);

                    // ── Gráfico de evolución mensual ───────────────────────
                    col.Item()
                        .Text("Evolución Mensual (valores acumulados)")
                        .Bold().FontSize(9).FontColor("#0D3B6E");
                    col.Item().Height(4);

                    col.Item().Height(155).Svg(size =>
                    {
                        var ic = System.Globalization.CultureInfo.InvariantCulture;
                        string F(float v)  => v.ToString("F1", ic);
                        string F0(float v) => v.ToString("F0", ic);

                        const float padL = 38f, padB = 20f, padT = 12f, padR = 14f;
                        float w     = size.Width;
                        float h     = size.Height;
                        float plotW = w - padL - padR;
                        float plotH = h - padB - padT;
                        int   n     = chartActual.Length;

                        if (n == 0)
                            return $"<svg xmlns='http://www.w3.org/2000/svg' width='{F(w)}' height='{F(h)}'/>";

                        float maxVal = MathF.Max(chartActual.Max(), chartEsperado.Max());
                        maxVal = MathF.Max(maxVal, metaEfectiva * 1.08f);
                        if (maxVal <= 0f) maxVal = 1f;

                        float baseY = padT + plotH;
                        float slotW = plotW / (n + 1);
                        float barW  = slotW * 0.48f;
                        float espW  = slotW * 0.26f;

                        float XPix(int i)   => padL + (i + 1) * slotW;
                        float YPix(float v) => padT + plotH - v / maxVal * plotH;

                        var sb = new System.Text.StringBuilder();
                        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{F(w)}' height='{F(h)}' font-family='DejaVu Sans'>");

                        sb.Append($"<rect x='{F(padL)}' y='{F(padT)}' width='{F(plotW)}' height='{F(plotH)}' fill='#F8F9FA'/>");

                        string[] mesAbrev = { "Ene","Feb","Mar","Abr","May","Jun","Jul","Ago","Sep","Oct","Nov","Dic" };
                        for (int g = 0; g <= 4; g++)
                        {
                            float v  = maxVal * g / 4f;
                            float yp = YPix(v);
                            sb.Append($"<line x1='{F(padL)}' y1='{F(yp)}' x2='{F(padL + plotW)}' y2='{F(yp)}' stroke='#C5D9F2' stroke-width='0.5'/>");
                            string lbl = isTasaCapt
                                ? $"{F0(v)}"
                                : $"{F0(v * 100f)}%";
                            sb.Append($"<text x='{F(padL - 3f)}' y='{F(yp + 2.5f)}' font-size='6.5' fill='#607B96' text-anchor='end'>{lbl}</text>");
                        }

                        float yMeta = YPix(metaEfectiva);
                        sb.Append($"<line x1='{F(padL)}' y1='{F(yMeta)}' x2='{F(padL + plotW)}' y2='{F(yMeta)}' stroke='#1A9E5C' stroke-width='1.3' stroke-dasharray='5,3'/>");
                        string metaLbl = isTasaCapt
                            ? $"Meta {F0(metaEfectiva)}"
                            : $"Meta {F0(metaEfectiva * 100f)}%";
                        sb.Append($"<text x='{F(padL + plotW - 52f)}' y='{F(yMeta - 3f)}' font-size='6' fill='#1A9E5C'>{metaLbl}</text>");

                        for (int i = 0; i < n; i++)
                        {
                            float cx      = XPix(i);
                            float hActual = chartActual[i]   / maxVal * plotH;
                            float hEsp    = chartEsperado[i] / maxVal * plotH;

                            sb.Append($"<rect x='{F(cx - barW / 2f)}' y='{F(baseY - hActual)}' width='{F(barW)}' height='{F(hActual)}' fill='rgba(13,59,110,0.85)'/>");
                            sb.Append($"<rect x='{F(cx + barW / 2f + 1.5f)}' y='{F(baseY - hEsp)}' width='{F(espW)}' height='{F(hEsp)}' fill='rgba(100,181,246,0.75)'/>");
                            sb.Append($"<text x='{F(cx)}' y='{F(baseY + 13f)}' font-size='6.5' fill='#3D5A80' text-anchor='middle'>{mesAbrev[mesesData[i].Mes - 1]}</text>");
                        }

                        sb.Append($"<line x1='{F(padL)}' y1='{F(padT)}' x2='{F(padL)}' y2='{F(baseY)}' stroke='#0D3B6E' stroke-width='1'/>");
                        sb.Append($"<line x1='{F(padL)}' y1='{F(baseY)}' x2='{F(padL + plotW)}' y2='{F(baseY)}' stroke='#0D3B6E' stroke-width='1'/>");

                        float lx = padL + 4f, ly = padT + 9f;
                        sb.Append($"<rect x='{F(lx)}' y='{F(ly - 5f)}' width='8' height='5' fill='rgba(13,59,110,0.85)'/>");
                        sb.Append($"<text x='{F(lx + 10f)}' y='{F(ly)}' font-size='6' fill='#0D3B6E'>Avance acumulado</text>");
                        lx += 92f;
                        sb.Append($"<rect x='{F(lx)}' y='{F(ly - 5f)}' width='8' height='5' fill='rgba(100,181,246,0.75)'/>");
                        sb.Append($"<text x='{F(lx + 10f)}' y='{F(ly)}' font-size='6' fill='#0D3B6E'>Esperado al mes</text>");

                        sb.Append("</svg>");
                        return sb.ToString();
                    });

                    col.Item().Height(12);

                    // ── Tabla evolución mensual ────────────────────────────
                    col.Item()
                        .Text(indicador.Mensual ? "Detalle Mensual" : "Evaluación Semestral (Jun / Dic)")
                        .Bold().FontSize(9).FontColor("#0D3B6E");
                    col.Item().Height(4);

                    col.Item().Table(tbl =>
                    {
                        tbl.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(52);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.ConstantColumn(52);
                        });

                        static IContainer TH(IContainer c) =>
                            c.Background("#0D3B6E").Padding(4).AlignCenter().AlignMiddle();
                        static IContainer TD(IContainer c) =>
                            c.Padding(4).AlignMiddle().AlignCenter();

                        tbl.Header(h =>
                        {
                            foreach (var t in new[] { "Mes", "Numerador", "Denominador",
                                "Avance", "Esperado", "Brecha", "Estado" })
                                h.Cell().Element(TH)
                                    .Text(t).Bold().FontColor(Colors.White).FontSize(7.5f);
                        });

                        int ri = 0;
                        foreach (var m in mesesData)
                        {
                            ri++;
                            string bg = ri % 2 == 0 ? "#EEF4FF" : Colors.White;

                            EstadoIndicador rowEst;
                            if      (m.Actual >= metaEfectiva)               rowEst = EstadoIndicador.Cumplida;
                            else if (m.Actual >= m.Esperado)                 rowEst = EstadoIndicador.EnCurso;
                            else if (m.Actual >= m.Esperado * 0.90f)         rowEst = EstadoIndicador.EnRiesgo;
                            else                                             rowEst = EstadoIndicador.Critico;

                            var (rl, rh) = EstadoStyle(rowEst);
                            string mn = NombreMes(m.Mes);

                            tbl.Cell().Background(bg).Element(TD).Text(mn);
                            tbl.Cell().Background(bg).Element(TD).Text($"{m.Num:N0}");
                            tbl.Cell().Background(bg).Element(TD).Text($"{m.Den:N0}");
                            tbl.Cell().Background(bg).Element(TD)
                                .Text(FormatTasa(m.Actual, indicador.IsTasa)).Bold();
                            tbl.Cell().Background(bg).Element(TD)
                                .Text(FormatEsperado(m.Esperado, (float)m.Den, indicador.IsTasa)).FontColor("#3D5A80");
                            tbl.Cell().Background(bg).Element(TD)
                                .Text(FormatBrechaVsMeta(m.Brecha, (float)m.Den, indicador.IsTasa))
                                .FontColor(m.Brecha >= 0 ? "#1A9E5C" : "#CB4335");
                            tbl.Cell().Background(rh).Padding(4).AlignCenter().AlignMiddle()
                                .Text(rl).Bold().FontColor(Colors.White).FontSize(7f);
                        }
                    });

                    col.Item().Height(12);

                    // ── Tabla de establecimientos ──────────────────────────
                    col.Item().Text("Desglose por Establecimiento")
                        .Bold().FontSize(9).FontColor("#0D3B6E");
                    col.Item().Height(4);

                    col.Item().Table(tbl2 =>
                    {
                        tbl2.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3.5f);
                            c.RelativeColumn(1.5f);
                            c.ConstantColumn(55);
                            c.ConstantColumn(60);
                            c.ConstantColumn(65);
                        });

                        static IContainer TH2(IContainer c) =>
                            c.Background("#0D3B6E").Padding(4).AlignCenter().AlignMiddle();

                        tbl2.Header(h =>
                        {
                            foreach (var t in new[] { "Establecimiento", "Sector",
                                "Numerador", "Denominador", "Avance" })
                                h.Cell().Element(TH2)
                                    .Text(t).Bold().FontColor(Colors.White).FontSize(7.5f);
                        });

                        int ri2 = 0;
                        foreach (var e in establecimientos)
                        {
                            ri2++;
                            string bg = ri2 % 2 == 0 ? "#EEF4FF" : Colors.White;
                            string valBg = e.Valor >= metaEfectiva         ? "#E3F7EC"
                                         : e.Valor < metaEfectiva * 0.9f   ? "#FDECEA"
                                         : bg;

                            tbl2.Cell().Background(bg).Padding(4).AlignMiddle()
                                .Text(e.Nombre).FontSize(7.5f);
                            tbl2.Cell().Background(bg).Padding(4).AlignMiddle()
                                .Text(e.Sector).FontSize(7.5f);
                            tbl2.Cell().Background(bg).Padding(4).AlignCenter().AlignMiddle()
                                .Text($"{e.Num:N0}");
                            tbl2.Cell().Background(bg).Padding(4).AlignCenter().AlignMiddle()
                                .Text($"{e.Den:N0}");
                            tbl2.Cell().Background(valBg).Padding(4).AlignCenter().AlignMiddle()
                                .Text(FormatTasa(e.Valor, indicador.IsTasa)).Bold();
                        }
                    });

                    col.Item().Height(12);

                    // ── Panel de análisis ──────────────────────────────────
                    col.Item()
                        .BorderLeft(3).BorderColor("#1565C0")
                        .PaddingLeft(8)
                        .Column(ins =>
                        {
                            ins.Item()
                                .Text("Análisis del indicador")
                                .Bold().FontSize(9).FontColor("#0A2342");
                            ins.Item().Height(5);

                            static void Insight(ColumnDescriptor ins,
                                string icono, string texto, string hexColor = "#0D3B6E")
                            {
                                ins.Item().Row(r =>
                                {
                                    r.ConstantItem(13).Text(icono).FontSize(8);
                                    r.RelativeItem().Text(texto).FontSize(7.5f).FontColor(hexColor);
                                });
                                ins.Item().Height(3);
                            }

                            float pct = metaEfectiva > 0
                                ? fila.Actual / metaEfectiva * 100f : 0f;
                            string cumplimientoTxt = indicador.IsTasa
                                ? $"Cumplimiento a {nombreMes}: {FormatTasa(fila.Actual, true)} de {FormatTasa(metaEfectiva, true)}."
                                : $"Cumplimiento a {nombreMes}: {pct:F1}% ({FormatTasa(fila.Actual, false)} de {FormatTasa(metaEfectiva, false)}).";
                            Insight(ins, "▶", cumplimientoTxt);

                            if (tendencia != 0f)
                            {
                                string tendTxt = tendencia >= 0
                                    ? $"Tendencia positiva respecto al mes anterior: {FormatBrechaVsMeta(tendencia, fila.Den, indicador.IsTasa)}."
                                    : $"Tendencia negativa respecto al mes anterior: {FormatBrechaVsMeta(tendencia, fila.Den, indicador.IsTasa)}.";
                                Insight(ins, tendencia >= 0 ? "↑" : "↓", tendTxt,
                                    tendencia >= 0 ? "#1A9E5C" : "#CB4335");
                            }

                            Insight(ins, "★",
                                $"Mejor desempeño acumulado: {NombreMes(mejorMes.Mes)} " +
                                $"({FormatTasa(mejorMes.Actual, indicador.IsTasa)}).",
                                "#1A9E5C");

                            if (peorMes.Mes != mejorMes.Mes)
                                Insight(ins, "▼",
                                    $"Menor desempeño acumulado: {NombreMes(peorMes.Mes)} " +
                                    $"({FormatTasa(peorMes.Actual, indicador.IsTasa)}).",
                                    "#CB4335");

                            if (mejorEstab.Nombre is not null && mejorEstab.Nombre != "—")
                                Insight(ins, "⊕",
                                    $"Establecimiento destacado: {mejorEstab.Nombre} " +
                                    $"({FormatTasa(mejorEstab.Valor, indicador.IsTasa)}).",
                                    "#1565C0");

                            if (establsBajoMeta.Count > 0)
                                Insight(ins, "⚠",
                                    $"Establecimientos bajo 90% de meta: " +
                                    $"{string.Join(", ", establsBajoMeta.Select(e => e.Nombre))}.",
                                    "#CB4335");
                            else if (establecimientos.Count > 0)
                                Insight(ins, "✓",
                                    "Todos los establecimientos superan el 90% de la meta.",
                                    "#1A9E5C");

                            if (indicador.Detalle is not null)
                            {
                                ins.Item().Height(4);
                                Insight(ins, "ℹ", indicador.Detalle, "#3D5A80");
                            }
                        });
                });
            });
        }

        // ── Endpoint ─────────────────────────────────────────────────────────

        [HttpGet("informeMensual/{tipoIndicador:int}/{ano:int}")]
        public async Task<IActionResult> InformeMensual(
            int tipoIndicador, int ano,
            [FromQuery] long? sectorId,
            [FromQuery] long? establecimientoId)
        {
            var establecimiento = establecimientoId.HasValue? _db.Establecimiento.FirstOrDefault(e=>e.Id == establecimientoId.Value):null;
            var sector = sectorId.HasValue ? _db.Sector.FirstOrDefault(s => s.Id == sectorId.Value):null;

            var indicadores = await _db.Indicador
                .Where(i => i.Año == ano && i.Tipoindicador == tipoIndicador)
                .Include(i => i.ResultadoIndicadors)
                    .ThenInclude(r => r.Establecimiento)
                        .ThenInclude(e => e.Sector)
                .OrderBy(i => i.Orden)
                .ToListAsync();

            if (!indicadores.Any())
                return NotFound($"No se encontraron indicadores para el año {ano}.");

            int mes = indicadores
                .SelectMany(i => i.ResultadoIndicadors)
                .Where(r => !EstablecimientosExcluidos.Contains(r.id_establecimiento))
                .Where(r => !sectorId.HasValue || r.Establecimiento?.id_sector == sectorId.Value)
                .Where(r => !establecimientoId.HasValue || r.id_establecimiento == establecimientoId.Value)
                .Select(r => r.Mes)
                .DefaultIfEmpty(0)
                .Max();

            if (mes == 0)
                return NotFound($"No hay datos cargados para el año {ano}.");

            // Pre-fetch denominadores oct–dic del año anterior para indicadores con período oct–sep
            var octSepOrden = indicadores
                .Where(i => i.EsPeriodoOctubreSep)
                .Select(i => i.Orden)
                .Distinct()
                .ToList();

            Dictionary<int, decimal> denPrevAnoMap = new();
            if (octSepOrden.Count > 0)
            {
                var prevResultados = await _db.Indicador
                    .Where(i => i.Año == ano - 1
                             && i.Tipoindicador == indicadores[0].Tipoindicador
                             && octSepOrden.Contains(i.Orden))
                    .Select(i => new
                    {
                        i.Orden,
                        Resultados = i.ResultadoIndicadors
                            .Where(r => r.Mes >= 10 && !EstablecimientosExcluidos.Contains(r.id_establecimiento))
                            .Select(r => new
                            {
                                r.id_establecimiento,
                                SectorId = r.Establecimiento != null ? r.Establecimiento.id_sector : (long?)null,
                                Denominador = r.Denominador + r.DenominadorP
                            })
                    })
                    .ToListAsync();

                denPrevAnoMap = prevResultados.ToDictionary(
                    x => x.Orden,
                    x =>
                    {
                        var res = x.Resultados.AsEnumerable();
                        if (sectorId.HasValue)
                            res = res.Where(r => r.SectorId == sectorId.Value);
                        if (establecimientoId.HasValue)
                            res = res.Where(r => r.id_establecimiento == establecimientoId.Value);
                        return decimal.Round(res.Sum(r => r.Denominador));
                    });
            }

            var filas = indicadores
                .Select(i =>
                {
                    var src = i.ResultadoIndicadors.AsEnumerable()
                        .Where(r => !EstablecimientosExcluidos.Contains(r.id_establecimiento));
                    if (sectorId.HasValue)
                        src = src.Where(r => r.Establecimiento?.id_sector == sectorId.Value);
                    if (establecimientoId.HasValue)
                        src = src.Where(r => r.id_establecimiento == establecimientoId.Value);
                    decimal denPrev = denPrevAnoMap.GetValueOrDefault(i.Orden, 0m);
                    return BuildFila(i, ano, mes, src, denPrev);
                })
                .ToList();
            if(filas.Count==0)
                return NotFound($"No hay datos para el sector o establecimiento seleccionado.");

            var dfi = new System.Globalization.CultureInfo("es-CL").DateTimeFormat;
            string NombreMes(int m) { var s = dfi.GetMonthName(m); return char.ToUpper(s[0]) + s[1..]; }
            string nombreMes = NombreMes(mes);
            
            

            var titulo = "Red Salud Comunal Monte Patria";
            if (sector!=null)
            {
                titulo = "Sector " + sector.Nombre;
            }
            if (establecimiento!=null)
            {
                titulo = "Establecimiento: " + establecimiento.Nombre;
            }
            
           

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(ts => ts.FontFamily("DejaVu Sans").FontSize(8));

                    // ── Encabezado ─────────────────────────────────────────
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item()
                                    .Text(titulo)
                                    .Bold().FontSize(13).FontColor("#0A2342");
                                inner.Item()
                                    .Text($"Informe de Avance Indicadores {ano}")
                                    .FontSize(10).FontColor("#0D3B6E");
                            });
                            row.ConstantItem(130).AlignRight().Column(inner =>
                            {
                                inner.Item()
                                    .Text($"Corte: {nombreMes} {ano}")
                                    .FontSize(9).FontColor("#3D5A80");
                                inner.Item()
                                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(7.5f).FontColor("#607B96");
                            });
                        });
                        col.Item().PaddingTop(4)
                            .BorderBottom(1).BorderColor("#1565C0");
                        col.Item().Height(6);
                    });

                    // ── Pie de página ──────────────────────────────────────
                    page.Footer().BorderTop(1).BorderColor("#C5D9F2").PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Column(c =>
                        {
                            c.Item().Text("Área de Estadísticas · Departamento de Salud Monte Patria")
                                .FontSize(6.5f).FontColor("#607B96");
                            c.Item().Text("ricardocontreras@mpatria.cl")
                                .FontSize(6f).FontColor("#95A5A6");
                        });
                        row.ConstantItem(100).AlignCenter().AlignMiddle().Text(txt =>
                        {
                            txt.Span("Página ").FontColor("#607B96").FontSize(7.5f);
                            txt.CurrentPageNumber().FontColor("#607B96").FontSize(7.5f);
                            txt.Span(" de ").FontColor("#607B96").FontSize(7.5f);
                            txt.TotalPages().FontColor("#607B96").FontSize(7.5f);
                        });
                        row.RelativeItem().AlignRight().AlignMiddle()
                            .Text("Comentarios o sugerencias: ricardocontreras@mpatria.cl")
                            .FontSize(6f).FontColor("#95A5A6");
                    });

                    // ── Contenido ──────────────────────────────────────────
                    page.Content().Column(col =>
                    {
                        // Tabla principal
                        col.Item().Table(table =>
                        {
                            // Definición de columnas
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(22);   // N°
                                cols.RelativeColumn(3.5f); // Indicador
                                cols.ConstantColumn(52);   // Meta
                                cols.ConstantColumn(62);   // Avance actual
                                cols.ConstantColumn(65);   // Estado
                            });

                            // Cabecera de tabla
                            static IContainer HeaderCell(IContainer c) =>
                                c.Background("#0D3B6E")
                                 .Padding(4)
                                 .AlignCenter()
                                 .AlignMiddle();

                            table.Header(h =>
                            {
                                foreach (var titulo in new[] { "N°", "Indicador", "Meta",
                                    "Avance actual", "Estado" })
                                {
                                    h.Cell().Element(HeaderCell)
                                        .Text(titulo).FontColor(Colors.White).Bold().FontSize(7.5f);
                                }
                            });

                            // Filas de datos
                            int rowIdx = 0;
                            foreach (var fila in filas)
                            {
                                rowIdx++;
                                string rowBg = rowIdx % 2 == 0 ? "#EEF4FF" : Colors.White;

                                static IContainer DataCell(IContainer c, string bg) =>
                                    c.Background(bg).Padding(4).AlignMiddle();

                                var (estadoLabel, estadoHex) = EstadoStyle(fila.Estado);
                                string brechaHex = fila.Brecha >= 0 ? "#1A9E5C" : "#CB4335";

                                table.Cell().Element(c => DataCell(c, rowBg))
                                    .AlignCenter().Text($"{fila.Orden}").FontColor("#555555");

                                table.Cell().Element(c => DataCell(c, rowBg))
                                    .Text(fila.Nombre).FontSize(7.5f);

                                table.Cell().Element(c => DataCell(c, rowBg))
                                    .AlignCenter()
                                    .Text(FormatTasa(fila.Meta, fila.IsTasa)).Bold();

                                table.Cell().Element(c => DataCell(c, rowBg))
                                    .AlignCenter()
                                    .Text(FormatTasa(fila.Actual, fila.IsTasa));

                                table.Cell()
                                    .Background(estadoHex)
                                    .Padding(4).AlignCenter().AlignMiddle()
                                    .Text(estadoLabel)
                                    .Bold().FontColor(Colors.White).FontSize(7.5f);
                            }
                        });

                        // Sección de alertas
                        var alertas = filas
                            .Where(f => f.Estado == EstadoIndicador.Critico
                                     || f.Estado == EstadoIndicador.EnRiesgo)
                            .OrderBy(f => f.Estado)
                            .ThenBy(f => f.Orden)
                            .ToList();

                        if (alertas.Any())
                        {
                            col.Item().Height(14);
                            col.Item()
                                .BorderLeft(3).BorderColor("#CB4335")
                                .PaddingLeft(8)
                                .Column(alertCol =>
                                {
                                    alertCol.Item()
                                        .Text("⚠ Indicadores que requieren atención")
                                        .Bold().FontSize(9).FontColor("#CB4335");

                                    alertCol.Item().Height(4);

                                    foreach (var a in alertas)
                                    {
                                        var (lbl, hex) = EstadoStyle(a.Estado);
                                        alertCol.Item().Row(row =>
                                        {
                                            row.ConstantItem(60)
                                                .Background(hex)
                                                .Padding(3).AlignCenter()
                                                .Text(lbl).Bold().FontColor(Colors.White).FontSize(7f);

                                            row.RelativeItem().PaddingLeft(6).AlignMiddle()
                                                .Text(txt =>
                                                {
                                                    txt.Span($"{a.Nombre}  ").Bold().FontSize(7.5f);
                                                    txt.Span($"Avance: {FormatTasa(a.Actual, a.IsTasa)}")
                                                                       .FontColor("#3D5A80").FontSize(7.5f);
                                                });
                                        });
                                        alertCol.Item().Height(3);
                                    }
                                                     });
                                            }
                                        });
                                    });

                                    // ── Páginas de detalle por indicador ─────────────────────────
                                    foreach (var ind in indicadores)
                                    {
                                        var src = ind.ResultadoIndicadors.AsEnumerable()
                                            .Where(r => !EstablecimientosExcluidos.Contains(r.id_establecimiento));
                                        if (sectorId.HasValue)
                                            src = src.Where(r => r.Establecimiento?.id_sector == sectorId.Value);
                                        if (establecimientoId.HasValue)
                                            src = src.Where(r => r.id_establecimiento == establecimientoId.Value);

                                        decimal denPrev = denPrevAnoMap.GetValueOrDefault(ind.Orden, 0m);
                                        AddDetalleIndicadorPage(container, ind, mes, ano, src, denPrev, titulo);
                                    }
                                });

            var nombreArchivo = $"InformeMensual_{ano}_{mes:D2}.pdf";
            if(sector!=null)
            {
                nombreArchivo = $"InformeMensual_{sector.Nombre}_{ano}_{mes:D2}.pdf";
            }
            if (establecimiento!=null)
            {
                nombreArchivo = $"InformeMensual_{establecimiento.Nombre}_{ano}_{mes:D2}.pdf";
            }
            var pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        // ── Detalle por indicador ────────────────────────────────────────────

        [HttpGet("detalleIndicador/{id:int}")]
        public async Task<IActionResult> DetalleIndicador(
            int id,

            [FromQuery] long? sectorId,
            [FromQuery] long? establecimientoId)
        {
            var indicador = await _db.Indicador
                .Where(i => i.Id == id)
                .Include(i => i.ResultadoIndicadors.Where(e => !EstablecimientosExcluidos.Contains(e.id_establecimiento)))
                    .ThenInclude(r => r.Establecimiento)
                        .ThenInclude(e => e.Sector)
                .FirstOrDefaultAsync();

            if (indicador is null)
                return NotFound($"No se encontró el indicador con ID {id}.");

            var resultadosSrc = indicador.ResultadoIndicadors.AsEnumerable();
            if (establecimientoId.HasValue)
                resultadosSrc = resultadosSrc.Where(r => r.id_establecimiento == establecimientoId.Value);
            else if (sectorId.HasValue)
                resultadosSrc = resultadosSrc.Where(r => r.Establecimiento?.id_sector == sectorId.Value);

            // Mes de corte = máximo mes reportado en el año, independiente del filtro aplicado.
            // EstablecimientosExcluidos ya están excluidos por el filtro del Include.
            int mes = indicador.ResultadoIndicadors
                .Select(r => r.Mes)
                .DefaultIfEmpty(0)
                .Max();

            if (mes == 0)
                return NotFound($"No hay datos cargados para el indicador con ID {id}.");

            int ano = indicador.Año;

            // Denominador oct–dic del año anterior (sólo para indicadores con período oct–sep)
            decimal denPrevAnoOctDic = await GetDenOctDicPrevAnoAsync(indicador);
            float metaEfectiva = indicador.IsDenFijo ? 1.0f : indicador.Meta;

            // ── Datos acumulados por mes
            var mesesData = Enumerable.Range(1, mes).Select(m =>
            {
                var hastaM = resultadosSrc.Where(r => r.Mes <= m).ToList();
                decimal num = decimal.Round(hastaM.Sum(r => r.Numerador + r.NumeradorP));
                decimal den = indicador.EsPeriodoOctubreSep
                    ? decimal.Round(hastaM.Where(r => r.Mes < 10).Sum(r => r.Denominador + r.DenominadorP)) + denPrevAnoOctDic
                    : decimal.Round(hastaM.Sum(r => r.Denominador + r.DenominadorP));

                if (indicador.IsDenFijo && den != 0)
                    den = decimal.Round(den * (decimal)indicador.Meta);

                float actual   = den != 0 ? MathF.Round((float)(num / den), 4) : 0f;
                float esperado = indicador.Mensual
                    ? MathF.Round(metaEfectiva * (m / 12f), 4)
                    : MathF.Round(metaEfectiva * (m <= 6 ? 0.5f : 1.0f), 4);

                return (Mes: m, Num: num, Den: den, Actual: actual,
                        Esperado: esperado, Brecha: MathF.Round(actual - esperado, 4));
            }).ToList();

            // Preservar el dato del mes de corte antes del posible filtrado semestral
            // (se usa en las tarjetas KPI para mostrar num/den del corte exacto)
            var currentMesEntry = mesesData[^1];

            // Los indicadores semestrales solo se evalúan en junio y diciembre
            if (!indicador.Mensual)
                mesesData = mesesData.Where(m => m.Mes == 6 || m.Mes == 12).ToList();

            // ── Datos por establecimiento ──────────────────────────────────────
            var establecimientos = resultadosSrc
                .Where(r => r.Mes <= mes)
                .GroupBy(r => r.id_establecimiento)
                .Select(g =>
                {
                    var first = g.First();
                    decimal num = decimal.Round(g.Sum(r => r.Numerador + r.NumeradorP));
                    decimal den = decimal.Round(g.Sum(r => r.Denominador + r.DenominadorP));

                    if (indicador.IsDenFijo && den != 0)
                        den = decimal.Round(den * (decimal)indicador.Meta);

                    float val   = den != 0 ? MathF.Round((float)(num / den), 4) : 0f;
                    return (
                        Nombre: first.Establecimiento?.Nombre ?? "—",
                        Sector: first.Establecimiento?.Sector?.Nombre ?? "—",
                        Num: num, Den: den, Valor: val
                    );
                })
                .OrderBy(e => e.Sector)
                .ThenBy(e => e.Nombre)
                .ToList();

            // ── Estado al mes de corte ─────────────────────────────────────────
            var fila = BuildFila(indicador, ano, mes, resultadosSrc, denPrevAnoOctDic);
            var (estadoLabel, estadoHex) = EstadoStyle(fila.Estado);

            var dfi = new System.Globalization.CultureInfo("es-CL").DateTimeFormat;
            string NombreMes(int m) { var s = dfi.GetMonthName(m); return char.ToUpper(s[0]) + s[1..]; }
            string nombreMes = NombreMes(mes);

            // ── Arrays para el gráfico ─────────────────────────────────────────
            float[] chartActual   = mesesData.Select(m => m.Actual).ToArray();
            float[] chartEsperado = mesesData.Select(m => m.Esperado).ToArray();
            float   metaVal       = metaEfectiva;
            bool    isTasaCapt    = indicador.IsTasa;

            // ── Insights ──────────────────────────────────────────────────────
            var mejorMes  = mesesData.Count > 0 ? mesesData.MaxBy(m => m.Actual) : currentMesEntry;
            var peorMes   = mesesData.Count > 0 ? mesesData.MinBy(m => m.Actual) : currentMesEntry;
            float tendencia = mesesData.Count >= 2 ? mesesData[^1].Actual - mesesData[^2].Actual : 0f;

            var establsBajoMeta = establecimientos
                .Where(e => e.Valor < metaEfectiva * 0.9f)
                .OrderBy(e => e.Valor)
                .Take(3)
                .ToList();
            var mejorEstab = establecimientos.Count > 0 ? establecimientos.MaxBy(e => e.Valor) : default;

            var ubicacion = "Red Salud Comunal Monte Patria";

            if (sectorId.HasValue && sectorId.Value != 0)
            {
                var sector = _db.Sector.Find(sectorId.Value);
                ubicacion = "Sector " + sector.Nombre;
            }
            if (establecimientoId.HasValue && establecimientoId.Value != 0)
            {
                var establecimiento = _db.Establecimiento.Find(establecimientoId.Value);
                ubicacion = "Establecimiento: " + establecimiento.Nombre;
            }

            // ── PDF ───────────────────────────────────────────────────────────
            var pdf = Document.Create(container =>
            {
                AddDetalleIndicadorPage(container, indicador, mes, ano, resultadosSrc, denPrevAnoOctDic, ubicacion);
            });

            var pdfBytes2 = pdf.GeneratePdf();
            return File(pdfBytes2, "application/pdf",
                        $"Detalle_Indicador_{id}_{ano}_{mes:D2}.pdf");
        }
    }
}
