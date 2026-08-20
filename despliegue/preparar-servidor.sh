#!/usr/bin/env bash
#
# Prepara un servidor Ubuntu recién creado para alojar el sitio.
# Se corre UNA sola vez, en el servidor, como root o con sudo.
#
#   sudo bash preparar-servidor.sh
#
# Después de esto queda todo listo salvo el certificado, que necesita que el
# dominio ya apunte al servidor.

set -euo pipefail

USUARIO="enricci"
CARPETA_APP="/var/www/enricci"
CARPETA_DATOS="/var/lib/enricci"
CARPETA_CONFIG="/etc/enricci"

echo "==> Actualizando el sistema"
apt-get update -qq
apt-get upgrade -y -qq

echo "==> Instalando lo necesario"
# aspnetcore-runtime-8.0: para correr el sitio. No hace falta el SDK: el
# proyecto se compila en la máquina de desarrollo y acá sólo se ejecuta.
apt-get install -y -qq \
    aspnetcore-runtime-8.0 \
    nginx \
    certbot python3-certbot-nginx \
    unzip rsync \
    iptables-persistent

echo "==> Creando el usuario del servicio"
# Sin shell y sin home: este usuario existe únicamente para correr el sitio.
if ! id -u "$USUARIO" >/dev/null 2>&1; then
    adduser --system --group --no-create-home --shell /usr/sbin/nologin "$USUARIO"
fi

echo "==> Creando las carpetas"
# La aplicación va en una carpeta y los datos en otra, a propósito: así el
# despliegue puede reemplazar la aplicación entera sin tocar la base ni las
# fotos, que es lo único que no se puede volver a generar.
mkdir -p "$CARPETA_APP"
mkdir -p "$CARPETA_DATOS"/{fotos,respaldos,claves}
mkdir -p "$CARPETA_CONFIG"
mkdir -p /var/www/certbot

chown -R "$USUARIO:$USUARIO" "$CARPETA_DATOS"
chown -R "$USUARIO:$USUARIO" "$CARPETA_APP"

# La configuración tiene contraseñas: sólo la lee root y el servicio.
chown root:"$USUARIO" "$CARPETA_CONFIG"
chmod 750 "$CARPETA_CONFIG"

echo "==> Abriendo los puertos 80 y 443 en el cortafuegos de la máquina"
# Las imágenes de Ubuntu de Oracle Cloud traen reglas de iptables que sólo
# dejan pasar SSH. Abrir el puerto en la consola de Oracle no alcanza: hay que
# abrirlo también acá. Es el tropiezo más común al desplegar en Oracle.
iptables -I INPUT 5 -p tcp --dport 80  -m state --state NEW,ESTABLISHED -j ACCEPT
iptables -I INPUT 6 -p tcp --dport 443 -m state --state NEW,ESTABLISHED -j ACCEPT
netfilter-persistent save

echo
echo "Listo. Falta:"
echo "  1. Crear $CARPETA_CONFIG/enricci.env (ver enricci.env.ejemplo)"
echo "  2. Copiar enricci.service a /etc/systemd/system/"
echo "  3. Copiar nginx-enricci.conf a /etc/nginx/sites-available/enricci"
echo "  4. Desplegar el sitio con publicar.sh desde la máquina de desarrollo"
echo "  5. Cuando el dominio resuelva, pedir el certificado con certbot"
