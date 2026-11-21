import { useColorScheme } from 'nativewind';
import { colores } from './paleta';

// Para los lugares donde no llega una clase de NativeWind (iconos, StatusBar, ActivityIndicator...).
// Devuelve el color del tema actual: color('primario') da el claro o el -oscuro según corresponda.
export function useColor() {
  const { colorScheme } = useColorScheme();
  const oscuro = colorScheme === 'dark';
  return (nombre: string): string => (oscuro ? colores[`${nombre}-oscuro`] : undefined) ?? colores[nombre];
}

export const fuentes = {
  normal: 'Nunito_400Regular',
  semi: 'Nunito_600SemiBold',
  negrita: 'Nunito_800ExtraBold',
};
