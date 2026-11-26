import type { DocumentoConsentimiento } from '../types/Consentimiento';

// Mismo documento que ConsentimientoService de Blazor. Si cambia el texto allá, hay que copiarlo acá.
export const documentosConsentimiento: DocumentoConsentimiento[] = [
  {
    id: 1,
    prestacionId: 8,
    version: '1.0',
    titulo: 'Consentimiento informado para castración con anestesia general',
    texto:
      'Autorizo al equipo veterinario de VITALIA a realizar la castración del animal indicado, bajo anestesia general. ' +
      'Me explicaron en qué consiste el procedimiento, los cuidados previos (ayuno de 8 horas de comida y 2 de agua) y los posteriores. ' +
      'Entiendo que toda anestesia y cirugía tienen riesgos, poco frecuentes pero posibles: reacciones a los fármacos, sangrado, ' +
      'infección de la herida y, excepcionalmente, complicaciones graves o la muerte. ' +
      'Autorizo al veterinario a tomar las medidas necesarias si surge una complicación durante la cirugía y me comprometo a ' +
      'informar cualquier enfermedad, medicación o alergia del animal. Puedo hacer preguntas antes de firmar y retirar este ' +
      'consentimiento avisando a la clínica antes del procedimiento.',
    vigenteDesde: new Date(2026, 10, 1).getTime(),
    vigente: true,
  },
];
