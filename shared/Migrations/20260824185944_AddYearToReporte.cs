using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddYearToReporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""REMTool"".""IX_version_rem_id_serie"";
                DROP INDEX IF EXISTS ""REMTool"".""IX_hoja_rem_id_serie_rem"";");

            migrationBuilder.AddColumn<int>(
                name: "año",
                schema: "REMTool",
                table: "reporte",
                type: "integer",
                precision: 32,
                scale: 0,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""REMTool"".reporte AS r
                SET ""año"" = COALESCE(
                    (
                        SELECT EXTRACT(YEAR FROM MAX(v.fecha))::integer
                        FROM ""REMTool"".registro AS registro
                        INNER JOIN ""REMTool"".prestacion AS prestacion
                            ON prestacion.id = registro.id_prestacion
                        INNER JOIN ""REMTool"".version_rem AS v
                            ON v.id = prestacion.id_version
                        WHERE registro.id_reporte = r.id
                    ),
                    2026
                )
                WHERE r.""año"" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "año",
                schema: "REMTool",
                table: "reporte",
                type: "integer",
                precision: 32,
                scale: 0,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldPrecision: 32,
                oldScale: 0,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reporte_año_mes_id_establecimiento",
                schema: "REMTool",
                table: "reporte",
                columns: new[] { "año", "mes", "id_establecimiento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reporte_año_mes_id_establecimiento",
                schema: "REMTool",
                table: "reporte");

            migrationBuilder.DropColumn(
                name: "año",
                schema: "REMTool",
                table: "reporte");

            migrationBuilder.CreateIndex(
                name: "IX_version_rem_id_serie",
                schema: "REMTool",
                table: "version_rem",
                column: "id_serie");

            migrationBuilder.CreateIndex(
                name: "IX_hoja_rem_id_serie_rem",
                schema: "REMTool",
                table: "hoja_rem",
                column: "id_serie_rem");
        }
    }
}
