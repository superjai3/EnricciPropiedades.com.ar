# Auditoría web · Enricci Propiedades

2026-08-27 · ASP.NET Core 8 Razor Pages + SQLite · modelo de negocio: **servicios**
Rama `mejoras/auditoria-web-2026-08-27`

## Resumen

De los **50 puntos aplicables** al modelo «servicios»:

| Estado | Puntos |
|---|---|
| **`ok`** — cumplido y verificado | **32** |
| `parcial` — está pero a medias | 6 |
| `cliente` — depende de datos de Horacio | 8 |
| `falta` — no está y se puede hacer | 4 |

**4 arreglados en esta pasada** (C7, C8, M1 y M3, más limpieza de R2). Quedan
**2 críticos pendientes**, los dos por falta de datos que sólo tiene Horacio.

> **Corrección.** Una versión anterior de este informe decía «45 de 50 cumplidos».
> Ese número no salía del detalle punto por punto, salía de sumar mal: contaba como
> cumplidos los `parcial` y los `cliente`. El número real de `ok` es **32**. Los
> puntos individuales de más abajo nunca cambiaron; lo que estaba mal era el total.

El sitio ya estaba muy por encima de la media técnica: canonical, Open Graph
completo, JSON-LD, sitemap y robots generados desde el dominio configurado, fuentes
con `preload` y `font-display: swap`, caché con `immutable`, HSTS, `prefers-color-scheme`
y `prefers-reduced-motion`, skip link y `:focus-visible`. El build de Release sale con
0 advertencias y 0 errores, antes y después de esta pasada.

Lo que faltaba no era código: era **medición, textos legales y prueba social**. Los
dos primeros quedaron resueltos en esta pasada. El tercero depende de Horacio.

### Corrección sobre el estado del sitio

La primera versión de este informe decía que el sitio no estaba publicado. **Está
publicado y funcionando**, verificado el 2026-08-27:

- Sirve desde `168.138.128.137` detrás de **nginx**, con la aplicación por detrás.
- Dominio provisorio **`enricci-propiedades.duckdns.org`**, con certificado de
  Let's Encrypt válido hasta el 2026-11-20.
- `http://` redirige a `https://` con **301** en el dominio. Compresión **brotli**
  activa. HSTS con `max-age=2592000` (30 días). La CSP llega intacta.
- Primer byte en **0,68 s** y home completa en **1,33 s** con 123 KB. Es un buen
  número; no reemplaza a Lighthouse, pero descarta que haya un problema grueso.

## Arreglado en esta pasada

### `1d843c6` · Rendimiento: tres JPG sobrantes y el LCP de la ficha
Las tres fotos de propiedad ya tenían su versión WebP y las vistas la usaban desde
hacía rato: los `.jpg` habían quedado colgados y sólo los referenciaba el sembrador
de catálogo de ejemplo, que ahora apunta al WebP. Son 568 KB que viajaban en cada
despliegue sin que nadie los pidiera.

De paso, la foto grande de la ficha lleva `fetchpriority="high"`: es el LCP de esa
página y el navegador no tenía cómo saberlo.

Se borró también `db/`, con `Enricci.sql` y `SQLQuery3.sql`, SQL de otro proyecto
arrastrado de la plantilla inicial.

### `5cb46c5` · Confianza: política de privacidad, en borrador marcado
Era el punto más urgente de toda la auditoría: el formulario de contacto guarda
nombre, mail, teléfono y mensaje en la base, y eso es tratamiento de datos personales
bajo la Ley 25.326 sin nada publicado que lo declare.

El texto describe con exactitud lo que la aplicación hace hoy —los campos que guarda,
las cookies que deja, el respaldo diario de 14 copias, que las consultas sólo las ve
el panel—, así que como descripción técnica es correcto.

**Lo jurídico no está validado.** Va marcado como borrador en un comentario al tope
del archivo, con la lista de lo que falta (CUIT, inscripción ante la AAIP, casilla
para reclamos) y el aviso de que el plazo de conservación de dos años **hoy se cumple
a mano**, porque no hay purga automática.

La sección de cookies se escribe sola según haya analítica configurada o no, para que
el texto no pueda quedar mintiendo cuando se encienda la medición. Enlazada desde el
pie y sumada al sitemap.

### `9ae2bdc` · Medición: analítica y aviso de cookies, apagados hasta tener el ID
Resuelto como un **interruptor único**: la sección `Analitica` de `appsettings.json`
nace vacía, y mientras lo esté no se carga ningún script de terceros, no aparece
ningún aviso y la CSP queda igual de cerrada que antes. Al pegar el ID de Google
Analytics se encienden **a la vez** la medición y el aviso que la habilita — que es lo
que exige la ley: nunca uno sin el otro.

`gtag.js` **no se descarga hasta que alguien acepta**. El modo consentimiento de
Google permitiría cargarlo y que no midiera, pero eso ya es un pedido a un servidor de
Google con la IP del visitante antes de que decida nada. Rechazar y aceptar son el
mismo botón, del mismo tamaño y a la misma distancia. La decisión se guarda en
`localStorage`, no en una cookie: guardar la negativa en una cookie sería dejar justo
lo que el visitante rechazó.

Los tres eventos de conversión quedan puestos en `enricci.js` —envío del formulario,
clic en WhatsApp y clic en el teléfono, con la ubicación desde donde se tocó— y no
hacen nada mientras no exista `gtag`.

**Verificado con el sitio levantado en los dos estados.** Apagado: la CSP y el HTML
salen idénticos a antes, y el único `<script src>` de la home sigue siendo
`enricci.js`. Encendido con un ID de prueba: aparece el aviso, la CSP se abre
exactamente a `googletagmanager.com` y `google-analytics.com` y a nada más, y sigue
sin haber ninguna etiqueta de script de terceros en el HTML inicial.

## Pendiente: crítico

### M2 · Search Console verificado
Sin verificar. Es lo que avisa si Google deja de indexar el sitio. Ahora que el sitio
responde en `enricci-propiedades.duckdns.org` **ya se puede dar de alta**, sin esperar
al dominio definitivo.
**Qué hace falta:** acceso a la cuenta de Google de Horacio.

### M1 · Analítica: falta el identificador
El código está listo y probado. Falta que Horacio cree una propiedad de Google
Analytics 4 y pase el ID `G-XXXXXXXXXX`. Se pega en `Analitica:Id` de appsettings y
con eso se enciende todo, incluido el aviso de cookies y los eventos de M3.

## Pendiente: importante

### C5 · Testimonios con nombre, cara y empresa
No hay ninguno. En una inmobiliaria de barrio con tres generaciones, es el activo más
desaprovechado del sitio.
**Qué hace falta:** que Horacio consiga 3–4 clientes dispuestos, con nombre completo,
la operación concreta y **permiso escrito**. No se inventan.

### V9 · Prueba social junto al punto de decisión
Depende de C5. Sin testimonios reales no hay nada que colocar junto al formulario.

### V7 · FAQs que responden objeciones reales
No hay página de preguntas frecuentes. Las de este negocio salen solas del teléfono:
cuánto cobran de comisión, cuánto tarda una tasación, qué papeles hacen falta para
alquilar, quién paga el sellado.

**No se armó a propósito.** El checklist marca V7 como no automatizable, y con razón:
las respuestas son información del negocio. Una FAQ con respuestas inventadas sobre
comisiones es peor que no tener FAQ. En cuanto Horacio conteste las preguntas, la
página se arma en una tarde.

### T1 · HTTPS: la IP responde por HTTP sin redirigir
En el dominio, `http://` devuelve **301** a `https://` — correcto. Pero
`http://168.138.128.137/` devuelve **200** y sirve el sitio en claro.
**Qué hace falta:** un bloque en nginx que redirija también cuando el `Host` es la IP,
o que directamente rechace los pedidos que no traigan el dominio.

### Canónicas apuntando al dominio provisorio
En producción, `<link rel="canonical">` dice `enricci-propiedades.duckdns.org`, que es
lo correcto hoy. **Pero el día que resuelva `enriccipropiedades.com` hay que
cargarlo en `Sitio:Dominio`** o Google va a seguir indexando el dominio viejo. Es una
línea de configuración; lo importante es no olvidarla.

### C9 · Redes sociales
Instagram está cargado y enlazado. `SitioInfo.Facebook` está **vacío**: o se completa,
o se deja así (el código ya filtra los vacíos, no rompe nada).

### C3 · Razón social y CUIT
La matrícula CUCICBA 2377 está visible y es verificable — bien. Falta el CUIT.

### C1 · "Sobre nosotros" con caras y nombres
La página existe y `RetratoTitular` ya contempla la foto. Verificar que la foto real
esté cargada en el servidor.

### S7 · Blog o centro de recursos
No existe. En servicios locales es lo que hace que el sitio aparezca en búsquedas que
no son la marca ("cómo tasar un departamento en Monserrat"). Decisión de si se
sostiene: un blog abandonado resta.

### T10 · Backups verificados
La app hace respaldo diario propio (`Respaldo.Habilitado: true`, 14 copias). Falta
**restaurar uno y comprobar que funciona**. Un backup sin restauración probada no
cuenta.

### R1 · Core Web Vitals
Los tiempos de red están bien (0,68 s al primer byte, 1,33 s en total). Falta pasar
Lighthouse sobre la URL real para tener LCP, INP y CLS. Los datos de campo necesitan
tráfico, que necesita M1 y M2.

## Pendiente: opcional

- **V8 · Newsletter** — necesita proveedor, incentivo y base legal. No es prioridad acá.
- **V10 · Vídeo de presentación** — producción de Horacio.
- **V18 · Casos de estudio** — las cifras las da el cliente. Nunca se estiman.
- **V5 · Precios transparentes** — decisión comercial: publicar o no los honorarios.
- **S10 · Contenido único** — revisar que las descripciones de las 9 propiedades
  importadas de Argenprop no repitan el texto del portal de origen.

## Necesita datos del cliente

Lista concreta para pedirle a Horacio, ordenada por lo que más mueve la aguja:

1. **Alta en Google Business Profile** — es lo que más pesa en búsqueda local, y no se
   puede hacer desde el código.
2. **ID de Google Analytics 4** (`G-XXXXXXXXXX`). Con eso se enciende toda la medición.
3. **Acceso a Google Search Console** y alta de la propiedad.
4. **3–4 testimonios** con nombre completo, operación y permiso escrito.
5. **Respuestas a las FAQs**: comisión, plazos de tasación, papeles, sellado.
6. **Política de privacidad revisada** por asesoría legal.
7. **CUIT** de la inmobiliaria.
8. **URL de Facebook**, o confirmación de que no hay perfil.
9. **Foto real del titular** para "Quiénes somos", si no está cargada en el servidor.
10. **Credenciales SMTP** — `Correo:Habilitado` sigue en `false`. Las consultas se
    guardan en la base igual, así que no se pierde ningún contacto, pero no llega el
    aviso por mail.
11. **El dominio `enriccipropiedades.com`** cuando resuelva, para cargarlo en
    `Sitio:Dominio` y recién ahí acotar `AllowedHosts`, hoy en `*`.

## Fuera del checklist, pero conviene

- **`AllowedHosts: "*"`** — acotarlo al dominio real. Hoy la aplicación responde a
  cualquier `Host`, que es lo que permite que la IP sirva el sitio en claro.
- **`wwwroot/lib`** — 712 KB de jQuery y jquery-validation. Dos páginas del panel usan
  la validación, así que jQuery se queda; revisar si sobra el resto.
- **Email de contacto en `@gmail.com`** — con el dominio resuelto conviene pasar a
  `contacto@enriccipropiedades.com`. Pesa en confianza.
- **Las 9 publicaciones importadas de Argenprop** tienen superficies, baños y
  antigüedades en 0 (= "no figura"); falta completarlas desde el panel. Y queda una con
  el barrio cargado como «Río de Janeiro», que en CABA es una avenida y no un barrio —
  el panel ya la marca con un aviso.

## No aplica

Puntos descartados por el modelo de negocio (servicios):

- **T7** redirecciones 301 — sitio nuevo, no hay URLs antiguas que redirigir.
- **T8** hreflang — un solo idioma.
- **R4** CDN — no aplica al modelo.
- **S8** breadcrumbs, **S9** buscador interno — de e-commerce y SaaS. (Aun así la ficha
  ya emite `BreadcrumbList`, y el listado ya filtra por barrio, tipo, operación,
  ambientes, precio y orden.)
- **C6** reseñas en ficha, **C11** página de estado, **C12** garantía — e-commerce y SaaS.
- **V6, V11–V17, V19, V20** — e-commerce, SaaS y portfolio.
- **U3** compartir, **U5** filtros de catálogo, **U6** fotos con zoom — otros modelos.
- **M5** embudo, **M6** A/B, **M7** alertas de caída — e-commerce y SaaS.
