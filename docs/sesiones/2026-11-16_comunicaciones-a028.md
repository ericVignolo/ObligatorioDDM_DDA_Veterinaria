# Sesión 2026-11-16 (6) — Comunicaciones y derechos, A-028 (Eric)

Rama: `eric/sesion-2026-11-16-comunicaciones-a028` desde `desarrollo`. PR #33 contra `desarrollo`.

## Qué se hizo

- Eric aprobó la parte 2 de A-027: PR #32 mezclado. A-027 quedó completa.
- **Comunicaciones** (`ComunicacionesService`): bandeja de salida simulada con estados Pendiente, Enviado y Fallido, tres intentos y reintento manual (RNF-CON-04). Cinco tipos: resumen clínico, comprobante, aviso de la clínica (esenciales) y aviso de refuerzo y alerta sanitaria (opcionales).
- **Preferencias** por tipo y por cliente: correo, solo en la app o no recibir; lo esencial no se apaga y lo opcional no viene premarcado (RN-43, RF-CLI-18, RF-CLI-20).
- **Resumen clínico:** al firmar la consulta se genera el mensaje al responsable (RF-CLI-10). La firma sigue funcionando aunque el cliente no lo reciba.
- **Avisos de refuerzo** (`AvisosRefuerzoService`): idempotentes, sin duplicar el mismo refuerzo (RF-CLI-07, RNF-CON-10).
- **Reenvío bajo pedido** (RF-REC-07): desde la bandeja y desde el comprobante («Enviar al cliente»).
- **Derechos del titular** (`DerechosService`): exportar en JSON, pedir la eliminación, cancelarla, y que el Administrador la apruebe (anonimiza la cuenta y conserva animales, historia, cargos y consentimientos) o la rechace con motivo (RF-CLI-19, RNF-LEG-02). Política de conservación visible para el cliente y el Administrador (RNF-LEG-06).
- Pantallas: `/panel/comunicaciones`, `/panel/admin/derechos`, y en Mi cuenta «Mis comunicaciones» y «Mis datos personales».
- Bitácora: entidades nuevas «Comunicaciones» y «Datos personales».
- Especificación: `docs/analisis/ESPECIFICACION_COMUNICACIONES.md` (CM-01..16, DR-01..09, AC-01..12).

## Decisiones

| Id | Decisión |
|---|---|
| D-COM-01 | Sin correo real en la sección A: bandeja en memoria. El envío es de la API y necesita tablas de cola, preferencias y solicitudes. Eric preguntó si hacía falta la base de datos: no para esta tarea; sí para el envío real. |
| D-COM-03 | Aviso de refuerzo por correo por defecto; alerta sanitaria apagada hasta que el cliente la active. **A confirmar por Eric.** |
| D-DER-02 | Eliminar = anonimizar; lo clínico, los cargos y los consentimientos se conservan sin identificar al titular. |
| D-DER-03 | Plazos legales de conservación «a confirmar»: no se inventan. |

## Archivos tocados

Modelos: `Comunicacion`, `PreferenciaComunicacion`, `TipoComunicacion`, `SolicitudDerecho`, `PoliticaConservacion`; `Cuenta` (Eliminada, FechaEliminacion).
Servicios: `ComunicacionesService`, `AvisosRefuerzoService`, `DerechosService` (nuevos); `ConsultaService` (genera el resumen), `CuentaService` (Todas, Anonimizar), `BitacoraService`.
Páginas y componentes: `Comunicaciones`, `AdminDerechos`, `MisComunicaciones`, `MisDerechos`, `MiCuenta`, `Comprobante`, `PanelInicio`.
Pruebas: `ComunicacionesTests`, `DerechosTests`, `CajaNegra/ComunicacionesDerechosCajaNegraTests`.

## Cómo probarlo

1. **Cliente:** ingresar como cliente → Mi cuenta → «Mis comunicaciones»: cambiar el canal de cada tipo (lo esencial no deja «No recibir»; el aviso de refuerzo viene por correo; la alerta sanitaria, apagada).
2. **Resumen:** `ferreira@vitalia.uy` firma una consulta de un animal de ese cliente. En el cliente, «Mis comunicaciones» muestra el resumen; en `recepcion@vitalia.uy` → Comunicaciones aparece en la bandeja.
3. **Reintentos:** en Comunicaciones activar «Simular falla», emitir un aviso de la clínica, pulsar «Procesar cola» dos veces (queda Fallido), desactivar la falla y «Reintentar».
4. **Reenvío:** «Reenviar» sobre cualquier mensaje; y en un comprobante «Enviar al cliente».
5. **Refuerzos:** un cliente con «Cargar ejemplos» → Comunicaciones → «Generar avisos de refuerzo» (dos veces: la segunda no duplica).
6. **Alerta sanitaria** (admin): el cliente la activa, el admin la emite filtrando por especie.
7. **Derechos:** en Mi cuenta → «Mis datos personales»: preparar y descargar el JSON; pedir la eliminación (con un cargo pendiente o un turno próximo, lo rechaza explicando por qué); como `admin@vitalia.uy` → Datos personales → aprobar. El cliente ya no puede ingresar y sus animales siguen en la historia.

## Qué falta

- [ ] Eric: probar con F5 y confirmar D-COM-03.
- [ ] A-029 accesibilidad y adaptación de escritorio.
- [ ] Decidir proveedor de correo antes de integrar con la API.
