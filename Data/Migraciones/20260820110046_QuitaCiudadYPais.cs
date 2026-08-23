using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enricci_Propiedades.Data.Migraciones
{
    /// <inheritdoc />
    public partial class QuitaCiudadYPais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "Pais",
                table: "Propiedades");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
