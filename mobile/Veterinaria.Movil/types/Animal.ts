// Ficha de un animal o de un grupo de animales (B-007, RF-CLI-01). Mismos campos que Models/Animal.cs de Blazor.
// En la sección C la guarda la API; el Cliente solo ve los suyos (RN-18).
export type Animal = {
  id: number;
  responsableId: number;
  nombre: string;
  especie: string;
  raza: string;
  sexo: string;
  fechaNacimiento: string | null; // AAAA-MM-DD
  // Identificadores permitidos: microchip (único, RN-41) y caravana de campo.
  microchip: string;
  caravana: string;
  // Para animales de campo que se atienden en grupo (por ejemplo, una majada).
  esGrupo: boolean;
  cantidadGrupo: number | null;
  senasParticulares: string;
  antecedentes: string;
  fechaAlta: number;
};

// Datos del formulario de alta y edición (CU-CLI-01). La fecha y la cantidad se escriben como texto.
export type DatosAnimal = {
  nombre: string;
  especie: string;
  raza: string;
  sexo: string;
  fechaNacimiento: string; // DD/MM/AAAA, opcional
  microchip: string;
  caravana: string;
  esGrupo: boolean;
  cantidadGrupo: string;
  senasParticulares: string;
  antecedentes: string;
};
