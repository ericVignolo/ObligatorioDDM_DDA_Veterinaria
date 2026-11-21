import type { Clinica } from '../types/Clinica';

// Clínica ficticia: los mismos datos que ClinicaService de Blazor. En la sección C: GET /api/v1/clinica.
export const clinica: Clinica = {
  nombre: 'VITALIA',
  razonSocial: 'VITALIA Servicios Veterinarios S.A.S.',
  lema: 'Clínica veterinaria para animales de compañía y de campo',
  presentacion:
    'Atendemos perros, gatos, caballos, ovinos y bovinos. Trabajamos en la clínica y, cuando hace falta, vamos a tu casa o a tu campo.',
  direccion: 'Calle 18 de Julio 1234',
  ciudad: 'Nueva Palmira, Colonia',
  telefono: '+598 4544 0000',
  telefonoGuardia: '+598 99 000 000',
  correo: 'contacto@vitalia.uy',
  horario: 'Lunes a viernes de 9 a 19 h · Sábados de 9 a 13 h',
  cobertura: 'Asistencia a domicilio hasta 30 km desde la clínica.',
};
