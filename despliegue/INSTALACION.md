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

Completar `Admin__Email` (el correo con el que se entra al panel; sin él no se
crea el usuario), la casilla y la contraseña de aplicación de Gmail. **Dejar
`Sitio__Dominio` vacío hasta que el dominio resuelva de verdad**, y `AllowedHosts`
comentado hasta que el sitio esté andando por el dominio.

Este archivo nunca va al repositorio: tiene contraseñas.

### El servicio

```bash
# desde la máquina de desarrollo
scp -i ~/.ssh/enricci.key despliegue/enricci.service ubuntu@LA-IP:/tmp/

# en el servidor
sudo install -m 644 /tmp/enricci.service /etc/systemd/system/enricci.service
sudo systemctl daemon-reload
sudo systemctl enable enricci
```

La configuración de nginx no se copia a mano: la escribe `dominio.sh` en el paso
siguiente, hecha a medida del dominio que se elija y de la versión de nginx que
tenga la máquina. Un archivo de ejemplo guardado en el repositorio envejece sin
que nadie se entere y se descubre el día del apuro.

---

## 4. El dominio y el certificado

Sirve igual para un dominio provisorio —para mostrarle el sitio a alguien antes
de comprar el definitivo— que para el real cuando esté comprado.

### Un dominio provisorio y gratuito, con DuckDNS

1. Entrar a <https://www.duckdns.org>, iniciar sesión con Google o GitHub.
2. En **domains**, escribir el nombre que se quiera (por ejemplo `enricci`) y
   crearlo. Queda `enricci.duckdns.org`.
3. En la fila del dominio, poner en **current ip** la IP del servidor y
   **update ip**. Tarda alrededor de un minuto en propagarse.

### Aplicarlo

```powershell
.\despliegue\dominio.ps1 enricci.duckdns.org
```

```bash
bash despliegue/dominio.sh enricci.duckdns.org
```

Deja nginx atendiendo en ese nombre, saca el certificado de Let's Encrypt, pasa
todo a HTTPS, le anota el dominio a la aplicación —así las URL canónicas, las de
compartir y el mapa del sitio salen con el dominio y no con la IP— y comprueba
que responda. Se puede correr las veces que haga falta: si el certificado ya
está, lo reutiliza.

El mismo comando sirve el día que esté el dominio definitivo:

```powershell
.\despliegue\dominio.ps1 enricci-propiedades.com.ar
```

### Si el navegador no abre el sitio

Casi siempre falta la regla del puerto **443** en el cortafuegos de Oracle, que
es el único que el script no puede tocar: *Networking → Virtual Cloud Networks →
la VCN → Security Lists → Default Security List → Add Ingress Rules*, origen
`0.0.0.0/0`, TCP, puerto de destino 443.

---

## 5. Desplegar el sitio

Desde la máquina de desarrollo, en la carpeta del proyecto. Editar primero
`SERVIDOR` y `LLAVE` arriba de `despliegue/publicar.sh`, o pasarlos por variable:

```bash
SERVIDOR=ubuntu@LA-IP LLAVE=~/.ssh/enricci.key bash despliegue/publicar.sh --primera-vez
```

`--primera-vez` sube además la base de datos y las fotos que están en esta
máquina. **Se usa una sola vez**: en los despliegues siguientes hay que correrlo
sin esa opción, o pisaría lo que se cargó desde el panel.

De ahí en más, publicar un cambio es **fusionarlo en `main` y hacer push**:
GitHub Actions compila y despliega solo (ver [Despliegue automático (GitHub
Actions)](#despliegue-automático-github-actions), más abajo). Para hacerlo a
mano desde el PC —si GitHub está caído o hay que desplegar algo que no está en
`main`— sigue estando:

```bash
bash despliegue/publicar.sh
```

El script compila, sube, reemplaza la aplicación, reinicia y **comprueba que el
sitio responda**. Si no responde, deja el registro a la vista y explica cómo
volver a la versión anterior, que quedó guardada. Lo que corre en el servidor
está en `despliegue/instalar-en-servidor.sh`, que es el mismo archivo que usa
el despliegue automático: por los dos caminos pasa exactamente lo mismo.

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

---

### La contraseña del panel

Si la base es nueva, en el primer arranque se genera una contraseña al azar que
queda en un archivo con permisos 600 en la carpeta de datos (no en el registro,
que se copia y se comparte):

```bash
sudo cat /var/lib/enricci/clave-inicial.txt
```

El registro sólo dice dónde quedó (`sudo journalctl -u enricci | grep -i
"contraseña inicial"`). **El archivo se borra solo en el primer ingreso correcto
al panel**; si por algún motivo siguiera ahí, borrarlo a mano. Si se pierde la
contraseña: borrar la fila de la tabla `Usuarios` y reiniciar el servicio, que
crea una nueva.

---

## Despliegue automático (GitHub Actions)

Cada push a `main` compila el proyecto y lo despliega en el servidor sin tocar
el PC. Lo hace el workflow `.github/workflows/deploy.yml`, que se llama
**«Desplegar a Oracle»** y replica paso a paso a `publicar.sh`: compila en
Release, arma el paquete sin las fotos ni `appsettings.Development.json`, lo
sube por SSH a `/tmp` del servidor y corre `despliegue/instalar-en-servidor.sh`
—el mismo script que usa `publicar.sh`—, que reemplaza `/var/www/enricci`,
enlaza la carpeta de fotos, deja todo como `enricci:enricci`, reinicia el
servicio y comprueba que `http://127.0.0.1:5000/` conteste 200.

Lo nuevo respecto de publicar a mano: **si el proyecto no compila, el
despliegue se corta antes de tocar el servidor**, y el sitio queda con la
versión anterior. Y como nunca dos despliegues corren a la vez, dos push
seguidos se hacen uno detrás del otro.

La base de datos, las fotos y los respaldos viven en `/var/lib/enricci` y el
workflow no los toca, igual que `publicar.sh`. En el servidor no hay que
instalar ni cambiar nada: sólo autorizar una llave SSH más.

Para que funcione hacen falta **una llave SSH exclusiva para GitHub** y **tres
secrets** en el repositorio. Hasta que estén, el workflow falla en el paso
«Comprobar que estén los secrets» con un mensaje que lo dice: es lo esperado, no
hay nada roto.

### Paso 0: la IP tiene que ser fija

El workflow se conecta a la IP (o al dominio) que se guarde en el secret
`ORACLE_HOST`. Si la IP de la instancia es la efímera y Oracle la cambia, el
despliegue deja de llegar al servidor. Antes de seguir, reservarla como se
explica en [Fijar la dirección IP](#fijar-la-dirección-ip). Si se cambia igual
algún día, alcanza con actualizar el secret.

### Paso 1: generar una llave SSH sólo para GitHub

Se usa una llave nueva, distinta de `enricci.key`: si algún día hay que
revocarle el acceso a GitHub, se borra esta y la del PC sigue andando. Sin
frase de paso (`-N ""`), porque en GitHub no hay nadie para escribirla.

En PowerShell, en el PC de Jaime (Windows trae `ssh-keygen`):

```powershell
ssh-keygen -t ed25519 -N '""' -C "github-actions-enricci" -f "$env:USERPROFILE\.ssh\enricci-github"
```

En Git Bash o Linux:

```bash
ssh-keygen -t ed25519 -N "" -C "github-actions-enricci" -f ~/.ssh/enricci-github
```

Quedan dos archivos en `~/.ssh`:

| Archivo | Qué es | Adónde va |
| --- | --- | --- |
| `enricci-github` | la llave **privada** | al secret `ORACLE_SSH_KEY` en GitHub, y a ningún otro lado |
| `enricci-github.pub` | la llave **pública** | al servidor, en `authorized_keys` del usuario `ubuntu` |

### Paso 2: autorizar la llave pública en el servidor

Hay que agregar el contenido de `enricci-github.pub` (una sola línea que empieza
con `ssh-ed25519`) al final de `~/.ssh/authorized_keys` del usuario con el que
se despliega, que es `ubuntu` —el mismo que usa `publicar.sh`—. Entrando con la
llave de siempre:

```powershell
Get-Content "$env:USERPROFILE\.ssh\enricci-github.pub" | ssh -i "$env:USERPROFILE\.ssh\enricci.key" ubuntu@LA-IP "cat >> ~/.ssh/authorized_keys"
```

```bash
cat ~/.ssh/enricci-github.pub | ssh -i ~/.ssh/enricci.key ubuntu@LA-IP "cat >> ~/.ssh/authorized_keys"
```

Para comprobar que quedó bien, entrar con la llave nueva. Tiene que abrir sin
pedir contraseña:

```powershell
ssh -i "$env:USERPROFILE\.ssh\enricci-github" ubuntu@LA-IP "echo funciona"
```

Nada más en el servidor: `ubuntu` ya tiene `sudo` sin contraseña —así lo trae
la imagen de Oracle y así lo usa `publicar.sh`— y el workflow no necesita otra
cosa.

### Paso 3: crear los tres secrets en GitHub

En el repositorio, en GitHub: **Settings → Secrets and variables → Actions →
New repository secret**. Se crean tres, uno por vez. El nombre va **exacto**,
en mayúsculas:

| Name | Secret (el valor) |
| --- | --- |
| `ORACLE_SSH_KEY` | La llave **privada entera**: todo el contenido de `enricci-github`, desde la línea `-----BEGIN OPENSSH PRIVATE KEY-----` hasta `-----END OPENSSH PRIVATE KEY-----` inclusive, con los saltos de línea tal como están. |
| `ORACLE_HOST` | La IP fija del servidor (por ejemplo `168.138.128.137`) o el dominio, sin `http://` ni barra final. |
| `ORACLE_USER` | `ubuntu` |

Para copiar la llave privada sin equivocarse:

```powershell
Get-Content "$env:USERPROFILE\.ssh\enricci-github" | Set-Clipboard
```

y pegar en el campo del secret. Una vez guardado, GitHub no lo vuelve a
mostrar; si hay dudas de que quedó bien, se vuelve a pegar encima («Update»).

### Paso 4: probarlo

Ir a la pestaña **Actions** del repositorio, entrar en **Desplegar a Oracle**,
**Run workflow** sobre `main`. Se puede seguir paso a paso; si algo falla, el
paso en rojo dice por qué. También se dispara solo con cada push a `main`.

Desde ahí, la rutina de trabajo es: desarrollar en una rama, fusionar en
`main`, hacer push y **mirar que el run termine en verde**. Si termina en
rojo, el servidor sigue con la versión anterior; el motivo está en el registro
del run.

### Plan B: publicar desde el PC

`publicar.sh` (y `publicar.ps1`) siguen funcionando igual que siempre y corren
exactamente el mismo `instalar-en-servidor.sh`. Sirven si GitHub está caído,
si hay que desplegar algo que todavía no está en `main`, o si el workflow falla
por algo de GitHub y hay apuro.

### Si el despliegue falla

- **«Faltan los secrets»**: falta crear alguno de los tres del paso 3, o el
  nombre no está exacto.
- **`ssh-keyscan` no pudo llegar / `Connection timed out`**: la IP del secret
  no es la del servidor (¿cambió la efímera?) o el puerto 22 está cerrado en el
  cortafuegos de Oracle.
- **`Permission denied (publickey)`**: la llave pública no quedó en
  `authorized_keys` de `ubuntu`, o en `ORACLE_SSH_KEY` se pegó la pública en vez
  de la privada, o se pegó incompleta.
- **Falla en «Compilar en Release»**: el código no compila; nada llegó al
  servidor. Se arregla y se vuelve a hacer push.
- **Falla en «Desplegar en el servidor»**: el servicio no arrancó o el sitio no
  contestó. El registro del run muestra las últimas líneas del `journalctl` y
  el comando para volver atrás, igual que `publicar.sh`.

---

## 6. Cerrar

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
