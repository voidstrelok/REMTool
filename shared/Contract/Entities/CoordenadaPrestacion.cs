namespace RemTool.Shared
{
    public partial class CoordenadaPrestacion
    {
        public int Id { get; set; }

        public int IdPrestacion { get; set; }

        public int IdCelda { get; set; }

        public int Orden { get; set; }

        public string CodigoColumna { get; set; } = String.Empty;

        public string CeldaDiccionario { get; set; } = String.Empty;

        public string CeldaBase { get; set; } = String.Empty;

        public virtual Prestacion Prestacion { get; set; } = null!;

        public virtual CeldaSeccionRem Celda { get; set; } = null!;
    }
}
