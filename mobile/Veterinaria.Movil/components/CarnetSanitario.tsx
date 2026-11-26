import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { AVISO_CATALOGO_DEMO, carnetDe, DIAS_AVISO_REFUERZO, historialDe, servicioPara, textoAlerta } from '../services/vacunaService';
import { diaCorto } from '../utils/fechas';
import { Boton } from './Boton';
import { EstadoRefuerzo } from './EstadoRefuerzo';
import { Icono } from './Icono';
import { Nota, Parrafo, Subtitulo } from './Textos';

type Props = { animalId: number; clienteId: number; nombre: string };

// Carné de vacunas y antiparasitarios de un animal (B-010, RF-CLI-07), solo lectura, como CarnetSanitario.razor:
// alertas arriba, la última aplicación de cada producto con su estado y, plegado, el historial completo.
export function CarnetSanitario({ animalId, clienteId, nombre }: Props) {
  const router = useRouter();
  const [verTodo, setVerTodo] = useState(false);
  const carnet = carnetDe(animalId, clienteId);
  const historial = historialDe(animalId, clienteId);
  const alertas = carnet.filter((i) => i.estado !== 'Vigente');

  return (
    <View className="gap-3 rounded-tarjeta border border-borde bg-superficie p-4 dark:border-borde-oscuro dark:bg-superficie-oscuro">
      <Subtitulo>Vacunas y antiparasitarios</Subtitulo>

      {carnet.length === 0 ? (
        <Parrafo suave>Todavía no hay vacunas ni antiparasitarios registrados para {nombre}.</Parrafo>
      ) : (
        <>
          {alertas.map((item) => (
            <View
              key={item.aplicacion.id}
              accessibilityLiveRegion="polite"
              className={`gap-2 rounded-boton p-3 ${item.estado === 'Vencida' ? 'bg-peligro-suave dark:bg-peligro-suave-oscuro' : 'bg-aviso-suave dark:bg-aviso-suave-oscuro'}`}
            >
              <Text className="font-nunito text-base text-texto dark:text-texto-oscuro">
                <Text className="font-nunito-negrita">{item.aplicacion.producto}: </Text>
                {textoAlerta(item, diaCorto(item.aplicacion.fechaRefuerzo))}
              </Text>
              <Boton
                texto="Reservar turno"
                icono="calendar-outline"
                variante="secundario"
                onPress={() => router.push({ pathname: '/reservar', params: { animal: String(animalId), servicio: String(servicioPara(item.aplicacion.tipo)) } })}
              />
            </View>
          ))}

          {carnet.map((item) => (
            <View key={item.aplicacion.id} className="gap-1 border-b border-borde pb-3 dark:border-borde-oscuro">
              <View className="flex-row flex-wrap items-center justify-between gap-2">
                <Text className="flex-1 font-nunito-semi text-base text-texto dark:text-texto-oscuro">{item.aplicacion.producto}</Text>
                <EstadoRefuerzo estado={item.estado} />
              </View>
              <Nota>
                {item.aplicacion.tipo} · {item.aplicacion.laboratorio}
              </Nota>
              <Parrafo>
                Aplicada el {diaCorto(item.aplicacion.fechaAplicacion)} · lote {item.aplicacion.lote}
              </Parrafo>
              <Parrafo>Refuerzo: {diaCorto(item.aplicacion.fechaRefuerzo)}</Parrafo>
            </View>
          ))}

          {historial.length > carnet.length ? (
            <>
              <Pressable
                onPress={() => setVerTodo((v) => !v)}
                accessibilityRole="button"
                accessibilityState={{ expanded: verTodo }}
                aria-expanded={verTodo}
                className="min-h-12 flex-row items-center gap-2 active:opacity-70"
              >
                <Icono nombre={verTodo ? 'chevron-up-outline' : 'chevron-down-outline'} color="primario" tamano={18} />
                <Text className="font-nunito-semi text-base text-primario dark:text-primario-oscuro">
                  {verTodo ? 'Ocultar el historial' : `Ver todas las aplicaciones (${historial.length})`}
                </Text>
              </Pressable>
              {verTodo
                ? historial.map((a) => (
                    <Nota key={a.id}>
                      {diaCorto(a.fechaAplicacion)}: {a.producto}, lote {a.lote} · {a.aplicadaPor}
                    </Nota>
                  ))
                : null}
            </>
          ) : null}
        </>
      )}

      <Nota>Te avisamos {DIAS_AVISO_REFUERZO} días antes de cada refuerzo, en la pestaña Avisos.</Nota>
      <Nota>{AVISO_CATALOGO_DEMO}</Nota>
    </View>
  );
}
