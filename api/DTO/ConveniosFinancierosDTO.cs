using System.ComponentModel.DataAnnotations;

namespace RemTool.DTO;

public sealed class ConvenioFinancieroRequest
{
    [Required, StringLength(200)] public string Nombre { get; init; } = string.Empty;
    [StringLength(80)] public string? Codigo { get; init; }
    [Range(2000, 2100)] public int Anio { get; init; }
    public string? Descripcion { get; init; }
    public DateOnly FechaInicio { get; init; }
    public DateOnly FechaTermino { get; init; }
    [Required, StringLength(150)] public string Responsable { get; init; } = string.Empty;
    [RegularExpression("Activo|Cerrado")] public string Estado { get; init; } = "Activo";
}

public sealed class ItemPresupuestarioRequest
{
    [Required, StringLength(150)] public string Nombre { get; init; } = string.Empty;
    public bool Activo { get; init; } = true;
}

public sealed class MovimientoFinancieroRequest
{
    public DateOnly Fecha { get; init; }
    [Required, StringLength(300)] public string Descripcion { get; init; } = string.Empty;
    [Range(0.01, 9999999999999999d)] public decimal Monto { get; init; }
    public int? ItemPresupuestarioId { get; init; }
    [Required] public int? SubItemPresupuestarioId { get; init; }
    [RegularExpression("Gasto")] public string TipoMovimiento { get; init; } = "Gasto";
    [RegularExpression("Factura|Orden de Compra")] public string? TipoDocumento { get; init; }
    [StringLength(100)] public string? NumeroDocumento { get; init; }
    [StringLength(200)] public string? Proveedor { get; init; }
    public string? Observaciones { get; init; }
}

public sealed class SubItemPresupuestarioRequest
{
    [Required, StringLength(150)] public string Nombre { get; init; } = string.Empty;
    [Range(0.01, 9999999999999999d)] public decimal PresupuestoAsignado { get; init; }
    public bool Activo { get; init; } = true;
}

public record ResumenFinancieroDto(decimal Presupuesto, decimal Ejecutado, decimal Disponible, decimal PorcentajeEjecucion);
public record SubItemFinancieroDto(int Id, int ItemPresupuestarioId, string Nombre, decimal PresupuestoAsignado, decimal Ejecutado, decimal Disponible, bool Activo);
public record ItemFinancieroDto(int Id, string Nombre, decimal PresupuestoAsignado, decimal Ejecutado, decimal Disponible, bool Activo, IReadOnlyList<SubItemFinancieroDto> SubItems);
public record MovimientoFinancieroDto(int Id, DateOnly Fecha, string Descripcion, decimal Monto, string TipoMovimiento, int? ItemPresupuestarioId, string? ItemNombre, int? SubItemPresupuestarioId, string? SubItemNombre, string? TipoDocumento, string? NumeroDocumento, string? Proveedor, string? Observaciones, DateTimeOffset FechaCreacion, DateTimeOffset FechaModificacion);
public record ConvenioFinancieroResumenDto(int Id, string Nombre, string? Codigo, int Anio, string Responsable, string Estado, decimal Presupuesto, decimal Ejecutado, decimal Disponible, decimal PorcentajeEjecucion);
public record ConvenioFinancieroDetalleDto(int Id, string Nombre, string? Codigo, int Anio, string? Descripcion, DateOnly FechaInicio, DateOnly FechaTermino, string Responsable, string Estado, DateTimeOffset FechaCreacion, ResumenFinancieroDto Resumen, IReadOnlyList<ItemFinancieroDto> Items, IReadOnlyList<MovimientoFinancieroDto> Movimientos);
public record DashboardConvenioDto(int ConvenioId, string Convenio, decimal Ejecutado, int CantidadMovimientos);
public record DashboardFinancieroDto(int Anio, int? Mes, int? ConvenioId, decimal Ejecutado, int CantidadMovimientos, IReadOnlyList<DashboardConvenioDto> Convenios);
