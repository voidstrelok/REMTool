using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RemTool.Shared
{
    public class CoordenadaPrestacionEntityTypeConfiguration : IEntityTypeConfiguration<CoordenadaPrestacion>
    {
        public void Configure(EntityTypeBuilder<CoordenadaPrestacion> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Prestacion)
                .WithMany(x => x.Coordenadas)
                .HasForeignKey(x => x.IdPrestacion)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Celda)
                .WithOne(x => x.CoordenadaPrestacion)
                .HasForeignKey<CoordenadaPrestacion>(x => x.IdCelda)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => new { x.IdPrestacion, x.Orden }).IsUnique();

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".coordenada_prestacion_id_seq'::regclass)");

            builder.Property(x => x.IdPrestacion).HasColumnName("id_prestacion");
            builder.Property(x => x.IdCelda).HasColumnName("id_celda");
            builder.Property(x => x.Orden).HasColumnName("orden");
            builder.Property(x => x.CodigoColumna).HasColumnName("codigo_columna").HasColumnType("character varying");
            builder.Property(x => x.CeldaDiccionario).HasColumnName("celda_diccionario").HasColumnType("character varying");
            builder.Property(x => x.CeldaBase).HasColumnName("celda_base").HasColumnType("character varying");

            builder.ToTable("coordenada_prestacion", "REMTool");
        }
    }
}
