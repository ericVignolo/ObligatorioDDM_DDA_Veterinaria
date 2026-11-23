// Formatos de D-REG-01 (mismos que ValidacionDatos de Blazor): documento por tipo y teléfono en E.164 (+598 por defecto).
// En la sección C las reglas las aplica la API; esto solo evita mandar datos que ya se sabe que son inválidos.

// Cédula uruguaya: 7 u 8 dígitos; el último es el dígito verificador.
export function cedulaValida(cedula: string): boolean {
  const digitos = (cedula ?? '').replace(/[.\-\s]/g, '');
  if (!/^\d{7,8}$/.test(digitos)) return false;

  const cuerpo = digitos.slice(0, -1).padStart(7, '0');
  const verificador = Number(digitos[digitos.length - 1]);
  const pesos = [2, 9, 8, 7, 6, 3, 4];
  const suma = pesos.reduce((acc, peso, i) => acc + Number(cuerpo[i]) * peso, 0);
  return (10 - (suma % 10)) % 10 === verificador;
}

export function documentoValido(tipo: string, numero: string): boolean {
  if (tipo === 'CI') return cedulaValida(numero);
  // Pasaporte u otro documento extranjero: alfanumérico de 5 a 20 caracteres.
  return /^[A-Za-z0-9]{5,20}$/.test(numero ?? '');
}

// Devuelve la cédula solo con dígitos, o el documento en mayúsculas.
export function normalizarDocumento(tipo: string, numero: string): string {
  return tipo === 'CI' ? (numero ?? '').replace(/\D/g, '') : (numero ?? '').trim().toUpperCase();
}

// Devuelve el teléfono en E.164 (por ejemplo +59899123456) o null si no es válido.
export function normalizarTelefono(telefono: string): string | null {
  let limpio = (telefono ?? '').replace(/[\s\-().]/g, '');
  if (limpio.startsWith('00')) {
    limpio = '+' + limpio.slice(2);
  } else if (!limpio.startsWith('+')) {
    // Sin prefijo internacional se asume Uruguay; se quita el 0 inicial (099 → +59899).
    limpio = '+598' + limpio.replace(/^0+/, '');
  }
  return /^\+[1-9]\d{7,14}$/.test(limpio) ? limpio : null;
}

export function normalizarCorreo(correo: string): string {
  return (correo ?? '').trim().toLowerCase();
}

// Formato razonable de correo (la API hace la validación de verdad).
export function correoValido(correo: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test((correo ?? '').trim()) && correo.trim().length <= 254;
}
