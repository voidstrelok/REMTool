using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace RemTool.Shared;
public class SubItemPresupuestarioEntityTypeConfiguration : IEntityTypeConfiguration<SubItemPresupuestario>
{
    public void Configure(EntityTypeBuilder<SubItemPresupuestario> b) { b.ToTable("sub_item_presupuestario","REMTool"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).HasColumnName("id").ValueGeneratedOnAdd(); b.Property(x=>x.ItemPresupuestarioId).HasColumnName("item_presupuestario_id"); b.Property(x=>x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired(); b.Property(x=>x.PresupuestoAsignado).HasColumnName("presupuesto_asignado").HasPrecision(18,2); b.Property(x=>x.Activo).HasColumnName("activo"); b.HasIndex(x=>new{x.ItemPresupuestarioId,x.Nombre}).IsUnique(); b.HasOne(x=>x.ItemPresupuestario).WithMany(x=>x.SubItems).HasForeignKey(x=>x.ItemPresupuestarioId).OnDelete(DeleteBehavior.Cascade); }
}
