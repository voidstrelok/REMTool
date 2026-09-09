using System;
using System.Collections.Generic;

namespace RemTool.Shared
{
    public partial class SeccionRem
    {
        public long Id { get; set; }

        public int? IdVersionHoja { get; set; }

        // Campo legado: se conserva para no romper instalaciones existentes.
        public long IdHojaRem { get; set; }

        public string Codigo { get; set; } = String.Empty;

        public string Nombre { get; set; } = String.Empty;

        public int Orden { get; set; }

        public string RangoDiccionario { get; set; } = String.Empty;

        public string RangoBase { get; set; } = String.Empty;

        public int FilaInicio { get; set; }
        public int ColumnaInicio { get; set; }
        public int FilaFin { get; set; }
        public int ColumnaFin { get; set; }
        public int OffsetPrestacion { get; set; }
        public int OffsetColumna { get; set; }

        public string HtmlEstructura { get; set; } = String.Empty;

        public virtual VersionHojaRem? VersionHoja { get; set; }

        public virtual ICollection<FilaSeccionRem> Filas { get; set; } = new HashSet<FilaSeccionRem>();
        public virtual ICollection<Prestacion> Prestacions { get; set; } = new HashSet<Prestacion>();
    }
}
