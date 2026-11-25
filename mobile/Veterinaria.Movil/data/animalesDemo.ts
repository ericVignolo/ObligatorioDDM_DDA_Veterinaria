import type { Animal } from '../types/Animal';

// Mismos animales de demostración que AnimalService de Blazor (Ana Rodríguez = 100, Pedro Gómez = 101).
// Toby también sirve para probar que no se puede repetir su microchip (RN-41).
const alta = new Date(2026, 9, 1).getTime();

export const animalesDemo: Animal[] = [
  { id: 1, responsableId: 100, nombre: 'Toby', especie: 'Perro', raza: 'Mestizo', sexo: 'Macho', fechaNacimiento: null, microchip: '858000000000001', caravana: '', esGrupo: false, cantidadGrupo: null, senasParticulares: '', antecedentes: '', fechaAlta: alta },
  { id: 2, responsableId: 100, nombre: 'Mishi', especie: 'Gato', raza: 'Común europeo', sexo: 'Hembra', fechaNacimiento: null, microchip: '', caravana: '', esGrupo: false, cantidadGrupo: null, senasParticulares: '', antecedentes: '', fechaAlta: alta },
  { id: 3, responsableId: 101, nombre: 'Rodeo La Esperanza', especie: 'Vaca', raza: 'Hereford', sexo: 'Hembra', fechaNacimiento: null, microchip: '', caravana: 'UY-BV-0815', esGrupo: true, cantidadGrupo: 25, senasParticulares: '', antecedentes: '', fechaAlta: alta },
  { id: 4, responsableId: 101, nombre: 'Canela', especie: 'Caballo', raza: 'Criollo', sexo: 'Hembra', fechaNacimiento: null, microchip: '', caravana: '', esGrupo: false, cantidadGrupo: null, senasParticulares: '', antecedentes: '', fechaAlta: alta },
];
