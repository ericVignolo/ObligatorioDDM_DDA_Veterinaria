import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Icono } from './Icono';

type Props = {
  marcada: boolean;
  onCambio: (marcada: boolean) => void;
  children: ReactNode;
  error?: string;
  etiqueta: string;
};

// Casilla de aceptación (RN-43): nunca llega marcada de antemano. El texto puede llevar un enlace al documento.
export function Casilla({ marcada, onCambio, children, error, etiqueta }: Props) {
  return (
    <View className="gap-1">
      <View className="min-h-12 flex-row items-center gap-3">
        <Pressable
          onPress={() => onCambio(!marcada)}
          accessibilityRole="checkbox"
          accessibilityLabel={etiqueta}
          accessibilityState={{ checked: marcada }}
          aria-checked={marcada}
          hitSlop={8}
          className="h-12 w-8 items-center justify-center"
        >
          <Icono nombre={marcada ? 'checkbox' : 'square-outline'} color={marcada ? 'primario' : 'texto-suave'} tamano={26} />
        </Pressable>
        <View className="flex-1">{children}</View>
      </View>
      {error ? <Text className="font-nunito-semi text-sm text-peligro dark:text-peligro-oscuro">{error}</Text> : null}
    </View>
  );
}
