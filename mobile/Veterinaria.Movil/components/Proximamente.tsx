import { Pantalla } from './Pantalla';
import { Vacio } from './Vacio';

// Pestañas que ya existen en el mapa pero se construyen en una tarea posterior.
export function Proximamente({ icono, titulo, tarea }: { icono: string; titulo: string; tarea: string }) {
  return (
    <Pantalla>
      <Vacio icono={icono} titulo={titulo} texto={`Esta pantalla se construye en ${tarea}.`} />
    </Pantalla>
  );
}
