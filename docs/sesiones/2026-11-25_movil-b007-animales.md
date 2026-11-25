# Sesión 2026-11-25 — Móvil: «Volver» en Urgencias (B-006) y animales del Cliente (B-007)

Eric probó B-006 en el navegador y funciona. Se trabaja rama por rama: B-006 → B-007 → B-008…, hasta mezclar con la API y el escritorio.

## B-006 (rama `eric/sesion-2026-11-23-movil`)
- Commit `8d53cd4`: **botón «Volver»** en el encabezado de Urgencias cuando se abre desde «Cómo saber si es urgente» (Inicio) o «Ver urgencias» (La clínica). Lleva a la pantalla de origen. Si se entra desde la barra de pestañas, no aparece. Mide 48 px de alto.

## B-007 (rama `eric/sesion-2026-11-25-movil-b007`, desde la de B-006)
Mismas reglas que `AnimalService` y `AnimalFormulario` de Blazor (A-012, A-013, CU-CLI-01):
- **Mis animales** (`app/(tabs)/animales.tsx`): solo los del responsable (RN-18), por nombre, con insignia «Grupo de N». Si no hay ninguno: estado vacío y «Cargar ejemplos (caballo y oveja)».
- **Registrar** (`app/animales/nuevo.tsx`) y **Editar** (`app/animales/[id]/editar.tsx`), con el formulario compartido `components/FormularioAnimal.tsx`: nombre, especie y sexo obligatorios; raza, fecha de nacimiento (DD/MM/AAAA, no futura), microchip (15 números, único, RN-41), caravana (hasta 20 letras, números o guiones), señas y antecedentes opcionales. Un **grupo** pide cantidad (2 a 10.000) y no lleva microchip ni fecha.
- **Microchip repetido:** no se revela de quién es el otro animal (CU-CLI-01 A1).
- **Ficha** (`app/animales/[id]/index.tsx`): datos, responsable, señas y antecedentes. Un animal ajeno o inexistente muestra «Animal no disponible» sin distinguir el caso (RN-18).
- **Historia clínica, carné e internación** quedan anunciados en la ficha y se construyen en B-009, B-010 y B-011.
- Nuevos: `types/Animal.ts`, `data/animalesDemo.ts` (Toby, Mishi, Rodeo La Esperanza y Canela, como Blazor), `services/animalService.ts`.

## Comprobado
- `tsc --noEmit` sin errores y paquete web generado con `expo export`.
- Prueba automática en navegador (390×844), 19 de 19 bien y sin errores en consola: «Volver» en Urgencias (aparece, mide 48 px, vuelve a Inicio, no aparece desde la pestaña); lista de Ana sin animales de Pedro; obligatorios; fecha inexistente y futura; microchip corto y repetido; alta que lleva a la ficha; edición a grupo (exige cantidad, rechaza 1); ficha y lista actualizadas; animal ajeno no visible. Formulario revisado también en modo oscuro.
- **No** se probó en Expo Go.

## Pendiente
- Subir las ramas con GitHub Desktop (desde Cowork no se puede hacer `push`).
- Probar en el celular con Expo Go.
- Menor, ya existía: en la versión web el nombre accesible de los botones con icono incluye el carácter del icono (en el celular el icono queda oculto para el lector de pantalla).
- Siguiente: B-008, reserva y turnos.

## Cómo probarlo
1. `cd mobile\Veterinaria.Movil`, `npm install` y `npx expo start` (o `w` para la web).
2. Ingresar como Ana (`ana.demo@vitalia.uy`, contraseña de las cuentas de prueba) y abrir **Animales**.
3. Registrar un animal; probar el microchip de Toby (`858000000000001`) para ver el rechazo.
4. Editarlo, marcar «Es un grupo de animales» y guardar.
