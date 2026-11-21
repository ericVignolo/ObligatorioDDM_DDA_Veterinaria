import { Stack, useLocalSearchParams } from 'expo-router';
import { View } from 'react-native';
import { Insignia } from '../../components/Insignia';
import { Pantalla } from '../../components/Pantalla';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { obtenerPorCodigo } from '../../services/legalService';

// Privacidad y Términos versionados (RF-VIS-10, RN-43), con el mismo texto que Blazor.
export default function DocumentoLegal() {
  const { codigo } = useLocalSearchParams<{ codigo: string }>();
  const documento = obtenerPorCodigo(codigo ?? '');

  if (!documento) {
    return (
      <Pantalla>
        <Vacio icono="document-outline" titulo="Documento no encontrado" />
      </Pantalla>
    );
  }

  const [anio, mes, dia] = documento.vigenteDesde.split('-');

  return (
    <Pantalla>
      <Stack.Screen options={{ title: documento.titulo }} />
      <Titulo>{documento.titulo}</Titulo>
      <View className="flex-row gap-2">
        <Insignia texto={`Versión ${documento.version}`} tipo="info" />
        <Insignia texto={`Vigente desde ${dia}/${mes}/${anio}`} />
      </View>
      {documento.secciones.map((s) => (
        <View key={s.titulo} className="gap-2">
          <Subtitulo>{s.titulo}</Subtitulo>
          {s.bloques.map((b, i) => (
            <Parrafo key={i}>{b.tipo === 'li' ? `• ${b.texto}` : b.texto}</Parrafo>
          ))}
        </View>
      ))}
      <Nota>La clínica, sus datos y sus profesionales son ficticios. El texto no fue revisado por un abogado.</Nota>
    </Pantalla>
  );
}
