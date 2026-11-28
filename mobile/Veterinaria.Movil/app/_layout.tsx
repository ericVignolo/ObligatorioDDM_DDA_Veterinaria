import '../global.css';
import { Nunito_400Regular } from '@expo-google-fonts/nunito/400Regular';
import { Nunito_600SemiBold } from '@expo-google-fonts/nunito/600SemiBold';
import { Nunito_800ExtraBold } from '@expo-google-fonts/nunito/800ExtraBold';
import { useFonts } from 'expo-font';
import { Stack } from 'expo-router';
import { useEffect } from 'react';
import { Platform } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { useColorScheme } from 'nativewind';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AvisoProvider } from '../context/AvisoContext';
import { SesionProvider } from '../context/SesionContext';
import { fuentes, useColor } from '../tema';

// Si se abre un detalle directamente (enlace), las pestañas quedan debajo para poder volver.
export const unstable_settings = { initialRouteName: '(tabs)' };

export default function RootLayout() {
  const [fuentesListas] = useFonts({ Nunito_400Regular, Nunito_600SemiBold, Nunito_800ExtraBold });
  const { colorScheme, setColorScheme } = useColorScheme();
  const color = useColor();

  // Con darkMode 'class', NativeWind no sigue al sistema hasta que se lo pedimos.
  // En la versión web 'system' no aplica la clase "dark": se pasa el valor del navegador.
  useEffect(() => {
    if (Platform.OS === 'web') {
      setColorScheme(window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    } else {
      setColorScheme('system');
    }
    // Solo al abrir la app: después manda la elección de Perfil.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (!fuentesListas) return null;

  return (
    <SafeAreaProvider>
      <SesionProvider>
        <AvisoProvider>
          <StatusBar style={colorScheme === 'dark' ? 'light' : 'dark'} />
          <Stack
            screenOptions={{
              headerStyle: { backgroundColor: color('superficie') },
              headerTintColor: color('primario'),
              headerTitleStyle: { fontFamily: fuentes.negrita, color: color('texto') },
              headerBackTitle: 'Volver',
              contentStyle: { backgroundColor: color('fondo') },
            }}
          >
            {/* Las pestañas por rol (app/(tabs)) y, encima, las pantallas de detalle. */}
            <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
            <Stack.Screen name="servicios/[id]" options={{ title: 'Servicio' }} />
            <Stack.Screen name="profesionales/index" options={{ title: 'Profesionales' }} />
            <Stack.Screen name="profesionales/[id]" options={{ title: 'Profesional' }} />
            <Stack.Screen name="clinica" options={{ title: 'La clínica' }} />
            <Stack.Screen name="legales/[codigo]" options={{ title: 'Documento' }} />
            <Stack.Screen name="registro" options={{ title: 'Crear cuenta' }} />
            <Stack.Screen name="verificar" options={{ title: 'Verificar correo' }} />
            <Stack.Screen name="recuperar" options={{ title: 'Recuperar contraseña' }} />
            <Stack.Screen name="mi-cuenta" options={{ title: 'Mi cuenta' }} />
            <Stack.Screen name="preferencias" options={{ title: 'Preferencias y privacidad' }} />
            <Stack.Screen name="consentimiento/[turnoId]" options={{ title: 'Consentimiento' }} />
            <Stack.Screen name="reservar" options={{ title: 'Reservar turno' }} />
            <Stack.Screen name="asistencia/index" options={{ title: 'Asistencia' }} />
            <Stack.Screen name="asistencia/nueva" options={{ title: 'Pedir asistencia' }} />
            <Stack.Screen name="asistencia/[id]" options={{ title: 'Solicitud' }} />
            <Stack.Screen name="animales/nuevo" options={{ title: 'Registrar animal' }} />
            <Stack.Screen name="animales/[id]/index" options={{ title: 'Ficha del animal' }} />
            <Stack.Screen name="animales/[id]/editar" options={{ title: 'Editar animal' }} />
            <Stack.Screen name="animales/[id]/historia" options={{ title: 'Historia clínica' }} />
            <Stack.Screen name="animales/[id]/adjunto/[adjuntoId]" options={{ title: 'Archivo' }} />
          </Stack>
        </AvisoProvider>
      </SesionProvider>
    </SafeAreaProvider>
  );
}
