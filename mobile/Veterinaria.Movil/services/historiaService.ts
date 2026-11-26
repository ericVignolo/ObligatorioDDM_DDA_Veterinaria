import { consultasDemo } from '../data/consultasDemo';
import type { Adjunto, Consulta } from '../types/Consulta';
import { obtenerPorId as obtenerAnimal } from './animalService';

// Historia clínica del Cliente en modo lectura (B-009, RF-CLI-05, CU-CLI-05). Misma idea que HistoriaDe de Blazor.
// En la sección C: GET /api/v1/animales/{id}/historia y, para un adjunto, un acceso temporal emitido por la API.

// Vigencia del acceso temporal simulado a un adjunto (CU-CLI-05, paso 6). En la API lo define RNF-SEG.
export const MINUTOS_ACCESO_ADJUNTO = 5;

// Solo consultas firmadas de un animal del cliente (RN-18), de la más reciente a la más antigua.
// Si el animal no es suyo, devuelve null sin distinguir el caso.
export function historiaDe(animalId: number, clienteId: number): Consulta[] | null {
  if (!obtenerAnimal(animalId, clienteId)) return null;
  return consultasDemo.filter((c) => c.animalId === animalId).sort((a, b) => b.fechaFirma - a.fechaFirma);
}

// Signos vitales en una línea, como RegistroClinico.razor.
export function signosVitales(c: Consulta): string {
  const partes: string[] = [];
  const n = (v: number) => v.toLocaleString('es-UY', { maximumFractionDigits: 2 });
  if (c.pesoKg !== null) partes.push(`peso ${n(c.pesoKg)} kg`);
  if (c.temperaturaC !== null) partes.push(`temperatura ${n(c.temperaturaC)} °C`);
  if (c.frecuenciaCardiaca !== null) partes.push(`FC ${c.frecuenciaCardiaca} lpm`);
  if (c.frecuenciaRespiratoria !== null) partes.push(`FR ${c.frecuenciaRespiratoria} rpm`);
  return partes.join(' · ');
}

// Busca un adjunto dentro de la historia autorizada del cliente.
export function adjuntoDe(animalId: number, adjuntoId: number, clienteId: number): { consulta: Consulta; adjunto: Adjunto } | null {
  const historia = historiaDe(animalId, clienteId);
  for (const consulta of historia ?? []) {
    const adjunto = consulta.adjuntos.find((a) => a.id === adjuntoId);
    if (adjunto) return { consulta, adjunto };
  }
  return null;
}

// Acceso temporal simulado: vence a los pocos minutos y hay que volver a pedirlo (sin enlaces permanentes).
export function emitirAcceso(): number {
  return Date.now() + MINUTOS_ACCESO_ADJUNTO * 60 * 1000;
}
