import { cuentasDemo } from '../data/cuentasDemo';
import { profesionales } from '../data/profesionales';
import type { Errores, Resultado } from '../types/Cuenta';
import type {
  DatosEmpeoramiento,
  DatosSolicitud,
  EstadoAsistencia,
  EventoAsistencia,
  Modalidad,
  NivelUrgencia,
  SolicitudAsistencia,
} from '../types/Asistencia';
import { normalizarTelefono } from '../utils/validacion';
import { especies, obtenerPorId as obtenerAnimal } from './animalService';
import { obtenerPorId as obtenerCuenta } from './cuentaService';

// Solicitudes de asistencia del Cliente en memoria (B-013 y B-014, ESPECIFICACION_ALERTAS_ASISTENCIA, RN-46 a RN-59),
// con las mismas reglas que AsistenciaService de Blazor: creación, plazos de 10 minutos y 24 horas, empeoramiento.
// En la sección C: POST y GET /api/v1/asistencias y POST /api/v1/asistencias/{id}/empeoramiento; la API valida,
// clasifica, calcula la distancia, ejecuta los plazos con una tarea programada y avisa por notificaciones.

export const NIVELES: { nombre: NivelUrgencia; simbolo: string; descripcion: string }[] = [
  { nombre: 'Rojo', simbolo: '‼', descripcion: 'Posible riesgo vital: guardia inmediata' },
  { nombre: 'Naranja', simbolo: '!', descripcion: 'Urgencia alta: en el horario de la clínica, primero' },
  { nombre: 'Amarillo', simbolo: '●', descripcion: 'Prioritaria: tolera una espera controlada' },
  { nombre: 'Verde', simbolo: '✓', descripcion: 'Estable o programable' },
];

export const MODALIDADES: { nombre: Modalidad; detalle: string }[] = [
  { nombre: 'Visita', detalle: 'Un veterinario va a tu casa o a tu campo' },
  { nombre: 'Orientación y traslado', detalle: 'Te orientan y coordinás llevarlo a la clínica' },
];

// Parámetros por defecto (decisiones del 31/10); los edita el Administrador.
export const COBERTURA_KM = 30;
export const MINUTOS_ESCALAR_ROJO = 10;
export const HORAS_REEVALUAR = 24;
export const TARIFA_BASE = 1500;
export const TARIFA_POR_KM = 40;
export const TARIFA_POR_ANIMAL_EXTRA = 300;
export const CANTIDAD_MAXIMA = 1000;
export const LARGOS = { sintomas: 1000, inicioSintomas: 100, pesoAproximado: 40, riesgos: 300, direccion: 200, referencias: 300, telefono: 30, motivo: 300 };

// Veterinario de guardia de demostración (id de personal): Dra. Ferreira.
const GUARDIA_ID = 2;

const solicitudes: SolicitudAsistencia[] = [];
const eventos: EventoAsistencia[] = [];
let ultimaSolicitud = 0;
let ultimoEvento = 0;

// 0 = máxima prioridad.
export const prioridad = (nivel: NivelUrgencia) => NIVELES.findIndex((n) => n.nombre === nivel);

// RN-58: base + distancia + animales extra; la orientación no tiene costo.
export const calcularCosto = (modalidad: Modalidad, distanciaKm: number, cantidad: number) =>
  modalidad === 'Visita' ? TARIFA_BASE + TARIFA_POR_KM * distanciaKm + TARIFA_POR_ANIMAL_EXTRA * Math.max(0, cantidad - 1) : 0;

// RF-CLI-25: hay que aceptar el costo antes de confirmar, salvo que no tenga costo o sea un rojo (no se demora la urgencia).
export const pideAceptarCosto = (modalidad: Modalidad | '', nivel: NivelUrgencia | '') => modalidad === 'Visita' && nivel !== 'Rojo';

// RN-48: lunes a viernes de 9 a 19 y sábados de 9 a 13.
export function enHorarioClinica(momento: number): boolean {
  const d = new Date(momento);
  const horas = d.getHours() + d.getMinutes() / 60;
  if (d.getDay() === 0) return false;
  return horas >= 9 && horas < (d.getDay() === 6 ? 13 : 19);
}

export const sinAceptar = (s: SolicitudAsistencia) =>
  s.estado === 'Pendiente de asignación' || s.estado === 'Ofrecida' || s.estado === 'Requiere coordinación de Recepción';

// RN-48: fuera del horario, lo no rojo y sin aceptar queda para el próximo día de atención.
export const paraProximoDia = (s: SolicitudAsistencia, ahora = Date.now()) => s.nivel !== 'Rojo' && sinAceptar(s) && !enHorarioClinica(ahora);

export const estaActiva = (s: SolicitudAsistencia) => !['Finalizada', 'Cancelada por la clínica', 'No concretada'].includes(s.estado);

// RN-57: la ubicación del veterinario se comparte solo durante el traslado.
export const compartiendoUbicacion = (s: SolicitudAsistencia) => s.estado === 'En camino';

// RN-50: pasaron 24 horas sin aceptar y el cliente todavía no respondió la reevaluación.
export const pideReevaluacion = (s: SolicitudAsistencia) => estaActiva(s) && s.reevaluacionPedida && !s.reevaluacionRespondida;

const fueraDeCobertura = (s: SolicitudAsistencia) => s.estado === 'Requiere coordinación de Recepción' && s.motivoCoordinacion.startsWith('Fuera de cobertura');

// RN-54: especie y, para un rojo, además de guardia o dentro de su jornada. La cercanía no vuelve elegible a nadie.
// En la demostración todas las matrículas están vigentes.
function elegibles(s: SolicitudAsistencia, ahora: number) {
  const d = new Date(ahora);
  const hhmm = `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  return cuentasDemo.filter((u) => {
    if (u.rol !== 'Veterinario') return false;
    const ficha = profesionales.find((p) => p.id === u.profesionalId);
    if (!ficha || !ficha.especies.includes(s.especie)) return false;
    return s.nivel !== 'Rojo' || u.id === GUARDIA_ID || ficha.jornada.some((f) => f.dia === d.getDay() && hhmm >= f.inicio && hhmm < f.fin);
  });
}

function evento(s: SolicitudAsistencia, fecha: number, tipo: string, texto: string, autor: string, extra: Partial<EventoAsistencia> = {}) {
  ultimoEvento++;
  eventos.push({ id: ultimoEvento, solicitudId: s.id, fecha, tipo, texto, autor, ...extra });
}

// RN-55: sin nadie elegible o fuera de cobertura, la solicitud no se descarta: pasa a Recepción.
function escalar(s: SolicitudAsistencia, ahora: number, motivo: string) {
  s.estado = 'Requiere coordinación de Recepción';
  s.ofrecidaA = [];
  s.fechaOferta = null;
  s.motivoCoordinacion = motivo;
  evento(s, ahora, 'Escalada a Recepción', motivo, 'Sistema');
}

// RN-49: el rojo se ofrece enseguida a los elegibles y a la guardia.
function activarGuardia(s: SolicitudAsistencia, ahora: number) {
  const lista = elegibles(s, ahora);
  if (lista.length === 0) return escalar(s, ahora, 'Rojo sin veterinario elegible de guardia o en jornada.');
  s.estado = 'Ofrecida';
  s.ofrecidaA = lista.map((u) => u.id);
  s.fechaOferta = ahora;
  evento(s, ahora, 'Ofrecida', `Ofrecida a ${lista.map((u) => u.nombre).join(', ')}.`, 'Sistema');
}

type Nueva = Omit<
  SolicitudAsistencia,
  | 'id'
  | 'estado'
  | 'nivel'
  | 'fechaCreacion'
  | 'ofrecidaA'
  | 'fechaOferta'
  | 'costo'
  | 'motivoCoordinacion'
  | 'motivoCierre'
  | 'veterinarioId'
  | 'veterinarioNombre'
  | 'fechaAceptacion'
  | 'etaMinutos'
  | 'reevaluacionPedida'
  | 'reevaluacionRespondida'
>;

function agregar(datos: Nueva, ahora: number, autor: string): SolicitudAsistencia {
  ultimaSolicitud++;
  const s: SolicitudAsistencia = {
    ...datos,
    id: ultimaSolicitud,
    // D-ASI-01: sin cuestionario clínico aprobado, el nivel preliminar es el que indica el cliente.
    nivel: datos.nivelCliente,
    estado: 'Pendiente de asignación',
    fechaCreacion: ahora,
    ofrecidaA: [],
    fechaOferta: null,
    veterinarioId: 0,
    veterinarioNombre: '',
    fechaAceptacion: null,
    etaMinutos: null,
    costo: calcularCosto(datos.modalidad, datos.distanciaKm, datos.cantidad),
    reevaluacionPedida: false,
    reevaluacionRespondida: false,
    motivoCoordinacion: '',
    motivoCierre: '',
  };
  solicitudes.push(s);
  evento(s, ahora, 'Recibida', `${s.canal === 'App' ? 'Desde la app' : 'Por teléfono'}. Nivel indicado: ${s.nivel}.`, autor);
  if (s.modalidad === 'Visita' && s.distanciaKm > COBERTURA_KM) {
    escalar(s, ahora, `Fuera de cobertura (${s.distanciaKm} km, máximo ${COBERTURA_KM} km): la visita no está confirmada.`);
  } else if (s.nivel === 'Rojo') {
    activarGuardia(s, ahora);
  }
  return s;
}

// Las mismas cinco solicitudes con las que arranca el escritorio.
function cargarDemo() {
  const ahora = Date.now();
  const demo: [number, number, string, number, string, NivelUrgencia, Modalidad, string, number, number][] = [
    // cliente, animal, especie, cantidad, síntomas, nivel, modalidad, dirección, km, minutos atrás
    [101, 3, 'Vaca', 25, 'Varias vacas rengas y una caída que no se levanta.', 'Naranja', 'Visita', 'Ruta 21 km 230, Establecimiento La Esperanza', 18, 120],
    [100, 1, 'Perro', 1, 'Vómitos desde ayer, come poco.', 'Amarillo', 'Visita', 'Rivera 1234, Nueva Palmira', 6, 26 * 60],
    [100, 2, 'Gato', 1, 'Consulta si es normal que esté decaída después de la vacuna.', 'Verde', 'Orientación y traslado', '', 0, 60],
    [101, 0, 'Oveja', 1, 'Oveja con dificultad para parir.', 'Amarillo', 'Visita', 'Paraje Las Flores, camino vecinal', 45, 40],
    [101, 0, 'Caballo', 1, 'Potrillo con cólico fuerte, se revuelca.', 'Rojo', 'Visita', 'Chacra Los Álamos, Carmelo', 12, 15],
  ];
  for (const [clienteId, animalId, especie, cantidad, sintomas, nivel, modalidad, direccion, distanciaKm, minutos] of demo) {
    agregar(
      {
        clienteId, animalId, especie, cantidad, sintomas, inicioSintomas: '', pesoAproximado: '', riesgos: '', modalidad, direccion, referencias: '',
        ubicacionGps: false, latitud: null, longitud: null, distanciaKm,
        telefonoContacto: clienteId === 100 ? '+59899111222' : '+59899333444', canal: 'Teléfono', claveIdempotencia: '', nivelCliente: nivel,
      },
      ahora - minutos * 60 * 1000,
      'Sistema (demo)',
    );
  }
  revisarPlazos(ahora);
}

// Lo que en la API hace una tarea programada (RN-50, RN-55): rojos ofrecidos sin aceptar en 10 minutos pasan a
// Recepción y lo pendiente de 24 horas pide una reevaluación. Sin respuesta no se cierra ni baja el nivel.
export function revisarPlazos(ahora = Date.now()) {
  for (const s of solicitudes.filter(estaActiva)) {
    if (s.nivel === 'Rojo' && s.estado === 'Ofrecida' && s.fechaOferta !== null && ahora - s.fechaOferta >= MINUTOS_ESCALAR_ROJO * 60 * 1000) {
      escalar(s, ahora, `Nadie aceptó el rojo en ${MINUTOS_ESCALAR_ROJO} minutos: contactar al cliente.`);
    }
    if (sinAceptar(s) && !s.reevaluacionPedida && ahora - s.fechaCreacion >= HORAS_REEVALUAR * 60 * 60 * 1000) {
      s.reevaluacionPedida = true;
      evento(s, ahora, 'Reevaluación pedida', 'Pasaron 24 horas: ¿el estado se mantuvo o empeoró?', 'Sistema');
    }
  }
}

cargarDemo();

export const nuevaClave = () => `app-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;

export const datosVacios = (telefono: string): DatosSolicitud => ({
  modalidad: '', animalId: null, especie: '', cantidad: '1', sintomas: '', inicioSintomas: '', pesoAproximado: '', riesgos: '',
  nivel: '', direccion: '', referencias: '', latitud: null, longitud: null, telefonoContacto: telefono, aceptaCosto: false, claveIdempotencia: nuevaClave(),
});

const cantidadDe = (texto: string) => (/^\d+$/.test(texto.trim()) ? Number(texto.trim()) : 0);
const tieneGps = (d: Pick<DatosSolicitud, 'latitud' | 'longitud'>) => d.latitud !== null && d.longitud !== null;

export function validar(datos: DatosSolicitud, clienteId: number): Errores<DatosSolicitud> {
  const e: Errores<DatosSolicitud> = {};
  const animal = datos.animalId ? obtenerAnimal(datos.animalId, clienteId) : undefined;
  if (!datos.modalidad) e.modalidad = 'Elegí la modalidad.';
  if (datos.animalId === null || (datos.animalId !== 0 && !animal)) e.animalId = 'Elegí el animal o «Sin registrar o grupo».';
  if (datos.animalId === 0 && !especies.includes(datos.especie)) e.especie = 'Indicá la especie del animal o del grupo.';
  if (datos.animalId === 0 || animal?.esGrupo) {
    const n = cantidadDe(datos.cantidad);
    if (n < 1 || n > CANTIDAD_MAXIMA) e.cantidad = `La cantidad debe estar entre 1 y ${CANTIDAD_MAXIMA}.`;
    else if (animal?.cantidadGrupo && n > animal.cantidadGrupo) e.cantidad = `El grupo tiene ${animal.cantidadGrupo} animales.`;
  }
  (['sintomas', 'inicioSintomas', 'pesoAproximado', 'riesgos', 'direccion', 'referencias'] as const).forEach((campo) => {
    if (datos[campo].trim().length > LARGOS[campo]) e[campo] = `Puede tener hasta ${LARGOS[campo]} caracteres.`;
  });
  if (!datos.sintomas.trim()) e.sintomas = 'Describí los síntomas.';
  if (!datos.inicioSintomas.trim()) e.inicioSintomas = 'Contá desde cuándo pasa.';
  if (!datos.nivel) e.nivel = 'Elegí qué tan grave lo ves.';
  // RN-56: una visita necesita destino: la ubicación del celular o la dirección escrita.
  if (datos.modalidad === 'Visita' && !datos.direccion.trim() && !tieneGps(datos)) {
    e.direccion = 'Una visita necesita tu ubicación o la dirección. Sin ninguna podés pedir orientación y traslado.';
  }
  if (!normalizarTelefono(datos.telefonoContacto)) e.telefonoContacto = 'Escribí un teléfono de contacto válido.';
  if (pideAceptarCosto(datos.modalidad, datos.nivel) && !datos.aceptaCosto) e.aceptaCosto = 'Para confirmar la visita aceptá el criterio de costo.';
  return e;
}

// CU-CLI-19, pasos 7 a 10. Repetir el envío con la misma clave no crea otra solicitud (RNF-CON-10).
// La deuda previa no se consulta: no bloquea ningún pedido (RN-58).
export function crear(datos: DatosSolicitud, clienteId: number): Resultado & { id?: number } {
  const cuenta = obtenerCuenta(clienteId);
  if (!cuenta?.verificada) return { exito: false, mensaje: 'Ingresá con una cuenta verificada para pedir asistencia.' };

  const repetida = solicitudes.find((s) => s.clienteId === clienteId && datos.claveIdempotencia && s.claveIdempotencia === datos.claveIdempotencia);
  if (repetida) return { exito: true, mensaje: `La solicitud n.º ${repetida.id} ya estaba registrada.`, id: repetida.id };

  if (Object.keys(validar(datos, clienteId)).length > 0 || !datos.modalidad || !datos.nivel) return { exito: false, mensaje: 'Revisá los datos marcados.' };

  const animal = datos.animalId ? obtenerAnimal(datos.animalId, clienteId) : undefined;
  const visita = datos.modalidad === 'Visita';
  const gps = visita && tieneGps(datos);
  const s = agregar(
    {
      clienteId,
      animalId: animal?.id ?? 0,
      // Con un animal registrado, la especie sale de su ficha.
      especie: animal?.especie ?? datos.especie,
      cantidad: animal && !animal.esGrupo ? 1 : cantidadDe(datos.cantidad),
      sintomas: datos.sintomas.trim(),
      inicioSintomas: datos.inicioSintomas.trim(),
      pesoAproximado: datos.pesoAproximado.trim(),
      riesgos: datos.riesgos.trim(),
      modalidad: datos.modalidad,
      direccion: visita ? datos.direccion.trim() : '',
      referencias: visita ? datos.referencias.trim() : '',
      // Para orientación no se guarda ninguna ubicación.
      ubicacionGps: gps,
      latitud: gps ? datos.latitud : null,
      longitud: gps ? datos.longitud : null,
      distanciaKm: 0,
      telefonoContacto: normalizarTelefono(datos.telefonoContacto) as string,
      canal: 'App',
      claveIdempotencia: datos.claveIdempotencia,
      nivelCliente: datos.nivel,
    },
    Date.now(),
    `${cuenta.nombre} ${cuenta.apellido} (cliente)`,
  );
  return { exito: true, mensaje: `Solicitud n.º ${s.id} enviada.`, id: s.id };
}

// Solo las solicitudes del propio cliente (RN-18), las activas primero y de la más reciente a la más antigua.
export function deCliente(clienteId: number) {
  revisarPlazos();
  return solicitudes
    .filter((s) => s.clienteId === clienteId)
    .sort((a, b) => Number(estaActiva(b)) - Number(estaActiva(a)) || b.fechaCreacion - a.fechaCreacion);
}

export function obtenerDe(id: number, clienteId: number) {
  revisarPlazos();
  return solicitudes.find((s) => s.id === id && s.clienteId === clienteId);
}

export const reevaluacionesPendientes = (clienteId: number) => deCliente(clienteId).filter(pideReevaluacion);

// Lo que el cliente ve del historial: sin los veterinarios a los que se ofreció ni los motivos de rechazo.
const PARA_CLIENTE: Record<string, (e: EventoAsistencia) => string> = {
  Recibida: (e) => e.texto,
  Ofrecida: () => 'Se ofreció a un veterinario disponible.',
  'Escalada a Recepción': (e) => e.texto,
  Aceptada: (e) => e.texto,
  'En camino': () => 'El veterinario salió hacia tu dirección.',
  'En el lugar': () => 'El veterinario llegó.',
  'En atención': () => 'Comenzó la atención.',
  Finalizada: () => 'La asistencia terminó.',
  'Cancelada por la clínica': (e) => e.texto,
  'No concretada': (e) => e.texto,
  'Empeoramiento informado': (e) => e.texto,
  'Nivel elevado por empeoramiento': (e) => `De ${e.nivelAnterior} a ${e.nivelNuevo}.`,
  'Nivel cambiado por el veterinario': (e) => `De ${e.nivelAnterior} a ${e.nivelNuevo}. ${e.texto}`,
  'Nivel confirmado': (e) => e.texto,
  'Reevaluación pedida': (e) => e.texto,
  'Reevaluación respondida': (e) => e.texto,
};

export function historialDe(id: number, clienteId: number) {
  if (!obtenerDe(id, clienteId)) return [];
  return eventos
    .filter((e) => e.solicitudId === id && PARA_CLIENTE[e.tipo])
    .sort((a, b) => b.id - a.id)
    .map((e) => ({ id: e.id, fecha: e.fecha, tipo: e.tipo, texto: PARA_CLIENTE[e.tipo](e) }));
}

function aplicarNivel(s: SolicitudAsistencia, nuevo: NivelUrgencia, tipo: string, motivo: string, autor: string, ahora: number) {
  const anterior = s.nivel;
  s.nivel = nuevo;
  evento(s, ahora, tipo, motivo, autor, { nivelAnterior: anterior, nivelNuevo: nuevo });
  // Si sube a rojo y nadie la aceptó, la guardia se activa de inmediato; fuera de cobertura sigue en Recepción (D-ASI-07).
  if (nuevo === 'Rojo' && sinAceptar(s) && !fueraDeCobertura(s)) activarGuardia(s, ahora);
}

export const empeoramientoVacio = (): DatosEmpeoramiento => ({ nivel: '', motivo: '', claveIdempotencia: nuevaClave() });

export function validarEmpeoramiento(d: DatosEmpeoramiento): Errores<DatosEmpeoramiento> {
  const e: Errores<DatosEmpeoramiento> = {};
  if (!d.nivel) e.nivel = 'Elegí qué tan grave lo ves ahora.';
  if (!d.motivo.trim()) e.motivo = 'Contá qué cambió.';
  else if (d.motivo.trim().length > LARGOS.motivo) e.motivo = `Puede tener hasta ${LARGOS.motivo} caracteres.`;
  return e;
}

// RN-51, sección 10: el evento queda siempre y el nivel nuevo es el mayor entre el actual y el informado.
// El cliente no puede bajarlo: solo un veterinario, con motivo (RN-52). Un reintento con la misma clave no duplica.
export function informarEmpeoramiento(id: number, clienteId: number, datos: DatosEmpeoramiento): Resultado {
  const s = obtenerDe(id, clienteId);
  if (!s) return { exito: false, mensaje: 'No encontramos esa solicitud entre las tuyas.' };
  if (eventos.some((e) => e.solicitudId === id && e.claveIdempotencia && e.claveIdempotencia === datos.claveIdempotencia)) {
    return { exito: true, mensaje: 'Ese aviso ya estaba registrado.' };
  }
  if (!estaActiva(s)) return { exito: false, mensaje: 'Esta solicitud ya está cerrada. Si es grave, llamá a la guardia.' };
  if (Object.keys(validarEmpeoramiento(datos)).length > 0 || !datos.nivel) return { exito: false, mensaje: 'Elegí el nivel y contá qué cambió.' };

  const cuenta = obtenerCuenta(clienteId);
  const autor = `${cuenta?.nombre} ${cuenta?.apellido} (cliente)`;
  const ahora = Date.now();
  evento(s, ahora, 'Empeoramiento informado', `Nivel informado: ${datos.nivel}. ${datos.motivo.trim()}`, autor, { claveIdempotencia: datos.claveIdempotencia });
  // Responder la reevaluación de las 24 horas con un empeoramiento también la cierra (sección 11).
  if (s.reevaluacionPedida) s.reevaluacionRespondida = true;

  if (prioridad(datos.nivel) > prioridad(s.nivel)) {
    return { exito: true, mensaje: `Registrado. El nivel sigue en ${s.nivel}: solo un veterinario puede bajarlo.` };
  }
  if (datos.nivel !== s.nivel) {
    aplicarNivel(s, datos.nivel, 'Nivel elevado por empeoramiento', datos.motivo.trim(), autor, ahora);
    return { exito: true, mensaje: `Empeoramiento registrado: la solicitud pasó a ${s.nivel}.` };
  }
  return { exito: true, mensaje: 'Empeoramiento registrado; el nivel se mantiene.' };
}

// Sección 11: el cliente responde que el estado se mantuvo. No cambia el nivel ni el estado.
export function responderSeMantiene(id: number, clienteId: number): Resultado {
  const s = obtenerDe(id, clienteId);
  if (!s || !pideReevaluacion(s)) return { exito: false, mensaje: 'Esta solicitud no tiene una reevaluación pendiente.' };
  s.reevaluacionRespondida = true;
  const cuenta = obtenerCuenta(clienteId);
  evento(s, Date.now(), 'Reevaluación respondida', 'El cliente indica que el estado se mantuvo.', `${cuenta?.nombre} ${cuenta?.apellido} (cliente)`);
  return { exito: true, mensaje: 'Gracias. La solicitud sigue con el mismo nivel.' };
}

// RNF-LEG-09: al cerrar la asistencia se dejan de usar y se descartan las coordenadas del cliente.
function cerrar(s: SolicitudAsistencia) {
  s.latitud = null;
  s.longitud = null;
}

// SOLO DEMOSTRACIÓN: hace lo que en la versión final hacen Recepción y el veterinario desde sus pantallas
// (asignar, aceptar, salir, llegar, atender, finalizar), para poder ver el seguimiento en la app.
export function simularAvance(id: number, clienteId: number): Resultado {
  const s = obtenerDe(id, clienteId);
  if (!s || !estaActiva(s)) return { exito: false, mensaje: 'Esta solicitud ya está cerrada.' };
  const ahora = Date.now();

  if (sinAceptar(s)) {
    // Para la demostración se acepta aunque esté fuera de horario o pase por Recepción.
    const veterinario = elegibles({ ...s, nivel: 'Verde' }, ahora)[0];
    if (!veterinario) return { exito: false, mensaje: `Ningún veterinario de la demostración atiende ${s.especie}.` };
    if (s.modalidad === 'Visita' && s.distanciaKm === 0) {
      // D-ASI-11: la distancia la carga Recepción; con la API la calcula el mapa.
      s.distanciaKm = 8;
      s.costo = calcularCosto(s.modalidad, s.distanciaKm, s.cantidad);
    }
    s.estado = 'Aceptada';
    s.veterinarioId = veterinario.id;
    s.veterinarioNombre = veterinario.nombre;
    s.fechaAceptacion = ahora;
    s.ofrecidaA = [];
    // RN-53: estimación recién ahora (15 min + 2 min por km, D-ASI-10).
    s.etaMinutos = s.modalidad === 'Visita' ? 15 + s.distanciaKm * 2 : null;
    evento(s, ahora, 'Aceptada', s.etaMinutos === null ? `${veterinario.nombre} te va a contactar.` : `${veterinario.nombre} aceptó. Llegada estimada en ${s.etaMinutos} minutos.`, veterinario.nombre);
    return { exito: true, mensaje: 'Demostración: un veterinario aceptó la solicitud.' };
  }

  const siguiente: Partial<Record<EstadoAsistencia, EstadoAsistencia>> =
    s.modalidad === 'Visita'
      ? { Aceptada: 'En camino', 'En camino': 'En el lugar', 'En el lugar': 'En atención', 'En atención': 'Finalizada' }
      : { Aceptada: 'En atención', 'En atención': 'Finalizada' };
  const nuevo = siguiente[s.estado];
  if (!nuevo) return { exito: false, mensaje: 'No hay un paso siguiente.' };
  s.estado = nuevo;
  evento(s, ahora, nuevo, nuevo === 'Finalizada' ? 'Atención registrada por el veterinario.' : '', s.veterinarioNombre);
  if (nuevo === 'Finalizada') cerrar(s);
  return { exito: true, mensaje: `Demostración: ${nuevo}.` };
}

// Momento en que el veterinario salió, para el mapa de demostración.
export const salidaDe = (id: number) => eventos.find((e) => e.solicitudId === id && e.tipo === 'En camino')?.fecha ?? null;

// Qué significa cada estado para el cliente: nunca se promete profesional ni horario antes de aceptar (RN-53).
export function explicacionEstado(s: SolicitudAsistencia): string {
  switch (s.estado) {
    case 'Pendiente de asignación':
      return paraProximoDia(s)
        ? 'La clínica está cerrada: queda pendiente para el próximo día de atención.'
        : 'Recepción la va a revisar y ofrecer a un veterinario. Todavía no hay un profesional asignado.';
    case 'Ofrecida':
      return 'Se ofreció a un veterinario y estamos esperando que acepte. Todavía no está confirmada.';
    case 'Requiere coordinación de Recepción':
      return 'Recepción tiene que contactarte para coordinar. La visita no está confirmada.';
    case 'Aceptada':
      return s.modalidad === 'Visita' ? `${s.veterinarioNombre} aceptó y se está preparando para salir.` : `${s.veterinarioNombre} aceptó y te va a contactar.`;
    case 'En camino':
      return `${s.veterinarioNombre} está en camino. Mientras dure el traslado podés ver su ubicación.`;
    case 'En el lugar':
      return `${s.veterinarioNombre} llegó.`;
    case 'En atención':
      return `${s.veterinarioNombre} está atendiendo.`;
    case 'Finalizada':
      return s.modalidad === 'Visita' ? 'La asistencia terminó. El cargo queda pendiente en Recepción.' : 'La orientación terminó.';
    default:
      return s.motivoCierre || 'La solicitud se cerró sin atención. Consultá con Recepción.';
  }
}
