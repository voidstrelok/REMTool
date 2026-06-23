using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RemTool.Shared
{
    public partial class Comuna
    {
        public long Id { get; set; }

        public string CodDeis { get; set; } = String.Empty;

        public string Nombre { get; set; } = String.Empty;

        public long IdServicio { get; set; }
        [JsonIgnore]

        public virtual ICollection<Reporte> Reportes { get; set; } = new HashSet<Reporte>();
    }
}
