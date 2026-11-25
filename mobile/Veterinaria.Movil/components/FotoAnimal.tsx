import { Image, View } from 'react-native';
import { Icono } from './Icono';

type Props = {
  uri: string;
  nombre: string;
  tamano?: 'chica' | 'grande';
  // En una lista el nombre ya está al lado: la foto es decorativa y el lector de pantalla la saltea (RNF-USA-06).
  decorativa?: boolean;
};

// Foto del animal (RF-CLI-01) o, si no tiene, la huella como marcador.
export function FotoAnimal({ uri, nombre, tamano = 'chica', decorativa }: Props) {
  const medida = tamano === 'grande' ? 'h-56 w-full rounded-tarjeta' : 'h-14 w-14 rounded-full';
  if (!uri) {
    return (
      <View
        accessibilityElementsHidden={decorativa}
        importantForAccessibility={decorativa ? 'no-hide-descendants' : 'auto'}
        accessible={!decorativa}
        accessibilityLabel={decorativa ? undefined : `${nombre} todavía no tiene foto`}
        className={`${medida} items-center justify-center bg-primario-suave dark:bg-primario-suave-oscuro`}
      >
        <Icono nombre="paw-outline" color="primario" tamano={tamano === 'grande' ? 48 : 24} />
      </View>
    );
  }
  return (
    <Image
      source={{ uri }}
      resizeMode="cover"
      accessible={!decorativa}
      accessibilityElementsHidden={decorativa}
      importantForAccessibility={decorativa ? 'no' : 'yes'}
      accessibilityLabel={decorativa ? undefined : `Foto de ${nombre}`}
      className={`${medida} bg-superficie-2 dark:bg-superficie-2-oscuro`}
    />
  );
}
