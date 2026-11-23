import { View } from 'react-native';
import { Boton } from './Boton';
import { Icono } from './Icono';
import { Nota, Subtitulo } from './Textos';

type Props = { icono: string; titulo: string; texto?: string; accion?: { texto: string; onPress: () => void } };

// Estado vacío: qué pasa y qué se puede hacer.
export function Vacio({ icono, titulo, texto, accion }: Props) {
  return (
    <View className="items-center gap-3 px-6 py-10">
      <View className="rounded-full bg-primario-suave p-4 dark:bg-primario-suave-oscuro">
        <Icono nombre={icono} color="primario" tamano={32} />
      </View>
      <Subtitulo>{titulo}</Subtitulo>
      {texto ? (
        <View className="items-center">
          <Nota>{texto}</Nota>
        </View>
      ) : null}
      {accion ? (
        <View className="mt-2 self-stretch">
          <Boton texto={accion.texto} onPress={accion.onPress} variante="secundario" />
        </View>
      ) : null}
    </View>
  );
}
