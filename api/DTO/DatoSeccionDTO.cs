using System;
using System.Collections.Generic;

namespace RemTool
{
    public class DatoSeccionDTO
    {
        public long Id {get; set;}
        public string Coordenada {get; set;}
        public int NumColumna {get; set;}
        public long IdTipo {get; set;}
        public TipoDatoDTO TipoDato {get; set;}
    }
}
