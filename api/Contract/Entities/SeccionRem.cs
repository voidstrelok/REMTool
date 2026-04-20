using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemTool
{
    public partial class SeccionRem
    {
        public long Id { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public long IdHojaRem { get; set; }
    }
}
