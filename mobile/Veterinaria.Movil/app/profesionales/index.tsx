import { useRouter } from 'expo-router';
import { FlatList, Text, View } from 'react-native';
import { Insignia } from '../../components/Insignia';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { obtenerPublicos } from '../../services/profesionalService';

// Profesionales publicados (RF-VIS-02, HU-VIS-02), sin matrícula ni datos internos.
export default function Profesionales() {
  const router = useRouter();

  return (
    <FlatList
      className="flex-1 bg-fondo dark:bg-fondo-oscuro"
      contentContainerClassName="p-5 pb-10"
      data={obtenerPublicos()}
      keyExtractor={(p) => String(p.id)}
      ItemSeparatorComponent={() => <View className="h-3" />}
      ListEmptyComponent={<Vacio icono="people-outline" titulo="No hay profesionales publicados" />}
      renderItem={({ item }) => (
        <Tarjeta
          etiqueta={`${item.nombre}, ${item.especialidad}. Ver horarios`}
          onPress={() => router.push({ pathname: '/profesionales/[id]', params: { id: String(item.id) } })}
        >
          <Text className="font-nunito-negrita text-lg text-texto dark:text-texto-oscuro">{item.nombre}</Text>
          <Insignia texto={item.especialidad} tipo="exito" />
          <Nota>{item.descripcion}</Nota>
          <Nota>Atiende: {item.especies.join(', ')}</Nota>
        </Tarjeta>
      )}
    />
  );
}
