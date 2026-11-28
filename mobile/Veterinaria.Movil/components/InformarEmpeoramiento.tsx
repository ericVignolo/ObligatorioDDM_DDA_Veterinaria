import { useState } from 'react';
import { Text, View } from 'react-native';
import { useAviso } from '../context/AvisoContext';
import { empeoramientoVacio, informarEmpeoramiento, LARGOS, NIVELES, prioridad, validarEmpeoramiento } from '../services/asistenciaService';
import type { DatosEmpeoramiento, NivelUrgencia } from '../types/Asistencia';
import type { Errores } from '../types/Cuenta';
import { Boton } from './Boton';
import { Campo } from './Campo';
import { Opcion } from './Opcion';
import { Nota, Subtitulo } from './Textos';

type Props = { solicitudId: number; clienteId: number; nivelActual: NivelUrgencia; onListo: () => void; onCancelar: () => void };

// Informar empeoramiento (B-014, RF-CLI-24, RN-51): el cliente elige cómo lo ve ahora y cuenta qué cambió.
// Se ofrecen el nivel actual y los más graves: el cliente no puede bajarlo (RN-52).
export function InformarEmpeoramiento({ solicitudId, clienteId, nivelActual, onListo, onCancelar }: Props) {
  const avisar = useAviso();
  const [datos, setDatos] = useState<DatosEmpeoramiento>(empeoramientoVacio);
  const [errores, setErrores] = useState<Errores<DatosEmpeoramiento>>({});
  const [mensaje, setMensaje] = useState('');
  const niveles = NIVELES.filter((n) => prioridad(n.nombre) <= prioridad(nivelActual));

  const enviar = () => {
    const e = validarEmpeoramiento(datos);
    setErrores(e);
    if (Object.keys(e).length > 0) return;
    const r = informarEmpeoramiento(solicitudId, clienteId, datos);
    if (!r.exito) return setMensaje(r.mensaje);
    avisar(r.mensaje, 'exito');
    onListo();
  };

  return (
    <View className="gap-3">
      <Subtitulo>¿Qué tan grave lo ves ahora?</Subtitulo>
      <View accessibilityRole="radiogroup" accessibilityLabel="Gravedad ahora" className="gap-2">
        {niveles.map((n) => (
          <Opcion
            key={n.nombre}
            titulo={`${n.simbolo} ${n.nombre}${n.nombre === nivelActual ? ' (actual)' : ''}`}
            detalle={n.descripcion}
            elegida={datos.nivel === n.nombre}
            onPress={() => {
              setDatos((d) => ({ ...d, nivel: n.nombre }));
              setErrores((x) => ({ ...x, nivel: undefined }));
            }}
          />
        ))}
      </View>
      {errores.nivel ? <Text className="font-nunito-semi text-sm text-peligro dark:text-peligro-oscuro">{errores.nivel}</Text> : null}
      <Campo
        etiqueta="Qué cambió"
        value={datos.motivo}
        onChangeText={(motivo) => {
          setDatos((d) => ({ ...d, motivo }));
          setErrores((x) => ({ ...x, motivo: undefined }));
        }}
        error={errores.motivo}
        maxLength={LARGOS.motivo}
        multiline
      />
      <Nota>Si es grave, además llamá a la guardia.</Nota>
      {mensaje ? <Text accessibilityRole="alert" className="font-nunito-semi text-base text-peligro dark:text-peligro-oscuro">{mensaje}</Text> : null}
      <Boton texto="Enviar aviso" icono="alert-circle-outline" variante="peligro" onPress={enviar} />
      <Boton texto="Cancelar" variante="secundario" onPress={onCancelar} />
    </View>
  );
}
