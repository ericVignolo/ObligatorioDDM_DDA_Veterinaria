import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import type { Usuario } from '../types/Usuario';

// Sesión simulada de la sección B: vive en memoria y se pierde al cerrar la app.
// En la sección C se reemplaza por los tokens de la API guardados con expo-secure-store (C-033).
type Sesion = {
  usuario: Usuario | null;
  ingresar: (usuario: Usuario) => void;
  salir: () => void;
};

const SesionContext = createContext<Sesion | null>(null);

export function SesionProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<Usuario | null>(null);

  const valor = useMemo<Sesion>(
    () => ({ usuario, ingresar: setUsuario, salir: () => setUsuario(null) }),
    [usuario],
  );

  return <SesionContext.Provider value={valor}>{children}</SesionContext.Provider>;
}

export function useSesion(): Sesion {
  const sesion = useContext(SesionContext);
  if (!sesion) throw new Error('useSesion se usa dentro de SesionProvider');
  return sesion;
}
