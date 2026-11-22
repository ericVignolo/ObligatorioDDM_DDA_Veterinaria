# Sesión 2026-11-22 — Cierre de la API (Diego, con Claude)

## Antes de empezar

Eric mezcló los PR #39 a #51 en `desarrollo` y en `main` (21/11). Esta sesión sale de `desarrollo`.

Plan aprobado por Diego, con cada parte en su rama encadenada y su PR:
1. derechos del titular;
2. tareas automáticas;
3. correo real;
4. tiempo real;
5. verificación en dos pasos.

Decisiones de Diego:
- **dos pasos con app autenticadora** (TOTP);
- **correo real** listo para configurar, pero sin datos cargados: mientras tanto sigue el envío de prueba.

## Parte 1: Derechos del titular

Rama `diego/sesion-2026-11-22-derechos`. PR #52. No cambia la base.

- **Cliente:**
  - ve las políticas de conservación;
  - descarga sus datos en JSON, y el pedido queda registrado;
  - ve qué falta resolver para eliminar la cuenta;
  - pide la eliminación (con confirmación) y la puede cancelar.
- **Administrador:**
  - aprobar **anonimiza** la cuenta: datos personales neutros, se borran comunicaciones, preferencias y códigos, y se cierran las sesiones. Animales, historia clínica, cargos y consentimientos se conservan;
  - rechazar pide motivo;
  - antes de aprobar se vuelven a revisar los pendientes.
- **Archivos:**
  - `Services/IDerechosService.cs` y `DerechosService.cs`;
  - `Repositories/IDerechosRepository.cs` y `DerechosRepository.cs`;
  - `Controllers/DerechosController.cs`;
  - `DTOs/DerechosDtos.cs`;
  - `Program.cs`;
  - sección 18 del `.http`.
- **Pruebas:** `ApiDerechosTests`, 5 nuevas. CI en verde.
