import type { ReactNode } from 'react';
import { Pressable, View } from 'react-native';

type Props = { children: ReactNode; onPress?: () => void; etiqueta?: string };

// Tarjeta de superficie (S4). Si recibe onPress, toda la tarjeta es tocable.
export function Tarjeta({ children, onPress, etiqueta }: Props) {
  const clases =
    'gap-2 rounded-tarjeta border border-borde bg-superficie p-4 dark:border-borde-oscuro dark:bg-superficie-oscuro';
  if (!onPress) return <View className={clases}>{children}</View>;
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={etiqueta}
      className={`${clases} active:bg-superficie-2 dark:active:bg-superficie-2-oscuro`}
    >
      {children}
    </Pressable>
  );
}
