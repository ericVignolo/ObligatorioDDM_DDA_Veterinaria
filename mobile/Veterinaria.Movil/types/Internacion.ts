// Internación (B-011, RF-CLI-09, CU-CLI-08). Mismos campos que Models/Internacion.cs y NovedadInternacion.cs de Blazor.
export type EstadoInternacion = 'Admitida' | 'Internado' | 'Alta' | 'Anulada';

export type Internacion = {
  id: number;
  animalId: number;
  clienteId: number;
  estado: EstadoInternacion;
  motivo: string;
  observaciones: string; // administrativas: no las ve el Cliente
  fechaAdmision: number;
  planTerapeutico: string; // solo el Veterinario
  evolucionFinal: string;
  indicacionesDomicilio: string;
  fechaAlta: number | null;
  veterinarioAlta: string;
};

// Una entrada publicada no se edita ni se borra: se rectifica con otra (RN-24).
export type NovedadInternacion = {
  id: number;
  internacionId: number;
  tipo: 'Novedad' | 'Evolución' | 'Rectificación';
  texto: string;
  visible: boolean; // false = nota interna del personal (RN-23)
  rectificaId: number; // 0 = no corrige ninguna
  fecha: number;
  autorNombre: string;
};

// Lo único que sale del servicio hacia el Cliente: sin plan, observaciones ni notas internas (RN-23).
export type SeguimientoInternacion = {
  id: number;
  estado: Exclude<EstadoInternacion, 'Anulada'>;
  motivo: string;
  fechaAdmision: number;
  alta: { fecha: number; veterinario: string; evolucionFinal: string; indicaciones: string } | null;
  novedades: Pick<NovedadInternacion, 'id' | 'texto' | 'fecha' | 'autorNombre' | 'rectificaId'>[];
};
