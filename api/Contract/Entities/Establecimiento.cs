using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemTool
{
    public partial class Establecimiento
    {
        public long Id { get; set; }

        public long id_sector { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public string? Director { get; set; }

        public virtual Sector Sector { get; set; } = null!;

        public string? CodDeis { get; set; }

        public virtual ICollection<ResultadoIndicador> ResultadoIndicadors { get; set; } = new HashSet<ResultadoIndicador>();
        public virtual ICollection<Reporte> Reportes { get; set; } = new HashSet<Reporte>();
    }
}
