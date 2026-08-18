using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddPuntoResumen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "punto_resumen",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying", nullable: false),
                    categoria = table.Column<string>(type: "character varying", nullable: false),
                    expresion = table.Column<string>(type: "character varying", nullable: false),
                    id_serie_rem = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_punto_resumen", x => x.id);
                    table.ForeignKey(
                        name: "FK_punto_resumen_serie_rem_id_serie_rem",
                        column: x => x.id_serie_rem,
                        principalSchema: "REMTool",
                        principalTable: "serie_rem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_punto_resumen_id_serie_rem",
                schema: "REMTool",
                table: "punto_resumen",
                column: "id_serie_rem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "punto_resumen",
                schema: "REMTool");
        }
    }
}
