using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalCellMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dependencias_total",
                schema: "REMTool",
                table: "celda_seccion_rem",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "es_total",
                schema: "REMTool",
                table: "celda_seccion_rem",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "formula_origen",
                schema: "REMTool",
                table: "celda_seccion_rem",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "operacion_total",
                schema: "REMTool",
                table: "celda_seccion_rem",
                type: "character varying",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tipo_total",
                schema: "REMTool",
                table: "celda_seccion_rem",
                type: "character varying",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dependencias_total",
                schema: "REMTool",
                table: "celda_seccion_rem");

            migrationBuilder.DropColumn(
                name: "es_total",
                schema: "REMTool",
                table: "celda_seccion_rem");

            migrationBuilder.DropColumn(
                name: "formula_origen",
                schema: "REMTool",
                table: "celda_seccion_rem");

            migrationBuilder.DropColumn(
                name: "operacion_total",
                schema: "REMTool",
                table: "celda_seccion_rem");

            migrationBuilder.DropColumn(
                name: "tipo_total",
                schema: "REMTool",
                table: "celda_seccion_rem");
        }
    }
}
