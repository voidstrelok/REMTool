using Microsoft.EntityFrameworkCore;
using RemTool.Shared;

namespace RemTool.Dal;

internal sealed class ConveniosFinancierosLegacyPostgresContext(DbContextOptions<ConveniosFinancierosLegacyPostgresContext> options) : DbContext(options)
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
    }
}
