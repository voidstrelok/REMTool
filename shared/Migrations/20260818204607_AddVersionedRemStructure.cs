using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedRemStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SEQUENCE IF NOT EXISTS ""REMTool"".version_hoja_rem_id_seq
                    AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
                CREATE SEQUENCE IF NOT EXISTS ""REMTool"".fila_seccion_rem_id_seq
                    AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
                CREATE SEQUENCE IF NOT EXISTS ""REMTool"".celda_seccion_rem_id_seq
                    AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
                CREATE SEQUENCE IF NOT EXISTS ""REMTool"".coordenada_prestacion_id_seq
                    AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
            ");

            migrationBuilder.AddColumn<string>(
                name: "codigo",
                schema: "REMTool",
                table: "seccion_rem",
                type: "character varying",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "columna_fin",
                schema: "REMTool",
                table: "seccion_rem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "columna_inicio",
                schema: "REMTool",
                table: "seccion_rem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "fila_fin",
                schema: "REMTool",
                table: "seccion_rem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "fila_inicio",
                schema: "REMTool",
                table: "seccion_rem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "html_estructura",
                schema: "REMTool",
                table: "seccion_rem",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "id_version_hoja",
                schema: "REMTool",
                table: "seccion_rem",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "offset_columna",
                schema: "REMTool",
                table: "seccion_rem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "offset_prestacion",
                schema: "REMTool",
                table: "seccion_rem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "orden",
                schema: "REMTool",
                table: "seccion_rem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "rango_base",
                schema: "REMTool",
                table: "seccion_rem",
                type: "character varying",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "rango_diccionario",
                schema: "REMTool",
                table: "seccion_rem",
                type: "character varying",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "id_fila_seccion",
                schema: "REMTool",
                table: "prestacion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "id_seccion",
                schema: "REMTool",
                table: "prestacion",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nombre",
                schema: "REMTool",
                table: "prestacion",
                type: "character varying",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "orden",
                schema: "REMTool",
                table: "prestacion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "fila_seccion_rem",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false, defaultValueSql: "nextval('\"REMTool\".fila_seccion_rem_id_seq'::regclass)"),
                    id_seccion = table.Column<long>(type: "bigint", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    fila_origen = table.Column<int>(type: "integer", nullable: false),
                    tiene_prestacion = table.Column<bool>(type: "boolean", nullable: false),
                    tipo_fila = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fila_seccion_rem", x => x.id);
                    table.ForeignKey(
                        name: "FK_fila_seccion_rem_seccion_rem_id_seccion",
                        column: x => x.id_seccion,
                        principalSchema: "REMTool",
                        principalTable: "seccion_rem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "version_hoja_rem",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false, defaultValueSql: "nextval('\"REMTool\".version_hoja_rem_id_seq'::regclass)"),
                    id_version = table.Column<int>(type: "integer", nullable: false),
                    id_hoja = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_version_hoja_rem", x => x.id);
                    table.ForeignKey(
                        name: "FK_version_hoja_rem_hoja_rem_id_hoja",
                        column: x => x.id_hoja,
                        principalSchema: "REMTool",
                        principalTable: "hoja_rem",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_version_hoja_rem_version_rem_id_version",
                        column: x => x.id_version,
                        principalSchema: "REMTool",
                        principalTable: "version_rem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "celda_seccion_rem",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false, defaultValueSql: "nextval('\"REMTool\".celda_seccion_rem_id_seq'::regclass)"),
                    id_fila = table.Column<int>(type: "integer", nullable: false),
                    fila_origen = table.Column<int>(type: "integer", nullable: false),
                    columna_origen = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<string>(type: "text", nullable: false),
                    celda_diccionario = table.Column<string>(type: "character varying", nullable: false),
                    celda_base = table.Column<string>(type: "character varying", nullable: false),
                    row_span = table.Column<int>(type: "integer", nullable: false),
                    col_span = table.Column<int>(type: "integer", nullable: false),
                    es_celda_ancla = table.Column<bool>(type: "boolean", nullable: false),
                    es_editable = table.Column<bool>(type: "boolean", nullable: false),
                    es_entrada_prestacion = table.Column<bool>(type: "boolean", nullable: false),
                    estilo_origen = table.Column<int>(type: "integer", nullable: false),
                    color_fondo = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_celda_seccion_rem", x => x.id);
                    table.ForeignKey(
                        name: "FK_celda_seccion_rem_fila_seccion_rem_id_fila",
                        column: x => x.id_fila,
                        principalSchema: "REMTool",
                        principalTable: "fila_seccion_rem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "coordenada_prestacion",
                schema: "REMTool",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", precision: 32, scale: 0, nullable: false, defaultValueSql: "nextval('\"REMTool\".coordenada_prestacion_id_seq'::regclass)"),
                    id_prestacion = table.Column<int>(type: "integer", nullable: false),
                    id_celda = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    codigo_columna = table.Column<string>(type: "character varying", nullable: false),
                    celda_diccionario = table.Column<string>(type: "character varying", nullable: false),
                    celda_base = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coordenada_prestacion", x => x.id);
                    table.ForeignKey(
                        name: "FK_coordenada_prestacion_celda_seccion_rem_id_celda",
                        column: x => x.id_celda,
                        principalSchema: "REMTool",
                        principalTable: "celda_seccion_rem",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_coordenada_prestacion_prestacion_id_prestacion",
                        column: x => x.id_prestacion,
                        principalSchema: "REMTool",
                        principalTable: "prestacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seccion_rem_id_version_hoja_orden",
                schema: "REMTool",
                table: "seccion_rem",
                columns: new[] { "id_version_hoja", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prestacion_id_fila_seccion",
                schema: "REMTool",
                table: "prestacion",
                column: "id_fila_seccion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prestacion_id_seccion",
                schema: "REMTool",
                table: "prestacion",
                column: "id_seccion");

            migrationBuilder.CreateIndex(
                name: "IX_celda_seccion_rem_id_fila",
                schema: "REMTool",
                table: "celda_seccion_rem",
                column: "id_fila");

            migrationBuilder.CreateIndex(
                name: "IX_coordenada_prestacion_id_celda",
                schema: "REMTool",
                table: "coordenada_prestacion",
                column: "id_celda",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coordenada_prestacion_id_prestacion_orden",
                schema: "REMTool",
                table: "coordenada_prestacion",
                columns: new[] { "id_prestacion", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fila_seccion_rem_id_seccion_orden",
                schema: "REMTool",
                table: "fila_seccion_rem",
                columns: new[] { "id_seccion", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_version_hoja_rem_id_hoja",
                schema: "REMTool",
                table: "version_hoja_rem",
                column: "id_hoja");

            migrationBuilder.CreateIndex(
                name: "IX_version_hoja_rem_id_version_id_hoja",
                schema: "REMTool",
                table: "version_hoja_rem",
                columns: new[] { "id_version", "id_hoja" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_prestacion_fila_seccion_rem_id_fila_seccion",
                schema: "REMTool",
                table: "prestacion",
                column: "id_fila_seccion",
                principalSchema: "REMTool",
                principalTable: "fila_seccion_rem",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_prestacion_seccion_rem_id_seccion",
                schema: "REMTool",
                table: "prestacion",
                column: "id_seccion",
                principalSchema: "REMTool",
                principalTable: "seccion_rem",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_seccion_rem_version_hoja_rem_id_version_hoja",
                schema: "REMTool",
                table: "seccion_rem",
                column: "id_version_hoja",
                principalSchema: "REMTool",
                principalTable: "version_hoja_rem",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prestacion_fila_seccion_rem_id_fila_seccion",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.DropForeignKey(
                name: "FK_prestacion_seccion_rem_id_seccion",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.DropForeignKey(
                name: "FK_seccion_rem_version_hoja_rem_id_version_hoja",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropTable(
                name: "coordenada_prestacion",
                schema: "REMTool");

            migrationBuilder.DropTable(
                name: "version_hoja_rem",
                schema: "REMTool");

            migrationBuilder.DropTable(
                name: "celda_seccion_rem",
                schema: "REMTool");

            migrationBuilder.DropTable(
                name: "fila_seccion_rem",
                schema: "REMTool");

            migrationBuilder.DropIndex(
                name: "IX_seccion_rem_id_version_hoja_orden",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropIndex(
                name: "IX_prestacion_id_fila_seccion",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.DropIndex(
                name: "IX_prestacion_id_seccion",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.DropColumn(
                name: "codigo",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "columna_fin",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "columna_inicio",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "fila_fin",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "fila_inicio",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "html_estructura",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "id_version_hoja",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "offset_columna",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "offset_prestacion",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "orden",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "rango_base",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "rango_diccionario",
                schema: "REMTool",
                table: "seccion_rem");

            migrationBuilder.DropColumn(
                name: "id_fila_seccion",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.DropColumn(
                name: "id_seccion",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.DropColumn(
                name: "nombre",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.DropColumn(
                name: "orden",
                schema: "REMTool",
                table: "prestacion");

            migrationBuilder.Sql(@"
                DROP SEQUENCE IF EXISTS ""REMTool"".coordenada_prestacion_id_seq;
                DROP SEQUENCE IF EXISTS ""REMTool"".celda_seccion_rem_id_seq;
                DROP SEQUENCE IF EXISTS ""REMTool"".fila_seccion_rem_id_seq;
                DROP SEQUENCE IF EXISTS ""REMTool"".version_hoja_rem_id_seq;
            ");
        }
    }
}
