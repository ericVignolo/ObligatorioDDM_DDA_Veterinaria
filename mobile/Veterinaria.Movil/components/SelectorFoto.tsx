import * as ImagePicker from 'expo-image-picker';
import { useState } from 'react';
import { Linking, Platform, Text, View } from 'react-native';
import { Boton } from './Boton';
import { FotoAnimal } from './FotoAnimal';
import { Nota } from './Textos';

type Props = { foto: string; nombre: string; onCambio: (uri: string) => void };

// CU-CLI-01 A2: si la foto no sirve se rechaza solo el archivo y se explica el formato o el tamaño permitido.
const FORMATOS = ['image/jpeg', 'image/png', 'image/webp'];
const TAMANO_MAXIMO = 5 * 1024 * 1024;

// Recorte cuadrado y compresión para que la foto pese poco (en la sección C se sube a la API).
const OPCIONES: ImagePicker.ImagePickerOptions = { mediaTypes: ['images'], allowsEditing: true, aspect: [1, 1], quality: 0.6 };

// Tomar o elegir la foto del animal (B-007, RF-CLI-01). Es opcional.
export function SelectorFoto({ foto, nombre, onCambio }: Props) {
  const [error, setError] = useState('');
  const [sinPermiso, setSinPermiso] = useState(false);

  const usar = (resultado: ImagePicker.ImagePickerResult) => {
    if (resultado.canceled || resultado.assets.length === 0) return;
    const imagen = resultado.assets[0];
    if (imagen.mimeType && !FORMATOS.includes(imagen.mimeType)) {
      setError('Esa imagen no es JPG, PNG ni WebP. Elegí otra.');
      return;
    }
    if (imagen.fileSize && imagen.fileSize > TAMANO_MAXIMO) {
      setError('La foto pesa más de 5 MB. Elegí otra o sacala de nuevo.');
      return;
    }
    onCambio(imagen.uri);
  };

  const tomar = async () => {
    setError('');
    setSinPermiso(false);
    try {
      const permiso = await ImagePicker.requestCameraPermissionsAsync();
      if (!permiso.granted) {
        // Se explica qué pasa y se ofrece otra salida (Arquitectura de permisos móviles).
        setSinPermiso(true);
        setError('Sin permiso para usar la cámara. Podés darlo en los ajustes del celular o elegir una foto de la galería.');
        return;
      }
      usar(await ImagePicker.launchCameraAsync(OPCIONES));
    } catch {
      setError('No pudimos abrir la cámara. Probá elegir una foto de la galería.');
    }
  };

  const elegir = async () => {
    setError('');
    setSinPermiso(false);
    try {
      usar(await ImagePicker.launchImageLibraryAsync(OPCIONES));
    } catch {
      setError('No pudimos abrir la galería. Intentá de nuevo.');
    }
  };

  return (
    <View className="gap-3">
      <FotoAnimal uri={foto} nombre={nombre || 'Tu animal'} tamano="grande" />
      <Nota>Una foto ayuda a la clínica a reconocerlo y evita confusiones en la ficha y en la historia clínica.</Nota>
      <View className="gap-2">
        <Boton texto="Tomar foto" icono="camera-outline" variante="secundario" onPress={tomar} />
        <Boton texto={foto ? 'Cambiar por otra de la galería' : 'Elegir de la galería'} icono="images-outline" variante="secundario" onPress={elegir} />
        {foto ? <Boton texto="Quitar foto" icono="trash-outline" variante="secundario" onPress={() => onCambio('')} /> : null}
      </View>
      {error ? (
        <View accessibilityLiveRegion="polite" className="gap-2 rounded-boton bg-peligro-suave p-3 dark:bg-peligro-suave-oscuro">
          <Text className="font-nunito-semi text-base text-peligro dark:text-peligro-oscuro">{error}</Text>
          {sinPermiso && Platform.OS !== 'web' ? <Boton texto="Abrir ajustes" icono="settings-outline" variante="secundario" onPress={() => Linking.openSettings()} /> : null}
        </View>
      ) : null}
    </View>
  );
}
