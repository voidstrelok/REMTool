namespace RemTool.Shared;

public class ItemPresupuestario
{
    public int Id { get; set; }
    public int ConvenioFinancieroId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PresupuestoAsignado { get; set; }
    public bool Activo { get; set; } = true;
    public ConvenioFinanciero ConvenioFinanciero { get; set; } = null!;
    public ICollection<MovimientoFinanciero> Movimientos { get; set; } = new HashSet<MovimientoFinanciero>();
}
