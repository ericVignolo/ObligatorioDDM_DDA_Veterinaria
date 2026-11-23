import type { ReactNode } from 'react';
import { Text } from 'react-native';

// Escala tipográfica (S2) con Nunito. Siempre con sus colores de modo claro y oscuro.
export function Titulo({ children }: { children: ReactNode }) {
  return (
    <Text accessibilityRole="header" className="font-nunito-negrita text-2xl text-texto dark:text-texto-oscuro">
      {children}
    </Text>
  );
}

export function Subtitulo({ children }: { children: ReactNode }) {
  return (
    <Text accessibilityRole="header" className="font-nunito-negrita text-lg text-texto dark:text-texto-oscuro">
      {children}
    </Text>
  );
}

export function Parrafo({ children, suave }: { children: ReactNode; suave?: boolean }) {
  return (
    <Text
      className={`font-nunito text-base ${suave ? 'text-texto-suave dark:text-texto-suave-oscuro' : 'text-texto dark:text-texto-oscuro'}`}
    >
      {children}
    </Text>
  );
}

export function Nota({ children }: { children: ReactNode }) {
  return <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">{children}</Text>;
}
