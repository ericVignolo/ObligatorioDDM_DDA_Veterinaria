"""Regenera las notas de código del cerebro (VeterinariaBrain/40 Código), separadas por aplicación.

Uso, desde la raíz del repositorio:
    python VeterinariaBrain/_scripts/actualizar_codigo.py

Lee tres aplicaciones y escribe cada una en su carpeta:
    desktop/Veterinaria.Blazor  ->  40 Código/Desktop
    api/Veterinaria.Api         ->  40 Código/Api      (notas con sufijo «(API)»)
    mobile/                     ->  40 Código/Mobile   (notas con sufijo «(Móvil)»)

Si una aplicación todavía no existe, se saltea. Solo reescribe las notas marcadas con
`generado: true`; las notas escritas a mano (por ejemplo «Carpeta api») no se tocan.

Los sufijos evitan que Obsidian confunda, por ejemplo, el modelo `Prestacion` de Blazor con el
de la API. Cuando el mismo nombre existe en más de una aplicación, cada nota enlaza a las otras
en la sección «En las otras aplicaciones».
"""
import re
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
CEREBRO = RAIZ / "VeterinariaBrain"
DESTINO = CEREBRO / "40 Código"
IGNORAR = {"App", "Routes", "_Imports", "Error", "NotFound", "ReconnectModal", "Program"}
IGNORAR_CARPETAS = {"node_modules", "bin", "obj", ".expo", "Properties", "wwwroot", "Migrations"}

# Carpeta del cerebro según el tipo de archivo.
CARPETAS = {
    "pagina": "Páginas", "pantalla": "Pantallas", "componente": "Componentes",
    "servicio": "Servicios", "modelo": "Modelos", "controlador": "Controladores",
    "repositorio": "Repositorios", "dto": "DTOs", "datos": "Datos", "otro": "Otros",
}


def leer(ruta):
    return ruta.read_text(encoding="utf-8-sig")


def ignorado(ruta):
    return any(p in IGNORAR_CARPETAS for p in ruta.parts) or ruta.stem in IGNORAR


def descripcion(texto):
    """Primer comentario del archivo: // en C#/TS, @* *@ en Razor o /** */ en TS."""
    candidatos = []
    for patron, flags in ((r"^\s*//\s*(.+)$", re.M), (r"@\*\s*(.+?)\s*\*@", re.S), (r"/\*\*?\s*(.+?)\s*\*/", re.S)):
        m = re.search(patron, texto, flags)
        if m:
            candidatos.append((m.start(), m.group(1)))
    if not candidatos:
        return ""
    texto = min(candidatos)[1]
    return " ".join(t.strip(" *") for t in texto.split())


def tareas(texto):
    return sorted(set(re.findall(r"\b([AGBC]-\d{3})\b", texto)))


def requisitos(texto):
    return sorted(set(re.findall(r"\b(R[FN]-[A-Z]{0,4}-?\d{2}|RNF-[A-Z]{3}-\d{2}|D-[A-Z]{3}-\d{2}|CU-[A-Z]{3}-\d{2}|HU-[A-Z]{3}-\d{2})\b", texto)))


# ---------------------------------------------------------------- inventario por aplicación

def inventario_desktop(app):
    elementos = {}
    for archivo in sorted(app.glob("Models/**/*.cs")):
        elementos[archivo.stem] = ("modelo", archivo)
    for archivo in sorted(app.glob("Services/**/*.cs")):
        elementos[archivo.stem] = ("servicio", archivo)
    for archivo in sorted(app.glob("Components/**/*.razor")):
        if not ignorado(archivo):
            tipo = "pagina" if "@page" in leer(archivo) else "componente"
            elementos[archivo.stem] = (tipo, archivo)
    return elementos


def inventario_api(app):
    tipos = {"Models": "modelo", "Data": "datos", "Controllers": "controlador", "Services": "servicio",
             "Repositories": "repositorio", "DTOs": "dto", "Dtos": "dto"}
    elementos = {}
    for carpeta, tipo in tipos.items():
        for archivo in sorted(app.glob(f"{carpeta}/**/*.cs")):
            if not ignorado(archivo):
                elementos[archivo.stem] = (tipo, archivo)
    return elementos


def inventario_mobile(app):
    elementos = {}
    for archivo in sorted(list(app.rglob("*.tsx")) + list(app.rglob("*.ts")) + list(app.rglob("*.jsx")) + list(app.rglob("*.js"))):
        relativa = archivo.relative_to(app)
        if ignorado(relativa) or archivo.name.endswith((".d.ts", ".config.js", ".config.ts")):
            continue
        partes = {p.lower() for p in relativa.parts[:-1]}
        if partes & {"screens", "pantallas", "app"}:
            tipo = "pantalla"
        elif partes & {"components", "componentes"}:
            tipo = "componente"
        elif partes & {"services", "servicios", "api"}:
            tipo = "servicio"
        elif partes & {"models", "modelos", "types", "tipos"}:
            tipo = "modelo"
        else:
            tipo = "otro"
        nombre = archivo.stem
        if nombre in ("index", "_layout"):  # rutas de Expo Router: usar la carpeta para no repetir
            nombre = f"{relativa.parent.as_posix().replace('/', '-') or 'raiz'}-{nombre}"
        elementos[nombre] = (tipo, archivo)
    return elementos


APLICACIONES = [
    # clave, carpeta del cerebro, código, sufijo de las notas, nota de la carpeta, inventario
    ("desktop", "Desktop", RAIZ / "desktop" / "Veterinaria.Blazor", "", "Carpeta desktop", inventario_desktop),
    ("api", "Api", RAIZ / "api" / "Veterinaria.Api", " (API)", "Carpeta api", inventario_api),
    ("mobile", "Mobile", RAIZ / "mobile", " (Móvil)", "Carpeta mobile", inventario_mobile),
]


# ---------------------------------------------------------------- dependencias

def dependencias(nombre, texto, elementos):
    usa = set()
    usa |= set(re.findall(r"@inject\s+(\w+)", texto))
    usa |= set(re.findall(r"private readonly (\w+)\b", texto))
    usa |= set(re.findall(r"""import\s+(?:[\w{},\s*]+\s+from\s+)?['"][^'"]*/(\w+)['"]""", texto))
    usa |= {c for c in re.findall(r"<(\w+)[\s/>]", texto) if c in elementos and elementos[c][0] in ("componente", "pantalla")}
    usa |= {m for m, (tipo, _) in elementos.items() if tipo in ("modelo", "dto") and re.search(rf"\b{re.escape(m)}\b", texto)}
    usa.discard(nombre)
    return sorted(u for u in usa if u in elementos)


# ---------------------------------------------------------------- escritura

def borrar_generadas():
    for app in APLICACIONES:
        carpeta_app = DESTINO / app[1]
        if carpeta_app.exists():
            for nota in carpeta_app.rglob("*.md"):
                if "generado: true" in nota.read_text(encoding="utf-8"):
                    nota.unlink()


def main():
    borrar_generadas()
    todas = {}  # clave -> {nombre: datos}
    for clave, carpeta_app, codigo, sufijo, nota_carpeta, inventario in APLICACIONES:
        if not codigo.exists():
            todas[clave] = {}
            continue
        crudo = inventario(codigo)
        elementos = {}
        for nombre, (tipo, archivo) in crudo.items():
            texto = leer(archivo)
            elementos[nombre] = {"tipo": tipo, "archivo": archivo, "texto": texto,
                                 "rutas": re.findall(r'@page "([^"]+)"', texto) +
                                          re.findall(r'\[(?:Route|Http\w+)\("([^"]+)"\)\]', texto)}
        for nombre, e in elementos.items():
            e["usa"] = dependencias(nombre, e["texto"], crudo)
        todas[clave] = elementos

    def titulo(clave, nombre):
        return nombre + next(a[3] for a in APLICACIONES if a[0] == clave)

    total = 0
    for clave, carpeta_app, codigo, sufijo, nota_carpeta, _ in APLICACIONES:
        elementos = todas[clave]
        usado_por = {n: [] for n in elementos}
        for nombre, e in elementos.items():
            for u in e["usa"]:
                usado_por[u].append(nombre)
        for nombre, e in sorted(elementos.items()):
            carpeta = DESTINO / carpeta_app / CARPETAS[e["tipo"]]
            carpeta.mkdir(parents=True, exist_ok=True)
            relativa = e["archivo"].relative_to(RAIZ).as_posix()
            t = titulo(clave, nombre)
            lineas = ["---", f"tipo: {e['tipo']}", f"app: {clave}", f'fuente: "{relativa}"',
                      f"tags: [codigo, {e['tipo']}, {clave}]", "generado: true", "---", f"# {t}", "",
                      f"> Fuente: `{relativa}` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.", ""]
            if e["rutas"]:
                lineas += ["**Ruta:** " + ", ".join(f"`{r}`" for r in e["rutas"]), ""]
            if descripcion(e["texto"]):
                lineas += [descripcion(e["texto"]), ""]
            if tareas(e["texto"]):
                lineas += ["**Tareas:** " + ", ".join(tareas(e["texto"])), ""]
            if requisitos(e["texto"]):
                lineas += ["**Requisitos y decisiones:** " + ", ".join(requisitos(e["texto"])), ""]
            lineas += [f"Parte de [[Mapa Código]] · [[{nota_carpeta}]]", ""]
            if e["usa"]:
                lineas += ["## Usa", *[f"- [[{titulo(clave, u)}]]" for u in e["usa"]], ""]
            if usado_por[nombre]:
                lineas += ["## Usado por (si cambia, revisar estos)",
                           *[f"- [[{titulo(clave, u)}]]" for u in sorted(usado_por[nombre])], ""]
            otras = [titulo(c, nombre) for c in todas if c != clave and nombre in todas[c]]
            if otras:
                lineas += ["## En las otras aplicaciones", *[f"- [[{o}]]" for o in otras], ""]
            (carpeta / f"{t}.md").write_text("\n".join(lineas), encoding="utf-8")
            total += 1

    # Mapa del código, con una sección por aplicación
    mapa = ["---", "tipo: mapa", "tags: [mapa]", "generado: true", "---", "# Mapa Código", "",
            "Volver a [[Inicio]]. Una sección por aplicación; cada nota lista qué usa, quién la usa "
            "y, si existe, su equivalente en las otras aplicaciones.", ""]
    for clave, carpeta_app, codigo, sufijo, nota_carpeta, _ in APLICACIONES:
        elementos = todas[clave]
        mapa += [f"## {carpeta_app} — [[{nota_carpeta}]]", ""]
        if not elementos:
            mapa += ["Todavía sin código.", ""]
            continue
        for tipo, nombre_carpeta in CARPETAS.items():
            grupo = sorted(n for n, e in elementos.items() if e["tipo"] == tipo)
            if not grupo:
                continue
            filas = [f"- [[{titulo(clave, n)}]]" + (f" · `{elementos[n]['rutas'][0]}`" if elementos[n]["rutas"] else "")
                     for n in grupo]
            mapa += [f"### {nombre_carpeta} ({len(grupo)})", *filas, ""]
    (CEREBRO / "00 Mapas" / "Mapa Código.md").write_text("\n".join(mapa), encoding="utf-8")

    print(f"{total} notas de código generadas.")


if __name__ == "__main__":
    main()
