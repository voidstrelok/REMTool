using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemTool;
using System;
using System.Collections.Generic;

namespace RemTool
{
    public class IndicadorEntityTypeConfiguration : IEntityTypeConfiguration<Indicador>
    {
        public void Configure(EntityTypeBuilder<Indicador> builder)
        {
            builder
                .HasKey(x => x.Id);

            builder
                .Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id")
                .HasPrecision(32, 0)
                .HasDefaultValueSql("nextval('\"REMTool\".meta_id_seq'::regclass)");

            builder
                .HasMany(x => x.ResultadoIndicadors)
                .WithOne(x => x.Indicador)
                .HasForeignKey(x => x.id_indicador)
                .OnDelete(DeleteBehavior.NoAction);

            builder
                .Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasColumnType("character varying");

            builder
                .Property(x => x.Formula)
                .HasColumnName("formula")
                .HasColumnType("jsonb");

            builder
                .Property(x => x.Año)
                .HasColumnName("año")
                .HasPrecision(32, 0);

            builder
                .Property(x => x.IsDenFijo)
                .HasColumnName("is_den_fijo");

            builder
                .Property(x => x.IsTasa)
                .HasColumnName("is_tasa");

            builder
                .Property(x => x.FormulaDenFijo)
                .HasColumnName("formula_den_fijo")
                .HasColumnType("character varying");

            builder
                .Property(x => x.Meta)
                .HasColumnName("meta")
                .HasPrecision(24);

            builder
                .Property(x => x.Peso)
                .HasColumnName("peso")
                .HasPrecision(24);

            builder
                .Property(x => x.Mensual)
                .HasColumnName("mensual");

            builder
                .Property(x => x.Orden)
                .HasColumnName("orden")
                .HasPrecision(32, 0);

            builder
                .Property(x => x.Detalle)
                .HasColumnName("detalle")
                .HasColumnType("character varying");

            builder
                .Property(x => x.Tipoindicador)
                .HasColumnName("id_tipoindicador")
                .HasPrecision(32, 0);

            builder
                .Property(x => x.EsPeriodoOctubreSep)
                .HasColumnName("es_periodo_octubre_sep")
                .HasDefaultValue(false);

            builder
                .Property(x => x.IsColaborativo)
                .HasColumnName("is_colaborativo");

            builder
                .ToTable("indicador", "REMTool");
        }
    }
}
