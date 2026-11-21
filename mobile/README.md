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

Si Expo Go no conecta desde el celular: `npx expo start --tunnel`. Para revisar los tipos: `npm run typecheck`.

## Estructura (estilo del curso)

```text
mobile/Veterinaria.Movil/
├── app/                 pantallas (Expo Router): _layout.tsx, index.tsx, servicios/, acceso.tsx
├── components/          piezas reutilizables (Boton, TarjetaPrestacion)
├── data/                datos ficticios (los mismos que los Services de Blazor)
├── services/            funciones que leen los datos; en la sección C pasan a llamar a la API
├── types/               tipos de TypeScript (mismos nombres que Models/ de Blazor)
├── hooks/  context/     hooks propios y contexto (sesión, tema), por ahora vacías
├── tema/                paleta de VITALIA (paleta.js) y useColor() para colores en código
├── global.css  tailwind.config.js  babel.config.js  metro.config.js   configuración de NativeWind
└── app.json             nombre VITALIA, esquema vitalia://, modo claro y oscuro automático
```

## Colores y tema

- La paleta sale de `design/vitalia/opcion-*/vars.json` y está en `tema/paleta.js`, con los mismos nombres que en el diseño. **Hoy está activa la opción A**; cuando Diego elija, se cambia `OPCION_ACTIVA` y nada más.
- En las pantallas: `bg-fondo dark:bg-fondo-oscuro`, `text-texto dark:text-texto-oscuro`, `bg-primario active:bg-primario-hover`, `rounded-tarjeta`, `rounded-boton`, `font-nunito`, `font-nunito-semi`, `font-nunito-negrita`.
- Donde no llega una clase (encabezados, iconos, StatusBar): `const color = useColor(); color('primario')` devuelve el claro o el oscuro según el tema.
