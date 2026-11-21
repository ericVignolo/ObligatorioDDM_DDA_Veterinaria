import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { Animated, Platform, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Icono } from '../components/Icono';

// Avisos flotantes (S6 del diseño): éxito, error o información, 4 segundos abajo de la pantalla.
type TipoAviso = 'exito' | 'error' | 'info';
type Aviso = { id: number; tipo: TipoAviso; texto: string };

const AvisoContext = createContext<(texto: string, tipo?: TipoAviso) => void>(() => {});

const estilos: Record<TipoAviso, { caja: string; icono: string; color: string }> = {
  exito: { caja: 'bg-exito dark:bg-exito-oscuro', icono: 'checkmark-circle-outline', color: 'sobre-primario' },
  error: { caja: 'bg-peligro dark:bg-peligro-oscuro', icono: 'alert-circle-outline', color: 'sobre-primario' },
  info: { caja: 'bg-texto dark:bg-texto-oscuro', icono: 'information-circle-outline', color: 'fondo' },
};

export function AvisoProvider({ children }: { children: ReactNode }) {
  const [aviso, setAviso] = useState<Aviso | null>(null);
  const opacidad = useRef(new Animated.Value(0)).current;
  const insets = useSafeAreaInsets();

  const mostrar = useCallback((texto: string, tipo: TipoAviso = 'info') => {
    setAviso({ id: Date.now(), tipo, texto });
  }, []);

  useEffect(() => {
    if (!aviso) return;
    Animated.timing(opacidad, { toValue: 1, duration: 200, useNativeDriver: Platform.OS !== 'web' }).start();
    const t = setTimeout(() => {
      Animated.timing(opacidad, { toValue: 0, duration: 200, useNativeDriver: Platform.OS !== 'web' }).start(() => setAviso(null));
    }, 4000);
    return () => clearTimeout(t);
  }, [aviso, opacidad]);

  return (
    <AvisoContext.Provider value={mostrar}>
      {children}
      {aviso ? (
        <Animated.View
          pointerEvents="none"
          accessibilityLiveRegion="polite"
          style={{ opacity: opacidad, position: 'absolute', left: 16, right: 16, bottom: insets.bottom + 72 }}
        >
          <View className={`flex-row items-center gap-3 rounded-boton px-4 py-3 ${estilos[aviso.tipo].caja}`}>
            <Icono nombre={estilos[aviso.tipo].icono} color={estilos[aviso.tipo].color} tamano={22} />
            <Text className="flex-1 font-nunito-semi text-base text-sobre-primario dark:text-fondo-oscuro">{aviso.texto}</Text>
          </View>
        </Animated.View>
      ) : null}
    </AvisoContext.Provider>
  );
}

export function useAviso() {
  return useContext(AvisoContext);
}
