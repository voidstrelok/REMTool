using System;
using System.Collections.Generic;

namespace RemTool
{
    public class HeaderSeccionDTO
    {
        public long Id {get; set;}
        public string Texto {get; set;}
        public int Colspan {get; set;}
        public int Rowspan {get; set;}
        public long IdSeccion {get; set;}
        public int NumFila {get; set;}
        public int NumColumna {get; set;}
        public long? IdDato {get; set;}
        public DatoSeccionDTO? DatoSeccion {get; set;}
    }
}
