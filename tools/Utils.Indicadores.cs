using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using RemTool.Shared;

namespace RemTools
{
    public partial class Utils
    {
        public void CalcularIndicadores(int año)
        {
            var metas = Bdd.Indicador
                .AsNoTracking()
                .Where(i => i.Año == año)
                .OrderBy(m => m.Orden)
                .ToList();
            var establecimientos = Bdd.Establecimiento
                .AsNoTracking()
                .ToList();

            Bdd.ResultadoIndicador
                .Where(r => r.Indicador.Año == año)
                .ExecuteDelete();
            Bdd.ChangeTracker.Clear();

            var metasPreparadas = new List<(Indicador Meta, AstNode Numerador, AstNode Denominador)>();
            foreach (var meta in metas)
            {
                var ast = ParseNode(JsonElement.Parse(meta.Formula));
                var (astNumerador, astDenominador) = Split(ast);
                metasPreparadas.Add((meta, astNumerador, astDenominador));
            }

            var dataProvider = new RemDataProvider(Bdd);
            var seriesMensuales = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "A",
                "BM",
                "D"
            };
            var serieP = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "P"
            };
            foreach (var preparada in metasPreparadas)
            {
                var collectCtx = new EvaluationContext
                {
                    Año = año,
                    EstablecimientoId = null,
                    Mes = 1,
                    DataProvider = dataProvider
                };
                preparada.Numerador.Collect(collectCtx);
                preparada.Denominador.Collect(collectCtx);
            }

            dataProvider.CargarPrestaciones(año, DateTime.Now);
            dataProvider.CargarFonasa(año);
            dataProvider.CargarPercapitaSsc(año);

            int mesMax = año == DateTime.Now.Year
                ? Bdd.Reporte
                    .AsNoTracking()
                    .Where(reporte => reporte.Año == año
                        && reporte.Registros.Any(registro =>
                            registro.Prestacion.VersionRem.SerieRem.Nombre == "A"
                            || registro.Prestacion.VersionRem.SerieRem.Nombre == "BM"
                            || registro.Prestacion.VersionRem.SerieRem.Nombre == "D"
                            || registro.Prestacion.VersionRem.SerieRem.Nombre == "P"))
                    .Select(reporte => (int?)reporte.Mes)
                    .Max() ?? 0
                : 12;

            foreach (var preparada in metasPreparadas)
            {
                var meta = preparada.Meta;
                Console.WriteLine(meta.Nombre);
                var resultadosIndicadores = new List<ResultadoIndicador>();

                foreach (var establecimiento in establecimientos)
                {
                    var ctxP = new EvaluationContext
                    {
                        Año = año,
                        EstablecimientoId = (int)establecimiento.Id,
                        Mes = dataProvider.PMesAplicacion,
                        DataProvider = dataProvider,
                        SeriesIncluidas = serieP
                    };
                    decimal numeradorP = preparada.Numerador.Evaluate(ctxP);
                    decimal denominadorP = preparada.Denominador.Evaluate(ctxP);

                    decimal denominadorFijo = 0;
                    if (meta.IsDenFijo)
                    {
                        var ctxDenFijo = new EvaluationContext
                        {
                            Año = año,
                            EstablecimientoId = (int)establecimiento.Id,
                            Mes = dataProvider.PMonth,
                            DataProvider = dataProvider,
                            SeriesIncluidas = seriesMensuales
                        };
                        denominadorFijo = preparada.Denominador.Evaluate(ctxDenFijo);
                    }

                    for (int mes = 1; mes <= 12; mes++)
                    {
                        if (mes > mesMax) break;

                        var ctx = new EvaluationContext
                        {
                            Año = año,
                            EstablecimientoId = (int)establecimiento.Id,
                            Mes = mes,
                            DataProvider = dataProvider,
                        SeriesIncluidas = seriesMensuales
                        };

                        var nuevoResultado = new ResultadoIndicador
                        {
                            Mes = mes,
                            id_indicador = meta.Id,
                            id_establecimiento = establecimiento.Id,
                            Numerador = preparada.Numerador.Evaluate(ctx),
                            Denominador = meta.IsDenFijo ? (mes == 1 ? denominadorFijo : 0m) : preparada.Denominador.Evaluate(ctx),
                            NumeradorP = mes == dataProvider.PMesAplicacion ? numeradorP : 0m,
                            DenominadorP = mes == dataProvider.PMesAplicacion ? denominadorP : 0m
                        };

                        // Override manual: pie diabético
                        if (meta.Id == 19)
                        {
                            nuevoResultado.NumeradorP = 0;
                            nuevoResultado.Denominador = nuevoResultado.DenominadorP;
                            nuevoResultado.DenominadorP = 0;
                        }
                        if (mes == 1 && meta.Id == 19)
                        {
                            nuevoResultado.Mes = 4;
                            switch (establecimiento.Id)
                            {
                                case 13:
                                    nuevoResultado.Numerador = 408;
                                    break;
                                case 3:
                                    nuevoResultado.Numerador = 205;
                                    break;
                                case 5:
                                    nuevoResultado.Numerador = 0;
                                    break;
                                case 8:
                                    nuevoResultado.Numerador = 394;
                                    break;
                            }
                        }

                        resultadosIndicadores.Add(nuevoResultado);
                    }
                }

                Bdd.AddRange(resultadosIndicadores);
                Bdd.SaveChanges();
                Bdd.ChangeTracker.Clear();
            }
        }

        public (AstNode Numerador, AstNode Denominador) Split(AstNode root)
        {
            if (root is OpNode op && op.Op == "div")
                return (op.Args[0], op.Args[1]);

            return (root, new NumberNode { Value = 1 });
        }

        private AstNode ParseNode(JsonElement el)
        { 
            return el.GetProperty("type").GetString() switch
            {
                "value" => new ValueNode
                {
                    Prestacion = el.GetProperty("prestacion").GetString(),
                    Columna = el.GetProperty("columna").GetInt32()
                },
                "number" => new NumberNode
                {
                    Value = el.GetProperty("value").GetDecimal()
                },
                "variable" => new VariableNode
                {
                    Name = el.GetProperty("name").GetString(),
                    Filters = el.TryGetProperty("filters", out var f)
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(f)
                        : new()
                },
                "op" => new OpNode
                {
                    Op = el.GetProperty("op").GetString(),
                    Args = el.GetProperty("args").EnumerateArray().Select(ParseNode).ToList()
                },
                var t => throw new Exception($"Nodo desconocido: {t}")
            };
        }
    }
}
