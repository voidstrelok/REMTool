using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RemTool.Shared;

#nullable disable

namespace RemTool.Shared.Migrations
{
    [DbContext(typeof(RemToolDataContext))]
    [Migration("20260819171200_OptimizeStructureLoad")]
    /// <inheritdoc />
    public partial class OptimizeStructureLoad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_hoja_rem_id_serie_rem_Nombre",
                schema: "REMTool",
                table: "hoja_rem",
                columns: new[] { "id_serie_rem", "nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_version_rem_id_serie_Nombre",
                schema: "REMTool",
                table: "version_rem",
                columns: new[] { "id_serie", "nombre" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hoja_rem_id_serie_rem_Nombre",
                schema: "REMTool",
                table: "hoja_rem");

            migrationBuilder.DropIndex(
                name: "IX_version_rem_id_serie_Nombre",
                schema: "REMTool",
                table: "version_rem");
        }
    }
}
