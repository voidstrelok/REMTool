using System;

namespace RemTool.Shared
{
    public partial class ResultadoIndicador
    {
        public int Id { get; set; }

        public int id_indicador { get; set; }

        public long id_establecimiento { get; set; }

        public virtual Indicador Indicador { get; set; } = null!;

        public virtual Establecimiento Establecimiento { get; set; } = null!;

        public int Mes { get; set; }

        public decimal Numerador { get; set; }

        public decimal Denominador { get; set; }

        public decimal NumeradorP { get; set; }

        public decimal DenominadorP { get; set; }
    }
}
