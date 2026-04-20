using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class RegistroEntityTypeConfiguration : IEntityTypeConfiguration<Registro>
    {
        public void Configure(EntityTypeBuilder<Registro> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .HasOne(x => x.Prestacion)
                .WithMany(x => x.Registros)
                .HasForeignKey(x => x.id_prestacion)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .HasOne(x => x.Reporte)
                .WithMany(x => x.Registros)
                .HasForeignKey(x => x.id_reporte)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".registro_id_seq'::regclass)");

            builder
                .Property(x => x.Valor)
                .HasColumnName("valor");

            builder
                .ToTable("registro", "REMTool");
        }
    }
}
