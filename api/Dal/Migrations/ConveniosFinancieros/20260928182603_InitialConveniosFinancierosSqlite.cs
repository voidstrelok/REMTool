using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Dal.Migrations.ConveniosFinancieros
{
    /// <inheritdoc />
    public partial class InitialConveniosFinancierosSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "convenio_financiero",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    codigo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    anio = table.Column<int>(type: "INTEGER", nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    fecha_inicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    fecha_termino = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    responsable = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_convenio_financiero", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proveedor_financiero",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedor_financiero", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item_presupuestario",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    convenio_financiero_id = table.Column<int>(type: "INTEGER", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_presupuestario", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_presupuestario_convenio_financiero_convenio_financiero_id",
                        column: x => x.convenio_financiero_id,
                        principalTable: "convenio_financiero",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sub_item_presupuestario",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    item_presupuestario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    presupuesto_asignado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sub_item_presupuestario", x => x.id);
                    table.ForeignKey(
                        name: "FK_sub_item_presupuestario_item_presupuestario_item_presupuestario_id",
                        column: x => x.item_presupuestario_id,
                        principalTable: "item_presupuestario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_financiero",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    convenio_financiero_id = table.Column<int>(type: "INTEGER", nullable: false),
                    item_presupuestario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    sub_item_presupuestario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    proveedor_financiero_id = table.Column<int>(type: "INTEGER", nullable: true),
                    fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    tipo_movimiento = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    tipo_documento = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    numero_documento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    proveedor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    observaciones = table.Column<string>(type: "TEXT", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_financiero", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimiento_financiero_convenio_financiero_convenio_financiero_id",
                        column: x => x.convenio_financiero_id,
                        principalTable: "convenio_financiero",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_movimiento_financiero_item_presupuestario_item_presupuestario_id",
                        column: x => x.item_presupuestario_id,
                        principalTable: "item_presupuestario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_financiero_proveedor_financiero_proveedor_financiero_id",
                        column: x => x.proveedor_financiero_id,
                        principalTable: "proveedor_financiero",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_financiero_sub_item_presupuestario_sub_item_presupuestario_id",
                        column: x => x.sub_item_presupuestario_id,
                        principalTable: "sub_item_presupuestario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_convenio_financiero_anio_estado",
                table: "convenio_financiero",
                columns: new[] { "anio", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_item_presupuestario_convenio_financiero_id",
                table: "item_presupuestario",
                column: "convenio_financiero_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_financiero_convenio_financiero_id_fecha",
                table: "movimiento_financiero",
                columns: new[] { "convenio_financiero_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_financiero_item_presupuestario_id",
                table: "movimiento_financiero",
                column: "item_presupuestario_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_financiero_proveedor_financiero_id",
                table: "movimiento_financiero",
                column: "proveedor_financiero_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_financiero_sub_item_presupuestario_id",
                table: "movimiento_financiero",
                column: "sub_item_presupuestario_id");

            migrationBuilder.CreateIndex(
                name: "IX_proveedor_financiero_nombre_normalizado",
                table: "proveedor_financiero",
                column: "nombre_normalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sub_item_presupuestario_item_presupuestario_id_nombre",
                table: "sub_item_presupuestario",
                columns: new[] { "item_presupuestario_id", "nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimiento_financiero");

            migrationBuilder.DropTable(
                name: "proveedor_financiero");

            migrationBuilder.DropTable(
                name: "sub_item_presupuestario");

            migrationBuilder.DropTable(
                name: "item_presupuestario");

            migrationBuilder.DropTable(
                name: "convenio_financiero");
        }
    }
}
