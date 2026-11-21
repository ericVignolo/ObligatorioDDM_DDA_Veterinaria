# Sesión 2026-11-21 — Inicio de la sección B (móvil)

Rama: `eric/sesion-2026-11-21-movil`, desde `desarrollo`.

## Estado de la API al empezar (revisado en `diego/sesion-2026-11-21-asistencia`)
- Base de datos completa: 52 tablas y migraciones `Inicial`, `ModulosClinicos` y `Sesiones`.
- API cerca del 90 %: 22 controladores, unos 100 endpoints y 950 métodos de prueba. Falta: 2FA del Administrador, correo real, tareas programadas, tiempo real, derechos del titular y borrado de cuentas sin verificar.
- Los PR #39 a #51 todavía no están en `desarrollo` (Diego los mezcla al final).

## Qué se hizo
- Proyecto `mobile/Veterinaria.Movil`: Expo SDK 58 con TypeScript, Expo Router, NativeWind 4.2.7, Tailwind 3.4 y Nunito.
- Paleta de VITALIA (opciones A y B de `design/vitalia`, activa la A) en `tema/paleta.js`, con modo claro y oscuro.
- Primer recorrido del Visitante con datos de demostración: inicio (datos de la clínica y guardia), lista de servicios, detalle y `/acceso` provisorio. Son los mismos datos y las mismas reglas que Blazor (D-CAT-02/04).
- Comprobado: `tsc` sin errores, `npm ci` limpio y paquete de Android generado con `expo export`.

## Cómo probarlo
1. `cd mobile/Veterinaria.Movil`, `npm install` y `npx expo start`; abrir en Expo Go.
2. Inicio → «Ver servicios»: tienen que aparecer 7 servicios, sin «Guardería».
3. «Revisión de ovinos y bovinos» muestra «Consultar o llamar», que abre el marcador.
4. «Castración» → «Ver detalle»: aparece el aviso de consentimiento informado.
5. Cambiar el celular a modo oscuro: la app cambia de colores.

## Qué falta
- Registro, verificación e ingreso (equivalentes de A-006 a A-011), con la sesión en `context/`.
- Animales y reserva en cuatro pasos, y después los flujos del Cliente y del Veterinario.
- Iconos: definir la biblioteca (opción A: contorno fino) y cargar el ícono y la pantalla de inicio de VITALIA en `assets/`.
- Diego elige entre la opción A y la B de diseño.
