import { useLocalSearchParams, useRouter } from 'expo-router';
import { View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Insignia } from '../../components/Insignia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { useAviso } from '../../context/AvisoContext';
import { useSesion } from '../../context/SesionContext';
import { cuentasDemo } from '../../data/cuentasDemo';
import { obtenerPorId } from '../../services/prestacionService';
import type { Usuario } from '../../types/Usuario';

// Provisoria (B-004): elegir una cuenta de demostración para recorrer las pestañas de cada rol.
// El formulario real de ingreso, registro, verificación y recuperación llega en B-006.
export default function Ingresar() {
  const router = useRouter();
  const avisar = useAviso();
  const { ingresar } = useSesion();
  const { servicio } = useLocalSearchParams<{ servicio?: string }>();
  const elegido = servicio ? obtenerPorId(Number(servicio)) : undefined;

  const entrar = (cuenta: Usuario) => {
    // Recepción y Administrador trabajan en escritorio (MAPA_MOVIL.md).
    if (cuenta.rol === 'Recepcionista' || cuenta.rol === 'Administrador') {
      avisar(`${cuenta.rol}: usá la versión de escritorio para tu trabajo.`, 'info');
      return;
    }
    ingresar(cuenta);
    avisar(`Ingresaste como ${cuenta.nombre}.`, 'exito');
    if (cuenta.rol === 'Veterinario') {
      router.replace('/agenda');
    } else if (elegido) {
      // A-011: volver a la acción interrumpida, con el inicio debajo para poder volver.
      // La reserva en 4 pasos llega en B-008.
      router.replace('/');
      router.push({ pathname: '/servicios/[id]', params: { id: String(elegido.id) } });
    } else {
      router.replace('/');
    }
  };

  return (
    <Pantalla>
      <Titulo>Ingresá o creá tu cuenta</Titulo>
      {elegido ? <Parrafo>Para reservar «{elegido.nombre}» necesitás ingresar.</Parrafo> : null}

      <Tarjeta>
        <Insignia texto="Demostración" tipo="info" />
        <Subtitulo>Entrar con una cuenta de prueba</Subtitulo>
        <Nota>Sin contraseña por ahora: el formulario de ingreso y el registro se construyen en B-006.</Nota>
        <View className="mt-2 gap-2">
          {cuentasDemo
            .filter((c) => c.id === 100 || c.id === 2 || c.id === 1)
            .map((c) => (
              <Boton
                key={c.id}
                texto={`${c.nombre} · ${c.rol}`}
                variante={c.rol === 'Cliente' ? 'primario' : 'secundario'}
                icono={c.rol === 'Cliente' ? 'person-outline' : c.rol === 'Veterinario' ? 'medkit-outline' : 'desktop-outline'}
                onPress={() => entrar(c)}
              />
            ))}
        </View>
      </Tarjeta>
    </Pantalla>
  );
}
