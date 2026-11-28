import * as Location from 'expo-location';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { Boton } from './Boton';
import { Insignia } from './Insignia';
import { Nota, Parrafo } from './Textos';

type Props = {
  latitud: number | null;
  longitud: number | null;
  onCambio: (ubicacion: { latitud: number | null; longitud: number | null }) => void;
};

// Ubicación del celular para una visita (B-014, RF-CLI-22, RNF-LEG-09): primero se explica para qué se usa,
// quién la ve y cuánto dura, y se pide solo cuando el cliente toca el botón. Si no la da, escribe la dirección.
// Se pide una sola vez, sin seguimiento en segundo plano. Las coordenadas no se muestran ni se registran en la consola.
export function UbicacionGps({ latitud, longitud, onCambio }: Props) {
  const [buscando, setBuscando] = useState(false);
  const [problema, setProblema] = useState('');
  const compartida = latitud !== null && longitud !== null;

  const compartir = async () => {
    setProblema('');
    setBuscando(true);
    try {
      const permiso = await Location.requestForegroundPermissionsAsync();
      if (!permiso.granted) {
        setProblema('No diste permiso para usar la ubicación. Escribí la dirección abajo.');
        return;
      }
      const posicion = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.Balanced });
      onCambio({ latitud: posicion.coords.latitude, longitud: posicion.coords.longitude });
    } catch {
      // Sección 13, «Sin GPS»: se sigue con la dirección escrita.
      setProblema('No pudimos obtener tu ubicación (¿está apagado el GPS?). Escribí la dirección abajo.');
    } finally {
      setBuscando(false);
    }
  };

  return (
    <View className="gap-2">
      <Nota>
        Si compartís tu ubicación, se usa solo para esta asistencia: la ven Recepción y el veterinario que la acepte, y se descarta cuando termina. No se sigue tu ubicación después.
      </Nota>
      {compartida ? (
        <View className="gap-2">
          <Insignia texto="Ubicación del celular agregada" tipo="exito" />
          <Boton texto="Quitar mi ubicación" icono="close-circle-outline" variante="secundario" onPress={() => onCambio({ latitud: null, longitud: null })} />
        </View>
      ) : (
        <Boton texto="Compartir mi ubicación actual" icono="locate-outline" variante="secundario" onPress={compartir} cargando={buscando} deshabilitado={buscando} />
      )}
      {problema ? (
        <View accessibilityRole="alert">
          <Text className="font-nunito-semi text-sm text-aviso dark:text-aviso-oscuro">{problema}</Text>
        </View>
      ) : null}
      {compartida ? <Parrafo suave>Igual podés escribir la dirección o referencias para llegar.</Parrafo> : null}
    </View>
  );
}
