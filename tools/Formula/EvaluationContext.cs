using RemTool.Shared;
namespace RemTools
{
    public class EvaluationContext
    {
        public int Año { get; set; }
        public int Mes { get; set; }
        public int? EstablecimientoId { get; set; }
        public IDataProvider DataProvider { get; set; }
        /// <summary>Filtra la evaluación a una única serie ("A" o "P"). Null = ambas.</summary>
        public string SoloSerie { get; set; }
    }
}
