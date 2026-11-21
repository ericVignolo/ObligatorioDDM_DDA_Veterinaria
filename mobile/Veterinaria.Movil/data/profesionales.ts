import type { FranjaHoraria, Profesional } from '../types/Profesional';

// Los mismos profesionales que ProfesionalService de Blazor (RN-45: sin títulos ni premios no demostrables).
// En la sección C: GET /api/v1/profesionales.
const franjas = (dias: number[], inicio: string, fin: string): FranjaHoraria[] => dias.map((dia) => ({ dia, inicio, fin }));
const LUN_A_VIE = [1, 2, 3, 4, 5];

export const profesionales: Profesional[] = [
  {
    id: 1,
    nombre: 'Dra. Lucía Ferreira',
    especialidad: 'Clínica de pequeños animales',
    descripcion: 'Consultas generales, vacunación y controles de perros, gatos y otros animales de compañía.',
    jornada: franjas(LUN_A_VIE, '09:00', '14:00'),
    descansos: franjas(LUN_A_VIE, '11:30', '12:00'),
    especies: ['Perro', 'Gato', 'Ave', 'Conejo', 'Reptil', 'Otra'],
    prestacionIds: [1, 2, 3, 8],
    publicado: true,
  },
  {
    id: 2,
    nombre: 'Dr. Martín Silveira',
    especialidad: 'Grandes animales',
    descripcion: 'Atención de bovinos, ovinos, caprinos y porcinos, revisiones de rodeo y visitas a campo.',
    jornada: [...franjas(LUN_A_VIE, '14:00', '19:00'), ...franjas([6], '09:00', '13:00')],
    descansos: franjas(LUN_A_VIE, '16:30', '17:00'),
    especies: ['Vaca', 'Oveja', 'Cabra', 'Cerdo'],
    prestacionIds: [1, 2, 3],
    publicado: true,
  },
  {
    id: 3,
    nombre: 'Dra. Valentina Acosta',
    especialidad: 'Equinos',
    descripcion: 'Clínica y rehabilitación de caballos. La rehabilitación requiere evaluación previa.',
    jornada: franjas([2, 4], '09:00', '17:00'),
    descansos: franjas([2, 4], '12:30', '13:30'),
    especies: ['Caballo'],
    prestacionIds: [1],
    publicado: true,
  },
  {
    id: 5,
    nombre: 'Sofía Méndez',
    especialidad: 'Baño y estética',
    descripcion: 'Baño, corte y cuidado estético de perros y gatos. No realiza atención clínica.',
    jornada: [...franjas(LUN_A_VIE, '10:00', '18:00'), ...franjas([6], '09:00', '13:00')],
    descansos: franjas(LUN_A_VIE, '13:00', '14:00'),
    especies: ['Perro', 'Gato'],
    prestacionIds: [6],
    publicado: true,
  },
  {
    id: 4,
    nombre: 'Dr. Andrés Pereira',
    especialidad: 'Clínica general',
    descripcion: 'No publicado: no debe aparecer.',
    jornada: [],
    descansos: [],
    especies: [],
    prestacionIds: [],
    publicado: false,
  },
];
