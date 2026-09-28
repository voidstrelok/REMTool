namespace RemTool.Shared;

public class ItemPresupuestario
{
    public int Id { get; set; }
    public int ConvenioFinancieroId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public ConvenioFinanciero ConvenioFinanciero { get; set; } = null!;
    public ICollection<MovimientoFinanciero> Movimientos { get; set; } = new HashSet<MovimientoFinanciero>();
    public ICollection<SubItemPresupuestario> SubItems { get; set; } = new HashSet<SubItemPresupuestario>();
}
