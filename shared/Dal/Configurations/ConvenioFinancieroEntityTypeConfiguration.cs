using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared;

public class ConvenioFinancieroEntityTypeConfiguration : IEntityTypeConfiguration<ConvenioFinanciero>
{
    public void Configure(EntityTypeBuilder<ConvenioFinanciero> builder)
    {
        builder.ToTable("convenio_financiero", "REMTool");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(80);
        builder.Property(x => x.Anio).HasColumnName("anio");
        builder.Property(x => x.Descripcion).HasColumnName("descripcion");
        builder.Property(x => x.FechaInicio).HasColumnName("fecha_inicio");
        builder.Property(x => x.FechaTermino).HasColumnName("fecha_termino");
        builder.Property(x => x.PresupuestoTotal).HasColumnName("presupuesto_total").HasPrecision(18, 2);
        builder.Property(x => x.Responsable).HasColumnName("responsable").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20).IsRequired();
        builder.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        builder.HasIndex(x => new { x.Anio, x.Estado });
    }
}
