import { Pressable, Text } from 'react-native';

type Props = {
  texto: string;
  onPress: () => void;
  variante?: 'primario' | 'secundario' | 'acento';
};

const fondos = {
  primario: 'bg-primario active:bg-primario-hover dark:bg-primario-oscuro dark:active:bg-primario-hover-oscuro',
  acento: 'bg-acento active:bg-acento-hover dark:bg-acento-oscuro dark:active:bg-acento-hover-oscuro',
  secundario: 'border border-primario dark:border-primario-oscuro bg-transparent active:bg-primario-suave dark:active:bg-primario-suave-oscuro',
};

const textos = {
  primario: 'text-sobre-primario dark:text-sobre-primario-oscuro',
  acento: 'text-sobre-acento dark:text-sobre-acento-oscuro',
  secundario: 'text-primario dark:text-primario-oscuro',
};

export function Boton({ texto, onPress, variante = 'primario' }: Props) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      className={`min-h-12 items-center justify-center rounded-boton px-4 ${fondos[variante]}`}
    >
      <Text className={`font-nunito-semi text-base ${textos[variante]}`}>{texto}</Text>
    </Pressable>
  );
}
