# EnricciPropiedades.com.ar

Sitio web de **R. H. Enricci Propiedades**, inmobiliaria de la Ciudad Autónoma de
Buenos Aires con oficina en Solís 581 (Monserrat) desde 1932.

Aplicación **ASP.NET Core Razor Pages** (net7.0), sin dependencias de front-end:
el diseño, los componentes y los comportamientos son propios.

## Cómo ejecutarlo

```bash
dotnet restore
dotnet run
```

Luego abrir la URL que muestra la consola (por defecto `https://localhost:7xxx`).

## Estructura

```
Models/
  Propiedad.cs          Modelo de publicación (operación, tipo, superficies, precio…)
  SitioInfo.cs          Datos de contacto de la inmobiliaria en un único lugar
  ArteFachada.cs        Portada SVG generada para publicaciones sin fotografía
Services/
  PropiedadesService.cs Catálogo en memoria + búsqueda y filtros
Pages/
  Index                 Portada: hero, buscador, destacadas, servicios, barrios
  Propiedades           Listado con filtros por operación, tipo, barrio, ambientes y precio
  Ficha                 Detalle de una publicación (ruta /propiedad/{id})
  Servicios             Panorama de servicios + preguntas frecuentes
  Tasacion              Formulario de pedido de tasación
  Cobranza              Administración y cobranza de alquileres
  Asesoria_Legal        Servicios legales asociados
  Quienes_Somos         Historia, línea de tiempo y valores
  Mision / Vision       Páginas institucionales
  Contacto              Formulario de consulta
  Error                 404 y errores generales
  Shared/
    _Layout             Encabezado, navegación, pie y botón flotante de WhatsApp
    _Iconos             Sprite SVG de íconos
    _TarjetaPropiedad   Tarjeta reutilizable de publicación
wwwroot/
  css/enricci.css       Sistema de diseño completo (tokens, componentes, utilidades)
  js/enricci.js         Tema claro/oscuro, menú móvil, acordeones, animaciones
  fonts/                Tipografías auto-alojadas (Be Vietnam Pro y Manuale)
  imagenes/             Fotografías de las propiedades y logo
```

## Cómo cargar una propiedad nueva

Hoy el catálogo vive en memoria, en `Services/PropiedadesService.cs`
(método `Sembrar`). Para publicar una propiedad nueva basta con agregar un
objeto `Propiedad` a esa lista con un `Id` único.

Las fotos van en `wwwroot/imagenes/<Dirección>/` y se referencian en la
propiedad `Fotos` con la ruta codificada para URL, por ejemplo
`/imagenes/Solis%20700/Frente.jpg`. Si una publicación no tiene fotos, el sitio
dibuja una portada vectorial generada a partir del `Id`, así nunca queda una
imagen rota.

Cuando exista base de datos, alcanza con reemplazar la implementación de
`PropiedadesService` manteniendo la misma superficie pública.

## Formularios

Los formularios de **Contacto** y **Tasación** validan del lado del servidor y
registran la consulta en el log de la aplicación. Como todavía no hay servicio
de correo configurado, al enviarlos se ofrece un enlace con el mensaje ya
armado para WhatsApp y para correo electrónico, de modo que la consulta llegue
igual. Para envío automático de correo hay que sumar un servicio SMTP
(por ejemplo `MailKit`) en el `OnPost` de cada página.

## Datos de contacto

Todos los datos (dirección, teléfonos, correo, horarios) se editan en un único
archivo: `Models/SitioInfo.cs`.
