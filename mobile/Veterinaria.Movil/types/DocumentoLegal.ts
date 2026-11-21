export type BloqueLegal = { tipo: 'p' | 'li'; texto: string };

export type DocumentoLegal = {
  codigo: 'privacidad' | 'terminos';
  titulo: string;
  version: string;
  vigenteDesde: string; // AAAA-MM-DD
  secciones: { titulo: string; bloques: BloqueLegal[] }[];
};
