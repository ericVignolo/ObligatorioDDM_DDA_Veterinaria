import { Text, View } from 'react-native';
import { Icono } from './Icono';

const NOMBRES = ['Animal', 'Servicio', 'Fecha y hora', 'Confirmar'];

// Indicador de los cuatro pasos de la reserva (RC-D07, P-RC-01), como PasosReserva.razor.
export function PasosReserva({ actual }: { actual: number }) {
  return (
    <View
      accessible
      accessibilityLabel={`Paso ${actual} de ${NOMBRES.length}: ${NOMBRES[actual - 1]}`}
      className="flex-row items-start justify-between gap-1"
    >
      {NOMBRES.map((nombre, i) => {
        const numero = i + 1;
        const hecho = numero < actual;
        const activo = numero === actual;
        return (
          <View key={nombre} className="flex-1 items-center gap-1" importantForAccessibility="no-hide-descendants">
            <View
              className={`h-8 w-8 items-center justify-center rounded-full ${
                activo ? 'bg-primario dark:bg-primario-oscuro' : hecho ? 'bg-primario-suave dark:bg-primario-suave-oscuro' : 'border border-borde dark:border-borde-oscuro'
              }`}
            >
              {hecho ? (
                <Icono nombre="checkmark-outline" color="primario" tamano={18} />
              ) : (
                <Text className={`font-nunito-negrita text-sm ${activo ? 'text-sobre-primario dark:text-sobre-primario-oscuro' : 'text-texto-suave dark:text-texto-suave-oscuro'}`}>{numero}</Text>
              )}
            </View>
            <Text className={`text-center font-nunito-semi text-xs ${activo ? 'text-primario dark:text-primario-oscuro' : 'text-texto-suave dark:text-texto-suave-oscuro'}`}>{nombre}</Text>
          </View>
        );
      })}
    </View>
  );
}
