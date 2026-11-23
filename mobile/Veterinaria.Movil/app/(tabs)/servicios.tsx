import { useRouter } from 'expo-router';
import { FlatList, View } from 'react-native';
import { TarjetaPrestacion } from '../../components/TarjetaPrestacion';
import { Vacio } from '../../components/Vacio';
import { useSesion } from '../../context/SesionContext';
import { obtenerPublicas } from '../../services/prestacionService';

// Catálogo público (RF-VIS-01, D-CAT-02/04): solo servicios publicados y con precio.
export default function Servicios() {
  const router = useRouter();
  const { usuario } = useSesion();

  return (
    <FlatList
      className="flex-1 bg-fondo dark:bg-fondo-oscuro"
      contentContainerClassName="p-5 pb-10"
      data={obtenerPublicas()}
      keyExtractor={(item) => String(item.id)}
      ItemSeparatorComponent={() => <View className="h-3" />}
      ListEmptyComponent={<Vacio icono="list-outline" titulo="No hay servicios publicados" texto="Llamá a la clínica para consultar." />}
      renderItem={({ item }) => (
        <TarjetaPrestacion
          prestacion={item}
          onVerDetalle={(id) => router.push({ pathname: '/servicios/[id]', params: { id: String(id) } })}
          // Acción protegida: sin sesión, primero ingresar (A-011).
          onReservar={(id) =>
            usuario
              ? router.push({ pathname: '/servicios/[id]', params: { id: String(id) } })
              : router.push({ pathname: '/ingresar', params: { servicio: String(id) } })
          }
        />
      )}
    />
  );
}
