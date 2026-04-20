interface Header{
  id: number;
  texto: string;
  colspan: number;
  rowspan: number;
  idSeccion: number;
  numFila: number;
  numColumna: number;
  idDato?: number;
  datoSeccion: DatoSeccion
}

interface DatoSeccion{
  id: number;
  coordenada: string;
  numColumna: number;
  idTipo: number;
  tipoDato: TipoDato
}

interface TipoDato{
  id: number;
  tipo: string;
}