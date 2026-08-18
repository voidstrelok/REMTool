namespace RemTool.Shared
{
    public class PuntoResumen
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Expresion { get; set; } = string.Empty;
        public long IdSerieRem { get; set; }
        public virtual SerieRem SerieRem { get; set; } = null!;
    }
}
