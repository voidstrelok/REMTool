using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemTool
{
    public partial class Parametros
    {
        public int Id { get; set; }

        public bool ServicioEnabled { get; set; }

        public DateOnly UltimaActualizacion { get; set; }
    }
}
