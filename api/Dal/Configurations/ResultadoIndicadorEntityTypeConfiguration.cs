using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class ResultadoIndicadorEntityTypeConfiguration : IEntityTypeConfiguration<ResultadoIndicador>
    {
        public void Configure(EntityTypeBuilder<ResultadoIndicador> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .HasOne(x => x.Indicador)
                .WithMany(x => x.ResultadoIndicadors)
                .HasForeignKey(x => x.id_indicador)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .HasOne(x => x.Establecimiento)
                .WithMany(x => x.ResultadoIndicadors)
                .HasForeignKey(x => x.id_establecimiento)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".resultado_indicador_id_seq'::regclass)");

            builder
                .Property(x => x.Mes)
                .HasColumnName("mes")
                .HasPrecision(32, 0);

            builder
                .Property(x => x.Numerador)
                .HasColumnName("numerador");

            builder
                .Property(x => x.Denominador)
                .HasColumnName("denominador");

            builder
                .ToTable("resultado_indicador", "REMTool");
        }
    }
}
