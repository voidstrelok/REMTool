namespace RemTool
{
    public class PuntoResumenResultadoDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int PlanillasConsideradas { get; set; }
    }

    public class ComputarResumenRequest
    {
        public string SerieNombre { get; set; } = string.Empty;
        public int? Mes { get; set; }
        public int? Año { get; set; }
        public List<EntryDatosDTO> Entries { get; set; } = [];
    }

    public class EntryDatosDTO
    {
        public string CodDeis { get; set; } = string.Empty;
        public bool IncluidoEnResumen { get; set; } = true;
        public bool TieneErrores { get; set; }
        public bool TieneAdvertencias { get; set; }
        public List<PrestacionDatosDTO> Datos { get; set; } = [];
    }

    public class ComputarResumenResponseDTO
    {
        public string Serie { get; set; } = string.Empty;
        public int? Mes { get; set; }
        public int? Año { get; set; }
        public ResumenCoberturaDTO Cobertura { get; set; } = new();
        public List<PuntoResumenResultadoDTO> Puntos { get; set; } = [];
        public List<string> ErroresCalculo { get; set; } = [];
    }

    public class ResumenCoberturaDTO
    {
        public int PlanillasRecibidas { get; set; }
        public int PlanillasConsideradas { get; set; }
        public int PlanillasExcluidas { get; set; }
        public int PlanillasConErrores { get; set; }
        public int PlanillasConAdvertencias { get; set; }
    }
}
