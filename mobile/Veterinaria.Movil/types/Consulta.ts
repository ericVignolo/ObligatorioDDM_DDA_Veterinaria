// Consulta firmada de la historia clínica (B-009, RF-CLI-05). Mismos campos que Models/Consulta.cs de Blazor
// que puede ver el Cliente: sin la huella del contenido firmado, que solo ve el personal.
// El Cliente nunca recibe borradores (A-RC-14): la API devuelve solo consultas firmadas de sus animales (RN-18).
export type Enmienda = {
  id: number;
  motivo: string;
  texto: string;
  autorNombre: string;
  fecha: number;
};

// Adjunto de una consulta (RF-CLI-08, CU-CLI-05). En la sección C la API emite un acceso temporal para verlo.
export type Adjunto = {
  id: number;
  nombre: string;
  tipo: 'imagen' | 'pdf';
  tamanoKb: number;
  // Retirado por la clínica: se lista pero ya no se puede abrir (CU-CLI-05, alternativa).
  retirado: boolean;
};

export type Consulta = {
  id: number;
  animalId: number;
  autorNombre: string;
  fechaFirma: number;
  motivo: string;
  anamnesis: string;
  examenFisico: string;
  temperaturaC: number | null;
  frecuenciaCardiaca: number | null;
  frecuenciaRespiratoria: number | null;
  pesoKg: number | null;
  alergias: string;
  diagnostico: string;
  tipoDiagnostico: 'Presuntivo' | 'Definitivo';
  tratamiento: string;
  indicaciones: string;
  enmiendas: Enmienda[];
  adjuntos: Adjunto[];
};
