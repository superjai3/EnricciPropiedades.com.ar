# Publica el sitio en el servidor, desde PowerShell.
#
#   .\despliegue\publicar.ps1
#   .\despliegue\publicar.ps1 -PrimeraVez     # sube además la base y las fotos actuales
#   .\despliegue\publicar.ps1 -Servidor ubuntu@1.2.3.4
#
# Se puede correr desde cualquier carpeta: el script se ubica solo.
#
# Por qué es una envoltura y no una traducción: el despliegue de verdad está en
# publicar.sh y es el mismo que corre en cualquier máquina. Tener dos programas
# haciendo lo mismo termina siempre igual —uno de los dos queda viejo— y el que
# queda viejo es el que se usa el día que hay un apuro.

[CmdletBinding()]
param(
    # Dónde se publica. Vacío usa el que trae publicar.sh.
    [string] $Servidor = "",

    # Llave SSH. Vacío usa ~/.ssh/enricci.key, que es donde la dejamos.
    [string] $Llave = "",

    # Sólo la primera vez sobre un servidor recién instalado: sube también la
    # base de datos y las fotos que haya en esta máquina.
    [switch] $PrimeraVez
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\comun.ps1"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Host "No encontré dotnet. El sitio se compila en esta máquina antes de subirlo." -ForegroundColor Red
    Write-Host "Se instala el SDK de .NET 8 desde: https://dotnet.microsoft.com/download"
    exit 1
}

# Las variables de entorno son cómo publicar.sh recibe la configuración.
# Se ponen sólo si se pidieron: si no, mandan las de adentro del script.
if ($Servidor) { $env:SERVIDOR = $Servidor }
if ($Llave)    { $env:LLAVE    = Ruta-Para-Bash $Llave }

$argumentos = @()
if ($PrimeraVez) { $argumentos += "--primera-vez" }

$codigo = Invocar-Bash "despliegue/publicar.sh" $argumentos

if ($codigo -ne 0) {
    Write-Host ""
    Write-Host "El despliegue falló (código $codigo). Arriba está el motivo." -ForegroundColor Red
    exit $codigo
}

Write-Host ""
Write-Host "Publicado." -ForegroundColor Green
