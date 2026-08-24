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

El sitio está publicado en un servidor propio (Oracle Cloud Free Tier, Ubuntu
24.04) detrás de nginx. El dominio definitivo va a ser
`enricci-propiedades.com.ar`; mientras tanto responde en un dominio provisorio
de DuckDNS. Ver *Publicar en el servidor*.

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
  Propiedad.cs          Publicación (operación, tipo, ubicación, precio, fotos…)
  Usuario.cs            Usuario del panel; guarda hash y sal, nunca la contraseña
  SitioInfo.cs          Datos de la inmobiliaria: contacto, coordenadas, horario
                        y barrios; de acá salen también los datos estructurados
  Slug.cs               "Constitución" → "constitucion", para las URL de barrio
  BarriosDeBuenosAires.cs  Lista cerrada de barrios de CABA y partidos del GBA
  BarrioValidoAttribute.cs Valida que el barrio sea uno de esa lista
  Consulta.cs           Consulta recibida por los formularios; incluye la hora local
  OpcionesSitio.cs      Dominio del sitio, para las URL absolutas
  OpcionesRespaldo.cs   Configuración del respaldo automático
  ArteFachada.cs        Portada SVG generada para publicaciones sin fotografía
  OpcionesCorreo.cs     Configuración del envío de correo
  Cotizacion.cs         Tipo de cambio del día, con su fecha y su fuente
  OpcionesCotizacion.cs De dónde sale el dólar y cada cuánto se consulta
  OpcionesEscritura.cs  Conceptos de la calculadora de gastos de escrituración
  Suscripcion.cs        Alguien anotado para que le avisen de propiedades nuevas
  OpcionesAlertas.cs    Configuración de los avisos de propiedades nuevas
Data/
  EnricciContexto.cs    DbContext: propiedades, usuarios y consultas
  SembradorInicial.cs   Migración, catálogo de ejemplo y alta del administrador
  Migraciones/          Historial de cambios del esquema
Services/
  PropiedadesService.cs Catálogo: consulta pública, búsqueda y ABM del panel
  UsuariosService.cs    Autenticación y cambio de contraseña
  ClaveHash.cs          PBKDF2-SHA256, 210.000 iteraciones
  ConsultasService.cs   Alta y seguimiento de las consultas del sitio
  DatosEstructurados.cs Bloques de schema.org para buscadores y asistentes
  FotosService.cs       Reduce a WEBP, genera miniatura y borra las fotos del panel
  RespaldoService.cs    Copia la base y las fotos en un .zip, con rotación
  RespaldoProgramado.cs Dispara el respaldo una vez por día
  RutaBaseDeDatos.cs    Resuelve dónde está el archivo .db
  CorreoService.cs      Envío por SMTP, a la oficina o a un visitante
  RetratoTitular.cs     Mira una sola vez si está la foto del titular
  CotizacionService.cs  Dólar del Banco Nación, refrescado en segundo plano
  LimiteEnvios.cs       Freno por IP de los formularios públicos
Pages/
  Index                 Portada: hero, buscador, destacadas, servicios, barrios
  Propiedades           Listado con filtros por operación, tipo, barrio, ambientes y precio
  Ficha                 Detalle de una publicación (ruta /propiedad/{id}),
                        con su propio formulario de consulta
  Escrituracion         Calculadora de gastos de escrituración
                        (ruta /gastos-de-escrituracion; nace apagada)
  Servicios             Panorama de servicios + preguntas frecuentes
  Tasacion              Formulario de pedido de tasación
  Cobranza              Administración y cobranza de alquileres
  Asesoria_Legal        Servicios legales asociados
  Quienes_Somos         Historia, línea de tiempo y valores
  Mision / Vision       Páginas institucionales
  Contacto              Formulario de consulta
  Error                 404 y errores generales
  Sitemap               Mapa del sitio en /sitemap.xml
  Robots                robots.txt generado, con los rastreadores de IA
  Llms                  /llms.txt: resumen del sitio para asistentes con IA
  Barrio                Página por barrio en /propiedades/{barrio}
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
  js/enricci.js         Tema claro/oscuro, menú móvil, acordeones, animaciones,
                        favoritos, compartir y la calculadora
  fonts/web/            Tipografías propias en WOFF2, recortadas a los caracteres
                        que el sitio usa: 1,2 MB de TTF quedaron en 197 KB.
                        En Be_Vietnam_Pro/ y Manuale/ sólo quedan las licencias,
                        que la OFL exige que viajen con la tipografía
  imagenes/             Logo y fotografías; las cargadas desde el panel van a
                        imagenes/propiedades/{id}/ y no se versionan
db/                     Scripts SQL sueltos, fuera de wwwroot para que no se sirvan
despliegue/
  publicar.sh / .ps1    Compila, sube y reemplaza la aplicación en el servidor
  dominio.sh / .ps1     Configura nginx y el certificado para un dominio
  comun.ps1             Lo que comparten los scripts de PowerShell
  preparar-servidor.sh  Instala y configura una máquina desde cero
  enricci.service       Unidad de systemd
  enricci.env.ejemplo   Plantilla de la configuración del servidor
  INSTALACION.md        El paso a paso completo
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

Hay tres formularios: **Contacto**, **Tasación** y el que está **dentro de cada
ficha**. Los tres validan del lado del servidor y tienen un campo trampa contra
robots.

El de la ficha es más corto a propósito —nombre, correo, teléfono y mensaje— y
viene con el mensaje ya escrito: acá ya se sabe por qué propiedad preguntan, y
cada campo de más es una consulta menos que llega. Al enviarlo la página
redirige en vez de pintar la respuesta ahí mismo, así el visitante queda frente
a la confirmación y refrescar no reenvía la consulta.

Sobre el campo trampa hay un **freno de envíos por dirección IP**
(`Services/LimiteEnvios.cs`): diez envíos cada diez minutos, compartidos por los
tres formularios. El número es holgado a propósito, porque en redes móviles
cientos de personas comparten una misma IP y frenar a alguien que quiere
consultar cuesta más caro que dejar pasar un spam. Va aparte del limitador de
ASP.NET porque una ficha tiene que poder abrirse mil veces —buscadores
incluidos— y aun así no aceptar mil consultas.

Lo que llega se **guarda en la base** y se ve en el panel, en *Consultas*: quién
escribió, cuándo, por qué propiedad, y si ya se le respondió. Cada una admite notas internas.

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

## El precio en pesos

Lo que se publica en dólares se muestra además con su equivalente en pesos, con
la fecha y la fuente a la vista, tanto en la ficha como en las tarjetas del
listado. Sección `Cotizacion` de `appsettings.json`:

```json
"Cotizacion": {
  "Habilitada": true,
  "Url": "https://dolarapi.com/v1/dolares/oficial",
  "MinutosEntreConsultas": 60,
  "Fuente": "Banco Nación",
  "Punta": "venta"
}
```

La de fábrica devuelve el dólar oficial, que es el que publica el Banco de la
Nación Argentina. `Punta` en `venta` es la que paga quien compra dólares, que es
la situación de quien está por comprar una propiedad.

El valor se refresca **en segundo plano** y las páginas lo leen ya resuelto: una
consulta a otro servidor en medio del armado de la página agregaría espera al
visitante y lo dejaría a merced de que la fuente responda.

Si la fuente se cae **se sigue mostrando el último valor bueno con su fecha**: un
valor de ayer sirve, ninguno no. Si nunca se pudo consultar, no se muestra nada
en pesos — un número inventado, o uno viejo sin fecha, es peor que no ponerlo.

## La calculadora de gastos de escrituración

Contesta la primera pregunta de toda consulta: cuánto hay que tener además del
precio. Muestra el detalle concepto por concepto, quién paga cada uno, los
subtotales de comprador y vendedor y el total en dólares y en pesos.

**Nace apagada, y es a propósito.** Los porcentajes reales —sellos, honorarios de
escribano, certificaciones— los fijan la Ciudad, el colegio de escribanos y cada
operación, cambian con el tiempo y admiten excepciones. Publicar un número
equivocado en el sitio de una inmobiliaria no es un error de cálculo: es una
promesa que después hay que sostener frente a un cliente.

Mientras `Habilitada` esté en `false`, la página redirige a *Servicios*, no
aparece ningún enlace hacia ella y no figura en el mapa del sitio.

```json
"Escritura": {
  "Habilitada": false,
  "Vigencia": "agosto de 2026",
  "Nota": "Consultanos el caso concreto antes de reservar.",
  "Conceptos": [
    {
      "Nombre": "Impuesto de sellos",
      "Porcentaje": 0,
      "Paga": "Ambos",
      "Detalle": "Tributo de la Ciudad sobre el valor de la operación."
    }
  ]
}
```

`Porcentaje` va sobre el precio; `Fijo` es para lo que no depende del valor de la
propiedad —informes, certificaciones—. `Paga` acepta `Comprador`, `Vendedor` o
`Ambos`, y *Ambos* se muestra repartido por mitades. En el servidor los mismos
valores se cargan por variables de entorno; está documentado en
`despliegue/enricci.env.ejemplo`.

El cálculo lo hace el servidor y el navegador sólo lo rehace mientras se
escribe: **sin JavaScript el botón envía el formulario** y la página vuelve con
la cuenta hecha.

## Quién atiende

El sitio pone a **Raúl Horacio Enricci** a la vista: en la portada, en *Quiénes
somos*, en el panel de cada ficha y al lado del formulario de contacto. La idea
es simple: saber que del otro lado hay una persona con nombre, cara y matrícula
verificable, y no una casilla genérica, es la diferencia entre consultar y
cerrar la pestaña. Esa sección de la portada reemplazó a tres testimonios
inventados.

La foto va en `wwwroot/imagenes/horacio.jpg` y **sí se versiona**, a diferencia
de las fotos del catálogo. Esas las carga Horacio desde el panel y viven en el
servidor; ésta es parte del sitio, como el logo, y versionarla hace que viaje
sola en cada despliegue en lugar de depender de que alguien se acuerde de
subirla por separado.

`Services/RetratoTitular.cs` mira **una sola vez, al arrancar**, si el archivo
está. Si no está, las páginas muestran las iniciales en lugar de un ícono roto y
los datos estructurados omiten el `image`. Preguntarle al disco en cada visita
sería pagar una consulta de sistema de archivos por página, y el archivo no
cambia durante la vida del proceso.

En los datos estructurados se declara como `founder` **y** como `employee`: lo
primero dice quién fundó la inmobiliaria, lo segundo quién atiende hoy, y es lo
segundo lo que le sirve a alguien que está por escribir.

## Buscar, guardar y compartir

- **Búsqueda por texto** en la portada y en el listado, además de los filtros.
  Compara sin acentos ni mayúsculas contra título, dirección, barrio, tipo,
  operación y descripción, y exige que estén *todas* las palabras.
- **Propiedades guardadas.** El corazón de cada tarjeta las anota en el propio
  navegador (`localStorage`, clave `enricci-favoritos`); no viajan a ningún lado
  ni hacen falta datos personales. En el listado hay un filtro para ver sólo esas.
- **Compartir un aviso.** En el teléfono abre el menú del sistema; en la
  computadora copia el enlace. Es distinto de consultarle a la inmobiliaria: es
  para mandarle la propiedad a otra persona.

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
  "Dominio": ""
}
```

De ahí salen las URL canónicas, las de Open Graph, el `sitemap.xml` y la línea
`Sitemap:` de `robots.txt`. Con el dominio puesto se usa **siempre** ese, aunque
el visitante haya entrado por la IP o por un túnel de pruebas: si no, los
buscadores verían la misma página publicada en dos direcciones distintas y
repartirían el posicionamiento entre las dos.

**Vacío** —que es como viene— las URL absolutas salen del host del pedido, que
es lo correcto en desarrollo y mientras el dominio no esté dado de alta. Poner
acá un dominio que todavía no existe es peor que dejarlo vacío: el sitio arma
igual todas sus direcciones con él, y un aviso compartido por WhatsApp termina
apuntando a la nada.

No hace falta escribirlo a mano en el servidor: lo deja puesto
`despliegue/dominio.sh` cuando configura el dominio y su certificado.

Cuando el dominio esté andando conviene además acotar `AllowedHosts`, que hoy
está en `*`:

```json
"AllowedHosts": "enricci-propiedades.com.ar;www.enricci-propiedades.com.ar"
```

## Publicar en el servidor

El paso a paso completo —desde una máquina vacía— está en
`despliegue/INSTALACION.md`. Para el día a día alcanza con:

```powershell
git pull origin claude/horacio-real-estate-website-bt6gfo
.\despliegue\publicar.ps1
```

```bash
bash despliegue/publicar.sh
```

Compila en Release, arma el paquete, lo sube, reemplaza la aplicación, reinicia
y **comprueba que el sitio responda**. Si no responde, deja el registro a la
vista y explica cómo volver a la versión anterior, que quedó guardada.

**La base de datos y las fotos no se tocan**: viven en `/var/lib/enricci`, fuera
de la carpeta de la aplicación, justamente para que un despliegue no pueda
pisarlas. La carpeta de fotos dentro de `wwwroot` es un enlace que el despliegue
vuelve a crear cada vez. La opción `--primera-vez` / `-PrimeraVez` sube además la
base y las fotos de la máquina de desarrollo, y por eso **se usa una sola vez**:
en un servidor en uso pisaría lo que se cargó desde el panel.

Para ponerle un dominio con HTTPS —uno provisorio de DuckDNS o el definitivo—:

```powershell
.\despliegue\dominio.ps1 el-dominio-que-sea
```

Deja nginx atendiendo en ese nombre, saca el certificado de Let's Encrypt, pasa
todo a HTTPS, le anota el dominio a la aplicación y comprueba que responda. Se
puede correr las veces que haga falta: si el certificado ya está, lo reutiliza.

Los `.ps1` no reimplementan nada: buscan el bash que viene con Git para Windows
y le pasan el trabajo. Tener dos programas haciendo lo mismo termina siempre
igual —uno de los dos queda viejo— y el que queda viejo es el que se usa el día
que hay un apuro.

Ojo con esto último: si el proxy reenvía otro nombre de host, el sitio responde
400 a todo. Conviene cambiarlo con el sitio ya publicado y andando, no antes.

## Buscadores y asistentes con IA

Todo lo que sigue sale de un solo lugar: `Models/SitioInfo.cs` para los datos de
la empresa y `Services/DatosEstructurados.cs` para armar los bloques. Los datos
estructurados repartidos por las vistas se desincronizan del contenido visible al
primer cambio de texto, y un buscador que encuentra que lo declarado no coincide
con lo que se ve deja de confiar en el resto.

### Qué se declara y dónde

| Página | Datos estructurados |
| --- | --- |
| Todas | `RealEstateAgent` + `WebSite`, enlazados en un `@graph` |
| Ficha | `RealEstateListing` + `BreadcrumbList` |
| Barrio | `CollectionPage` con `ItemList` + `BreadcrumbList` |
| Servicios | `FAQPage` |
| Tasación, Cobranza, Asesoría legal | `Service` |

La inmobiliaria lleva un `@id` fijo (`{dominio}/#inmobiliaria`) al que apuntan
las demás entidades. Sin eso, cada página declara *otra* empresa con el mismo
nombre, y el buscador no termina de armar una ficha única.

Del `RealEstateAgent` cuelgan las **coordenadas** de la oficina —tomadas de
OpenStreetMap—, el **horario** en piezas, la **matrícula CUCICBA** como
credencial, los **barrios donde opera** y los perfiles de redes. Es lo que
contesta «¿atienden en San Cristóbal?» o «¿a qué hora abren?» sin que nadie
tenga que leer la página.

### Páginas por barrio

`/propiedades/{barrio}` —por ejemplo `/propiedades/monserrat`— es una página
propia, no el listado filtrado. La diferencia importa: lo que la gente busca no
es «propiedades» sino «departamentos en venta en Monserrat», y una página que se
distingue de otra sólo por la cadena de consulta no compite, porque los
buscadores la tratan como la misma página filtrada.

Se generan solas a partir de los barrios que tienen publicaciones, entran en el
sitemap y se enlazan desde la portada, desde cada ficha y entre ellas. Un barrio
sin publicaciones devuelve 404: si no hay contenido, mejor decirlo.

El texto de cada una sale del catálogo —cuántas hay, de qué tipo, desde qué
precio—, así dice algo distinto en cada barrio y no queda desactualizado solo.

### Los barrios son una lista cerrada

El barrio de una publicación se elige de una lista —`Models/BarriosDeBuenosAires.cs`—
y no se escribe a mano. El campo era texto libre y así se coló un «Río de
Janeiro», que en la Ciudad es una avenida y no un barrio; con él entró al
catálogo una publicación que figuraba fuera del país.

La lista tiene tres grupos:

- **Ciudad de Buenos Aires** — los 48 barrios oficiales.
- **Zonas de la Ciudad** — nombres que no son barrios oficiales pero que el
  mercado usa todos los días, y que el propio catálogo ya usaba: Congreso,
  Barrio Norte, Once, Abasto, Microcentro, Las Cañitas, Palermo Soho… Si se
  prefiere obligar a usar sólo los 48 oficiales, se borra esa lista y listo.
- **Gran Buenos Aires** — los 24 partidos.

La validación es del lado del servidor (`BarrioValidoAttribute`), no sólo en el
desplegable: el formulario se puede enviar sin pasar por el navegador, y un
barrio inventado se traduce en una página por barrio que no le sirve a nadie.

De la lista sale también **la provincia**, que no se guarda: los barrios de la
Ciudad declaran «Ciudad Autónoma de Buenos Aires» y los partidos, «Provincia de
Buenos Aires». El país es siempre `AR`. Un dato que se puede derivar y además se
guarda termina, tarde o temprano, diciendo algo distinto del que lo origina.

Las publicaciones importadas que tengan un barrio fuera de la lista aparecen
avisadas arriba del listado del panel, con un enlace para corregirlas.

### GEO: los asistentes con IA

- **`/llms.txt`** — un resumen del sitio en Markdown, generado: quiénes son,
  desde cuándo, qué matrícula, qué barrios, qué horario, el índice de páginas,
  las páginas por barrio con su cantidad de publicaciones y las preguntas
  frecuentes completas. Un asistente que tiene que contestar «¿qué inmobiliarias
  hay en Monserrat?» no se lee el sitio entero; de acá saca los datos duros sin
  tener que interpretarlos.
- **`/robots.txt` nombra uno por uno** a GPTBot, OAI-SearchBot, ChatGPT-User,
  ClaudeBot, Claude-User, PerplexityBot, Perplexity-User, Google-Extended y
  Applebot-Extended. Con `User-agent: *` ya estarían permitidos, pero varios
  buscan su propio nombre y Google-Extended sólo se gobierna con una regla
  propia. Además deja la decisión por escrito: a una inmobiliaria le conviene
  que la citen.
- **Las preguntas frecuentes son el contenido más citable del sitio.** Viven
  como datos en `Servicios.cshtml.cs` y de ahí salen las tres cosas: el
  acordeón que se ve, el bloque `FAQPage` y la sección de `llms.txt`. No pueden
  decir cosas distintas.
- Todo el sitio se sirve renderizado desde el servidor, sin necesidad de
  ejecutar JavaScript para ver el contenido, que es lo que estos rastreadores
  necesitan.

### El resto

`/sitemap.xml` se arma con las páginas fijas, las de barrio y cada propiedad
publicada; las dadas de baja no figuran. `/robots.txt` también se genera —no es
un archivo suelto en `wwwroot`— para que la línea del sitemap salga siempre del
dominio configurado.

Al compartir una ficha o una página de barrio, la vista previa muestra una
**foto de la propiedad** y no el isologo. El panel lleva `noindex, nofollow` y
queda fuera por `Disallow`.

### Lo que no se puede hacer desde el código

- **Google Business Profile**: darlo de alta y verificarlo por correo postal es
  lo que más mueve la aguja en búsquedas locales, y hay que hacerlo a mano. Los
  datos tienen que coincidir *exactamente* con los del sitio (nombre, dirección,
  teléfono), que salen de `SitioInfo.cs`.
- **Google Search Console y Bing Webmaster Tools**: dar de alta el dominio y
  enviar el sitemap cuando esté publicado.
- El sitio **sí** declara un `SearchAction`, apuntado al buscador por texto de
  `/Propiedades`. Antes no lo hacía, y con razón: hasta que existió la búsqueda
  habría sido anunciar algo que no funcionaba, que es peor que no anunciar nada.

## Datos de contacto

Todos los datos (dirección, teléfonos, correo, horarios, Instagram) se editan en
un único archivo: `Models/SitioInfo.cs`.

## Lo que todavía no está confirmado

Estas cosas están escritas en el sitio pero **no vienen de la inmobiliaria**. No
deberían quedar publicadas así: o las confirma Horacio, o se sacan.

- **Las cifras institucionales**: "desde 1932", los hitos de 1958, 1984 y 2006 de
  `Quienes_Somos.cshtml` y el contador de 1.400 operaciones de la portada.
- **Los porcentajes de la calculadora de escrituración**. Por eso nace apagada.
- **Los datos que faltan en el catálogo**: superficies, baños y antigüedades que
  la ficha de origen no declaraba y quedaron en 0, y tres publicaciones con
  precio *Consultar*.

Y falta lo que sólo se puede hacer desde afuera del código: comprar el dominio,
generar la contraseña de aplicación de Gmail para que salgan los avisos de las
consultas, pasar la IP del servidor a reservada y dar de alta el sitio en Google
Search Console y en Google Business Profile.
