using System;
using System.Collections.Generic;

namespace RemTool.Shared
{
    public partial class Sector
    {
        public long Id { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public virtual ICollection<Establecimiento> Establecimientos { get; set; } = new HashSet<Establecimiento>();
    }
}
