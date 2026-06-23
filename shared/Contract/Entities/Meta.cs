using System;

namespace RemTool.Shared
{
    public partial class Meta
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public string Formula { get; set; } = String.Empty;

        public int Año { get; set; }
    }
}
