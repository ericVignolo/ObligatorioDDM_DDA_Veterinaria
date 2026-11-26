// Preferencias de comunicación (B-012, RF-CLI-18, RF-CLI-20). Como Models/TipoComunicacion.cs y PreferenciaComunicacion.cs.
export type Canal = 'Correo' | 'Solo en la app' | 'No recibir';

export type TipoComunicacion = {
  nombre: string;
  descripcion: string;
  // Los esenciales son parte del servicio: se elige el canal, pero no se apagan (D-COM-02).
  esencial: boolean;
  canalPorDefecto: Canal;
};

export type PreferenciaComunicacion = { cuentaId: number; tipo: string; canal: Canal; fecha: number };
