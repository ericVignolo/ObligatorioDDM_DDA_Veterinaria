import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';
import { Boton } from '../components/Boton';
import { Campo } from '../components/Campo';
import { Insignia } from '../components/Insignia';
import { Pantalla } from '../components/Pantalla';
import { Tarjeta } from '../components/Tarjeta';
import { Nota, Parrafo, Titulo } from '../components/Textos';
import { useAviso } from '../context/AvisoContext';
import { useSesion } from '../context/SesionContext';
import { codigoRecuperacionDemo, obtenerCorreoRecuperacion, obtenerPorId, restablecer, solicitarRecuperacion } from '../services/cuentaService';
import { correoValido } from '../utils/validacion';

// Recuperar la contraseña con un código por correo (A-007, D-REG-03). La respuesta es siempre la misma,
// exista o no la cuenta (REG-08). Dos pasos en la misma pantalla: pedir el código y crear la contraseña nueva.
export default function Recuperar() {
  const router = useRouter();
  const avisar = useAviso();
  const { usuario, salir } = useSesion();
  // Desde «Mi cuenta» (sesión abierta) el correo es el de la cuenta y no se cambia acá.
  // Se pasa solo el origen, nunca el correo, para no dejar datos personales en la dirección.
  const { desde } = useLocalSearchParams<{ desde?: string }>();
  const correoCuenta = desde === 'cuenta' && usuario?.rol === 'Cliente' ? (obtenerPorId(usuario.id)?.correo ?? '') : '';
  const [paso, setPaso] = useState<'pedir' | 'restablecer'>('pedir');
  const [correo, setCorreo] = useState(correoCuenta);
  const [codigo, setCodigo] = useState('');
  const [nueva, setNueva] = useState('');
  const [repetir, setRepetir] = useState('');
  const [errores, setErrores] = useState<{ correo?: string; codigo?: string; nueva?: string; repetir?: string }>({});
  const [mensaje, setMensaje] = useState('');
  const [exito, setExito] = useState(true);

  const pedir = () => {
    if (!correo.trim()) return setErrores({ correo: 'Ingresá tu correo electrónico.' });
    if (!correoValido(correo)) return setErrores({ correo: 'El correo no tiene un formato válido.' });
    setErrores({});
    const r = solicitarRecuperacion(correo);
    setExito(r.exito);
    setMensaje(r.mensaje);
    setPaso('restablecer');
  };

  const cambiar = () => {
    const e: typeof errores = {};
    if (!/^\d{6}$/.test(codigo.trim())) e.codigo = 'El código tiene 6 números.';
    if (!nueva) e.nueva = 'Ingresá una contraseña.';
    else if (nueva.length < 8 || nueva.length > 64) e.nueva = 'La contraseña debe tener entre 8 y 64 caracteres.';
    if (!repetir) e.repetir = 'Repetí la contraseña.';
    else if (repetir !== nueva) e.repetir = 'Las contraseñas no coinciden.';
    setErrores(e);
    if (Object.keys(e).length > 0) return;

    const r = restablecer(obtenerCorreoRecuperacion(), codigo, nueva);
    if (!r.exito) {
      setExito(false);
      setMensaje(r.mensaje);
      setCodigo('');
      return;
    }
    // D-REG-03: al cambiar la contraseña se cierra la sesión abierta de esa cuenta (como en Blazor).
    if (usuario) salir();
    avisar(r.mensaje, 'exito');
    router.replace('/ingresar');
  };

  if (paso === 'pedir') {
    return (
      <Pantalla>
        <Titulo>Recuperar contraseña</Titulo>
        <Parrafo suave>
          {correoCuenta
            ? 'Te enviamos un código al correo de tu cuenta para crear una contraseña nueva. Al cambiarla, volvés a ingresar con la nueva.'
            : 'Escribí el correo de tu cuenta y te enviamos un código para crear una contraseña nueva.'}
        </Parrafo>
        <Campo
          etiqueta="Correo electrónico"
          value={correo}
          onChangeText={setCorreo}
          editable={!correoCuenta}
          error={errores.correo}
          keyboardType="email-address"
          autoCapitalize="none"
          autoComplete="email"
          autoCorrect={false}
          onSubmitEditing={pedir}
        />
        <Boton texto="Enviarme un código" icono="mail-outline" onPress={pedir} />
        <Boton texto={correoCuenta ? 'Volver a Mi cuenta' : 'Volver a ingresar'} variante="secundario" onPress={() => router.back()} />
      </Pantalla>
    );
  }

  // SOLO DEMOSTRACIÓN: el código se muestra porque todavía no se envían correos.
  const demo = codigoRecuperacionDemo(obtenerCorreoRecuperacion());

  return (
    <Pantalla>
      <Titulo>Creá una contraseña nueva</Titulo>
      <View accessibilityLiveRegion="polite" className={`rounded-boton p-3 ${exito ? 'bg-info-suave dark:bg-info-suave-oscuro' : 'bg-peligro-suave dark:bg-peligro-suave-oscuro'}`}>
        <Text className={`font-nunito-semi text-base ${exito ? 'text-info dark:text-info-oscuro' : 'text-peligro dark:text-peligro-oscuro'}`}>{mensaje}</Text>
      </View>

      {demo ? (
        <Tarjeta>
          <Insignia texto="Demostración" tipo="aviso" />
          <Parrafo>
            Como todavía no se envían correos, el código es <Text className="font-nunito-negrita">{demo}</Text>.
          </Parrafo>
        </Tarjeta>
      ) : null}

      <Campo etiqueta="Código" value={codigo} onChangeText={setCodigo} error={errores.codigo} keyboardType="number-pad" autoComplete="one-time-code" maxLength={6} />
      <Campo etiqueta="Contraseña nueva" value={nueva} onChangeText={setNueva} error={errores.nueva} secureTextEntry autoCapitalize="none" autoComplete="new-password" ayuda="Entre 8 y 64 caracteres." />
      <Campo etiqueta="Repetir contraseña" value={repetir} onChangeText={setRepetir} error={errores.repetir} secureTextEntry autoCapitalize="none" autoComplete="new-password" />
      <Boton texto="Cambiar contraseña" icono="key-outline" onPress={cambiar} />
      <Boton texto="Pedir otro código" variante="secundario" onPress={() => setPaso('pedir')} />
      <Nota>El código vence a los 15 minutos. Tenés 5 intentos por código y podés pedir hasta 3 por hora.</Nota>
    </Pantalla>
  );
}
