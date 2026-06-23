using System.Collections.Generic;

namespace RemTool.Shared
{
    public partial class Convenio
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public virtual ICollection<IndicadorConvenio> IndicadorConvenios { get; set; } = new HashSet<IndicadorConvenio>();
    }
}
