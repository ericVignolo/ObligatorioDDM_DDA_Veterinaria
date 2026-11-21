import type { ReactNode } from 'react';
import { ScrollView } from 'react-native';

// Contenedor de cada pantalla: fondo del tema, margen y desplazamiento.
export function Pantalla({ children }: { children: ReactNode }) {
  return (
    <ScrollView className="flex-1 bg-fondo dark:bg-fondo-oscuro" contentContainerClassName="gap-4 p-5 pb-10">
      {children}
    </ScrollView>
  );
}
