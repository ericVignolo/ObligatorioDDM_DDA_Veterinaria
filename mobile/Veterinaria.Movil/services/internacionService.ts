import { internacionesDemo, novedadesDemo } from '../data/internacionesDemo';
import type { SeguimientoInternacion } from '../types/Internacion';
import { obtenerPorId as obtenerAnimal } from './animalService';

// Seguimiento de internación del Cliente, solo lectura (B-011, RF-CLI-09, CU-CLI-08), como DeAnimal y
// NovedadesVisibles de InternacionService de Blazor. En la sección C: GET /api/v1/mis-internaciones (lista) y
// GET /api/v1/mis-internaciones/{id} (novedades visibles); la API filtra lo interno. El tiempo real todavía no existe:
// acá las novedades se ven al actualizar (RNF-DIS-04).

// Abiertas y altas de un animal del cliente, de la más reciente a la más antigua; las admisiones anuladas no se muestran.
// Si el animal no es suyo, devuelve una lista vacía (RN-18). Solo salen las entradas visibles (RN-23).
export function seguimientoDe(animalId: number, clienteId: number): SeguimientoInternacion[] {
  if (!obtenerAnimal(animalId, clienteId)) return [];
  return internacionesDemo
    .filter((i) => i.animalId === animalId && i.clienteId === clienteId)
    .sort((a, b) => b.fechaAdmision - a.fechaAdmision)
    .flatMap((i) => {
      if (i.estado === 'Anulada') return [];
      return [
        {
          id: i.id,
          estado: i.estado,
          motivo: i.motivo,
          fechaAdmision: i.fechaAdmision,
          // RN-25: la evolución final y las indicaciones se muestran recién con el alta.
          alta:
            i.estado === 'Alta' && i.fechaAlta !== null
              ? { fecha: i.fechaAlta, veterinario: i.veterinarioAlta, evolucionFinal: i.evolucionFinal, indicaciones: i.indicacionesDomicilio }
              : null,
          // Se copian solo los campos que ve el cliente, de la más reciente a la más antigua.
          novedades: novedadesDemo
            .filter((n) => n.internacionId === i.id && n.visible)
            .sort((a, b) => b.fecha - a.fecha || b.id - a.id)
            .map(({ id, texto, fecha, autorNombre, rectificaId }) => ({ id, texto, fecha, autorNombre, rectificaId })),
        },
      ];
    });
}
