using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RemTool.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddFiltroEstablecimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filtro_establecimiento",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_establecimiento = table.Column<long>(type: "bigint", precision: 64, scale: 0, nullable: false),
                    id_indicador = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: true),
                    tipo = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filtro_establecimiento", x => x.id);
                    table.ForeignKey(
                        name: "FK_filtro_establecimiento_establecimiento_id_establecimiento",
                        column: x => x.id_establecimiento,
                        principalSchema: "REMTool",
                        principalTable: "establecimiento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_filtro_establecimiento_indicador_id_indicador",
                        column: x => x.id_indicador,
                        principalSchema: "REMTool",
                        principalTable: "indicador",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_filtro_establecimiento_id_establecimiento",
                schema: "REMTool",
                table: "filtro_establecimiento",
                column: "id_establecimiento",
                unique: true,
                filter: "\"id_indicador\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_filtro_establecimiento_id_establecimiento_id_indicador",
                schema: "REMTool",
                table: "filtro_establecimiento",
                columns: new[] { "id_establecimiento", "id_indicador" },
                unique: true,
                filter: "\"id_indicador\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_filtro_establecimiento_id_indicador",
                schema: "REMTool",
                table: "filtro_establecimiento",
                column: "id_indicador");

            // Seed: exclusiones globales que anteriormente estaban hard-codeadas en IndicadorService.
            // tipo = 0 â?' Excluir, id_indicador = NULL â?' regla global.
            migrationBuilder.InsertData(
                schema: "REMTool",
                table: "filtro_establecimiento",
                columns: new[] { "id_establecimiento", "id_indicador", "tipo" },
                values: new object[,]
                {
                    { 14L, null, 0 }, // SARMontePatria
                    { 15L, null, 0 }, // ClinicaDentalMovilMontePatria
                    { 16L, null, 0 }  // SURElPalqui
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filtro_establecimiento",
                schema: "REMTool");
        }
    }
}
