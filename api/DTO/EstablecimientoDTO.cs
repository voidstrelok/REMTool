using System;
using System.Collections.Generic;

namespace RemTool
{
    public class EstablecimientoDTO
    {
        public long Id { get; set; }

        public long id_sector { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public string? Director { get; set; }

        public virtual Sector Sector { get; set; } = null!;

        public string? CodDeis { get; set; }

    }
}
