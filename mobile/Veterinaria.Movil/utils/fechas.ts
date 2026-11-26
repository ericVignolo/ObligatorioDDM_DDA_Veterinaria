// Fechas en español sin depender de Intl (Hermes en Android puede no traer todos los idiomas).
const DIAS = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'];
const DIAS_CORTOS = ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'];
const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];

const dos = (n: number) => String(n).padStart(2, '0');

// Día sin hora, a las 00:00 locales.
export function inicioDelDia(fecha: Date | number): Date {
  const d = new Date(fecha);
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
}

export function sumarDias(fecha: Date, dias: number): Date {
  return new Date(fecha.getFullYear(), fecha.getMonth(), fecha.getDate() + dias);
}

export function mismoDia(a: Date | number, b: Date | number): boolean {
  return inicioDelDia(a).getTime() === inicioDelDia(b).getTime();
}

// «14:30»
export function hora(fecha: Date | number): string {
  const d = new Date(fecha);
  return `${dos(d.getHours())}:${dos(d.getMinutes())}`;
}

// «jueves 26 de noviembre»
export function diaLargo(fecha: Date | number): string {
  const d = new Date(fecha);
  return `${DIAS[d.getDay()]} ${d.getDate()} de ${MESES[d.getMonth()]}`;
}

// «jue 26/11/2026»
export function diaCorto(fecha: Date | number): string {
  const d = new Date(fecha);
  return `${DIAS_CORTOS[d.getDay()]} ${d.getDate()}/${dos(d.getMonth() + 1)}/${d.getFullYear()}`;
}

// Para las fichas de días: «jue» y «26 nov»
export function chipDia(fecha: Date): { semana: string; fecha: string } {
  return { semana: DIAS_CORTOS[fecha.getDay()], fecha: `${fecha.getDate()} ${MESES[fecha.getMonth()].slice(0, 3)}` };
}

export function nombreDia(numero: number): string {
  return DIAS[numero];
}
