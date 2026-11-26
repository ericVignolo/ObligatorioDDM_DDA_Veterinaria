import { Text, View } from 'react-native';
import { signosVitales } from '../services/historiaService';
import { deConsulta } from '../services/vacunaService';
import type { Adjunto, Consulta } from '../types/Consulta';
import { diaCorto, hora } from '../utils/fechas';
import { Fila } from './Fila';
import { Insignia } from './Insignia';
import { Nota, Parrafo, Subtitulo } from './Textos';

type Props = { consulta: Consulta; onAbrirAdjunto: (adjunto: Adjunto) => void };

// Una consulta firmada, en modo lectura (RN-12), con su firma, sus enmiendas debajo del original y sus adjuntos.
// Como RegistroClinico.razor, sin la huella del contenido (solo la ve el personal) y sin controles de edición.
export function RegistroClinico({ consulta, onAbrirAdjunto }: Props) {
  const signos = signosVitales(consulta);
  const dato = (etiqueta: string, valor: string) =>
    valor.trim() ? (
      <View className="gap-0.5">
        <Text className="font-nunito-semi text-sm text-texto-suave dark:text-texto-suave-oscuro">{etiqueta}</Text>
        <Parrafo>{valor}</Parrafo>
      </View>
    ) : null;

  return (
    <View
      accessibilityLabel={`Consulta del ${diaCorto(consulta.fechaFirma)}: ${consulta.motivo}`}
      className="gap-3 rounded-tarjeta border border-borde bg-superficie p-4 dark:border-borde-oscuro dark:bg-superficie-oscuro"
    >
      <View className="flex-row flex-wrap items-center justify-between gap-2">
        <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">{diaCorto(consulta.fechaFirma)}</Text>
        <Insignia texto="Firmada" tipo="neutro" />
      </View>
      <Subtitulo>{consulta.motivo}</Subtitulo>

      {consulta.alergias.trim() ? (
        <View accessibilityRole="alert" className="rounded-boton bg-aviso-suave p-3 dark:bg-aviso-suave-oscuro">
          <Text className="font-nunito-semi text-base text-texto dark:text-texto-oscuro">Alergias: {consulta.alergias}</Text>
        </View>
      ) : null}

      {dato('Anamnesis', consulta.anamnesis)}
      {dato('Examen físico', consulta.examenFisico)}
      {dato('Signos vitales', signos)}
      {dato('Diagnóstico', `${consulta.diagnostico} (${consulta.tipoDiagnostico.toLowerCase()})`)}
      {dato('Tratamiento', consulta.tratamiento)}
      {dato('Indicaciones', consulta.indicaciones)}

      {deConsulta(consulta.id).length > 0 ? (
        <View className="gap-0.5">
          <Text className="font-nunito-semi text-sm text-texto-suave dark:text-texto-suave-oscuro">Vacunas y antiparasitarios aplicados</Text>
          {deConsulta(consulta.id).map((a) => (
            <Parrafo key={a.id}>
              • {a.producto} · {a.laboratorio} · lote {a.lote} · refuerzo {diaCorto(a.fechaRefuerzo)}
            </Parrafo>
          ))}
        </View>
      ) : null}

      {/* Enmiendas: el original nunca cambia; cada corrección queda fechada y firmada debajo (RN-12). */}
      {[...consulta.enmiendas]
        .sort((a, b) => a.fecha - b.fecha)
        .map((e) => (
          <View key={e.id} accessibilityLabel={`Enmienda del ${diaCorto(e.fecha)}`} className="gap-1 border-l-4 border-acento pl-3 dark:border-acento-oscuro">
            <Nota>
              Enmienda del {diaCorto(e.fecha)} a las {hora(e.fecha)} · {e.autorNombre} · motivo: {e.motivo}
            </Nota>
            <Parrafo>{e.texto}</Parrafo>
          </View>
        ))}

      {consulta.adjuntos.length > 0 ? (
        <View className="gap-1">
          <Text className="font-nunito-semi text-sm text-texto-suave dark:text-texto-suave-oscuro">Archivos adjuntos</Text>
          {consulta.adjuntos.map((a) => (
            <Fila
              key={a.id}
              icono={a.retirado ? 'remove-circle-outline' : a.tipo === 'pdf' ? 'document-text-outline' : 'image-outline'}
              texto={a.nombre}
              detalle={a.retirado ? 'La clínica lo retiró: ya no está disponible' : `${a.tipo === 'pdf' ? 'PDF' : 'Imagen'} · ${a.tamanoKb} KB`}
              onPress={a.retirado ? undefined : () => onAbrirAdjunto(a)}
            />
          ))}
        </View>
      ) : null}

      <View className="border-t border-borde pt-2 dark:border-borde-oscuro">
        <Nota>
          Firmado por {consulta.autorNombre} el {diaCorto(consulta.fechaFirma)} a las {hora(consulta.fechaFirma)}
        </Nota>
      </View>
    </View>
  );
}
