using System;
using System.Collections.Generic;

namespace RemTool.Shared
{
    public partial class Prestacion
    {
        public int Id { get; set; }

        public int id_version { get; set; }

        public int id_hoja { get; set; }

        public string CodigoPrestacion { get; set; } = String.Empty;

        public bool IsEnabled { get; set; }

        public virtual VersionRem VersionRem { get; set; } = null!;

        public List<string> Coordenada { get; set; } = new();

        public virtual HojaRem HojaRem { get; set; } = null!;

        public virtual ICollection<Registro> Registros { get; set; } = new HashSet<Registro>();
    }
}
