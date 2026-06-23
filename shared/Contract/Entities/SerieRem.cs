using System;
using System.Collections.Generic;

namespace RemTool.Shared
{
    public partial class SerieRem
    {
        public long Id { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public virtual ICollection<VersionRem> VersionRems { get; set; } = new HashSet<VersionRem>();
        public virtual ICollection<HojaRem> HojaRems { get; set; } = new HashSet<HojaRem>();
    }
}
