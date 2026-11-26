import { Stack, useLocalSearchParams, useRouter } from 'expo-router';
import { View } from 'react-native';
import { FotoAnimal } from '../../../components/FotoAnimal';
import { Pantalla } from '../../../components/Pantalla';
import { RegistroClinico } from '../../../components/RegistroClinico';
import { Nota, Titulo } from '../../../components/Textos';
import { Vacio } from '../../../components/Vacio';
import { useSesion } from '../../../context/SesionContext';
import { obtenerPorId } from '../../../services/animalService';
import { historiaDe } from '../../../services/historiaService';

// Historia clínica del Cliente (B-009, RF-CLI-05, CU-CLI-05): solo lectura, solo consultas firmadas de sus animales.
export default function HistoriaAnimal() {
  const router = useRouter();
  const { usuario } = useSesion();
  const { id } = useLocalSearchParams<{ id: string }>();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const animal = clienteId === null ? undefined : obtenerPorId(Number(id), clienteId);
  const historia = clienteId === null ? null : historiaDe(Number(id), clienteId);

  // RN-18 y A-RC-14: no se distingue entre «no existe» y «no es tuyo», y no se muestra nada clínico.
  if (!animal || historia === null) {
    return (
      <Pantalla>
        <Vacio icono="paw-outline" titulo="Animal no disponible" texto="No encontramos ese animal entre los tuyos." accion={{ texto: 'Ir a mis animales', onPress: () => router.navigate('/animales') }} />
      </Pantalla>
    );
  }

  return (
    <Pantalla>
      <Stack.Screen options={{ title: `Historia de ${animal.nombre}` }} />
      <View className="flex-row items-center gap-3">
        <FotoAnimal uri={animal.foto} nombre={animal.nombre} decorativa />
        <View className="flex-1">
          <Titulo>Historia clínica de {animal.nombre}</Titulo>
        </View>
      </View>
      <Nota>Consultas firmadas por la clínica, de la más reciente a la más antigua. Las correcciones (enmiendas) se muestran debajo del registro original, que nunca cambia.</Nota>

      {historia.length === 0 ? (
        <Vacio icono="document-text-outline" titulo={`Todavía no hay consultas registradas para ${animal.nombre}`} texto="Cuando el veterinario firme una consulta, la vas a ver acá." />
      ) : (
        historia.map((c) => (
          <RegistroClinico
            key={c.id}
            consulta={c}
            onAbrirAdjunto={(a) => router.push({ pathname: '/animales/[id]/adjunto/[adjuntoId]', params: { id: String(animal.id), adjuntoId: String(a.id) } })}
          />
        ))
      )}
    </Pantalla>
  );
}
