// Acción protegida interrumpida (D-REG-05, A-011): solo se guarda el servicio elegido (o que quería pedir asistencia)
// y la hora, nunca en el servidor. Dura 30 minutos; pasado ese tiempo se descarta sin aviso y se vuelve al inicio (REG-09).
// En memoria, como en IntencionService de Blazor.
const MINUTOS_VIGENCIA = 30;

type Intencion = number | 'asistencia' | null; // número = id del servicio a reservar

let intencion: Intencion = null;
let guardada = 0;

export function guardarIntencion(nueva: number | 'asistencia') {
  intencion = nueva;
  guardada = Date.now();
}

function vigente(): Intencion {
  if (intencion !== null && Date.now() - guardada > MINUTOS_VIGENCIA * 60 * 1000) {
    intencion = null;
  }
  return intencion;
}

// Servicio pendiente si sigue vigente; si no, null.
export function intencionVigente(): number | null {
  const actual = vigente();
  return typeof actual === 'number' ? actual : null;
}

// Pantalla a la que hay que volver después de ingresar o verificar, o null si no quedó nada pendiente.
export function rutaPendiente() {
  const actual = vigente();
  if (actual === null) return null;
  return actual === 'asistencia'
    ? ({ pathname: '/asistencia/nueva' } as const)
    : ({ pathname: '/reservar', params: { servicio: String(actual) } } as const);
}

export const asistenciaPendiente = () => vigente() === 'asistencia';

export function descartarIntencion() {
  intencion = null;
}
