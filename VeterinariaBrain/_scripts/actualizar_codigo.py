"""Regenera las notas de código del cerebro (VeterinariaBrain/40 Código) a partir de desktop/Veterinaria.Blazor.

Uso, desde la raíz del repositorio:
    python VeterinariaBrain/_scripts/actualizar_codigo.py

Solo reescribe las notas marcadas con `generado: true`; las notas escritas a mano no se tocan.
"""
import re
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
APP = RAIZ / "desktop" / "Veterinaria.Blazor"
CEREBRO = RAIZ / "VeterinariaBrain"
DESTINO = CEREBRO / "40 Código"
CARPETAS = {"pagina": "Páginas", "componente": "Componentes", "servicio": "Servicios", "modelo": "Modelos"}
IGNORAR = {"App", "Routes", "_Imports", "Error", "NotFound", "ReconnectModal"}


def leer(ruta):
    return ruta.read_text(encoding="utf-8-sig")


def descripcion(texto):
    """Primer comentario del archivo: // en C# o @* *@ en Razor."""
    m = re.search(r"^\s*//\s*(.+)$", texto, re.M)
    r = re.search(r"@\*\s*(.+?)\s*\*@", texto, re.S)
    candidatos = [x for x in (m and (m.start(), m.group(1)), r and (r.start(), r.group(1))) if x]
    if not candidatos:
        return ""
    return " ".join(min(candidatos)[1].split())


def tareas(texto):
    return sorted(set(re.findall(r"\b([AGBC]-\d{3})\b", texto)))


def requisitos(texto):
    return sorted(set(re.findall(r"\b(R[FN]-[A-Z]{0,4}-?\d{2}|RNF-[A-Z]{3}-\d{2}|D-[A-Z]{3}-\d{2}|CU-[A-Z]{3}-\d{2}|HU-[A-Z]{3}-\d{2})\b", texto)))


# 1. Inventario
elementos = {}
for archivo in sorted(APP.glob("Models/*.cs")):
    elementos[archivo.stem] = {"tipo": "modelo", "archivo": archivo, "texto": leer(archivo)}
for archivo in sorted(APP.glob("Services/*.cs")):
    elementos[archivo.stem] = {"tipo": "servicio", "archivo": archivo, "texto": leer(archivo)}
for archivo in sorted(APP.glob("Components/**/*.razor")):
    if archivo.stem in IGNORAR:
        continue
    texto = leer(archivo)
    tipo = "pagina" if "@page" in texto else "componente"
    elementos[archivo.stem] = {"tipo": tipo, "archivo": archivo, "texto": texto}

nombres = set(elementos)

# 2. Dependencias: servicios inyectados, componentes usados y modelos referenciados
for nombre, e in elementos.items():
    texto = e["texto"]
    usa = set(re.findall(r"@inject\s+(\w+)", texto))
    usa |= set(re.findall(r"private readonly (\w+Service)\b", texto))
    usa |= {c for c in re.findall(r"<(\w+)[\s/>]", texto) if c in nombres and elementos[c]["tipo"] == "componente"}
    usa |= {m for m in nombres if elementos[m]["tipo"] == "modelo" and re.search(rf"\b{m}\b", texto)}
    usa.discard(nombre)
    e["usa"] = sorted(u for u in usa if u in nombres)
    e["rutas"] = re.findall(r'@page "([^"]+)"', texto)

usado_por = {n: [] for n in nombres}
for nombre, e in elementos.items():
    for u in e["usa"]:
        usado_por[u].append(nombre)


def titulo(nombre):
    # Solo el nombre de la clase: es único y válido como nombre de archivo en Windows.
    return nombre


def enlace(nombre):
    return f"[[{titulo(nombre)}]]"


# 3. Borrar las notas generadas anteriores y escribir las nuevas
for carpeta in CARPETAS.values():
    for nota in (DESTINO / carpeta).glob("*.md") if (DESTINO / carpeta).exists() else []:
        if "generado: true" in nota.read_text(encoding="utf-8"):
            nota.unlink()

for nombre, e in sorted(elementos.items()):
    carpeta = DESTINO / CARPETAS[e["tipo"]]
    carpeta.mkdir(parents=True, exist_ok=True)
    relativa = e["archivo"].relative_to(RAIZ).as_posix()
    lineas = [
        "---",
        f"tipo: {e['tipo']}",
        f'fuente: "{relativa}"',
        f"tags: [codigo, {e['tipo']}]",
        "generado: true",
        "---",
        f"# {titulo(nombre)}",
        "",
        f"> Fuente: `{relativa}` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.",
        "",
    ]
    if e["rutas"]:
        lineas.append("**Ruta:** " + ", ".join(f"`{r}`" for r in e["rutas"]))
        lineas.append("")
    if descripcion(e["texto"]):
        lineas += [descripcion(e["texto"]), ""]
    if tareas(e["texto"]):
        lineas += ["**Tareas:** " + ", ".join(tareas(e["texto"])), ""]
    if requisitos(e["texto"]):
        lineas += ["**Requisitos y decisiones:** " + ", ".join(requisitos(e["texto"])), ""]
    lineas += ["Parte de [[Mapa Código]] · [[Carpeta desktop]]", ""]
    if e["usa"]:
        lineas += ["## Usa", *[f"- {enlace(u)}" for u in e["usa"]], ""]
    if usado_por[nombre]:
        lineas += ["## Usado por (si cambia, revisar estos)", *[f"- {enlace(u)}" for u in sorted(usado_por[nombre])], ""]
    (carpeta / f"{titulo(nombre)}.md").write_text("\n".join(lineas), encoding="utf-8")

# 4. Mapa del código
mapa = ["---", "tipo: mapa", "tags: [mapa]", "generado: true", "---", "# Mapa Código", "",
        "Volver a [[Inicio]]. Proyecto: [[Carpeta desktop]]. Cada nota lista qué usa y quién la usa.", ""]
for tipo, carpeta in CARPETAS.items():
    grupo = sorted(n for n, e in elementos.items() if e["tipo"] == tipo)
    filas = [f"- {enlace(n)}" + (f" · `{elementos[n]['rutas'][0]}`" if elementos[n]["rutas"] else "") for n in grupo]
    mapa += [f"## {carpeta} ({len(grupo)})", *filas, ""]
(CEREBRO / "00 Mapas" / "Mapa Código.md").write_text("\n".join(mapa), encoding="utf-8")

print(f"{len(elementos)} notas de código generadas.")
