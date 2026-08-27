/* R. H. Enricci Propiedades — comportamiento de interfaz (JS propio, sin librerías) */
(function () {
    'use strict';

    /* ---------- Tema claro / oscuro ---------- */
    var raiz = document.documentElement;

    function temaGuardado() {
        try { return localStorage.getItem('enricci-tema'); } catch (e) { return null; }
    }

    function aplicarTema(tema) {
        if (tema) { raiz.setAttribute('data-tema', tema); }
        else { raiz.removeAttribute('data-tema'); }
        try { localStorage.setItem('enricci-tema', tema || ''); } catch (e) { /* modo privado */ }
    }

    function temaActivo() {
        var t = raiz.getAttribute('data-tema');
        if (t) { return t; }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'oscuro' : 'claro';
    }

    var guardado = temaGuardado();
    if (guardado === 'oscuro' || guardado === 'claro') { raiz.setAttribute('data-tema', guardado); }

    document.addEventListener('click', function (ev) {
        var boton = ev.target.closest('[data-accion="tema"]');
        if (!boton) { return; }
        aplicarTema(temaActivo() === 'oscuro' ? 'claro' : 'oscuro');
    });

    /* ---------- Barra fija con sombra al hacer scroll ---------- */
    var barra = document.querySelector('.barra');
    if (barra) {
        var marcarBarra = function () {
            barra.classList.toggle('esta-fija', window.scrollY > 12);
        };
        marcarBarra();
        window.addEventListener('scroll', marcarBarra, { passive: true });
    }

    /* ---------- Menú móvil ---------- */
    var botonMenu = document.querySelector('.hamburguesa');
    var panel = document.querySelector('.panel-movil');

    function cerrarPanel() {
        if (!panel || !botonMenu) { return; }
        panel.classList.remove('abierto');
        botonMenu.setAttribute('aria-expanded', 'false');
        document.body.classList.remove('sin-scroll');
    }

    if (botonMenu && panel) {
        botonMenu.addEventListener('click', function () {
            var abierto = panel.classList.toggle('abierto');
            botonMenu.setAttribute('aria-expanded', abierto ? 'true' : 'false');
            document.body.classList.toggle('sin-scroll', abierto);
        });
        panel.addEventListener('click', function (ev) {
            if (ev.target.closest('a')) { cerrarPanel(); }
        });
    }

    document.addEventListener('keydown', function (ev) {
        if (ev.key === 'Escape') { cerrarPanel(); }
    });

    window.addEventListener('resize', function () {
        if (window.innerWidth > 1080) { cerrarPanel(); }
    });

    /* ---------- Desplegables (menú móvil, FAQ) ---------- */
    document.addEventListener('click', function (ev) {
        var disparador = ev.target.closest('[data-desplegable]');
        if (!disparador) { return; }
        var abierto = disparador.getAttribute('aria-expanded') === 'true';
        disparador.setAttribute('aria-expanded', abierto ? 'false' : 'true');
    });

    /* ---------- Revelado al hacer scroll ---------- */
    var aRevelar = document.querySelectorAll('.revelar');
    if (aRevelar.length) {
        if ('IntersectionObserver' in window) {
            var observador = new IntersectionObserver(function (entradas) {
                entradas.forEach(function (entrada) {
                    if (entrada.isIntersecting) {
                        entrada.target.classList.add('visible');
                        observador.unobserve(entrada.target);
                    }
                });
            }, { rootMargin: '0px 0px -8% 0px', threshold: 0.08 });
            aRevelar.forEach(function (el) { observador.observe(el); });
        } else {
            aRevelar.forEach(function (el) { el.classList.add('visible'); });
        }
    }

    /* ---------- Contadores de métricas ---------- */
    var metricas = document.querySelectorAll('[data-contador]');
    if (metricas.length && 'IntersectionObserver' in window) {
        var reducido = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var obsNum = new IntersectionObserver(function (entradas) {
            entradas.forEach(function (entrada) {
                if (!entrada.isIntersecting) { return; }
                var el = entrada.target;
                obsNum.unobserve(el);
                var destino = parseFloat(el.getAttribute('data-contador')) || 0;
                var sufijo = el.getAttribute('data-sufijo') || '';
                if (reducido) { el.textContent = destino + sufijo; return; }
                var inicio = null;
                var duracion = 1400;
                var paso = function (t) {
                    if (inicio === null) { inicio = t; }
                    var avance = Math.min((t - inicio) / duracion, 1);
                    var suave = 1 - Math.pow(1 - avance, 3);
                    el.textContent = Math.round(destino * suave) + sufijo;
                    if (avance < 1) { requestAnimationFrame(paso); }
                };
                requestAnimationFrame(paso);
            });
        }, { threshold: 0.4 });
        metricas.forEach(function (el) { obsNum.observe(el); });
    }

    /* ---------- Pestañas del buscador (venta / alquiler / …) ---------- */
    var tabs = document.querySelectorAll('.buscador__tab');
    if (tabs.length) {
        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                tabs.forEach(function (t) {
                    t.classList.remove('activo');
                    t.setAttribute('aria-pressed', 'false');
                });
                tab.classList.add('activo');
                tab.setAttribute('aria-pressed', 'true');
                var campo = document.getElementById('campo-operacion');
                if (campo) { campo.value = tab.getAttribute('data-valor') || ''; }
            });
        });
    }

    /* ---------- Visor de fotos de la propiedad ----------
       Foto grande + miniaturas + pantalla completa. Teclado, deslizamiento
       táctil y foco controlado. Todo se engancha desde acá: la política de
       contenido del sitio no permite manejadores escritos en el HTML. */
    (function () {
        var visor = document.querySelector('[data-visor]');
        if (!visor) { return; }

        var foto = visor.querySelector('[data-visor-foto]');
        var contador = visor.querySelector('[data-visor-contador]');
        var minis = Array.prototype.slice.call(visor.querySelectorAll('[data-visor-ir]'));
        if (!foto) { return; }

        var fuentes = minis.length
            ? minis.map(function (m) { return m.querySelector('img').getAttribute('src'); })
            : [foto.getAttribute('src')];

        var actual = 0;
        var reducido = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        function pintar(indice, conFundido) {
            if (indice < 0) { indice = fuentes.length - 1; }
            if (indice >= fuentes.length) { indice = 0; }
            actual = indice;

            var aplicar = function () {
                foto.setAttribute('src', fuentes[actual]);
                foto.setAttribute('alt', 'Foto ' + (actual + 1) + ' de la propiedad');
                foto.classList.remove('cambiando');
            };

            if (conFundido && !reducido) {
                foto.classList.add('cambiando');
                window.setTimeout(aplicar, 140);
            } else {
                aplicar();
            }

            if (contador) { contador.textContent = (actual + 1) + ' / ' + fuentes.length; }

            minis.forEach(function (m, i) {
                m.classList.toggle('activo', i === actual);
                if (i === actual && m.scrollIntoView) {
                    m.scrollIntoView({ block: 'nearest', inline: 'nearest' });
                }
            });

            if (faroImagen && faro.classList.contains('abierto')) {
                faroImagen.setAttribute('src', fuentes[actual]);
                faroImagen.setAttribute('alt', 'Foto ' + (actual + 1) + ' de la propiedad');
                if (faroContador) { faroContador.textContent = (actual + 1) + ' / ' + fuentes.length; }
            }
        }

        /* --- Pantalla completa: se arma una sola vez --- */
        var faro = document.createElement('div');
        faro.className = 'faro';
        faro.setAttribute('role', 'dialog');
        faro.setAttribute('aria-modal', 'true');
        faro.setAttribute('aria-label', 'Fotos de la propiedad');
        faro.innerHTML =
            '<div class="faro__barra">' +
              '<span class="faro__contador" data-faro-contador></span>' +
              '<button class="faro__cerrar" type="button" data-faro-cerrar aria-label="Cerrar">' +
                '<svg aria-hidden="true"><use href="#i-cerrar"></use></svg></button>' +
            '</div>' +
            '<div class="faro__cuerpo"><img data-faro-img src="" alt="" /></div>' +
            '<div class="faro__pie">' +
              '<button class="faro__paso faro__paso--atras" type="button" data-visor-paso="-1" aria-label="Foto anterior">' +
                '<svg aria-hidden="true"><use href="#i-chevron"></use></svg></button>' +
              '<button class="faro__paso faro__paso--adelante" type="button" data-visor-paso="1" aria-label="Foto siguiente">' +
                '<svg aria-hidden="true"><use href="#i-chevron"></use></svg></button>' +
            '</div>';
        document.body.appendChild(faro);

        var faroImagen = faro.querySelector('[data-faro-img]');
        var faroContador = faro.querySelector('[data-faro-contador]');
        var focoPrevio = null;

        function abrirFaro() {
            focoPrevio = document.activeElement;
            faroImagen.setAttribute('src', fuentes[actual]);
            faroImagen.setAttribute('alt', 'Foto ' + (actual + 1) + ' de la propiedad');
            if (faroContador) { faroContador.textContent = (actual + 1) + ' / ' + fuentes.length; }
            faro.classList.add('abierto');
            document.body.classList.add('sin-scroll');
            faro.querySelector('[data-faro-cerrar]').focus();
        }

        function cerrarFaro() {
            faro.classList.remove('abierto');
            document.body.classList.remove('sin-scroll');
            if (focoPrevio && focoPrevio.focus) { focoPrevio.focus(); }
        }

        /* --- Interacción --- */
        document.addEventListener('click', function (ev) {
            var paso = ev.target.closest('[data-visor-paso]');
            if (paso) { pintar(actual + parseInt(paso.getAttribute('data-visor-paso'), 10), true); return; }

            var ir = ev.target.closest('[data-visor-ir]');
            if (ir) { pintar(parseInt(ir.getAttribute('data-visor-ir'), 10), true); return; }

            if (ev.target.closest('[data-visor-ampliar]')) { abrirFaro(); return; }
            if (ev.target.closest('[data-faro-cerrar]')) { cerrarFaro(); return; }
            // Un clic en el fondo del visor a pantalla completa también cierra.
            if (ev.target === faro || ev.target.closest('.faro__cuerpo') === ev.target) { cerrarFaro(); }
        });

        document.addEventListener('keydown', function (ev) {
            var enFaro = faro.classList.contains('abierto');
            if (ev.key === 'Escape' && enFaro) { cerrarFaro(); return; }
            // Fuera del visor a pantalla completa, las flechas sólo actúan si el
            // foco está dentro del visor: no se le roba el teclado a la página.
            if (!enFaro && !visor.contains(document.activeElement)) { return; }
            if (ev.key === 'ArrowLeft') { ev.preventDefault(); pintar(actual - 1, true); }
            if (ev.key === 'ArrowRight') { ev.preventDefault(); pintar(actual + 1, true); }
        });

        /* --- Deslizar con el dedo --- */
        var xInicial = null;
        function alTocar(elemento) {
            elemento.addEventListener('touchstart', function (ev) {
                xInicial = ev.changedTouches[0].clientX;
            }, { passive: true });
            elemento.addEventListener('touchend', function (ev) {
                if (xInicial === null) { return; }
                var recorrido = ev.changedTouches[0].clientX - xInicial;
                xInicial = null;
                if (Math.abs(recorrido) < 45) { return; }
                pintar(actual + (recorrido < 0 ? 1 : -1), true);
            }, { passive: true });
        }
        alTocar(visor.querySelector('.visor__escenario'));
        alTocar(faro.querySelector('.faro__cuerpo'));

        pintar(0, false);
    })();

    /* ---------- Ver la contraseña que se está escribiendo ----------
       El botón se inserta desde acá y no en la vista: sin JavaScript el campo
       sigue andando igual, y no hay que tocar cada formulario que lo use. */
    (function () {
        var campos = document.querySelectorAll('input[type="password"]');
        if (!campos.length) { return; }

        Array.prototype.forEach.call(campos, function (campo) {
            var envoltorio = document.createElement('div');
            envoltorio.className = 'campo-clave';
            campo.parentNode.insertBefore(envoltorio, campo);
            envoltorio.appendChild(campo);

            var boton = document.createElement('button');
            boton.type = 'button';
            boton.className = 'ver-clave';
            boton.setAttribute('aria-pressed', 'false');
            boton.setAttribute('aria-label', 'Mostrar la contraseña');
            boton.title = 'Mostrar la contraseña';
            boton.innerHTML =
                '<svg class="icono-visible" aria-hidden="true"><use href="#i-ojo"></use></svg>' +
                '<svg class="icono-oculto" aria-hidden="true"><use href="#i-ojo-tachado"></use></svg>';

            boton.addEventListener('click', function () {
                var visible = campo.getAttribute('type') === 'text';
                campo.setAttribute('type', visible ? 'password' : 'text');
                boton.setAttribute('aria-pressed', visible ? 'false' : 'true');

                var texto = visible ? 'Mostrar la contraseña' : 'Ocultar la contraseña';
                boton.setAttribute('aria-label', texto);
                boton.title = texto;

                // Devuelve el foco al campo, con el cursor al final.
                campo.focus();
                var largo = campo.value.length;
                try { campo.setSelectionRange(largo, largo); } catch (e) { /* algunos navegadores */ }
            });

            envoltorio.appendChild(boton);
        });
    })();

    /* ---------- Confirmación antes de una acción destructiva ----------
       Va por atributo y no por onsubmit en el HTML: la política de contenido
       del sitio no permite manejadores de eventos escritos en la marca. */
    document.addEventListener('submit', function (ev) {
        var formulario = ev.target.closest('[data-confirmar]');
        if (!formulario) { return; }
        if (!window.confirm(formulario.getAttribute('data-confirmar'))) {
            ev.preventDefault();
        }
    });

    /* ---------- Evitar el doble envío ----------
       Con conexión lenta es fácil apretar dos veces y duplicar una publicación
       o una consulta. El botón queda deshabilitado y avisa que está trabajando. */
    document.addEventListener('submit', function (ev) {
        var formulario = ev.target;
        if (ev.defaultPrevented || formulario.hasAttribute('data-sin-bloqueo')) { return; }

        var boton = formulario.querySelector('button[type="submit"], button:not([type])');
        if (!boton || boton.disabled) { return; }

        // Se difiere para no cortar el envío del formulario en curso.
        window.setTimeout(function () {
            boton.disabled = true;
            boton.setAttribute('aria-busy', 'true');
            if (boton.dataset.textoEnvio) { boton.textContent = boton.dataset.textoEnvio; }
        }, 0);
    });

    /* ---------- Propiedades guardadas ----------
       Viven en el navegador de cada visitante: no hace falta que se registre y
       la inmobiliaria no guarda ningún dato de quien mira. */
    var CLAVE_FAVORITOS = 'enricci-favoritos';

    function favoritos() {
        try {
            var crudo = localStorage.getItem(CLAVE_FAVORITOS);
            var lista = crudo ? JSON.parse(crudo) : [];
            return Array.isArray(lista) ? lista.map(String) : [];
        } catch (e) {
            return [];
        }
    }

    function guardarFavoritos(lista) {
        try { localStorage.setItem(CLAVE_FAVORITOS, JSON.stringify(lista)); } catch (e) { }
    }

    function pintarFavoritos() {
        var guardados = favoritos();

        document.querySelectorAll('[data-favorito]').forEach(function (boton) {
            var guardado = guardados.indexOf(String(boton.dataset.favorito)) !== -1;
            boton.setAttribute('aria-pressed', guardado ? 'true' : 'false');
            boton.setAttribute(
                'aria-label',
                (guardado ? 'Quitar de favoritos: ' : 'Guardar en favoritos: ') + (boton.dataset.titulo || ''));
        });

        var chip = document.querySelector('[data-filtro-favoritos]');
        if (chip) {
            var cuenta = chip.querySelector('[data-cuenta-favoritos]');
            if (cuenta) { cuenta.textContent = guardados.length; }
            chip.hidden = guardados.length === 0 && chip.getAttribute('aria-pressed') !== 'true';
        }
    }

    document.addEventListener('click', function (ev) {
        var boton = ev.target.closest('[data-favorito]');
        if (!boton) { return; }

        // La tarjeta entera es un enlace: sin esto, guardar navegaría.
        ev.preventDefault();
        ev.stopPropagation();

        var id = String(boton.dataset.favorito);
        var lista = favoritos();
        var posicion = lista.indexOf(id);

        if (posicion === -1) { lista.push(id); } else { lista.splice(posicion, 1); }

        guardarFavoritos(lista);
        pintarFavoritos();
        aplicarFiltroFavoritos();
    });

    /* Mostrar sólo lo guardado, sin volver al servidor. */
    function aplicarFiltroFavoritos() {
        var chip = document.querySelector('[data-filtro-favoritos]');
        if (!chip) { return; }

        var soloFavoritos = chip.getAttribute('aria-pressed') === 'true';
        var guardados = favoritos();
        var visibles = 0;

        document.querySelectorAll('[data-tarjeta-propiedad]').forEach(function (tarjeta) {
            var guardada = guardados.indexOf(String(tarjeta.dataset.tarjetaPropiedad)) !== -1;
            var mostrar = !soloFavoritos || guardada;
            // Se esconde la celda de la grilla: si se escondiera sólo la
            // tarjeta, quedaría el hueco vacío ocupando lugar.
            var celda = tarjeta.closest('.grilla-props > *') || tarjeta;
            celda.hidden = !mostrar;
            if (mostrar) { visibles++; }
        });

        var vacio = document.querySelector('[data-favoritos-vacio]');
        if (vacio) { vacio.hidden = !(soloFavoritos && visibles === 0); }
    }

    var chipFavoritos = document.querySelector('[data-filtro-favoritos]');
    if (chipFavoritos) {
        chipFavoritos.addEventListener('click', function () {
            var activo = chipFavoritos.getAttribute('aria-pressed') === 'true';
            chipFavoritos.setAttribute('aria-pressed', activo ? 'false' : 'true');
            chipFavoritos.classList.toggle('activo', !activo);
            aplicarFiltroFavoritos();
            pintarFavoritos();
        });
    }

    pintarFavoritos();
    aplicarFiltroFavoritos();

    /* ---------- Compartir la publicación ----------
       En el teléfono abre el menú del sistema —WhatsApp, mensajes, correo—; en
       la computadora, donde ese menú no existe, copia el enlace. */
    document.addEventListener('click', function (ev) {
        var boton = ev.target.closest('[data-compartir]');
        if (!boton) { return; }

        ev.preventDefault();

        var datos = {
            title: boton.dataset.titulo || document.title,
            text: boton.dataset.texto || '',
            url: boton.dataset.url || window.location.href
        };

        if (navigator.share) {
            navigator.share(datos).catch(function () { /* el visitante canceló */ });
            return;
        }

        var avisar = function (texto) {
            var original = boton.dataset.textoOriginal || boton.textContent.trim();
            boton.dataset.textoOriginal = original;
            var etiqueta = boton.querySelector('[data-compartir-texto]');
            if (etiqueta) {
                etiqueta.textContent = texto;
                window.setTimeout(function () { etiqueta.textContent = original; }, 2200);
            }
        };

        if (navigator.clipboard) {
            navigator.clipboard.writeText(datos.url)
                .then(function () { avisar('¡Enlace copiado!'); })
                .catch(function () { window.prompt('Copiá el enlace:', datos.url); });
        } else {
            window.prompt('Copiá el enlace:', datos.url);
        }
    });

    /* ---------- Calculadora de gastos de escrituración ---------- */
    /* El servidor ya dejó la página calculada; esto sólo rehace la cuenta
       mientras se escribe, para no tener que recargar por cada prueba. Si algo
       de acá falla, el formulario sigue funcionando como formulario. */
    var calculadora = document.querySelector('[data-calculadora]');

    if (calculadora) {
        var campoPrecio = calculadora.querySelector('[data-calculadora-precio]');
        var filas = document.querySelectorAll('[data-gasto]');
        var cotizacion = parseFloat(calculadora.dataset.cotizacion || '0');
        var fuente = calculadora.dataset.fuente || '';

        var moneda = function (valor) {
            return 'USD ' + Math.round(valor).toLocaleString('es-AR');
        };

        var escribir = function (selector, texto) {
            var destino = document.querySelector(selector);
            if (destino) { destino.textContent = texto; }
        };

        var recalcular = function () {
            var precio = parseFloat(campoPrecio.value);
            if (!isFinite(precio) || precio <= 0) { return; }

            var comprador = 0;
            var vendedor = 0;

            filas.forEach(function (fila) {
                var porcentaje = parseFloat(fila.dataset.porcentaje || '0');
                var fijo = parseFloat(fila.dataset.fijo || '0');
                var importe = precio * porcentaje / 100 + fijo;

                var paga = fila.dataset.paga;
                if (paga === 'Vendedor') { vendedor += importe; }
                else if (paga === 'Ambos') { comprador += importe / 2; vendedor += importe / 2; }
                else { comprador += importe; }

                var celda = fila.querySelector('[data-gasto-importe]');
                if (celda) { celda.textContent = moneda(importe); }
            });

            escribir('[data-total-comprador]', moneda(comprador));
            escribir('[data-total-vendedor]', moneda(vendedor));
            escribir('[data-total-necesita]', moneda(precio + comprador));

            var enPesos = document.querySelector('[data-total-pesos]');
            if (enPesos && cotizacion > 0) {
                /* Se rearma entero para que la aclaración de la fuente no se
                   pierda: sin fecha ni origen, el número en pesos no dice nada. */
                enPesos.textContent = '≈ $ ' + Math.round((precio + comprador) * cotizacion).toLocaleString('es-AR');

                var pie = document.createElement('span');
                pie.style.display = 'block';
                pie.style.fontSize = '.76rem';
                pie.style.opacity = '.85';
                pie.textContent = 'Según ' + fuente + ' · valor orientativo';
                enPesos.appendChild(pie);
            }
        };

        campoPrecio.addEventListener('input', recalcular);

        /* Con la cuenta al vuelo el botón deja de hacer falta. Se esconde recién
           acá, cuando ya sabemos que el script corrió. */
        var boton = calculadora.querySelector('[data-calculadora-boton]');
        if (boton) { boton.hidden = true; }

        calculadora.addEventListener('submit', function (ev) { ev.preventDefault(); recalcular(); });
    }

    /* ---------- Año dinámico en el pie ---------- */
    var anio = document.querySelector('[data-anio]');
    if (anio) { anio.textContent = String(new Date().getFullYear()); }

    /* ---------- Eventos de conversión ----------

       Las tres cosas que en esta inmobiliaria significan "alguien quiere hablar":
       mandar el formulario, escribir por WhatsApp y llamar por teléfono.

       Si no hay analítica configurada, `window.gtag` no existe y todo esto no
       hace nada: ni error en consola, ni pedido de red. Los disparadores quedan
       puestos para el día en que Horacio cargue el identificador. */
    function medir(evento, datos) {
        if (typeof window.gtag !== 'function') { return; }
        window.gtag('event', evento, datos || {});
    }

    var formularioContacto = document.querySelector('[data-form-contacto]');
    if (formularioContacto) {
        formularioContacto.addEventListener('submit', function () {
            // En el submit y no en la respuesta: si la página navega, el evento
            // ya salió. GA4 los manda con sendBeacon, que sobrevive a la
            // descarga de la página.
            var motivo = formularioContacto.querySelector('[name$="Motivo"]');
            medir('generate_lead', {
                metodo: 'formulario',
                motivo: motivo ? motivo.value : ''
            });
        });
    }

    document.addEventListener('click', function (evento) {
        var enlace = evento.target.closest('a[href]');
        if (!enlace) { return; }

        var destino = enlace.getAttribute('href') || '';

        if (destino.indexOf('wa.me/') !== -1) {
            medir('generate_lead', { metodo: 'whatsapp', desde: ubicacion(enlace) });
        } else if (destino.indexOf('tel:') === 0) {
            medir('generate_lead', { metodo: 'telefono', desde: ubicacion(enlace) });
        }
    });

    /* Desde dónde se tocó: sirve para saber si convierte más el botón flotante,
       el del pie o el de la ficha de una propiedad. */
    function ubicacion(enlace) {
        if (enlace.closest('.wsp-flotante')) { return 'boton-flotante'; }
        if (enlace.closest('footer')) { return 'pie'; }
        if (enlace.closest('.ficha')) { return 'ficha'; }
        return 'pagina';
    }
})();
