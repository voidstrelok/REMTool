using System.Collections.Generic;

namespace RemTool.Shared
{
    public partial class VersionHojaRem
    {
        public int Id { get; set; }

        public int IdVersion { get; set; }

        public int IdHoja { get; set; }

        public int Orden { get; set; }

        public virtual VersionRem VersionRem { get; set; } = null!;

        public virtual HojaRem HojaRem { get; set; } = null!;

        public virtual ICollection<SeccionRem> Secciones { get; set; } = new HashSet<SeccionRem>();
    }
}
