using System;

namespace RemTool.Shared
{
    public partial class Parametros
    {
        public int Id { get; set; }

        public bool ServicioEnabled { get; set; }

        public bool MonitoreoEnabled { get; set; }

        public DateOnly UltimaActualizacion { get; set; }
    }
}
