// Turno reservado (B-008, CU-CLI-02). Mismos campos que Models/Turno.cs de Blazor que usa el Cliente.
// En la sección C lo crea la API dentro de una transacción (DT-BD-02).
export type EstadoTurno = 'Pendiente de seña' | 'Confirmado' | 'En espera' | 'En curso' | 'Finalizado' | 'Ausente' | 'Cancelado';

export type Turno = {
  id: number;
  clienteId: number;
  animalId: number;
  prestacionId: number;
  profesionalId: number;
  inicio: number; // milisegundos (hora local)
  fin: number;
  estado: EstadoTurno;
  fechaCreacion: number;
  // Cancelación: motivo opcional, cuándo y quién (Cliente o Recepción).
  motivoCancelacion: string;
  fechaCancelacion: number | null;
  canceladoPor: string;
  // Reprogramación: el turno conserva su número y se guarda el horario anterior.
  inicioAnterior: number | null;
};
