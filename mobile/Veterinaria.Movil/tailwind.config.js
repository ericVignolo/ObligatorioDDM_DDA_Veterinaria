const { colores } = require('./tema/paleta');

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./app/**/*.{ts,tsx}', './components/**/*.{ts,tsx}'],
  presets: [require('nativewind/preset')],
  // 'class' permite seguir al sistema y también cambiar el tema a mano (useColorScheme de nativewind).
  darkMode: 'class',
  theme: {
    extend: {
      // Uso: bg-fondo dark:bg-fondo-oscuro, text-texto dark:text-texto-oscuro, etc.
      colors: colores,
      fontFamily: {
        nunito: ['Nunito_400Regular'],
        'nunito-semi': ['Nunito_600SemiBold'],
        'nunito-negrita': ['Nunito_800ExtraBold'],
      },
      borderRadius: { tarjeta: '16px', boton: '12px' },
    },
  },
  plugins: [],
};
