using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace RemTool.Shared;
public class ProveedorFinancieroEntityTypeConfiguration : IEntityTypeConfiguration<ProveedorFinanciero>
{
    public void Configure(EntityTypeBuilder<ProveedorFinanciero> b) { b.ToTable("proveedor_financiero","REMTool"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).HasColumnName("id").ValueGeneratedOnAdd(); b.Property(x=>x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired(); b.Property(x=>x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(200).IsRequired(); b.Property(x=>x.FechaCreacion).HasColumnName("fecha_creacion"); b.HasIndex(x=>x.NombreNormalizado).IsUnique(); }
}
