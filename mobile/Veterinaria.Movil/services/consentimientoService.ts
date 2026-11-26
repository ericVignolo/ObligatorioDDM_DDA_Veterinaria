import { documentosConsentimiento } from '../data/consentimientoDemo';
import type { Consentimiento, DocumentoConsentimiento, EstadoConsentimiento } from '../types/Consentimiento';
import type { Resultado } from '../types/Cuenta';
import type { Turno } from '../types/Turno';
import { sha256 } from '../utils/sha256';
import { obtenerDe as obtenerTurno, proximosDe } from './agendaService';
import { obtenerPorId as obtenerCuenta } from './cuentaService';
import { obtenerPorId as obtenerPrestacion } from './prestacionService';

// Consentimiento informado del Cliente (B-012, RF-CLI-11, RN-10, RN-18, RN-43, P-RC-06), con las reglas K-01 a K-14
// de ESPECIFICACION_CONSENTIMIENTO.md, como ConsentimientoService de Blazor. No es una firma electrónica avanzada.
// En la sección C: GET y POST /api/v1/turnos/{id}/consentimiento; la API verifica identidad, versión y guarda la huella.

export const LARGO_MAXIMO_NOMBRE = 120;

const respuestas: Consentimiento[] = [];
let ultimoId = 0;

export const requiere = (turno: Turno) => obtenerPrestacion(turno.prestacionId)?.requiereConsentimiento === true;

export const documentoVigente = (prestacionId: number): DocumentoConsentimiento | undefined =>
  documentosConsentimiento.find((d) => d.prestacionId === prestacionId && d.vigente);

// Se puede responder mientras el turno no empezó (K-02, incluye «Pendiente de seña» como Blazor).
export const abierto = (t: Turno) => t.estado === 'Confirmado' || t.estado === 'En espera' || t.estado === 'Pendiente de seña';

// Historial de un turno del cliente, de la más reciente a la más antigua.
export const respuestasDe = (turnoId: number, clienteId: number) =>
  respuestas.filter((r) => r.turnoId === turnoId && r.clienteId === clienteId).sort((a, b) => b.id - a.id);

// Todas las respuestas del cliente (CU-CLI-17: consentimientos clínicos históricos).
export const respuestasDeCliente = (clienteId: number) => respuestas.filter((r) => r.clienteId === clienteId).sort((a, b) => b.id - a.id);

// RN-10: vale si la última respuesta es una aceptación de la versión vigente.
export function estadoDe(turno: Turno): EstadoConsentimiento {
  if (!requiere(turno)) return 'No requiere';
  const ultima = respuestasDe(turno.id, turno.clienteId)[0];
  if (!ultima) return 'Pendiente';
  if (ultima.respuesta === 'Rechazado') return 'Rechazado';
  return ultima.documentoId === documentoVigente(turno.prestacionId)?.id ? 'Firmado' : 'Versión nueva';
}

// K-08: turnos próximos del cliente que necesitan su consentimiento.
export const pendientesDe = (clienteId: number) => proximosDe(clienteId).filter((t) => abierto(t) && requiere(t) && estadoDe(t) !== 'Firmado');

// Sin mayúsculas ni espacios de más; las tildes cuentan (K-12).
const normalizar = (texto: string) => texto.split(' ').filter(Boolean).join(' ').toLowerCase();

type Preparado = { turno: Turno; documento: DocumentoConsentimiento; nombreCuenta: string } | { error: string };

function preparar(turnoId: number, clienteId: number): Preparado {
  const turno = obtenerTurno(turnoId, clienteId);
  const cuenta = obtenerCuenta(clienteId);
  // RN-18: si no es su turno no se distingue de que no exista.
  if (!turno || !cuenta) return { error: 'No encontramos ese turno entre los tuyos.' };
  if (!requiere(turno)) return { error: 'Este turno no necesita consentimiento.' };
  if (!abierto(turno)) return { error: 'Este turno ya empezó o terminó: el consentimiento no se puede cambiar.' };
  const documento = documentoVigente(turno.prestacionId);
  if (!documento) return { error: 'El documento no está disponible. Consultá en la clínica.' };
  return { turno, documento, nombreCuenta: `${cuenta.nombre} ${cuenta.apellido}` };
}

function registrar(turno: Turno, documento: DocumentoConsentimiento, respuesta: Consentimiento['respuesta'], nombre: string) {
  const fecha = Date.now();
  const partes = [turno.id, turno.clienteId, turno.animalId, turno.prestacionId, documento.version, documento.texto, respuesta, nombre, new Date(fecha).toISOString()];
  ultimoId++;
  respuestas.push({
    id: ultimoId,
    turnoId: turno.id,
    clienteId: turno.clienteId,
    animalId: turno.animalId,
    prestacionId: turno.prestacionId,
    documentoId: documento.id,
    version: documento.version,
    respuesta,
    nombreEscrito: nombre,
    canal: 'En línea',
    fecha,
    hash: sha256(partes.join('|')),
  });
}

// CU-CLI-10, pasos 4 a 6: casilla marcada y nombre completo igual al de la cuenta (K-03).
// documentoLeidoId: la versión que la pantalla mostró; si cambió mientras tanto, no se firma.
export function firmar(turnoId: number, clienteId: number, documentoLeidoId: number, acepta: boolean, nombre: string): Resultado {
  const p = preparar(turnoId, clienteId);
  if ('error' in p) return { exito: false, mensaje: p.error };
  if (p.documento.id !== documentoLeidoId) return { exito: false, mensaje: 'La clínica actualizó el documento: leé la versión nueva antes de firmar.' };
  if (!acepta || !nombre.trim() || nombre.length > LARGO_MAXIMO_NOMBRE) return { exito: false, mensaje: 'Marcá que leíste y aceptás, y escribí tu nombre completo.' };
  if (normalizar(nombre) !== normalizar(p.nombreCuenta)) return { exito: false, mensaje: 'El nombre no coincide con el de tu cuenta. Escribilo como figura en «Mi cuenta».' };
  if (estadoDe(p.turno) === 'Firmado') return { exito: false, mensaje: 'Ya firmaste este consentimiento.' };

  registrar(p.turno, p.documento, 'Aceptado', nombre.trim());
  return { exito: true, mensaje: 'Consentimiento firmado. Queda guardado junto al turno.' };
}

// Rechazar, o retirar uno firmado antes del procedimiento (K-05, K-10).
export function rechazar(turnoId: number, clienteId: number): Resultado {
  const p = preparar(turnoId, clienteId);
  if ('error' in p) return { exito: false, mensaje: p.error };
  if (respuestasDe(turnoId, clienteId)[0]?.respuesta === 'Rechazado') return { exito: false, mensaje: 'Ya rechazaste este consentimiento.' };

  registrar(p.turno, p.documento, 'Rechazado', '');
  return { exito: true, mensaje: 'No autorizaste el procedimiento: la cirugía no se hará. Podés cancelar el turno o llamar a la clínica.' };
}
