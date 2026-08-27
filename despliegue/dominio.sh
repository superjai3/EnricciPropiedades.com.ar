#!/usr/bin/env bash
#
# Pone el sitio a responder en un dominio, con HTTPS. Sirve tanto para el
# provisorio de DuckDNS como para el definitivo cuando esté comprado:
#
#   bash despliegue/dominio.sh enricci.duckdns.org
#   bash despliegue/dominio.sh enriccipropiedades.com enriccipropiedades.com.ar
#
# Antes de correrlo, el dominio tiene que estar apuntando a la IP del servidor.
# En DuckDNS eso es escribir el nombre y la IP en el panel; tarda un minuto.
#
# Qué hace, en orden: deja nginx atendiendo por HTTP en ese nombre, saca el
# certificado de Let's Encrypt, pasa nginx a HTTPS, le avisa a la aplicación
# cuál es su dominio y la reinicia. Se puede volver a correr las veces que haga
# falta: si el certificado ya está, lo reutiliza.

set -euo pipefail

# El primero es el dominio principal —el que ve el visitante y el que declaran
# las URL canónicas— y los demás son alias que redirigen a él.
#
# Enricci tiene dos comprados: enriccipropiedades.com y enriccipropiedades.com.ar.
# Sirviendo el mismo sitio en los dos, un buscador ve el contenido duplicado en
# dos direcciones y reparte el posicionamiento; con uno principal y el otro
# redirigiendo, todo el peso queda en uno solo.
#
#   bash despliegue/dominio.sh enriccipropiedades.com enriccipropiedades.com.ar
#
# El correo se reconoce por la arroba, vaya en la posición que vaya, así que la
# forma vieja —dominio y después correo— sigue funcionando igual.
# Let's Encrypt lo usa sólo para avisar si un certificado está por vencer sin
# haberse renovado. Nunca manda publicidad.
MAIL="horacioenricci@gmail.com"
DOMINIOS=()

for arg in "$@"; do
    if [[ "$arg" == *"@"* ]]; then
        MAIL="$arg"
        continue
    fi
    # Un dominio con "http://" adelante o con barra al final es el error más
    # probable al copiar y pegar. Se limpia en vez de fallar más adelante.
    limpio="${arg#http://}"
    limpio="${limpio#https://}"
    limpio="${limpio%%/*}"
    # El www lo agrega el script solo; si viene escrito, se quita para no
    # terminar pidiendo un certificado para www.www.dominio.
    limpio="${limpio#www.}"
    [[ -n "$limpio" ]] && DOMINIOS+=("$limpio")
done

SERVIDOR="${SERVIDOR:-ubuntu@168.138.128.137}"
LLAVE="${LLAVE:-$HOME/.ssh/enricci.key}"

if [[ ${#DOMINIOS[@]} -eq 0 ]]; then
    echo "Falta el dominio. Ejemplos:" >&2
    echo "  bash despliegue/dominio.sh enricci.duckdns.org" >&2
    echo "  bash despliegue/dominio.sh enriccipropiedades.com enriccipropiedades.com.ar" >&2
    exit 1
fi

DOMINIO="${DOMINIOS[0]}"
ALIAS="${DOMINIOS[*]:1}"

echo "==> Configurando $DOMINIO en $SERVIDOR"
[[ -n "$ALIAS" ]] && echo "    redirigen a él: $ALIAS"

ssh -i "$LLAVE" -o StrictHostKeyChecking=accept-new "$SERVIDOR" \
    "DOMINIO='$DOMINIO' ALIAS='$ALIAS' MAIL='$MAIL' bash -s" <<'REMOTO'
set -euo pipefail

# Todos los nombres que va a atender el sitio: cada dominio y su www. El www se
# agrega siempre porque medio mundo lo escribe, y un certificado que no lo cubre
# da un aviso de seguridad en el navegador, que es peor que un 404.
NOMBRES=""
for d in $DOMINIO $ALIAS; do
    NOMBRES="$NOMBRES $d www.$d"
done
NOMBRES="${NOMBRES# }"

echo "  - comprobando que los dominios resuelvan"
# Se comprueban todos antes de pedir el certificado. Let's Encrypt limita a
# cinco fallos por hora: si uno de los nombres no resuelve, el pedido entero
# falla y se gasta un intento por un dominio que ni siquiera es el principal.
FALTAN=""
for n in $NOMBRES; do
    if getent hosts "$n" > /dev/null; then
        echo "    $n -> $(getent hosts "$n" | awk '{print $1}' | head -1)"
    else
        echo "    $n -> NO RESUELVE"
        FALTAN="$FALTAN $n"
    fi
done

if [[ -n "$FALTAN" ]]; then
    echo >&2
    echo "  ERROR: estos nombres no resuelven:$FALTAN" >&2
    echo "  Revisá que cada uno tenga su registro A apuntando a este servidor," >&2
    echo "  esperá a que propague y volvé a intentar." >&2
    exit 1
fi

sudo mkdir -p /var/www/certbot

# --- Paso 1: nginx por HTTP ---------------------------------------------------
# Hace falta antes del certificado: Let's Encrypt comprueba que el dominio es
# nuestro pidiendo un archivo por el puerto 80. Sin esto no hay certificado.
echo "  - dejando nginx atendiendo por HTTP"
sudo tee /etc/nginx/sites-available/enricci > /dev/null <<'NGINX'
server {
    listen 80;
    listen [::]:80;
    server_name NOMBRES_AQUI;

    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    client_max_body_size 160M;
    server_tokens off;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host  $host;
        proxy_buffering off;
    }
}
NGINX

# Todos los nombres MENOS el principal: son los que redirigen. El www del
# propio dominio principal entra acá también, para que haya una sola
# direccion que sirva contenido.
REDIRIGEN=""
for n in $NOMBRES; do
    [ "$n" = "$DOMINIO" ] || REDIRIGEN="$REDIRIGEN $n"
done
REDIRIGEN="${REDIRIGEN# }"

sudo sed -i "s/NOMBRES_AQUI/$NOMBRES/g" /etc/nginx/sites-available/enricci
sudo sed -i "s/ALIAS_AQUI/$REDIRIGEN/g" /etc/nginx/sites-available/enricci
sudo sed -i "s/DOMINIO_AQUI/$DOMINIO/g" /etc/nginx/sites-available/enricci
sudo ln -sf /etc/nginx/sites-available/enricci /etc/nginx/sites-enabled/enricci
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx

# --- Paso 2: el certificado ---------------------------------------------------
echo "  - pidiendo el certificado a Let's Encrypt"
# Un -d por cada nombre, todos en el MISMO certificado. Pedir uno por dominio
# multiplica los intentos contra el límite de Let's Encrypt —cinco fallos por
# hora— y después hay que vigilar varias renovaciones en vez de una.
D_ARGS=""
for n in $NOMBRES; do D_ARGS="$D_ARGS -d $n"; done

# --cert-name fija cómo se llama la carpeta en /etc/letsencrypt/live/, que es la
# ruta que después escribe nginx. Sin esto, certbot le pone el nombre del PRIMER
# dominio que tuvo el certificado —acá, el provisorio de DuckDNS— y nginx se
# queda buscando una carpeta que no existe.
#
# --expand es para cuando ya hay un certificado que cubre parte de los nombres
# pedidos: sin la bandera, certbot para y pregunta si querés ampliarlo, y en un
# script sin nadie mirando eso es un cuelgue. Es el caso al mudarse de un
# dominio provisorio a uno definitivo.
sudo certbot certonly --webroot -w /var/www/certbot \
    --cert-name "$DOMINIO" \
    $D_ARGS \
    --expand \
    --non-interactive --agree-tos --email "$MAIL" \
    --keep-until-expiring

# --- Paso 3: nginx por HTTPS --------------------------------------------------
echo "  - pasando nginx a HTTPS"
sudo tee /etc/nginx/sites-available/enricci > /dev/null <<'NGINX'
# HTTP: sólo para renovar el certificado y mandar todo a HTTPS.
#
# Atiende TODOS los nombres —el principal, los alias y sus www— y los manda al
# principal por HTTPS. Así cualquier forma de escribir la dirección termina en
# la misma, que es lo que necesita un buscador para no repartir el
# posicionamiento entre varias.
server {
    listen 80;
    listen [::]:80;
    server_name NOMBRES_AQUI;

    # Tiene que seguir accesible por HTTP: si se redirige también esto, la
    # renovación automática falla y el certificado vence en noventa días.
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    location / {
        return 301 https://DOMINIO_AQUI$request_uri;
    }
}

# HTTPS de los alias: mismo certificado, y de acá también al principal.
# Va antes del bloque principal a propósito: nginx elige por nombre exacto, así
# que el orden no cambia nada, pero leerlo en este orden deja claro que los
# alias no sirven contenido.
server {
    listen 443 ssl;
    listen [::]:443 ssl;
HTTP2_AQUI
    server_name ALIAS_AQUI;

    ssl_certificate     /etc/letsencrypt/live/DOMINIO_AQUI/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/DOMINIO_AQUI/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;

    return 301 https://DOMINIO_AQUI$request_uri;
}

server {
    listen 443 ssl;
    listen [::]:443 ssl;
HTTP2_AQUI
    # Sólo el dominio pelado. El www y los alias entran por el bloque de arriba
    # y redirigen acá: si www sirviera contenido, volverían a ser dos
    # direcciones con lo mismo, que es justo lo que se quiere evitar.
    server_name DOMINIO_AQUI;

    ssl_certificate     /etc/letsencrypt/live/DOMINIO_AQUI/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/DOMINIO_AQUI/privkey.pem;

    # Los ajustes de TLS van escritos acá y no incluidos desde un archivo de
    # certbot: ese archivo lo instala el complemento de nginx, que puede no
    # estar, y entonces nginx no arranca por un include que falta.
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 1d;
    ssl_session_tickets off;

    # El panel acepta hasta 12 fotos de 20 MB en una sola tanda y el límite de
    # nginx viene en 1 MB. Sin esto, subir fotos falla con un 413.
    client_max_body_size 160M;
    server_tokens off;

    access_log /var/log/nginx/enricci-access.log;
    error_log  /var/log/nginx/enricci-error.log;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;

        # Sin estas cabeceras el sitio cree que todos los pedidos llegan por HTTP
        # desde 127.0.0.1: las URL canónicas saldrían mal y el freno por IP de la
        # pantalla de ingreso agruparía a todos los visitantes en uno solo.
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host  $host;

        proxy_set_header Upgrade    $http_upgrade;
        proxy_set_header Connection $http_connection;
        proxy_cache_bypass $http_upgrade;

        proxy_connect_timeout 30s;
        proxy_send_timeout    300s;
        proxy_read_timeout    300s;

        proxy_buffering off;
    }
}
NGINX

# Todos los nombres MENOS el principal: son los que redirigen. El www del
# propio dominio principal entra acá también, para que haya una sola
# direccion que sirva contenido.
REDIRIGEN=""
for n in $NOMBRES; do
    [ "$n" = "$DOMINIO" ] || REDIRIGEN="$REDIRIGEN $n"
done
REDIRIGEN="${REDIRIGEN# }"

sudo sed -i "s/NOMBRES_AQUI/$NOMBRES/g" /etc/nginx/sites-available/enricci
sudo sed -i "s/ALIAS_AQUI/$REDIRIGEN/g" /etc/nginx/sites-available/enricci
sudo sed -i "s/DOMINIO_AQUI/$DOMINIO/g" /etc/nginx/sites-available/enricci

# HTTP/2 se pide de dos maneras distintas según la versión de nginx, y la que
# no corresponde no es una advertencia: nginx directamente no arranca. Hasta la
# 1.25.0 va pegado al listen; de la 1.25.1 en adelante es una directiva aparte.
VERSION_NGINX="$(nginx -v 2>&1 | sed 's|.*/||' | tr -d '[:space:]')"
if [[ "$(printf '%s\n1.25.1\n' "$VERSION_NGINX" | sort -V | head -1)" == "1.25.1" ]]; then
    echo "    nginx $VERSION_NGINX: http2 como directiva"
    sudo sed -i 's/^HTTP2_AQUI$/    http2 on;/' /etc/nginx/sites-available/enricci
else
    echo "    nginx $VERSION_NGINX: http2 pegado al listen"
    sudo sed -i '/^HTTP2_AQUI$/d' /etc/nginx/sites-available/enricci
    sudo sed -i 's/^\( *\)listen 443 ssl;$/\1listen 443 ssl http2;/' /etc/nginx/sites-available/enricci
    sudo sed -i 's/^\( *\)listen \[::\]:443 ssl;$/\1listen [::]:443 ssl http2;/' /etc/nginx/sites-available/enricci
fi

sudo nginx -t
sudo systemctl reload nginx

# --- Paso 4: avisarle a la aplicación cuál es su dominio ----------------------
# Con esto las URL canónicas, las de compartir y el mapa del sitio salen con el
# dominio y no con la IP, aunque alguien entre por la IP.
echo "  - anotando el dominio en la configuración"
if sudo grep -q '^Sitio__Dominio=' /etc/enricci/enricci.env; then
    sudo sed -i "s|^Sitio__Dominio=.*|Sitio__Dominio=$DOMINIO|" /etc/enricci/enricci.env
else
    echo "Sitio__Dominio=$DOMINIO" | sudo tee -a /etc/enricci/enricci.env > /dev/null
fi

echo "  - reiniciando el sitio"
sudo systemctl restart enricci
sleep 5

if ! systemctl is-active --quiet enricci; then
    echo "  ERROR: el sitio no quedó andando." >&2
    sudo journalctl -u enricci -n 30 --no-pager
    exit 1
fi

echo "  - asegurando el puerto 443 en el cortafuegos de la máquina"
# Idempotente: si la regla ya está —la pone preparar-servidor.sh— no hace nada.
if ! sudo iptables -C INPUT -p tcp --dport 443 -m state --state NEW,ESTABLISHED -j ACCEPT 2>/dev/null; then
    sudo iptables -I INPUT 6 -p tcp --dport 443 -m state --state NEW,ESTABLISHED -j ACCEPT
    sudo netfilter-persistent save > /dev/null
    echo "    se agregó la regla que faltaba"
fi

echo "  - comprobando que responda por HTTPS"
# Se resuelve a mano contra 127.0.0.1 a propósito: así se prueba nginx, el
# certificado y el sitio sin depender de que la máquina pueda salir a internet
# y volver a entrar por su propia IP pública, que no siempre está permitido.
codigo=$(curl -s --resolve "$DOMINIO:443:127.0.0.1" -o /dev/null -w '%{http_code}' "https://$DOMINIO/")
if [[ "$codigo" != "200" ]]; then
    echo "  ERROR: https://$DOMINIO/ contestó $codigo" >&2
    sudo journalctl -u enricci -n 20 --no-pager
    exit 1
fi

echo "  - comprobando que la renovación automática esté armada"
sudo systemctl list-timers 'certbot*' --no-pager | head -3

echo
echo "  El sitio responde en https://$DOMINIO/"
REMOTO

echo
echo "Listo. Mostrale al cliente: https://$DOMINIO/"
echo
echo "Si desde el navegador no abre, falta la regla del puerto 443 en el"
echo "cortafuegos de Oracle, que es el único que no se puede tocar desde acá:"
echo "  Networking -> Virtual Cloud Networks -> la VCN -> Security Lists ->"
echo "  Default Security List -> Add Ingress Rules"
echo "  Origen 0.0.0.0/0, TCP, puerto de destino 443."
