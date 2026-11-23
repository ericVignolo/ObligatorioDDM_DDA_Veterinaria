// Quién ingresó. Recepción y Administrador usan el escritorio (MAPA_MOVIL.md).
export type Rol = 'Cliente' | 'Veterinario' | 'Recepcionista' | 'Administrador';

export type Usuario = {
  id: number;
  nombre: string;
  correo: string;
  rol: Rol;
  profesionalId?: number; // solo Veterinario
};
