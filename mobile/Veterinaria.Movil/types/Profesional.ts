// Mismo modelo que Models/Profesional.cs, solo con lo público (sin matrícula ni ausencias).
// dia: 1 = lunes … 6 = sábado, 0 = domingo (como DayOfWeek de C#). Horas en formato HH:mm.
export type FranjaHoraria = { dia: number; inicio: string; fin: string };

export type Profesional = {
  id: number;
  nombre: string;
  especialidad: string;
  descripcion: string;
  jornada: FranjaHoraria[];
  descansos: FranjaHoraria[];
  especies: string[];
  prestacionIds: number[];
  publicado: boolean;
};
