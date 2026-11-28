// Solicitud de orientación o asistencia (B-013 y B-014, RF-CLI-21 a 25). Mismos campos que Models/SolicitudAsistencia.cs
// de Blazor, más tres datos de la sección 5 de la especificación que el escritorio no pide: inicio, peso y riesgos.
export type NivelUrgencia = 'Rojo' | 'Naranja' | 'Amarillo' | 'Verde';

export type Modalidad = 'Visita' | 'Orientación y traslado';

export type EstadoAsistencia =
  | 'Pendiente de asignación'
  | 'Ofrecida'
  | 'Requiere coordinación de Recepción'
  | 'Aceptada'
  | 'En camino'
  | 'En el lugar'
  | 'En atención'
  | 'Finalizada'
  | 'Cancelada por la clínica'
  | 'No concretada';

export type SolicitudAsistencia = {
  id: number;
  clienteId: number;
  animalId: number; // 0 = animal sin registrar o grupo descrito en la solicitud (RN-59)
  especie: string;
  cantidad: number;
  sintomas: string;
  inicioSintomas: string;
  pesoAproximado: string;
  riesgos: string;
  modalidad: Modalidad;
  direccion: string;
  referencias: string;
  // GPS solo con consentimiento para esta asistencia (RF-CLI-22, RNF-LEG-09); se descarta al cerrarla.
  ubicacionGps: boolean;
  latitud: number | null;
  longitud: number | null;
  distanciaKm: number; // 0 en la app: la calcula el mapa o la carga Recepción
  telefonoContacto: string;
  canal: 'App' | 'Teléfono';
  claveIdempotencia: string;
  nivelCliente: NivelUrgencia;
  nivel: NivelUrgencia; // antes de la revisión profesional solo puede subir (RN-46)
  estado: EstadoAsistencia;
  fechaCreacion: number;
  ofrecidaA: number[]; // veterinarios (id de personal) a los que está ofrecida
  fechaOferta: number | null;
  // Veterinario que aceptó y llegada estimada (RN-53: solo después de aceptar).
  veterinarioId: number;
  veterinarioNombre: string;
  fechaAceptacion: number | null;
  etaMinutos: number | null;
  costo: number;
  reevaluacionPedida: boolean;
  reevaluacionRespondida: boolean;
  motivoCoordinacion: string;
  motivoCierre: string;
};

export type EventoAsistencia = {
  id: number;
  solicitudId: number;
  fecha: number;
  tipo: string;
  texto: string;
  autor: string;
  nivelAnterior?: NivelUrgencia;
  nivelNuevo?: NivelUrgencia;
  claveIdempotencia?: string;
};

// Aviso de empeoramiento del cliente (RN-51).
export type DatosEmpeoramiento = { nivel: NivelUrgencia | ''; motivo: string; claveIdempotencia: string };

// Datos del formulario. La cantidad se escribe como texto.
export type DatosSolicitud = {
  modalidad: Modalidad | '';
  animalId: number | null; // null = todavía no eligió; 0 = sin registrar o grupo
  especie: string;
  cantidad: string;
  sintomas: string;
  inicioSintomas: string;
  pesoAproximado: string;
  riesgos: string;
  nivel: NivelUrgencia | '';
  direccion: string;
  referencias: string;
  latitud: number | null;
  longitud: number | null;
  telefonoContacto: string;
  aceptaCosto: boolean;
  claveIdempotencia: string;
};
