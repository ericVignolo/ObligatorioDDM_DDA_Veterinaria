# Catálogo público de servicios — contrato inicial de la API

Fecha: 2026-10-08

Estado: borrador; Diego cierra las decisiones pendientes y registra su justificación; no implementado.

## Propósito y límite

Primer recorrido pequeño de la rama `feature/escritorio-api`: una persona sin cuenta abre el catálogo en Blazor, Blazor consulta la API compartida y presenta los servicios públicos con descripción y precio de referencia. La misma API servirá después a React Native. Este documento concreta **RF-VIS-01** y **HU-VIS-01**, dentro de **CU-VIS-01**; no especifica todavía la administración completa de servicios (**RF-ADM-02**), la reserva ni el cobro.

Las reglas canónicas siguen en [MATRIZ_REQUISITOS.md](../contexto-base/MATRIZ_REQUISITOS.md). Los nombres de rutas y campos siguientes son una **propuesta de contrato técnico**, no nuevos RF ni decisiones de negocio aprobadas. Los puntos abiertos están identificados al final.

## Recorrido visible

1. El visitante abre «Servicios» sin autenticarse.
2. Blazor solicita la primera página a `GET /api/v1/servicios?page=1&pageSize=20` por HTTPS. Blazor no consulta la base de datos.
3. La API devuelve únicamente servicios publicados y vigentes para el público **con un importe de referencia publicable**, ordenados de forma estable. Un servicio visible puede no admitir reserva en línea. La lista se pagina en el servidor.
4. Blazor muestra nombre, descripción breve y el precio de referencia precedido por «Desde». Si no admite reserva en línea, ofrece «Consultar/llamar» en lugar de la acción de reservar. Un estado vacío ofrece una vía de contacto. La presentación no promete que el importe sea el cargo final.
5. Si el visitante intenta reservar desde un servicio, se inicia el flujo de acceso/registro y se conserva la intención no sensible para retomarla tras la verificación; la reserva no se ejecuta desde el catálogo. Ese recorrido pertenece a `CU-VIS-03` y `CU-CLI-02`.

## Contrato HTTP propuesto para lectura

| Operación | Acceso | Entrada | Éxito |
|---|---|---|---|
| `GET /api/v1/servicios` | Público | `page` y `pageSize`; paginación servidor | `200` con `items`, `page`, `pageSize`, `totalItems` |
| `GET /api/v1/servicios/{id}` | Público | Identificador opaco del servicio | `200` con el detalle público; `404` si no existe o no es público/vigente |

Ejemplo **ilustrativo de forma, no de datos reales ni precios aprobados**:

```json
{
  "items": [
    {
      "id": "servicio-ejemplo",
      "nombre": "Servicio de ejemplo",
      "descripcion": "Descripción pública del servicio.",
      "reservableEnLinea": false,
      "precioReferencia": {
        "importe": 1000,
        "moneda": "UYU"
      }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1
}
```

`id` no debe codificar datos personales. **Decisión de Eric (2026-10-08, D-CAT-01):** el precio visible se rotula siempre «Desde». La API entrega un importe de referencia respaldado por una tarifa vigente; no envía la palabra «Desde» como sustituto del dato. **D-CAT-02/04 parcialmente cerradas:** sin importe publicable, el servicio no aparece; un servicio no reservable en línea sí puede aparecer y muestra «Consultar/llamar». Sigue abierto el cálculo exacto del importe base y los demás criterios de publicación. El ejemplo **no fija** moneda, tarifa, categoría, duración ni precio de ningún servicio. La respuesta pública no incluye costos internos, datos de clientes, agenda privada ni registros clínicos.

La API valida parámetros de paginación y responde `400` con un error legible si son inválidos. Una página válida sin resultados devuelve `200` con `items: []`; un detalle no público devuelve `404` sin revelar si existe internamente. Ante una falla del servicio, Blazor muestra un mensaje de recuperación y contacto, sin presentar datos ficticios. El formato uniforme de errores y los límites exactos de paginación se fijarán al definir las convenciones comunes de la API.

## Reglas que debe preservar la implementación

- La lectura es anónima. La publicación/edición de servicios y tarifas será una operación administrativa autorizada por la API; **no forma parte de estos dos GET**.
- El precio del catálogo es orientativo. El cargo real se calcula con la tarifa vigente en la fecha de la prestación (**RN-33**), no copiando ciegamente el precio que el visitante vio al reservar.
- Los datos públicos deben tener origen verificable; no publicar ofertas, precios ni afirmaciones inventadas (**RN-45**).
- El modelo de datos debe permitir posteriormente administrar servicios, tipos de cita, duraciones y tarifas con fecha de vigencia (**RF-ADM-02**). No se debe sobrescribir el historial de tarifas como si fuera un único precio actual.
- El contrato se documenta y versiona (**RNF-MAN-05**); listados extensos se paginan en el servidor (**RNF-REN-03**). La API aplica reglas; Blazor y React Native son consumidores (**RT-01 a RT-09**).

## Criterios de aceptación y pruebas para Diego

| ID | Comprobación | Resultado esperado |
|---|---|---|
| CAT-01 | Abrir catálogo sin sesión | API `200`; Blazor muestra servicios públicos sin pedir acceso. |
| CAT-02 | Servicio despublicado, inactivo o fuera de vigencia | No aparece en lista ni en detalle público. |
| CAT-02A | Servicio sin importe de referencia publicable | No aparece en lista ni en detalle público. |
| CAT-02B | Servicio publicado no reservable en línea | Aparece con precio «Desde» y opción «Consultar/llamar»; no ofrece reserva directa. |
| CAT-03 | Consultar identificador inexistente o no público | `404`, sin revelar datos internos. |
| CAT-04 | Lista vacía | API `200` con `items: []`; Blazor muestra estado vacío y contacto. |
| CAT-05 | Listado mayor que una página | Paginación estable y `totalItems` coherente; no se carga toda la tabla para paginar en Blazor. |
| CAT-06 | Parámetros de paginación inválidos | `400` con explicación comprensible. |
| CAT-07 | Tarifa de referencia distinta de la tarifa al prestar | Catálogo no determina el cargo; la prueba del cargo se completará en H2 según RN-33. |
| CAT-08 | Intentar reservar desde el catálogo | Acceso/registro y reanudación de intención; no se crea turno anónimo. Se verificará con H1. |

**Criterio de avance:** este documento permite discutir y preparar el endpoint público, pero no marca RF-VIS-01 como implementado. Para cerrarlo faltan las decisiones D-CAT-02 a 04, contrato final, código API y Blazor, y evidencia de CAT-01 a CAT-06. CAT-07/08 son pruebas de integración de hitos posteriores.

## Decisiones pendientes

| ID | Clasificación | Pregunta | Impacto |
|---|---|---|---|
| D-CAT-01 | Diseño/UX | **Cerrada 2026-10-08:** Eric indicó mostrar siempre el precio como «Desde». | Blazor rotula así todo importe público de referencia. |
| D-CAT-02 | Regla de negocio / diseño-UX | **Parcial 2026-10-08:** Eric permite servicios visibles sin reserva en línea, con «Consultar/llamar». Falta definir publicación y vigencia concretas. | Filtro de los GET y modelo de publicación. |
| D-CAT-03 | Decisión técnica | Convención común de paginación, errores y formato definitivo de identificadores. | Contrato OpenAPI compartido. |
| D-CAT-04 | Regla de negocio | **Parcial 2026-10-08:** Eric indicó ocultar servicios sin importe publicable. Falta definir de qué tarifa vigente sale el importe «Desde» si hay variantes. | Cálculo de referencia sin anuncios engañosos. |

No cerrar estas decisiones mediante valores de ejemplo. Ver el [tablero de decisiones](DECISIONES_PARA_DESARROLLO.md) para su orden junto con registro y consulta.
