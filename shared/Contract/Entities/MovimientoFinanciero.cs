namespace RemTool.Shared;

public class MovimientoFinanciero
{
    public int Id { get; set; }
    public int ConvenioFinancieroId { get; set; }
    public int? ItemPresupuestarioId { get; set; }
    public DateOnly Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string TipoMovimiento { get; set; } = "Gasto";
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Proveedor { get; set; }
    public string? Observaciones { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset FechaModificacion { get; set; }
    public ConvenioFinanciero ConvenioFinanciero { get; set; } = null!;
    public ItemPresupuestario? ItemPresupuestario { get; set; }
}
