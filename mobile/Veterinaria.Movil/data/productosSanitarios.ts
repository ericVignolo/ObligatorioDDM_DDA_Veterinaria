import type { ProductoSanitario } from '../types/Vacuna';

// El mismo catálogo de demostración que VacunaService de Blazor (a validar por un veterinario).
export const AVISO_CATALOGO_DEMO =
  'Catálogo de demostración: productos, especies e intervalos de refuerzo a validar por un veterinario antes de usarlos en la clínica.';

export const productosSanitarios: ProductoSanitario[] = [
  { id: 1, nombre: 'Vacuna polivalente canina', tipo: 'Vacuna', laboratorio: 'Laboratorio Demo Norte', especies: ['Perro'], intervaloRefuerzoDias: 365 },
  { id: 2, nombre: 'Vacuna antirrábica', tipo: 'Vacuna', laboratorio: 'Laboratorio Demo Sur', especies: ['Perro', 'Gato'], intervaloRefuerzoDias: 365 },
  { id: 3, nombre: 'Vacuna triple felina', tipo: 'Vacuna', laboratorio: 'Laboratorio Demo Norte', especies: ['Gato'], intervaloRefuerzoDias: 365 },
  { id: 4, nombre: 'Vacuna antitetánica equina', tipo: 'Vacuna', laboratorio: 'Laboratorio Demo Campo', especies: ['Caballo'], intervaloRefuerzoDias: 365 },
  { id: 5, nombre: 'Vacuna clostridial', tipo: 'Vacuna', laboratorio: 'Laboratorio Demo Campo', especies: ['Vaca', 'Oveja', 'Cabra'], intervaloRefuerzoDias: 180 },
  { id: 6, nombre: 'Antiparasitario interno', tipo: 'Antiparasitario', laboratorio: 'Laboratorio Demo Sur', especies: ['Perro', 'Gato'], intervaloRefuerzoDias: 90 },
  { id: 7, nombre: 'Antiparasitario externo', tipo: 'Antiparasitario', laboratorio: 'Laboratorio Demo Sur', especies: ['Perro', 'Gato'], intervaloRefuerzoDias: 30 },
];
