import { profesionales } from '../data/profesionales';
import type { Animal } from '../types/Animal';
import type { Resultado } from '../types/Cuenta';
import type { Profesional } from '../types/Profesional';
import type { EstadoTurno, Turno } from '../types/Turno';
import { inicioDelDia, mismoDia, sumarDias } from '../utils/fechas';
import { obtenerPorId as obtenerAnimal } from './animalService';
import { obtenerPorId as obtenerPrestacion, obtenerPublicas } from './prestacionService';
import { obtenerPublicos } from './profesionalService';

// Disponibilidad y turnos de demostración en memoria (sección B). Misma lógica que AgendaService de Blazor.
// En la sección C: GET /api/v1/disponibilidad y POST/PUT/DELETE /api/v1/turnos; la API revalida y protege la agenda.

// Reglas de P-RC-02 (parámetros por defecto, RF-ADM-10).
export const MINUTOS_ENTRE_HORARIOS = 15;
export const HORAS_DE_ANTICIPACION = 2;
export const DIAS_HACIA_ADELANTE = 60;
export const HORAS_PARA_CANCELAR = 24;
// RN-08: desde 2 ausencias sin aviso en 12 meses se advierte; desde 3 se reserva por Recepción.
export const AVISO_AUSENCIAS = 2;
export const LIMITE_AUSENCIAS = 3;
// RN-35: las cirugías esperan la seña (el porcentaje es el de CobroService de Blazor).
export const PORCENTAJE_SENA = 30;

const UNA_HORA = 60 * 60 * 1000;
const UN_MINUTO = 60 * 1000;

// Estados que ocupan la agenda (FLUJO_RESERVA_CONSULTA §4.1).
const ESTADOS_QUE_OCUPAN: EstadoTurno[] = ['Pendiente de seña', 'Confirmado', 'En espera', 'En curso'];
export const ocupa = (t: Turno) => ESTADOS_QUE_OCUPAN.includes(t.estado);

// Días no laborables de la clínica (los mismos que Blazor).
const feriados = ['2026-12-25', '2027-01-01', '2027-01-06'];
const clave = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
export const esFeriado = (dia: Date) => feriados.includes(clave(dia));

const turnos: Turno[] = [];
let ultimoId = 0;

const minutos = (texto: string) => {
  const [h, m] = texto.split(':').map(Number);
  return h * 60 + m;
};
const enDia = (dia: Date, hhmm: string) => new Date(dia.getFullYear(), dia.getMonth(), dia.getDate(), 0, minutos(hhmm)).getTime();

function profesionalPorId(id: number): Profesional | undefined {
  return profesionales.find((p) => p.id === id);
}

function agregar(datos: Omit<Turno, 'id' | 'motivoCancelacion' | 'fechaCancelacion' | 'canceladoPor' | 'inicioAnterior'> & Partial<Turno>): Turno {
  ultimoId++;
  const turno: Turno = { motivoCancelacion: '', fechaCancelacion: null, canceladoPor: '', inicioAnterior: null, ...datos, id: ultimoId };
  turnos.push(turno);
  return turno;
}

// Turnos de los clientes de demostración (Ana = 100, Pedro = 101) para hoy y mañana, como Blazor.
// Solo se cargan si el profesional trabaja ese día.
function cargarTurnosDemo(dia: Date) {
  // [profesional, cliente, animal, servicio, hora]
  const demo: [number, number, number, number, string][] = [
    [1, 100, 1, 1, '09:00'], // Ferreira: Toby, consulta
    [1, 100, 2, 2, '10:00'], // Ferreira: Mishi, vacunación
    [1, 100, 2, 3, '11:00'], // Ferreira: Mishi, desparasitación
    [5, 100, 1, 6, '12:00'], // Méndez: Toby, baño y estética
    [2, 101, 3, 1, '14:00'], // Silveira: rodeo, consulta
    [2, 101, 3, 2, '15:00'], // Silveira: rodeo, vacunación
    [3, 101, 4, 1, '09:00'], // Acosta: Canela, consulta
    [1, 100, 2, 8, '12:00'], // Ferreira: Mishi, castración
  ];
  for (const [profesionalId, clienteId, animalId, prestacionId, hhmm] of demo) {
    const profesional = profesionalPorId(profesionalId);
    const prestacion = obtenerPrestacion(prestacionId);
    if (!profesional || !prestacion?.duracionMinutos || !profesional.jornada.some((f) => f.dia === dia.getDay())) continue;
    const inicio = enDia(dia, hhmm);
    agregar({ clienteId, animalId, prestacionId, profesionalId, inicio, fin: inicio + prestacion.duracionMinutos * UN_MINUTO, estado: 'Confirmado', fechaCreacion: sumarDias(new Date(), -3).getTime() });
  }
}

// Dos turnos pasados de Ana para que el historial no esté vacío (solo en la app).
function cargarHistorialDemo() {
  const hoy = inicioDelDia(new Date());
  const realizado = enDia(sumarDias(hoy, -12), '10:00');
  agregar({ clienteId: 100, animalId: 1, prestacionId: 2, profesionalId: 1, inicio: realizado, fin: realizado + 15 * UN_MINUTO, estado: 'Finalizado', fechaCreacion: realizado - 5 * 24 * UNA_HORA });
  const cancelado = enDia(sumarDias(hoy, -5), '09:30');
  agregar({ clienteId: 100, animalId: 2, prestacionId: 1, profesionalId: 1, inicio: cancelado, fin: cancelado + 30 * UN_MINUTO, estado: 'Cancelado', fechaCreacion: cancelado - 7 * 24 * UNA_HORA, motivoCancelacion: 'Viaje', fechaCancelacion: cancelado - 2 * 24 * UNA_HORA, canceladoPor: 'Cliente' });
}

cargarHistorialDemo();
cargarTurnosDemo(inicioDelDia(new Date()));
cargarTurnosDemo(sumarDias(inicioDelDia(new Date()), 1));

export const primerDia = () => inicioDelDia(Date.now() + HORAS_DE_ANTICIPACION * UNA_HORA);
export const ultimoDia = () => sumarDias(inicioDelDia(new Date()), DIAS_HACIA_ADELANTE);

// Servicios que se reservan en línea (con duración).
export function reservables() {
  return obtenerPublicas().filter((p) => p.reservableEnLinea && (p.duracionMinutos ?? 0) > 0);
}

// Solo quienes hacen ese servicio y atienden esa especie (como ObtenerPara de Blazor).
export function profesionalesPara(prestacionId: number, especie: string): Profesional[] {
  return obtenerPublicos().filter((p) => p.prestacionIds.includes(prestacionId) && p.especies.includes(especie));
}

// Choca con otro turno activo del mismo profesional o del mismo animal.
function superpone(profesionalId: number, animalId: number, inicio: number, fin: number, excluir: number) {
  return turnos.some((t) => ocupa(t) && t.id !== excluir && (t.profesionalId === profesionalId || t.animalId === animalId) && inicio < t.fin && fin > t.inicio);
}

// Horarios de inicio libres cada 15 minutos. Un turno ocupa toda su duración (RN-01, RN-02, RN-07).
// excluirTurnoId: al reprogramar, el turno que se mueve no se cuenta como ocupado.
export function horariosLibres(profesionalId: number, prestacionId: number, animalId: number, dia: Date, excluirTurnoId = 0): number[] {
  const libres: number[] = [];
  const profesional = profesionalPorId(profesionalId);
  const prestacion = obtenerPrestacion(prestacionId);
  const duracion = (prestacion?.duracionMinutos ?? 0) * UN_MINUTO;
  if (!profesional || duracion <= 0) return libres;

  // Antelación mínima, horizonte y feriados (RN-05, RF-ADM-04).
  const d = inicioDelDia(dia);
  if (d < primerDia() || d > ultimoDia() || esFeriado(d)) return libres;

  const minimo = Date.now() + HORAS_DE_ANTICIPACION * UNA_HORA;
  for (const franja of profesional.jornada.filter((f) => f.dia === d.getDay())) {
    const finJornada = enDia(d, franja.fin);
    for (let horario = enDia(d, franja.inicio); horario + duracion <= finJornada; horario += MINUTOS_ENTRE_HORARIOS * UN_MINUTO) {
      const fin = horario + duracion;
      if (horario < minimo) continue;
      // No puede pisar un descanso del profesional.
      const pisaDescanso = profesional.descansos.some((x) => x.dia === d.getDay() && horario < enDia(d, x.fin) && fin > enDia(d, x.inicio));
      if (!pisaDescanso && !superpone(profesionalId, animalId, horario, fin, excluirTurnoId)) libres.push(horario);
    }
  }
  return libres;
}

// Primer día, desde `desde`, con al menos un horario libre (evita «sin horarios» con una cirugía de 90 min).
export function proximoDiaConHorarios(profesionalId: number, prestacionId: number, animalId: number, desde: Date, excluirTurnoId = 0): Date | null {
  for (let dia = desde < primerDia() ? primerDia() : inicioDelDia(desde); dia <= ultimoDia(); dia = sumarDias(dia, 1)) {
    if (horariosLibres(profesionalId, prestacionId, animalId, dia, excluirTurnoId).length > 0) return dia;
  }
  return null;
}

// Por qué un día no tiene horarios (mismos textos que Blazor).
export function motivoSinHorarios(profesionalId: number, dia: Date): string {
  const d = inicioDelDia(dia);
  const profesional = profesionalPorId(profesionalId);
  if (d < primerDia() || d > ultimoDia())
    return `Elegí un día entre hoy y los próximos ${DIAS_HACIA_ADELANTE} días, con al menos ${HORAS_DE_ANTICIPACION} horas de anticipación.`;
  if (esFeriado(d)) return 'Ese día la clínica no atiende. Elegí otro día.';
  if (profesional && !profesional.jornada.some((f) => f.dia === d.getDay())) return `${profesional.nombre} no atiende ese día de la semana. Elegí otro día.`;
  return 'No quedan horarios libres ese día. Probá con otro.';
}

// RN-08: ausencias sin aviso del cliente en los últimos 12 meses.
export function ausenciasRecientes(clienteId: number): number {
  const desde = new Date();
  desde.setMonth(desde.getMonth() - 12);
  return turnos.filter((t) => t.clienteId === clienteId && t.estado === 'Ausente' && t.inicio >= desde.getTime()).length;
}

export const bloqueadoPorAusencias = (clienteId: number) => ausenciasRecientes(clienteId) >= LIMITE_AUSENCIAS;

export function mensajeAusencias(clienteId: number): string {
  const n = ausenciasRecientes(clienteId);
  if (n >= LIMITE_AUSENCIAS) return `Tenés ${n} turnos sin asistir ni avisar en los últimos 12 meses: para reservar comunicate con Recepción.`;
  if (n >= AVISO_AUSENCIAS)
    return `Tenés ${n} turnos sin asistir ni avisar en los últimos 12 meses. Si llegás a ${LIMITE_AUSENCIAS}, vas a tener que reservar por Recepción. Si no podés venir, cancelá con anticipación.`;
  return '';
}

// Revalida todo al confirmar: el horario pudo ocuparse mientras el cliente elegía (CU-CLI-02, paso 5).
export function reservar(clienteId: number, animalId: number, prestacionId: number, profesionalId: number, inicio: number): Resultado & { turno?: Turno } {
  if (!obtenerAnimal(animalId, clienteId)) return { exito: false, mensaje: 'No encontramos ese animal entre los tuyos.' };
  if (bloqueadoPorAusencias(clienteId)) return { exito: false, mensaje: mensajeAusencias(clienteId) };

  const prestacion = obtenerPrestacion(prestacionId);
  if (!prestacion?.duracionMinutos || !horariosLibres(profesionalId, prestacionId, animalId, new Date(inicio)).includes(inicio))
    return { exito: false, mensaje: 'Ese horario ya no está disponible. Te mostramos los horarios actualizados.' };

  // RN-35: las cirugías arrancan esperando la seña.
  const estado: EstadoTurno = prestacion.requiereConsentimiento ? 'Pendiente de seña' : 'Confirmado';
  const turno = agregar({ clienteId, animalId, prestacionId, profesionalId, inicio, fin: inicio + prestacion.duracionMinutos * UN_MINUTO, estado, fechaCreacion: Date.now() });
  return {
    exito: true,
    mensaje: estado === 'Pendiente de seña' ? 'Horario reservado. La cirugía se confirma cuando pagues la seña en Recepción.' : 'Turno confirmado.',
    turno,
  };
}

// Próximos turnos del cliente (RF-CLI-04).
export function proximosDe(clienteId: number): Turno[] {
  return turnos.filter((t) => t.clienteId === clienteId && t.fin >= Date.now() && ocupa(t)).sort((a, b) => a.inicio - b.inicio);
}

// Cancelados o ya pasados (RF-CLI-04).
export function historialDe(clienteId: number): Turno[] {
  return turnos.filter((t) => t.clienteId === clienteId && (!ocupa(t) || t.fin < Date.now())).sort((a, b) => b.inicio - a.inicio);
}

// Solo turnos del propio cliente (RN-18).
export function obtenerDe(turnoId: number, clienteId: number): Turno | undefined {
  return turnos.find((t) => t.id === turnoId && t.clienteId === clienteId);
}

// RN-06: el cliente cancela o reprograma solo un turno Confirmado (o esperando seña) con más de 24 h.
export function puedeGestionar(t: Turno): boolean {
  return (t.estado === 'Confirmado' || t.estado === 'Pendiente de seña') && t.inicio - Date.now() > HORAS_PARA_CANCELAR * UNA_HORA;
}

export function cancelar(turnoId: number, clienteId: number, motivo: string): Resultado {
  const turno = obtenerDe(turnoId, clienteId);
  if (!turno || (turno.estado !== 'Confirmado' && turno.estado !== 'Pendiente de seña')) return { exito: false, mensaje: 'Ese turno ya no se puede cancelar.' };
  if (!puedeGestionar(turno)) return { exito: false, mensaje: `Faltan menos de ${HORAS_PARA_CANCELAR} horas: la cancelación se hace por Recepción.` };
  turno.estado = 'Cancelado';
  turno.motivoCancelacion = motivo.trim();
  turno.fechaCancelacion = Date.now();
  turno.canceladoPor = 'Cliente';
  return { exito: true, mensaje: 'El turno quedó cancelado y el horario se liberó.' };
}

// Cambia el horario del mismo turno. Si el nuevo no está libre, el turno queda como estaba.
export function reprogramar(turnoId: number, clienteId: number, nuevoInicio: number): Resultado {
  const turno = obtenerDe(turnoId, clienteId);
  if (!turno || !puedeGestionar(turno))
    return { exito: false, mensaje: `Este turno ya no se puede reprogramar. Si faltan menos de ${HORAS_PARA_CANCELAR} horas, consultá en Recepción.` };
  if (!horariosLibres(turno.profesionalId, turno.prestacionId, turno.animalId, new Date(nuevoInicio), turno.id).includes(nuevoInicio))
    return { exito: false, mensaje: 'Ese horario ya no está disponible. Tu turno sigue como estaba; te mostramos los horarios actualizados.' };
  const duracion = turno.fin - turno.inicio;
  turno.inicioAnterior = turno.inicio;
  turno.inicio = nuevoInicio;
  turno.fin = nuevoInicio + duracion;
  return { exito: true, mensaje: 'Turno reprogramado.' };
}

// SOLO DEMOSTRACIÓN: ocupa un horario como si otro cliente lo hubiera reservado mientras se confirmaba.
export function ocuparComoOtroCliente(profesionalId: number, prestacionId: number, inicio: number) {
  const prestacion = obtenerPrestacion(prestacionId);
  if (!prestacion?.duracionMinutos) return;
  agregar({ clienteId: 0, animalId: 0, prestacionId, profesionalId, inicio, fin: inicio + prestacion.duracionMinutos * UN_MINUTO, estado: 'Confirmado', fechaCreacion: Date.now() });
}

// «Consulta clínica general para Toby con Dra. Lucía Ferreira»
export function resumen(t: Turno, animal: Animal | undefined): string {
  const servicio = obtenerPrestacion(t.prestacionId)?.nombre ?? 'Servicio';
  const profesional = profesionalPorId(t.profesionalId)?.nombre ?? '';
  return `${servicio} para ${animal?.nombre ?? 'tu animal'} con ${profesional}`;
}

export { mismoDia };
