import { View } from 'react-native';
import { llamar, obtenerClinica } from '../services/clinicaService';
import { Boton } from './Boton';
import { Parrafo, Subtitulo } from './Textos';

// Aviso para un caso grave: el teléfono de la guardia siempre a mano.
export function LlamarGuardia({ texto }: { texto: string }) {
  const clinica = obtenerClinica();
  return (
    <View accessibilityRole="alert" className="gap-3 rounded-tarjeta bg-peligro-suave p-4 dark:bg-peligro-suave-oscuro">
      <Subtitulo>No esperes: llamá a la guardia</Subtitulo>
      <Parrafo>{texto}</Parrafo>
      <Boton texto={`Llamar a la guardia (${clinica.telefonoGuardia})`} icono="call-outline" variante="peligro" onPress={() => llamar(clinica.telefonoGuardia)} />
    </View>
  );
}
