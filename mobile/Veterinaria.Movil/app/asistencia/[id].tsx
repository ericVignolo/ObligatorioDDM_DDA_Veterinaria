import { useLocalSearchParams, useRouter } from 'expo-router';
import { Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Insignia } from '../../components/Insignia';
import { LlamarGuardia } from '../../components/LlamarGuardia';
import { NivelAsistencia } from '../../components/NivelAsistencia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useSesion } from '../../context/SesionContext';
import { obtenerPorId as obtenerAnimal } from '../../services/animalService';
import { estaActiva, explicacionEstado, obtenerDe } from '../../services/asistenciaService';
import { llamar, obtenerClinica } from '../../services/clinicaService';
import { formatearPrecio } from '../../services/prestacionService';
import { diaCorto, hora } from '../../utils/fechas';

// Constancia y detalle de una solicitud (B-013, CU-CLI-19, paso 10): nivel, estado real y datos enviados.
// El seguimiento, la reevaluación y «Informar empeoramiento» se agregan en B-014.
export default function DetalleAsistencia() {
  const router = useRouter();
  const { usuario } = useSesion();
  const { id, nueva } = useLocalSearchParams<{ id: string; nueva?: string }>();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const solicitud = clienteId === null ? undefined : obtenerDe(Number(id), clienteId);

  // RN-18: no se distingue entre «no existe» y «no es tuya».
  if (clienteId === null || !solicitud) {
    return (
      <Pantalla>
        <Vacio icono="medkit-outline" titulo="Solicitud no disponible" texto="No encontramos esa solicitud entre las tuyas." accion={{ texto: 'Ir al inicio', onPress: () => router.navigate('/') }} />
      </Pantalla>
    );
  }

  const animal = solicitud.animalId ? obtenerAnimal(solicitud.animalId, clienteId) : undefined;
  const dato = (etiqueta: string, valor: string) =>
    valor ? (
      <View className="gap-0.5">
        <Text className="font-nunito-semi text-sm text-texto-suave dark:text-texto-suave-oscuro">{etiqueta}</Text>
        <Parrafo>{valor}</Parrafo>
      </View>
    ) : null;

  return (
    <Pantalla>
      <Titulo>{nueva ? 'Recibimos tu solicitud' : `Solicitud n.º ${solicitud.id}`}</Titulo>

      <Tarjeta>
        {nueva ? <Nota>Solicitud n.º {solicitud.id}</Nota> : null}
        <NivelAsistencia nivel={solicitud.nivel} />
        <Insignia texto={solicitud.estado} tipo={solicitud.estado === 'Requiere coordinación de Recepción' ? 'aviso' : 'info'} />
        <Parrafo>{explicacionEstado(solicitud)}</Parrafo>
      </Tarjeta>

      {estaActiva(solicitud) ? (
        solicitud.nivel === 'Rojo' ? (
          <LlamarGuardia texto="Es un caso rojo: llamá también por teléfono, sobre todo si empeora." />
        ) : (
          <Tarjeta>
            <Subtitulo>Si empeora</Subtitulo>
            <Parrafo>Llamá enseguida. Informar el empeoramiento desde la app todavía no está disponible.</Parrafo>
            <Boton texto="Llamar a Recepción" icono="call-outline" variante="secundario" onPress={() => llamar(obtenerClinica().telefono)} />
            <Boton texto="Llamar a la guardia" icono="call-outline" variante="peligro" onPress={() => llamar(obtenerClinica().telefonoGuardia)} />
          </Tarjeta>
        )
      ) : null}

      <Tarjeta>
        <Subtitulo>Lo que enviaste</Subtitulo>
        {dato('Enviada', `${diaCorto(solicitud.fechaCreacion)} a las ${hora(solicitud.fechaCreacion)} · por ${solicitud.canal === 'App' ? 'la app' : 'teléfono'}`)}
        {dato('Modalidad', solicitud.modalidad)}
        {dato('Animal', `${animal?.nombre ?? 'Sin registrar'} · ${solicitud.especie}${solicitud.cantidad > 1 ? ` · ${solicitud.cantidad} afectados` : ''}`)}
        {dato('Síntomas', solicitud.sintomas)}
        {dato('Desde cuándo', solicitud.inicioSintomas)}
        {dato('Peso o tamaño', solicitud.pesoAproximado)}
        {dato('Riesgos', solicitud.riesgos)}
        {dato('Dirección', [solicitud.direccion, solicitud.referencias].filter(Boolean).join(' · '))}
        {dato('Teléfono de contacto', solicitud.telefonoContacto)}
        {dato(
          'Costo',
          solicitud.modalidad !== 'Visita'
            ? 'Sin costo'
            : solicitud.distanciaKm > 0
              ? `${formatearPrecio(solicitud.costo)} (${solicitud.distanciaKm} km)`
              : `Desde ${formatearPrecio(solicitud.costo)}, más la distancia`,
        )}
      </Tarjeta>

      <Boton texto="Mis solicitudes" icono="list-outline" variante="secundario" onPress={() => router.dismissTo('/asistencia')} />
    </Pantalla>
  );
}
