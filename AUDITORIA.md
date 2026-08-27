# Auditoría web · Enricci Propiedades

2026-08-27 · ASP.NET Core 8 Razor Pages + SQLite · modelo de negocio: **servicios**

## Resumen

**41 de 50 puntos aplicables cumplidos.** 0 arreglados en esta pasada (fue una
pasada de sólo lectura: se pidió el listado de mejoras, no la aplicación).
**4 puntos críticos siguen pendientes**, y tres de ellos dependen de que el sitio
salga a producción.

El sitio está muy por encima de la media técnica: canonical, Open Graph completo,
JSON-LD, sitemap y robots generados desde el dominio configurado, fuentes con
`preload` + `font-display: swap`, caché con `immutable`, HSTS, `prefers-color-scheme`
y `prefers-reduced-motion`, skip link y `:focus-visible`. El build de Release sale
con 0 advertencias y 0 errores.

Lo que falta no es código: es **medición, textos legales y prueba social**.

## Arreglado automáticamente

Nada. Esta pasada no aplicó cambios. Los puntos automatizables que quedan abiertos
están listados abajo con la marca 🔧 y se pueden aplicar en esta misma rama.

## Pendiente: crítico

### M1 · Analítica instalada
No hay ningún script de analítica en el proyecto. Hoy el sitio no mide nada: ni
visitas, ni de dónde llegan, ni qué páginas se leen.
**Qué hace falta:** que Horacio cree una propiedad de Google Analytics 4 y pase el
ID `G-XXXXXXX`. El script se deja puesto con un marcador. 🔧 (la parte de código)

### M3 · Eventos de conversión definidos
Sin analítica no hay eventos. Los tres que importan en este negocio: envío del
formulario de contacto, clic en WhatsApp y clic en el teléfono.
**Qué hace falta:** el ID de M1. Los disparadores se pueden dejar escritos desde ya. 🔧

### M2 · Search Console verificado
Sin verificar. Es lo que avisa si Google deja de indexar el sitio.
**Qué hace falta:** acceso a la cuenta de Google de Horacio, y el dominio ya resuelto.

### C7 · Políticas claras y accesibles
**No existe página de privacidad.** El formulario de contacto guarda nombre,
email, teléfono y mensaje en la base: eso es tratamiento de datos personales y
en Argentina cae bajo la Ley 25.326. Sin política publicada, el formulario está
recogiendo datos sin base declarada.
**Qué hace falta:** se puede dejar un borrador marcado
`<!-- BORRADOR: revisar con asesoría legal antes de publicar -->` con el texto
técnico (qué se guarda, para qué, cuánto tiempo, cómo se pide la baja). El texto
final lo valida un asesor. 🔧 (borrador)

## Pendiente: importante

### C5 · Testimonios con nombre, cara y empresa
No hay ninguno. En una inmobiliaria de barrio con tres generaciones, es el activo
más desaprovechado del sitio.
**Qué hace falta:** que Horacio consiga 3–4 clientes dispuestos, con nombre
completo, la operación concreta y **permiso escrito**. No se inventan.

### V9 · Prueba social junto al punto de decisión
Depende de C5. Sin testimonios reales no hay nada que colocar junto al formulario.

### V7 · FAQs que responden objeciones reales
No hay página de preguntas frecuentes. Las de este negocio salen solas del
teléfono: cuánto cobran de comisión, cuánto tarda una tasación, qué papeles hacen
falta para alquilar, quién paga el sellado.
**Qué hace falta:** las respuestas las da Horacio. La página se arma en una tarde. 🔧 (estructura)

### S7 · Blog o centro de recursos
No existe. En servicios locales es lo que hace que el sitio aparezca en búsquedas
que no son la marca ("cómo tasar un departamento en Monserrat").
**Qué hace falta:** decisión de si se sostiene. Un blog abandonado resta.

### C9 · Redes sociales enlazadas y activas
Instagram está cargado y enlazado. `SitioInfo.Facebook` está **vacío**: o se
completa, o se deja como está (el código ya filtra los vacíos, así que no rompe nada).

### C3 · Razón social y CUIT
La matrícula CUCICBA 2377 está visible y es verificable — bien. Falta el CUIT.
**Qué hace falta:** el número, de Horacio.

### C1 · "Sobre nosotros" con caras y nombres
La página existe y el servicio `RetratoTitular` ya contempla la foto del titular.
Verificar que la foto real esté cargada en producción.

### R2 · Imágenes en WebP 🔧
44 de 52 imágenes ya están en WebP. Quedan tres fotos de propiedad en JPG:
- `wwwroot/imagenes/Solis 700/Frente.jpg` — 256 KB (por encima del umbral de 200 KB)
- `wwwroot/imagenes/Av Entre Rios 500/Frente.jpg` — 180 KB
- `wwwroot/imagenes/Cochabamba 1700/Frente.jpg` — 132 KB

### R3 · Lazy loading 🔧
7 `<img>` en las vistas, 4 con `loading`. Revisar las tres restantes: si están bajo
el pliegue les falta `loading="lazy"`; si una es la imagen del LCP, tiene que
seguir **sin** él.

### R1 · Core Web Vitals medidos
No medibles todavía: el sitio no está publicado. En cuanto haya URL, pasar
Lighthouse y anotar LCP / INP / CLS.

### T10 · Backups verificados
La app hace respaldo diario propio (`Respaldo.Habilitado: true`, 14 copias). Lo que
falta es **restaurar uno y comprobar que funciona**. Un backup sin restauración
probada no cuenta.

### T1 · HTTPS forzado
El código está: `UseHttpsRedirection()` y `UseHsts()`. Falta el certificado en el
servidor, que llega con el despliegue en Oracle Cloud.

## Pendiente: opcional

- **V8 · Newsletter** — necesita proveedor, incentivo y base legal. No es prioridad aquí.
- **V10 · Vídeo de presentación** — producción de Horacio.
- **V18 · Casos de estudio** — las cifras las da el cliente. Nunca se estiman.
- **V5 · Precios transparentes** — decisión comercial: publicar o no el esquema de honorarios.
- **S10 · Contenido único** — revisar que las descripciones de las 9 propiedades
  importadas de Argenprop no repitan texto del portal de origen.

## Necesita datos del cliente

Lista concreta para pedirle a Horacio:

1. **ID de Google Analytics 4** (`G-XXXXXXX`).
2. **Acceso a Google Search Console** y alta de la propiedad.
3. **CUIT** de la inmobiliaria.
4. **3–4 testimonios** con nombre completo, operación y permiso escrito.
5. **Respuestas a las FAQs**: comisión, plazos de tasación, papeles, sellado.
6. **Política de privacidad revisada** por asesoría legal.
7. **URL de Facebook** o confirmación de que no hay perfil.
8. **Foto real del titular** para "Quiénes somos", si no está cargada.
9. **Alta en Google Business Profile** — lo que más mueve la aguja en búsqueda local.
10. Confirmación del **dominio** `enricci-propiedades.com.ar` y la **IP de la instancia**
    Oracle Cloud, para cerrar `Sitio:Dominio` y acotar `AllowedHosts` (hoy en `*`).
11. **Credenciales SMTP** — `Correo:Habilitado` está en `false`; las consultas se
    guardan en la base igual, pero no llega el aviso por mail.

## Fuera del checklist, pero conviene

- **`db/` en el repositorio** — contiene `Enricci.sql` y `SQLQuery3.sql`, SQL de otro
  proyecto. Basura de arrastre: se borra.
- **`wwwroot/lib`** — 712 KB de jQuery y jquery-validation. Dos páginas del panel
  usan la validación, así que jQuery se queda; revisar si sobra algo.
- **`AllowedHosts: "*"`** — hay que acotarlo al dominio real antes de publicar.
- **Email de contacto en `@gmail.com`** — con el dominio resuelto conviene pasar a
  `contacto@enricci-propiedades.com.ar`. Pesa en confianza.

## No aplica

Puntos descartados por el modelo de negocio (servicios):

- **T7** redirecciones 301 — sitio nuevo, no hay URLs antiguas que redirigir.
- **T8** hreflang — un solo idioma.
- **R4** CDN — no aplica al modelo.
- **S8** breadcrumbs, **S9** buscador interno — de e-commerce y SaaS. (Aun así la
  ficha ya emite `BreadcrumbList` y el listado ya tiene filtros por barrio, tipo,
  operación, ambientes, precio y orden.)
- **C6** reseñas en ficha, **C11** página de estado, **C12** garantía — e-commerce y SaaS.
- **V6, V11, V12, V13, V14, V15, V16, V17, V19, V20** — e-commerce, SaaS y portfolio.
- **U3** compartir, **U5** filtros de catálogo, **U6** fotos con zoom — otros modelos.
- **M5** embudo, **M6** A/B, **M7** alertas de caída — e-commerce y SaaS.
- **C8** banner de cookies — el sitio sólo usa cookies técnicas (sesión del panel y
  antiforgery), que no requieren consentimiento. **Ojo:** en cuanto entre la
  analítica de M1, el banner pasa a ser obligatorio y no puede cargar nada antes
  de que el visitante decida.
