import type { EstadoTurno as Estado } from '../types/Turno';
import { Insignia, type TipoInsignia } from './Insignia';

// Etiqueta única del estado de un turno (A-018), como EstadoTurno.razor.
// Un turno Confirmado cuyo horario ya pasó se muestra como «Realizado».
export function EstadoTurno({ estado, pasado }: { estado: Estado; pasado?: boolean }) {
  const texto = estado === 'Confirmado' && pasado ? 'Realizado' : estado === 'En curso' ? 'En consulta' : estado;
  const tipos: Record<Estado, TipoInsignia> = {
    'Pendiente de seña': 'aviso',
    Confirmado: pasado ? 'neutro' : 'info',
    'En espera': 'info',
    'En curso': 'exito',
    Finalizado: 'neutro',
    Ausente: 'peligro',
    Cancelado: 'neutro',
  };
  return <Insignia texto={texto} tipo={tipos[estado]} />;
}
