# Instrucciones persistentes del proyecto

Antes de planificar, diseñar o implementar, leer:

1. `docs/contexto-base/CONTEXTO_BASE.md`.
2. `docs/contexto-base/MATRIZ_REQUISITOS.md` cuando el trabajo afecte funciones, reglas, validaciones, seguridad, datos o pruebas.
3. `docs/contexto-base/ANALISIS_COMPLEMENTARIO.md` cuando el trabajo afecte alcance, fases, arquitectura, UML, riesgos o defensa.
4. `docs/analisis/ESPECIFICACION_ALERTAS_ASISTENCIA.md` cuando el trabajo afecte alertas de asistencia, gravedad, ubicación, asignación, seguimiento, costos o atención fuera de horario.

## Reglas no negociables vigentes

- Mantener una única API ASP.NET Core en C# como autoridad de datos, autenticación, autorización y reglas de negocio.
- Blazor y React Native consumen esa API; ningún cliente accede directamente a la base de datos.
- La interfaz principal para computadora se desarrolla con Blazor/C#.
- La aplicación móvil se desarrolla con React Native y su diseño se prepara en Figma.
- Las reglas críticas pertenecen al backend/dominio y, cuando corresponda, se refuerzan con transacciones o restricciones de base de datos.
- Clasificar cada requisito nuevo como: decisión técnica, requerimiento funcional, regla de negocio, requerimiento no funcional, diseño/UX, pseudocódigo o consentimiento/alerta.
- Aplicar la redacción y clasificación de RF, RN-01 a RN-59 y RNF documentada en `MATRIZ_REQUISITOS.md`. No sustituirla por resúmenes informales.
- La navegación pública sin autenticación está permitida; las acciones protegidas deben solicitar inicio de sesión o registro y luego retomar la intención original del usuario.
- Un recorrido se diseña en alta fidelidad o se programa cuando sus decisiones, estados y pseudocódigo están cerrados (puerta H0 por recorrido, DT-ORG-03 en `CONTEXTO_BASE.md`).
- Las advertencias comunes (autoridad de la API, límites de los prototipos, estados de un requisito) se escriben solo en la sección 3.1 de `CONTEXTO_BASE.md`; los demás documentos la enlazan.
- Cualquier cambio de arquitectura, tecnología o regla obligatoria debe actualizar primero la documentación de contexto.

## Responsables y autorizaciones (2026-11-03)

- **Diego y Eric** son los dos responsables principales del proyecto, con **la misma autoridad** y sin jerarquía entre ellos.
- Cualquiera de los dos puede pedir tareas, modificar funcionalidades, tomar decisiones y mezclar sus PR. Ninguno necesita autorización del otro para tareas normales de desarrollo.
- Antes de modificaciones importantes, cambios de arquitectura, borrar archivos, instalar dependencias o cualquier acción que pueda afectar al proyecto, Claude explica qué hará y pide autorización a **quien esté trabajando en esa sesión**.
- Claude respeta las instrucciones, la documentación y las decisiones ya acordadas. No cambia tecnologías, estructura, convenciones del docente ni funcionalidades acordadas por iniciativa propia.
- Claude explica los cambios que hizo y comunica cualquier problema que encuentre.
- Ambos mantienen el proyecto sincronizado mediante GitHub y coordinan los cambios para evitar conflictos.
- Estas reglas valen igual cuando trabaja Diego y cuando trabaja Eric.

## Sesiones de trabajo

- Al **iniciar** una sesión, leer la última bitácora de `docs/sesiones/` y su lista de pendientes.
- Al **cerrar** una sesión, crear `docs/sesiones/AAAA-MM-DD_tema-breve.md` con: qué se hizo, archivos tocados, decisiones, cómo probarlo y qué falta.
- **Una rama por sesión** (`<nombre>/sesion-AAAA-MM-DD`), que sale de la rama de la sección; todo lo de esa sesión va ahí y se abre **un solo PR** al final (DT-ORG-06). Quien trabajó la prueba y mezcla su propio PR; el otro puede revisarlo después (DT-ORG-05).
- Claude no puede compilar: el código llega sin probar y se prueba con F5 antes de mezclar. Por eso nunca se trabaja directo sobre `desktop`.
- **Cerebro de Obsidian (`VeterinariaBrain/`):** antes de cambiar código, revisar en `VeterinariaBrain/40 Código/<Desktop|Api|Mobile>/` la sección «Usado por» de cada archivo que se va a tocar, para prever qué más se ve afectado. Al cerrar la sesión, regenerar las notas de código con `python VeterinariaBrain/_scripts/actualizar_codigo.py` y actualizar a mano las notas de documentos, sesiones y mapas que correspondan.

## Calidad: compilación, pruebas y revisión (2026-11-09)

- **CI:** GitHub Actions («Compilar y probar», `.github/workflows/ci.yml`) compila y corre `tests/Veterinaria.Tests` en cada PR y en cada push a `desktop`/`main`. Claude lee el resultado con `gh api` y corrige lo que falle **antes** de pedir la prueba con F5.
- Un PR con el CI en rojo no se mezcla.
- Cada regla nueva o cambiada del backend agrega o ajusta su prueba xUnit en el mismo PR (`tests/README.md`).
- **Agente revisor:** cuando quien trabaja en la sesión lo pide (por ejemplo «revisá el PR»), Claude lanza un agente revisor independiente que compara el cambio con `MATRIZ_REQUISITOS.md`, las decisiones y las convenciones del docente, y busca errores típicos de Razor. Sus hallazgos se corrigen o se informan en el PR.
- El CI y el revisor no reemplazan la prueba con F5 de quien trabajó.

## Convenciones de código (estilo del docente)

- Blazor Web App, .NET 10, interactividad Server con ubicación Global.
- Carpetas: `Models/` (clases de datos), `Services/` (lógica y datos), `Components/` (componentes reutilizables como `PrestacionCard`), `Components/Pages/` (páginas).
- Modelos: clases comunes con `{ get; set; }`, valores por defecto (`= ""`) y nombres en español.
- Datos sin API (sección A): un `XxxService` con una `List<>` privada, registrado con `AddScoped<XxxService>()` e inyectado con `@inject`. Sin interfaces. En la sección C se reemplaza por `XxxApiService` con `AddHttpClient`.
- Páginas: `@foreach` con componentes que reciben `[Parameter]`; detalle con `@page "/ruta/{Id:int}"` y `OnParametersSet`; acciones desde un componente con `EventCallback`.
- Formularios: `EditForm` con `DataAnnotationsValidator` y `ValidationMessage`; reglas con `[Required]`, `[Range]`, etc. en el modelo.
- Textos visibles en español rioplatense; cultura `es-UY`.

## Punto de continuación

Actualizado: 2026-11-09.

- **Entrega: jueves 4 de diciembre de 2026** (DT-ORG-07).
- **Primero, si trabaja Diego:** (1) decidir el proveedor MySQL de DT-BD-02 — Pomelo solo soporta EF Core 9, sin soporte desde el 2026-11-10; el profesor usa `MySql.EntityFrameworkCore` (Oracle) con EF Core 10; (2) crear la base de datos (le corresponde a Diego, acordado el 2026-11-09); (3) aplicar el disparador del Plan B con la fecha de entrega (R1 y R2 en el límite). Mensaje completo en `docs/analisis/MENSAJE_PARA_DIEGO.md`.
- Fase: construcción, sección A (DT-ORG-04, 2026-10-31).

- Equipo: Eric y Diego, con apoyo de asistentes de IA.
- Decisiones del primer recorrido (catálogo → registro → reserva → consulta) cerradas por Diego: ver `docs/analisis/DECISIONES_PARA_DESARROLLO.md`.
- Base de datos: MySQL 8.4 LTS; proveedor de EF Core en revisión (DT-BD-02); agenda protegida con `SELECT … FOR UPDATE` (DT-BD-01 y DT-BD-02).
- Ramas: secciones A (`desktop`), B (`desktop-mobile`) y C (`desktop-mobile-api`), encadenadas, con merge a `main` al aprobar cada una; trabajo diario en ramas cortas con PR (DT-ORG-02).
- Plan B de alcance listo, sin recortes aplicados: `docs/analisis/PLAN_B_ALCANCE.md`.
- Estado de la sección A y próximos pasos: ver la última bitácora en `docs/sesiones/`.
- Cobro, internación, avisos de refuerzo, parámetros de asistencia y proveedores: decididos el 2026-10-31 (`docs/analisis/DECISIONES_ERIC_2026-10-31.md`). Cada módulo los incorpora a su especificación al abordarse.
