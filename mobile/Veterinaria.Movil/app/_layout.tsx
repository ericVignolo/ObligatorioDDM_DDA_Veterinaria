import '../global.css';
import { Nunito_400Regular, Nunito_600SemiBold, Nunito_800ExtraBold, useFonts } from '@expo-google-fonts/nunito';
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useColorScheme } from 'nativewind';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { fuentes, useColor } from '../tema';

export default function RootLayout() {
  const [fuentesListas] = useFonts({ Nunito_400Regular, Nunito_600SemiBold, Nunito_800ExtraBold });
  const { colorScheme } = useColorScheme();
  const color = useColor();

  if (!fuentesListas) return null;

  return (
    <SafeAreaProvider>
      <StatusBar style={colorScheme === 'dark' ? 'light' : 'dark'} />
      <Stack
        screenOptions={{
          headerStyle: { backgroundColor: color('superficie') },
          headerTintColor: color('primario'),
          headerTitleStyle: { fontFamily: fuentes.negrita, color: color('texto') },
          contentStyle: { backgroundColor: color('fondo') },
        }}
      >
        <Stack.Screen name="index" options={{ title: 'VITALIA' }} />
        <Stack.Screen name="servicios/index" options={{ title: 'Servicios' }} />
        <Stack.Screen name="servicios/[id]" options={{ title: 'Detalle del servicio' }} />
        <Stack.Screen name="acceso" options={{ title: 'Ingresar' }} />
      </Stack>
    </SafeAreaProvider>
  );
}
