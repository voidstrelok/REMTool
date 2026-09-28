namespace RemTool.Shared;

public class ProveedorFinanciero
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombreNormalizado { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public ICollection<MovimientoFinanciero> Movimientos { get; set; } = new HashSet<MovimientoFinanciero>();
}
