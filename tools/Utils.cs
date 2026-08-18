using System.Text.RegularExpressions;
using RemTool.Shared;
namespace RemTools
{
    public partial class Utils
    {
        private readonly RemToolDataContext Bdd;

        public Utils(RemToolDataContext dbContext)
        {
            Bdd = dbContext;
        }

        private const string RegexHoja = @"[a-zA-Z]*[0-9]*\[[^\]]*.";
        private const string RegexCelda = @"[A-Z]+[0-9]+";

        public static string ExtraerSerieDesdeNombre(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var serieActual = Regex.Match(
                raw,
                @"\bSERIE\s+([A-Za-z]+)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (serieActual.Success)
                return serieActual.Groups[1].Value.ToUpperInvariant();

            var serieAntigua = Regex.Match(
                raw,
                @"\bREM\s+([A-Za-z]+)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return serieAntigua.Success
                ? serieAntigua.Groups[1].Value.ToUpperInvariant()
                : string.Empty;
        }
    }
}
