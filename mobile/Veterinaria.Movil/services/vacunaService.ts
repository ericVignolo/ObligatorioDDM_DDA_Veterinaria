import { AVISO_CATALOGO_DEMO, productosSanitarios } from '../data/productosSanitarios';
import type { Animal } from '../types/Animal';
import type { AplicacionSanitaria, EstadoRefuerzo, ItemCarnet } from '../types/Vacuna';
import { inicioDelDia, sumarDias } from '../utils/fechas';
import { obtenerDe as animalesDe, obtenerPorId as obtenerAnimal } from './animalService';

// Carné sanitario y alertas de refuerzo del Cliente (B-010, RF-CLI-07), misma lógica que VacunaService de Blazor.
// En la sección C: GET /api/v1/animales/{id}/carnet y GET /api/v1/alertas-sanitarias; los avisos por bandeja
// y correo los manda un proceso diario de la API (A-028).

export { AVISO_CATALOGO_DEMO };

// Días de anticipación del aviso: 5 por defecto (decisión del 31/10, entre 3 y 5). Lo configura el Administrador.
export const DIAS_AVISO_REFUERZO = 5;

// Servicios del catálogo para ponerse al día: vacunación (2) o desparasitación (3).
export const servicioPara = (tipo: AplicacionSanitaria['tipo']) => (tipo === 'Vacuna' ? 2 : 3);

const DIA = 24 * 60 * 60 * 1000;
const aplicaciones: AplicacionSanitaria[] = [];
let ultimoId = 0;

// Diferencia en días de calendario (sin depender de la hora).
const diasEntre = (desde: number, hasta: number) => Math.round((inicioDelDia(hasta).getTime() - inicioDelDia(desde).getTime()) / DIA);

function agregarDemo(animalId: number, consultaId: number, productoId: number, haceDias: number, autor: string) {
  const producto = productosSanitarios.find((p) => p.id === productoId);
  if (!producto) return;
  const fecha = sumarDias(inicioDelDia(new Date()), -haceDias);
  ultimoId++;
  aplicaciones.push({
    id: ultimoId,
    animalId,
    consultaId,
    productoId,
    producto: producto.nombre,
    tipo: producto.tipo,
    laboratorio: producto.laboratorio,
    // Blazor muestra el lote vigente del inventario de demostración: L<artículo>-B.
    lote: `L${producto.id}-B`,
    fechaAplicacion: fecha.getTime(),
    // RN-19: la fecha de refuerzo la calcula el producto.
    fechaRefuerzo: sumarDias(fecha, producto.intervaloRefuerzoDias).getTime(),
    aplicadaPor: autor,
  });
}

// Los mismos casos que Blazor: vigente, refuerzo próximo y vencido, sin esperar meses.
agregarDemo(1, 1, 1, 60, 'Dra. Lucía Ferreira'); // Toby: polivalente, de su consulta firmada → vigente
agregarDemo(1, 0, 6, 86, 'Dra. Lucía Ferreira'); // Toby: antiparasitario interno → refuerzo en 4 días
agregarDemo(1, 0, 2, 400, 'Dra. Lucía Ferreira'); // Toby: antirrábica → vencida
agregarDemo(2, 0, 3, 200, 'Dra. Lucía Ferreira'); // Mishi: triple felina → vigente
agregarDemo(4, 0, 4, 100, 'Dra. Valentina Acosta'); // Canela: antitetánica → vigente

export function estadoDe(a: AplicacionSanitaria): EstadoRefuerzo {
  const dias = diasEntre(Date.now(), a.fechaRefuerzo);
  if (dias < 0) return 'Vencida';
  return dias <= DIAS_AVISO_REFUERZO ? 'Refuerzo próximo' : 'Vigente';
}

// Todas las aplicaciones de un animal del cliente, de la más reciente a la más antigua (RN-18).
export function historialDe(animalId: number, clienteId: number): AplicacionSanitaria[] {
  if (!obtenerAnimal(animalId, clienteId)) return [];
  return aplicaciones.filter((a) => a.animalId === animalId).sort((x, y) => y.fechaAplicacion - x.fechaAplicacion || y.id - x.id);
}

// Carné: la última aplicación de cada producto con el estado de su refuerzo; primero lo más urgente.
export function carnetDe(animalId: number, clienteId: number): ItemCarnet[] {
  const vistos = new Set<number>();
  const items: ItemCarnet[] = [];
  for (const a of historialDe(animalId, clienteId)) {
    if (vistos.has(a.productoId)) continue;
    vistos.add(a.productoId);
    items.push({ aplicacion: a, estado: estadoDe(a), diasParaRefuerzo: diasEntre(Date.now(), a.fechaRefuerzo) });
  }
  return items.sort((x, y) => x.aplicacion.fechaRefuerzo - y.aplicacion.fechaRefuerzo);
}

// RF-CLI-07: refuerzos próximos o vencidos de los animales del cliente, primero lo más urgente.
export function alertasDe(clienteId: number): { animal: Animal; item: ItemCarnet }[] {
  return animalesDe(clienteId)
    .flatMap((animal) => carnetDe(animal.id, clienteId).filter((i) => i.estado !== 'Vigente').map((item) => ({ animal, item })))
    .sort((x, y) => x.item.diasParaRefuerzo - y.item.diasParaRefuerzo);
}

// Vacunas y antiparasitarios aplicados en una consulta firmada (para la historia clínica).
export function deConsulta(consultaId: number): AplicacionSanitaria[] {
  return aplicaciones.filter((a) => a.consultaId === consultaId && consultaId !== 0);
}

// Texto de la alerta, como CarnetSanitario.razor.
export function textoAlerta(item: ItemCarnet, fecha: string): string {
  if (item.estado === 'Vencida') return `El refuerzo venció el ${fecha}. Pedí un turno para ponerlo al día.`;
  if (item.diasParaRefuerzo === 0) return `El refuerzo es hoy (${fecha}).`;
  return `El refuerzo es en ${item.diasParaRefuerzo} ${item.diasParaRefuerzo === 1 ? 'día' : 'días'} (${fecha}).`;
}

// Carné de ejemplo para los animales de «Cargar ejemplos» (como CargarCarnetEjemplo de Blazor):
// el caballo con el refuerzo por vencer y la majada con uno vencido. No se repite si ya tienen aplicaciones.
export function cargarCarnetEjemplo(clienteId: number) {
  for (const animal of animalesDe(clienteId)) {
    if (aplicaciones.some((a) => a.animalId === animal.id)) continue;
    if (animal.nombre === 'Lucero' && animal.especie === 'Caballo') agregarDemo(animal.id, 0, 4, 361, 'Dra. Valentina Acosta');
    else if (animal.nombre === 'Majada del potrero norte' && animal.especie === 'Oveja') agregarDemo(animal.id, 0, 5, 200, 'Dra. Valentina Acosta');
  }
}
