import type { Consulta } from '../types/Consulta';

// Las mismas consultas firmadas de demostración que ConsultaService de Blazor (Toby y Canela).
// Los adjuntos solo existen en la app: Blazor los deja para la API (RF-VET-03). Son archivos de demostración, no sensibles.
const DIA = 24 * 60 * 60 * 1000;
const haceDias = (dias: number) => {
  const d = new Date();
  d.setHours(10, 25, 0, 0);
  return d.getTime() - dias * DIA;
};

export const consultasDemo: Consulta[] = [
  {
    id: 1,
    animalId: 1,
    autorNombre: 'Dra. Lucía Ferreira',
    fechaFirma: haceDias(60),
    motivo: 'Control anual y vacunación',
    anamnesis: '',
    examenFisico: '',
    temperaturaC: null,
    frecuenciaCardiaca: null,
    frecuenciaRespiratoria: null,
    pesoKg: 18.4,
    alergias: '',
    diagnostico: 'Paciente sano. Sobrepeso leve.',
    tipoDiagnostico: 'Definitivo',
    tratamiento: 'Vacuna polivalente aplicada. Dieta con 10 % menos de ración.',
    indicaciones: '',
    enmiendas: [
      {
        id: 1,
        motivo: 'Dato omitido',
        texto: 'El responsable informó después de la consulta que tuvo una reacción leve a una pipeta antiparasitaria en 2025.',
        autorNombre: 'Dra. Lucía Ferreira',
        fecha: haceDias(58),
      },
    ],
    adjuntos: [
      { id: 1, nombre: 'Radiografía de tórax (demostración).png', tipo: 'imagen', tamanoKb: 28, retirado: false },
      { id: 2, nombre: 'Hemograma de laboratorio externo (demostración).pdf', tipo: 'pdf', tamanoKb: 112, retirado: false },
      { id: 3, nombre: 'Foto de evolución (retirada).jpg', tipo: 'imagen', tamanoKb: 64, retirado: true },
    ],
  },
  {
    id: 2,
    animalId: 4,
    autorNombre: 'Dra. Valentina Acosta',
    fechaFirma: haceDias(30),
    motivo: 'Claudicación del miembro anterior izquierdo',
    anamnesis: '',
    examenFisico: '',
    temperaturaC: null,
    frecuenciaCardiaca: null,
    frecuenciaRespiratoria: null,
    pesoKg: 430,
    alergias: 'Penicilina',
    diagnostico: 'Tendinitis leve del flexor digital superficial.',
    tipoDiagnostico: 'Presuntivo',
    tratamiento: 'Reposo en box 10 días, frío local dos veces por día. Control en dos semanas.',
    indicaciones: '',
    enmiendas: [],
    adjuntos: [{ id: 4, nombre: 'Ecografía del tendón (demostración).png', tipo: 'imagen', tamanoKb: 28, retirado: false }],
  },
];
