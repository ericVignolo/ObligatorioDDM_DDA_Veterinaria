import Ionicons from '@expo/vector-icons/Ionicons';
import type { ComponentProps } from 'react';
import { useColor } from '../tema';

// Iconos de contorno (opción A de diseño). `color` es un nombre de la paleta: primario, texto-suave, peligro…
type NombreIcono = ComponentProps<typeof Ionicons>['name'];

type Props = { nombre: NombreIcono | string; color?: string; tamano?: number };

export function Icono({ nombre, color = 'texto', tamano = 22 }: Props) {
  const c = useColor();
  return <Ionicons name={nombre as NombreIcono} size={tamano} color={c(color)} accessibilityElementsHidden importantForAccessibility="no" />;
}
