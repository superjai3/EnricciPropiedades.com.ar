#!/usr/bin/env bash
#
# Publica el sitio en el servidor. Se corre en la máquina de desarrollo, desde
# la carpeta del proyecto (en Windows, con Git Bash):
#
#   bash despliegue/publicar.sh
#   bash despliegue/publicar.sh --primera-vez    # sube además la base y las fotos actuales
#
# Qué hace: compila en Release, arma un paquete, lo sube, reemplaza la
# aplicación y reinicia el servicio. La base de datos y las fotos NO se tocan:
# viven en /var/lib/enricci, fuera de la carpeta de la aplicación, justamente
# para que un despliegue no pueda pisarlas.

set -euo pipefail

# --- Configurar esto una vez --------------------------------------------------
# La IP es la efímera que tiene la instancia hoy. Si Oracle le asigna otra
# —pasa si la instancia se apaga y se vuelve a encender—, se cambia acá.
SERVIDOR="${SERVIDOR:-ubuntu@168.138.128.137}"
LLAVE="${LLAVE:-$HOME/.ssh/enricci.key}"
# ------------------------------------------------------------------------------

CARPETA_APP="/var/www/enricci"
CARPETA_DATOS="/var/lib/enricci"
PROYECTO="Enricci Propiedades.csproj"
SALIDA="$(mktemp -d)"
PAQUETE="$(mktemp -u).tar.gz"

primera_vez=false
if [[ "${1:-}" == "--primera-vez" ]]; then
    primera_vez=true
fi

ssh_() { ssh -i "$LLAVE" -o StrictHostKeyChecking=accept-new "$SERVIDOR" "$@"; }

limpiar() { rm -rf "$SALIDA" "$PAQUETE"; }
trap limpiar EXIT

echo "==> Compilando en Release"
dotnet publish "$PROYECTO" -c Release -o "$SALIDA" --nologo -v quiet

# Las fotos del catálogo viven en el servidor. Si viajaran en el paquete,
# cada despliegue reemplazaría las que Horacio subió desde el panel por las
# que quedaron en esta máquina.
rm -rf "$SALIDA/wwwroot/imagenes/propiedades"

# appsettings.Development.json no tiene nada que hacer en el servidor.
rm -f "$SALIDA/appsettings.Development.json"

echo "==> Armando el paquete ($(du -sh "$SALIDA" | cut -f1))"
tar -czf "$PAQUETE" -C "$SALIDA" .

echo "==> Subiendo"
scp -i "$LLAVE" -q "$PAQUETE" "$SERVIDOR:/tmp/enricci-despliegue.tar.gz"

if $primera_vez; then
    echo "==> Primera vez: subiendo la base y las fotos actuales"

    if [[ -f enricci.db ]]; then
        scp -i "$LLAVE" -q enricci.db "$SERVIDOR:/tmp/enricci.db"
    fi

    if [[ -d wwwroot/imagenes/propiedades ]]; then
        tar -czf "$PAQUETE.fotos" -C wwwroot/imagenes/propiedades .
        scp -i "$LLAVE" -q "$PAQUETE.fotos" "$SERVIDOR:/tmp/enricci-fotos.tar.gz"
        rm -f "$PAQUETE.fotos"
    fi
fi

echo "==> Desplegando en el servidor"
# Lo que pasa en el servidor está en instalar-en-servidor.sh, compartido con el
# despliegue automático de GitHub Actions: es el mismo código en los dos casos.
ssh_ "primera_vez=$primera_vez bash -s" < "$(dirname "$0")/instalar-en-servidor.sh"

echo
echo "Listo."
