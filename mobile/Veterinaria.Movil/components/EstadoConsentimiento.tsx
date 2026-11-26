import type { EstadoConsentimiento as Estado } from '../types/Consentimiento';
import { Insignia } from './Insignia';

// Estado del consentimiento de un turno (B-012, RN-10). No muestra nada si el servicio no lo pide.
export function EstadoConsentimiento({ estado }: { estado: Estado }) {
  if (estado === 'No requiere') return null;
  return <Insignia texto={`Consentimiento: ${estado.toLowerCase()}`} tipo={estado === 'Firmado' ? 'exito' : estado === 'Rechazado' ? 'peligro' : 'aviso'} />;
}
