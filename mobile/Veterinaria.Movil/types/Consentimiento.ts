// Consentimiento informado (B-012, RF-CLI-11, CU-CLI-10). Mismos campos que Models/DocumentoConsentimiento.cs
// y Consentimiento.cs de Blazor.
export type DocumentoConsentimiento = {
  id: number;
  prestacionId: number;
  version: string;
  titulo: string;
  texto: string;
  vigenteDesde: number;
  vigente: boolean;
};

// Una respuesta nunca se modifica: la siguiente es otro registro (K-05).
export type Consentimiento = {
  id: number;
  turnoId: number;
  clienteId: number;
  animalId: number;
  prestacionId: number;
  documentoId: number;
  version: string;
  respuesta: 'Aceptado' | 'Rechazado';
  nombreEscrito: string;
  canal: string;
  fecha: number;
  hash: string;
};

export type EstadoConsentimiento = 'No requiere' | 'Pendiente' | 'Firmado' | 'Rechazado' | 'Versión nueva';
