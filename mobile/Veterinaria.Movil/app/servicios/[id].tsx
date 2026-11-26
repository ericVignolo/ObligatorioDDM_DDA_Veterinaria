import { Stack, useLocalSearchParams, useRouter } from 'expo-router';
import { View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Fila } from '../../components/Fila';
import { Insignia } from '../../components/Insignia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useSesion } from '../../context/SesionContext';
import { llamar, obtenerClinica } from '../../services/clinicaService';
import { formatearPrecio, obtenerPorId } from '../../services/prestacionService';
import { obtenerPublicos } from '../../services/profesionalService';

export default function DetalleServicio() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();
  const { usuario } = useSesion();
  const prestacion = obtenerPorId(Number(id));

  if (!prestacion) {
    return (
      <Pantalla>
        <Vacio icono="help-circle-outline" titulo="Servicio no disponible" accion={{ texto: 'Volver a servicios', onPress: () => router.replace('/servicios') }} />
      </Pantalla>
    );
  }

  const quienes = obtenerPublicos().filter((p) => p.prestacionIds.includes(prestacion.id));

  const reservar = () => {
    if (!usuario) {
      router.push({ pathname: '/ingresar', params: { servicio: String(prestacion.id) } });
      return;
    }
    router.push({ pathname: '/reservar', params: { servicio: String(prestacion.id) } });
  };

  return (
    <Pantalla>
      <Stack.Screen options={{ title: prestacion.nombre }} />
      <Titulo>{prestacion.nombre}</Titulo>
      <Parrafo suave>{prestacion.descripcion}</Parrafo>
      <View className="flex-row flex-wrap gap-2">
        <Insignia texto={`Desde ${formatearPrecio(prestacion.precioDesde)}`} tipo="exito" />
        {prestacion.duracionMinutos ? <Insignia texto={`${prestacion.duracionMinutos} min`} /> : null}
        <Insignia texto={prestacion.reservableEnLinea ? 'Reserva en línea' : 'Se coordina con la clínica'} tipo="info" />
      </View>

      {prestacion.requiereConsentimiento ? (
        <View className="rounded-boton bg-aviso-suave p-3 dark:bg-aviso-suave-oscuro">
          <Parrafo>Requiere firmar el consentimiento informado antes del día del turno.</Parrafo>
        </View>
      ) : null}

      {prestacion.reservableEnLinea ? (
        <Boton texto="Reservar turno" icono="calendar-outline" onPress={reservar} />
      ) : (
        <Boton texto="Consultar o llamar" icono="call-outline" variante="acento" onPress={() => llamar(obtenerClinica().telefono)} />
      )}

      {quienes.length > 0 ? (
        <Tarjeta>
          <Subtitulo>Quiénes lo realizan</Subtitulo>
          {quienes.map((p) => (
            <Fila
              key={p.id}
              icono="person-outline"
              texto={p.nombre}
              detalle={p.especialidad}
              onPress={() => router.push({ pathname: '/profesionales/[id]', params: { id: String(p.id) } })}
            />
          ))}
        </Tarjeta>
      ) : null}
      <Nota>El precio es «desde»: el valor final depende de lo que se haga en la atención.</Nota>
    </Pantalla>
  );
}
