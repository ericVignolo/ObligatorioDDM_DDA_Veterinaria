// Acción protegida interrumpida (D-REG-05, A-011): solo se guarda el servicio elegido y la hora, nunca en el servidor.
// Dura 30 minutos; pasado ese tiempo se descarta sin aviso y se vuelve al inicio (REG-09).
// En memoria, como en IntencionService de Blazor.
const MINUTOS_VIGENCIA = 30;

let servicioId: number | null = null;
let guardada = 0;

export function guardarIntencion(id: number) {
  servicioId = id;
  guardada = Date.now();
}

// Servicio pendiente si sigue vigente; si no, null.
export function intencionVigente(): number | null {
  if (servicioId !== null && Date.now() - guardada > MINUTOS_VIGENCIA * 60 * 1000) {
    servicioId = null;
  }
  return servicioId;
}

export function descartarIntencion() {
  servicioId = null;
}
