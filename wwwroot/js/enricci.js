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

    /* ---------- Galería del detalle de propiedad ---------- */
    document.addEventListener('click', function (ev) {
        var mini = ev.target.closest('[data-galeria-mini]');
        if (!mini) { return; }
        var principal = document.querySelector('[data-galeria-principal] img');
        var imagenMini = mini.querySelector('img');
        if (!principal || !imagenMini) { return; }
        var temp = principal.getAttribute('src');
        principal.setAttribute('src', imagenMini.getAttribute('src'));
        imagenMini.setAttribute('src', temp);
    });

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

    /* ---------- Año dinámico en el pie ---------- */
    var anio = document.querySelector('[data-anio]');
    if (anio) { anio.textContent = String(new Date().getFullYear()); }
})();
