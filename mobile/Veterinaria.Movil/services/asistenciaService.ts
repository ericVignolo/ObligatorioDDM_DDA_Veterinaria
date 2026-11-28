import { cuentasDemo } from '../data/cuentasDemo';
import { profesionales } from '../data/profesionales';
import type { Errores, Resultado } from '../types/Cuenta';
import type { DatosSolicitud, EventoAsistencia, Modalidad, NivelUrgencia, SolicitudAsistencia } from '../types/Asistencia';
import { normalizarTelefono } from '../utils/validacion';
import { especies, obtenerPorId as obtenerAnimal } from './animalService';
import { obtenerPorId as obtenerCuenta } from './cuentaService';

// Solicitudes de asistencia del Cliente en memoria (B-013, ESPECIFICACION_ALERTAS_ASISTENCIA, RN-46 a RN-59),
// con las mismas reglas de creación que AsistenciaService de Blazor. El seguimiento, los plazos (10 minutos y 24 horas)
// y el empeoramiento llegan en B-014.
// En la sección C: POST y GET /api/v1/asistencias; la API valida, clasifica, calcula la distancia y activa la guardia.

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
export const TARIFA_BASE = 1500;
export const TARIFA_POR_KM = 40;
export const TARIFA_POR_ANIMAL_EXTRA = 300;
export const CANTIDAD_MAXIMA = 1000;
export const LARGOS = { sintomas: 1000, inicioSintomas: 100, pesoAproximado: 40, riesgos: 300, direccion: 200, referencias: 300, telefono: 30 };

// Veterinario de guardia de demostración (id de personal): Dra. Ferreira.
const GUARDIA_ID = 2;

const solicitudes: SolicitudAsistencia[] = [];
const eventos: EventoAsistencia[] = [];
let ultimaSolicitud = 0;
let ultimoEvento = 0;

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

const sinAceptar = (s: SolicitudAsistencia) => s.estado === 'Pendiente de asignación' || s.estado === 'Ofrecida' || s.estado === 'Requiere coordinación de Recepción';

// RN-48: fuera del horario, lo no rojo y sin aceptar queda para el próximo día de atención.
export const paraProximoDia = (s: SolicitudAsistencia, ahora = Date.now()) => s.nivel !== 'Rojo' && sinAceptar(s) && !enHorarioClinica(ahora);

export const estaActiva = (s: SolicitudAsistencia) => !['Finalizada', 'Cancelada por la clínica', 'No concretada'].includes(s.estado);

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

function evento(s: SolicitudAsistencia, fecha: number, tipo: string, texto: string, autor: string) {
  ultimoEvento++;
  eventos.push({ id: ultimoEvento, solicitudId: s.id, fecha, tipo, texto, autor });
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

type Nueva = Omit<SolicitudAsistencia, 'id' | 'estado' | 'nivel' | 'fechaCreacion' | 'ofrecidaA' | 'fechaOferta' | 'costo' | 'motivoCoordinacion'>;

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
    costo: calcularCosto(datos.modalidad, datos.distanciaKm, datos.cantidad),
    motivoCoordinacion: '',
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
        clienteId, animalId, especie, cantidad, sintomas, inicioSintomas: '', pesoAproximado: '', riesgos: '', modalidad, direccion, referencias: '', distanciaKm,
        telefonoContacto: clienteId === 100 ? '+59899111222' : '+59899333444', canal: 'Teléfono', claveIdempotencia: '', nivelCliente: nivel,
      },
      ahora - minutos * 60 * 1000,
      'Sistema (demo)',
    );
  }
}

cargarDemo();

export const nuevaClave = () => `app-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;

export const datosVacios = (telefono: string): DatosSolicitud => ({
  modalidad: '', animalId: null, especie: '', cantidad: '1', sintomas: '', inicioSintomas: '', pesoAproximado: '', riesgos: '',
  nivel: '', direccion: '', referencias: '', telefonoContacto: telefono, aceptaCosto: false, claveIdempotencia: nuevaClave(),
});

const cantidadDe = (texto: string) => (/^\d+$/.test(texto.trim()) ? Number(texto.trim()) : 0);

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
  const largo = (campo: 'sintomas' | 'inicioSintomas' | 'pesoAproximado' | 'riesgos' | 'direccion' | 'referencias') => {
    if (datos[campo].trim().length > LARGOS[campo]) e[campo] = `Puede tener hasta ${LARGOS[campo]} caracteres.`;
  };
  (['sintomas', 'inicioSintomas', 'pesoAproximado', 'riesgos', 'direccion', 'referencias'] as const).forEach(largo);
  if (!datos.sintomas.trim()) e.sintomas = 'Describí los síntomas.';
  if (!datos.inicioSintomas.trim()) e.inicioSintomas = 'Contá desde cuándo pasa.';
  if (!datos.nivel) e.nivel = 'Elegí qué tan grave lo ves.';
  // RN-56: una visita necesita destino.
  if (datos.modalidad === 'Visita' && !datos.direccion.trim()) e.direccion = 'Una visita necesita la dirección. Sin ubicación podés pedir orientación y traslado.';
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
export const deCliente = (clienteId: number) =>
  solicitudes
    .filter((s) => s.clienteId === clienteId)
    .sort((a, b) => Number(estaActiva(b)) - Number(estaActiva(a)) || b.fechaCreacion - a.fechaCreacion);

export const obtenerDe = (id: number, clienteId: number) => solicitudes.find((s) => s.id === id && s.clienteId === clienteId);

// Qué significa cada estado para el cliente: nunca se promete profesional ni horario (RN-53).
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
    default:
      return 'Consultá el detalle con Recepción.';
  }
}
