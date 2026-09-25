namespace RemTools
{
    public class NumberNode : AstNode
    {
        public decimal Value { get; set; }

        public override decimal Evaluate(EvaluationContext ctx) => Value;

        public override void Collect(EvaluationContext ctx) { }
    }
}
