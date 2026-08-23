using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enricci_Propiedades.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Propiedades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Barrio = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Operacion = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Moneda = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Precio = table.Column<double>(type: "REAL", nullable: false),
                    Expensas = table.Column<double>(type: "REAL", nullable: false),
                    Ambientes = table.Column<int>(type: "INTEGER", nullable: false),
                    Dormitorios = table.Column<int>(type: "INTEGER", nullable: false),
                    Banios = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperficieCubierta = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperficieTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Antiguedad = table.Column<int>(type: "INTEGER", nullable: false),
                    Cochera = table.Column<bool>(type: "INTEGER", nullable: false),
                    Balcon = table.Column<bool>(type: "INTEGER", nullable: false),
                    AptoCredito = table.Column<bool>(type: "INTEGER", nullable: false),
                    Destacada = table.Column<bool>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Comodidades = table.Column<string>(type: "TEXT", nullable: false),
                    Fotos = table.Column<string>(type: "TEXT", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propiedades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    HashClave = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sal = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DebeCambiarClave = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimoIngreso = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_Barrio",
                table: "Propiedades",
                column: "Barrio");

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_Estado",
                table: "Propiedades",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_Operacion",
                table: "Propiedades",
                column: "Operacion");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Propiedades");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
