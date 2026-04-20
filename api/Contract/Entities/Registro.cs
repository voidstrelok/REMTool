using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemTool
{
    public partial class Registro
    {
        public int Id { get; set; }

        public int id_prestacion { get; set; }

        public int id_reporte { get; set; }

        public List<int> Valor { get; set; }

        public virtual Prestacion Prestacion { get; set; } = null!;

        public virtual Reporte Reporte { get; set; } = null!;
    }
}
