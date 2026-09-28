using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RemTool.Shared;

#nullable disable

namespace RemTool.Dal.Migrations;

[DbContext(typeof(RemToolDataContext))]
[Migration("20260928130000_AddConveniosFinancieros")]
public partial class AddConveniosFinancieros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "convenio_financiero", schema: "REMTool", columns: table => new
        {
            id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true), anio = table.Column<int>(nullable: false),
            descripcion = table.Column<string>(type: "text", nullable: true), fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false), fecha_termino = table.Column<DateOnly>(type: "date", nullable: false),
            presupuesto_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false), responsable = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false), fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_convenio_financiero", x => x.id));
        migrationBuilder.CreateTable(name: "item_presupuestario", schema: "REMTool", columns: table => new
        {
            id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), convenio_financiero_id = table.Column<int>(nullable: false),
            nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false), presupuesto_asignado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false), activo = table.Column<bool>(nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_item_presupuestario", x => x.id); table.ForeignKey("FK_item_presupuestario_convenio_financiero", x => x.convenio_financiero_id, principalSchema: "REMTool", principalTable: "convenio_financiero", principalColumn: "id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable(name: "movimiento_financiero", schema: "REMTool", columns: table => new
        {
            id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), convenio_financiero_id = table.Column<int>(nullable: false), item_presupuestario_id = table.Column<int>(nullable: true),
            fecha = table.Column<DateOnly>(type: "date", nullable: false), descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false), monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            tipo_movimiento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false), tipo_documento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true), numero_documento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true), proveedor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true), observaciones = table.Column<string>(type: "text", nullable: true), fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_movimiento_financiero", x => x.id); table.ForeignKey("FK_movimiento_financiero_convenio", x => x.convenio_financiero_id, principalSchema: "REMTool", principalTable: "convenio_financiero", principalColumn: "id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_movimiento_financiero_item", x => x.item_presupuestario_id, principalSchema: "REMTool", principalTable: "item_presupuestario", principalColumn: "id", onDelete: ReferentialAction.Restrict); });
        migrationBuilder.CreateIndex("IX_convenio_financiero_anio_estado", "convenio_financiero", new[] { "anio", "estado" }, schema: "REMTool");
        migrationBuilder.CreateIndex("IX_item_presupuestario_convenio_financiero_id", "item_presupuestario", "convenio_financiero_id", schema: "REMTool");
        migrationBuilder.CreateIndex("IX_movimiento_financiero_convenio_financiero_id_fecha", "movimiento_financiero", new[] { "convenio_financiero_id", "fecha" }, schema: "REMTool");
        migrationBuilder.CreateIndex("IX_movimiento_financiero_item_presupuestario_id", "movimiento_financiero", "item_presupuestario_id", schema: "REMTool");
    }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable("movimiento_financiero", "REMTool"); migrationBuilder.DropTable("item_presupuestario", "REMTool"); migrationBuilder.DropTable("convenio_financiero", "REMTool"); }
}
