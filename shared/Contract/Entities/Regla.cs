using System;

namespace RemTool.Shared
{
    public partial class Regla
    {
        public int Id { get; set; }

        public int id_version { get; set; }

        public int IdTipoRegla { get; set; }

        public string Expresion { get; set; } = String.Empty;

        public string Mensaje { get; set; } = String.Empty;

        public virtual VersionRem VersionREM { get; set; } = null!;

        public virtual TipoRegla TipoRegla { get; set; } = null!;
    }
}
