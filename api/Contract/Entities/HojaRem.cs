using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RemTool
{
    public partial class HojaRem
    {
        public int Id { get; set; }

        public long id_serie_rem { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public virtual SerieRem SerieRem { get; set; } = null!;
        [JsonIgnore]

        public virtual ICollection<Prestacion> Prestacions { get; set; } = new HashSet<Prestacion>();
    }
}
