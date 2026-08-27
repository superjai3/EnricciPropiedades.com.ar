# Pone el sitio a responder en un dominio, con HTTPS, desde PowerShell.
#
#   .\despliegue\dominio.ps1 enricci.duckdns.org
#   .\despliegue\dominio.ps1 enriccipropiedades.com -Mail micorreo@ejemplo.com
#
# Antes de correrlo, el dominio tiene que estar apuntando a la IP del servidor.
# En DuckDNS eso es escribir el nombre y la IP en el panel; tarda un minuto.

[CmdletBinding()]
param(
    # El dominio que va a atender el sitio.
    [Parameter(Mandatory, Position = 0)]
    [string] $Dominio,

    # Correo al que Let's Encrypt avisa si un certificado está por vencer sin
    # haberse renovado. Vacío usa el que trae dominio.sh.
    [string] $Mail = "",

    [string] $Servidor = "",
    [string] $Llave = ""
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\comun.ps1"

if ($Servidor) { $env:SERVIDOR = $Servidor }
if ($Llave)    { $env:LLAVE    = Ruta-Para-Bash $Llave }

# El segundo argumento del script es el correo, así que si se pide hay que
# mandarlo en esa posición.
$argumentos = @($Dominio)
if ($Mail) { $argumentos += $Mail }

$codigo = Invocar-Bash "despliegue/dominio.sh" $argumentos

if ($codigo -ne 0) {
    Write-Host ""
    Write-Host "No se pudo configurar el dominio (código $codigo). Arriba está el motivo." -ForegroundColor Red
    exit $codigo
}

Write-Host ""
Write-Host "El sitio ya responde en https://$Dominio/" -ForegroundColor Green
