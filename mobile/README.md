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
