import { useFocusEffect, useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { InformarEmpeoramiento } from '../../components/InformarEmpeoramiento';
import { Insignia } from '../../components/Insignia';
import { LlamarGuardia } from '../../components/LlamarGuardia';
import { MapaDemo } from '../../components/MapaDemo';
import { NivelAsistencia } from '../../components/NivelAsistencia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useAviso } from '../../context/AvisoContext';
import { useSesion } from '../../context/SesionContext';
import { obtenerPorId as obtenerAnimal } from '../../services/animalService';
import {
  compartiendoUbicacion,
  estaActiva,
  explicacionEstado,
  historialDe,
  obtenerDe,
  pideReevaluacion,
  responderSeMantiene,
  salidaDe,
  simularAvance,
} from '../../services/asistenciaService';
import { llamar, obtenerClinica } from '../../services/clinicaService';
import { formatearPrecio } from '../../services/prestacionService';
import { diaCorto, hora } from '../../utils/fechas';

const SEGUNDOS_ACTUALIZAR = 15;

// Seguimiento de una solicitud (B-013 y B-014, CU-CLI-19 paso 10, RF-CLI-23 y RF-CLI-24): nivel, estado real,
// veterinario y llegada estimada solo después de aceptar (RN-53), mapa durante el traslado (RN-57),
// reevaluación de las 24 horas (RN-50), informar empeoramiento (RN-51) e historial.
// La pantalla se actualiza sola cada 15 segundos mientras está abierta; con la API llegan además notificaciones.
export default function DetalleAsistencia() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario } = useSesion();
  const { id, nueva } = useLocalSearchParams<{ id: string; nueva?: string }>();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const [ahora, setAhora] = useState(Date.now());
  const [empeorando, setEmpeorando] = useState(false);
  const refrescar = () => setAhora(Date.now());

  useFocusEffect(
    useCallback(() => {
      setAhora(Date.now());
      const reloj = setInterval(() => setAhora(Date.now()), SEGUNDOS_ACTUALIZAR * 1000);
      return () => clearInterval(reloj);
    }, []),
  );

  const solicitud = clienteId === null ? undefined : obtenerDe(Number(id), clienteId);

  // RN-18: no se distingue entre «no existe» y «no es tuya».
  if (clienteId === null || !solicitud) {
    return (
      <Pantalla>
        <Vacio icono="medkit-outline" titulo="Solicitud no disponible" texto="No encontramos esa solicitud entre las tuyas." accion={{ texto: 'Ir al inicio', onPress: () => router.navigate('/') }} />
      </Pantalla>
    );
  }

  const activa = estaActiva(solicitud);
  const salida = salidaDe(solicitud.id);
  const historial = historialDe(solicitud.id, clienteId);
  const animal = solicitud.animalId ? obtenerAnimal(solicitud.animalId, clienteId) : undefined;
  const dato = (etiqueta: string, valor: string) =>
    valor ? (
      <View className="gap-0.5">
        <Text className="font-nunito-semi text-sm text-texto-suave dark:text-texto-suave-oscuro">{etiqueta}</Text>
        <Parrafo>{valor}</Parrafo>
      </View>
    ) : null;

  const seMantiene = () => {
    const r = responderSeMantiene(solicitud.id, clienteId);
    avisar(r.mensaje, r.exito ? 'exito' : 'error');
    refrescar();
  };

  const avanzarDemo = () => {
    const r = simularAvance(solicitud.id, clienteId);
    avisar(r.mensaje, r.exito ? 'info' : 'error');
    refrescar();
  };

  return (
    <Pantalla>
      <Titulo>{nueva ? 'Recibimos tu solicitud' : `Solicitud n.º ${solicitud.id}`}</Titulo>

      <Tarjeta>
        {nueva ? <Nota>Solicitud n.º {solicitud.id}</Nota> : null}
        <NivelAsistencia nivel={solicitud.nivel} />
        <Insignia texto={solicitud.estado} tipo={solicitud.estado === 'Requiere coordinación de Recepción' ? 'aviso' : activa ? 'info' : 'neutro'} />
        <Parrafo>{explicacionEstado(solicitud)}</Parrafo>
        {solicitud.etaMinutos !== null && solicitud.estado === 'Aceptada' ? (
          <Nota>Tiempo estimado de viaje cuando salga: {solicitud.etaMinutos} min. Es una estimación.</Nota>
        ) : null}
        {compartiendoUbicacion(solicitud) && salida !== null && solicitud.etaMinutos !== null ? (
          <MapaDemo salida={salida} etaMinutos={solicitud.etaMinutos} ahora={ahora} destino={solicitud.direccion || 'Tu ubicación'} />
        ) : null}
        <Nota>Actualizado a las {hora(ahora)}.</Nota>
        <Boton texto="Actualizar estado" icono="refresh-outline" variante="secundario" onPress={refrescar} />
      </Tarjeta>

      {pideReevaluacion(solicitud) && !empeorando ? (
        <View accessibilityRole="alert" className="gap-3 rounded-tarjeta bg-aviso-suave p-4 dark:bg-aviso-suave-oscuro">
          <Subtitulo>Pasaron 24 horas: ¿cómo sigue?</Subtitulo>
          <Parrafo>Contanos si el estado se mantuvo o empeoró. Si no respondés, la solicitud sigue igual: no se cierra ni baja de nivel.</Parrafo>
          <Boton texto="Se mantiene igual" icono="checkmark-outline" variante="secundario" onPress={seMantiene} />
          <Boton texto="Empeoró" icono="trending-up-outline" variante="peligro" onPress={() => setEmpeorando(true)} />
        </View>
      ) : null}

      {activa && solicitud.nivel === 'Rojo' ? <LlamarGuardia texto="Es un caso rojo: llamá también por teléfono, sobre todo si empeora." /> : null}

      {activa ? (
        <Tarjeta>
          {empeorando ? (
            <InformarEmpeoramiento
              solicitudId={solicitud.id}
              clienteId={clienteId}
              nivelActual={solicitud.nivel}
              onListo={() => {
                setEmpeorando(false);
                refrescar();
              }}
              onCancelar={() => setEmpeorando(false)}
            />
          ) : (
            <>
              <Subtitulo>¿Empeoró?</Subtitulo>
              <Parrafo>Avisanos en cualquier momento. Si lo ves más grave, la solicitud sube de prioridad.</Parrafo>
              <Boton texto="Informar empeoramiento" icono="alert-circle-outline" variante="peligro" onPress={() => setEmpeorando(true)} />
              {solicitud.nivel !== 'Rojo' ? (
                <Boton texto="Llamar a Recepción" icono="call-outline" variante="secundario" onPress={() => llamar(obtenerClinica().telefono)} />
              ) : null}
            </>
          )}
        </Tarjeta>
      ) : null}

      {/* SOLO DEMOSTRACIÓN: en la versión final avanzan Recepción y el veterinario desde sus pantallas. */}
      {activa ? (
        <Tarjeta>
          <Insignia texto="Demostración" tipo="aviso" />
          <Nota>Simula lo que harían Recepción y el veterinario, para ver el seguimiento.</Nota>
          <Boton texto="Simular el siguiente paso" icono="play-forward-outline" variante="secundario" onPress={avanzarDemo} />
        </Tarjeta>
      ) : null}

      <Tarjeta>
        <Subtitulo>Lo que enviaste</Subtitulo>
        {dato('Enviada', `${diaCorto(solicitud.fechaCreacion)} a las ${hora(solicitud.fechaCreacion)} · por ${solicitud.canal === 'App' ? 'la app' : 'teléfono'}`)}
        {dato('Modalidad', solicitud.modalidad)}
        {dato('Animal', `${animal?.nombre ?? 'Sin registrar'} · ${solicitud.especie}${solicitud.cantidad > 1 ? ` · ${solicitud.cantidad} afectados` : ''}`)}
        {dato('Gravedad que indicaste', solicitud.nivelCliente)}
        {dato('Síntomas', solicitud.sintomas)}
        {dato('Desde cuándo', solicitud.inicioSintomas)}
        {dato('Peso o tamaño', solicitud.pesoAproximado)}
        {dato('Riesgos', solicitud.riesgos)}
        {dato('Dirección', [solicitud.direccion, solicitud.referencias].filter(Boolean).join(' · '))}
        {solicitud.ubicacionGps ? dato('Ubicación del celular', solicitud.latitud !== null ? 'Compartida para esta asistencia' : 'Ya descartada') : null}
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

      <Tarjeta>
        <Subtitulo>Historial</Subtitulo>
        {historial.map((e) => (
          <View key={e.id} className="gap-0.5 border-l-2 border-borde pl-3 dark:border-borde-oscuro">
            <Text className="font-nunito-semi text-sm text-texto dark:text-texto-oscuro">
              {e.tipo} · {diaCorto(e.fecha)} {hora(e.fecha)}
            </Text>
            {e.texto ? <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">{e.texto}</Text> : null}
          </View>
        ))}
      </Tarjeta>

      <Boton texto="Mis solicitudes" icono="list-outline" variante="secundario" onPress={() => router.dismissTo('/asistencia')} />
    </Pantalla>
  );
}
