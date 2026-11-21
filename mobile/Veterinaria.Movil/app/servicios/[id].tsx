import { useLocalSearchParams, useRouter } from 'expo-router';
import { ScrollView, Text } from 'react-native';
import { Boton } from '../../components/Boton';
import { formatearPrecio, obtenerPorId } from '../../services/prestacionService';

export default function DetalleServicio() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();
  const prestacion = obtenerPorId(Number(id));

  if (!prestacion) {
    return (
      <ScrollView className="flex-1 bg-fondo dark:bg-fondo-oscuro" contentContainerClassName="gap-4 p-5">
        <Text className="font-nunito-negrita text-xl text-texto dark:text-texto-oscuro">Servicio no disponible</Text>
        <Boton texto="Volver a servicios" variante="secundario" onPress={() => router.replace('/servicios')} />
      </ScrollView>
    );
  }

  return (
    <ScrollView className="flex-1 bg-fondo dark:bg-fondo-oscuro" contentContainerClassName="gap-3 p-5">
      <Text className="font-nunito-negrita text-2xl text-texto dark:text-texto-oscuro">{prestacion.nombre}</Text>
      <Text className="font-nunito text-base text-texto-suave dark:text-texto-suave-oscuro">{prestacion.descripcion}</Text>
      <Text className="font-nunito-semi text-lg text-primario dark:text-primario-oscuro">Desde {formatearPrecio(prestacion.precioDesde)}</Text>
      {prestacion.duracionMinutos ? (
        <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">Duración: {prestacion.duracionMinutos} min</Text>
      ) : null}
      {prestacion.requiereConsentimiento ? (
        <Text className="rounded-boton bg-aviso-suave p-3 font-nunito text-sm text-aviso dark:bg-aviso-suave-oscuro dark:text-aviso-oscuro">
          Requiere firmar el consentimiento informado antes del día del turno.
        </Text>
      ) : null}
      {prestacion.reservableEnLinea ? (
        <Boton texto="Reservar turno" onPress={() => router.push({ pathname: '/acceso', params: { servicio: String(prestacion.id) } })} />
      ) : null}
    </ScrollView>
  );
}
