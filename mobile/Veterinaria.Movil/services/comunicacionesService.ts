import type { Canal, PreferenciaComunicacion, TipoComunicacion } from '../types/Comunicacion';
import type { Resultado } from '../types/Cuenta';
import { obtenerPorId as obtenerCuenta } from './cuentaService';

// Preferencias de comunicación del Cliente (B-012, RF-CLI-18, RF-CLI-20, RN-43), mismo catálogo y reglas (CM-13)
// que ComunicacionesService de Blazor. La app no envía mensajes.
// En la sección C: GET y PUT /api/v1/mis-preferencias; la API registra finalidad, respuesta, fecha y canal.

export const tipos: TipoComunicacion[] = [
  { nombre: 'Resumen clínico', descripcion: 'Resumen de la consulta cuando el veterinario la firma.', esencial: true, canalPorDefecto: 'Correo' },
  { nombre: 'Comprobante de cobro', descripcion: 'Comprobante de un cobro, cuando Recepción lo envía.', esencial: true, canalPorDefecto: 'Correo' },
  { nombre: 'Aviso de la clínica', descripcion: 'Cambios que afectan tu atención: cierres, horarios, feriados.', esencial: true, canalPorDefecto: 'Correo' },
  { nombre: 'Aviso de refuerzo', descripcion: 'Vacunas y desparasitaciones próximas a vencer de tus animales.', esencial: false, canalPorDefecto: 'Correo' },
  { nombre: 'Alerta sanitaria', descripcion: 'Alertas de la clínica para animales de tu misma especie, como un brote en la zona.', esencial: false, canalPorDefecto: 'No recibir' },
];

export const canalesDe = (tipo: TipoComunicacion): Canal[] => (tipo.esencial ? ['Correo', 'Solo en la app'] : ['Correo', 'Solo en la app', 'No recibir']);

const preferencias: PreferenciaComunicacion[] = [];

// Lo que eligió el cliente; undefined si rige el canal por defecto.
export const eleccionDe = (cuentaId: number, tipo: string) => preferencias.find((p) => p.cuentaId === cuentaId && p.tipo === tipo);

export const canalDe = (cuentaId: number, tipo: TipoComunicacion): Canal => eleccionDe(cuentaId, tipo.nombre)?.canal ?? tipo.canalPorDefecto;

export function cambiarPreferencia(cuentaId: number, nombreTipo: string, canal: Canal): Resultado {
  const tipo = tipos.find((t) => t.nombre === nombreTipo);
  if (!obtenerCuenta(cuentaId) || !tipo) return { exito: false, mensaje: 'No pudimos guardar esa preferencia.' };
  if (!canalesDe(tipo).includes(canal))
    return { exito: false, mensaje: `«${nombreTipo}» es parte del servicio: podés elegir correo o solo la app, pero no dejar de recibirlo.` };

  const existente = eleccionDe(cuentaId, nombreTipo);
  if (existente) {
    existente.canal = canal;
    existente.fecha = Date.now();
  } else {
    preferencias.push({ cuentaId, tipo: nombreTipo, canal, fecha: Date.now() });
  }
  return { exito: true, mensaje: `Listo: «${nombreTipo}» → ${canal}. Vale para los próximos mensajes.` };
}
