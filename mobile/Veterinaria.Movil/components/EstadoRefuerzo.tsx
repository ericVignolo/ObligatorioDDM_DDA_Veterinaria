import type { EstadoRefuerzo as Estado } from '../types/Vacuna';
import { Insignia } from './Insignia';

// Etiqueta única del estado de un refuerzo (A-021). El texto dice el estado: el color solo acompaña.
export function EstadoRefuerzo({ estado }: { estado: Estado }) {
  return <Insignia texto={estado} tipo={estado === 'Vigente' ? 'exito' : estado === 'Vencida' ? 'peligro' : 'aviso'} />;
}
