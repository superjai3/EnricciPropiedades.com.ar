# Notas para trabajar en este proyecto

## Cómo publica Jaime

Trabaja en **Windows, con PowerShell** (no con Git Bash). El proyecto está en
`C:\Users\jaime\source\repos\EnricciPropiedades.com.ar`.

**Al final de todo mensaje que incluya cambios publicables, hay que pasarle
estas tres líneas, sin que las pida y siempre en PowerShell:**

```powershell
cd C:\Users\jaime\source\repos\EnricciPropiedades.com.ar
git pull origin claude/horacio-real-estate-website-bt6gfo
.\despliegue\publicar.ps1
```

Nunca darle comandos de bash (`VAR=valor comando`, `bash script.sh`): PowerShell
no los entiende y ya se perdió tiempo con eso. `publicar.ps1` es la envoltura que
encuentra el bash de Git y le pasa el trabajo.

Nunca sugerir `-PrimeraVez`: esa opción sube la base y las fotos de la máquina
de desarrollo y pisaría lo que Horacio cargó desde el panel.

El despliegue al servidor lo corre él: desde el entorno de Claude no hay acceso
al puerto 22 de la instancia ni está la llave SSH.

## Idioma

Todo en castellano rioplatense: los mensajes, los comentarios del código, los
nombres de clases y variables, y los textos del sitio.

## El servidor

Oracle Cloud Free Tier, Ubuntu 24.04, `168.138.128.137` (IP efímera). La
aplicación vive en `/var/www/enricci` y los datos —base, fotos, respaldos— en
`/var/lib/enricci`, fuera del alcance de un despliegue. El dominio definitivo
va a ser `enricci-propiedades.com.ar`, todavía sin comprar.

## Datos que no están confirmados

No publicar números ni afirmaciones que no vengan de la inmobiliaria. Quedan
pendientes de confirmación: los porcentajes de la calculadora de gastos de
escrituración (por eso nace apagada), los años de trayectoria y la línea de
tiempo de "Quiénes somos" (los hitos están escritos pero no se muestran hasta
que Horacio los confirme; ver `SitioInfo.HitosConfirmados`).

## Testimonios y reseñas

La portada tenía tres testimonios inventados; se eliminaron. **Ningún
testimonio, reseña ni cifra de clientes se publica sin el consentimiento por
escrito** de quien lo firma (un correo o documento que guarda la inmobiliaria),
con el nombre tal como se va a mostrar. Si no hay consentimiento, no se publica,
aunque lo pida el cliente.

## UX/UI, SEO/GEO y despliegue continuo (reglas del cliente, 14/09/2026)

- Todo cambio visible usa los estilos existentes de `wwwroot`, con etiquetas asociadas, foco visible, contraste correcto, textos claros y sin desbordes en móvil.
- Ningún cambio empeora el rastreo ni la visibilidad en buscadores y motores de IA: títulos, descripciones, datos estructurados, sitemap y `llms.txt` se revisan en cada cambio.
- **Cada mejora terminada se sube a producción en el momento**: fusionar en `main`, `dotnet build` en el PC y ejecutar `despliegue/publicar.sh`. Este repositorio no tiene despliegue automático.
