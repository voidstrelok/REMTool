using System.Collections.Generic;

namespace RemTool
{
    public partial class Indicador
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Formula { get; set; } = string.Empty;

        public int Año { get; set; }

        public bool IsDenFijo { get; set; }

        public bool IsTasa { get; set; }

        public string? FormulaDenFijo { get; set; }

        public float Meta { get; set; }

        public float Peso { get; set; }

        public bool Mensual { get; set; }

        public int Orden { get; set; }

        public string? Detalle { get; set; }

        public int? Tipoindicador { get; set; }

        /// <summary>
        /// Cuando es true, el denominador se calcula sobre el período octubre del año anterior – septiembre del año en curso.
        /// </summary>
        public bool EsPeriodoOctubreSep { get; set; }

        /// <summary>
        /// Si es true, el denominador es comunal y los establecimientos solo aportan numerador.
        /// </summary>
        public bool IsColaborativo { get; set; }

        public virtual ICollection<ResultadoIndicador> ResultadoIndicadors { get; set; } = new HashSet<ResultadoIndicador>();

        public virtual ICollection<IndicadorConvenio> IndicadorConvenios { get; set; } = new HashSet<IndicadorConvenio>();


    }
}
