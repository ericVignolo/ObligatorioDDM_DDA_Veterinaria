// Mismo modelo que desktop/Veterinaria.Blazor/Models/Prestacion.cs (se llama Prestacion para no confundir con services/).
export type Prestacion = {
  id: number;
  nombre: string;
  descripcion: string;
  precioDesde: number;
  reservableEnLinea: boolean;
  duracionMinutos?: number; // solo los que se reservan en línea
  publicado: boolean;
  requiereConsentimiento?: boolean; // cirugías: consentimiento informado (RN-10)
};
