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

# --- Encontrar el bash de Git -------------------------------------------------
# Ojo con el bash.exe de System32: ese abre WSL, que es otra máquina virtual
# donde no están ni el proyecto ni la llave. Se descarta a propósito.
function Buscar-Bash {
    $candidatos = @(
        "$env:ProgramFiles\Git\bin\bash.exe",
        "${env:ProgramFiles(x86)}\Git\bin\bash.exe",
        "$env:LOCALAPPDATA\Programs\Git\bin\bash.exe"
    )

    # Si Git está en el PATH, su bash está al lado: .../cmd/git.exe -> .../bin/bash.exe
    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($git) {
        $raizGit = Split-Path (Split-Path $git.Source -Parent) -Parent
        $candidatos += (Join-Path $raizGit "bin\bash.exe")
    }

    foreach ($ruta in $candidatos) {
        if ($ruta -and (Test-Path $ruta)) {
            return (Resolve-Path $ruta).Path
        }
    }

    return $null
}

# Una ruta puesta a mano gana siempre: si alguien la configuró es porque la
# búsqueda automática no le sirvió.
$bash = if ($env:BASH_ENRICCI -and (Test-Path $env:BASH_ENRICCI)) {
    $env:BASH_ENRICCI
} else {
    Buscar-Bash
}

if (-not $bash) {
    Write-Host ""
    Write-Host "No encontré el bash de Git." -ForegroundColor Red
    Write-Host "Se instala con Git para Windows: https://git-scm.com/download/win"
    Write-Host "Si Git ya está instalado en otra carpeta, pasame la ruta del bash asi:"
    Write-Host '  $env:BASH_ENRICCI = "D:\Git\bin\bash.exe"'
    exit 1
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Host "No encontré dotnet. El sitio se compila en esta máquina antes de subirlo." -ForegroundColor Red
    Write-Host "Se instala el SDK de .NET 8 desde: https://dotnet.microsoft.com/download"
    exit 1
}

# --- Correr el despliegue -----------------------------------------------------
# publicar.sh usa rutas relativas al proyecto, así que hay que pararse ahí.
$raiz = Split-Path $PSScriptRoot -Parent
$anterior = Get-Location
$codigo = 1

try {
    Set-Location $raiz

    # Las variables de entorno son cómo publicar.sh recibe la configuración.
    # Se ponen sólo si se pidieron: si no, mandan las de adentro del script.
    if ($Servidor) { $env:SERVIDOR = $Servidor }
    # El bash de Git entiende C:/Users/... pero se confunde con las barras
    # invertidas de Windows, que para él son escapes. Se dan vuelta.
    if ($Llave)    { $env:LLAVE    = $Llave -replace '\\', '/' }

    $argumentos = @("despliegue/publicar.sh")
    if ($PrimeraVez) { $argumentos += "--primera-vez" }

    & $bash @argumentos
    $codigo = $LASTEXITCODE
}
finally {
    Set-Location $anterior
}

if ($codigo -ne 0) {
    Write-Host ""
    Write-Host "El despliegue falló (código $codigo). Arriba está el motivo." -ForegroundColor Red
    exit $codigo
}

Write-Host ""
Write-Host "Publicado." -ForegroundColor Green
