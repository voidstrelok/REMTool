using RemTool.Shared;
namespace RemTools
{
    public class ValueNode : AstNode
    {
        public string Prestacion { get; set; }
        public int Columna { get; set; }

        public override decimal Evaluate(EvaluationContext ctx) =>
            ctx.DataProvider.GetPrestacionValue(Prestacion, Columna, ctx);

        public override void Collect(EvaluationContext ctx) =>
            ctx.DataProvider.CollectPrestacion(Prestacion);
    }
}
