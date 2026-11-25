import { useLocalSearchParams, useRouter } from 'expo-router';
import { FormularioAnimal } from '../../../components/FormularioAnimal';
import { Pantalla } from '../../../components/Pantalla';
import { Titulo } from '../../../components/Textos';
import { Vacio } from '../../../components/Vacio';
import { useAviso } from '../../../context/AvisoContext';
import { useSesion } from '../../../context/SesionContext';
import { editar, obtenerPorId, paraEditar } from '../../../services/animalService';

// Edición de la ficha (B-007, CU-CLI-01). Solo el responsable puede editarla (RN-18).
export default function EditarAnimal() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario } = useSesion();
  const { id } = useLocalSearchParams<{ id: string }>();
  const animal = usuario?.rol === 'Cliente' ? obtenerPorId(Number(id), usuario.id) : undefined;

  if (!usuario || !animal) {
    return (
      <Pantalla>
        <Vacio icono="paw-outline" titulo="Animal no disponible" texto="No encontramos ese animal entre los tuyos." accion={{ texto: 'Ir a mis animales', onPress: () => router.navigate('/animales') }} />
      </Pantalla>
    );
  }

  return (
    <Pantalla>
      <Titulo>Editar a {animal.nombre}</Titulo>
      <FormularioAnimal
        inicial={paraEditar(animal)}
        textoBoton="Guardar cambios"
        onGuardar={(datos) => {
          const r = editar(animal.id, datos, usuario.id);
          if (r.exito) {
            avisar(r.mensaje, 'exito');
            router.back();
          }
          return r;
        }}
      />
    </Pantalla>
  );
}
