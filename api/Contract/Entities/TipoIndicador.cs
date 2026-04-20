using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemTool
{
    public partial class TipoIndicador
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = String.Empty;
    }
}
