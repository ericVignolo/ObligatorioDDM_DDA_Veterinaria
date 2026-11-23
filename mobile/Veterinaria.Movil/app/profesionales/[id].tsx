import { Stack, useLocalSearchParams, useRouter } from 'expo-router';
import { Pantalla } from '../../components/Pantalla';
import { Fila } from '../../components/Fila';
import { Insignia } from '../../components/Insignia';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { obtenerPublicas } from '../../services/prestacionService';
import { describirJornada, obtenerPorId } from '../../services/profesionalService';

export default function DetalleProfesional() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();
  const profesional = obtenerPorId(Number(id));

  if (!profesional) {
    return (
      <Pantalla>
        <Vacio icono="help-circle-outline" titulo="Profesional no disponible" accion={{ texto: 'Ver profesionales', onPress: () => router.replace('/profesionales') }} />
      </Pantalla>
    );
  }

  const servicios = obtenerPublicas().filter((s) => profesional.prestacionIds.includes(s.id));

  return (
    <Pantalla>
      <Stack.Screen options={{ title: profesional.nombre }} />
      <Titulo>{profesional.nombre}</Titulo>
      <Insignia texto={profesional.especialidad} tipo="exito" />
      <Parrafo suave>{profesional.descripcion}</Parrafo>

      <Tarjeta>
        <Subtitulo>Horario</Subtitulo>
        {describirJornada(profesional).map((linea) => (
          <Parrafo key={linea}>{linea}</Parrafo>
        ))}
        <Nota>Las licencias y feriados se ven al elegir el día de la reserva.</Nota>
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Especies que atiende</Subtitulo>
        <Parrafo>{profesional.especies.join(', ')}</Parrafo>
      </Tarjeta>

      {servicios.length > 0 ? (
        <Tarjeta>
          <Subtitulo>Servicios</Subtitulo>
          {servicios.map((s) => (
            <Fila key={s.id} icono="medkit-outline" texto={s.nombre} onPress={() => router.push({ pathname: '/servicios/[id]', params: { id: String(s.id) } })} />
          ))}
        </Tarjeta>
      ) : null}
    </Pantalla>
  );
}
