import { View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Insignia, type TipoInsignia } from '../../components/Insignia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo } from '../../components/Textos';
import { llamar, obtenerClinica } from '../../services/clinicaService';

// Mismo contenido que Urgencias.razor (HU-VIS-06, RF-VIS-06): primero el teléfono, sin formularios ni sesión.
const SENALES = [
  'no respira o respira con mucha dificultad;',
  'perdió el conocimiento o tiene convulsiones;',
  'tiene un sangrado que no se detiene;',
  'fue atropellado, sufrió una caída fuerte o tiene una herida profunda;',
  'pudo haber comido un veneno, un medicamento o una planta tóxica;',
  'tiene el abdomen hinchado y duro, o intenta vomitar sin poder;',
  'es una hembra con un parto que no avanza.',
];

// Niveles de ESPECIFICACION_ALERTAS_ASISTENCIA.md, en orden de prioridad.
const NIVELES: { nombre: string; tipo: TipoInsignia; significado: string; horario: string }[] = [
  { nombre: 'Rojo', tipo: 'peligro', significado: 'Posible riesgo para la vida o empeoramiento grave.', horario: 'En todo horario, por la guardia. Es la prioridad máxima.' },
  { nombre: 'Naranja', tipo: 'acento', significado: 'Urgencia alta, sin riesgo para la vida confirmado.', horario: 'En el horario de la clínica.' },
  { nombre: 'Amarillo', tipo: 'aviso', significado: 'Necesita atención prioritaria, pero puede esperar un poco.', horario: 'En el horario de la clínica, después de los naranjas.' },
  { nombre: 'Verde', tipo: 'exito', significado: 'Situación estable o que se puede programar.', horario: 'En el horario de la clínica, después de los niveles anteriores.' },
];

export default function Urgencias() {
  const clinica = obtenerClinica();

  return (
    <Pantalla>
      <View className="gap-3 rounded-tarjeta bg-peligro-suave p-4 dark:bg-peligro-suave-oscuro">
        <Subtitulo>Si tu animal está en peligro, llamá ahora a la guardia.</Subtitulo>
        <Boton texto="Llamar a la guardia" icono="call-outline" variante="peligro" onPress={() => llamar(clinica.telefonoGuardia)} />
        <Parrafo>
          Guardia: {clinica.telefonoGuardia}. Atiende las 24 horas. Dirección: {clinica.direccion}, {clinica.ciudad}.
        </Parrafo>
      </View>

      <Subtitulo>Llamá a la guardia sin esperar si tu animal:</Subtitulo>
      <View className="gap-2">
        {SENALES.map((s) => (
          <Parrafo key={s}>• {s}</Parrafo>
        ))}
      </View>
      <Nota>Esta lista es orientativa. No reemplaza la evaluación de un veterinario: ante la duda, llamá.</Nota>

      <Subtitulo>Cómo clasificamos las solicitudes de asistencia</Subtitulo>
      <Parrafo>Cuando pedís asistencia, el equipo asigna un nivel según la gravedad. El nivel define el orden de atención.</Parrafo>
      {NIVELES.map((n) => (
        <Tarjeta key={n.nombre}>
          <Insignia texto={n.nombre} tipo={n.tipo} />
          <Parrafo>{n.significado}</Parrafo>
          <Nota>{n.horario}</Nota>
        </Tarjeta>
      ))}
      <Nota>El nivel es una clasificación inicial y puede cambiar cuando un veterinario evalúa el caso. Pedir asistencia no garantiza un horario de llegada.</Nota>

      <Subtitulo>Asistencia a domicilio</Subtitulo>
      <Parrafo>
        {clinica.cobertura} Para pedirla desde la aplicación vas a necesitar una cuenta. Si es grave, no esperes: llamá a la guardia.
      </Parrafo>
    </Pantalla>
  );
}
