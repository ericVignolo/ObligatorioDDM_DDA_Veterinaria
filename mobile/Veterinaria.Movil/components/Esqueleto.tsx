import { useEffect, useRef } from 'react';
import { Animated, Platform, View } from 'react-native';

// Esqueleto de carga (S6): se usa cuando la espera pasa de unos 300 ms. Pulsa suave, sin girar.
export function Esqueleto({ lineas = 3 }: { lineas?: number }) {
  const opacidad = useRef(new Animated.Value(0.4)).current;

  useEffect(() => {
    const animacion = Animated.loop(
      Animated.sequence([
        Animated.timing(opacidad, { toValue: 1, duration: 700, useNativeDriver: Platform.OS !== 'web' }),
        Animated.timing(opacidad, { toValue: 0.4, duration: 700, useNativeDriver: Platform.OS !== 'web' }),
      ]),
    );
    animacion.start();
    return () => animacion.stop();
  }, [opacidad]);

  return (
    <Animated.View style={{ opacity: opacidad }} accessibilityLabel="Cargando" accessibilityRole="progressbar">
      <View className="gap-3 rounded-tarjeta border border-borde bg-superficie p-4 dark:border-borde-oscuro dark:bg-superficie-oscuro">
        <View className="h-5 w-2/3 rounded-md bg-superficie-2 dark:bg-superficie-2-oscuro" />
        {Array.from({ length: lineas }).map((_, i) => (
          <View key={i} className={`h-4 rounded-md bg-superficie-2 dark:bg-superficie-2-oscuro ${i === lineas - 1 ? 'w-1/2' : 'w-full'}`} />
        ))}
      </View>
    </Animated.View>
  );
}
