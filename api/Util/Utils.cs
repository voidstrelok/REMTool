using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeOpenXml;

namespace RemTool.Util
{
    public static class Utils
    {
        public static string RegexHoja = @"[a-zA-Z]*[0-9]*\[[^\]]*.";
        public static string RegexCelda = @"[A-Z]+[0-9]+";

        public static void AddHeading(MainDocumentPart mainPart, string text, int level)
        {
            var body = mainPart.Document.Body;
            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId() { Val = "Heading" + (level + 1) }
                ),
                new Run(
                    new RunProperties(
                        new Bold(),
                        new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = (level == 0 ? "32" : level == 1 ? "28" : "24") } // Tamaño según nivel
                    ),
                    new Text(text)
                )
            );
            body.AppendChild(paragraph);
        }

        public static void AddParagraph(MainDocumentPart mainPart, string text)
        {
            var body = mainPart.Document.Body;
            body.AppendChild(new Paragraph(new Run(new Text(text))));
        }
        public static string ParseaHojas(string regla, ExcelPackage Libro)
        {
            var Hoja = Libro.Workbook.Worksheets[regla.Substring(0, regla.IndexOf("["))];
            return Regex.Replace(regla.Substring(regla.IndexOf("[")), RegexCelda, x => Extrae(x.Value, Hoja));

        }

        static string Extrae(string rango, ExcelWorksheet hoja)
        {
            if (hoja.Cells[rango].Value != null)
            {
                if (hoja.Cells[rango].Value == "")
                {
                    return "";
                }
                else
                {
                    if (hoja.Cells[rango].Value.ToString().Equals("#VALUE!"))
                        return "''";
                    else
                        return hoja.Cells[rango].Value.ToString();

                }

            }
            else
            {
                return "0";
            }

        }
    }
}
