# Catálogo público de servicios — contrato inicial de la API

Fecha: 2026-10-08

Estado: decisiones D-CAT-01 a D-CAT-04 **cerradas** (D-CAT-01 por Eric, el resto por Diego el 2026-10-29); contrato listo para implementar; no implementado.

## Propósito y límite

Primer recorrido pequeño del proyecto. En la sección A (rama `desktop`) Blazor lo consume con datos de demostración a través de un adaptador con la misma forma que este contrato; en la sección C la API lo implementa de verdad. Una persona sin cuenta abre el catálogo en Blazor, Blazor consulta la API compartida y presenta los servicios públicos con descripción y precio de referencia. La misma API servirá después a React Native. Este documento concreta **RF-VIS-01** y **HU-VIS-01**, dentro de **CU-VIS-01**; no especifica todavía la administración completa de servicios (**RF-ADM-02**), la reserva ni el cobro.

Las reglas canónicas siguen en [MATRIZ_REQUISITOS.md](../contexto-base/MATRIZ_REQUISITOS.md). Los nombres de rutas y campos siguientes son una **propuesta de contrato técnico**, no nuevos RF ni decisiones de negocio aprobadas. Las decisiones están al final.

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

`id` no debe codificar datos personales. **Decisión de Eric (2026-10-08, D-CAT-01):** el precio visible se rotula siempre «Desde». La API entrega un importe de referencia respaldado por una tarifa vigente; no envía la palabra «Desde» como sustituto del dato. **D-CAT-02/04 parcialmente cerradas:** sin importe publicable, el servicio no aparece; un servicio no reservable en línea sí puede aparecer y muestra «Consultar/llamar». El importe y los criterios de publicación están en D-CAT-02 y D-CAT-04. El ejemplo **no fija** moneda, tarifa, categoría, duración ni precio de ningún servicio. La respuesta pública no incluye costos internos, datos de clientes, agenda privada ni registros clínicos.

La API valida parámetros de paginación y responde `400` con un error legible si son inválidos. Una página válida sin resultados devuelve `200` con `items: []`; un detalle no público devuelve `404` sin revelar si existe internamente. Ante una falla del servicio, Blazor muestra un mensaje de recuperación y contacto, sin presentar datos ficticios. El formato de errores y los límites de paginación son los de D-CAT-03.

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

**Criterio de avance:** las decisiones están cerradas. RF-VIS-01 queda implementado cuando existan código API y Blazor y evidencia de CAT-01 a CAT-06. CAT-07/08 son pruebas de integración de hitos posteriores.

## Decisiones

| ID | Clasificación | Pregunta | Impacto |
|---|---|---|---|
| D-CAT-01 | Diseño/UX | **Cerrada 2026-10-08:** Eric indicó mostrar siempre el precio como «Desde». | Blazor rotula así todo importe público de referencia. |
| D-CAT-02 | Regla de negocio / diseño-UX | **Cerrada 2026-10-29 (Diego):** un servicio aparece si cumple las tres condiciones: marcado «Publicado» por Administración, vigente hoy (`vigenteDesde` ≤ hoy y `vigenteHasta` vacío o posterior) y con importe publicable (D-CAT-04). «Reservable en línea» es un indicador aparte; si está apagado se muestra «Consultar/llamar» (Eric, 2026-10-08). | Filtro de los GET y modelo de publicación. |
| D-CAT-03 | Decisión técnica | **Cerrada 2026-10-29 (Diego):** paginación `page` (desde 1) y `pageSize` (por defecto 20, máximo 50; fuera de rango → `400`). Errores en formato Problem Details (RFC 9457, `application/problem+json`) con `type`, `title`, `status`, `detail` y un `code` propio estable; sin datos internos ni clínicos. Identificadores UUID en texto. Rutas versionadas con `/api/v1/`. Se aplica a toda la API, no solo al catálogo. | Contrato OpenAPI compartido. |
| D-CAT-04 | Regla de negocio | **Cerrada 2026-10-29 (Diego):** el importe «Desde» es la menor tarifa vigente hoy entre las variantes publicadas del servicio (por especie, duración u otra variante). Sin ninguna tarifa vigente el servicio no aparece (Eric, 2026-10-08). Moneda UYU, en pesos enteros. | Cálculo de referencia sin anuncios engañosos. |

Los valores del ejemplo JSON siguen siendo ilustrativos. Ver el [tablero de decisiones](DECISIONES_PARA_DESARROLLO.md) para su orden junto con registro y consulta.
