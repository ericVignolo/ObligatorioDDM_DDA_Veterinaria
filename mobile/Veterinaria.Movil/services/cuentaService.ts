import { CONTRASENA_DEMO, cuentasDemo } from '../data/cuentasDemo';
import type { Cuenta, DatosPerfil, DatosRegistro, Resultado } from '../types/Cuenta';
import type { Usuario } from '../types/Usuario';
import { documentoValido, normalizarCorreo, normalizarDocumento, normalizarTelefono } from '../utils/validacion';
import { obtenerPorCodigo } from './legalService';

// Cuentas de demostración en memoria (sección B): se pierden al cerrar la app. Misma lógica que CuentaService de Blazor.
// En la sección C se reemplaza por llamadas a la API (POST /api/v1/cuentas, /verificacion, /sesiones, /recuperacion...),
// que valida, guarda, manda el correo y entrega los tokens. Esta app nunca verifica nada de verdad (CONTEXTO_BASE 3.1).

// Parámetros de D-REG-02.
const MINUTOS_VIGENCIA_CODIGO = 15;
const INTENTOS_POR_CODIGO = 5;
const REENVIOS_POR_HORA = 3;
const UNA_HORA = 60 * 60 * 1000;

export const departamentos = [
  'Artigas', 'Canelones', 'Cerro Largo', 'Colonia', 'Durazno', 'Flores', 'Florida', 'Lavalleja', 'Maldonado', 'Montevideo',
  'Paysandú', 'Río Negro', 'Rivera', 'Rocha', 'Salto', 'San José', 'Soriano', 'Tacuarembó', 'Treinta y Tres',
];

const vacia = (): Omit<Cuenta, 'id' | 'nombre' | 'apellido' | 'correo' | 'telefono'> => ({
  tipoDocumento: '',
  numeroDocumento: '',
  calle: '',
  numeroPuerta: '',
  apartamento: '',
  localidad: '',
  departamento: '',
  referencias: '',
  contrasena: CONTRASENA_DEMO,
  verificada: true,
  fechaCreacion: new Date(2026, 9, 1).getTime(),
  versionTerminosAceptada: '',
  versionPrivacidadAceptada: '',
  fechaAceptacion: 0,
  codigo: '',
  codigoVence: 0,
  intentosRestantes: 0,
  reenvios: [],
  codigoRecuperacion: '',
  codigoRecuperacionVence: 0,
  intentosRecuperacion: 0,
  pedidosRecuperacion: [],
});

// Clientes de demostración: los mismos que CuentaService de Blazor (entran con la contraseña de demostración).
const cuentas: Cuenta[] = [
  { id: 100, nombre: 'Ana', apellido: 'Rodríguez', correo: 'ana.demo@vitalia.uy', telefono: '+59899111222', ...vacia() },
  { id: 101, nombre: 'Pedro', apellido: 'Gómez', correo: 'pedro.demo@vitalia.uy', telefono: '+59899333444', ...vacia() },
];
let ultimoId = 0;

// Correos para las pantallas de verificación y recuperación (en Blazor, CorreoPendiente y CorreoRecuperacion).
let correoPendiente = '';
let correoRecuperacion = '';

export const obtenerCorreoPendiente = () => correoPendiente;
export const obtenerCorreoRecuperacion = () => correoRecuperacion;

const nombreCompleto = (c: Cuenta) => `${c.nombre} ${c.apellido}`.trim();

export function usuarioDe(cuenta: Cuenta): Usuario {
  return { id: cuenta.id, nombre: nombreCompleto(cuenta), correo: cuenta.correo, rol: 'Cliente' };
}

export function obtenerPorId(id: number): Cuenta | undefined {
  return cuentas.find((c) => c.id === id);
}

export function obtenerPorCorreo(correo: string): Cuenta | undefined {
  const buscado = normalizarCorreo(correo);
  return cuentas.find((c) => c.correo === buscado);
}

const generarCodigo = () => String(Math.floor(Math.random() * 1_000_000)).padStart(6, '0');

function asignarCodigo(cuenta: Cuenta) {
  cuenta.codigo = generarCodigo();
  cuenta.codigoVence = Date.now() + MINUTOS_VIGENCIA_CODIGO * 60 * 1000;
  cuenta.intentosRestantes = INTENTOS_POR_CODIGO;
}

// SOLO DEMOSTRACIÓN: todavía no se envían correos, así que la pantalla muestra el código.
export function codigoDemo(correo: string): string {
  return obtenerPorCorreo(correo)?.codigo ?? '';
}

export function codigoRecuperacionDemo(correo: string): string {
  return obtenerPorCorreo(correo)?.codigoRecuperacion ?? '';
}

// Crea la cuenta sin verificar y genera el primer código (CU-VIS-03).
export function registrar(datos: DatosRegistro): Resultado {
  const correo = normalizarCorreo(datos.correo);
  const documento = normalizarDocumento(datos.tipoDocumento, datos.numeroDocumento);
  const telefono = normalizarTelefono(datos.telefono);

  // REG-02: no se crea un duplicado ni se dice de quién es el correo.
  if (cuentas.some((c) => c.correo === correo)) {
    return { exito: false, mensaje: 'No pudimos crear la cuenta con ese correo. Si ya tenés una, ingresá o recuperá tu contraseña.' };
  }
  if (cuentas.some((c) => c.tipoDocumento === datos.tipoDocumento && c.numeroDocumento === documento)) {
    return { exito: false, mensaje: 'No pudimos crear la cuenta con ese documento. Si ya tenés una, consultá en Recepción.' };
  }
  // REG-03: las aceptaciones obligatorias también se controlan acá, no solo en el formulario.
  if (!datos.aceptaTerminos || !datos.aceptaPrivacidad || !telefono || !documentoValido(datos.tipoDocumento, datos.numeroDocumento)) {
    return { exito: false, mensaje: 'Revisá los datos y las aceptaciones obligatorias.' };
  }

  ultimoId++;
  const ahora = Date.now();
  const cuenta: Cuenta = {
    ...vacia(),
    id: ultimoId,
    nombre: datos.nombre.trim(),
    apellido: datos.apellido.trim(),
    correo,
    telefono,
    tipoDocumento: datos.tipoDocumento,
    numeroDocumento: documento,
    calle: datos.calle.trim(),
    numeroPuerta: datos.numeroPuerta.trim(),
    apartamento: datos.apartamento.trim(),
    localidad: datos.localidad.trim(),
    departamento: datos.departamento,
    referencias: datos.referencias.trim(),
    contrasena: datos.contrasena,
    verificada: false,
    fechaCreacion: ahora,
    versionTerminosAceptada: obtenerPorCodigo('terminos')?.version ?? '',
    versionPrivacidadAceptada: obtenerPorCodigo('privacidad')?.version ?? '',
    fechaAceptacion: ahora,
  };
  asignarCodigo(cuenta);
  cuentas.push(cuenta);
  correoPendiente = correo;
  return { exito: true, mensaje: 'Cuenta creada. Te enviamos un código para verificar tu correo.' };
}

export function verificar(correo: string, codigo: string): Resultado {
  const cuenta = obtenerPorCorreo(correo);
  if (!cuenta) return { exito: false, mensaje: 'No encontramos una cuenta pendiente de verificar con ese correo.' };
  if (cuenta.verificada) return { exito: true, mensaje: 'Tu cuenta ya estaba verificada.' };
  if (cuenta.intentosRestantes <= 0) return { exito: false, mensaje: 'El código se invalidó por demasiados intentos. Pedí uno nuevo.' };
  if (Date.now() > cuenta.codigoVence) return { exito: false, mensaje: 'El código venció. Pedí uno nuevo.' };

  if ((codigo ?? '').trim() !== cuenta.codigo) {
    cuenta.intentosRestantes--;
    return {
      exito: false,
      mensaje:
        cuenta.intentosRestantes > 0
          ? `El código no es correcto. Te quedan ${cuenta.intentosRestantes} intentos.`
          : 'El código no es correcto y se invalidó. Pedí uno nuevo.',
    };
  }

  cuenta.verificada = true;
  cuenta.codigo = '';
  correoPendiente = '';
  return { exito: true, mensaje: '¡Listo! Tu correo quedó verificado.' };
}

export function reenviarCodigo(correo: string): Resultado {
  const cuenta = obtenerPorCorreo(correo);
  if (!cuenta || cuenta.verificada) return { exito: false, mensaje: 'No hay un código pendiente para ese correo.' };

  const desde = Date.now() - UNA_HORA;
  cuenta.reenvios = cuenta.reenvios.filter((f) => f >= desde);
  if (cuenta.reenvios.length >= REENVIOS_POR_HORA) {
    return { exito: false, mensaje: 'Ya pediste 3 códigos en la última hora. Esperá un rato y volvé a intentar.' };
  }
  cuenta.reenvios.push(Date.now());
  asignarCodigo(cuenta);
  return { exito: true, mensaje: 'Te enviamos un código nuevo. El anterior ya no sirve.' };
}

// Para retomar la verificación de una cuenta que no la completó.
export function marcarPendiente(correo: string) {
  correoPendiente = normalizarCorreo(correo);
}

// D-REG-03: la respuesta es siempre la misma, exista o no la cuenta (REG-08).
export function solicitarRecuperacion(correo: string): Resultado {
  correoRecuperacion = normalizarCorreo(correo);
  const respuesta: Resultado = { exito: true, mensaje: 'Si el correo está registrado, te enviamos un código para crear una contraseña nueva.' };

  const cuenta = obtenerPorCorreo(correo);
  if (!cuenta) return respuesta;

  const desde = Date.now() - UNA_HORA;
  cuenta.pedidosRecuperacion = cuenta.pedidosRecuperacion.filter((f) => f >= desde);
  // Mismo mensaje al llegar al límite: no se revela que la cuenta existe.
  if (cuenta.pedidosRecuperacion.length >= REENVIOS_POR_HORA) return respuesta;

  cuenta.pedidosRecuperacion.push(Date.now());
  cuenta.codigoRecuperacion = generarCodigo();
  cuenta.codigoRecuperacionVence = Date.now() + MINUTOS_VIGENCIA_CODIGO * 60 * 1000;
  cuenta.intentosRecuperacion = INTENTOS_POR_CODIGO;
  return respuesta;
}

export function restablecer(correo: string, codigo: string, nuevaContrasena: string): Resultado {
  const error: Resultado = { exito: false, mensaje: 'El código no es válido o venció. Pedí uno nuevo.' };
  const cuenta = obtenerPorCorreo(correo);
  if (!cuenta || !cuenta.codigoRecuperacion) return error;
  if (cuenta.intentosRecuperacion <= 0 || Date.now() > cuenta.codigoRecuperacionVence) return error;

  if ((codigo ?? '').trim() !== cuenta.codigoRecuperacion) {
    cuenta.intentosRecuperacion--;
    return {
      exito: false,
      mensaje:
        cuenta.intentosRecuperacion > 0
          ? `El código no es correcto. Te quedan ${cuenta.intentosRecuperacion} intentos.`
          : 'El código no es correcto y se invalidó. Pedí uno nuevo.',
    };
  }

  cuenta.contrasena = nuevaContrasena;
  cuenta.codigoRecuperacion = '';
  correoRecuperacion = '';
  return { exito: true, mensaje: 'Listo, tu contraseña cambió. Ya podés ingresar con la nueva.' };
}

// Resultado del ingreso. No distingue entre correo inexistente y contraseña incorrecta.
export type ResultadoIngreso =
  | { tipo: 'ok'; usuario: Usuario }
  | { tipo: 'sin-verificar'; correo: string }
  | { tipo: 'invalida' };

export function validarCredenciales(correo: string, contrasena: string): ResultadoIngreso {
  const normal = normalizarCorreo(correo);

  // Clientes (incluye los que se registraron en esta sesión).
  const cuenta = obtenerPorCorreo(normal);
  if (cuenta) {
    if (cuenta.contrasena !== contrasena) return { tipo: 'invalida' };
    if (!cuenta.verificada) return { tipo: 'sin-verificar', correo: cuenta.correo };
    return { tipo: 'ok', usuario: usuarioDe(cuenta) };
  }

  // Personal (Veterinario, Recepción, Administrador): cuentas de demostración con la contraseña de demostración.
  const personal = cuentasDemo.find((u) => u.rol !== 'Cliente' && u.correo === normal);
  if (personal && contrasena === CONTRASENA_DEMO) return { tipo: 'ok', usuario: personal };
  return { tipo: 'invalida' };
}

// A-009: guarda los datos editables. Se vuelve a validar aunque el formulario ya lo haya hecho.
export function actualizarPerfil(cuentaId: number, datos: DatosPerfil): Resultado {
  const cuenta = obtenerPorId(cuentaId);
  if (!cuenta || !cuenta.verificada) return { exito: false, mensaje: 'No encontramos tu cuenta. Volvé a ingresar.' };

  const telefono = normalizarTelefono(datos.telefono);
  if (
    !telefono ||
    !datos.nombre.trim() || datos.nombre.trim().length > 60 ||
    !datos.apellido.trim() || datos.apellido.trim().length > 60 ||
    !datos.calle.trim() || datos.calle.trim().length > 100 ||
    !datos.numeroPuerta.trim() || datos.numeroPuerta.trim().length > 10 ||
    datos.apartamento.trim().length > 10 ||
    !datos.localidad.trim() || datos.localidad.trim().length > 60 ||
    !departamentos.includes(datos.departamento) ||
    datos.referencias.trim().length > 200
  ) {
    return { exito: false, mensaje: 'Revisá los datos marcados antes de guardar.' };
  }

  cuenta.nombre = datos.nombre.trim();
  cuenta.apellido = datos.apellido.trim();
  cuenta.telefono = telefono;
  cuenta.calle = datos.calle.trim();
  cuenta.numeroPuerta = datos.numeroPuerta.trim();
  cuenta.apartamento = datos.apartamento.trim();
  cuenta.localidad = datos.localidad.trim();
  cuenta.departamento = datos.departamento;
  cuenta.referencias = datos.referencias.trim();
  return { exito: true, mensaje: 'Guardamos los cambios de tu cuenta.' };
}

// D-PER-05: cambia la contraseña con la sesión iniciada. Exige la actual; después se vuelve a ingresar.
export function cambiarContrasena(cuentaId: number, actual: string, nueva: string, repetir: string): Resultado {
  const cuenta = obtenerPorId(cuentaId);
  if (!cuenta || !cuenta.verificada) return { exito: false, mensaje: 'No encontramos tu cuenta. Volvé a ingresar.' };
  if (cuenta.contrasena !== actual) return { exito: false, mensaje: 'La contraseña actual no es correcta.' };
  if (nueva.length < 8 || nueva.length > 64 || nueva !== repetir) return { exito: false, mensaje: 'Revisá la contraseña nueva.' };
  if (nueva === actual) return { exito: false, mensaje: 'La contraseña nueva tiene que ser distinta de la actual.' };

  cuenta.contrasena = nueva;
  cuenta.codigoRecuperacion = '';
  return { exito: true, mensaje: 'Tu contraseña cambió. Por seguridad cerramos la sesión: ingresá con la contraseña nueva.' };
}
