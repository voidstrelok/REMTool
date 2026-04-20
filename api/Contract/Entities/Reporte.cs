using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemTool
{
    public partial class Reporte
    {
        public int Id { get; set; }

        public long id_comuna { get; set; }

        public long id_establecimiento { get; set; }

        public int Mes { get; set; }

        public virtual Comuna Comuna { get; set; } = null!;

        public virtual Establecimiento Establecimiento { get; set; } = null!;

        public virtual ICollection<Registro> Registros { get; set; } = new HashSet<Registro>();
    }
}
