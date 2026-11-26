import { useFocusEffect, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { View } from 'react-native';
import { Boton } from '../../components/Boton';
import { FotoAnimal } from '../../components/FotoAnimal';
import { Icono } from '../../components/Icono';
import { Insignia } from '../../components/Insignia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useSesion } from '../../context/SesionContext';
import { cargarEjemplos, obtenerDe } from '../../services/animalService';
import { cargarCarnetEjemplo } from '../../services/vacunaService';
import type { Animal } from '../../types/Animal';

// «Mis animales» del Cliente (B-007, RF-CLI-01). Solo ve los animales de los que es responsable (RN-18).
export default function Animales() {
  const router = useRouter();
  const { usuario } = useSesion();
  const cuentaId = usuario?.rol === 'Cliente' ? usuario.id : null;
  const [animales, setAnimales] = useState<Animal[]>([]);

  // Se vuelve a leer cada vez que se entra, para ver los altas y cambios recién guardados.
  useFocusEffect(
    useCallback(() => {
      setAnimales(cuentaId === null ? [] : obtenerDe(cuentaId));
    }, [cuentaId]),
  );

  if (cuentaId === null) {
    return (
      <Pantalla>
        <Vacio icono="paw-outline" titulo="Ingresá para ver tus animales" accion={{ texto: 'Ingresar', onPress: () => router.navigate('/ingresar') }} />
      </Pantalla>
    );
  }

  const ejemplos = () => {
    cargarEjemplos(cuentaId);
    cargarCarnetEjemplo(cuentaId);
    setAnimales(obtenerDe(cuentaId));
  };

  return (
    <Pantalla>
      <Titulo>Mis animales</Titulo>
      <Nota>Solo ves los animales de los que sos responsable.</Nota>
      <Boton texto="Registrar animal" icono="add-circle-outline" onPress={() => router.push('/animales/nuevo')} />

      {animales.length === 0 ? (
        <>
          <Vacio icono="paw-outline" titulo="Todavía no registraste ningún animal" texto="Registralo para poder reservar turnos y ver su historia." />
          <Boton texto="Cargar ejemplos (caballo y oveja)" variante="secundario" onPress={ejemplos} />
        </>
      ) : (
        animales.map((a) => (
          <Tarjeta key={a.id} onPress={() => router.push(`/animales/${a.id}`)} etiqueta={`${a.nombre}, ${a.especie}. Ver ficha`}>
            <View className="flex-row items-center gap-3">
              <FotoAnimal uri={a.foto} nombre={a.nombre} decorativa />
              <View className="flex-1 gap-1">
                <Subtitulo>{a.nombre}</Subtitulo>
                <Parrafo suave>{[a.especie, a.raza].filter(Boolean).join(' · ')}</Parrafo>
                {a.esGrupo ? <Insignia texto={`Grupo de ${a.cantidadGrupo}`} tipo="info" /> : null}
              </View>
              <Icono nombre="chevron-forward-outline" color="texto-suave" tamano={18} />
            </View>
          </Tarjeta>
        ))
      )}
    </Pantalla>
  );
}
