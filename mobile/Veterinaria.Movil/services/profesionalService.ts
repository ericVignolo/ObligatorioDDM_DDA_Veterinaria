import { profesionales } from '../data/profesionales';
import type { Profesional } from '../types/Profesional';

const DIAS = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

export function obtenerPublicos(): Profesional[] {
  return profesionales.filter((p) => p.publicado);
}

export function obtenerPorId(id: number): Profesional | undefined {
  return obtenerPublicos().find((p) => p.id === id);
}

// Igual que DescribirJornada de Blazor: «Lunes: 09:00 a 14:00 (descanso 11:30 a 12:00)», de lunes a domingo.
export function describirJornada(p: Profesional): string[] {
  return [...p.jornada]
    .sort((a, b) => (a.dia + 6) % 7 - (b.dia + 6) % 7 || a.inicio.localeCompare(b.inicio))
    .map((f) => {
      const descanso = p.descansos.find((d) => d.dia === f.dia && d.inicio >= f.inicio && d.fin <= f.fin);
      return `${DIAS[f.dia]}: ${f.inicio} a ${f.fin}` + (descanso ? ` (descanso ${descanso.inicio} a ${descanso.fin})` : '');
    });
}
