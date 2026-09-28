using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared;

public class MovimientoFinancieroEntityTypeConfiguration : IEntityTypeConfiguration<MovimientoFinanciero>
{
    public void Configure(EntityTypeBuilder<MovimientoFinanciero> builder)
    {
        builder.ToTable("movimiento_financiero", "REMTool");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ConvenioFinancieroId).HasColumnName("convenio_financiero_id");
        builder.Property(x => x.ItemPresupuestarioId).HasColumnName("item_presupuestario_id");
        builder.Property(x => x.SubItemPresupuestarioId).HasColumnName("sub_item_presupuestario_id");
        builder.Property(x => x.ProveedorFinancieroId).HasColumnName("proveedor_financiero_id");
        builder.Property(x => x.Fecha).HasColumnName("fecha");
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Monto).HasColumnName("monto").HasPrecision(18, 2);
        builder.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento").HasMaxLength(30).IsRequired();
        builder.Property(x => x.TipoDocumento).HasColumnName("tipo_documento").HasMaxLength(80);
        builder.Property(x => x.NumeroDocumento).HasColumnName("numero_documento").HasMaxLength(100);
        builder.Property(x => x.Proveedor).HasColumnName("proveedor").HasMaxLength(200);
        builder.Property(x => x.Observaciones).HasColumnName("observaciones");
        builder.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        builder.Property(x => x.FechaModificacion).HasColumnName("fecha_modificacion");
        builder.HasIndex(x => new { x.ConvenioFinancieroId, x.Fecha });
        builder.HasOne(x => x.ConvenioFinanciero).WithMany(x => x.Movimientos).HasForeignKey(x => x.ConvenioFinancieroId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ItemPresupuestario).WithMany(x => x.Movimientos).HasForeignKey(x => x.ItemPresupuestarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SubItemPresupuestario).WithMany(x => x.Movimientos).HasForeignKey(x => x.SubItemPresupuestarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProveedorFinanciero).WithMany(x => x.Movimientos).HasForeignKey(x => x.ProveedorFinancieroId).OnDelete(DeleteBehavior.Restrict);
    }
}
