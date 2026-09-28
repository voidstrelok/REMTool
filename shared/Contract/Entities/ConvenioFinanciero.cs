namespace RemTool.Shared;

public class ConvenioFinanciero
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public int Anio { get; set; }
    public string? Descripcion { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaTermino { get; set; }
    public decimal PresupuestoTotal { get; set; }
    public string Responsable { get; set; } = string.Empty;
    public string Estado { get; set; } = "Activo";
    public DateTimeOffset FechaCreacion { get; set; }
    public ICollection<ItemPresupuestario> Items { get; set; } = new HashSet<ItemPresupuestario>();
    public ICollection<MovimientoFinanciero> Movimientos { get; set; } = new HashSet<MovimientoFinanciero>();
}
