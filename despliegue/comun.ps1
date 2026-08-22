# Lo que comparten los scripts de PowerShell del despliegue.
#
# Se usa con punto adelante, que es como PowerShell incorpora un archivo al
# script que lo llama:
#
#   . "$PSScriptRoot\comun.ps1"
#   Invocar-Bash "despliegue/publicar.sh"

# Encuentra el bash que viene con Git para Windows.
#
# Ojo con el bash.exe de System32: ese abre WSL, que es otra máquina virtual
# donde no están ni el proyecto ni la llave. Se descarta a propósito.
function Resolver-Bash {
    # Una ruta puesta a mano gana siempre: si alguien la configuró es porque la
    # búsqueda automática no le sirvió.
    if ($env:BASH_ENRICCI -and (Test-Path $env:BASH_ENRICCI)) {
        return $env:BASH_ENRICCI
    }

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

# Corre un script de bash del proyecto, parado en la carpeta del proyecto, y
# devuelve su código de salida. Los scripts usan rutas relativas a la raíz.
function Invocar-Bash {
    param(
        [Parameter(Mandatory)] [string]   $Script,
        [Parameter()]          [string[]] $Argumentos = @()
    )

    $bash = Resolver-Bash

    if (-not $bash) {
        Write-Host ""
        Write-Host "No encontré el bash de Git." -ForegroundColor Red
        Write-Host "Se instala con Git para Windows: https://git-scm.com/download/win"
        Write-Host "Si Git ya está instalado en otra carpeta, pasame la ruta del bash asi:"
        Write-Host '  $env:BASH_ENRICCI = "D:\Git\bin\bash.exe"'
        return 1
    }

    $raiz = Split-Path $PSScriptRoot -Parent
    $anterior = Get-Location
    $codigo = 1

    try {
        Set-Location $raiz

        # Out-Host y no a secas: en PowerShell todo lo que una función escribe
        # sin destino se suma a lo que devuelve. Sin esto, el código de salida
        # volvería mezclado con las cien líneas que imprime el despliegue.
        & $bash @(@($Script) + $Argumentos) | Out-Host
        $codigo = $LASTEXITCODE
    }
    finally {
        Set-Location $anterior
    }

    return $codigo
}

# El bash de Git entiende C:/Users/... pero se confunde con las barras
# invertidas de Windows, que para él son escapes. Se dan vuelta.
function Ruta-Para-Bash {
    param([string] $Ruta)
    return $Ruta -replace '\\', '/'
}
