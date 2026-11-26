// Vacunas y antiparasitarios (B-010, RF-CLI-07). Mismos campos que Models/ProductoSanitario.cs,
// AplicacionSanitaria.cs e ItemCarnet.cs de Blazor que ve el Cliente.
export type ProductoSanitario = {
  id: number;
  nombre: string;
  tipo: 'Vacuna' | 'Antiparasitario';
  laboratorio: string;
  especies: string[];
  // Días entre una aplicación y su refuerzo: la fecha de refuerzo se calcula, nadie la escribe (RN-19).
  intervaloRefuerzoDias: number;
};

export type AplicacionSanitaria = {
  id: number;
  animalId: number;
  consultaId: number; // 0 = anterior al sistema
  productoId: number;
  producto: string;
  tipo: 'Vacuna' | 'Antiparasitario';
  laboratorio: string;
  lote: string;
  fechaAplicacion: number; // 00:00 del día
  fechaRefuerzo: number;
  aplicadaPor: string;
};

export type EstadoRefuerzo = 'Vigente' | 'Refuerzo próximo' | 'Vencida';

// Una línea del carné: la última aplicación de cada producto y el estado de su refuerzo.
export type ItemCarnet = {
  aplicacion: AplicacionSanitaria;
  estado: EstadoRefuerzo;
  diasParaRefuerzo: number; // negativo si ya pasó
};
