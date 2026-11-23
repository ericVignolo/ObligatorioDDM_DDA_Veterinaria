import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Boton } from '../../components/Boton';
import { Campo } from '../../components/Campo';
import { Insignia } from '../../components/Insignia';
import { Pantalla } from '../../components/Pantalla';
import { Tarjeta } from '../../components/Tarjeta';
import { Nota, Parrafo, Subtitulo, Titulo } from '../../components/Textos';
import { useAviso } from '../../context/AvisoContext';
import { useSesion } from '../../context/SesionContext';
import { CONTRASENA_DEMO } from '../../data/cuentasDemo';
import { marcarPendiente, validarCredenciales } from '../../services/cuentaService';
import { guardarIntencion, intencionVigente } from '../../services/intencionService';
import { obtenerPorId } from '../../services/prestacionService';
import { correoValido } from '../../utils/validacion';

// Cuentas de prueba para la demostración: completan el formulario, no lo saltean.
const PRUEBAS = [
  { texto: 'Ana Rodríguez · Cliente', correo: 'ana.demo@vitalia.uy' },
  { texto: 'Dra. Lucía Ferreira · Veterinario', correo: 'ferreira@vitalia.uy' },
  { texto: 'Laura Benítez · Recepción', correo: 'recepcion@vitalia.uy' },
];

// Ingreso (A-008, B-006). Acción protegida → ingresar → volver a lo que se estaba haciendo (A-011, D-REG-05).
export default function Ingresar() {
  const router = useRouter();
  const avisar = useAviso();
  const { ingresar } = useSesion();
  const { servicio } = useLocalSearchParams<{ servicio?: string }>();
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [errores, setErrores] = useState<{ correo?: string; contrasena?: string }>({});
  const [mensaje, setMensaje] = useState('');

  // La intención se guarda en el cliente y dura 30 minutos (REG-09).
  if (servicio) guardarIntencion(Number(servicio));
  const elegido = intencionVigente() !== null ? obtenerPorId(intencionVigente() as number) : undefined;

  const entrar = () => {
    const e: { correo?: string; contrasena?: string } = {};
    if (!correo.trim()) e.correo = 'Ingresá tu correo electrónico.';
    else if (!correoValido(correo)) e.correo = 'El correo no tiene un formato válido.';
    if (!contrasena) e.contrasena = 'Ingresá tu contraseña.';
    setErrores(e);
    setMensaje('');
    if (e.correo || e.contrasena) return;

    const r = validarCredenciales(correo, contrasena);
    if (r.tipo === 'invalida') {
      // No se distingue entre correo inexistente y contraseña incorrecta.
      setMensaje('El correo o la contraseña no son correctos.');
      return;
    }
    if (r.tipo === 'sin-verificar') {
      marcarPendiente(r.correo);
      avisar('Tu correo todavía no está verificado.', 'info');
      router.push('/verificar');
      return;
    }

    const usuario = r.usuario;
    // Recepción y Administrador trabajan en escritorio (MAPA_MOVIL.md).
    if (usuario.rol === 'Recepcionista' || usuario.rol === 'Administrador') {
      setMensaje(`${usuario.rol}: usá la versión de escritorio para tu trabajo.`);
      return;
    }
    ingresar(usuario);
    avisar(`Ingresaste como ${usuario.nombre}.`, 'exito');
    if (usuario.rol === 'Veterinario') {
      router.replace('/agenda');
      return;
    }
    // Vuelve a la acción interrumpida, con el inicio debajo para poder volver. La reserva en 4 pasos llega en B-008.
    const pendiente = intencionVigente();
    router.replace('/');
    if (pendiente !== null) router.push({ pathname: '/servicios/[id]', params: { id: String(pendiente) } });
  };

  return (
    <Pantalla>
      <Titulo>Ingresá o creá tu cuenta</Titulo>
      {elegido ? <Parrafo>Para reservar «{elegido.nombre}» necesitás ingresar.</Parrafo> : null}

      <Tarjeta>
        <Campo
          etiqueta="Correo electrónico"
          value={correo}
          onChangeText={setCorreo}
          error={errores.correo}
          keyboardType="email-address"
          autoCapitalize="none"
          autoComplete="email"
          autoCorrect={false}
        />
        <Campo
          etiqueta="Contraseña"
          value={contrasena}
          onChangeText={setContrasena}
          error={errores.contrasena}
          secureTextEntry
          autoCapitalize="none"
          autoComplete="current-password"
          onSubmitEditing={entrar}
        />
        {mensaje ? (
          <View accessibilityLiveRegion="polite" className="rounded-boton bg-peligro-suave p-3 dark:bg-peligro-suave-oscuro">
            <Text className="font-nunito-semi text-base text-peligro dark:text-peligro-oscuro">{mensaje}</Text>
          </View>
        ) : null}
        <Boton texto="Ingresar" icono="log-in-outline" onPress={entrar} />
        <Pressable accessibilityRole="link" onPress={() => router.push('/recuperar')} className="min-h-12 justify-center">
          <Text className="font-nunito-semi text-base text-primario dark:text-primario-oscuro">¿Olvidaste tu contraseña?</Text>
        </Pressable>
      </Tarjeta>

      <Boton texto="Crear cuenta" icono="person-add-outline" variante="secundario" onPress={() => router.push('/registro')} />

      <Tarjeta>
        <Insignia texto="Demostración" tipo="info" />
        <Subtitulo>Cuentas de prueba</Subtitulo>
        <Nota>Completan el correo y la contraseña de demostración; después tocá «Ingresar».</Nota>
        <View className="mt-1 gap-2">
          {PRUEBAS.map((c) => (
            <Boton
              key={c.correo}
              texto={c.texto}
              variante="secundario"
              onPress={() => {
                setCorreo(c.correo);
                setContrasena(CONTRASENA_DEMO);
                setErrores({});
                setMensaje('');
              }}
            />
          ))}
        </View>
      </Tarjeta>
    </Pantalla>
  );
}
