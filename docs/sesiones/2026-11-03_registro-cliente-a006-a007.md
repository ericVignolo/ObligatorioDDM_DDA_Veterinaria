# Sesión 2026-11-03 (2) — Registro de cliente y verificación (A-006, A-007)

Participantes: Eric (con Claude). Rama: `eric/a-006-registro` desde `desktop` → PR a `desktop`.

## Contexto al iniciar

- PR #5 (inicio y páginas públicas) y PR #6 (README) mezclados; `desktop` integrada a `main` (PR #7).
- Eric preguntó si servicios y profesionales pueden cargarse desde una base de datos: sí, está previsto en la sección C. Una sola API con un controller por tema (`ServiciosController`, `ProfesionalesController`), MySQL detrás, y el Administrador los carga desde pantallas (RF-ADM-01, 02 y 04). En Blazor, `PrestacionService` se reemplaza por `PrestacionApiService` sin cambiar las páginas.

## Qué se hizo

**A-006 — Registro** (`/registro`)
- `Models/RegistroCliente.cs`: los 7 datos obligatorios de D-REG-01 con `[Required]`, `[EmailAddress]`, `[StringLength]`, `[Compare]` y dos aceptaciones con `[Range(true)]`. Reglas entre campos con `IValidatableObject`.
- `Models/ValidacionDatos.cs`: cédula uruguaya con dígito verificador; pasaporte u otro documento de 5 a 20 letras o números; teléfono normalizado a E.164 (+598 si no tiene prefijo).
- `Models/Cuenta.cs`, `Models/ResultadoOperacion.cs` y `Services/CuentaService.cs`: cuentas en memoria; correo y documento únicos sin revelar de quién son (REG-02); contraseña guardada con PBKDF2 y salt (RNF-SEG-01); versión de Términos y Privacidad aceptada (RN-43).
- `Pages/Registro.razor`: formulario con `EditForm` en cuatro bloques, apartamento y referencias marcados como opcionales, departamentos de Uruguay y dos casillas separadas sin marcar con enlace a cada documento.
- `Pages/Acceso.razor`: explica por qué se pide cuenta, nombra el servicio elegido y ofrece «Crear cuenta». «Ingresar» queda deshabilitado hasta A-008.

**A-007 — Verificación** (`/verificar`)
- `Models/CodigoVerificacion.cs` y `Pages/Verificar.razor`: código de 6 dígitos, vence a los 15 min, 5 intentos (al quinto error se invalida), hasta 3 reenvíos por hora (D-REG-02).
- Como todavía no hay correo, el código se muestra en pantalla con un aviso de demostración.
- Al verificar, si venía de «Reservar turno», ofrece volver a ese servicio (CU-VIS-03).

## Decisiones

- Las cuentas viven en `CuentaService` (registrado con `AddScoped`, como los demás). En Blazor Server eso dura lo que dura la pestaña: si se recarga la página, se pierden. Es lo esperado en la sección A; la persistencia real es de la API.
- El correo pendiente de verificar se guarda en el servicio y no en la URL, para no exponerlo.

## Cómo probarlo

1. GitHub Desktop → *Fetch origin* → rama `eric/a-006-registro` → F5.
2. Servicios → «Reservar turno» en Consulta → «Crear cuenta».
3. Enviar vacío: aparecen los mensajes de cada campo. Probar cédula `1.234.567-3` (inválida) y `1.234.567-2` (válida); teléfono `099 123 456`.
4. Sin marcar las aceptaciones: dice cuál falta.
5. Crear la cuenta → `/verificar` muestra el código de demostración. Probar un código incorrecto (descuenta intentos), «Enviarme otro código» (máximo 3) y el correcto → «Volver al servicio».
6. Registrar otra vez el mismo correo sin recargar: no deja crear el duplicado.

> El código no se compiló desde el entorno de Claude. Si Visual Studio marca un error, se corrige en esta rama.

## Pendientes

- [x] Eric probó el registro, la verificación y las páginas legales: funciona. PR #8 mezclado el 2026-11-04.
- [x] A-006 marcada; A-007 marcada como parcial (`[~]`, falta recuperación de contraseña).
- [ ] **Siguiente:** A-008 inicio y cierre de sesión; A-011 retomar la acción con límite de 30 min (D-REG-05); recuperación de contraseña (D-REG-03).
- [ ] Diego: decidir proveedor MySQL de DT-BD-02.
- [ ] Botones Reservar / Consultar en el detalle del servicio.
- [ ] Borrar ramas viejas desde GitHub (ver bitácora anterior).
