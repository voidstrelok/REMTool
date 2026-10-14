using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTool.Dal;

public sealed class ConveniosFinancierosDbContext(DbContextOptions<ConveniosFinancierosDbContext> options) : DbContext(options)
{
    public DbSet<ConvenioFinanciero> ConvenioFinanciero => Set<ConvenioFinanciero>();
    public DbSet<ItemPresupuestario> ItemPresupuestario => Set<ItemPresupuestario>();
    public DbSet<SubItemPresupuestario> SubItemPresupuestario => Set<SubItemPresupuestario>();
    public DbSet<MovimientoFinanciero> MovimientoFinanciero => Set<MovimientoFinanciero>();
    public DbSet<ProveedorFinanciero> ProveedorFinanciero => Set<ProveedorFinanciero>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConvenioFinancieroEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ItemPresupuestarioEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new SubItemPresupuestarioEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoFinancieroEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProveedorFinancieroEntityTypeConfiguration());

        // SQLite no admite esquemas. Se conservan los nombres de tabla existentes,
        // pero se eliminan las referencias al esquema PostgreSQL REMTool.
        modelBuilder.Entity<ConvenioFinanciero>().ToTable("convenio_financiero");
        modelBuilder.Entity<ItemPresupuestario>().ToTable("item_presupuestario");
        modelBuilder.Entity<SubItemPresupuestario>().ToTable("sub_item_presupuestario");
        modelBuilder.Entity<MovimientoFinanciero>().ToTable("movimiento_financiero");
        modelBuilder.Entity<ProveedorFinanciero>().ToTable("proveedor_financiero");

        base.OnModelCreating(modelBuilder);
    }
}
