using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enricci_Propiedades.Data.Migraciones
{
    /// <summary>
    /// Carga por única vez las publicaciones reales de la inmobiliaria, tomadas
    /// de su perfil de Argenprop. Va como migración y no en el sembrador para
    /// que corra una sola vez por base: si Horacio da de baja alguna, no
    /// reaparece en el siguiente arranque.
    ///
    /// Sólo se vuelcan los datos que la ficha de origen declara. Donde decía
    /// "no figura" queda 0, que el sitio muestra como dato ausente, en lugar de
    /// inventar un valor. Los precios "a consultar" van en 0, que es como el
    /// modelo representa "Consultar".
    /// </summary>
    public partial class ImportaCatalogoReal : Migration
    {
        /// <summary>
        /// Medianoche UTC y no la hora de la importación: el sello tiene que
        /// quedar en el pasado con seguridad. Con una hora futura, estas
        /// publicaciones se ordenarían por encima de cualquier edición real,
        /// porque el panel y la portada ordenan por fecha de actualización.
        /// </summary>
        private static readonly DateTime Importacion = new(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] Columnas =
        {
            "Titulo", "Direccion", "Barrio", "Operacion", "Tipo", "Estado", "Moneda",
            "Precio", "Expensas", "Ambientes", "Dormitorios", "Banios",
            "SuperficieCubierta", "SuperficieTotal", "Antiguedad",
            "Cochera", "Balcon", "AptoCredito", "Destacada",
            "Descripcion", "Comodidades", "Fotos", "FechaAlta", "FechaActualizacion"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Propiedades",
                columns: Columnas,
                values: new object[,]
                {
                    {
                        "Tres ambientes con dependencias al frente, en el Edificio Galli",
                        "Av. Entre Ríos 600, piso 1", "Congreso", "Venta", "Departamento", "Disponible", "USD",
                        143000d, 230000d, 3, 2, 0, 86, 100, 50,
                        false, true, false, false,
                        "Tres ambientes con dependencias en el Edificio Galli: 86 m² cubiertos más un patio " +
                        "cubierto de 14 m², que llevan la superficie total a 100 m². Balcón de 1,50 m² al " +
                        "frente y baulera. Muy luminoso. Se estudia una operación de USD 100.000 más cuotas.",
                        "[\"Dependencias\",\"Al frente\",\"Balc\\u00F3n\",\"Baulera\",\"Patio cubierto\",\"Muy luminoso\"]",
                        "[]", Importacion, Importacion
                    },
                    {
                        "Tres ambientes con dependencias, balcón a la calle y patio interno",
                        "Av. Entre Ríos 600, piso 1", "Congreso", "Alquiler", "Departamento", "Disponible", "ARS",
                        1200000d, 230000d, 3, 2, 0, 86, 100, 50,
                        false, true, false, false,
                        "El mismo departamento del Edificio Galli, también disponible en alquiler: tres " +
                        "ambientes con dependencias, 86 m² cubiertos más un patio cubierto de 14 m² que " +
                        "totalizan 100 m². Balcón a la calle, baulera y patio interno.",
                        "[\"Dependencias\",\"Balc\\u00F3n a la calle\",\"Baulera\",\"Patio interno\",\"Al frente\"]",
                        "[]", Importacion, Importacion
                    },
                    {
                        "Un ambiente apto profesional, al norte y con vista abierta",
                        "Av. Rivadavia 1300, piso 12", "Congreso", "Venta", "Departamento", "Disponible", "USD",
                        45000d, 125000d, 1, 0, 1, 24, 0, 50,
                        false, false, false, false,
                        "Departamento de un ambiente con baño completo y kitchenette con placard. " +
                        "Orientación norte, vista abierta y mucha luz durante todo el día. Apto " +
                        "profesional. Se escuchan ofertas.",
                        "[\"Apto profesional\",\"Orientaci\\u00F3n norte\",\"Vista abierta\",\"Muy luminoso\",\"Kitchenette con placard\"]",
                        "[]", Importacion, Importacion
                    },
                    {
                        "Cochera fija cubierta sobre Av. Rivadavia",
                        "Av. Rivadavia 1300", "Congreso", "Venta", "Cochera", "Disponible", "USD",
                        9500d, 24992d, 0, 0, 0, 0, 0, 0,
                        true, false, false, false,
                        "Cochera fija y cubierta, con acceso por rampa, en pleno Congreso y a metros " +
                        "de la avenida.",
                        "[\"Fija\",\"Cubierta\",\"Acceso por rampa\"]",
                        "[]", Importacion, Importacion
                    },
                    {
                        "Monoambiente divisible al contrafrente, con balcón y renta",
                        "Av. Belgrano 1600, piso 7", "Monserrat", "Venta", "Departamento", "Disponible", "USD",
                        0d, 80000d, 1, 0, 1, 32, 0, 20,
                        false, true, false, false,
                        "Monoambiente divisible de 7 x 3,5 metros con cocina integrada, al contrafrente " +
                        "y con balcón. Muy luminoso. Se vende con renta en curso.",
                        "[\"Divisible\",\"Balc\\u00F3n\",\"Contrafrente\",\"Cocina integrada\",\"Con renta\",\"Muy luminoso\"]",
                        "[]", Importacion, Importacion
                    },
                    {
                        "Local a la calle con frente de blindex sobre Solís",
                        "Solís 300", "Congreso", "Venta", "LocalComercial", "Disponible", "USD",
                        0d, 40000d, 0, 0, 1, 0, 38, 0,
                        false, false, false, false,
                        "Local a la calle de aproximadamente 8 metros de frente por 5 de fondo, con " +
                        "frente de blindex, baño pequeño y kitchenette. Destino comercial.",
                        "[\"A la calle\",\"Frente de blindex\",\"Kitchenette\",\"Destino comercial\"]",
                        "[]", Importacion, Importacion
                    },
                    {
                        "Cochera en Riobamba, a media cuadra de Corrientes",
                        "Riobamba 300", "Congreso", "Venta", "Cochera", "Disponible", "USD",
                        17000d, 0d, 0, 0, 0, 0, 0, 0,
                        true, false, false, false,
                        "Cochera en el segundo piso, con rampa muy cómoda y seguridad las 24 horas. " +
                        "A media cuadra de Av. Corrientes y a una cuadra de Av. Callao.",
                        "[\"Rampa c\\u00F3moda\",\"Seguridad 24 h\",\"Segundo piso\"]",
                        "[]", Importacion, Importacion
                    },
                    {
                        "PH de estilo francés con entrada independiente en Palermo",
                        "Humboldt 2300, piso 1", "Palermo", "Venta", "PH", "Disponible", "USD",
                        330000d, 0d, 0, 0, 2, 0, 0, 60,
                        false, false, false, false,
                        "PH de estilo francés con entrada independiente, en un edificio reciclado a " +
                        "nuevo. Uso comercial para varios destinos y también apto vivienda.",
                        "[\"Entrada independiente\",\"Estilo franc\\u00E9s\",\"Reciclado a nuevo\",\"Apto vivienda\",\"Uso comercial\"]",
                        "[]", Importacion, Importacion
                    },
                    {
                        "Edificio hotelero de 48 habitaciones en Copacabana, Río de Janeiro",
                        "Copacabana, Río de Janeiro, Brasil", "Río de Janeiro", "Venta", "LocalComercial", "Disponible", "USD",
                        0d, 0d, 0, 0, 0, 0, 1500, 0,
                        false, false, false, false,
                        "Venta de un edificio hotelero de 48 habitaciones y 1.500 m² en Copacabana, " +
                        "entre Copacabana e Ipanema, Río de Janeiro. Dirección y precio a consultar.",
                        "[\"48 habitaciones\",\"1.500 m\\u00B2\",\"Copacabana\",\"Precio a consultar\"]",
                        "[]", Importacion, Importacion
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Se borran por dirección y operación, que es lo que identifica cada
            // publicación importada, y no por Id, que lo asigna la base.
            migrationBuilder.Sql(
                """
                DELETE FROM Propiedades
                WHERE FechaAlta = '2026-08-19 00:00:00'
                  AND Direccion IN (
                      'Av. Entre Ríos 600, piso 1',
                      'Av. Rivadavia 1300, piso 12',
                      'Av. Rivadavia 1300',
                      'Av. Belgrano 1600, piso 7',
                      'Solís 300',
                      'Riobamba 300',
                      'Humboldt 2300, piso 1',
                      'Copacabana, Río de Janeiro, Brasil'
                  );
                """);
        }
    }
}
