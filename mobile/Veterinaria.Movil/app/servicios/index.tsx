import { useRouter } from 'expo-router';
import { FlatList, Text, View } from 'react-native';
import { TarjetaPrestacion } from '../../components/TarjetaPrestacion';
import { obtenerPublicas } from '../../services/prestacionService';

export default function Servicios() {
  const router = useRouter();
  const lista = obtenerPublicas();

  return (
    <FlatList
      className="flex-1 bg-fondo dark:bg-fondo-oscuro"
      contentContainerClassName="p-5"
      data={lista}
      keyExtractor={(item) => String(item.id)}
      ItemSeparatorComponent={() => <View className="h-3" />}
      ListEmptyComponent={<Text className="py-10 text-center font-nunito text-texto-suave">No hay servicios publicados.</Text>}
      renderItem={({ item }) => (
        <TarjetaPrestacion
          prestacion={item}
          onVerDetalle={(id) => router.push({ pathname: '/servicios/[id]', params: { id: String(id) } })}
          onReservar={(id) => router.push({ pathname: '/acceso', params: { servicio: String(id) } })}
        />
      )}
    />
  );
}
