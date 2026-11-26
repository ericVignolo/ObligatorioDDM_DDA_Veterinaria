import { Stack, useLocalSearchParams, useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { Image, Text, View } from 'react-native';
import { Boton } from '../../../../components/Boton';
import { Insignia } from '../../../../components/Insignia';
import { Pantalla } from '../../../../components/Pantalla';
import { Tarjeta } from '../../../../components/Tarjeta';
import { Nota, Parrafo, Titulo } from '../../../../components/Textos';
import { Vacio } from '../../../../components/Vacio';
import { useSesion } from '../../../../context/SesionContext';
import { adjuntoDe, emitirAcceso, MINUTOS_ACCESO_ADJUNTO } from '../../../../services/historiaService';
import { diaCorto, hora } from '../../../../utils/fechas';

const IMAGEN_DEMO = require('../../../../assets/demo/estudio-demo.png');

// Ver un adjunto de la historia (B-009, RF-CLI-08, CU-CLI-05 pasos 5 a 7) con un acceso temporal simulado.
// En la sección C la API verifica la responsabilidad y emite un enlace que vence; nunca hay enlaces permanentes.
export default function VerAdjunto() {
  const router = useRouter();
  const { usuario } = useSesion();
  const { id, adjuntoId } = useLocalSearchParams<{ id: string; adjuntoId: string }>();
  const clienteId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const encontrado = clienteId === null ? null : adjuntoDe(Number(id), Number(adjuntoId), clienteId);

  const [vence, setVence] = useState(() => emitirAcceso());
  const [ahora, setAhora] = useState(Date.now());
  useEffect(() => {
    const t = setInterval(() => setAhora(Date.now()), 15000);
    return () => clearInterval(t);
  }, []);

  if (!encontrado) {
    return (
      <Pantalla>
        <Vacio icono="document-outline" titulo="Archivo no disponible" texto="No encontramos ese archivo en la historia de tus animales." accion={{ texto: 'Volver', onPress: () => router.back() }} />
      </Pantalla>
    );
  }

  const { consulta, adjunto } = encontrado;
  const vencido = ahora >= vence;

  return (
    <Pantalla>
      <Stack.Screen options={{ title: adjunto.tipo === 'pdf' ? 'Documento' : 'Imagen' }} />
      <Titulo>{adjunto.nombre}</Titulo>
      <Nota>
        De la consulta del {diaCorto(consulta.fechaFirma)} · {consulta.motivo}
      </Nota>
      <Insignia texto="Archivo de demostración" tipo="aviso" />

      {adjunto.retirado ? (
        <Tarjeta>
          <Parrafo>La clínica retiró este archivo: ya no está disponible.</Parrafo>
        </Tarjeta>
      ) : vencido ? (
        // CU-CLI-05, alternativa «enlace vencido»: se pide un acceso nuevo.
        <Tarjeta>
          <Parrafo>El acceso a este archivo venció. Pedí uno nuevo para volver a verlo.</Parrafo>
          <Boton texto="Pedir un acceso nuevo" icono="refresh-outline" onPress={() => { setVence(emitirAcceso()); setAhora(Date.now()); }} />
        </Tarjeta>
      ) : adjunto.tipo === 'imagen' ? (
        <>
          {/* El contenedor fija la proporción (900 × 700) y la imagen lo llena: en la web Image no respeta aspectRatio. */}
          <View style={{ width: '100%', aspectRatio: 900 / 700, borderRadius: 12, overflow: 'hidden', backgroundColor: '#000' }}>
            <Image
              source={IMAGEN_DEMO}
              resizeMode="contain"
              accessibilityLabel={`${adjunto.nombre}. Imagen de demostración, no es un estudio real.`}
              style={{ width: '100%', height: '100%' }}
            />
          </View>
          <Nota>
            Acceso temporal: vence a las {hora(vence)} ({MINUTOS_ACCESO_ADJUNTO} minutos). Después hay que pedir uno nuevo.
          </Nota>
        </>
      ) : (
        // CU-CLI-05, alternativa «formato sin previsualización».
        <Tarjeta>
          <View className="flex-row items-center gap-2">
            <Text className="font-nunito-negrita text-base text-texto dark:text-texto-oscuro">PDF · {adjunto.tamanoKb} KB</Text>
          </View>
          <Parrafo>Este formato no tiene vista previa en la app de demostración. Con la API vas a poder descargarlo y abrirlo con el visor del celular.</Parrafo>
          <Nota>
            Acceso temporal: vence a las {hora(vence)} ({MINUTOS_ACCESO_ADJUNTO} minutos).
          </Nota>
        </Tarjeta>
      )}
    </Pantalla>
  );
}
