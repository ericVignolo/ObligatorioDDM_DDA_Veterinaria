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

## Foto del animal (RF-CLI-01, pedido de Eric el 25/11)
RF-CLI-01 (Must) y `ALCANCE_ENTREGA.md` piden la foto en el alta y la edición, y CU-CLI-01 A2 dice que una foto inválida se rechaza sola, con el motivo. No estaba en Blazor (decía «se agrega cuando exista la API»), ni en la app, ni en la API. Va en esta misma rama porque es el mismo requisito.
- **App:** dependencia nueva `expo-image-picker` ~58.0.10 (aprobada por Eric) y textos de permiso en `app.json`. `components/SelectorFoto.tsx` ofrece «Tomar foto», «Elegir de la galería» y «Quitar foto», con recorte cuadrado y compresión. Si no hay permiso de cámara, lo explica, ofrece «Abrir ajustes» y deja la galería. Acepta JPG, PNG o WebP de hasta 5 MB. `components/FotoAnimal.tsx` la muestra: miniatura en la lista (decorativa, RNF-USA-06) y grande en la ficha («Foto de …»). Sin foto, se ve la huella.
- **Blazor:** campo `Foto` en `Animal` y `AnimalFormulario`; `AnimalForm.razor` la carga con `InputFile` (mismos formatos y tamaño), la achica a 800 px en el navegador y la guarda como data URL en memoria. Componente `FotoAnimal.razor` en Mis animales (`AnimalCard`), la ficha, el encabezado de la consulta (`AtencionConsulta`) y las dos historias clínicas, para confirmar que es el paciente correcto. Estilos en `wwwroot/app.css`. Prueba nueva `Foto_se_guarda_se_edita_y_se_quita`.
- **API (falta, rama de Diego):** campo de foto en la entidad `Animal`, migración y un endpoint para subir la imagen (por ejemplo `POST /api/v1/animales/{id}/foto`) con los mismos límites.

## Comprobado
- `tsc --noEmit` sin errores y paquete web generado con `expo export`.
- Prueba automática en navegador (390×844), 19 de 19 bien y sin errores en consola: «Volver» en Urgencias (aparece, mide 48 px, vuelve a Inicio, no aparece desde la pestaña); lista de Ana sin animales de Pedro; obligatorios; fecha inexistente y futura; microchip corto y repetido; alta que lleva a la ficha; edición a grupo (exige cantidad, rechaza 1); ficha y lista actualizadas; animal ajeno no visible. Formulario revisado también en modo oscuro.
- Foto en la app (navegador): marcador sin foto, GIF rechazado, JPG aceptado con vista previa, foto en ficha y lista, se conserva al editar, se quita, y «Tomar foto» funciona en la web. 8 de 8 bien, sin errores en consola.
- Blazor compila sin errores ni advertencias. La lógica de la foto en `AnimalService` se comprobó con un programa aparte. **No** se pudieron correr las pruebas xUnit (sin acceso a NuGet desde la nube): correrlas en Visual Studio. La pantalla de Blazor con la foto falta verla con F5.
- **No** se probó en Expo Go (la cámara real solo se prueba en el celular).

## Pendiente
- Subir las ramas con GitHub Desktop (desde Cowork no se puede hacer `push`).
- Probar en el celular con Expo Go.
- Menor, ya existía: en la versión web el nombre accesible de los botones con icono incluye el carácter del icono (en el celular el icono queda oculto para el lector de pantalla).
- Próxima sesión (pedido de Eric): en **Mi cuenta**, enlace «¿No recordás tu contraseña actual?» que lleve a la recuperación con el correo cargado, en la app y en Blazor. La recuperación ya existe desde Ingresar (B-006 y Blazor).
- Después: B-008, reserva y turnos.

## Cómo probarlo
1. `cd mobile\Veterinaria.Movil`, `npm install` y `npx expo start` (o `w` para la web).
2. Ingresar como Ana (`ana.demo@vitalia.uy`, contraseña de las cuentas de prueba) y abrir **Animales**.
3. Registrar un animal; probar el microchip de Toby (`858000000000001`) para ver el rechazo.
4. Editarlo, marcar «Es un grupo de animales» y guardar.
