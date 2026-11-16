# Pruebas automáticas

`Veterinaria.Tests` es un proyecto xUnit que prueba las **reglas de negocio** de la sección A
directamente sobre los servicios (sin abrir el navegador). GitHub las corre solo en cada PR
y en cada push a `desarrollo` o `main` (workflow «Compilar y probar», `.github/workflows/ci.yml`).

## Cómo correrlas en la PC

- **Visual Studio:** abrir `desktop/Veterinaria.Blazor/Veterinaria.Blazor.slnx` → menú *Prueba* →
  *Explorador de pruebas* → *Ejecutar todo*.
- **Terminal:** `dotnet test tests/Veterinaria.Tests/Veterinaria.Tests.csproj`

## Qué cubre cada archivo

| Archivo | Tareas | Reglas |
|---|---|---|
| `ValidacionDatosTests` | A-006 | Cédula con dígito verificador, pasaporte, teléfono en E.164, aceptaciones obligatorias (RN-43) |
| `CuentaYSesionTests` | A-006 a A-008, A-010 | Correo y documento únicos, 5 intentos por código, 3 reenvíos por hora, sin verificar no entra (RN-38), recuperación, mensaje genérico, roles del personal |
| `AnimalesYCatalogoTests` | A-003, A-004, A-012, A-013 | Lo no publicado no aparece, profesionales por servicio y especie, microchip único (RN-41), solo animales propios (RN-18) |
| `AgendaTests` | A-014 a A-017 | Horarios cada 15 min, turno bloquea su duración, descansos, ausencias, feriados, 60 días, 24 h para gestionar (RN-06), cancelar libera, reprogramar conserva el número, acciones de Recepción |
| `ConsultaTests` | A-019, A-020 | Una consulta por turno, solo el veterinario asignado, versión, firma con contraseña y matrícula (RN-13), diagnóstico obligatorio (RN-20), firmada inmutable y enmiendas (RN-12), historia sin borradores, lecturas auditadas (RN-40, P-RC-05) |
| `VacunaTests` | A-021 | Refuerzo calculado por el producto (RN-19), lote vencido no se aplica (RN-15), producto no aplicable a la especie, un producto por consulta, solo el veterinario de la consulta abierta, el carnet cuenta solo consultas firmadas, estados vigente / refuerzo próximo / vencida, alertas solo de los animales propios (RN-18) |
| `InventarioTests` | A-022 | Baja automática de lotes vencidos (RN-29), FEFO (RN-27), sin stock no se registra ni cambia nada (RN-26), apartar al registrar y descontar al firmar, lo apartado no lo usa otra consulta, alerta al cruzar el mínimo (RN-28), tablero, compras, ajustes y bajas solo del Administrador con motivo (RN-31), movimientos con origen (RN-30) |
| `InternacionTests` | A-024 | Solo Recepción admite y anula, una internación abierta por animal (RN-21), grupos no, ingreso clínico solo del Veterinario, box exclusivo (RN-22) y del tipo de la especie, notas internas vs. visibles (RN-23), rectificar sin borrar (RN-24), alta con evolución e indicaciones que libera el box (RN-25), el cliente ve solo lo suyo (RN-18) |
| `ConsentimientoTests` | A-025 | La castración pide consentimiento, firma con casilla y nombre de la cuenta, huella y versión, solo el responsable (RN-18), rechazo y nueva firma sin borrar, versión nueva invalida lo firmado, sin consentimiento la cirugía no pasa a En curso (RN-10) |
| `CobroTests` | A-023 | Cargo solo al firmar y sin duplicarse (RN-32), precio de la prestación (RN-33), cobro solo de Recepción, total y parcial, sin superar el saldo ni cobrar un cargo pagado, medios válidos y número de operación en transferencias, comprobantes correlativos, anulación solo del Administrador con motivo y nota de crédito sin modificar el comprobante (RN-34), totales sin anulados, cada cliente ve solo sus cargos (RN-18) |

**Caja negra (`CajaNegra/`):** pruebas que escribe el agente probador (`.claude/agents/probador.md`) leyendo solo la especificación, los modelos y las firmas públicas, sin ver la implementación. Si una falla, se revisa si el error está en el código o en la especificación.

`Fabrica.cs` arma los servicios igual que `Program.cs` y tiene ayudas (cliente verificado,
animal nuevo, primer día en que atiende un profesional).

## Al agregar una regla nueva

Cada tarea que agrega o cambia una regla del backend agrega o ajusta su prueba en el mismo PR.
Las pruebas no reemplazan la prueba con F5: verifican reglas, no cómo se ve la pantalla.
