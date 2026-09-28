using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RemTool.Shared;

#nullable disable

namespace RemTool.Dal.Migrations;

[DbContext(typeof(RemToolDataContext))]
[Migration("20260928190000_MoveBudgetsToSubItems")]
public partial class MoveBudgetsToSubItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "presupuesto_asignado",
            schema: "REMTool",
            table: "sub_item_presupuestario",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("""
            INSERT INTO "REMTool".sub_item_presupuestario (item_presupuestario_id, nombre, presupuesto_asignado, activo)
            SELECT i.id, 'General', 0, TRUE
            FROM "REMTool".item_presupuestario i
            WHERE NOT EXISTS (
                SELECT 1 FROM "REMTool".sub_item_presupuestario s
                WHERE s.item_presupuestario_id = i.id AND s.nombre = 'General'
            );
            """);

        migrationBuilder.Sql("""
            UPDATE "REMTool".sub_item_presupuestario s
            SET presupuesto_asignado = COALESCE((
                SELECT SUM(m.monto)
                FROM "REMTool".movimiento_financiero m
                WHERE m.sub_item_presupuestario_id = s.id
            ), 0);
            """);

        migrationBuilder.Sql("""
            UPDATE "REMTool".sub_item_presupuestario general
            SET presupuesto_asignado = general.presupuesto_asignado + GREATEST(
                i.presupuesto_asignado - COALESCE((
                    SELECT SUM(m.monto)
                    FROM "REMTool".movimiento_financiero m
                    WHERE m.item_presupuestario_id = i.id
                ), 0),
                0
            )
            FROM "REMTool".item_presupuestario i
            WHERE general.item_presupuestario_id = i.id
              AND general.nombre = 'General';
            """);

        migrationBuilder.DropColumn(name: "presupuesto_asignado", schema: "REMTool", table: "item_presupuestario");
        migrationBuilder.DropColumn(name: "presupuesto_total", schema: "REMTool", table: "convenio_financiero");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "presupuesto_asignado",
            schema: "REMTool",
            table: "item_presupuestario",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "presupuesto_total",
            schema: "REMTool",
            table: "convenio_financiero",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("""
            UPDATE "REMTool".item_presupuestario i
            SET presupuesto_asignado = COALESCE((
                SELECT SUM(s.presupuesto_asignado)
                FROM "REMTool".sub_item_presupuestario s
                WHERE s.item_presupuestario_id = i.id
            ), 0);
            """);

        migrationBuilder.Sql("""
            UPDATE "REMTool".convenio_financiero c
            SET presupuesto_total = COALESCE((
                SELECT SUM(i.presupuesto_asignado)
                FROM "REMTool".item_presupuestario i
                WHERE i.convenio_financiero_id = c.id
            ), 0);
            """);

        migrationBuilder.DropColumn(name: "presupuesto_asignado", schema: "REMTool", table: "sub_item_presupuestario");
    }
}
