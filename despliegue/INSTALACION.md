# Publicar el sitio en Oracle Cloud (capa gratuita)

Guía completa, de la cuenta recién creada al sitio andando en el dominio. Todo
lo que hay acá entra en la capa **Always Free** de Oracle: no vence a los doce
meses y no genera cargos mientras no se salga de esos límites.

El costo recurrente es **cero**. Lo único que se paga es la registración anual
del dominio `.com.ar` en NIC Argentina, que va por fuera.

---

## 1. La instancia

En la consola de Oracle Cloud: **Compute → Instances → Create instance**.

| Qué | Cuál | Por qué |
| --- | --- | --- |
| Región de origen | **São Paulo** (`sa-saopaulo-1`) | Es la más cercana a Buenos Aires. **No se puede cambiar después**, así que conviene elegirla al crear la cuenta. |
| Imagen | **Canonical Ubuntu 24.04** | Trae el runtime de .NET 8 en sus propios repositorios; en 22.04 hay que agregar el de Microsoft. |
| Forma | **VM.Standard.A1.Flex**, 4 OCPU y 24 GB | Es la ARM de la capa gratuita. Sobra para este sitio. |
| Disco | 50 GB (el mínimo alcanza y sobra) | La capa gratuita da hasta 200 GB en total. |
| Clave SSH | Guardar la privada que ofrece descargar | Es la única forma de entrar; Oracle no la vuelve a mostrar. |

**Si dice «Out of host capacity»** —pasa seguido con las ARM— hay dos caminos:
probar en otro dominio de disponibilidad, o crear una **VM.Standard.E2.1.Micro**,
que también es Always Free y casi siempre está disponible. Tiene 1 GB de RAM: el
sitio anda igual, pero conviene agregarle memoria de intercambio:

```bash
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile
sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### Fijar la dirección IP

Por omisión la IP pública es efímera y puede cambiar. En **Instance → Attached
VNICs → IP addresses**, editar la IP pública y pasarla a **Reserved**. Si no, un
día el dominio deja de resolver sin que nadie haya tocado nada.

---

## 2. Abrir los puertos — los dos cortafuegos

Este es el tropiezo más común al desplegar en Oracle: hay **dos** cortafuegos y
hay que abrir los dos.

**El de Oracle**, en la consola: *Networking → Virtual Cloud Networks → la VCN →
Security Lists → Default Security List → Add Ingress Rules*. Agregar dos reglas,
origen `0.0.0.0/0`, protocolo TCP, puertos de destino **80** y **443**.

**El de la máquina**, dentro de Ubuntu: lo resuelve `preparar-servidor.sh`. Si se
abre sólo el de Oracle, el sitio parece inalcanzable sin ningún error visible.

---

## 3. Preparar el servidor

Desde la máquina de desarrollo, con Git Bash:

```bash
scp -i ~/.ssh/enricci.key despliegue/preparar-servidor.sh ubuntu@LA-IP:/tmp/
ssh -i ~/.ssh/enricci.key ubuntu@LA-IP
sudo bash /tmp/preparar-servidor.sh
```

Instala el runtime de .NET 8, nginx y certbot; crea el usuario del servicio, las
carpetas y abre los puertos en el cortafuegos de la máquina.

### La configuración con las contraseñas

```bash
# desde la máquina de desarrollo
scp -i ~/.ssh/enricci.key despliegue/enricci.env.ejemplo ubuntu@LA-IP:/tmp/

# en el servidor
sudo install -m 640 -o root -g enricci /tmp/enricci.env.ejemplo /etc/enricci/enricci.env
sudo nano /etc/enricci/enricci.env
```

Completar la casilla y la contraseña de aplicación de Gmail. **Dejar
`Sitio__Dominio` vacío hasta que el dominio resuelva de verdad**, y `AllowedHosts`
comentado hasta que el sitio esté andando por el dominio.

Este archivo nunca va al repositorio: tiene contraseñas.

### El servicio y nginx

```bash
# desde la máquina de desarrollo
scp -i ~/.ssh/enricci.key despliegue/enricci.service despliegue/nginx-enricci.conf ubuntu@LA-IP:/tmp/

# en el servidor
sudo install -m 644 /tmp/enricci.service /etc/systemd/system/enricci.service
sudo systemctl daemon-reload
sudo systemctl enable enricci

sudo install -m 644 /tmp/nginx-enricci.conf /etc/nginx/sites-available/enricci
sudo ln -sf /etc/nginx/sites-available/enricci /etc/nginx/sites-enabled/enricci
sudo rm -f /etc/nginx/sites-enabled/default
```

nginx todavía **no** va a arrancar: la configuración pide certificados que
aún no existen. Es lo que sigue.

---

## 4. El dominio

En NIC Argentina, delegar el dominio a un servicio de DNS. Cloudflare tiene un
plan gratuito que alcanza de sobra.

Cargar dos registros apuntando a la IP reservada:

| Tipo | Nombre | Valor |
| --- | --- | --- |
| A | `@` | la IP pública |
| A | `www` | la IP pública |

**En Cloudflare, dejar el proxy en «DNS only» (la nube gris).** Con el proxy
encendido, la validación de Let's Encrypt por HTTP falla y el sitio ve la IP de
Cloudflare en lugar de la del visitante.

La propagación puede tardar. Comprobar antes de seguir:

```bash
dig +short www.enricci-propiedades.com.ar
```

Tiene que devolver la IP del servidor. **No seguir hasta que la devuelva**: el
certificado no se puede emitir antes.

---

## 5. El certificado

Con el dominio resolviendo:

```bash
# en el servidor
sudo certbot certonly --webroot -w /var/www/certbot \
  -d enricci-propiedades.com.ar -d www.enricci-propiedades.com.ar \
  --agree-tos -m horacioenricci@gmail.com --no-eff-email

sudo nginx -t && sudo systemctl reload nginx
```

certbot deja instalado un temporizador que renueva solo. Comprobarlo:

```bash
sudo certbot renew --dry-run
```

---

## 6. Desplegar el sitio

Desde la máquina de desarrollo, en la carpeta del proyecto. Editar primero
`SERVIDOR` y `LLAVE` arriba de `despliegue/publicar.sh`, o pasarlos por variable:

```bash
SERVIDOR=ubuntu@LA-IP LLAVE=~/.ssh/enricci.key bash despliegue/publicar.sh --primera-vez
```

`--primera-vez` sube además la base de datos y las fotos que están en esta
máquina. **Se usa una sola vez**: en los despliegues siguientes hay que correrlo
sin esa opción, o pisaría lo que se cargó desde el panel.

De ahí en más, publicar un cambio es:

```bash
bash despliegue/publicar.sh
```

El script compila, sube, reemplaza la aplicación, reinicia y **comprueba que el
sitio responda**. Si no responde, deja el registro a la vista y explica cómo
volver a la versión anterior, que quedó guardada.

### Desde PowerShell

`publicar.sh` es un script de bash y PowerShell no lo entiende. Para eso está
`publicar.ps1`, que es una envoltura: busca el bash que viene con Git para
Windows y le pasa el trabajo. El despliegue sigue siendo uno solo.

```powershell
git pull origin claude/horacio-real-estate-website-bt6gfo
.\despliegue\publicar.ps1
```

Se puede correr desde cualquier carpeta: el script se ubica solo. Acepta las
mismas opciones:

```powershell
.\despliegue\publicar.ps1 -PrimeraVez
.\despliegue\publicar.ps1 -Servidor ubuntu@1.2.3.4 -Llave "C:\Users\vos\.ssh\enricci.key"
```

Si Windows se niega a ejecutar el script por la política de scripts, esto lo
habilita para el usuario actual y se pide una sola vez:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

### La contraseña del panel

Si la base es nueva, en el primer arranque se genera una contraseña al azar que
se escribe **una sola vez** en el registro:

```bash
sudo journalctl -u enricci | grep -i "contraseña inicial"
```

Hay que anotarla en ese momento. Si se pierde: borrar la fila de la tabla
`Usuarios` y reiniciar el servicio, que crea una nueva.

---

## 7. Cerrar

Con el sitio andando por el dominio, dos ajustes finales en
`/etc/enricci/enricci.env`:

```bash
Sitio__Dominio=www.enricci-propiedades.com.ar
AllowedHosts=enricci-propiedades.com.ar;www.enricci-propiedades.com.ar
```

```bash
sudo systemctl restart enricci
```

Y dar de alta el dominio en **Google Search Console**, enviando
`https://www.enricci-propiedades.com.ar/sitemap.xml`.

---

## Uso diario

```bash
sudo systemctl status enricci          # ¿está andando?
sudo journalctl -u enricci -f          # ver el registro en vivo
sudo journalctl -u enricci -n 100      # las últimas 100 líneas
sudo systemctl restart enricci         # reiniciar
```

**Los respaldos corren solos**, todos los días a las 3 de la mañana (hora de
Buenos Aires), y quedan en `/var/lib/enricci/respaldos`. Desde *Panel →
Respaldos* se puede hacer uno a mano y **descargarlo**.

Conviene bajar uno cada tanto: un respaldo que vive en el mismo servidor no
sirve el día que se pierde el servidor.

---

## Cosas que conviene saber

- **La capa gratuita de Oracle recupera instancias inactivas** en cuentas que
  sólo usan recursos gratuitos. Un sitio con visitas reales no debería tener
  problema, pero si el sitio va a estar mucho tiempo sin tráfico, conviene
  mirarlo de vez en cuando.
- **Los términos de la capa gratuita cambian.** Conviene verificarlos al darse
  de alta y no darlos por sentados.
- **La base y las fotos viven en `/var/lib/enricci`**, fuera de la carpeta de la
  aplicación, justamente para que un despliegue no pueda pisarlas. Si algún día
  se cambia esa estructura, revisar `publicar.sh`.
