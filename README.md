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
  OpcionesCorreo.cs     Configuración del envío de correo
Services/
  PropiedadesService.cs Catálogo en memoria + búsqueda y filtros
  CorreoService.cs      Envío por SMTP de las consultas de los formularios
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
  Sitemap               Mapa del sitio en /sitemap.xml
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

## Formularios y envío de correo

Los formularios de **Contacto** y **Tasación** validan del lado del servidor,
tienen un campo trampa contra robots y registran la consulta en el log.

Además intentan enviarla por correo a la inmobiliaria. El envío se configura en
la sección `Correo` de `appsettings.json`:

```json
"Correo": {
  "Habilitado": true,
  "Servidor": "smtp.gmail.com",
  "Puerto": 587,
  "UsarSsl": true,
  "Usuario": "casilla@gmail.com",
  "Clave": "",
  "Remitente": "casilla@gmail.com",
  "Destinatario": "horacioenricci@gmail.com"
}
```

**La contraseña no se guarda en el repositorio.** En desarrollo conviene usar
user-secrets y en el servidor, una variable de entorno:

```bash
dotnet user-secrets set "Correo:Clave" "la-contraseña-de-aplicación"
# o bien, en el servidor:
export Correo__Clave="la-contraseña-de-aplicación"
```

Con Gmail hay que generar una *contraseña de aplicación* (no sirve la del
correo) y tener la verificación en dos pasos activada.

Si el envío está apagado o el servidor de correo falla, el formulario **no se
rompe**: la consulta queda en el log y la pantalla de confirmación le ofrece al
visitante mandar el mismo mensaje por WhatsApp o por correo con un clic.

## Buscadores

`/sitemap.xml` se genera solo a partir de las páginas fijas y de cada propiedad
publicada. `wwwroot/robots.txt` lo declara: si el dominio final no es
`www.enriccipropiedades.com.ar`, hay que actualizar esa línea.

## Datos de contacto

Todos los datos (dirección, teléfonos, correo, horarios) se editan en un único
archivo: `Models/SitioInfo.cs`.
