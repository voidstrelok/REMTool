namespace RemTools
{
    public partial class Utils
    {
        public static List<Dictionary<string, string>> CargarCSV(string rutaArchivo, bool tieneEncabezado = true)
        {
            var registros = new List<Dictionary<string, string>>();

            if (!File.Exists(rutaArchivo))
                throw new FileNotFoundException($"El archivo CSV no existe: {rutaArchivo}");

            using var reader = new StreamReader(rutaArchivo);
            string[] encabezados = null;
            bool primeraLinea = true;

            while (!reader.EndOfStream)
            {
                var linea = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(linea)) continue;

                var valores = linea.Split(',');

                if (primeraLinea && tieneEncabezado)
                {
                    encabezados = valores;
                    primeraLinea = false;
                    continue;
                }
                else if (primeraLinea && !tieneEncabezado)
                {
                    encabezados = Enumerable.Range(1, valores.Length).Select(i => $"Col{i}").ToArray();
                    primeraLinea = false;
                }

                var registro = new Dictionary<string, string>();
                for (int i = 0; i < valores.Length && i < encabezados.Length; i++)
                    registro[encabezados[i]] = valores[i].Trim().Trim('"', '\'');

                registros.Add(registro);
            }

            return registros;
        }

        public static List<T> CargarCSV<T>(string rutaArchivo, bool tieneEncabezado = true) where T : new()
        {
            var registros = new List<T>();
            var propiedades = typeof(T).GetProperties();

            if (!File.Exists(rutaArchivo))
                throw new FileNotFoundException($"El archivo CSV no existe: {rutaArchivo}");

            using var reader = new StreamReader(rutaArchivo);
            string[] encabezados = null;
            bool primeraLinea = true;

            while (!reader.EndOfStream)
            {
                var linea = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(linea)) continue;

                var valores = ParseCSVLine(linea);

                if (primeraLinea && tieneEncabezado)
                {
                    encabezados = valores;
                    primeraLinea = false;
                    continue;
                }

                var registro = new T();
                for (int i = 0; i < valores.Length && i < encabezados.Length; i++)
                {
                    var propiedad = propiedades.FirstOrDefault(p =>
                        p.Name.Equals(encabezados[i], StringComparison.OrdinalIgnoreCase));

                    if (propiedad != null && propiedad.CanWrite)
                    {
                        try
                        {
                            var valor = valores[i].Trim().Trim('"', '\'');
                            propiedad.SetValue(registro, Convert.ChangeType(valor, propiedad.PropertyType));
                        }
                        catch { }
                    }
                }
                registros.Add(registro);
            }

            return registros;
        }

        private static string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                    current.Append(c);
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
