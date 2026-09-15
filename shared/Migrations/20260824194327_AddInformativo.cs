using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddInformativo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "informativo",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    titulo = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    texto_enlace = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fecha_publicacion = table.Column<DateTime>(type: "date", nullable: false),
                    vigente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    destacado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_informativo", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_informativo_vigente_destacado_fecha_publicacion",
                schema: "REMTool",
                table: "informativo",
                columns: new[] { "vigente", "destacado", "fecha_publicacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "informativo",
                schema: "REMTool");
        }
    }
}
