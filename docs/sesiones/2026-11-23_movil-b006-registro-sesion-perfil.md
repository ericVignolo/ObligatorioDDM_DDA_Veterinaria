# Sesión 2026-11-23 — Móvil: registro, sesión y perfil (B-006) y dos decisiones de Eric

Rama: `eric/sesion-2026-11-23-movil`, desde `eric/sesion-2026-11-21-movil-b001` (B-001, B-004 y B-005, todavía sin mezclar en `desarrollo`).

## Decisiones de Eric (que estaban delegadas en Diego)
- **DT-DIS-01:** sistema de diseño **Opción A** (`design/vitalia/opcion-a/`). La app móvil ya la tenía activa; A-032 la aplica a Blazor.
- **D-CAT-05:** «Revisión de ovinos y bovinos» la atiende el Dr. Martín Silveira y «Rehabilitación equina» la Dra. Valentina Acosta. Aplicado en los datos de demostración de Blazor y de la app. **Falta en la API:** la semilla `ProfesionalPrestacion` de `AppDbContext` (Silveira–4 y Acosta–5) necesita una migración nueva; corresponde a la rama de la API.

## Qué se hizo (B-006)
- **Ingreso real** (`app/(tabs)/ingresar.tsx`): formulario de correo y contraseña con los mensajes de Blazor; no distingue correo inexistente de contraseña incorrecta. Una cuenta sin verificar va a la verificación. Recepción y Administrador reciben el aviso de usar escritorio. Los botones de prueba ahora solo completan el formulario.
- **Crear cuenta** (`app/registro.tsx`): los siete datos obligatorios de D-REG-01 (con cédula validada por dígito verificador y teléfono normalizado a E.164) y las dos aceptaciones separadas y sin marcar de D-REG-04, con enlaces a Términos y Privacidad.
- **Verificar correo** (`app/verificar.tsx`): código de 6 números, 5 intentos, 3 reenvíos por hora y vigencia de 15 minutos (D-REG-02). Al verificar, ingresa sin pedir la contraseña y retoma la acción pendiente.
- **Recuperar contraseña** (`app/recuperar.tsx`): respuesta idéntica exista o no la cuenta (D-REG-03, REG-08); pide el código y la contraseña nueva en la misma pantalla.
- **Mi cuenta** (`app/mi-cuenta.tsx`, desde Perfil, solo Cliente): edición de datos y cambio de contraseña (cierra la sesión, como Blazor).
- **Acción pendiente** (`services/intencionService.ts`): se guarda solo en la app y vence a los 30 minutos (D-REG-05, REG-09).
- Servicios y utilidades nuevos: `services/cuentaService.ts` (cuentas en memoria, mismas reglas que `CuentaService` de Blazor), `utils/validacion.ts`, componentes `Selector` y `Casilla`.

## Archivos tocados
- Nuevos: `app/registro.tsx`, `app/verificar.tsx`, `app/recuperar.tsx`, `app/mi-cuenta.tsx`, `services/cuentaService.ts`, `services/intencionService.ts`, `utils/validacion.ts`, `types/Cuenta.ts`, `components/Selector.tsx`, `components/Casilla.tsx`.
- Cambiados: `app/(tabs)/ingresar.tsx`, `app/(tabs)/perfil.tsx`, `app/_layout.tsx`, `context/SesionContext.tsx`, `data/profesionales.ts`, `tema/paleta.js`, `mobile/README.md`, `ProfesionalService.cs` (Blazor), `CONTEXTO_BASE.md`, `AGENTS.md`, `PLAN_TAREAS_PROYECTO.md`.
- Las notas de `VeterinariaBrain/` se regeneraron con `actualizar_codigo.py`; incluye notas de la rama que estaban desactualizadas.

## Comprobado
- `tsc --noEmit` sin errores y paquete web generado con `expo export`.
- 37 comprobaciones de la lógica de cuentas (compiladas aparte y ejecutadas con Node): REG-02, REG-03, REG-07, REG-08 y REG-09, cédula y teléfono, ingreso, reenvíos, recuperación, perfil y cambio de contraseña.
- **No** se vio en pantalla ni en un celular: falta la prueba con Expo Go.
- Pendiente de pasar el agente probador de caja negra sobre B-006.

## Cómo probarlo (Expo Go)
1. `cd mobile/Veterinaria.Movil`, `npm install` y `npx expo start`.
2. Ingresar: probar vacío (mensajes por campo), correo o clave incorrectos, y «Ana Rodríguez · Cliente» + «Ingresar».
3. Crear cuenta: dejar todo vacío (errores por campo), cédula inválida (`1.234.567-3`) y válida (`1.234.567-2`), sin marcar las aceptaciones, y con todo correcto.
4. Verificar: usar el código que muestra la pantalla; probar uno incorrecto 5 veces y pedir otro. Al verificar queda adentro.
5. Sin sesión, tocar «Reservar turno» en un servicio → Ingresar → volver al servicio.
6. Recuperar contraseña: correo existente y uno inexistente (mismo mensaje); restablecer con el código.
7. Perfil → Mi cuenta: editar el teléfono, guardar y cambiar la contraseña.
8. Servicios: «Revisión de ovinos y bovinos» y «Rehabilitación equina» muestran ahora a Silveira y Acosta.

## Qué falta
- Prueba con F5/Expo Go y pasar el agente probador.
- Cambio de correo con verificación (D-PER-04): en Blazor existe; en la app queda para escritorio.
- Las cuentas de la app son de demostración y viven en memoria; contraseñas y tokens reales llegan en la sección C.
- B-007: animales del Cliente.

## Integración (misma jornada)
Rama `eric/sesion-2026-11-23-integracion`, desde `main`: se mezclaron la API y la base de datos completas (PR #52 a #56, hasta `diego/sesion-2026-11-22-dos-pasos`), la app móvil (B-001 a B-006) y el README. Un solo conflicto, en notas generadas de `VeterinariaBrain`, resuelto regenerándolas. `desarrollo` y `main` no se tocaron.
No se compiló el C# en esta sesión (sin SDK de .NET): hay que abrir la solución con F5 y correr las pruebas. Siguen pendientes la migración con la semilla de D-CAT-05 en la API y `Update-Database` para `SegundoFactor`.
