using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeRegistroLoad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_registro_id_reporte_id_prestacion",
                schema: "REMTool",
                table: "registro",
                columns: new[] { "id_reporte", "id_prestacion" });

            migrationBuilder.CreateIndex(
                name: "IX_prestacion_id_version",
                schema: "REMTool",
                table: "prestacion",
                column: "id_version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_registro_id_reporte_id_prestacion",
                schema: "REMTool",
                table: "registro");

            migrationBuilder.DropIndex(
                name: "IX_prestacion_id_version",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.CreateIndex(
                name: "IX_registro_id_reporte",
                schema: "REMTool",
                table: "registro",
                column: "id_reporte");
        }
    }
}
