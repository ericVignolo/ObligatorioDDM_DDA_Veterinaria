import { useFocusEffect, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { EstadoConsentimiento } from '../../components/EstadoConsentimiento';
import { EstadoRefuerzo } from '../../components/EstadoRefuerzo';
import { FotoAnimal } from '../../components/FotoAnimal';
import { Pantalla } from '../../components/Pantalla';
import { Proximamente } from '../../components/Proximamente';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useSesion } from '../../context/SesionContext';
import { obtenerPorId as obtenerAnimal } from '../../services/animalService';
import { reevaluacionesPendientes } from '../../services/asistenciaService';
import type { SolicitudAsistencia } from '../../types/Asistencia';
import { estadoDe as estadoConsentimiento, pendientesDe } from '../../services/consentimientoService';
import { obtenerPorId as obtenerPrestacion } from '../../services/prestacionService';
import { alertasDe, DIAS_AVISO_REFUERZO, servicioPara, textoAlerta } from '../../services/vacunaService';
import type { Turno } from '../../types/Turno';
import { diaCorto, hora } from '../../utils/fechas';

// Avisos del Cliente: reevaluaciones de asistencia de 24 horas (B-014, RN-50), consentimientos sin firmar (B-012, CU-CLI-10, paso 1) y alertas sanitarias de refuerzos
// próximos o vencidos de sus animales (B-010, RF-CLI-07).
// Las notificaciones push y del Veterinario se completan en B-017; el correo lo manda la API (A-028).
export default function Avisos() {
  const router = useRouter();
  const { usuario } = useSesion();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const [alertas, setAlertas] = useState<ReturnType<typeof alertasDe>>([]);

  const [pendientes, setPendientes] = useState<Turno[]>([]);
  const [reevaluar, setReevaluar] = useState<SolicitudAsistencia[]>([]);

  useFocusEffect(
    useCallback(() => {
      setPendientes(clienteId === null ? [] : pendientesDe(clienteId));
      setReevaluar(clienteId === null ? [] : reevaluacionesPendientes(clienteId));
      setAlertas(clienteId === null ? [] : alertasDe(clienteId));
    }, [clienteId]),
  );

  if (usuario?.rol === 'Veterinario') return <Proximamente icono="notifications-outline" titulo="Avisos" tarea="B-017" />;
  if (clienteId === null) {
    return (
      <Pantalla>
        <Vacio icono="notifications-outline" titulo="Ingresá para ver tus avisos" accion={{ texto: 'Ingresar', onPress: () => router.navigate('/ingresar') }} />
      </Pantalla>
    );
  }

  return (
    <Pantalla>
      <Titulo>Avisos</Titulo>
      <Nota>Consentimientos sin firmar y refuerzos de vacunas y antiparasitarios vencidos o en los próximos {DIAS_AVISO_REFUERZO} días.</Nota>

      {reevaluar.map((s) => (
        <Tarjeta key={`asistencia-${s.id}`}>
          <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">Solicitud de asistencia n.º {s.id}</Text>
          <Parrafo>Pasaron 24 horas y sigue pendiente. ¿El estado se mantuvo o empeoró?</Parrafo>
          <Boton texto="Responder" icono="medkit-outline" onPress={() => router.push({ pathname: '/asistencia/[id]', params: { id: String(s.id) } })} />
        </Tarjeta>
      ))}

      {pendientes.map((t) => (
        <Tarjeta key={`consentimiento-${t.id}`}>
          <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">
            {obtenerAnimal(t.animalId, clienteId)?.nombre} · {obtenerPrestacion(t.prestacionId)?.nombre}
          </Text>
          <EstadoConsentimiento estado={estadoConsentimiento(t)} />
          <Parrafo>
            Turno del {diaCorto(t.inicio)} a las {hora(t.inicio)}: sin tu consentimiento la cirugía no se puede empezar.
          </Parrafo>
          <Boton texto="Leer y responder" icono="document-text-outline" onPress={() => router.push({ pathname: '/consentimiento/[turnoId]', params: { turnoId: String(t.id) } })} />
        </Tarjeta>
      ))}

      {alertas.length === 0 && pendientes.length > 0 ? null : alertas.length === 0 ? (
        <Vacio icono="shield-checkmark-outline" titulo="Todo al día" texto="Ningún animal tiene refuerzos vencidos ni próximos." />
      ) : (
        alertas.map(({ animal, item }) => (
          <Tarjeta key={`${animal.id}-${item.aplicacion.id}`}>
            <View className="flex-row items-center gap-3">
              <FotoAnimal uri={animal.foto} nombre={animal.nombre} decorativa />
              <View className="flex-1 gap-1">
                <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">
                  {animal.nombre} · {item.aplicacion.producto}
                </Text>
                <EstadoRefuerzo estado={item.estado} />
              </View>
            </View>
            <Parrafo>{textoAlerta(item, diaCorto(item.aplicacion.fechaRefuerzo))}</Parrafo>
            <View className="flex-row gap-2">
              <View className="flex-1">
                <Boton texto="Ver carné" variante="secundario" onPress={() => router.push(`/animales/${animal.id}`)} />
              </View>
              <View className="flex-1">
                <Boton
                  texto="Reservar"
                  icono="calendar-outline"
                  onPress={() => router.push({ pathname: '/reservar', params: { animal: String(animal.id), servicio: String(servicioPara(item.aplicacion.tipo)) } })}
                />
              </View>
            </View>
          </Tarjeta>
        ))
      )}
      <Nota>Con la API los refuerzos también te llegan por correo, según lo que elijas en tus preferencias.</Nota>
      <Boton texto="Preferencias de avisos" icono="options-outline" variante="secundario" onPress={() => router.push('/preferencias')} />
    </Pantalla>
  );
}
