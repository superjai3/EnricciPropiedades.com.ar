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
ssh_ "primera_vez=$primera_vez bash -s" <<'REMOTO'
set -euo pipefail

CARPETA_APP="/var/www/enricci"
CARPETA_DATOS="/var/lib/enricci"

echo "  - parando el servicio"
sudo systemctl stop enricci || true

echo "  - reemplazando la aplicación"
sudo rm -rf "$CARPETA_APP".anterior
if [[ -d "$CARPETA_APP" ]]; then
    sudo mv "$CARPETA_APP" "$CARPETA_APP".anterior
fi
sudo mkdir -p "$CARPETA_APP"
sudo tar -xzf /tmp/enricci-despliegue.tar.gz -C "$CARPETA_APP"
rm -f /tmp/enricci-despliegue.tar.gz

if [[ "$primera_vez" == "true" ]]; then
    if [[ -f /tmp/enricci.db && ! -f "$CARPETA_DATOS/enricci.db" ]]; then
        echo "  - instalando la base de datos"
        sudo mv /tmp/enricci.db "$CARPETA_DATOS/enricci.db"
    fi

    if [[ -f /tmp/enricci-fotos.tar.gz ]]; then
        echo "  - instalando las fotos"
        sudo tar -xzf /tmp/enricci-fotos.tar.gz -C "$CARPETA_DATOS/fotos"
        rm -f /tmp/enricci-fotos.tar.gz
    fi
fi

# Las fotos que se suben desde el panel se guardan bajo wwwroot, pero tienen que
# sobrevivir al despliegue: la carpeta es un enlace a la de datos.
echo "  - enlazando la carpeta de fotos"
sudo mkdir -p "$CARPETA_APP/wwwroot/imagenes"
sudo ln -sfn "$CARPETA_DATOS/fotos" "$CARPETA_APP/wwwroot/imagenes/propiedades"

sudo chown -R enricci:enricci "$CARPETA_APP" "$CARPETA_DATOS"

echo "  - arrancando el servicio"
sudo systemctl start enricci

# systemd devuelve el control apenas arranca; se le da un momento y se comprueba
# que siga en pie, porque un error de configuración lo tumba a los pocos segundos.
sleep 5

if ! systemctl is-active --quiet enricci; then
    echo "  ERROR: el servicio no quedó andando. Últimas líneas del registro:"
    sudo journalctl -u enricci -n 30 --no-pager
    echo "  Se puede volver atrás con: sudo rm -rf /var/www/enricci && sudo mv /var/www/enricci.anterior /var/www/enricci && sudo systemctl start enricci"
    exit 1
fi

echo "  - comprobando que responda"
if curl -fsS -o /dev/null http://127.0.0.1:5000/ ; then
    echo "  el sitio responde"
else
    echo "  ERROR: el servicio está andando pero el sitio no responde."
    sudo journalctl -u enricci -n 30 --no-pager
    exit 1
fi

sudo rm -rf "$CARPETA_APP".anterior
REMOTO

echo
echo "Listo."
