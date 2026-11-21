"""Genera las imágenes de la carpeta design/vitalia/<opcion>/exports a partir de los tokens CSS (web/tokens-opcion-*.css).

Uso (desde la raíz del repositorio):  python design/vitalia/scripts/generar_exports.py
Requiere: pip install playwright && playwright install chromium (o Google Chrome instalado).
Los PNG salen a escala 2. Los contrastes se calculan con la fórmula de luminancia relativa de WCAG 2.1.
"""
import json
import re
import sys
from pathlib import Path

from playwright.sync_api import sync_playwright

RAIZ = Path(__file__).resolve().parents[1]
WEB = RAIZ / "web"
NOMBRES = {"a": "Opción A (Eric)", "b": "Opción B (Claude)"}


def leer_tokens(op):
    """Devuelve (claro, oscuro): dicts nombre -> valor, leídos del CSS de la opción."""
    css = (WEB / f"tokens-opcion-{op}.css").read_text(encoding="utf-8")
    bloques = re.findall(r'\[data-opcion="%s"\](\[data-tema="oscuro"\])?\s*\{(.*?)\n\}' % op, css, re.S)
    claro, oscuro = {}, {}
    for tema, cuerpo in bloques:
        destino = oscuro if tema else claro
        for nombre, valor in re.findall(r"--vt-([\w-]+):\s*([^;]+);", cuerpo):
            destino[nombre] = valor.strip()
    return claro, {**claro, **oscuro}


def luminancia(h):
    h = h.lstrip("#")
    r, g, b = (int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))
    f = lambda c: c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4
    return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b)


def contraste(a, b):
    x, y = sorted([luminancia(a), luminancia(b)], reverse=True)
    return (x + 0.05) / (y + 0.05)


# ---------- iconos de ejemplo (trazo propio, 24 px). En la app se usa la librería elegida (Lucide / Phosphor). ----------
ICONOS = {
    "Huella": '<circle cx="5" cy="10" r="1.6"/><circle cx="9" cy="5.5" r="1.6"/><circle cx="15" cy="5.5" r="1.6"/><circle cx="19" cy="10" r="1.6"/><path d="M12 12c-3 0-5.5 3-5.5 5.2 0 1.6 1.4 2.3 3 2.3 1 0 1.6-.4 2.5-.4s1.5.4 2.5.4c1.6 0 3-.7 3-2.3C17.5 15 15 12 12 12z"/>',
    "Calendario": '<rect x="3" y="5" width="18" height="16" rx="3"/><path d="M8 3v4M16 3v4M3 10h18"/>',
    "Vacuna": '<path d="m18 2 4 4M17 7l3-3M19 9 8.5 19.5 4 15 14.5 4.5l4.5 4.5zM6 17l-3 3M11 12l2 2M8 15l2 2"/>',
    "Corazón": '<path d="M12 21s-8-5-8-11a5 5 0 0 1 8-3.5A5 5 0 0 1 20 10c0 6-8 11-8 11z"/>',
    "Pastilla": '<rect x="2.5" y="8.5" width="19" height="7" rx="3.5" transform="rotate(-35 12 12)"/><path d="m9 9 6 6"/>',
    "Buscar": '<circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/>',
    "Persona": '<circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 3.6-6 8-6s8 2 8 6"/>',
    "Campana": '<path d="M6 16V11a6 6 0 0 1 12 0v5l2 2H4z"/><path d="M10 21h4"/>',
    "Tarjeta": '<rect x="2.5" y="5" width="19" height="14" rx="3"/><path d="M2.5 10h19M6 15h4"/>',
    "Alerta": '<path d="M12 3 2 20h20z"/><path d="M12 10v4M12 17h.01"/>',
    "Visto": '<circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/>',
    "Cama": '<path d="M3 18V7M3 13h18v5M21 13a3 3 0 0 0-3-3h-7v3"/>',
    "Reloj": '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
    "Casa": '<path d="m3 11 9-8 9 8"/><path d="M5 10v10h14V10M10 20v-6h4v6"/>',
    "Camión": '<path d="M2 6h12v10H2zM14 9h4l4 4v3h-8"/><circle cx="7" cy="18" r="2"/><circle cx="17" cy="18" r="2"/>',
    "Engranaje": '<circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3M5 5l2 2M17 17l2 2M19 5l-2 2M7 17l-2 2"/>',
}


def svg(nombre, duo=False, clase="vt-ico"):
    return f'<svg class="{clase}" viewBox="0 0 24 24" aria-hidden="true">{ICONOS[nombre]}</svg>'


def pagina(op, cuerpo, tema="claro"):
    return f"""<!doctype html><html lang="es-UY"><head><meta charset="utf-8">
<link rel="stylesheet" href="{(RAIZ / 'fuentes' / 'fuentes.css').as_uri()}">
<link rel="stylesheet" href="{(WEB / f'tokens-opcion-{op}.css').as_uri()}">
<link rel="stylesheet" href="{(WEB / 'componentes.css').as_uri()}">
<style>
body{{margin:0;background:#ddd}}
.tablero{{width:1200px;padding:40px;box-sizing:border-box;display:inline-block}}
.tablero h1{{font-size:var(--vt-t-xl);margin-bottom:4px}} .tablero h2{{font-size:var(--vt-t-l);margin:28px 0 10px}}
.fila{{display:flex;flex-wrap:wrap;gap:14px;align-items:flex-start}}
.muestra{{width:176px;border:1px solid var(--vt-borde);border-radius:var(--vt-radio-m);overflow:hidden;background:var(--vt-superficie);font-size:12px}}
.muestra .color{{height:64px;display:flex;align-items:flex-end;padding:6px 8px;font-weight:600;font-size:12px}}
.muestra .dato{{padding:8px;line-height:1.35}} .muestra code{{font-size:12px}}
table{{border-collapse:collapse;width:100%;font-size:14px}} td,th{{border-bottom:1px solid var(--vt-borde);padding:8px 10px;text-align:left}}
.ico{{width:120px;display:grid;place-items:center;gap:6px;padding:14px 6px;border:1px solid var(--vt-borde);border-radius:var(--vt-radio-m);background:var(--vt-superficie);font-size:12px}}
.ico svg{{width:32px;height:32px;color:var(--vt-primario)}}
.duo svg path,.duo svg rect,.duo svg circle{{fill:var(--vt-primario);fill-opacity:.18}}
.ok{{color:var(--vt-exito);font-weight:600}} .sp{{display:inline-block;width:20px;height:20px;border:3px solid currentColor;border-right-color:transparent;border-radius:50%;animation:g .8s linear infinite}}
@keyframes g{{to{{transform:rotate(360deg)}}}}
.barra{{height:10px;background:var(--vt-superficie-2);border-radius:99px;overflow:hidden}} .barra i{{display:block;height:100%;width:62%;background:var(--vt-primario)}}
.movil{{width:375px;height:812px;overflow:hidden;position:relative;padding:44px 16px 34px;box-sizing:border-box}}
</style></head><body>{cuerpo}</body></html>"""


def tablero(op, tema, id_, titulo, sub, contenido, extra_clase=""):
    return f'<div id="{id_}" data-opcion="{op}" data-tema="{tema}" class="tablero {extra_clase}"><h1>{titulo}</h1><p class="vt-muted">{NOMBRES[op]} · {sub}</p>{contenido}</div>'


def s1_colores(op, tokens, tema):
    t = tokens
    grupos = [
        ("Marca", ["primario", "primario-hover", "primario-suave", "acento", "acento-hover", "acento-suave"]),
        ("Estados", ["exito", "exito-suave", "aviso", "aviso-suave", "peligro", "peligro-suave", "info", "info-suave"]),
        ("Superficies y texto", ["fondo", "superficie", "superficie-2", "texto", "texto-suave", "borde", "foco"]),
    ]
    html = ""
    for g, nombres in grupos:
        html += f"<h2>{g}</h2><div class='fila'>"
        for n in nombres:
            v = t[n]
            if not v.startswith("#"):
                continue
            blanco, negro = contraste(v, "#FFFFFF"), contraste(v, t["texto"] if t["texto"].startswith("#") else "#000000")
            fg = "#FFFFFF" if blanco >= negro else "#111111"
            html += (f"<div class='muestra'><div class='color' style='background:{v};color:{fg}'>{v}</div>"
                     f"<div class='dato'><code>--vt-{n}</code><br>sobre fondo: {contraste(v, t['fondo']):.2f}:1<br>con texto: {contraste(v, t['texto']):.2f}:1</div></div>")
        html += "</div>"
    pares = [("texto", "fondo"), ("texto", "superficie"), ("texto-suave", "fondo"), ("sobre-primario", "primario"),
             ("sobre-acento", "acento"), ("primario", "fondo"), ("exito", "exito-suave"), ("aviso", "aviso-suave"),
             ("peligro", "peligro-suave"), ("info", "info-suave")]
    filas = ""
    for a, b in pares:
        va, vb = t[a], t[b]
        r = contraste(va, vb)
        filas += f"<tr><td><code>--vt-{a}</code> {va}</td><td><code>--vt-{b}</code> {vb}</td><td>{r:.2f}:1</td><td class='{'ok' if r >= 4.5 else ''}'>{'AA (4,5:1)' if r >= 4.5 else 'No es texto'}</td></tr>"
    html += f"<h2>Pares de texto verificados</h2><table><tr><th>Texto</th><th>Fondo</th><th>Relación</th><th>Resultado</th></tr>{filas}</table>"
    return tablero(op, tema, "S1", "Colores y contrastes", f"tema {tema}", html)


def s2_tipografia(op):
    escala = [("2xl", "Display", 800), ("xl", "Título", 800), ("l", "Subtítulo", 600), ("m", "Cuerpo: Toby tiene turno mañana a las 09:00", 400), ("s", "Texto de ayuda y tablas", 400), ("xs", "Etiqueta o dato mínimo", 600)]
    html = "<h2>Escala tipográfica</h2>"
    for k, texto, peso in escala:
        html += f"<div style='display:flex;gap:20px;align-items:baseline;border-bottom:1px solid var(--vt-borde);padding:6px 0'><code style='width:90px'>--vt-t-{k}</code><span style='font-size:var(--vt-t-{k});font-weight:{peso}'>{texto}</span></div>"
    html += "<h2>Espaciado (base 4 px)</h2><div class='fila' style='align-items:flex-end'>"
    for k, px in [("e1", 4), ("e2", 8), ("e3", 12), ("e4", 16), ("e6", 24), ("e8", 32)]:
        html += f"<div style='text-align:center;font-size:12px'><div style='width:{px}px;height:{px}px;background:var(--vt-primario);margin:0 auto 6px'></div>{k}<br>{px}</div>"
    html += "</div><h2>Radios y sombras</h2><div class='fila'>"
    for k in ["radio-s", "radio-m", "radio-l"]:
        html += f"<div style='width:150px;height:90px;background:var(--vt-superficie);border:1px solid var(--vt-borde);border-radius:var(--vt-{k});display:grid;place-items:center;font-size:12px'>--vt-{k}</div>"
    for k in ["sombra-1", "sombra-2"]:
        html += f"<div style='width:150px;height:90px;background:var(--vt-superficie);border-radius:var(--vt-radio-m);box-shadow:var(--vt-{k});display:grid;place-items:center;font-size:12px'>--vt-{k}</div>"
    html += "</div>"
    return tablero(op, "claro", "S2", "Tipografía, espaciado, radios y sombras", "escala base", html)


def s3_botones(op, tema):
    def fila(titulo, clase):
        return (f"<tr><td>{titulo}</td><td><button class='vt-btn {clase}'>Confirmar</button></td>"
                f"<td><button class='vt-btn {clase}' style='filter:brightness(.88)'>Presionado</button></td>"
                f"<td><button class='vt-btn {clase}' style='outline:3px solid var(--vt-foco);outline-offset:2px'>Con foco</button></td>"
                f"<td><button class='vt-btn {clase}' disabled>Deshabilitado</button></td>"
                f"<td><button class='vt-btn {clase}'><span class='sp'></span> Guardando…</button></td></tr>")
    html = ("<h2>Botones (alto mínimo 44 px)</h2><table><tr><th></th><th>Por defecto</th><th>Presionado</th><th>Foco</th><th>Deshabilitado</th><th>Cargando</th></tr>"
            + fila("Primario", "vt-btn-primario") + fila("Acento", "vt-btn-acento") + fila("Borde", "vt-btn-borde") + fila("Peligro", "vt-btn-peligro") + "</table>")
    html += ("<h2>Botones con icono</h2><div class='fila'>"
             f"<button class='vt-btn vt-btn-primario'>{svg('Calendario')} Reservar turno</button>"
             f"<button class='vt-btn vt-btn-acento'>{svg('Tarjeta')} Pagar seña</button>"
             f"<button class='vt-btn vt-btn-borde'>{svg('Buscar')} Buscar</button></div>")
    campo = lambda lab, val, extra="", ayuda="": (f"<div style='width:260px'><label class='vt-muted' style='font-weight:600'>{lab}</label>"
                                                  f"<input class='vt-input' value='{val}' {extra}><div class='vt-muted' style='min-height:20px'>{ayuda}</div></div>")
    html += ("<h2>Campos</h2><div class='fila'>" + campo("Nombre del animal", "Toby")
             + campo("Con foco", "Toby", "style='border-color:var(--vt-primario);box-shadow:0 0 0 3px var(--vt-primario-suave)'")
             + campo("Con error", "", "style='border-color:var(--vt-peligro)' placeholder='Obligatorio'", "<span style='color:var(--vt-peligro)'>Ingresá el nombre del animal.</span>")
             + campo("Deshabilitado", "Toby", "disabled style='opacity:.5'") + "</div>")
    return tablero(op, tema, "S3", "Botones y campos", f"tema {tema}", html)


def s4_componentes(op, tema):
    html = ("<h2>Insignias de estado de turno</h2><div class='fila'>"
            "<span class='vt-badge vt-badge-exito'>Confirmado</span><span class='vt-badge vt-badge-aviso'>Pendiente de seña</span>"
            "<span class='vt-badge vt-badge-info'>En espera</span><span class='vt-badge vt-badge-peligro'>Ausente</span><span class='vt-badge vt-badge-peligro'>Cancelado</span></div>")
    html += "<h2>Tarjetas</h2><div class='fila'>"
    for ico, tit, det, ins, cls in [("Calendario", "Consulta general", "30 min · desde $ 1.200", "Hay horarios hoy", "exito"),
                                    ("Vacuna", "Vacunación", "20 min · desde $ 900", "Refuerzo en 20 días", "info"),
                                    ("Corazón", "Castración", "90 min · desde $ 4.500", "Pendiente de seña", "aviso")]:
        html += (f"<article class='vt-card' style='width:260px'><div class='vt-icono-caja'>{svg(ico)}</div><h3 style='margin-top:10px'>{tit}</h3>"
                 f"<p class='vt-muted'>{det}</p><span class='vt-badge vt-badge-{cls}'>{ins}</span></article>")
    html += "</div><h2>Avisos</h2><div style='display:grid;gap:10px;max-width:640px'>"
    for cls, ico, tit, txt in [("exito", "Visto", "Turno confirmado", "Te enviamos el comprobante por correo."),
                               ("aviso", "Alerta", "Falta pagar la seña", "La cirugía se confirma cuando Recepción registre el 30 %."),
                               ("error", "Alerta", "No se pudo guardar", "Revisá la conexión y probá de nuevo."),
                               ("info", "Campana", "Refuerzo de vacuna", "A Toby le toca en 20 días. Te avisamos por correo.")]:
        var = {"exito": "exito", "aviso": "aviso", "error": "peligro", "info": "info"}[cls]
        html += (f"<div style='display:flex;gap:12px;align-items:center;padding:12px 16px;border-radius:var(--vt-radio-m);background:var(--vt-{var}-suave);color:var(--vt-{var});border:1px solid var(--vt-{var})'>"
                 f"{svg(ico)}<div><strong>{tit}</strong><div style='color:var(--vt-texto)'>{txt}</div></div></div>")
    html += "</div>"
    return tablero(op, tema, "S4", "Insignias, tarjetas y avisos", f"tema {tema}", html)


def s5_iconos(op):
    duo = "duo" if op == "b" else ""
    estilo = "Contorno fino (Lucide), trazo 1,75 px, 24 px" if op == "a" else "Duotono (Phosphor), 24 px"
    html = f"<h2>Estilo: {estilo}</h2><p class='vt-muted'>Iconos de muestra dibujados para esta imagen; en la aplicación se usa la librería indicada.</p><div class='fila {duo}'>"
    for n in ICONOS:
        html += f"<div class='ico'>{svg(n)}{n}</div>"
    html += "</div><h2>Tamaños</h2><div class='fila' style='align-items:center'>"
    for px in (16, 20, 24, 32, 48):
        html += f"<div class='{duo}' style='text-align:center;font-size:12px;color:var(--vt-primario)'><svg class='vt-ico' style='width:{px}px;height:{px}px' viewBox='0 0 24 24'>{ICONOS['Huella']}</svg><br>{px}</div>"
    html += "</div><p class='vt-muted'>Regla: un icono nunca es la única señal; siempre lleva texto y no depende del color (RNF de accesibilidad).</p>"
    return tablero(op, "claro", "S5", "Iconos", "biblioteca de muestra", html)


def s6_carga(op, tema):
    html = ("<h2>Cuánto esperar y qué mostrar (propuesta de diseño, no es una medición)</h2><table>"
            "<tr><th>Espera</th><th>Qué ve la persona</th><th>Ejemplo en VITALIA</th></tr>"
            "<tr><td>0 a 100 ms</td><td>Nada: se siente instantáneo; el botón reacciona al toque (120 ms)</td><td>Abrir el catálogo de servicios</td></tr>"
            "<tr><td>100 ms a 1 s</td><td>Esqueleto con la forma del contenido</td><td>Lista de turnos, agenda del día</td></tr>"
            "<tr><td>1 a 3 s</td><td>Esqueleto + botón en «Guardando…» con rueda</td><td>Confirmar un turno, cobrar</td></tr>"
            "<tr><td>3 a 10 s</td><td>Rueda con mensaje «Sigue trabajando…» y opción de cancelar</td><td>Exportar datos personales</td></tr>"
            "<tr><td>Más de 10 s</td><td>Barra de progreso o aviso para volver más tarde</td><td>Informes largos</td></tr></table>")
    html += "<h2>Movimiento</h2><div class='fila'>"
    for k, ms, uso in [("rapido", 120, "Botones, foco"), ("normal", 220, "Tarjetas, avisos al entrar"), ("lento", 400, "Pantallas, paneles")]:
        html += f"<div class='vt-card' style='width:230px'><strong>--vt-{k}</strong><div class='vt-muted'>{ms} ms · {uso}</div><div class='barra' style='margin-top:10px'><i></i></div></div>"
    html += "</div><h2>Esqueletos y carga</h2><div class='fila'>"
    html += ("<div class='vt-card' style='width:360px'><div style='display:flex;gap:12px;align-items:center'><div class='vt-esqueleto' style='width:44px;height:44px;border-radius:50%'></div>"
             "<div style='flex:1'><div class='vt-esqueleto' style='height:14px;width:70%;margin-bottom:8px'></div><div class='vt-esqueleto' style='height:12px;width:45%'></div></div></div>"
             "<div class='vt-esqueleto' style='height:60px;margin-top:14px'></div></div>")
    html += (f"<div class='vt-card' style='width:300px'><strong>Guardando turno…</strong><div class='barra' style='margin:12px 0'><i></i></div>"
             f"<button class='vt-btn vt-btn-primario'><span class='sp'></span> Guardando…</button></div></div>")
    html += ("<h2>Avisos flotantes</h2><div style='display:grid;gap:10px;max-width:520px'>"
             f"<div class='vt-toast vt-toast-exito'>{svg('Visto')} Turno confirmado. <span class='vt-muted'>Se cierra solo en 4 s</span></div>"
             f"<div class='vt-toast vt-toast-aviso'>{svg('Alerta')} Quedan 2 horarios para hoy. <span class='vt-muted'>6 s</span></div>"
             f"<div class='vt-toast vt-toast-error' role='alert'>{svg('Alerta')} No se pudo guardar. Probá de nuevo. <span class='vt-muted'>Queda hasta cerrarlo</span></div></div>")
    return tablero(op, tema, "S6", "Tiempos de carga y respuestas", f"tema {tema}", html)


def m1_reservar(op, tema):
    duo = "duo" if op == "b" else ""
    tarjetas = ""
    for ico, tit, det in [("Calendario", "Consulta general", "30 min · desde $ 1.200"), ("Vacuna", "Vacunación", "20 min · desde $ 900"), ("Corazón", "Castración", "90 min · desde $ 4.500")]:
        tarjetas += (f"<article class='vt-card' style='display:flex;gap:12px;align-items:center;margin-bottom:12px'><div class='vt-icono-caja {duo}'>{svg(ico)}</div>"
                     f"<div><strong>{tit}</strong><div class='vt-muted'>{det}</div></div></article>")
    horas = "".join(f"<button class='vt-btn {'vt-btn-primario' if h == '10:30' else 'vt-btn-borde'}' style='min-height:44px;padding:0 14px'>{h}</button>" for h in ["09:00", "09:30", "10:30", "12:00"])
    pestanas = ""
    for i, (n, t) in enumerate([("Calendario", "Turnos"), ("Huella", "Animales"), ("Campana", "Avisos"), ("Persona", "Cuenta")]):
        color = "var(--vt-primario)" if i == 0 else "var(--vt-texto-suave)"
        pestanas += f"<div class='{duo}' style='text-align:center;color:{color}'>{svg(n)}<br>{t}</div>"
    cuerpo = (f"<div class='movil'><p class='vt-muted'>Hola, Ana</p><h1 style='font-size:var(--vt-t-xl)'>Reservá un turno</h1>"
              f"<div class='fila' style='margin:10px 0 16px;gap:8px'><span class='vt-badge vt-badge-info'>Toby</span><span class='vt-badge vt-badge-exito'>Hay horarios hoy</span></div>"
              f"{tarjetas}<h3 style='margin-top:18px'>Horarios · Dra. Ferreira</h3><div class='fila' style='gap:8px'>{horas}</div>"
              f"<button class='vt-btn vt-btn-primario' style='width:100%;justify-content:center;margin-top:20px'>Confirmar turno</button>"
              f"<nav style='position:absolute;left:0;right:0;bottom:0;display:flex;justify-content:space-around;padding:10px 0 22px;background:var(--vt-superficie);border-top:1px solid var(--vt-borde);font-size:12px'>"
              f"{pestanas}</nav></div>")
    return f'<div id="M01" data-opcion="{op}" data-tema="{tema}" style="display:inline-block">{cuerpo}</div>'


def exportar(op):
    claro, oscuro = leer_tokens(op)
    salida = RAIZ / f"opcion-{op}" / "exports"
    salida.mkdir(parents=True, exist_ok=True)
    trabajos = [
        ("S1-colores", s1_colores(op, claro, "claro"), "claro", "S1"),
        ("S1-colores-oscuro", s1_colores(op, oscuro, "oscuro"), "oscuro", "S1"),
        ("S2-tipografia-espaciado", s2_tipografia(op), "claro", "S2"),
        ("S3-botones-campos", s3_botones(op, "claro"), "claro", "S3"),
        ("S3-botones-campos-oscuro", s3_botones(op, "oscuro"), "oscuro", "S3"),
        ("S4-insignias-tarjetas-avisos", s4_componentes(op, "claro"), "claro", "S4"),
        ("S4-insignias-tarjetas-avisos-oscuro", s4_componentes(op, "oscuro"), "oscuro", "S4"),
        ("S5-iconos", s5_iconos(op), "claro", "S5"),
        ("S6-tiempos-carga-respuestas", s6_carga(op, "claro"), "claro", "S6"),
        ("S6-tiempos-carga-respuestas-oscuro", s6_carga(op, "oscuro"), "oscuro", "S6"),
        ("M01-reservar-movil", m1_reservar(op, "claro"), "claro", "M01"),
        ("M01-reservar-movil-oscuro", m1_reservar(op, "oscuro"), "oscuro", "M01"),
    ]
    with sync_playwright() as p:
        try:
            navegador = p.chromium.launch()
        except Exception:
            navegador = p.chromium.launch(executable_path="/usr/bin/google-chrome")
        pagina_ = navegador.new_page(device_scale_factor=2, viewport={"width": 1300, "height": 900})
        for nombre, cuerpo, tema, id_ in trabajos:
            archivo = salida / f"{nombre}.html"
            archivo.write_text(pagina(op, cuerpo, tema), encoding="utf-8")
            pagina_.goto(archivo.as_uri())
            pagina_.evaluate("document.fonts.ready")
            pagina_.wait_for_timeout(150)
            pagina_.locator(f"#{id_}").screenshot(path=str(salida / f"{nombre}.png"), animations="disabled")
            archivo.unlink()
            print("ok", op, nombre)
        navegador.close()
    # Variables importables en OpenPencil (op vars:set @vars.json), mismo formato que el repo del equipo hermano.
    variables = {}
    for tema, tokens in (("claro", claro), ("oscuro", oscuro)):
        for k, v in tokens.items():
            if v.startswith("#"):
                variables[f"{k}" if tema == "claro" else f"{k}-oscuro"] = {"type": "color", "value": v.upper()}
    (RAIZ / f"opcion-{op}" / "vars.json").write_text(json.dumps(variables, indent=1, ensure_ascii=False), encoding="utf-8")


if __name__ == "__main__":
    for op in (sys.argv[1:] or ["a", "b"]):
        exportar(op)
