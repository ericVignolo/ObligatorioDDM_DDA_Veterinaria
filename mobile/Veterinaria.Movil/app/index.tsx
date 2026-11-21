import { useRouter } from 'expo-router';
import { Linking, ScrollView, Text, View } from 'react-native';
import { Boton } from '../components/Boton';
import { clinica } from '../data/clinica';

// Inicio del Visitante: datos de la clínica, servicios y acceso a urgencias (RF-VIS-06).
export default function Inicio() {
  const router = useRouter();

  return (
    <ScrollView className="flex-1 bg-fondo dark:bg-fondo-oscuro" contentContainerClassName="gap-4 p-5">
      <View className="gap-1">
        <Text className="font-nunito-negrita text-3xl text-primario dark:text-primario-oscuro">{clinica.nombre}</Text>
        <Text className="font-nunito text-base text-texto-suave dark:text-texto-suave-oscuro">Clínica veterinaria · {clinica.direccion}</Text>
        <Text className="font-nunito text-sm text-texto-suave dark:text-texto-suave-oscuro">{clinica.horario}</Text>
      </View>

      <View className="gap-2 rounded-tarjeta bg-peligro-suave p-4 dark:bg-peligro-suave-oscuro">
        <Text className="font-nunito-negrita text-base text-peligro dark:text-peligro-oscuro">¿Es una urgencia?</Text>
        <Text className="font-nunito text-sm text-texto dark:text-texto-oscuro">Llamá a la guardia: {clinica.telefonoGuardia}</Text>
        <Boton texto="Llamar a la guardia" variante="acento" onPress={() => Linking.openURL(`tel:${clinica.telefonoGuardia.replace(/\s/g, '')}`)} />
      </View>

      <Boton texto="Ver servicios" onPress={() => router.push('/servicios')} />
      <Boton texto="Ingresar o crear cuenta" variante="secundario" onPress={() => router.push('/acceso')} />
    </ScrollView>
  );
}
