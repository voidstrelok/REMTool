using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RemTool.Shared
{
    public partial class HojaRem
    {
        public int Id { get; set; }

        public long id_serie_rem { get; set; }

        public string Nombre { get; set; } = String.Empty;

        // Nombre técnico de la hoja en el archivo REM: Nombre.
        // Titulo es el nombre visible de la planilla y puede mantenerse
        // manualmente para series que no lo exponen de forma uniforme.
        public string Titulo { get; set; } = String.Empty;

        public virtual SerieRem SerieRem { get; set; } = null!;

        public virtual ICollection<VersionHojaRem> Versiones { get; set; } = new HashSet<VersionHojaRem>();

        [JsonIgnore]

        public virtual ICollection<Prestacion> Prestacions { get; set; } = new HashSet<Prestacion>();
    }
}
