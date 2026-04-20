using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemTool
{
    public partial class VersionRem
    {
        public int Id { get; set; }

        public long id_serie { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public DateOnly Fecha { get; set; }

        public virtual SerieRem SerieRem { get; set; } = null!;

        public virtual ICollection<Regla> Reglas { get; set; } = new HashSet<Regla>();
        public virtual ICollection<Prestacion> Prestacions { get; set; } = new HashSet<Prestacion>();
    }
}
