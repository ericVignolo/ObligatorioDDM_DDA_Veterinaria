import { useRouter } from 'expo-router';
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
import { codigoDemo, obtenerCorreoPendiente, obtenerPorCorreo, reenviarCodigo, usuarioDe, verificar } from '../services/cuentaService';
import { intencionVigente } from '../services/intencionService';

// Verificación del correo con un código de 6 números (A-007, D-REG-02). Verificada la cuenta, se ingresa sin pedir la contraseña
// otra vez y se retoma la acción interrumpida (A-011).
export default function Verificar() {
  const router = useRouter();
  const avisar = useAviso();
  const { ingresar } = useSesion();
  const [correo] = useState(obtenerCorreoPendiente());
  const [codigo, setCodigo] = useState('');
  const [error, setError] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [exito, setExito] = useState(false);
  const [, refrescar] = useState(0);

  const cuenta = obtenerPorCorreo(correo);

  if (!cuenta) {
    return (
      <Pantalla>
        <Titulo>Verificá tu correo</Titulo>
        <Parrafo>No hay una cuenta pendiente de verificar.</Parrafo>
        <Boton texto="Crear cuenta" icono="person-add-outline" onPress={() => router.replace('/registro')} />
      </Pantalla>
    );
  }

  const confirmar = () => {
    if (!/^\d{6}$/.test(codigo.trim())) {
      setError('El código tiene 6 números.');
      return;
    }
    setError('');
    const r = verificar(cuenta.correo, codigo);
    setExito(r.exito);
    setMensaje(r.mensaje);
    setCodigo('');
    if (r.exito) {
      // Verificada: entra a la sesión sin volver a pedir la contraseña.
      ingresar(usuarioDe(cuenta));
      avisar(r.mensaje, 'exito');
    }
  };

  const reenviar = () => {
    const r = reenviarCodigo(cuenta.correo);
    setExito(r.exito);
    setMensaje(r.mensaje);
    setCodigo('');
    setError('');
    refrescar((n) => n + 1);
  };

  // Vuelve al servicio que había elegido antes de registrarse, si sigue vigente (A-011, REG-09).
  const continuar = () => {
    const pendiente = intencionVigente();
    router.replace('/');
    if (pendiente !== null) router.push({ pathname: '/servicios/[id]', params: { id: String(pendiente) } });
  };

  if (cuenta.verificada) {
    return (
      <Pantalla>
        <Titulo>Verificá tu correo</Titulo>
        <View accessibilityLiveRegion="polite" className="rounded-boton bg-exito-suave p-3 dark:bg-exito-suave-oscuro">
          <Text className="font-nunito-semi text-base text-exito dark:text-exito-oscuro">{mensaje || 'Tu correo ya está verificado.'} Ya ingresaste con tu cuenta.</Text>
        </View>
        <Boton texto="Continuar" icono="arrow-forward-outline" onPress={continuar} />
      </Pantalla>
    );
  }

  return (
    <Pantalla>
      <Titulo>Verificá tu correo</Titulo>
      <Parrafo>
        Enviamos un código de 6 números a <Text className="font-nunito-negrita">{cuenta.correo}</Text>. Vence a los 15 minutos.
      </Parrafo>

      {/* SOLO DEMOSTRACIÓN: todavía no se envían correos reales. */}
      <Tarjeta>
        <Insignia texto="Demostración" tipo="aviso" />
        <Parrafo>
          Como todavía no se envían correos, el código es <Text className="font-nunito-negrita">{codigoDemo(cuenta.correo)}</Text>.
        </Parrafo>
      </Tarjeta>

      {mensaje ? (
        <View
          accessibilityLiveRegion="polite"
          className={`rounded-boton p-3 ${exito ? 'bg-info-suave dark:bg-info-suave-oscuro' : 'bg-peligro-suave dark:bg-peligro-suave-oscuro'}`}
        >
          <Text className={`font-nunito-semi text-base ${exito ? 'text-info dark:text-info-oscuro' : 'text-peligro dark:text-peligro-oscuro'}`}>{mensaje}</Text>
        </View>
      ) : null}

      <Campo
        etiqueta="Código"
        value={codigo}
        onChangeText={setCodigo}
        error={error}
        keyboardType="number-pad"
        autoComplete="one-time-code"
        maxLength={6}
        onSubmitEditing={confirmar}
      />
      <Boton texto="Verificar" icono="checkmark-circle-outline" onPress={confirmar} />
      <Boton texto="Enviarme otro código" variante="secundario" onPress={reenviar} />
      <Nota>Tenés 5 intentos por código y podés pedir hasta 3 códigos por hora.</Nota>
    </Pantalla>
  );
}
