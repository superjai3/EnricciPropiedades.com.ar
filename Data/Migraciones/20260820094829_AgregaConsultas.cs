using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enricci_Propiedades.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregaConsultas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Consultas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Origen = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PropiedadId = table.Column<int>(type: "INTEGER", nullable: true),
                    PropiedadTitulo = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CorreoEnviado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Atendida = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaAtendida = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_Atendida",
                table: "Consultas",
                column: "Atendida");

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_Fecha",
                table: "Consultas",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Consultas");
        }
    }
}
