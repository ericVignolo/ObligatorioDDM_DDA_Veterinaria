import { useRouter } from 'expo-router';
import { FormularioAnimal } from '../../components/FormularioAnimal';
import { Pantalla } from '../../components/Pantalla';
import { Nota, Titulo } from '../../components/Textos';
import { Vacio } from '../../components/Vacio';
import { useAviso } from '../../context/AvisoContext';
import { useSesion } from '../../context/SesionContext';
import { crear, datosVacios } from '../../services/animalService';

// Alta de un animal o grupo (B-007, CU-CLI-01). Queda a nombre de quien ingresó.
export default function NuevoAnimal() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario } = useSesion();

  if (!usuario || usuario.rol !== 'Cliente') {
    return (
      <Pantalla>
        <Vacio icono="paw-outline" titulo="Ingresá para registrar un animal" accion={{ texto: 'Ingresar', onPress: () => router.replace('/ingresar') }} />
      </Pantalla>
    );
  }

  return (
    <Pantalla>
      <Titulo>Registrar animal</Titulo>
      <Nota>Los campos sin «(opcional)» son obligatorios.</Nota>
      <FormularioAnimal
        inicial={datosVacios()}
        textoBoton="Registrar"
        onGuardar={(datos) => {
          const r = crear(datos, usuario.id);
          if (r.exito && r.id) {
            avisar(r.mensaje, 'exito');
            // Se reemplaza el formulario por la ficha: «Volver» lleva a la lista, no al formulario vacío.
            router.replace(`/animales/${r.id}`);
          }
          return r;
        }}
      />
    </Pantalla>
  );
}
