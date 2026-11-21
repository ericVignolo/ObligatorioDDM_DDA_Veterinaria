import { useLocalSearchParams } from 'expo-router';
import { View, Text } from 'react-native';
import { obtenerPorId } from '../services/prestacionService';

// Provisoria, igual que /acceso en Blazor: el registro y el ingreso vienen en la próxima sesión.
export default function Acceso() {
  const { servicio } = useLocalSearchParams<{ servicio?: string }>();
  const elegido = servicio ? obtenerPorId(Number(servicio)) : undefined;

  return (
    <View className="flex-1 gap-3 bg-fondo p-5 dark:bg-fondo-oscuro">
      <Text className="font-nunito-negrita text-2xl text-texto dark:text-texto-oscuro">Ingresá o creá tu cuenta</Text>
      {elegido ? (
        <Text className="font-nunito text-base text-texto-suave dark:text-texto-suave-oscuro">Para reservar «{elegido.nombre}» necesitás ingresar.</Text>
      ) : null}
      <Text className="rounded-boton bg-info-suave p-3 font-nunito text-sm text-info dark:bg-info-suave-oscuro dark:text-info-oscuro">En construcción.</Text>
    </View>
  );
}
