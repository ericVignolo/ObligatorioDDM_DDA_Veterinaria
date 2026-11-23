import { Text, View } from 'react-native';

export type TipoInsignia = 'exito' | 'aviso' | 'peligro' | 'info' | 'neutro' | 'acento';

// Insignia de estado (S4): turnos, vacunas, niveles de asistencia. Siempre con texto, no solo color.
const clases: Record<TipoInsignia, { caja: string; texto: string }> = {
  exito: { caja: 'bg-exito-suave dark:bg-exito-suave-oscuro', texto: 'text-exito dark:text-exito-oscuro' },
  aviso: { caja: 'bg-aviso-suave dark:bg-aviso-suave-oscuro', texto: 'text-aviso dark:text-aviso-oscuro' },
  peligro: { caja: 'bg-peligro-suave dark:bg-peligro-suave-oscuro', texto: 'text-peligro dark:text-peligro-oscuro' },
  info: { caja: 'bg-info-suave dark:bg-info-suave-oscuro', texto: 'text-info dark:text-info-oscuro' },
  acento: { caja: 'bg-acento-suave dark:bg-acento-suave-oscuro', texto: 'text-acento dark:text-acento-oscuro' },
  neutro: { caja: 'bg-superficie-2 dark:bg-superficie-2-oscuro', texto: 'text-texto-suave dark:text-texto-suave-oscuro' },
};

export function Insignia({ texto, tipo = 'neutro' }: { texto: string; tipo?: TipoInsignia }) {
  return (
    <View className={`self-start rounded-full px-3 py-1 ${clases[tipo].caja}`}>
      <Text className={`font-nunito-semi text-xs ${clases[tipo].texto}`}>{texto}</Text>
    </View>
  );
}
