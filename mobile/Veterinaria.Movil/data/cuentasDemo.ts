import type { Usuario } from '../types/Usuario';

// Cuentas de demostración: las mismas que CuentaService y PersonalService de Blazor y que la semilla de la API.
// Contraseña de todas: Vitalia2026! (solo demostración). El formulario real de ingreso llega en B-006.
export const CONTRASENA_DEMO = 'Vitalia2026!';

export const cuentasDemo: Usuario[] = [
  { id: 100, nombre: 'Ana Rodríguez', correo: 'ana.demo@vitalia.uy', rol: 'Cliente' },
  { id: 101, nombre: 'Pedro Gómez', correo: 'pedro.demo@vitalia.uy', rol: 'Cliente' },
  { id: 2, nombre: 'Dra. Lucía Ferreira', correo: 'ferreira@vitalia.uy', rol: 'Veterinario', profesionalId: 1 },
  { id: 3, nombre: 'Dr. Martín Silveira', correo: 'silveira@vitalia.uy', rol: 'Veterinario', profesionalId: 2 },
  { id: 4, nombre: 'Dra. Valentina Acosta', correo: 'acosta@vitalia.uy', rol: 'Veterinario', profesionalId: 3 },
  { id: 1, nombre: 'Laura Benítez', correo: 'recepcion@vitalia.uy', rol: 'Recepcionista' },
  { id: 5, nombre: 'Gonzalo Rivas', correo: 'admin@vitalia.uy', rol: 'Administrador' },
];
