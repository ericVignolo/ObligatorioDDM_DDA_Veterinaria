# Aplicación móvil

Aplicación React Native para Visitante, Cliente y Veterinario (sección B). Las reglas que aplican están en los [principios vigentes](../docs/contexto-base/CONTEXTO_BASE.md#31-principios-vigentes). Como en la sección A, usa **datos de demostración**: la conexión con la API llega en la sección C.

## Tecnología

| Pieza | Versión | Por qué |
|---|---|---|
| Expo (plantilla `blank-typescript`) | SDK 58 · React Native 0.88 · React 19.3 | La que usa el curso (ejemplo SIGMA 08) |
| Expo Router | 58 | Navegación por archivos en `app/` (manual de hooks de DDM) |
| NativeWind | 4.2.7 (estable) + Tailwind CSS 3.4 | La v5 todavía está en RC (noviembre de 2026) |
| Nunito | `@expo-google-fonts/nunito` | Tipografía de la opción A de diseño |

## Cómo correrla (Windows, terminal de VS Code)

```bash
cd mobile/Veterinaria.Movil
npm install          # solo la primera vez o cuando cambie package.json
npx expo start       # abrir con Expo Go (escanear el QR) o tocar «a» para el emulador de Android
```

Si Expo Go no conecta desde el celular: `npx expo start --tunnel`. Para revisar los tipos: `npm run typecheck`. Para verla en el navegador: `npm run web`.

## Estructura (estilo del curso)

```text
mobile/Veterinaria.Movil/
├── app/                       pantallas (Expo Router)
│   ├── _layout.tsx            proveedores (sesión, avisos), fuentes, tema y pila de pantallas
│   ├── (tabs)/                pestañas por rol: index, servicios, urgencias, ingresar,
│   │                          animales, turnos, agenda, asistencias, avisos, perfil
│   ├── servicios/[id].tsx     detalle de servicio
│   ├── profesionales/         lista y detalle con horarios
│   ├── registro.tsx           crear cuenta (siete datos y dos aceptaciones, D-REG-01/04)
│   ├── verificar.tsx          código de 6 números al correo (D-REG-02)
│   ├── recuperar.tsx          recuperar la contraseña con código (D-REG-03)
│   ├── mi-cuenta.tsx          datos y cambio de contraseña del Cliente
│   ├── clinica.tsx            datos de la clínica
│   └── legales/[codigo].tsx   Privacidad y Términos
├── components/                Boton, Campo, Insignia, Tarjeta, Fila, Textos, Icono, Esqueleto, Vacio,
│                              Pantalla, Proximamente, TarjetaPrestacion, Selector, Casilla
├── context/                   SesionContext (sesión simulada) y AvisoContext (avisos flotantes)
├── data/                      datos ficticios (los mismos que los Services de Blazor)
├── services/                  funciones que leen los datos; en la sección C pasan a llamar a la API
│                              (cuentaService: cuentas en memoria; intencionService: acción pendiente, 30 min)
├── utils/                     validacion.ts: cédula, documento, teléfono E.164 y correo
├── types/                     tipos de TypeScript (mismos nombres que Models/ de Blazor)
├── hooks/                     hooks propios (por ahora vacía)
└── tema/                      paleta de VITALIA (paleta.js) y useColor() para colores en código
```

## Navegación por rol

El mapa completo está en [`docs/analisis/MAPA_MOVIL.md`](../docs/analisis/MAPA_MOVIL.md).

| Rol | Pestañas |
|---|---|
| Visitante | Inicio · Servicios · Urgencias · Ingresar |
| Cliente | Inicio · Animales · Turnos · Avisos · Perfil |
| Veterinario | Agenda · Asistencias · Avisos · Perfil |

Recepción y Administrador trabajan en escritorio. Mientras no esté el formulario de ingreso (B-006), la pestaña **Ingresar** tiene botones de demostración: Ana Rodríguez (Cliente), Dra. Lucía Ferreira (Veterinario) y Laura Benítez (Recepción, que muestra el aviso de escritorio). La sesión vive en memoria: al recargar la app se pierde. Con B-006 el ingreso es un formulario real contra cuentas de demostración (contraseña de las cuentas de prueba: la misma de Blazor); los botones de prueba solo completan el formulario.

## Animales del Cliente (B-007)

Pestaña **Animales** (solo Cliente): lista, ficha (`app/animales/[id]/index.tsx`), alta (`app/animales/nuevo.tsx`) y edición (`app/animales/[id]/editar.tsx`) con el formulario compartido `components/FormularioAnimal.tsx`. Las reglas están en `services/animalService.ts` y son las mismas que `AnimalService` de Blazor (microchip de 15 números y único, grupos de 2 a 10.000, fecha no futura). Foto opcional con `expo-image-picker` (cámara o galería, JPG/PNG/WebP hasta 5 MB) en `components/SelectorFoto.tsx` y `components/FotoAnimal.tsx`. Los datos viven en memoria.

## Reserva y turnos (B-008)

**Reservar** (`app/reservar.tsx`) en cuatro pasos, como `Reservar.razor`: animal, servicio y profesional (solo quien hace ese servicio y atiende esa especie; si hay uno solo se elige solo), fecha y hora (fichas de días con los próximos 60 días y horarios cada 15 minutos) y confirmación. Se llega desde **Turnos**, el detalle de un servicio, la ficha del animal y después de ingresar o verificar la cuenta si había un servicio elegido. **Turnos** (`app/(tabs)/turnos.tsx`): próximos e historial, cancelar con motivo opcional y reprogramar (más de 24 h antes; si no, llamar a Recepción). Las reglas están en `services/agendaService.ts`, iguales a `AgendaService` de Blazor (anticipación de 2 h, descansos, superposición de profesional y animal, seña de las cirugías, ausencias RN-08). Los turnos viven en memoria.

## Historia clínica (B-009)

Desde la ficha del animal, **Historia clínica** (`app/animales/[id]/historia.tsx`): solo lectura y solo consultas firmadas de los animales del Cliente, de la más reciente a la más antigua, con la firma y las enmiendas debajo del original (`components/RegistroClinico.tsx`, como `RegistroClinico.razor`, sin la huella). Los **adjuntos** (`app/animales/[id]/adjunto/[adjuntoId].tsx`) son de demostración: una imagen claramente falsa (`assets/demo/estudio-demo.png`), un PDF sin vista previa y uno retirado; se abren con un acceso temporal simulado de 5 minutos. Datos en `data/consultasDemo.ts` y reglas en `services/historiaService.ts`.

## Vacunas y alertas (B-010)

En la ficha del animal, el **carné de vacunas y antiparasitarios** (`components/CarnetSanitario.tsx`, como `CarnetSanitario.razor`): última aplicación de cada producto con lote, refuerzo y estado (Vigente, Refuerzo próximo dentro de 5 días o Vencida), alertas arriba con «Reservar turno» y el historial completo plegado. La pestaña **Avisos** del Cliente reúne las alertas de todos sus animales, y el Inicio muestra cuántas hay. Reglas en `services/vacunaService.ts` (iguales a `VacunaService`; la fecha de refuerzo la calcula el producto, RN-19) y catálogo en `data/productosSanitarios.ts`.

## Internación (B-011)

En la ficha del animal, arriba del carné (`components/SeguimientoInternacion.tsx`, como el bloque de `AnimalDetalle.razor`): estado, ingreso, motivo y **solo las novedades visibles** (RN-23), con las correcciones señaladas (RN-24); al alta, evolución final e indicaciones para casa. «Actualizar novedades» reemplaza al tiempo real (RNF-DIS-04). `services/internacionService.ts` nunca entrega notas internas ni el plan terapéutico. Datos en `data/internacionesDemo.ts`: Canela (Pedro) en curso y Mishi (Ana) con alta.

## Consentimiento y preferencias (B-012)

**Consentimiento** (`app/consentimiento/[turnoId].tsx`, como `FirmarConsentimiento.razor`): se llega desde **Turnos** y **Avisos** en los servicios que lo piden; firma con casilla y nombre completo, rechazo, retiro antes del procedimiento e historial. Reglas en `services/consentimientoService.ts` (`ESPECIFICACION_CONSENTIMIENTO.md`), huella con `utils/sha256.ts`. **Preferencias y privacidad** (`app/preferencias.tsx`, desde Perfil): canal por tipo de comunicación (`services/comunicacionesService.ts`), aceptaciones obligatorias y consentimientos respondidos. Todo vive en memoria.

## Solicitud de asistencia (B-013)

**Pedir asistencia** (`app/asistencia/nueva.tsx`) desde Inicio, Urgencias o la ficha del animal: modalidad, animal, grupo o especie, síntomas, gravedad percibida, dirección para la visita y criterio de costo. `app/asistencia/index.tsx` lista las propias y `[id].tsx` muestra nivel, estado y datos enviados. Reglas en `services/asistenciaService.ts` (iguales a `AsistenciaService`, `ESPECIFICACION_ALERTAS_ASISTENCIA.md`); sin cuestionario clínico.

## Seguimiento de asistencia (B-014)

En `app/asistencia/[id].tsx`: estado, veterinario y llegada estimada después de aceptar, mapa de demostración durante «En camino» (`components/MapaDemo.tsx`), reevaluación de 24 horas (también en **Avisos**), **Informar empeoramiento** (`components/InformarEmpeoramiento.tsx`, solo puede subir el nivel) e historial. La ubicación del celular se comparte con consentimiento al pedir una visita (`components/UbicacionGps.tsx`, con `expo-location`); si no, dirección escrita. «Simular el siguiente paso» es solo para la demostración, hasta que el Veterinario tenga su agenda (B-015).

## Componentes compartidos

| Componente | Para qué |
|---|---|
| `Boton` | Variantes primario, secundario, acento y peligro; con icono, deshabilitado y cargando (alto mínimo 48) |
| `Campo` | Campo de formulario con etiqueta, ayuda, error y «(opcional)» |
| `Insignia` | Estado con texto: éxito, aviso, peligro, info, acento y neutro |
| `Tarjeta`, `Fila` | Superficie con borde; fila tocable con icono y flecha |
| `Titulo`, `Subtitulo`, `Parrafo`, `Nota` | Escala tipográfica con Nunito |
| `Esqueleto`, `Vacio`, `Proximamente` | Carga, sin datos y pantallas de tareas posteriores |
| `useAviso()` | Aviso flotante de 4 segundos: `avisar('Guardado', 'exito')` |

## Colores y tema

- La paleta sale de `design/vitalia/opcion-*/vars.json` y está en `tema/paleta.js`, con los mismos nombres que en el diseño. **Hoy está activa la opción A**; cuando Diego elija, se cambia `OPCION_ACTIVA` y nada más.
- En las pantallas: `bg-fondo dark:bg-fondo-oscuro`, `text-texto dark:text-texto-oscuro`, `bg-primario active:bg-primario-hover`, `rounded-tarjeta`, `rounded-boton`, `font-nunito`, `font-nunito-semi`, `font-nunito-negrita`.
- Donde no llega una clase (encabezados, iconos, StatusBar): `const color = useColor(); color('primario')` devuelve el claro o el oscuro según el tema.
