import { documentosLegales } from '../data/legales';

export function obtenerVigentes() {
  return documentosLegales;
}

export function obtenerPorCodigo(codigo: string) {
  return documentosLegales.find((d) => d.codigo === codigo);
}
