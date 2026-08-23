using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enricci_Propiedades.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregaCiudadYPais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "Propiedades",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pais",
                table: "Propiedades",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            // Las filas que ya existen quedarían con la cadena vacía. El catálogo
            // es de la Ciudad de Buenos Aires salvo una publicación en Brasil,
            // así que se rellena con eso y se corrige la excepción.
            migrationBuilder.Sql(
                """
                UPDATE Propiedades
                   SET Ciudad = 'Ciudad Autónoma de Buenos Aires',
                       Pais   = 'AR'
                 WHERE Ciudad = '' OR Ciudad IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE Propiedades
                   SET Ciudad = 'Río de Janeiro',
                       Pais   = 'BR'
                 WHERE Barrio = 'Río de Janeiro';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "Pais",
                table: "Propiedades");
        }
    }
}
