using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enricci_Propiedades.Data.Migraciones
{
    /// <summary>
    /// La importación del catálogo real quedó sellada a las 12:00 UTC, que en el
    /// momento de aplicarla todavía era hora futura. Como el panel y la portada
    /// ordenan por fecha de actualización, esas publicaciones se ubicaban por
    /// encima de cualquier alta o edición hecha por la inmobiliaria.
    ///
    /// Esto lleva el sello a medianoche del mismo día en las bases donde la
    /// importación ya corrió. Las bases nuevas ya lo reciben corregido.
    /// </summary>
    public partial class CorrigeFechaDeImportacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Propiedades
                SET FechaAlta          = '2026-08-19 00:00:00',
                    FechaActualizacion = '2026-08-19 00:00:00'
                WHERE FechaAlta = '2026-08-19 12:00:00'
                  AND FechaActualizacion = '2026-08-19 12:00:00';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Propiedades
                SET FechaAlta          = '2026-08-19 12:00:00',
                    FechaActualizacion = '2026-08-19 12:00:00'
                WHERE FechaAlta = '2026-08-19 00:00:00'
                  AND FechaActualizacion = '2026-08-19 00:00:00';
                """);
        }
    }
}
