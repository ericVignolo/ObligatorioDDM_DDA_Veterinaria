import { Pressable, Text, View } from 'react-native';
import { Icono } from './Icono';

type Props = { icono: string; texto: string; detalle?: string; onPress?: () => void };

// Fila de lista con icono, texto y flecha (accesos rápidos, contacto, documentos).
export function Fila({ icono, texto, detalle, onPress }: Props) {
  return (
    <Pressable
      onPress={onPress}
      disabled={!onPress}
      accessibilityRole={onPress ? 'button' : 'text'}
      className="min-h-12 flex-row items-center gap-3 py-2 active:opacity-70"
    >
      <Icono nombre={icono} color="primario" />
      <View className="flex-1">
        <Text className="font-nunito-semi text-base text-texto dark:text-texto-oscuro">{texto}</Text>
        {detalle ? <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">{detalle}</Text> : null}
      </View>
      {onPress ? <Icono nombre="chevron-forward-outline" color="texto-suave" tamano={18} /> : null}
    </Pressable>
  );
}
