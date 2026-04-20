using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemTool
{
    public partial class Regla
    {
        public int Id { get; set; }

        public int id_version { get; set; }

        public string Expresion { get; set; } = String.Empty;

        public string Mensaje { get; set; } = String.Empty;

        public virtual VersionRem VersionREM { get; set; } = null!;
    }
}
