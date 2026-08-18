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
            var Metas = Bdd.Indicador.Where(i => i.Año == año).OrderBy(m => m.Orden).ToList();
            var Establecimientos = Bdd.Establecimiento.ToList();

            Bdd.ResultadoIndicador.Include(r => r.Indicador).Where(r => r.Indicador.Año == año).ExecuteDelete();
            Bdd.SaveChanges();

            foreach (var meta in Metas)
            {
                Console.WriteLine(meta.Nombre);

                var ast = ParseNode(JsonElement.Parse(meta.Formula));
                var (astNumerador, astDenominador) = Split(ast);

                var DataProvider = new RemDataProvider(Bdd);

                var collectCtx = new EvaluationContext { Año = año, EstablecimientoId = null, Mes = 1, DataProvider = DataProvider };
                ast.Collect(collectCtx);

                DataProvider.CargarPrestaciones(año, DateTime.Now);
                DataProvider.CargarFonasa(año);
                DataProvider.CargarPercapitaSsc(año);

                var ResultadosIndicadores = new List<ResultadoIndicador>();

                
                int MesMax = año == DateTime.Now.Year
                    ? Bdd.Registro
                        .Where(r => r.Prestacion.VersionRem.Fecha.Year == año)
                        .Max(r => r.Reporte.Mes)
                    : 12;

                
                foreach (var Establecimiento in Establecimientos)
                {
                    // Contexto para el corte P (mes de aplicación: 1 si fallback año anterior, 6 o 12 si corte corriente)
                    var ctxP = new EvaluationContext
                    {
                        Año = año,
                        EstablecimientoId = (int)Establecimiento.Id,
                        Mes = DataProvider.PMesAplicacion,
                        DataProvider = DataProvider,
                        SoloSerie = "P"
                    };
                    decimal numeradorP = astNumerador.Evaluate(ctxP);
                    decimal denominadorP = astDenominador.Evaluate(ctxP);

                    // Para IsDenFijo también usamos la serie A en el corte
                    decimal denominadorFijo = 0;
                    if (meta.IsDenFijo)
                    {
                        var ctxDenFijo = new EvaluationContext
                        {
                            Año = año,
                            EstablecimientoId = (int)Establecimiento.Id,
                            Mes = DataProvider.PMonth,
                            DataProvider = DataProvider,
                            SoloSerie = "A"
                        };
                        denominadorFijo = astDenominador.Evaluate(ctxDenFijo);
                    }

                    for (int mes = 1; mes <= 12; mes++)
                    {
                        if (mes > MesMax) break;

                        var ctx = new EvaluationContext
                        {
                            Año = año,
                            EstablecimientoId = (int)Establecimiento.Id,
                            Mes = mes,
                            DataProvider = DataProvider,
                            SoloSerie = "A"
                        };

                        var nuevoResultado = new ResultadoIndicador
                        {
                            Mes = mes,
                            id_indicador = meta.Id,
                            id_establecimiento = Establecimiento.Id,
                            Numerador = astNumerador.Evaluate(ctx),
                            Denominador = meta.IsDenFijo ? (mes == 1 ? denominadorFijo : 0m) : astDenominador.Evaluate(ctx),
                            NumeradorP = mes == DataProvider.PMesAplicacion ? numeradorP : 0m,
                            DenominadorP = mes == DataProvider.PMesAplicacion ? denominadorP : 0m
                        };


                        // Override manual: pie diabético
                        if(meta.Id == 19)
                        {
                            nuevoResultado.NumeradorP = 0;
                            nuevoResultado.Denominador = nuevoResultado.DenominadorP;
                            nuevoResultado.DenominadorP = 0;
   
                        }
                        if (mes == 1 && meta.Id == 19)
                        {
                            nuevoResultado.Mes = 4;
                            switch (Establecimiento.Id)
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

                        ResultadosIndicadores.Add(nuevoResultado);
                    }
                }
                Bdd.AddRange(ResultadosIndicadores);
                Bdd.SaveChanges();
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
