namespace RemTool.Shared;

public class SubItemPresupuestario
{
    public int Id { get; set; }
    public int ItemPresupuestarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public ItemPresupuestario ItemPresupuestario { get; set; } = null!;
    public ICollection<MovimientoFinanciero> Movimientos { get; set; } = new HashSet<MovimientoFinanciero>();
}
