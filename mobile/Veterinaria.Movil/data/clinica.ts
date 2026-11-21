import type { Clinica } from '../types/Clinica';

// Clínica ficticia (mismos datos que ClinicaService de Blazor). En la sección C: GET /api/v1/clinica.
export const clinica: Clinica = {
  nombre: 'VITALIA',
  direccion: 'Nueva Palmira, Colonia',
  telefono: '+598 4544 0000',
  telefonoGuardia: '+598 99 000 000',
  correo: 'contacto@vitalia.uy',
  horario: 'Lunes a viernes de 9 a 19 y sábados de 9 a 13',
};
