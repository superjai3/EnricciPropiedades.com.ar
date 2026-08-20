# EnricciPropiedades.com.ar

Sitio web de **R. H. Enricci Propiedades**, inmobiliaria de la Ciudad Autónoma de
Buenos Aires con oficina en Solís 642, piso 1º D (Monserrat).

Aplicación **ASP.NET Core Razor Pages** (net8.0) con base **SQLite**, sin
dependencias de front-end: el diseño, los componentes y los comportamientos son
propios.

Del lado del servidor la única dependencia que no es de Microsoft es
**SixLabors.ImageSharp**, que procesa las fotos que se suben desde el panel. Va
bajo la Six Labors Split License, que es gratuita para organizaciones de menos
de un millón de dólares de facturación anual.

## Cómo ejecutarlo

```bash
dotnet restore
dotnet run
```

Luego abrir la URL que muestra la consola (por defecto `https://localhost:7227`).

La base de datos se crea sola en el primer arranque: aplica las migraciones
—que traen el catálogo real de la inmobiliaria— y da de alta el usuario del
panel.

## Estructura

```
Models/
  Propiedad.cs          Publicación (operación, tipo, superficies, precio, fotos…)
  Usuario.cs            Usuario del panel; guarda hash y sal, nunca la contraseña
  SitioInfo.cs          Datos de contacto de la inmobiliaria en un único lugar
  Consulta.cs           Consulta recibida por los formularios; incluye la hora local
  OpcionesSitio.cs      Dominio del sitio, para las URL absolutas
  OpcionesRespaldo.cs   Configuración del respaldo automático
  ArteFachada.cs        Portada SVG generada para publicaciones sin fotografía
  OpcionesCorreo.cs     Configuración del envío de correo
Data/
  EnricciContexto.cs    DbContext: propiedades, usuarios y consultas
  SembradorInicial.cs   Migración, catálogo de ejemplo y alta del administrador
  Migraciones/          Historial de cambios del esquema
Services/
  PropiedadesService.cs Catálogo: consulta pública, búsqueda y ABM del panel
  UsuariosService.cs    Autenticación y cambio de contraseña
  ClaveHash.cs          PBKDF2-SHA256, 210.000 iteraciones
  ConsultasService.cs   Alta y seguimiento de las consultas del sitio
  FotosService.cs       Reduce a WEBP, genera miniatura y borra las fotos del panel
  RespaldoService.cs    Copia la base y las fotos en un .zip, con rotación
  RespaldoProgramado.cs Dispara el respaldo una vez por día
  RutaBaseDeDatos.cs    Resuelve dónde está el archivo .db
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
  Robots                robots.txt generado con el dominio configurado
  Admin/                Panel de administración (requiere sesión iniciada)
    Ingresar            Pantalla de acceso
    Index               Listado de publicaciones con alta, edición y baja
    Editar              Formulario de carga y edición, con subida de fotos
    Consultas           Consultas recibidas, con notas y marcado de atendidas
    Respaldos           Respaldos hechos, descarga y «Respaldar ahora»
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
elige cuáles aparecen en la portada. También se trabajan las **consultas** que
llegan por el sitio y se manejan los **respaldos**.

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

Se suben desde el formulario de la publicación: JPG, JFIF, PNG o WEBP, hasta
20 MB cada una y 12 por propiedad.

**Ninguna foto se publica como vino.** Al subirla se la procesa con ImageSharp:

- Se aplica la orientación EXIF, para que las fotos sacadas de costado no salgan
  acostadas.
- Se le sacan todos los metadatos. Además de pesar, las fotos de teléfono suelen
  traer las coordenadas GPS de dónde fueron sacadas: publicarlas sería dar la
  ubicación exacta de la propiedad sin haberlo decidido.
- Se guardan dos versiones en WEBP con calidad 78: la publicada, de 1600 px de
  lado mayor, y una miniatura de 600 px con el sufijo `-min` para las tarjetas
  del listado.

La diferencia no es menor: una foto de teléfono de 4032×3024 y 1,9 MB queda en
241 KB la grande y 77 KB la miniatura. Sin esto, el listado de propiedades
cargaba varios megabytes de fotos por visita.

Las publicaciones cargadas antes de que existieran las miniaturas no las tienen;
en ese caso el sitio usa la foto original, que se ve igual y sólo pesa más.

Los archivos van a `wwwroot/imagenes/propiedades/{id}/` con un nombre generado,
nunca con el del archivo subido. Se valida la firma del archivo además de la
extensión. **La primera foto es la portada** —y es también la que se ve al
compartir el aviso por WhatsApp o por redes—. Si una publicación no tiene
ninguna, el sitio dibuja una portada vectorial generada a partir del `Id`, así
nunca queda una imagen rota.

**HEIC/HEIF no se aceptan**: es el formato con el que el iPhone saca fotos por
omisión y los navegadores no lo muestran. El panel lo rechaza con la explicación
de cómo resolverlo (*Ajustes → Cámara → Formatos → Más compatible*, o convertir
a JPG). Lo mismo con AVIF, TIFF, BMP y GIF.

Cada rechazo queda en el log con el nombre, el tipo y el tamaño del archivo, y
se le muestra al usuario en rojo —nunca dentro del cartel verde de éxito—
devolviéndolo a la pantalla de edición para que pueda resolverlo.

### Qué aparece en la portada

La portada muestra hasta **6** publicaciones marcadas como destacadas,
**de la modificada más recientemente a la más antigua**. El orden importa: si se
ordenara por `Id`, una propiedad recién destacada quedaría siempre última y, en
cuanto hubiera más de seis, no llegaría a verse nunca.

Si no hay **ninguna** marcada, se muestran las seis últimas publicadas: la
sección no puede quedar vacía por una cuestión de curaduría. El panel avisa en
los dos casos —cuando faltan destacadas y cuando sobran—.

## El catálogo

Las publicaciones reales de la inmobiliaria entran por la migración
`ImportaCatalogoReal`, tomadas de su perfil de Argenprop. Va como migración y no
como sembrado para que corra **una sola vez por base**: si se da de baja alguna,
no reaparece en el siguiente arranque.

Sólo se volcaron los datos que la ficha de origen declaraba. Donde decía "no
figura" quedó en 0 —que el sitio muestra como dato ausente— en lugar de inventar
un valor, y los precios "a consultar" van en 0, que es como el modelo representa
*Consultar*. **Conviene revisarlas desde el panel**: superficies, baños y
antigüedades faltan en varias.

Aparte existe un catálogo de 12 propiedades **ficticias** para probar el sitio
con contenido. No se carga salvo que se pida:

```json
"Admin": { "CargarCatalogoDeEjemplo": true }
```

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

Lo que hay que respaldar son dos cosas: el archivo `.db` y la carpeta
`wwwroot/imagenes/propiedades/`. Ninguna de las dos se versiona, y de las dos
se ocupa el respaldo automático (ver *Respaldos*).

Para cambiar el esquema:

```bash
dotnet dotnet-ef migrations add NombreDelCambio --output-dir Data/Migraciones
```

Las migraciones pendientes se aplican solas al arrancar.

## Las consultas del sitio

Los formularios de **Contacto** y **Tasación** validan del lado del servidor y
tienen un campo trampa contra robots. Lo que llega se **guarda en la base** y se
ve en el panel, en *Consultas*: quién escribió, cuándo, por qué propiedad, y si
ya se le respondió. Cada una admite notas internas.

El orden importa y es a propósito: **primero se guarda, después se avisa por
correo**. Al revés —que es como estaba— una casilla mal configurada o un
servidor SMTP caído hacían desaparecer el contacto, porque el único registro era
el log. El correo es un aviso; el registro es la base.

Las que no se pudieron avisar quedan marcadas *Sin aviso por correo*, y el panel
avisa arriba de todo cuando hay alguna: es la señal de que el envío está apagado
o mal configurado.

La barra del panel lleva el número de consultas sin atender, para que no haga
falta entrar a mirar.

## Envío de correo

El aviso de las consultas se configura en la sección `Correo` de
`appsettings.json`:

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
rompe**: la consulta ya quedó guardada, y la pantalla de confirmación le ofrece
al visitante mandar el mismo mensaje por WhatsApp o por correo con un clic.

## Respaldos

Todo lo que no está en el repositorio —la base y las fotos cargadas desde el
panel— se respalda solo. Sección `Respaldo` de `appsettings.json`:

```json
"Respaldo": {
  "Habilitado": true,
  "Carpeta": "respaldos",
  "HoraDiaria": 3,
  "Conservar": 14
}
```

Cada corrida deja un único `.zip` con `enricci.db` y la carpeta
`imagenes/propiedades`, y borra los más viejos hasta dejar los `Conservar`
últimos. La hora es la de Buenos Aires.

La base **no se copia con un File.Copy**: con el sitio andando, el archivo puede
tener escrituras a medio confirmar en el diario (`-wal`) y la copia saldría
inconsistente. Se usa `VACUUM INTO`, que es la forma que tiene SQLite de sacar
una foto entera y coherente sin frenar el sitio.

Desde *Panel → Respaldos* se puede hacer uno a mano, ver los que hay y
**descargarlos**. Conviene bajar uno cada tanto o apuntar `Carpeta` a un disco
distinto o a una carpeta sincronizada con la nube: un respaldo que vive en el
mismo servidor no sirve el día que se pierde el servidor.

Para restaurar: parar el sitio, reemplazar `enricci.db` por el del zip, dejar la
carpeta de fotos en `wwwroot/imagenes/propiedades` y volver a arrancar.

## Rendimiento

- **Compresión de respuestas** con Brotli y gzip. El HTML del listado pasa de
  130 KB a 20 KB.
- **Caché de archivos estáticos**: un año e `immutable` para lo que estrena URL
  cuando cambia —el CSS y el JavaScript, que salen con `?v=…`, las tipografías y
  las fotos del catálogo, que llevan nombre generado— y una semana para el
  resto.
- Las fotos se reducen y se convierten a WEBP al subirlas (ver *Fotos*), y las
  tarjetas del listado usan la miniatura.

`EnableForHttps` viene apagado de fábrica por el ataque BREACH, que deduce un
secreto de la página midiendo cuánto comprime. Acá está activado a propósito: el
único secreto en el HTML es el token antiforgery, y ASP.NET Core lo genera
distinto en cada pedido justamente para que esa medición no sirva de nada.

## El dominio

El dominio vive en la configuración y no escrito dentro del código. Sección
`Sitio` de `appsettings.json`:

```json
"Sitio": {
  "Dominio": "www.enricci-propiedades.com.ar"
}
```

De ahí salen las URL canónicas, las de Open Graph, el `sitemap.xml` y la línea
`Sitemap:` de `robots.txt`. Con el dominio puesto se usa **siempre** ese, aunque
el visitante haya entrado por la IP o por un túnel de pruebas: si no, los
buscadores verían la misma página publicada en dos direcciones distintas y
repartirían el posicionamiento entre las dos.

**Vacío** —que es como viene— las URL absolutas salen del host del pedido, que
es lo correcto en desarrollo y mientras el dominio no esté dado de alta.

Cuando el dominio esté andando conviene además acotar `AllowedHosts`, que hoy
está en `*`:

```json
"AllowedHosts": "enricci-propiedades.com.ar;www.enricci-propiedades.com.ar"
```

Ojo con esto último: si el proxy reenvía otro nombre de host, el sitio responde
400 a todo. Conviene cambiarlo con el sitio ya publicado y andando, no antes.

## Buscadores

`/sitemap.xml` se genera solo a partir de las páginas fijas y de cada propiedad
publicada; las dadas de baja no figuran. `/robots.txt` también se genera —no es
un archivo suelto en `wwwroot`— para que la línea del sitemap salga siempre del
dominio configurado: escrita a mano se desactualiza en cuanto el sitio cambia de
dirección, y apuntar el sitemap a un dominio que ya no es queda como un error en
las herramientas de los buscadores.

Cada ficha lleva sus **datos estructurados** de schema.org (`RealEstateListing`
más el `BreadcrumbList` de la ruta): precio, moneda, ambientes, dormitorios,
baños, superficie en m² y comodidades. Con eso el buscador entiende que la
página es un aviso inmobiliario y puede mostrar esos datos en el resultado, en
vez de dos líneas de texto suelto. Los campos que faltan —que en la base son 0—
no se declaran, para no afirmar que la propiedad no tiene ninguno; un precio en
0 significa *Consultar* y tampoco se publica como oferta.

Al compartir una ficha, la vista previa muestra la **foto de la propiedad** y no
el isologo. El panel lleva `noindex, nofollow`.

## Datos de contacto

Todos los datos (dirección, teléfonos, correo, horarios, Instagram) se editan en
un único archivo: `Models/SitioInfo.cs`.
