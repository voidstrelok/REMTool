using System;
using System.Collections.Generic;

namespace RemTool.Shared
{
    public partial class TipoRegla
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public virtual ICollection<Regla> Reglas { get; set; } = [];
    }
}
