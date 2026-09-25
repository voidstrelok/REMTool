using RemTool.Shared;
namespace RemTools
{
    public class EvaluationContext
    {
        public int Año { get; set; }
        public int Mes { get; set; }
        public int? EstablecimientoId { get; set; }
        public required IDataProvider DataProvider { get; init; }
        /// <summary>Series que pueden participar en la evaluación. Null = todas.</summary>
        public IReadOnlySet<string>? SeriesIncluidas { get; set; }
    }
}
