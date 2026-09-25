using RemTool.Shared;
namespace RemTools
{
    public interface IDataProvider
    {
        decimal GetPrestacionValue(string prestacion, int columna, EvaluationContext ctx);
        decimal ResolveVariable(string variableName, Dictionary<string, object> filters, EvaluationContext ctx);
        void CollectPrestacion(string prestacion);
    }
}
