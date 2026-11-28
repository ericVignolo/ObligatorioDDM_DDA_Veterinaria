import { Text, View } from 'react-native';
import { Icono } from './Icono';
import { Nota } from './Textos';

type Props = { salida: number; etaMinutos: number; ahora: number; destino: string };

// Recorrido del veterinario en camino (B-014, RF-CLI-23, RN-57): EXPERIENCIA DEMOSTRATIVA. Avanza según el tiempo
// estimado, no según una posición real. En la sección C lo reemplaza un mapa con la ubicación que comparte el
// veterinario durante el traslado y la ruta del proveedor de mapas.
export function MapaDemo({ salida, etaMinutos, ahora, destino }: Props) {
  const transcurridos = Math.max(0, (ahora - salida) / 60000);
  const avance = Math.min(1, transcurridos / etaMinutos);
  const faltan = Math.max(1, Math.round(etaMinutos - transcurridos));

  return (
    <View className="gap-2">
      <View
        accessible
        accessibilityLabel={`Mapa de demostración: el veterinario recorrió cerca del ${Math.round(avance * 100)} % del camino.`}
        className="h-28 justify-center rounded-boton bg-superficie-2 px-6 dark:bg-superficie-2-oscuro"
      >
        <View className="h-1 rounded-full bg-borde dark:bg-borde-oscuro" />
        <View className="absolute left-6 h-1 rounded-full bg-primario dark:bg-primario-oscuro" style={{ width: `${avance * 80}%` }} />
        <View className="absolute left-3 items-center" style={{ top: 18 }}>
          <Icono nombre="business" color="texto-suave" />
        </View>
        <View className="absolute right-3 items-center" style={{ top: 18 }}>
          <Icono nombre="location" color="peligro" />
        </View>
        <View className="absolute items-center" style={{ left: `${6 + avance * 80}%`, top: 16 }}>
          <Icono nombre="car" color="primario" tamano={26} />
        </View>
        <View className="absolute bottom-2 left-3 right-3 flex-row justify-between">
          <Text className="font-nunito text-xs text-texto-suave dark:text-texto-suave-oscuro">Clínica</Text>
          <Text numberOfLines={1} className="max-w-[60%] font-nunito text-xs text-texto-suave dark:text-texto-suave-oscuro">
            {destino}
          </Text>
        </View>
      </View>
      <Text accessibilityLiveRegion="polite" className="font-nunito-semi text-base text-texto dark:text-texto-oscuro">
        Llegada estimada en {faltan} min
      </Text>
      <Nota>Es una estimación y puede cambiar. Mapa de demostración: no muestra la posición real.</Nota>
    </View>
  );
}
