using System;

namespace RemTool.Shared
{
    public partial class SeccionRem
    {
        public long Id { get; set; }

        public string Nombre { get; set; } = String.Empty;

        public long IdHojaRem { get; set; }
    }
}
