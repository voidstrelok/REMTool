using System.Collections.Generic;
using System.Globalization;
public class Regla
{
    public string Expresion { get; set; }
    public string Mensaje { get; set; }
}
public static class Datos
{
    private static readonly IConfiguration _configuration;
    public static string consolidadoAPath;
    public static string versionBase;
    public static string versionBaseP;
    static Datos()
    {
        versionBase  = Environment.GetEnvironmentVariable("Paths__BaseSA");
        versionBaseP = Environment.GetEnvironmentVariable("Paths__BaseSP");
    }
        


    public static readonly Dictionary<string, Dictionary<string, string>> HojaControl = new()
    {
        { "Versión 1.4: Marzo 2025" , new Dictionary<string, string>
        {
            { "A01", "E5" },
            { "A02", "E6" },
            { "A03", "E7" },
            { "A04", "E8" },
            { "A05", "E9" },
            { "A06", "E10" },
            { "A07", "E11" },
            { "A08", "E12" },
            { "A09", "E13" },
            { "A11", "E14" },
            { "A11a", "E15" },
            { "A19a", "E16" },
            { "A19b", "E17" },
            { "A21", "E18" },
            { "A23", "E19" },
            { "A24", "E20" },
            { "A25", "E21" },
            { "A26", "E22" },
            { "A27", "E23" },
            { "A28", "E24" },
            { "A29", "E25" },
            { "A30", "E26" },
            { "A30AR", "E27" },
            { "A31", "E28" },
            { "A32", "E29" },
            { "A33", "E30" },
            { "A34", "E31" }
        } 
        },
        {"Versión 1.5: Mayo 2025", new Dictionary<string, string>
        {
            { "A01", "E5" },
            { "A02", "E6" },
            { "A03", "E7" },
            { "A04", "E8" },
            { "A05", "E9" },
            { "A06", "E10" },
            { "A07", "E11" },
            { "A08", "E12" },
            { "A09", "E13" },
            { "A11", "E14" },
            { "A11a", "E15" },
            { "A19a", "E16" },
            { "A19b", "E17" },
            { "A21", "E18" },
            { "A23", "E19" },
            { "A24", "E20" },
            { "A25", "E21" },
            { "A26", "E22" },
            { "A27", "E23" },
            { "A28", "E24" },
            { "A29", "E25" },
            { "A30", "E26" },
            { "A30AR", "E27" },
            { "A31", "E28" },
            { "A32", "E29" },
            { "A33", "E30" },
            { "A34", "E31" }
        }
        },
        {"Versión 1.0: Febrero 2026", new Dictionary<string, string>
        {
            { "A01", "E5" },
            { "A02", "E6" },
            { "A03", "E7" },
            { "A04", "E8" },
            { "A05", "E9" },
            { "A06", "E10" },
            { "A07", "E11" },
            { "A08", "E12" },
            { "A09", "E13" },
            { "A11", "E14" },
            { "A11a", "E15" },
            { "A19a", "E16" },
            { "A19b", "E17" },
            { "A21", "E18" },
            { "A23", "E19" },
            { "A24", "E20" },
            { "A25", "E21" },
            { "A26", "E22" },
            { "A27", "E23" },
            { "A28", "E24" },
            { "A29", "E25" },
            { "A30", "E26" },
            { "A30AR", "E27" },
            { "A31", "E28" },
            { "A32", "E29" },
            { "A33", "E30" },
            { "A34", "E31" }
        }
    },
        {"Versión 1.2: Febrero 2026", new Dictionary<string, string>
        {
            { "A01", "E5" },
            { "A02", "E6" },
            { "A03", "E7" },
            { "A04", "E8" },
            { "A05", "E9" },
            { "A06", "E10" },
            { "A07", "E11" },
            { "A08", "E12" },
            { "A09", "E13" },
            { "A11", "E14" },
            { "A11a", "E15" },
            { "A19a", "E16" },
            { "A19b", "E17" },
            { "A21", "E18" },
            { "A23", "E19" },
            { "A24", "E20" },
            { "A25", "E21" },
            { "A26", "E22" },
            { "A27", "E23" },
            { "A28", "E24" },
            { "A29", "E25" },
            { "A30", "E26" },
            { "A30AR", "E27" },
            { "A31", "E28" },
            { "A32", "E29" },
            { "A33", "E30" },
            { "A34", "E31" }
        }
    }  };

    // HojaControl para Serie P.
    // TODO: completar los mappings de las 11 hojas una vez confirmados los nombres.
    // Formato: { "<Nombre versión P>", new Dictionary<string, string> { { "P01", "<celda en Control>" }, ... } }
    public static readonly Dictionary<string, Dictionary<string, string>> HojaControlP = new()
    {
        // Ejemplo:
        // { "Versión 1.2: Febrero 2026", new Dictionary<string, string>
        //     {
        //         { "P01", "E5" },
        //         { "P02", "E6" },
        //         ...
        //     }
        // }
    };

}