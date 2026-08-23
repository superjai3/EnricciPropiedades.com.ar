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
escrituración (por eso nace apagada), los tres testimonios de la portada (son
inventados) y las cifras institucionales de "Quiénes somos".
