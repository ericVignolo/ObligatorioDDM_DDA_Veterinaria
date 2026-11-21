import type { Prestacion } from '../types/Prestacion';

// Datos ficticios de la sección B: los mismos que PrestacionService de Blazor.
// En la sección C se reemplazan por GET /api/v1/servicios sin tocar las pantallas.
export const prestaciones: Prestacion[] = [
  { id: 1, nombre: 'Consulta clínica general', descripcion: 'Revisión completa: anamnesis, examen físico, diagnóstico y tratamiento. Animales de compañía, equinos y otras especies.', precioDesde: 1200, reservableEnLinea: true, duracionMinutos: 30, publicado: true },
  { id: 2, nombre: 'Vacunación', descripcion: 'Aplicación con registro de producto, lote y fecha de refuerzo.', precioDesde: 600, reservableEnLinea: true, duracionMinutos: 15, publicado: true },
  { id: 3, nombre: 'Desparasitación', descripcion: 'Tratamiento antiparasitario indicado por el veterinario.', precioDesde: 450, reservableEnLinea: true, duracionMinutos: 15, publicado: true },
  { id: 4, nombre: 'Revisión de ovinos y bovinos', descripcion: 'Revisión individual o por grupo. Se coordina con la clínica.', precioDesde: 1500, reservableEnLinea: false, publicado: true },
  { id: 5, nombre: 'Rehabilitación equina', descripcion: 'Fisioterapia para caballos. Requiere evaluación previa.', precioDesde: 2500, reservableEnLinea: false, publicado: true },
  { id: 6, nombre: 'Baño y estética', descripcion: 'Baño, secado, corte y cuidado estético de perros y gatos.', precioDesde: 900, reservableEnLinea: true, duracionMinutos: 60, publicado: true },
  { id: 8, nombre: 'Castración', descripcion: 'Cirugía programada con anestesia general. Requiere firmar el consentimiento informado antes del día del turno.', precioDesde: 4500, reservableEnLinea: true, duracionMinutos: 90, publicado: true, requiereConsentimiento: true },
  { id: 7, nombre: 'Guardería', descripcion: 'No publicado: no debe aparecer.', precioDesde: 700, reservableEnLinea: false, publicado: false },
];
