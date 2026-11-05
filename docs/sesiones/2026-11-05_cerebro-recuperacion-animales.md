# Sesión 2026-11-05 — Cerebro de Obsidian, recuperación de contraseña y animales

Participantes: Eric (con Claude). Rama: `eric/sesion-2026-11-05` desde `desktop` (primera sesión con una rama por sesión, DT-ORG-06) → un PR a `desktop`.

## Contexto al iniciar

- Eric creó el cerebro de Obsidian `VeterinariaBrain`, pero estaba dentro de una **copia vieja del repositorio** (`OneDrive\Desktop\ObligatorioVeterinaria`, congelada el 20/10). En la computadora había tres copias; la única al día es la que usan GitHub Desktop y Visual Studio (`OneDrive\Documentos\GitHub\ObligatorioDDM_DDA_Veterinaria`).
- Eric movió el vault a la carpeta correcta y decidió **subirlo a GitHub** para que Diego también lo tenga.
- Dudas resueltas: la API expondrá endpoints de animales (`/api/v1/animales`, con RN-18 y RN-41) y acciones de cuenta (`/auth/registro`, `/auth/ingresar`, `/auth/recuperar`…). La base de datos solo guarda, con restricciones como correo y microchip únicos; nunca se expone una contraseña, solo su derivado.
- Las ramas `diego/cierre-h0`, `diego/proyecto-blazor`, `eric/readme-estado`, `eric/a-006-registro`, `eric/a-008-sesion` y `feature/escritorio-api` no tienen nada fuera de `desktop`: se pueden borrar.

## Qué se hizo

**Cerebro de Obsidian (`VeterinariaBrain/`)**
- Versionado en git; se excluye la configuración personal (`.obsidian/workspace*.json`) y `.claude/`.
- `_scripts/actualizar_codigo.py` genera una nota por página, componente, servicio y modelo con **«Usa»** y **«Usado por»**, más el **Mapa Código**. Hoy son 53 notas.
- Notas nuevas o al día: VITALIA, decisiones del 31/10, Plan B, README, CLAUDE.md, bitácoras del 02 al 05/11, Ramas git, MySQL, personas, Inicio, Carpeta desktop y el canvas de arquitectura. Colores por tipo en el gráfico.
- `AGENTS.md`: antes de tocar código, revisar «Usado por»; al cerrar, regenerar las notas.

**A-007 — Recuperación de contraseña (D-REG-03)**
- `Pages/Recuperar.razor` (`/recuperar`) y `Pages/Restablecer.razor` (`/restablecer`), modelos `SolicitudRecuperacion` y `NuevaContrasena`.
- `CuentaService.SolicitarRecuperacion` responde siempre lo mismo, exista o no la cuenta (REG-08). Código de 6 dígitos, 15 min, 5 intentos, 3 pedidos por hora.
- `CuentaService.Restablecer` cambia la contraseña con PBKDF2 y cierra la sesión abierta de esa cuenta.
- `/ingresar` tiene el enlace «¿Olvidaste tu contraseña?».

**A-012 y A-013 — Mis animales**
- `Models/Animal.cs`, `Models/AnimalFormulario.cs` y `Services/AnimalService.cs`: especie, raza, sexo, nacimiento, microchip (15 dígitos, único, RN-41), caravana, **grupos de campo** con cantidad, señas y antecedentes informados.
- Páginas `/animales` (listado), `/animales/{Id}` (ficha), `/animales/nuevo` y `/animales/{Id}/editar`, con el formulario compartido `AnimalForm` (avisa con `EventCallback`) y la tarjeta `AnimalCard`.
- `RequiereSesion`: las páginas de animales solo se ven con sesión; si no, ofrece ingresar.
- Cada cliente ve y edita solo sus animales; si pide uno ajeno, dice «no encontramos ese animal entre los tuyos» (RN-18).
- Botón «Cargar ejemplos (caballo y oveja)»: el caballo Lucero y una majada de 40 ovejas.
- Hay un animal de otro cliente con el microchip `858000000000001` para probar que no se puede repetir.
- La barra superior suma «Mis animales» con sesión iniciada.

## Decisiones

- El vault va a GitHub (Eric). Las notas de código se regeneran con el script; las demás se escriben a mano.
- La foto del animal queda para cuando exista la API, que guarda archivos.

## Cómo probarlo

1. Cerrar Obsidian y renombrar la carpeta local `VeterinariaBrain` a `VeterinariaBrain_viejo`.
2. GitHub Desktop → *Fetch origin* → rama `eric/sesion-2026-11-05` → F5 en Visual Studio.
3. Crear una cuenta, verificar y entrar a **Mis animales** → «Cargar ejemplos» → abrir Lucero y la majada → editar.
4. Registrar un animal con microchip `858000000000001` → debe decir que ya figura en el sistema.
5. Cambiar en la barra de direcciones a `/animales/1` (es de otro cliente) → «no encontramos ese animal».
6. Cerrar sesión → «¿Olvidaste tu contraseña?» → correo de la cuenta → código de demostración → contraseña nueva → ingresar con ella.
7. Abrir `VeterinariaBrain` en Obsidian y mirar el gráfico y el Mapa Código.

> No compilado desde el entorno de Claude. Si Visual Studio marca un error, se corrige en esta rama.

## Pendientes

- [x] Eric probó la rama: registro de animales (un loro con microchip), microchip repetido rechazado, ingreso y recuperación funcionan. Vault cargado en Obsidian. PR #12 mezclado.
- [x] A-007, A-012 y A-013 marcadas; README al día.
- [ ] Borrar `VeterinariaBrain_viejo` (Eric ya puede) y las dos copias viejas del repositorio (`OneDrive\Desktop\ObligatorioVeterinaria` y `source\repos\ObligatorioDDM_DDA_Veterinaria`).
- [ ] Borrar desde GitHub las 6 ramas ya integradas.
- [ ] Decidir si `.claude/skills` (skills de Obsidian) se sube al repositorio.
- [ ] Diego: proveedor MySQL de DT-BD-02 (EF Core 9 sin soporte desde el 2026-11-10).
- [ ] Siguiente: A-014 disponibilidad y A-015 reserva en cuatro pasos (completa el primer recorrido).
