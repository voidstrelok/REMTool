namespace RemTool.Shared
{
    public partial class CeldaSeccionRem
    {
        public int Id { get; set; }

        public int IdFila { get; set; }

        public int FilaOrigen { get; set; }

        public int ColumnaOrigen { get; set; }

        public string Valor { get; set; } = String.Empty;

        public string CeldaDiccionario { get; set; } = String.Empty;

        public string CeldaBase { get; set; } = String.Empty;

        public int RowSpan { get; set; } = 1;

        public int ColSpan { get; set; } = 1;

        public bool EsCeldaAncla { get; set; }

        public bool EsEditable { get; set; }

        public bool EsEntradaPrestacion { get; set; }

        public bool EsTotal { get; set; }

        public string TipoTotal { get; set; } = String.Empty;

        public string FormulaOrigen { get; set; } = String.Empty;

        public string DependenciasTotal { get; set; } = String.Empty;

        public string OperacionTotal { get; set; } = String.Empty;

        public int EstiloOrigen { get; set; }

        public string ColorFondo { get; set; } = String.Empty;

        public virtual FilaSeccionRem Fila { get; set; } = null!;

        public virtual CoordenadaPrestacion? CoordenadaPrestacion { get; set; }
    }
}
