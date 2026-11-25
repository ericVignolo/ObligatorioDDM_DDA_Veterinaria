import { useFocusEffect, useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { Text, View } from 'react-native';
import { Boton } from '../../../components/Boton';
import { Insignia } from '../../../components/Insignia';
import { Pantalla } from '../../../components/Pantalla';
import { Tarjeta } from '../../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../../components/Textos';
import { Vacio } from '../../../components/Vacio';
import { useSesion } from '../../../context/SesionContext';
import { mostrarFecha, obtenerPorId } from '../../../services/animalService';
import type { Animal } from '../../../types/Animal';

// Ficha del animal (B-007, RF-CLI-01). Si no existe o no es del Cliente, no se distingue el caso (RN-18).
export default function FichaAnimal() {
  const router = useRouter();
  const { usuario } = useSesion();
  const { id } = useLocalSearchParams<{ id: string }>();
  const [animal, setAnimal] = useState<Animal | undefined>();

  // Se vuelve a leer al volver de «Editar» para mostrar los datos nuevos.
  useFocusEffect(
    useCallback(() => {
      const a = usuario?.rol === 'Cliente' ? obtenerPorId(Number(id), usuario.id) : undefined;
      setAnimal(a ? { ...a } : undefined);
    }, [id, usuario]),
  );

  if (!usuario || !animal) {
    return (
      <Pantalla>
        <Vacio icono="paw-outline" titulo="Animal no disponible" texto="No encontramos ese animal entre los tuyos." accion={{ texto: 'Ir a mis animales', onPress: () => router.navigate('/animales') }} />
      </Pantalla>
    );
  }

  const dato = (etiqueta: string, valor: string) => (
    <View className="flex-row justify-between gap-3 border-b border-borde py-2 dark:border-borde-oscuro">
      <Text className="font-nunito-semi text-base text-texto-suave dark:text-texto-suave-oscuro">{etiqueta}</Text>
      <Text className="flex-1 text-right font-nunito text-base text-texto dark:text-texto-oscuro">{valor || 'Sin dato'}</Text>
    </View>
  );

  return (
    <Pantalla>
      <View className="gap-2">
        <Titulo>{animal.nombre}</Titulo>
        <View className="flex-row flex-wrap items-center gap-2">
          <Parrafo suave>{animal.especie}</Parrafo>
          {animal.esGrupo ? <Insignia texto={`Grupo de ${animal.cantidadGrupo}`} tipo="info" /> : null}
        </View>
      </View>

      <Tarjeta>
        <Subtitulo>Datos</Subtitulo>
        {dato('Raza', animal.raza)}
        {dato('Sexo', animal.sexo)}
        {!animal.esGrupo ? dato('Nacimiento', mostrarFecha(animal.fechaNacimiento)) : null}
        {!animal.esGrupo ? dato('Microchip', animal.microchip) : null}
        {dato('Caravana', animal.caravana)}
      </Tarjeta>

      <Tarjeta>
        <Subtitulo>Responsable</Subtitulo>
        <Parrafo>{usuario.nombre}</Parrafo>
        <Subtitulo>Señas particulares</Subtitulo>
        <Parrafo>{animal.senasParticulares || 'Sin dato'}</Parrafo>
        <Subtitulo>Antecedentes informados</Subtitulo>
        <Parrafo>{animal.antecedentes || 'Sin dato'}</Parrafo>
      </Tarjeta>

      <Boton texto="Editar" icono="create-outline" onPress={() => router.push(`/animales/${animal.id}/editar`)} />

      {/* Historia clínica (B-009), carné sanitario (B-010) e internación (B-011) se suman en sus tareas. */}
      <Tarjeta>
        <Subtitulo>Historia clínica y vacunas</Subtitulo>
        <Nota>La historia clínica, el carné de vacunas y las internaciones se agregan en las próximas tareas (B-009 a B-011).</Nota>
      </Tarjeta>
    </Pantalla>
  );
}
