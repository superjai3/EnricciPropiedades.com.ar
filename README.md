# EnricciPropiedades.com.ar

Sitio web de **R. H. Enricci Propiedades**, inmobiliaria de la Ciudad Autónoma de
Buenos Aires con oficina en Solís 581 (Monserrat) desde 1932.

Aplicación **ASP.NET Core Razor Pages** (net8.0) con base **SQLite**, sin
dependencias de front-end: el diseño, los componentes y los comportamientos son
propios.

## Cómo ejecutarlo

```bash
dotnet restore
dotnet run
```

Luego abrir la URL que muestra la consola (por defecto `https://localhost:7227`).

La base de datos se crea sola en el primer arranque: aplica las migraciones,
carga un catálogo de ejemplo de 12 propiedades y da de alta el usuario del panel.

## Estructura

```
Models/
  Propiedad.cs          Publicación (operación, tipo, superficies, precio, fotos…)
  Usuario.cs            Usuario del panel; guarda hash y sal, nunca la contraseña
  SitioInfo.cs          Datos de contacto de la inmobiliaria en un único lugar
  ArteFachada.cs        Portada SVG generada para publicaciones sin fotografía
  OpcionesCorreo.cs     Configuración del envío de correo
Data/
  EnricciContexto.cs    DbContext: propiedades y usuarios
  SembradorInicial.cs   Migración, catálogo de ejemplo y alta del administrador
  Migraciones/          Historial de cambios del esquema
Services/
  PropiedadesService.cs Catálogo: consulta pública, búsqueda y ABM del panel
  UsuariosService.cs    Autenticación y cambio de contraseña
  ClaveHash.cs          PBKDF2-SHA256, 210.000 iteraciones
  FotosService.cs       Alta y baja de las fotos que se suben desde el panel
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
  Admin/                Panel de administración (requiere sesión iniciada)
    Ingresar            Pantalla de acceso
    Index               Listado de publicaciones con alta, edición y baja
    Editar              Formulario de carga y edición, con subida de fotos
    Cuenta              Cambio de contraseña
    Salir               Cierre de sesión (sólo por POST)
  Shared/
    _Layout             Encabezado, navegación, pie y botón flotante de WhatsApp
    _Iconos             Sprite SVG de íconos
    _TarjetaPropiedad   Tarjeta reutilizable de publicación
wwwroot/
  css/enricci.css       Sistema de diseño completo (tokens, componentes, utilidades)
  css/panel.css         Lo propio del panel, apoyado en los mismos tokens
  js/enricci.js         Tema claro/oscuro, menú móvil, acordeones, animaciones
  fonts/                Tipografías auto-alojadas (Be Vietnam Pro y Manuale)
  imagenes/             Logo y fotografías; las cargadas desde el panel van a
                        imagenes/propiedades/{id}/ y no se versionan
db/                     Scripts SQL sueltos, fuera de wwwroot para que no se sirvan
```

## Panel de administración

Se entra por `/Admin/Ingresar`, o por el enlace **Administrar** al pie del sitio.
Desde ahí se publican, editan y dan de baja las propiedades, se suben fotos y se
elige cuáles aparecen en la portada.

### El usuario del panel

En el primer arranque se crea un único usuario a partir de la sección `Admin` de
`appsettings.json`:

```json
"Admin": {
  "Nombre": "Horacio Enricci",
  "Email": "horacioenricci@gmail.com",
  "ClaveInicial": ""
}
```

Con `ClaveInicial` vacía —que es lo recomendado— **se genera una contraseña al
azar y se escribe una sola vez en el log de arranque**. Hay que anotarla en ese
momento: no se vuelve a mostrar y en la base sólo queda su hash. Al primer
ingreso el panel obliga a cambiarla.

Si se prefiere fijar la contraseña inicial, conviene hacerlo por fuera del
repositorio:

```bash
dotnet user-secrets set "Admin:ClaveInicial" "la-que-quieras"
# o bien, en el servidor:
export Admin__ClaveInicial="la-que-quieras"
```

Si se pierde la contraseña, se borra la fila de la tabla `Usuarios` y en el
siguiente arranque se crea de nuevo con una clave nueva.

### Estados de una publicación

- **Disponible** — se ve en el sitio.
- **Reservada** — se ve, con el estado a la vista.
- **Dada de baja** — desaparece del sitio, del buscador y del sitemap, pero sigue
  en el panel por si hay que volver a publicarla. Es la forma de retirar una
  propiedad sin perder los datos; *Eliminar* sí borra todo, fotos incluidas.

### Fotos

Se suben desde el formulario de la publicación: JPG, PNG o WEBP, hasta 8 MB cada
una y 12 por propiedad. Se guardan en `wwwroot/imagenes/propiedades/{id}/` con un
nombre generado, nunca con el del archivo subido, y se valida la firma del
archivo además de la extensión. **La primera foto es la portada.** Si una
publicación no tiene ninguna, el sitio dibuja una portada vectorial generada a
partir del `Id`, así nunca queda una imagen rota.

## Seguridad

- **Cabeceras en todas las respuestas** (`Services/CabecerasSeguridad.cs`): una
  política de contenido (CSP) estricta, `nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy` y `Permissions-Policy`. Kestrel no anuncia el servidor.
- **CSP con nonce.** Los pocos `<script>` en línea del sitio se autorizan con un
  valor distinto en cada pedido; `script-src` **no** usa `unsafe-inline`, así que
  un texto inyectado no se ejecuta aunque llegue a la página. Por eso **no puede
  haber manejadores de eventos escritos en el HTML** (`onclick`, `onsubmit`…):
  para confirmar una acción destructiva se usa el atributo `data-confirmar`, que
  atiende `enricci.js`. Si agregás un script en línea, acordate del nonce:

  ```cshtml
  <script nonce="@Context.Nonce()"> … </script>
  ```

  Dentro de un `@section` el `Context` no está en alcance: tomá el valor arriba
  de la página con `ViewContext.HttpContext.Nonce()`.
- **Bloqueo de cuenta.** A los 5 intentos fallidos seguidos la cuenta queda
  bloqueada 5 minutos, y el plazo se duplica con cada tanda hasta 2 horas. Se
  reinicia con el primer ingreso correcto.
- **Freno por dirección IP** en la pantalla de ingreso: 20 pedidos por minuto.
  Uno corta la fuerza bruta contra una cuenta; el otro, contra muchas.
- **Cookies** de sesión y de antiforgery con `HttpOnly` y, en producción,
  `Secure`. El cierre de sesión es sólo por POST.
- El mensaje de ingreso fallido es siempre el mismo, de modo que no se puede
  averiguar qué correos existen.

## Diseño y accesibilidad

El sitio es responsive de 320 px para arriba, con el mismo sistema de diseño en
el panel. Algunas decisiones que conviene no deshacer sin querer:

- Las grillas usan `minmax(min(100%, Npx), 1fr)`: sin el `min()` desbordan en
  pantallas angostas.
- `body` lleva `overflow-x: clip` y no `hidden`, porque `hidden` crea un
  contenedor de scroll que rompe el `position: sticky` de la ficha y del panel.
- Los campos de formulario pasan a 16 px abajo de 860 px: con menos, Safari en
  iPhone amplía la página al enfocarlos.
- La tabla del panel se convierte en tarjetas abajo de 720 px; cada celda toma
  su rótulo del atributo `data-rotulo`.
- Los blancos táctiles llegan a 44 px bajo `@media (pointer: coarse)`.
- Las alturas de pantalla completa usan `dvh` con `vh` de respaldo.

## Base de datos

SQLite, en el archivo que indique `ConnectionStrings:Enricci` (por defecto
`enricci.db`, junto a la aplicación). La ruta relativa se resuelve contra la
carpeta del sitio, no contra el directorio de trabajo.

**El respaldo es copiar dos cosas:** el archivo `.db` y la carpeta
`wwwroot/imagenes/propiedades/`. Ninguna de las dos se versiona.

Para cambiar el esquema:

```bash
dotnet dotnet-ef migrations add NombreDelCambio --output-dir Data/Migraciones
```

Las migraciones pendientes se aplican solas al arrancar.

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
publicada; las dadas de baja no figuran. `wwwroot/robots.txt` lo declara: si el
dominio final no es `www.enriccipropiedades.com.ar`, hay que actualizar esa
línea. El panel lleva `noindex, nofollow`.

## Datos de contacto

Todos los datos (dirección, teléfonos, correo, horarios, Instagram) se editan en
un único archivo: `Models/SitioInfo.cs`.
