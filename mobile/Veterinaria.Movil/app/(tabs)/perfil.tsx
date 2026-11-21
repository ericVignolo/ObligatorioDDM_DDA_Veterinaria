import { useRouter } from 'expo-router';
import { useColorScheme } from 'nativewind';
import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Fila } from '../../components/Fila';
import { Insignia } from '../../components/Insignia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Subtitulo, Titulo } from '../../components/Textos';
import { useAviso } from '../../context/AvisoContext';
import { useSesion } from '../../context/SesionContext';

const TEMAS = [
  { valor: 'system', texto: 'Sistema' },
  { valor: 'light', texto: 'Claro' },
  { valor: 'dark', texto: 'Oscuro' },
] as const;

// Perfil mínimo (B-004): quién ingresó, tema y cerrar sesión. Datos y cambio de contraseña: B-006.
export default function Perfil() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario, salir } = useSesion();
  const { colorScheme, setColorScheme } = useColorScheme();
  // NativeWind no devuelve "system"; lo recordamos acá para marcar la opción elegida.
  const [elegido, setElegido] = useTemaElegido();

  if (!usuario) return null;

  return (
    <Pantalla>
      <View className="gap-1">
        <Titulo>{usuario.nombre}</Titulo>
        <Nota>{usuario.correo}</Nota>
        <Insignia texto={usuario.rol} tipo="info" />
      </View>

      <Tarjeta>
        <Subtitulo>Apariencia</Subtitulo>
        <View className="flex-row gap-2" accessibilityRole="radiogroup">
          {TEMAS.map((t) => {
            const activo = elegido === t.valor;
            return (
              <Pressable
                key={t.valor}
                accessibilityRole="radio"
                accessibilityState={{ checked: activo }}
                onPress={() => {
                  setColorScheme(t.valor);
                  setElegido(t.valor);
                }}
                className={`min-h-12 flex-1 items-center justify-center rounded-boton border ${
                  activo
                    ? 'border-primario bg-primario-suave dark:border-primario-oscuro dark:bg-primario-suave-oscuro'
                    : 'border-borde dark:border-borde-oscuro'
                }`}
              >
                <Text className="font-nunito-semi text-base text-texto dark:text-texto-oscuro">{t.texto}</Text>
              </Pressable>
            );
          })}
        </View>
        <Nota>Ahora: {colorScheme === 'dark' ? 'oscuro' : 'claro'}.</Nota>
      </Tarjeta>

      <Tarjeta>
        <Fila icono="document-text-outline" texto="Política de Privacidad" onPress={() => router.push({ pathname: '/legales/[codigo]', params: { codigo: 'privacidad' } })} />
        <Fila icono="document-text-outline" texto="Términos y Condiciones" onPress={() => router.push({ pathname: '/legales/[codigo]', params: { codigo: 'terminos' } })} />
        <Fila icono="business-outline" texto="La clínica" onPress={() => router.push('/clinica')} />
      </Tarjeta>

      <Boton
        texto="Cerrar sesión"
        icono="log-out-outline"
        variante="secundario"
        onPress={() => {
          salir();
          avisar('Cerraste la sesión.', 'info');
          router.replace('/');
        }}
      />
    </Pantalla>
  );
}

// Elección de tema compartida entre visitas a Perfil mientras la app está abierta.
type Tema = 'system' | 'light' | 'dark';
let temaElegido: Tema = 'system';

function useTemaElegido() {
  const [valor, setValor] = useState<Tema>(temaElegido);
  const cambiar = (v: Tema) => {
    temaElegido = v;
    setValor(v);
  };
  return [valor, cambiar] as const;
}
