using System.Globalization;
using System.Text.RegularExpressions;
using NCalc;

namespace RemTools
{
    internal sealed record ReglaExpressionResult(bool IsValid, string Message, object? Value = null);

    internal static class ReglaExpressionService
    {
        private static readonly Regex ReferenceRegex = new(
            @"(?<![A-Za-z0-9_])(?:[A-Za-z]+\d*)\[[A-Z]{1,3}\d+\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> ExtractReferences(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return [];

            return ReferenceRegex
                .Matches(expression)
                .Select(match => match.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(reference => reference)
                .ToList();
        }

        public static ReglaExpressionResult Validate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new(false, "La expresión está vacía.");

            try
            {
                var normalized = ReplaceReferences(expression, _ => "0");
                var value = new Expression(normalized).Evaluate();
                return new(true, $"Sintaxis válida. Resultado de referencia: {FormatValue(value)}", value);
            }
            catch (Exception ex)
            {
                return new(false, $"Expresión inválida: {ex.Message}");
            }
        }

        public static ReglaExpressionResult Evaluate(string expression, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new(false, "La expresión está vacía.");

            try
            {
                var normalized = ReplaceReferences(expression, match =>
                {
                    var reference = match.Value;
                    return values.TryGetValue(reference, out var value) && !string.IsNullOrWhiteSpace(value)
                        ? value.Trim().Replace("'", "''", StringComparison.Ordinal)
                        : "0";
                });

                var value = new Expression(normalized).Evaluate();
                return new(true, $"Resultado: {FormatValue(value)}", value);
            }
            catch (Exception ex)
            {
                return new(false, $"No se pudo evaluar: {ex.Message}");
            }
        }

        private static string ReplaceReferences(string expression, MatchEvaluator evaluator) =>
            ReferenceRegex.Replace(expression, evaluator);

        private static string FormatValue(object? value) => value switch
        {
            null => "(vacío)",
            bool boolean => boolean ? "VERDADERO" : "FALSO",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
            _ => value.ToString() ?? ""
        };
    }
}
