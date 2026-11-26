import type { Internacion, NovedadInternacion } from '../types/Internacion';
import { inicioDelDia, sumarDias } from '../utils/fechas';

// Internaciones de demostración con fechas relativas a hoy.
// Canela (Pedro Gómez) es el mismo caso que InternacionService de Blazor: en curso, con notas internas que el Cliente no ve.
// La de Mishi (Ana Rodríguez) es solo de la app: muestra un alta con indicaciones y una rectificación (RN-24, RN-25).
const HORA = 60 * 60 * 1000;
const aLas = (haceDias: number, horas: number) => sumarDias(inicioDelDia(new Date()), -haceDias).getTime() + horas * HORA;

export const internacionesDemo: Internacion[] = [
  {
    id: 1,
    animalId: 4,
    clienteId: 101,
    estado: 'Internado',
    motivo: 'Cólico leve, observación y fluidoterapia',
    observaciones: '',
    fechaAdmision: aLas(2, 9),
    planTerapeutico: 'Fluidoterapia IV 48 h, ayuno 12 h y reintroducción gradual de forraje. Control de frecuencia cardíaca y motilidad cada 6 h.',
    evolucionFinal: '',
    indicacionesDomicilio: '',
    fechaAlta: null,
    veterinarioAlta: '',
  },
  {
    id: 2,
    animalId: 2,
    clienteId: 100,
    estado: 'Alta',
    motivo: 'Deshidratación por gastroenteritis',
    observaciones: '',
    fechaAdmision: aLas(30, 10),
    planTerapeutico: 'Fluidoterapia y antiemético. Control cada 4 h.',
    evolucionFinal: 'Mishi recuperó la hidratación, come y no volvió a vomitar.',
    indicacionesDomicilio: 'Dieta blanda en porciones chicas durante 5 días, agua fresca siempre disponible y control si vuelve a vomitar.',
    fechaAlta: aLas(28, 17),
    veterinarioAlta: 'Dra. Lucía Ferreira',
  },
];

const entrada = (id: number, internacionId: number, tipo: NovedadInternacion['tipo'], texto: string, visible: boolean, fecha: number, autorNombre: string, rectificaId = 0): NovedadInternacion => ({
  id,
  internacionId,
  tipo,
  texto,
  visible,
  rectificaId,
  fecha,
  autorNombre,
});

const acosta = 'Dra. Valentina Acosta';
const ferreira = 'Dra. Lucía Ferreira';

export const novedadesDemo: NovedadInternacion[] = [
  entrada(1, 1, 'Evolución', 'Ingresa estable, FC 52 lpm, motilidad disminuida en cuadrante izquierdo.', false, aLas(2, 10), acosta),
  entrada(2, 1, 'Novedad', 'Canela ingresó bien y está tranquila en su box. Le estamos dando suero.', true, aLas(2, 11), acosta),
  entrada(3, 1, 'Evolución', 'Motilidad normalizada. Se inicia reintroducción de forraje.', false, aLas(1, 9), acosta),
  entrada(4, 1, 'Novedad', 'Canela comió heno con ganas y está más activa. Si sigue así, mañana evaluamos el alta.', true, aLas(1, 10), acosta),
  entrada(5, 2, 'Novedad', 'Mishi está con suero y descansando. Mañana a las 15 te contamos cómo sigue.', true, aLas(30, 12), ferreira),
  entrada(6, 2, 'Rectificación', 'Corrección: te contamos cómo sigue Mishi mañana a las 11, no a las 15.', true, aLas(30, 13), ferreira, 5),
  entrada(7, 2, 'Evolución', 'Sin vómitos en 12 h. Mucosas húmedas.', false, aLas(29, 9), ferreira),
  entrada(8, 2, 'Novedad', 'Mishi pasó bien la noche y ya aceptó comida.', true, aLas(29, 11), ferreira),
];
