#!/usr/bin/env bash
#
# La parte del despliegue que corre EN EL SERVIDOR. No se ejecuta a mano: la
# mandan por SSH tanto publicar.sh (desde la máquina de desarrollo) como el
# workflow de GitHub Actions (.github/workflows/deploy.yml), así:
#
#   ssh ... "primera_vez=false bash -s" < despliegue/instalar-en-servidor.sh
#
# Está en un archivo aparte y no copiado en los dos lugares por el mismo motivo
# que publicar.ps1 envuelve a publicar.sh en vez de traducirlo: dos copias de lo
# mismo terminan siempre con una vieja, y la vieja es la que se usa el día del
# apuro.
#
# Espera que el paquete ya esté en /tmp/enricci-despliegue.tar.gz y, si
# primera_vez=true, opcionalmente /tmp/enricci.db y /tmp/enricci-fotos.tar.gz.
# Para el servicio, guarda la versión anterior en /var/www/enricci.anterior,
# desempaqueta la nueva, enlaza la carpeta de fotos, arregla los permisos,
# arranca y comprueba que responda. Si no responde, deja el registro a la vista
# y explica cómo volver atrás.

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
