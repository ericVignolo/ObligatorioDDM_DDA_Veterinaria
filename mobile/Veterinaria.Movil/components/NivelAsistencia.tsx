import { View } from 'react-native';
import { NIVELES } from '../services/asistenciaService';
import type { NivelUrgencia } from '../types/Asistencia';
import { Insignia, type TipoInsignia } from './Insignia';
import { Nota } from './Textos';

const tipos: Record<NivelUrgencia, TipoInsignia> = { Rojo: 'peligro', Naranja: 'acento', Amarillo: 'aviso', Verde: 'exito' };

// Nivel de una solicitud: nunca solo color, siempre con nombre, símbolo y descripción (RN-47).
export function NivelAsistencia({ nivel }: { nivel: NivelUrgencia }) {
  const dato = NIVELES.find((n) => n.nombre === nivel);
  return (
    <View className="gap-1">
      <Insignia texto={`${dato?.simbolo} ${nivel}`} tipo={tipos[nivel]} />
      <Nota>{dato?.descripcion}</Nota>
    </View>
  );
}
