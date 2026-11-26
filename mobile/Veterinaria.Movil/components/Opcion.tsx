import { Pressable, Text, View } from 'react-native';
import { Icono } from './Icono';

type Props = { titulo: string; detalle?: string; elegida: boolean; onPress: () => void };

// Opción de una lista de elección única (animal, servicio, profesional). Alto mínimo 48.
export function Opcion({ titulo, detalle, elegida, onPress }: Props) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="radio"
      accessibilityState={{ checked: elegida }}
      aria-checked={elegida}
      accessibilityLabel={detalle ? `${titulo}, ${detalle}` : titulo}
      className={`min-h-12 flex-row items-center gap-3 rounded-boton border px-3 py-2 ${
        elegida ? 'border-primario bg-primario-suave dark:border-primario-oscuro dark:bg-primario-suave-oscuro' : 'border-borde bg-superficie active:bg-superficie-2 dark:border-borde-oscuro dark:bg-superficie-oscuro'
      }`}
    >
      <Icono nombre={elegida ? 'radio-button-on' : 'radio-button-off'} color={elegida ? 'primario' : 'texto-suave'} />
      <View className="flex-1">
        <Text className="font-nunito-semi text-base text-texto dark:text-texto-oscuro">{titulo}</Text>
        {detalle ? <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">{detalle}</Text> : null}
      </View>
    </Pressable>
  );
}
