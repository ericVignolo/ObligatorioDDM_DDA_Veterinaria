import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Icono } from './Icono';

type Props = {
  etiqueta: string;
  valor: string;
  opciones: string[];
  onCambio: (valor: string) => void;
  error?: string;
  marcador?: string;
};

// Selector de una opción de una lista corta (tipo de documento, departamento). Se despliega debajo del campo.
export function Selector({ etiqueta, valor, opciones, onCambio, error, marcador = 'Elegí una opción' }: Props) {
  const [abierto, setAbierto] = useState(false);
  const borde = error ? 'border-peligro dark:border-peligro-oscuro' : 'border-borde dark:border-borde-oscuro';

  return (
    <View className="gap-1">
      <Text className="font-nunito-semi text-sm text-texto dark:text-texto-oscuro">{etiqueta}</Text>
      <Pressable
        onPress={() => setAbierto((a) => !a)}
        accessibilityRole="button"
        accessibilityLabel={etiqueta}
        accessibilityValue={{ text: valor || marcador }}
        accessibilityState={{ expanded: abierto }}
        className={`min-h-12 flex-row items-center justify-between rounded-boton border bg-superficie px-3 dark:bg-superficie-oscuro ${borde}`}
      >
        <Text
          className={`font-nunito text-base ${valor ? 'text-texto dark:text-texto-oscuro' : 'text-texto-suave dark:text-texto-suave-oscuro'}`}
        >
          {valor || marcador}
        </Text>
        <Icono nombre={abierto ? 'chevron-up-outline' : 'chevron-down-outline'} color="texto-suave" tamano={18} />
      </Pressable>
      {abierto ? (
        <View className="overflow-hidden rounded-boton border border-borde bg-superficie dark:border-borde-oscuro dark:bg-superficie-oscuro">
          {opciones.map((o) => {
            const activa = o === valor;
            return (
              <Pressable
                key={o}
                accessibilityRole="radio"
                accessibilityState={{ checked: activa }}
                onPress={() => {
                  onCambio(o);
                  setAbierto(false);
                }}
                className={`min-h-12 flex-row items-center justify-between px-3 active:bg-superficie-2 dark:active:bg-superficie-2-oscuro ${
                  activa ? 'bg-primario-suave dark:bg-primario-suave-oscuro' : ''
                }`}
              >
                <Text className="font-nunito text-base text-texto dark:text-texto-oscuro">{o}</Text>
                {activa ? <Icono nombre="checkmark-outline" color="primario" tamano={18} /> : null}
              </Pressable>
            );
          })}
        </View>
      ) : null}
      {error ? <Text className="font-nunito-semi text-sm text-peligro dark:text-peligro-oscuro">{error}</Text> : null}
    </View>
  );
}
