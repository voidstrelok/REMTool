using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddInformeRem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "informe_rem",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    id_serie_rem = table.Column<long>(type: "bigint", nullable: false),
                    seccion_ids = table.Column<List<long>>(type: "bigint[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_informe_rem", x => x.id);
                    table.ForeignKey(
                        name: "FK_informe_rem_serie_rem_id_serie_rem",
                        column: x => x.id_serie_rem,
                        principalSchema: "REMTool",
                        principalTable: "serie_rem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_informe_rem_id_serie_rem_nombre",
                schema: "REMTool",
                table: "informe_rem",
                columns: new[] { "id_serie_rem", "nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "informe_rem",
                schema: "REMTool");
        }
    }
}
