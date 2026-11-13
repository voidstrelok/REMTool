using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemTool.Shared.Migrations;

[Migration("20260914120000_SeedInformativos")]
public partial class SeedInformativos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "REMTool".informativo
                (tipo, titulo, contenido, url, texto_enlace, fecha_publicacion, vigente, destacado)
            VALUES
                (2,
                 'Nueva versión de REMTool disponible',
                 'Ya está disponible la nueva versión de REMTool: versión 1.2 (febrero de 2026). Esta actualización incorpora mejoras para revisar, consultar y trabajar con la información REM.',
                 NULL,
                 NULL,
                 CURRENT_DATE,
                 TRUE,
                 TRUE),
                (3,
                 'Novedades de la plataforma',
                 'Se incorporaron nuevas herramientas para facilitar el trabajo de la red: Consultar REM, Construir REM Serie A, Consolidados Excel e informativos para mantenerte al día.',
                 NULL,
                 NULL,
                 CURRENT_DATE,
                 TRUE,
                 TRUE),
                (2,
                 'Nuevo enlace para ingresar',
                 'Desde ahora puedes ingresar a la plataforma a través del nuevo sitio de Estadísticas de la Red Comunal de Salud de Monte Patria.',
                 'https://estadisticas.apsmontepatria.cl/',
                 'Ingresar a Estadísticas',
                 CURRENT_DATE,
                 TRUE,
                 TRUE),
                (1,
                 'Estadísticas del mes disponibles',
                 'Las estadísticas correspondientes al mes ya están subidas y disponibles para su consulta en la plataforma.',
                 NULL,
                 NULL,
                 CURRENT_DATE,
                 TRUE,
                 TRUE);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "REMTool".informativo
            WHERE titulo IN (
                'Nueva versión de REMTool disponible',
                'Novedades de la plataforma',
                'Nuevo enlace para ingresar',
                'Estadísticas del mes disponibles'
            );
            """);
    }
}
