import { prestaciones } from '../data/prestaciones';
import type { Prestacion } from '../types/Prestacion';

// D-CAT-02/04: un servicio es público si está publicado y tiene precio.
const esPublica = (p: Prestacion) => p.publicado && p.precioDesde > 0;

export function obtenerPublicas(): Prestacion[] {
  return prestaciones.filter(esPublica);
}

export function obtenerPorId(id: number): Prestacion | undefined {
  return prestaciones.find((p) => p.id === id && esPublica(p));
}

export function formatearPrecio(valor: number): string {
  return `$ ${valor.toLocaleString('es-UY', { maximumFractionDigits: 0 })}`;
}
