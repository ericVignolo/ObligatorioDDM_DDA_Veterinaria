import { useLocalSearchParams, useRouter, Stack } from 'expo-router';
import { useMemo, useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { Boton } from '../components/Boton';
import { FotoAnimal } from '../components/FotoAnimal';
import { Opcion } from '../components/Opcion';
import { Pantalla } from '../components/Pantalla';
import { PasosReserva } from '../components/PasosReserva';
import { Tarjeta } from '../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../components/Textos';
import { Vacio } from '../components/Vacio';
import { useSesion } from '../context/SesionContext';
import {
  bloqueadoPorAusencias,
  DIAS_HACIA_ADELANTE,
  HORAS_DE_ANTICIPACION,
  HORAS_PARA_CANCELAR,
  horariosLibres,
  mensajeAusencias,
  motivoSinHorarios,
  obtenerDe,
  ocuparComoOtroCliente,
  PORCENTAJE_SENA,
  primerDia,
  profesionalesPara,
  proximoDiaConHorarios,
  puedeGestionar,
  reprogramar as reprogramarTurno,
  reservables,
  reservar,
  ultimoDia,
} from '../services/agendaService';
import { obtenerDe as animalesDe } from '../services/animalService';
import { descartarIntencion } from '../services/intencionService';
import { formatearPrecio } from '../services/prestacionService';
import { chipDia, diaLargo, hora, inicioDelDia, mismoDia, sumarDias } from '../utils/fechas';

// Reserva en cuatro pasos (B-008, CU-CLI-02, FLUJO_RESERVA_CONSULTA): animal, servicio y profesional, fecha y hora, confirmación.
// Con ?reprogramar=<turno> se conservan animal, servicio y profesional y solo se elige el nuevo horario (A-016).
export default function Reservar() {
  const router = useRouter();
  const { usuario } = useSesion();
  const params = useLocalSearchParams<{ servicio?: string; animal?: string; reprogramar?: string }>();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;

  const animales = useMemo(() => (clienteId === null ? [] : animalesDe(clienteId)), [clienteId]);
  const servicios = useMemo(() => reservables(), []);
  const reprogramando = useMemo(() => {
    if (clienteId === null || !params.reprogramar) return undefined;
    return obtenerDe(Number(params.reprogramar), clienteId);
  }, [clienteId, params.reprogramar]);

  // P-RC-01: con un solo animal, el paso 1 ya viene elegido. El servicio puede venir del catálogo.
  const animalInicial = reprogramando?.animalId ?? (params.animal ? Number(params.animal) : animales.length === 1 ? animales[0].id : 0);
  const servicioInicial = reprogramando?.prestacionId ?? (servicios.some((s) => s.id === Number(params.servicio)) ? Number(params.servicio) : 0);

  const [paso, setPaso] = useState(reprogramando ? 3 : 1);
  const [animalId, setAnimalId] = useState(animales.some((a) => a.id === animalInicial) ? animalInicial : 0);
  const [prestacionId, setPrestacionId] = useState(servicioInicial);
  // Si hay un solo profesional para ese servicio y esa especie, se elige solo (también si el servicio vino del catálogo).
  const unicoProfesional = (idAnimal: number, idServicio: number) => {
    const a = animales.find((x) => x.id === idAnimal);
    const lista = a && idServicio ? profesionalesPara(idServicio, a.especie) : [];
    return lista.length === 1 ? lista[0].id : 0;
  };
  const [profesionalId, setProfesionalId] = useState(reprogramando?.profesionalId ?? unicoProfesional(animalInicial, servicioInicial));
  const [dia, setDia] = useState<Date>(() => {
    const d = reprogramando ? inicioDelDia(reprogramando.inicio) : primerDia();
    return d < primerDia() ? primerDia() : d;
  });
  const [inicio, setInicio] = useState<number | null>(null);
  const [aviso, setAviso] = useState('');
  const [avisoSalto, setAvisoSalto] = useState('');
  const [version, setVersion] = useState(0); // fuerza a recalcular horarios tras un cambio de agenda

  const animal = animales.find((a) => a.id === animalId);
  const servicio = servicios.find((s) => s.id === prestacionId);
  const profesionales = animal && prestacionId ? profesionalesPara(prestacionId, animal.especie) : [];
  const profesional = profesionales.find((p) => p.id === profesionalId);
  const excluir = reprogramando?.id ?? 0;

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const horarios = useMemo(() => (profesional && animal ? horariosLibres(profesional.id, prestacionId, animal.id, dia, excluir) : []), [profesionalId, prestacionId, animalId, dia, excluir, version]);

  // Días del horizonte con su cantidad de horarios libres, para las fichas de días.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const dias = useMemo(() => {
    if (!profesional || !animal) return [];
    const lista: { dia: Date; libres: number }[] = [];
    for (let d = primerDia(); d <= ultimoDia(); d = sumarDias(d, 1)) lista.push({ dia: d, libres: horariosLibres(profesional.id, prestacionId, animal.id, d, excluir).length });
    return lista;
  }, [profesionalId, prestacionId, animalId, excluir, version]);

  if (clienteId === null) {
    return (
      <Pantalla>
        <Vacio icono="calendar-outline" titulo="Ingresá para reservar un turno" accion={{ texto: 'Ingresar', onPress: () => router.replace('/ingresar') }} />
      </Pantalla>
    );
  }

  if (params.reprogramar && (!reprogramando || !puedeGestionar(reprogramando))) {
    return (
      <Pantalla>
        <Vacio
          icono="time-outline"
          titulo="Este turno ya no se puede reprogramar"
          texto={`Si faltan menos de ${HORAS_PARA_CANCELAR} horas, consultá en Recepción.`}
          accion={{ texto: 'Volver a mis turnos', onPress: () => router.back() }}
        />
      </Pantalla>
    );
  }

  const irAPaso = (numero: number) => {
    setAviso('');
    if (numero === 3) entrarAFecha();
    setPaso(numero);
  };

  // Al entrar al paso 3, si el día no tiene lugar (por ejemplo, una cirugía de 90 min), se salta al primero con horarios.
  const entrarAFecha = () => {
    setAvisoSalto('');
    if (!profesional || !animal) return;
    if (horariosLibres(profesional.id, prestacionId, animal.id, dia, excluir).length > 0) return;
    const siguiente = proximoDiaConHorarios(profesional.id, prestacionId, animal.id, dia, excluir);
    if (siguiente) {
      setAvisoSalto(`El ${diaLargo(dia)} no queda lugar para este servicio. Te mostramos el primer día con horarios libres.`);
      setDia(siguiente);
      setInicio(null);
    }
  };

  const elegirServicio = (id: number) => {
    setPrestacionId(id);
    // Si el profesional ya no corresponde se quita; si hay uno solo, se elige.
    const lista = animal ? profesionalesPara(id, animal.especie) : [];
    setProfesionalId(lista.some((p) => p.id === profesionalId) ? profesionalId : lista.length === 1 ? lista[0].id : 0);
    setInicio(null);
  };

  const elegirAnimal = (id: number) => {
    setAnimalId(id);
    setProfesionalId(unicoProfesional(id, prestacionId));
    setInicio(null);
  };

  const confirmar = () => {
    if (!inicio || !animal || !profesional) return;
    let id = reprogramando?.id ?? 0;
    let r;
    if (reprogramando) {
      r = reprogramarTurno(reprogramando.id, clienteId, inicio);
    } else {
      const reserva = reservar(clienteId, animal.id, prestacionId, profesional.id, inicio);
      id = reserva.turno?.id ?? 0;
      r = reserva;
    }
    if (!r.exito) {
      // Se mantiene lo elegido y se muestran los horarios actualizados.
      setInicio(null);
      setVersion((v) => v + 1);
      setPaso(3);
      setAviso(r.mensaje);
      return;
    }
    descartarIntencion();
    router.replace({ pathname: '/turnos', params: reprogramando ? { reprogramado: String(id) } : { confirmado: String(id) } });
  };

  const alertaAusencias = !reprogramando ? mensajeAusencias(clienteId) : '';
  const titulo = reprogramando ? 'Reprogramar turno' : 'Reservar turno';

  return (
    <Pantalla>
      <Stack.Screen options={{ title: titulo }} />
      <Titulo>{titulo}</Titulo>
      <PasosReserva actual={paso} />

      {alertaAusencias ? (
        <View accessibilityLiveRegion="polite" className={`rounded-boton p-3 ${bloqueadoPorAusencias(clienteId) ? 'bg-peligro-suave dark:bg-peligro-suave-oscuro' : 'bg-aviso-suave dark:bg-aviso-suave-oscuro'}`}>
          <Parrafo>{alertaAusencias}</Parrafo>
        </View>
      ) : null}
      {aviso ? (
        <View accessibilityLiveRegion="assertive" className="rounded-boton bg-aviso-suave p-3 dark:bg-aviso-suave-oscuro">
          <Text className="font-nunito-semi text-base text-texto dark:text-texto-oscuro">{aviso}</Text>
        </View>
      ) : null}

      {/* Paso 1 — Animal */}
      {paso === 1 ? (
        <>
          <Subtitulo>¿Para qué animal es el turno?</Subtitulo>
          {animales.length === 0 ? (
            <Vacio
              icono="paw-outline"
              titulo="Todavía no registraste ningún animal"
              texto="Registralo y volvés acá. No cuenta como un paso más."
              accion={{
                texto: 'Registrar animal',
                onPress: () => router.push({ pathname: '/animales/nuevo', params: { volver: 'reservar', ...(prestacionId ? { servicio: String(prestacionId) } : {}) } }),
              }}
            />
          ) : (
            <>
              {animales.map((a) => (
                <Pressable
                  key={a.id}
                  onPress={() => elegirAnimal(a.id)}
                  accessibilityRole="radio"
                  accessibilityState={{ checked: a.id === animalId }}
                  aria-checked={a.id === animalId}
                  accessibilityLabel={`${a.nombre}, ${a.especie}`}
                  className={`min-h-12 flex-row items-center gap-3 rounded-boton border p-2 ${
                    a.id === animalId ? 'border-primario bg-primario-suave dark:border-primario-oscuro dark:bg-primario-suave-oscuro' : 'border-borde bg-superficie dark:border-borde-oscuro dark:bg-superficie-oscuro'
                  }`}
                >
                  <FotoAnimal uri={a.foto} nombre={a.nombre} decorativa />
                  <View className="flex-1">
                    <Text className="font-nunito-semi text-base text-texto dark:text-texto-oscuro">{a.nombre}</Text>
                    <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">{a.esGrupo ? `${a.especie} (grupo de ${a.cantidadGrupo})` : a.especie}</Text>
                  </View>
                </Pressable>
              ))}
              <Boton texto="Siguiente" onPress={() => irAPaso(2)} deshabilitado={!animalId} />
            </>
          )}
        </>
      ) : null}

      {/* Paso 2 — Servicio y profesional: solo quienes hacen ese servicio y atienden esa especie */}
      {paso === 2 && animal ? (
        <>
          <Subtitulo>Servicio para {animal.nombre}</Subtitulo>
          {servicios.map((s) => (
            <Opcion key={s.id} titulo={s.nombre} detalle={`${s.duracionMinutos} min · desde ${formatearPrecio(s.precioDesde)}`} elegida={s.id === prestacionId} onPress={() => elegirServicio(s.id)} />
          ))}
          {prestacionId ? (
            profesionales.length === 0 ? (
              <Tarjeta>
                <Parrafo>Ningún profesional hace este servicio para {animal.especie.toLowerCase()}. Elegí otro servicio o consultá en la clínica.</Parrafo>
              </Tarjeta>
            ) : (
              <>
                <Subtitulo>Profesional</Subtitulo>
                {profesionales.map((p) => (
                  <Opcion key={p.id} titulo={p.nombre} detalle={p.especialidad} elegida={p.id === profesionalId} onPress={() => { setProfesionalId(p.id); setInicio(null); }} />
                ))}
              </>
            )
          ) : null}
          <View className="flex-row gap-2">
            <View className="flex-1"><Boton texto="Volver" variante="secundario" onPress={() => irAPaso(1)} /></View>
            <View className="flex-1"><Boton texto="Siguiente" onPress={() => irAPaso(3)} deshabilitado={!prestacionId || !profesional} /></View>
          </View>
        </>
      ) : null}

      {/* Paso 3 — Fecha y hora */}
      {paso === 3 && animal && servicio && profesional ? (
        <>
          <Subtitulo>Fecha y hora</Subtitulo>
          {reprogramando ? (
            <Parrafo>
              Horario actual: <Text className="font-nunito-negrita">{diaLargo(reprogramando.inicio)}, {hora(reprogramando.inicio)}</Text>. Elegí el nuevo.
            </Parrafo>
          ) : null}
          <Nota>
            Disponibilidad de demostración. Mínimo {HORAS_DE_ANTICIPACION} h de anticipación y hasta {DIAS_HACIA_ADELANTE} días.
          </Nota>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerClassName="gap-2 py-1" accessibilityLabel="Días">
            {dias.map(({ dia: d, libres }) => {
              const elegido = mismoDia(d, dia);
              const { semana, fecha } = chipDia(d);
              return (
                <Pressable
                  key={d.getTime()}
                  onPress={() => { setDia(d); setInicio(null); setAvisoSalto(''); }}
                  accessibilityRole="button"
                  accessibilityState={{ selected: elegido }}
                  aria-selected={elegido}
                  accessibilityLabel={`${diaLargo(d)}, ${libres === 0 ? 'sin horarios' : `${libres} horarios libres`}`}
                  className={`min-h-16 w-16 items-center justify-center rounded-boton border ${
                    elegido ? 'border-primario bg-primario dark:border-primario-oscuro dark:bg-primario-oscuro' : 'border-borde bg-superficie dark:border-borde-oscuro dark:bg-superficie-oscuro'
                  } ${libres === 0 && !elegido ? 'opacity-40' : ''}`}
                >
                  <Text className={`font-nunito-semi text-xs ${elegido ? 'text-sobre-primario dark:text-sobre-primario-oscuro' : 'text-texto-suave dark:text-texto-suave-oscuro'}`}>{semana}</Text>
                  <Text className={`font-nunito-negrita text-sm ${elegido ? 'text-sobre-primario dark:text-sobre-primario-oscuro' : 'text-texto dark:text-texto-oscuro'}`}>{fecha}</Text>
                </Pressable>
              );
            })}
          </ScrollView>
          <Parrafo suave>{diaLargo(dia)}</Parrafo>
          {avisoSalto ? (
            <View accessibilityLiveRegion="polite" className="rounded-boton bg-info-suave p-3 dark:bg-info-suave-oscuro">
              <Text className="font-nunito text-base text-info dark:text-info-oscuro">{avisoSalto}</Text>
            </View>
          ) : null}
          {horarios.length === 0 ? (
            <Tarjeta>
              <Parrafo>{motivoSinHorarios(profesional.id, dia)}</Parrafo>
              {(() => {
                const proximo = proximoDiaConHorarios(profesional.id, prestacionId, animal.id, sumarDias(dia, 1), excluir);
                return proximo ? <Boton texto={`Ir al ${diaLargo(proximo)}`} variante="secundario" onPress={() => { setDia(proximo); setInicio(null); }} /> : null;
              })()}
            </Tarjeta>
          ) : (
            <>
              <Nota>
                Horarios libres con {profesional.nombre} ({servicio.duracionMinutos} min)
              </Nota>
              <View className="flex-row flex-wrap gap-2">
                {horarios.map((h) => (
                  <Pressable
                    key={h}
                    onPress={() => setInicio(h)}
                    accessibilityRole="radio"
                    accessibilityState={{ checked: h === inicio }}
                    aria-checked={h === inicio}
                    accessibilityLabel={hora(h)}
                    className={`min-h-12 min-w-20 items-center justify-center rounded-boton border px-3 ${
                      h === inicio ? 'border-primario bg-primario dark:border-primario-oscuro dark:bg-primario-oscuro' : 'border-primario bg-transparent dark:border-primario-oscuro'
                    }`}
                  >
                    <Text className={`font-nunito-semi text-base ${h === inicio ? 'text-sobre-primario dark:text-sobre-primario-oscuro' : 'text-primario dark:text-primario-oscuro'}`}>{hora(h)}</Text>
                  </Pressable>
                ))}
              </View>
            </>
          )}
          <View className="flex-row gap-2">
            <View className="flex-1">
              <Boton texto={reprogramando ? 'Cancelar cambio' : 'Volver'} variante="secundario" onPress={() => (reprogramando ? router.back() : irAPaso(2))} />
            </View>
            <View className="flex-1"><Boton texto="Siguiente" onPress={() => irAPaso(4)} deshabilitado={!inicio} /></View>
          </View>
        </>
      ) : null}

      {/* Paso 4 — Revisión y confirmación */}
      {paso === 4 && animal && servicio && profesional && inicio ? (
        <>
          <Subtitulo>Revisá y confirmá</Subtitulo>
          <Tarjeta>
            <Fila etiqueta="Animal" valor={`${animal.nombre} (${animal.especie})`} />
            <Fila etiqueta="Servicio" valor={`${servicio.nombre} · ${servicio.duracionMinutos} min`} />
            <Fila etiqueta="Profesional" valor={profesional.nombre} />
            {reprogramando ? <Fila etiqueta="Horario actual" valor={`${diaLargo(reprogramando.inicio)}, ${hora(reprogramando.inicio)}`} /> : null}
            <Fila etiqueta={reprogramando ? 'Nuevo horario' : 'Día y hora'} valor={`${diaLargo(inicio)}, ${hora(inicio)} a ${hora(inicio + (servicio.duracionMinutos ?? 0) * 60000)}`} />
            <Fila etiqueta="Precio" valor={`Desde ${formatearPrecio(servicio.precioDesde)}. El importe final se calcula con la tarifa vigente el día de la atención.`} />
          </Tarjeta>
          <Nota>Podés cancelar o reprogramar sin costo hasta {HORAS_PARA_CANCELAR} horas antes. Después, se hace por Recepción.</Nota>
          {!reprogramando && servicio.requiereConsentimiento ? (
            <View className="rounded-boton bg-info-suave p-3 dark:bg-info-suave-oscuro">
              <Text className="font-nunito text-base text-info dark:text-info-oscuro">
                Esta cirugía pide una seña del {PORCENTAJE_SENA} % del precio, que se paga en Recepción. Hasta pagarla, el horario queda reservado pero el turno no está confirmado. También hay que firmar el consentimiento informado antes del día del turno.
              </Text>
            </View>
          ) : null}
          <View className="flex-row gap-2">
            <View className="flex-1"><Boton texto="Volver" variante="secundario" onPress={() => irAPaso(3)} /></View>
            <View className="flex-1"><Boton texto={reprogramando ? 'Confirmar cambio' : 'Confirmar turno'} onPress={confirmar} /></View>
          </View>
          {/* SOLO DEMOSTRACIÓN: permite probar qué pasa si otro cliente toma el horario mientras confirmás. */}
          <Pressable accessibilityRole="button" onPress={() => profesional && ocuparComoOtroCliente(profesional.id, prestacionId, inicio)} className="min-h-12 justify-center active:opacity-70">
            <Text className="font-nunito text-sm text-primario underline dark:text-primario-oscuro">Demostración: simular que otro cliente reservó este horario</Text>
          </Pressable>
        </>
      ) : null}
    </Pantalla>
  );
}

function Fila({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return (
    <View className="gap-0.5 border-b border-borde py-2 dark:border-borde-oscuro">
      <Text className="font-nunito-semi text-sm text-texto-suave dark:text-texto-suave-oscuro">{etiqueta}</Text>
      <Text className="font-nunito text-base text-texto dark:text-texto-oscuro">{valor}</Text>
    </View>
  );
}
