namespace RemTools
{
    public class VariableNode : AstNode
    {
        public string Name { get; set; }
        public Dictionary<string, object> Filters { get; set; } = new();

        public override decimal Evaluate(EvaluationContext ctx) =>
            ctx.DataProvider.ResolveVariable(Name, Filters, ctx);

        public override void Collect(EvaluationContext ctx) { }
    }
}
