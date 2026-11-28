import { useFocusEffect, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Boton } from '../../components/Boton';
import { Insignia } from '../../components/Insignia';
import { NivelAsistencia } from '../../components/NivelAsistencia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useSesion } from '../../context/SesionContext';
import { deCliente, pideReevaluacion } from '../../services/asistenciaService';
import type { SolicitudAsistencia } from '../../types/Asistencia';
import { diaCorto, hora } from '../../utils/fechas';

// Solicitudes de asistencia del Cliente (B-013, B-014): las que piden reevaluación se marcan.
export default function MisAsistencias() {
  const router = useRouter();
  const { usuario } = useSesion();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const [lista, setLista] = useState<SolicitudAsistencia[]>([]);

  useFocusEffect(
    useCallback(() => {
      setLista(clienteId === null ? [] : deCliente(clienteId));
    }, [clienteId]),
  );

  if (clienteId === null) {
    return (
      <Pantalla>
        <Vacio icono="medkit-outline" titulo="Ingresá como cliente para ver tus solicitudes" accion={{ texto: 'Ir al inicio', onPress: () => router.navigate('/') }} />
      </Pantalla>
    );
  }

  return (
    <Pantalla>
      <Titulo>Mis solicitudes</Titulo>
      <Boton texto="Pedir asistencia" icono="add-circle-outline" onPress={() => router.push('/asistencia/nueva')} />
      {lista.length === 0 ? (
        <Vacio icono="medkit-outline" titulo="Todavía no pediste asistencia" />
      ) : (
        lista.map((s) => (
          <Tarjeta key={s.id} onPress={() => router.push({ pathname: '/asistencia/[id]', params: { id: String(s.id) } })} etiqueta={`Solicitud ${s.id}, ${s.nivel}, ${s.estado}`}>
            <Nota>
              n.º {s.id} · {diaCorto(s.fechaCreacion)} {hora(s.fechaCreacion)}
            </Nota>
            <Parrafo>{s.sintomas}</Parrafo>
            <NivelAsistencia nivel={s.nivel} />
            <Insignia texto={s.estado} tipo="info" />
            {pideReevaluacion(s) ? <Insignia texto="Responder reevaluación de 24 h" tipo="aviso" /> : null}
          </Tarjeta>
        ))
      )}
    </Pantalla>
  );
}
