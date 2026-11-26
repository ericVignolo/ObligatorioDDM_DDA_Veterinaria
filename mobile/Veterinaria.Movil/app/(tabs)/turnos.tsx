import { useFocusEffect, useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Campo } from '../../components/Campo';
import { EstadoConsentimiento } from '../../components/EstadoConsentimiento';
import { EstadoTurno } from '../../components/EstadoTurno';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useAviso } from '../../context/AvisoContext';
import { useSesion } from '../../context/SesionContext';
import { cancelar, HORAS_PARA_CANCELAR, historialDe, obtenerDe, proximosDe, puedeGestionar, resumen } from '../../services/agendaService';
import { obtenerPorId as obtenerAnimal } from '../../services/animalService';
import { llamar, obtenerClinica } from '../../services/clinicaService';
import { estadoDe as estadoConsentimiento } from '../../services/consentimientoService';
import type { Turno } from '../../types/Turno';
import { diaCorto, diaLargo, hora } from '../../utils/fechas';

// «Mis turnos» del Cliente (B-008, RF-CLI-04, A-016): próximos e historial, cancelar y reprogramar, como Turnos.razor.
export default function Turnos() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario } = useSesion();
  const { confirmado, reprogramado } = useLocalSearchParams<{ confirmado?: string; reprogramado?: string }>();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;

  const [proximos, setProximos] = useState<Turno[]>([]);
  const [historial, setHistorial] = useState<Turno[]>([]);
  const [verHistorial, setVerHistorial] = useState(false);
  const [cancelandoId, setCancelandoId] = useState(0);
  const [motivo, setMotivo] = useState('');
  const [errorMotivo, setErrorMotivo] = useState('');
  const [ocultarConstancia, setOcultarConstancia] = useState(false);

  const cargar = useCallback(() => {
    if (clienteId === null) return;
    setProximos(proximosDe(clienteId));
    setHistorial(historialDe(clienteId));
  }, [clienteId]);

  useFocusEffect(
    useCallback(() => {
      cargar();
      setOcultarConstancia(false);
    }, [cargar]),
  );

  if (clienteId === null) {
    return (
      <Pantalla>
        <Vacio icono="calendar-outline" titulo="Ingresá para ver tus turnos" accion={{ texto: 'Ingresar', onPress: () => router.navigate('/ingresar') }} />
      </Pantalla>
    );
  }

  const nombre = (t: Turno) => resumen(t, obtenerAnimal(t.animalId, clienteId));
  const idConstancia = Number(confirmado ?? reprogramado ?? 0);
  const constancia = !ocultarConstancia && idConstancia ? obtenerDe(idConstancia, clienteId) : undefined;

  const confirmarCancelacion = (id: number) => {
    if (motivo.length > 200) return setErrorMotivo('El motivo puede tener hasta 200 caracteres.');
    const r = cancelar(id, clienteId, motivo);
    avisar(r.mensaje, r.exito ? 'exito' : 'error');
    setCancelandoId(0);
    setOcultarConstancia(true);
    cargar();
  };

  return (
    <Pantalla>
      <Titulo>Mis turnos</Titulo>

      {/* Constancia después de reservar o reprogramar (A-015, A-016). */}
      {constancia ? (
        <View accessibilityLiveRegion="polite" className="gap-1 rounded-tarjeta bg-exito-suave p-4 dark:bg-exito-suave-oscuro">
          <Text className="font-nunito-negrita text-base text-exito dark:text-exito-oscuro">
            {reprogramado ? '¡Turno reprogramado!' : constancia.estado === 'Pendiente de seña' ? '¡Horario reservado!' : '¡Turno confirmado!'}
          </Text>
          <Parrafo>
            {nombre(constancia)}, el {diaLargo(constancia.inicio)} a las {hora(constancia.inicio)}. Número de turno: {constancia.id}.
          </Parrafo>
          {constancia.estado === 'Pendiente de seña' ? <Parrafo>La cirugía se confirma cuando pagues la seña en Recepción.</Parrafo> : null}
          <Nota>Podés cancelar o reprogramar sin costo hasta {HORAS_PARA_CANCELAR} horas antes.</Nota>
        </View>
      ) : null}

      <Boton texto="Reservar turno" icono="add-circle-outline" onPress={() => router.push('/reservar')} />

      {/* Pestañas Próximos / Historial */}
      <View accessibilityRole="tablist" className="flex-row rounded-boton border border-borde p-1 dark:border-borde-oscuro">
        {[
          { texto: `Próximos (${proximos.length})`, activo: !verHistorial, onPress: () => setVerHistorial(false) },
          { texto: `Historial (${historial.length})`, activo: verHistorial, onPress: () => setVerHistorial(true) },
        ].map((p) => (
          <Pressable
            key={p.texto}
            onPress={p.onPress}
            accessibilityRole="tab"
            accessibilityState={{ selected: p.activo }}
            aria-selected={p.activo}
            className={`min-h-12 flex-1 items-center justify-center rounded-boton ${p.activo ? 'bg-primario dark:bg-primario-oscuro' : ''}`}
          >
            <Text className={`font-nunito-semi text-base ${p.activo ? 'text-sobre-primario dark:text-sobre-primario-oscuro' : 'text-texto dark:text-texto-oscuro'}`}>{p.texto}</Text>
          </Pressable>
        ))}
      </View>

      {!verHistorial ? (
        proximos.length === 0 ? (
          <Vacio icono="calendar-outline" titulo="No tenés turnos próximos" texto="Reservá uno en cuatro pasos." />
        ) : (
          proximos.map((t) => (
            <Tarjeta key={t.id}>
              <Subtitulo>
                {diaLargo(t.inicio)}, {hora(t.inicio)}
              </Subtitulo>
              <Parrafo>{nombre(t)}</Parrafo>
              <EstadoTurno estado={t.estado} />
              <EstadoConsentimiento estado={estadoConsentimiento(t)} />
              {t.estado === 'Pendiente de seña' ? <Nota>Se confirma cuando pagues la seña en Recepción. El consentimiento se firma antes del día del turno.</Nota> : null}
              {estadoConsentimiento(t) !== 'No requiere' ? (
                <Boton
                  texto={estadoConsentimiento(t) === 'Firmado' ? 'Ver el consentimiento firmado' : 'Leer y firmar el consentimiento'}
                  icono="document-text-outline"
                  variante={estadoConsentimiento(t) === 'Firmado' ? 'secundario' : 'acento'}
                  onPress={() => router.push({ pathname: '/consentimiento/[turnoId]', params: { turnoId: String(t.id) } })}
                />
              ) : null}

              {cancelandoId === t.id ? (
                // Confirmación antes de cancelar, con motivo opcional (A-016).
                <View className="gap-2 rounded-boton bg-superficie-2 p-3 dark:bg-superficie-2-oscuro">
                  <Parrafo>¿Cancelar este turno? El horario queda libre para otros.</Parrafo>
                  <Campo etiqueta="Motivo" opcional value={motivo} onChangeText={(v) => { setMotivo(v); setErrorMotivo(''); }} error={errorMotivo} maxLength={200} />
                  <Boton texto="Sí, cancelar" variante="peligro" onPress={() => confirmarCancelacion(t.id)} />
                  <Boton texto="No, volver" variante="secundario" onPress={() => setCancelandoId(0)} />
                </View>
              ) : puedeGestionar(t) ? (
                <View className="flex-row gap-2">
                  <View className="flex-1">
                    <Boton texto="Reprogramar" icono="swap-horizontal-outline" variante="secundario" onPress={() => router.push({ pathname: '/reservar', params: { reprogramar: String(t.id) } })} />
                  </View>
                  <View className="flex-1">
                    <Boton texto="Cancelar" icono="close-circle-outline" variante="secundario" onPress={() => { setCancelandoId(t.id); setMotivo(''); setErrorMotivo(''); }} />
                  </View>
                </View>
              ) : (
                // RN-06: dentro de las 24 h interviene la clínica.
                <>
                  <Nota>Faltan menos de {HORAS_PARA_CANCELAR} horas: para cancelar o cambiar el turno, llamá a Recepción.</Nota>
                  <Boton texto="Llamar a Recepción" icono="call-outline" variante="secundario" onPress={() => llamar(obtenerClinica().telefono)} />
                </>
              )}
            </Tarjeta>
          ))
        )
      ) : historial.length === 0 ? (
        <Vacio icono="time-outline" titulo="Todavía no hay turnos pasados ni cancelados" />
      ) : (
        historial.map((t) => (
          <Tarjeta key={t.id}>
            <Parrafo suave>
              {diaCorto(t.inicio)} · {hora(t.inicio)}
            </Parrafo>
            <Parrafo>{nombre(t)}</Parrafo>
            <EstadoTurno estado={t.estado} pasado={t.fin < Date.now()} />
            {t.motivoCancelacion ? <Nota>Motivo: {t.motivoCancelacion}</Nota> : null}
          </Tarjeta>
        ))
      )}
    </Pantalla>
  );
}
