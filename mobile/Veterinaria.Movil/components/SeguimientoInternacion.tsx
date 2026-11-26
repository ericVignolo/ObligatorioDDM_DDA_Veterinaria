import { useState } from 'react';
import { Text, View } from 'react-native';
import { seguimientoDe } from '../services/internacionService';
import type { SeguimientoInternacion as Seguimiento } from '../types/Internacion';
import { diaCorto, hora } from '../utils/fechas';
import { Boton } from './Boton';
import { Insignia, type TipoInsignia } from './Insignia';
import { Nota, Parrafo, Subtitulo } from './Textos';

type Props = { animalId: number; clienteId: number; nombre: string };

const estados: Record<Seguimiento['estado'], { titulo: string; insignia: string; tipo: TipoInsignia }> = {
  Admitida: { titulo: 'Internación en curso', insignia: 'Esperando al veterinario', tipo: 'aviso' },
  Internado: { titulo: 'Internación en curso', insignia: 'Internado', tipo: 'info' },
  Alta: { titulo: 'Internación terminada', insignia: 'Alta', tipo: 'exito' },
};

const cuando = (fecha: number) => `${diaCorto(fecha)} a las ${hora(fecha)}`;

// Internaciones de un animal en la ficha (B-011, RF-CLI-09), como el bloque de AnimalDetalle.razor:
// estado, ingreso, alta con indicaciones y solo las novedades visibles (RN-23). El cliente no publica.
export function SeguimientoInternacion({ animalId, clienteId, nombre }: Props) {
  // Hora de la última actualización manual; al cambiar, se vuelve a leer el servicio (RNF-DIS-04).
  const [actualizado, setActualizado] = useState<number | null>(null);
  const internaciones = seguimientoDe(animalId, clienteId);

  const caja = 'gap-3 rounded-tarjeta border border-borde bg-superficie p-4 dark:border-borde-oscuro dark:bg-superficie-oscuro';

  if (internaciones.length === 0) {
    return (
      <View className={caja}>
        <Subtitulo>Internaciones</Subtitulo>
        <Parrafo suave>{nombre} no tiene internaciones registradas.</Parrafo>
      </View>
    );
  }

  return (
    <>
      {internaciones.map((i) => (
        <View key={i.id} className={caja}>
          <Subtitulo>{estados[i.estado].titulo}</Subtitulo>
          <Insignia texto={estados[i.estado].insignia} tipo={estados[i.estado].tipo} />
          <Nota>
            Ingresó el {cuando(i.fechaAdmision)} · {i.motivo}
          </Nota>
          {i.estado === 'Admitida' ? <Parrafo>Está esperando que el veterinario lo reciba.</Parrafo> : null}

          {i.alta ? (
            <View className="gap-2 rounded-boton bg-exito-suave p-3 dark:bg-exito-suave-oscuro">
              <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">Alta el {cuando(i.alta.fecha)}</Text>
              <Parrafo>{i.alta.evolucionFinal}</Parrafo>
              <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">Indicaciones para casa</Text>
              <Parrafo>{i.alta.indicaciones}</Parrafo>
              <Nota>Alta firmada por {i.alta.veterinario}</Nota>
            </View>
          ) : null}

          {i.novedades.length === 0 ? (
            <Parrafo suave>Todavía no hay novedades. Las vas a ver acá cuando el veterinario publique una.</Parrafo>
          ) : (
            i.novedades.map((n) => (
              <View key={n.id} className="gap-1 border-l-4 border-acento pl-3 dark:border-acento-oscuro">
                <Nota>
                  {cuando(n.fecha)} · {n.autorNombre}
                  {n.rectificaId !== 0 ? ' · corrección' : ''}
                </Nota>
                <Parrafo>{n.texto}</Parrafo>
              </View>
            ))
          )}

          {i.estado !== 'Alta' ? (
            <>
              <Boton texto="Actualizar novedades" icono="refresh-outline" variante="secundario" onPress={() => setActualizado(Date.now())} />
              {actualizado !== null ? (
                <View accessibilityLiveRegion="polite">
                  <Nota>Actualizado a las {hora(actualizado)}.</Nota>
                </View>
              ) : null}
            </>
          ) : null}
        </View>
      ))}
      <Nota>Demostración: las novedades se ven al actualizar. En la versión final llegan solas.</Nota>
    </>
  );
}
