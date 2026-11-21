import { Text, View } from 'react-native';
import { clinica } from '../data/clinica';
import { llamar } from '../services/clinicaService';
import { formatearPrecio } from '../services/prestacionService';
import type { Prestacion } from '../types/Prestacion';
import { Boton } from './Boton';
import { Insignia } from './Insignia';
import { Tarjeta } from './Tarjeta';
import { Nota } from './Textos';

// Equivale a PrestacionCard.razor: la tarjeta no decide a dónde ir, avisa a la pantalla.
type Props = {
  prestacion: Prestacion;
  onVerDetalle: (id: number) => void;
  onReservar: (id: number) => void;
};

export function TarjetaPrestacion({ prestacion, onVerDetalle, onReservar }: Props) {
  return (
    <Tarjeta>
      <Text className="font-nunito-negrita text-lg text-texto dark:text-texto-oscuro">{prestacion.nombre}</Text>
      <Nota>{prestacion.descripcion}</Nota>
      <View className="flex-row flex-wrap items-center gap-2">
        <Text className="font-nunito-semi text-base text-primario dark:text-primario-oscuro">
          Desde {formatearPrecio(prestacion.precioDesde)}
        </Text>
        {prestacion.duracionMinutos ? <Insignia texto={`${prestacion.duracionMinutos} min`} /> : null}
        {prestacion.requiereConsentimiento ? <Insignia texto="Requiere consentimiento" tipo="aviso" /> : null}
      </View>
      <View className="mt-2 gap-2">
        {prestacion.reservableEnLinea ? (
          <Boton texto="Reservar turno" icono="calendar-outline" onPress={() => onReservar(prestacion.id)} />
        ) : (
          // D-CAT-02: si no se reserva en línea, se ofrece consultar o llamar.
          <Boton texto="Consultar o llamar" icono="call-outline" variante="acento" onPress={() => llamar(clinica.telefono)} />
        )}
        <Boton texto="Ver detalle" variante="secundario" onPress={() => onVerDetalle(prestacion.id)} />
      </View>
    </Tarjeta>
  );
}
