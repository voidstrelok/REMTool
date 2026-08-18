namespace RemTool
{
    public class AnalisisRemDTO
    {
        public string CodDeis { get; set; } = string.Empty;
        public string NombreEstablecimiento { get; set; } = string.Empty;
        public long IdSector { get; set; }
        public string NombreSector { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int Mes { get; set; }
        public int Año { get; set; }
        public bool IncluidoEnResumen { get; set; } = true;
        public List<ValidacionRemDTO> Validaciones { get; set; } = [];
        public List<string> Errores { get; set; } = [];
        public List<string> Advertencias { get; set; } = [];
        public List<PrestacionDatosDTO> Datos { get; set; } = [];
    }

    public class ValidacionRemDTO
    {
        public string Campo { get; set; } = string.Empty;
        public string Esperado { get; set; } = string.Empty;
        public string Encontrado { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    public class PrestacionDatosDTO
    {
        public string Prestacion { get; set; } = string.Empty;
        public List<string> Valores { get; set; } = [];
    }
}
