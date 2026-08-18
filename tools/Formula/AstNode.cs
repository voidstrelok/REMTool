namespace RemTools
{
    public abstract class AstNode
    {
        public abstract decimal Evaluate(EvaluationContext ctx);
        public abstract void Collect(EvaluationContext ctx);
    }

    public class OpNode : AstNode
    {
        public string Op { get; set; }
        public List<AstNode> Args { get; set; }

        public override decimal Evaluate(EvaluationContext ctx) => Op switch
        {
            "sum" => Args.Sum(a => a.Evaluate(ctx)),
            "sub" => Args[0].Evaluate(ctx) - Args[1].Evaluate(ctx),
            "mul" => Args[0].Evaluate(ctx) * Args[1].Evaluate(ctx),
            "div" => SafeDivide(Args[0].Evaluate(ctx), Args[1].Evaluate(ctx)),
            _ => throw new InvalidOperationException($"Operador desconocido {Op}")
        };

        public override void Collect(EvaluationContext ctx) => Args.ForEach(a => a.Collect(ctx));

        private static decimal SafeDivide(decimal a, decimal b) => b == 0 ? 0 : a / b;
    }
}
