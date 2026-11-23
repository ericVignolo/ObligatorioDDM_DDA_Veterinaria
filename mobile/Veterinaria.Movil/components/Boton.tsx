import { ActivityIndicator, Pressable, Text, View } from 'react-native';
import { useColor } from '../tema';
import { Icono } from './Icono';

type Variante = 'primario' | 'secundario' | 'acento' | 'peligro';

type Props = {
  texto: string;
  onPress: () => void;
  variante?: Variante;
  icono?: string;
  deshabilitado?: boolean;
  cargando?: boolean;
};

// Estados de S3: por defecto, presionado, deshabilitado y cargando. Alto mínimo 48 (táctil).
const fondos: Record<Variante, string> = {
  primario: 'bg-primario active:bg-primario-hover dark:bg-primario-oscuro dark:active:bg-primario-hover-oscuro',
  acento: 'bg-acento active:bg-acento-hover dark:bg-acento-oscuro dark:active:bg-acento-hover-oscuro',
  peligro: 'bg-peligro dark:bg-peligro-oscuro active:opacity-90',
  secundario:
    'border border-primario bg-transparent active:bg-primario-suave dark:border-primario-oscuro dark:active:bg-primario-suave-oscuro',
};

const textos: Record<Variante, string> = {
  primario: 'text-sobre-primario dark:text-sobre-primario-oscuro',
  acento: 'text-sobre-acento dark:text-sobre-acento-oscuro',
  peligro: 'text-sobre-primario dark:text-sobre-primario-oscuro',
  secundario: 'text-primario dark:text-primario-oscuro',
};

const colorIcono: Record<Variante, string> = {
  primario: 'sobre-primario',
  acento: 'sobre-acento',
  peligro: 'sobre-primario',
  secundario: 'primario',
};

export function Boton({ texto, onPress, variante = 'primario', icono, deshabilitado, cargando }: Props) {
  const color = useColor();
  const inactivo = deshabilitado || cargando;
  return (
    <Pressable
      onPress={onPress}
      disabled={inactivo}
      accessibilityRole="button"
      accessibilityState={{ disabled: inactivo, busy: cargando }}
      className={`min-h-12 flex-row items-center justify-center gap-2 rounded-boton px-4 ${fondos[variante]} ${inactivo ? 'opacity-50' : ''}`}
    >
      {cargando ? (
        <ActivityIndicator color={color(colorIcono[variante])} />
      ) : icono ? (
        <View>
          <Icono nombre={icono} color={colorIcono[variante]} tamano={20} />
        </View>
      ) : null}
      <Text className={`font-nunito-semi text-base ${textos[variante]}`}>{texto}</Text>
    </Pressable>
  );
}
