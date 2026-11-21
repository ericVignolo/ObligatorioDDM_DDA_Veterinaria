import { Linking, Text, View } from 'react-native';
import { clinica } from '../data/clinica';
import { formatearPrecio } from '../services/prestacionService';
import type { Prestacion } from '../types/Prestacion';
import { Boton } from './Boton';

// Equivale a PrestacionCard.razor: la tarjeta no decide a dónde ir, avisa a la pantalla.
type Props = {
  prestacion: Prestacion;
  onVerDetalle: (id: number) => void;
  onReservar: (id: number) => void;
};

export function TarjetaPrestacion({ prestacion, onVerDetalle, onReservar }: Props) {
  return (
    <View className="gap-2 rounded-tarjeta border border-borde bg-superficie p-4 dark:border-borde-oscuro dark:bg-superficie-oscuro">
      <Text className="font-nunito-negrita text-lg text-texto dark:text-texto-oscuro">{prestacion.nombre}</Text>
      <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">{prestacion.descripcion}</Text>
      <Text className="font-nunito-semi text-base text-primario dark:text-primario-oscuro">
        Desde {formatearPrecio(prestacion.precioDesde)}
      </Text>
      <View className="mt-2 gap-2">
        {prestacion.reservableEnLinea ? (
          <Boton texto="Reservar turno" onPress={() => onReservar(prestacion.id)} />
        ) : (
          // D-CAT-02: si no se reserva en línea, se ofrece consultar o llamar.
          <Boton texto="Consultar o llamar" variante="acento" onPress={() => Linking.openURL(`tel:${clinica.telefono.replace(/\s/g, '')}`)} />
        )}
        <Boton texto="Ver detalle" variante="secundario" onPress={() => onVerDetalle(prestacion.id)} />
      </View>
    </View>
  );
}
