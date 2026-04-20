using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class PrestacionEntityTypeConfiguration : IEntityTypeConfiguration<Prestacion>
    {
        public void Configure(EntityTypeBuilder<Prestacion> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .HasOne(x => x.VersionRem)
                .WithMany(x => x.Prestacions)
                .HasForeignKey(x => x.id_version)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .HasOne(x => x.HojaRem)
                .WithMany(x => x.Prestacions)
                .HasForeignKey(x => x.id_hoja)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".prestacion_id_seq'::regclass)");

            builder
                .Property(x => x.CodigoPrestacion)
                .HasColumnName("codigo_prestacion")
                .HasColumnType("character varying");

            builder
                .Property(x => x.IsEnabled)
                .HasColumnName("is_enabled");

            builder
                .Property(x => x.Coordenada)
                .HasColumnName("coordenada");

            builder
                .ToTable("prestacion", "REMTool");
        }
    }
}
